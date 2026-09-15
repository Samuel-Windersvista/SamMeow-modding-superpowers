// =============================================================================
// BridgeConnection 的 HTTP 实现
//
// 拉取本地 BepInEx Client Bridge（默认 127.0.0.1:49777）的
// `/bridge/info`、`/raid/status`、`/raid/player`、`/raid/bots`、`/raid/events`。
// 使用 Node 22 全局 fetch + AbortSignal.timeout；连接拒绝 / 超时 / 非 2xx /
// JSON 解析失败 / schema 非法一律抛 BridgeUnreachableError。
//
// `/bridge/info` 不缓存：每次调用都实际请求，桥升级后协议校验即时生效
// （本地 HTTP 成本可忽略）；404 视为不可达并在消息提示可能为旧版桥。
// =============================================================================

import {
  BridgeUnreachableError,
  type BridgeCapabilities,
  type BridgeConnection,
  type BridgeInfo,
  type BridgeNetwork,
  type BridgeRaidBotsResult,
  type BridgeRaidEventsResult,
  type BridgeRaidPlayerResult,
  type BridgeRaidStatusResult,
  type BridgeSampling,
  type RaidBotDetail,
  type RaidBotsByCategory,
  type RaidBotsSpawner,
  type RaidEvent,
  type RaidEventKiller,
  type RaidPlayerEquipmentSlot,
  type RaidPlayerHealth,
  type RaidPlayerHealthParts,
  type RaidPlayerPosition,
  type RaidPlayerRotation,
  type RaidPlayerWeapon,
} from "./connection.js";

/** 单次 bridge 请求默认超时（ms） */
export const DEFAULT_BRIDGE_TIMEOUT_MS = 2_000;

export interface HttpBridgeConnectionOptions {
  /** 单次请求超时（ms），默认 2000 */
  timeoutMs?: number;
  /** 注入 fetch（单测用） */
  fetchImpl?: typeof fetch;
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isFiniteNumber(value: unknown): value is number {
  return typeof value === "number" && Number.isFinite(value);
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

function requireString(record: Record<string, unknown>, key: string, label: string): string {
  const value = record[key];
  if (typeof value !== "string") {
    throw new BridgeUnreachableError(`bridge ${label} 响应字段 ${key} 非法`);
  }
  return value;
}

/**
 * 宽容归一字符串字段：缺失 / 非字符串一律归一为 ""（不抛错）。
 * 用于 spike 阶段字段可能缺失的载荷（如 extraction），不因桥侧字段差异判不可达。
 */
function normalizeString(value: unknown): string {
  return typeof value === "string" ? value : "";
}

function requireFiniteNumber(record: Record<string, unknown>, key: string, label: string): number {
  const value = record[key];
  if (!isFiniteNumber(value)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应字段 ${key} 非法`);
  }
  return value;
}

function requireBoolean(record: Record<string, unknown>, key: string, label: string): boolean {
  const value = record[key];
  if (typeof value !== "boolean") {
    throw new BridgeUnreachableError(`bridge ${label} 响应字段 ${key} 非法`);
  }
  return value;
}

function requireStringArray(value: unknown, label: string): string[] {
  if (!Array.isArray(value) || value.some((entry) => typeof entry !== "string")) {
    throw new BridgeUnreachableError(`bridge ${label} 响应字段非法`);
  }
  return [...value];
}

/** 校验 inRaid 判别字段；返回 true/false，非法抛 BridgeUnreachableError */
function parseInRaid(body: unknown, label: string): { inRaid: boolean; record: Record<string, unknown> } {
  if (!isRecord(body) || typeof body.inRaid !== "boolean") {
    throw new BridgeUnreachableError(`bridge ${label} 响应缺少合法的 inRaid 字段`);
  }
  return { inRaid: body.inRaid, record: body };
}

// -----------------------------------------------------------------------------
// 解析器（导出供单测直接驱动）
// -----------------------------------------------------------------------------

/** 校验并归一 `/bridge/info` 响应体；非法抛 BridgeUnreachableError */
export function parseBridgeInfoPayload(body: unknown): BridgeInfo {
  const label = "/bridge/info";
  if (!isRecord(body)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应不是对象`);
  }
  const pluginVersion = requireString(body, "pluginVersion", label);
  const protocolVersion = requireFiniteNumber(body, "protocolVersion", label);

  const capabilitiesRaw = body.capabilities;
  if (!isRecord(capabilitiesRaw)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 capabilities 非法`);
  }
  const capabilities: BridgeCapabilities = {
    endpoints: requireStringArray(capabilitiesRaw.endpoints, `${label} capabilities.endpoints`),
    sections: requireStringArray(capabilitiesRaw.sections, `${label} capabilities.sections`),
  };

  const samplingRaw = body.sampling;
  if (!isRecord(samplingRaw)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 sampling 非法`);
  }
  const sampling: BridgeSampling = {
    intervalMs: requireFiniteNumber(samplingRaw, "intervalMs", `${label} sampling`),
  };

  const networkRaw = body.network;
  if (!isRecord(networkRaw)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 network 非法`);
  }
  const network: BridgeNetwork = {
    host: requireString(networkRaw, "host", `${label} network`),
    port: requireFiniteNumber(networkRaw, "port", `${label} network`),
  };

  return { pluginVersion, protocolVersion, capabilities, sampling, network };
}

/** 校验并归一 `/raid/status` 响应体；非法抛 BridgeUnreachableError */
export function parseRaidStatusPayload(body: unknown): BridgeRaidStatusResult {
  const label = "/raid/status";
  const { inRaid, record } = parseInRaid(body, label);
  if (!inRaid) {
    return { inRaid: false };
  }
  return {
    inRaid: true,
    map: requireString(record, "map", label),
    status: requireString(record, "status", label),
    remainingSeconds: requireFiniteNumber(record, "remainingSeconds", label),
    raidId: requireString(record, "raidId", label),
    sampleAgeMs: requireFiniteNumber(record, "sampleAgeMs", label),
  };
}

function parsePosition(value: unknown, label: string): RaidPlayerPosition {
  if (
    !isRecord(value) ||
    !isFiniteNumber(value.x) ||
    !isFiniteNumber(value.y) ||
    !isFiniteNumber(value.z)
  ) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 position 非法`);
  }
  return { x: value.x, y: value.y, z: value.z };
}

function parseRotation(value: unknown, label: string): RaidPlayerRotation {
  if (!isRecord(value) || !isFiniteNumber(value.x) || !isFiniteNumber(value.y)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 rotation 非法`);
  }
  return { x: value.x, y: value.y };
}

function parseHealth(value: unknown, label: string): RaidPlayerHealth {
  if (!isRecord(value)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 health 非法`);
  }
  const partsRaw = value.parts;
  if (!isRecord(partsRaw)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 health.parts 非法`);
  }
  const parts: RaidPlayerHealthParts = {
    Head: requireFiniteNumber(partsRaw, "Head", `${label} health.parts`),
    Chest: requireFiniteNumber(partsRaw, "Chest", `${label} health.parts`),
    Stomach: requireFiniteNumber(partsRaw, "Stomach", `${label} health.parts`),
    LeftArm: requireFiniteNumber(partsRaw, "LeftArm", `${label} health.parts`),
    RightArm: requireFiniteNumber(partsRaw, "RightArm", `${label} health.parts`),
    LeftLeg: requireFiniteNumber(partsRaw, "LeftLeg", `${label} health.parts`),
    RightLeg: requireFiniteNumber(partsRaw, "RightLeg", `${label} health.parts`),
  };
  return {
    alive: requireBoolean(value, "alive", `${label} health`),
    total: requireFiniteNumber(value, "total", `${label} health`),
    parts,
  };
}

/**
 * 解析玩家武器；缺省（字段不存在）或显式 null 均归一为 null（无武器语义）。
 * 字段存在但非法抛 BridgeUnreachableError。
 */
function parseWeapon(value: unknown, label: string): RaidPlayerWeapon | null {
  if (value === undefined || value === null) {
    return null;
  }
  if (!isRecord(value)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 weapon 非法`);
  }
  return {
    tpl: requireString(value, "tpl", `${label} weapon`),
    name: requireString(value, "name", `${label} weapon`),
    ammoInMag: requireFiniteNumber(value, "ammoInMag", `${label} weapon`),
    ammoInChamber: requireFiniteNumber(value, "ammoInChamber", `${label} weapon`),
  };
}

/** 解析装备槽摘要；缺省归一为空数组（无装备语义） */
function parseEquipment(value: unknown, label: string): RaidPlayerEquipmentSlot[] {
  if (value === undefined || value === null) {
    return [];
  }
  if (!Array.isArray(value)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 equipment 非法`);
  }
  return value.map((entry) => {
    if (!isRecord(entry)) {
      throw new BridgeUnreachableError(`bridge ${label} 响应 equipment[] 条目非法`);
    }
    return {
      slot: requireString(entry, "slot", `${label} equipment[]`),
      tpl: requireString(entry, "tpl", `${label} equipment[]`),
      name: requireString(entry, "name", `${label} equipment[]`),
    };
  });
}

/**
 * 校验并归一 `/raid/player` 响应体；非法抛 BridgeUnreachableError。
 * 归一保持确定性字段序（position/rotation/pose/health/weapon/equipment/sampleAgeMs），
 * 便于测试 diff。
 */
export function parseRaidPlayerPayload(body: unknown): BridgeRaidPlayerResult {
  const label = "/raid/player";
  const { inRaid, record } = parseInRaid(body, label);
  if (!inRaid) {
    return { inRaid: false };
  }
  return {
    inRaid: true,
    position: parsePosition(record.position, label),
    rotation: parseRotation(record.rotation, label),
    pose: requireString(record, "pose", label),
    health: parseHealth(record.health, label),
    weapon: parseWeapon(record.weapon, label),
    equipment: parseEquipment(record.equipment, label),
    sampleAgeMs: requireFiniteNumber(record, "sampleAgeMs", label),
  };
}

function parseByCategory(value: unknown, label: string): RaidBotsByCategory {
  if (!isRecord(value)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 byCategory 非法`);
  }
  return {
    pmc: requireFiniteNumber(value, "pmc", `${label} byCategory`),
    scav: requireFiniteNumber(value, "scav", `${label} byCategory`),
    boss: requireFiniteNumber(value, "boss", `${label} byCategory`),
    other: requireFiniteNumber(value, "other", `${label} byCategory`),
  };
}

function parseSpawner(value: unknown, label: string): RaidBotsSpawner {
  if (!isRecord(value)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 spawner 非法`);
  }
  return {
    aliveAndLoading: requireFiniteNumber(value, "aliveAndLoading", `${label} spawner`),
    delayed: requireFiniteNumber(value, "delayed", `${label} spawner`),
    allWithDelayed: requireFiniteNumber(value, "allWithDelayed", `${label} spawner`),
  };
}

function parseBotDetail(value: unknown, label: string): RaidBotDetail {
  if (!isRecord(value)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 bots[] 条目非法`);
  }
  return {
    x: requireFiniteNumber(value, "x", `${label} bots[]`),
    y: requireFiniteNumber(value, "y", `${label} bots[]`),
    z: requireFiniteNumber(value, "z", `${label} bots[]`),
    role: requireString(value, "role", `${label} bots[]`),
    side: requireString(value, "side", `${label} bots[]`),
    alive: requireBoolean(value, "alive", `${label} bots[]`),
  };
}

/**
 * 校验并归一 `/raid/bots` 响应体；非法抛 BridgeUnreachableError。
 * `expectDetail` 为请求侧意图：明细模式下要求 bots/truncated 存在。
 */
export function parseRaidBotsPayload(body: unknown, expectDetail: boolean): BridgeRaidBotsResult {
  const label = "/raid/bots";
  const { inRaid, record } = parseInRaid(body, label);
  if (!inRaid) {
    return { inRaid: false };
  }
  const summary = {
    total: requireFiniteNumber(record, "total", label),
    alive: requireFiniteNumber(record, "alive", label),
    byCategory: parseByCategory(record.byCategory, label),
    spawner: parseSpawner(record.spawner, label),
    sampleAgeMs: requireFiniteNumber(record, "sampleAgeMs", label),
  };
  if (!expectDetail) {
    return { inRaid: true, detail: false, ...summary };
  }
  const botsRaw = record.bots;
  if (!Array.isArray(botsRaw)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应明细缺少 bots 数组`);
  }
  const bots = botsRaw.map((entry) => parseBotDetail(entry, label));
  return {
    inRaid: true,
    detail: true,
    bots,
    truncated: requireBoolean(record, "truncated", label),
    ...summary,
  };
}

// -----------------------------------------------------------------------------
// /raid/events
// -----------------------------------------------------------------------------

/** 解析 death 事件的击杀者；缺省或显式 null 归一为 null（归属不可得，不猜） */
function parseKiller(value: unknown, label: string): RaidEventKiller | null {
  if (value === undefined || value === null) {
    return null;
  }
  if (!isRecord(value)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应 killer 非法`);
  }
  return {
    profileId: requireString(value, "profileId", `${label} killer`),
    name: requireString(value, "name", `${label} killer`),
    side: requireString(value, "side", `${label} killer`),
    role: requireString(value, "role", `${label} killer`),
    isLocal: requireBoolean(value, "isLocal", `${label} killer`),
  };
}

/**
 * 解析单个事件（判别联合，字段序稳定：seq/ts/type/raidId/payload）。
 * 只读取已知字段，未知字段忽略（桥侧新增字段不炸）。
 */
function parseRaidEvent(value: unknown, label: string): RaidEvent {
  const eventLabel = `${label} events[]`;
  if (!isRecord(value)) {
    throw new BridgeUnreachableError(`bridge ${eventLabel} 条目非法`);
  }
  const seq = requireFiniteNumber(value, "seq", eventLabel);
  const ts = requireString(value, "ts", eventLabel);
  const type = requireString(value, "type", eventLabel);
  const raidId = requireString(value, "raidId", eventLabel);
  const payload = value.payload;
  if (!isRecord(payload)) {
    throw new BridgeUnreachableError(`bridge ${eventLabel} payload 非法`);
  }
  switch (type) {
    case "damage":
      return {
        seq,
        ts,
        type,
        raidId,
        payload: {
          victimProfileId: requireString(payload, "victimProfileId", `${eventLabel} payload`),
          victimIsLocal: requireBoolean(payload, "victimIsLocal", `${eventLabel} payload`),
          part: requireString(payload, "part", `${eventLabel} payload`),
          amount: requireFiniteNumber(payload, "amount", `${eventLabel} payload`),
          sourceType: requireString(payload, "sourceType", `${eventLabel} payload`),
        },
      };
    case "death":
      return {
        seq,
        ts,
        type,
        raidId,
        payload: {
          victimProfileId: requireString(payload, "victimProfileId", `${eventLabel} payload`),
          victimIsLocal: requireBoolean(payload, "victimIsLocal", `${eventLabel} payload`),
          damageType: requireString(payload, "damageType", `${eventLabel} payload`),
          killer: parseKiller(payload.killer, `${eventLabel} payload`),
        },
      };
    case "extraction":
      return {
        seq,
        ts,
        type,
        raidId,
        payload: {
          // 宽容归一：spike 字段差异（缺失/非字符串）不视为响应非法。
          exitName: normalizeString(payload.exitName),
          status: normalizeString(payload.status),
        },
      };
    default:
      throw new BridgeUnreachableError(`bridge ${eventLabel} 事件类型非法：${type}`);
  }
}

/**
 * 校验并归一 `/raid/events` 响应体；非法抛 BridgeUnreachableError。
 * 桥侧路由始终返回 `{inRaid, seq, dropped, events}`（非 raid 时缓冲仍有效），
 * 故 `inRaid:false` 也照常解析，不丢弃字段。归一保持确定性字段序
 * （inRaid/seq/dropped/events）。
 */
export function parseRaidEventsPayload(body: unknown): BridgeRaidEventsResult {
  const label = "/raid/events";
  const { inRaid, record } = parseInRaid(body, label);
  const eventsRaw = record.events;
  if (!Array.isArray(eventsRaw)) {
    throw new BridgeUnreachableError(`bridge ${label} 响应缺少 events 数组`);
  }
  return {
    inRaid,
    seq: requireFiniteNumber(record, "seq", label),
    dropped: requireFiniteNumber(record, "dropped", label),
    events: eventsRaw.map((entry) => parseRaidEvent(entry, label)),
  };
}

// -----------------------------------------------------------------------------
// HTTP 连接
// -----------------------------------------------------------------------------

export class HttpBridgeConnection implements BridgeConnection {
  readonly host: string;
  readonly port: number;
  readonly baseUrl: string;

  private readonly timeoutMs: number;
  private readonly fetchImpl: typeof fetch;

  constructor(host: string, port: number, options: HttpBridgeConnectionOptions = {}) {
    this.host = host;
    this.port = port;
    this.baseUrl = `http://${host}:${port}`;
    this.timeoutMs = options.timeoutMs ?? DEFAULT_BRIDGE_TIMEOUT_MS;
    this.fetchImpl = options.fetchImpl ?? fetch;
  }

  /** 单次 GET + JSON 解析；连接拒绝/超时/非 2xx/非 JSON 抛 BridgeUnreachableError */
  private async requestJson(path: string, allow404Hint = false): Promise<unknown> {
    const url = `${this.baseUrl}${path}`;

    let response: Response;
    try {
      response = await this.fetchImpl(url, { signal: AbortSignal.timeout(this.timeoutMs) });
    } catch (error) {
      throw new BridgeUnreachableError(`无法连接 bridge（${url}）：${errorMessage(error)}`, {
        cause: error,
      });
    }

    if (!response.ok) {
      if (allow404Hint && response.status === 404) {
        throw new BridgeUnreachableError(
          `bridge 返回 404（${url}）：可能为旧版桥，不支持 ${path}；请更新 bridge 插件`,
        );
      }
      throw new BridgeUnreachableError(`bridge 返回非 2xx 状态：${response.status}（${url}）`);
    }

    try {
      return await response.json();
    } catch (error) {
      throw new BridgeUnreachableError(`bridge 响应不是合法 JSON（${url}）：${errorMessage(error)}`, {
        cause: error,
      });
    }
  }

  async getInfo(): Promise<BridgeInfo> {
    // 不缓存：每次工具调用都实际拉取，桥升级后协议校验即时生效。
    const body = await this.requestJson("/bridge/info", true);
    return parseBridgeInfoPayload(body);
  }

  async getRaidStatus(): Promise<BridgeRaidStatusResult> {
    return parseRaidStatusPayload(await this.requestJson("/raid/status"));
  }

  async getRaidPlayer(): Promise<BridgeRaidPlayerResult> {
    return parseRaidPlayerPayload(await this.requestJson("/raid/player"));
  }

  async getRaidBots(detail: boolean): Promise<BridgeRaidBotsResult> {
    const path = detail ? "/raid/bots?detail=1" : "/raid/bots";
    return parseRaidBotsPayload(await this.requestJson(path), detail);
  }

  async getRaidEvents(since?: number, limit?: number): Promise<BridgeRaidEventsResult> {
    // 查询参数顺序固定（since -> limit），便于录制/回放与断言。
    const params: string[] = [];
    if (since !== undefined) {
      params.push(`since=${since}`);
    }
    if (limit !== undefined) {
      params.push(`limit=${limit}`);
    }
    const suffix = params.length > 0 ? `?${params.join("&")}` : "";
    return parseRaidEventsPayload(await this.requestJson(`/raid/events${suffix}`));
  }
}
