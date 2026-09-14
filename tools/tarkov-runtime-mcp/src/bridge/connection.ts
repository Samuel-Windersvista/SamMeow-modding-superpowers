// =============================================================================
// Phase 2 唯一新接缝：BridgeConnection
//
// 工具层（raid_status / raid_player / raid_bots）只依赖本抽象；HTTP 实现
// （以及未来 WebSocket / 命名管道）封装在其后，测试以 fake 实现驱动。
//
// 错误模型（T03）：
//   - 连接类失败（拒绝/超时/非 2xx/响应非法）统一抛 BridgeUnreachableError，
//     工具层映射为 BRIDGE_UNREACHABLE（含「未安装/未运行/启动失败」提示）；
//   - `/bridge/info` 的 protocolVersion 与 EXPECTED_BRIDGE_PROTOCOL_VERSION
//     不一致时，工具层映射为 BRIDGE_VERSION_MISMATCH；
//   - `inRaid:false` 映射为 NOT_IN_RAID；
//   - 未注册的 raid.* 前缀仍回落 CLIENT_BRIDGE_NOT_INSTALLED。
// =============================================================================

/** MCP 期望的桥协议版本（握手门禁；与桥侧 `protocolVersion` 比对） */
export const EXPECTED_BRIDGE_PROTOCOL_VERSION = 1;

// -----------------------------------------------------------------------------
// /bridge/info
// -----------------------------------------------------------------------------

/** 桥自报能力清单 */
export interface BridgeCapabilities {
  /** 已实现端点（如 /bridge/info、/raid/status） */
  endpoints: string[];
  /** 已实现的状态 section */
  sections: string[];
}

/** 桥采样配置 */
export interface BridgeSampling {
  intervalMs: number;
}

/** 桥网络配置 */
export interface BridgeNetwork {
  host: string;
  port: number;
}

/** `/bridge/info` 的解析结果（字段序稳定） */
export interface BridgeInfo {
  pluginVersion: string;
  protocolVersion: number;
  capabilities: BridgeCapabilities;
  sampling: BridgeSampling;
  network: BridgeNetwork;
}

// -----------------------------------------------------------------------------
// /raid/status
// -----------------------------------------------------------------------------

/** `/raid/status` 的采样结果（判别联合，字段序稳定） */
export type BridgeRaidStatusResult =
  | { inRaid: false }
  | {
      inRaid: true;
      map: string;
      status: string;
      remainingSeconds: number;
      raidId: string;
      sampleAgeMs: number;
    };

// -----------------------------------------------------------------------------
// /raid/player
// -----------------------------------------------------------------------------

/** 玩家位置（EFT 世界坐标） */
export interface RaidPlayerPosition {
  x: number;
  y: number;
  z: number;
}

/** 玩家朝向（yaw/pitch） */
export interface RaidPlayerRotation {
  x: number;
  y: number;
}

/** 玩家各肢体血量 */
export interface RaidPlayerHealthParts {
  Head: number;
  Chest: number;
  Stomach: number;
  LeftArm: number;
  RightArm: number;
  LeftLeg: number;
  RightLeg: number;
}

/** 玩家血量（总 + 各肢体） */
export interface RaidPlayerHealth {
  alive: boolean;
  total: number;
  parts: RaidPlayerHealthParts;
}

/** `/raid/player` 的采样结果（判别联合，字段序稳定） */
export type BridgeRaidPlayerResult =
  | { inRaid: false }
  | {
      inRaid: true;
      position: RaidPlayerPosition;
      rotation: RaidPlayerRotation;
      pose: string;
      health: RaidPlayerHealth;
      sampleAgeMs: number;
    };

// -----------------------------------------------------------------------------
// /raid/bots
// -----------------------------------------------------------------------------

/** bot 分类计数 */
export interface RaidBotsByCategory {
  pmc: number;
  scav: number;
  boss: number;
  other: number;
}

/** bot 生成器计数 */
export interface RaidBotsSpawner {
  aliveAndLoading: number;
  delayed: number;
  allWithDelayed: number;
}

/** bot 明细条目 */
export interface RaidBotDetail {
  x: number;
  y: number;
  z: number;
  role: string;
  side: string;
  alive: boolean;
}

/** bot 摘要字段（摘要/明细两模式共有） */
export interface RaidBotsSummary {
  total: number;
  alive: number;
  byCategory: RaidBotsByCategory;
  spawner: RaidBotsSpawner;
  sampleAgeMs: number;
}

/**
 * `/raid/bots` 的采样结果（判别联合，字段序稳定）。
 * `detail` 为判别字段：false 时无 bots/truncated；true 时携带明细与截断标记。
 */
export type BridgeRaidBotsResult =
  | { inRaid: false }
  | ({ inRaid: true; detail: false } & RaidBotsSummary)
  | ({
      inRaid: true;
      detail: true;
      bots: RaidBotDetail[];
      truncated: boolean;
    } & RaidBotsSummary);

// -----------------------------------------------------------------------------
// 连接抽象
// -----------------------------------------------------------------------------

/** 桥连接层失败：连接拒绝 / 超时 / 非 2xx / 响应体非法 */
export class BridgeUnreachableError extends Error {
  constructor(message: string, options?: { cause?: unknown }) {
    super(message);
    this.name = "BridgeUnreachableError";
    if (options && "cause" in options) {
      this.cause = options.cause;
    }
  }
}

export interface BridgeConnection {
  /**
   * 拉取桥自报（版本/协议/能力/采样/网络）；HTTP 实现在成功后缓存。
   * 连接类失败抛 BridgeUnreachableError；404 亦视为不可达（可能为旧版桥）。
   */
  getInfo(): Promise<BridgeInfo>;
  /** 拉取当前 raid 元数据；连接类失败抛 BridgeUnreachableError */
  getRaidStatus(): Promise<BridgeRaidStatusResult>;
  /** 拉取当前 raid 玩家全字段；连接类失败抛 BridgeUnreachableError */
  getRaidPlayer(): Promise<BridgeRaidPlayerResult>;
  /** 拉取当前 raid bot 摘要（detail=true 时含明细）；连接类失败抛 BridgeUnreachableError */
  getRaidBots(detail: boolean): Promise<BridgeRaidBotsResult>;
}
