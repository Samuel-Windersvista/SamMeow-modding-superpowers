// =============================================================================
// BridgeConnection 的 HTTP 实现
//
// 拉取本地 BepInEx Client Bridge（默认 127.0.0.1:49777）的
// `/bridge/info`、`/raid/status`、`/raid/player`、`/raid/bots`。
// 使用 Node 22 全局 fetch + AbortSignal.timeout；连接拒绝 / 超时 / 非 2xx /
// JSON 解析失败 / schema 非法一律抛 BridgeUnreachableError。
//
// `/bridge/info` 成功后缓存（桥生命周期内版本/能力不变）；404 视为不可达并在
// 消息提示可能为旧版桥（不支持 /bridge/info）。
// =============================================================================

import {
  BridgeUnreachableError,
  type BridgeCapabilities,
  type BridgeConnection,
  type BridgeInfo,
  type BridgeNetwork,
  type BridgeRaidBotsResult,
  type BridgeRaidPlayerResult,
  type BridgeRaidStatusResult,
  type BridgeSampling,
  type RaidBotDetail,
  type RaidBotsByCategory,
  type RaidBotsSpawner,
  type RaidPlayerHealth,
  type RaidPlayerHealthParts,
  type RaidPlayerPosition,
  type RaidPlayerRotation,
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
 * 校验并归一 `/raid/player` 响应体；非法抛 BridgeUnreachableError。
 * 归一保持确定性字段序（position/rotation/pose/health/sampleAgeMs），便于测试 diff。
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
// HTTP 连接
// -----------------------------------------------------------------------------

export class HttpBridgeConnection implements BridgeConnection {
  readonly host: string;
  readonly port: number;
  readonly baseUrl: string;

  private readonly timeoutMs: number;
  private readonly fetchImpl: typeof fetch;
  private cachedInfo: BridgeInfo | null = null;

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
    if (this.cachedInfo) {
      return this.cachedInfo;
    }
    const body = await this.requestJson("/bridge/info", true);
    const info = parseBridgeInfoPayload(body);
    this.cachedInfo = info;
    return info;
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
}
