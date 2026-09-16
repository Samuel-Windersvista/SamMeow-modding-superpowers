// 测试辅助：fake BridgeConnection（Phase 2 唯一新接缝）
//
// 与 fake-connection.ts 同构：工具层测试以可编程 fake 驱动，不触网。
// 每个端点可配置为「静态值 / Error / 序号驱动的 responder」；缺省为合法空态
// （info 协议版本匹配、各 raid 端点 inRaid:false），便于按需覆盖单个路径。

import {
  BridgeEndpointUnavailableError,
  BridgeUnreachableError,
  EXPECTED_BRIDGE_PROTOCOL_VERSION,
  type BridgeConnection,
  type BridgeInfo,
  type BridgeLogsRecentResult,
  type BridgeLogsSummaryResult,
  type BridgeRaidBotsResult,
  type BridgeRaidEventsResult,
  type BridgeRaidPlayerResult,
  type BridgeRaidStatusResult,
} from "../../src/bridge/connection.js";

export type FakeResponder<T> = (call: number) => T | Error | Promise<T | Error>;
export type FakeSource<T> = T | Error | FakeResponder<T>;

export interface FakeBridgeOptions {
  info?: FakeSource<BridgeInfo>;
  status?: FakeSource<BridgeRaidStatusResult>;
  player?: FakeSource<BridgeRaidPlayerResult>;
  bots?: FakeSource<BridgeRaidBotsResult>;
  events?: FakeSource<BridgeRaidEventsResult>;
  logsRecent?: FakeSource<BridgeLogsRecentResult>;
  logsSummary?: FakeSource<BridgeLogsSummaryResult>;
}

export class FakeBridgeConnection implements BridgeConnection {
  /** 各端点的调用次数（可观测性） */
  readonly callCounts = {
    info: 0,
    status: 0,
    player: 0,
    bots: 0,
    events: 0,
    logsRecent: 0,
    logsSummary: 0,
  };
  /** 最近一次 getRaidBots 的 detail 入参（校验工具层透传） */
  lastBotsDetail: boolean | null = null;
  /** 最近一次 getRaidEvents 的入参（校验工具层透传） */
  lastEventsArgs: { since?: number; limit?: number } | null = null;
  /** 最近一次 getLogsRecent 的入参（校验工具层透传） */
  lastLogsRecentArgs: { since?: number; level?: string; limit?: number } | null = null;
  /** 最近一次 getLogsSummary 的入参（校验工具层透传） */
  lastLogsSummaryArgs: { since?: string | number } | null = null;

  constructor(private readonly options: FakeBridgeOptions = {}) {}

  async getInfo(): Promise<BridgeInfo> {
    this.callCounts.info += 1;
    return resolve(this.options.info, defaultBridgeInfo(), this.callCounts.info);
  }

  async getRaidStatus(): Promise<BridgeRaidStatusResult> {
    this.callCounts.status += 1;
    return resolve(this.options.status, { inRaid: false }, this.callCounts.status);
  }

  async getRaidPlayer(): Promise<BridgeRaidPlayerResult> {
    this.callCounts.player += 1;
    return resolve(this.options.player, { inRaid: false }, this.callCounts.player);
  }

  async getRaidEvents(since?: number, limit?: number): Promise<BridgeRaidEventsResult> {
    this.callCounts.events += 1;
    this.lastEventsArgs = { since, limit };
    return resolve(
      this.options.events,
      { inRaid: true, seq: 0, dropped: 0, events: [] },
      this.callCounts.events,
    );
  }

  async getRaidBots(detail: boolean): Promise<BridgeRaidBotsResult> {
    this.callCounts.bots += 1;
    this.lastBotsDetail = detail;
    const fallback: BridgeRaidBotsResult = detail
      ? {
          inRaid: true,
          detail: true,
          bots: [],
          truncated: false,
          total: 0,
          alive: 0,
          byCategory: { pmc: 0, scav: 0, boss: 0, other: 0 },
          spawner: { aliveAndLoading: 0, delayed: 0, allWithDelayed: 0 },
          sampleAgeMs: 0,
        }
      : {
          inRaid: true,
          detail: false,
          total: 0,
          alive: 0,
          byCategory: { pmc: 0, scav: 0, boss: 0, other: 0 },
          spawner: { aliveAndLoading: 0, delayed: 0, allWithDelayed: 0 },
          sampleAgeMs: 0,
        };
    return resolve(this.options.bots, fallback, this.callCounts.bots);
  }

  async getLogsRecent(
    since?: number,
    level?: string,
    limit?: number,
  ): Promise<BridgeLogsRecentResult> {
    this.callCounts.logsRecent += 1;
    this.lastLogsRecentArgs = { since, level, limit };
    return resolve(
      this.options.logsRecent,
      { seq: 0, dropped: 0, entries: [] },
      this.callCounts.logsRecent,
    );
  }

  async getLogsSummary(since?: string | number): Promise<BridgeLogsSummaryResult> {
    this.callCounts.logsSummary += 1;
    this.lastLogsSummaryArgs = { since };
    return resolve(
      this.options.logsSummary,
      { groups: [], overflowDropped: 0 },
      this.callCounts.logsSummary,
    );
  }
}

async function resolve<T>(
  source: FakeSource<T> | undefined,
  fallback: T,
  call: number,
): Promise<T> {
  const value = source === undefined ? fallback : source;
  const resolved = typeof value === "function" ? await (value as FakeResponder<T>)(call) : value;
  if (resolved instanceof Error) {
    throw resolved;
  }
  return resolved;
}

export function fakeBridge(options: FakeBridgeOptions = {}): FakeBridgeConnection {
  return new FakeBridgeConnection(options);
}

/** 构造不可达错误（连接类失败） */
export function unreachable(message = "bridge 不可达"): BridgeUnreachableError {
  return new BridgeUnreachableError(message);
}

/** 构造端点缺失错误（旧版桥 404：可达且协议通过，但不支持该端点） */
export function endpointMissing(endpoint = "/logs/recent"): BridgeEndpointUnavailableError {
  return new BridgeEndpointUnavailableError(
    `bridge 端点缺失（404 http://127.0.0.1:49777${endpoint}）：当前桥 DLL 为旧版，不支持 ${endpoint}；请更新桥 DLL（BepInEx/plugins/TarkovRuntimeBridge.dll）后重试`,
    endpoint,
  );
}

// -----------------------------------------------------------------------------
// 工厂：合法默认与 in-raid 采样结果
// -----------------------------------------------------------------------------

export function defaultBridgeInfo(overrides: Partial<BridgeInfo> = {}): BridgeInfo {
  return {
    pluginVersion: "0.1.0",
    protocolVersion: EXPECTED_BRIDGE_PROTOCOL_VERSION,
    capabilities: {
      endpoints: ["/bridge/info", "/raid/status", "/raid/player", "/raid/bots"],
      sections: ["status", "player", "bots"],
    },
    sampling: { intervalMs: 1000 },
    network: { host: "127.0.0.1", port: 49777 },
    ...overrides,
  };
}

export function inRaidStatus(
  overrides: Partial<Omit<Extract<BridgeRaidStatusResult, { inRaid: true }>, "inRaid">> = {},
): BridgeRaidStatusResult {
  return {
    inRaid: true,
    map: "Woods",
    status: "running",
    remainingSeconds: 1234,
    raidId: "raid-abc",
    sampleAgeMs: 100,
    ...overrides,
  };
}

export function inRaidPlayer(
  overrides: Partial<Omit<Extract<BridgeRaidPlayerResult, { inRaid: true }>, "inRaid">> = {},
): BridgeRaidPlayerResult {
  return {
    inRaid: true,
    position: { x: 1, y: 2, z: 3 },
    rotation: { x: 90, y: 45 },
    pose: "stand",
    health: {
      alive: true,
      total: 440,
      parts: {
        Head: 35,
        Chest: 85,
        Stomach: 70,
        LeftArm: 60,
        RightArm: 60,
        LeftLeg: 65,
        RightLeg: 65,
      },
    },
    weapon: {
      tpl: "5447a9cd4bdc2dbd208b4567",
      name: "Colt M4A1",
      ammoInMag: 30,
      ammoInChamber: 1,
    },
    equipment: [
      { slot: "Headwear", tpl: "5aa7e276e5b5b000171d0647", name: "Altyn helmet" },
      { slot: "Armor", tpl: "545cdb794bdc2d3a198b456a", name: "6B43 Zabralo" },
    ],
    sampleAgeMs: 100,
    ...overrides,
  };
}

/** 事件时间线（默认：一条 damage + 一条带击杀者的 death + 一条撤离） */
export function inRaidEvents(
  overrides: Partial<Omit<BridgeRaidEventsResult, "inRaid">> = {},
): BridgeRaidEventsResult {
  return {
    inRaid: true,
    seq: 3,
    dropped: 0,
    events: [
      {
        seq: 1,
        ts: "2026-09-15T10:00:00.000Z",
        type: "damage",
        raidId: "raid-abc",
        payload: {
          victimProfileId: "pmc-local",
          victimIsLocal: true,
          part: "LeftLeg",
          amount: 12.5,
          sourceType: "Bullet",
        },
      },
      {
        seq: 2,
        ts: "2026-09-15T10:00:05.000Z",
        type: "death",
        raidId: "raid-abc",
        payload: {
          victimProfileId: "bot-1",
          victimIsLocal: false,
          damageType: "Bullet",
          killer: {
            profileId: "pmc-local",
            name: "LocalPMC",
            side: "Bear",
            role: "pmc",
            isLocal: true,
          },
        },
      },
      {
        seq: 3,
        ts: "2026-09-15T10:05:00.000Z",
        type: "extraction",
        raidId: "raid-abc",
        payload: { exitName: "Crossroads", status: "Success" },
      },
    ],
    ...overrides,
  };
}

export function inRaidBotsSummary(
  overrides: Partial<Omit<Extract<BridgeRaidBotsResult, { detail: false }>, "inRaid" | "detail">> = {},
): BridgeRaidBotsResult {
  return {
    inRaid: true,
    detail: false,
    total: 10,
    alive: 8,
    byCategory: { pmc: 4, scav: 5, boss: 1, other: 0 },
    spawner: { aliveAndLoading: 9, delayed: 1, allWithDelayed: 10 },
    sampleAgeMs: 100,
    ...overrides,
  };
}

/** 日志条目环（默认：warning / error / fatal 三条，seq 单调） */
export function logsRecent(
  overrides: Partial<BridgeLogsRecentResult> = {},
): BridgeLogsRecentResult {
  return {
    seq: 3,
    dropped: 0,
    entries: [
      {
        seq: 1,
        ts: "2026-09-15T10:00:00.000Z",
        level: "warning",
        source: "Unity",
        text: "ComboBox: value is null",
      },
      {
        seq: 2,
        ts: "2026-09-15T10:00:01.000Z",
        level: "error",
        source: "Assembly-CSharp",
        text: "KeyNotFoundException: loot patch",
      },
      {
        seq: 3,
        ts: "2026-09-15T10:00:02.000Z",
        level: "fatal",
        source: "BepInEx",
        text: "AccessViolationException: TrackableTransform",
      },
    ],
    ...overrides,
  };
}

/** 日志聚合视图（默认：一条刷屏 error 组 + 一条 warning 组） */
export function logsSummary(
  overrides: Partial<BridgeLogsSummaryResult> = {},
): BridgeLogsSummaryResult {
  return {
    groups: [
      {
        key: "KeyNotFoundException: loot patch <n>",
        level: "error",
        source: "Assembly-CSharp",
        count: 6477,
        firstTs: "2026-09-15T10:00:00.000Z",
        lastTs: "2026-09-15T10:00:02.000Z",
        sampleText: "KeyNotFoundException: loot patch 42",
      },
      {
        key: "ComboBox: value is null",
        level: "warning",
        source: "Unity",
        count: 1,
        firstTs: "2026-09-15T09:59:00.000Z",
        lastTs: "2026-09-15T09:59:00.000Z",
        sampleText: "ComboBox: value is null",
      },
    ],
    overflowDropped: 0,
    ...overrides,
  };
}

export function inRaidBotsDetail(
  overrides: Partial<
    Omit<Extract<BridgeRaidBotsResult, { detail: true }>, "inRaid" | "detail">
  > = {},
): BridgeRaidBotsResult {
  return {
    inRaid: true,
    detail: true,
    total: 2,
    alive: 1,
    byCategory: { pmc: 1, scav: 1, boss: 0, other: 0 },
    spawner: { aliveAndLoading: 2, delayed: 0, allWithDelayed: 2 },
    sampleAgeMs: 100,
    bots: [
      { x: 10, y: 0, z: 20, role: "pmc", side: "Bear", alive: true },
      { x: 30, y: 0, z: 40, role: "scav", side: "Savage", alive: false },
    ],
    truncated: false,
    ...overrides,
  };
}
