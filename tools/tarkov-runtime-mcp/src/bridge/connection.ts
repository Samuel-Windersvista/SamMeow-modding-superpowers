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
//   - `inRaid:false` 映射为 NOT_IN_RAID（例外：`/raid/events` 在非 raid 时仍
//     返回缓冲，故 raid_events 工具不返回 NOT_IN_RAID，inRaid 仅为状态字段）；
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

/** 玩家当前手持武器（无武器时为 null） */
export interface RaidPlayerWeapon {
  /** 武器模板 id */
  tpl: string;
  /** 武器本地化名（桥侧尽力而为；不可得时为空串） */
  name: string;
  /** 弹匣内弹药数 */
  ammoInMag: number;
  /** 膛内弹药数 */
  ammoInChamber: number;
}

/** 玩家装备槽摘要（头盔/护甲/背包等） */
export interface RaidPlayerEquipmentSlot {
  /** 槽位名（如 Headwear/Armor/Backpack） */
  slot: string;
  /** 物品模板 id */
  tpl: string;
  /** 物品本地化名（不可得时为空串） */
  name: string;
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
      /** 当前手持武器；无武器时为 null */
      weapon: RaidPlayerWeapon | null;
      /** 装备槽摘要；无装备时为空数组 */
      equipment: RaidPlayerEquipmentSlot[];
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
// /raid/events
// -----------------------------------------------------------------------------

/** 事件类型（桥侧单点常量；未知类型视为响应非法） */
export type RaidEventType = "damage" | "death" | "extraction";

/** damage 事件载荷（字段序稳定） */
export interface RaidDamageEventPayload {
  victimProfileId: string;
  victimIsLocal: boolean;
  /** 受击部位（EBodyPart 名，如 Head/Chest） */
  part: string;
  /** 伤害量 */
  amount: number;
  /** 伤害来源类型（如 Bullet/Explosion/Fall） */
  sourceType: string;
}

/** death 事件的击杀者归属（不可得时为 null，不猜） */
export interface RaidEventKiller {
  profileId: string;
  name: string;
  side: string;
  role: string;
  isLocal: boolean;
}

/** death 事件载荷（字段序稳定） */
export interface RaidDeathEventPayload {
  victimProfileId: string;
  victimIsLocal: boolean;
  /** 致死伤害类型（EDamageType 名） */
  damageType: string;
  /** 击杀 = death 且 killer != null；归属不可得时为 null */
  killer: RaidEventKiller | null;
}

/** extraction 事件载荷（本地玩家撤离；字段以桥侧 spike 结论为准） */
export interface RaidExtractionEventPayload {
  /** 撤离点名称 */
  exitName: string;
  /** 撤离状态（如 Success/Failed） */
  status: string;
}

/** 单个事件（判别联合，字段序稳定：seq/ts/type/raidId/payload） */
export type RaidEvent =
  | { seq: number; ts: string; type: "damage"; raidId: string; payload: RaidDamageEventPayload }
  | { seq: number; ts: string; type: "death"; raidId: string; payload: RaidDeathEventPayload }
  | {
      seq: number;
      ts: string;
      type: "extraction";
      raidId: string;
      payload: RaidExtractionEventPayload;
    };

/**
 * `/raid/events` 的增量拉取结果（字段序稳定）。
 * 桥侧路由**始终**返回该结构：`inRaid` 仅为状态字段，非 raid 时仍携带缓冲
 * （事件带 raidId，跨 raid 保留），因此赛后时间线（含撤离事件）可读。
 * `dropped>0` 表示 since 过旧已被环形缓冲淘汰。
 */
export interface BridgeRaidEventsResult {
  /** 当前是否在 raid（false 时 seq/dropped/events 仍为有效缓冲内容） */
  inRaid: boolean;
  /** 桥进程内当前最新事件序号 */
  seq: number;
  /** since 过旧被环形缓冲淘汰的事件数 */
  dropped: number;
  events: RaidEvent[];
}

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

/** BridgeConnection 面方法名（录制/回放共用的单一来源） */
export const BRIDGE_METHODS = [
  "getInfo",
  "getRaidStatus",
  "getRaidPlayer",
  "getRaidBots",
  "getRaidEvents",
] as const;

/** BridgeConnection 方法名（由 BRIDGE_METHODS 派生） */
export type BridgeMethod = (typeof BRIDGE_METHODS)[number];

export interface BridgeConnection {
  /**
   * 拉取桥自报（版本/协议/能力/采样/网络）。
   * 每次调用都实际请求（协议校验即时生效）；连接类失败抛 BridgeUnreachableError；
   * 404 亦视为不可达（可能为旧版桥）。
   */
  getInfo(): Promise<BridgeInfo>;
  /** 拉取当前 raid 元数据；连接类失败抛 BridgeUnreachableError */
  getRaidStatus(): Promise<BridgeRaidStatusResult>;
  /** 拉取当前 raid 玩家全字段（含武器/装备）；连接类失败抛 BridgeUnreachableError */
  getRaidPlayer(): Promise<BridgeRaidPlayerResult>;
  /** 拉取当前 raid bot 摘要（detail=true 时含明细）；连接类失败抛 BridgeUnreachableError */
  getRaidBots(detail: boolean): Promise<BridgeRaidBotsResult>;
  /**
   * 增量拉取 raid 事件（`since` = 已消费的最后一条事件的 seq，缺省从最旧开始；
   * `seq` 字段是最新序号，仅用于判断是否有新事件；`limit` 截断时用最后一条已返回
   * 事件的 seq 续拉）。连接类失败抛 BridgeUnreachableError。
   */
  getRaidEvents(since?: number, limit?: number): Promise<BridgeRaidEventsResult>;
}
