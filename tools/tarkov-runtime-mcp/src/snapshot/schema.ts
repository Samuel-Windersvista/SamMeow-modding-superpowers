// =============================================================================
// 快照 schema 与常量
//
// 快照是某一时刻游戏状态的确定性读取结果（CONTEXT.md「Snapshot」）：
//   - 字段顺序固定、无时间戳噪声 -> 相同输入逐字节一致，可直接做金样本断言；
//   - 计数型摘要而非全量枚举；
//   - 每个 section 标注数据来源（路由名）与新鲜度。
//
// 本文件只定义结构与常量，不含 IO。
// =============================================================================

/** 快照 schema 版本；结构发生不兼容变更时递增 */
export const SNAPSHOT_SCHEMA_VERSION = 1;

/**
 * 本期实现的 section 名（工具层校验与能力自报共用）。
 *
 * 数组顺序即快照输出字段顺序（确定性），与入参顺序无关。
 */
export const SUPPORTED_SECTIONS = ["profile", "traders", "quests", "hideout", "inventory"] as const;

export type SnapshotSectionName = (typeof SUPPORTED_SECTIONS)[number];

/** section 数据来源种类：route=SPT server `/client/*` 路由；server-log=server 日志兜底（工单 04） */
export type SectionSourceKind = "route" | "server-log";

/**
 * section 元信息：数据来源 + 新鲜度。
 *
 * 新鲜度用类别而非墙钟时间戳表达，避免破坏快照确定性（spec 用户故事 16）：
 * `live` 表示数据在本次调用内实时读取，非缓存。
 */
export interface SectionMeta {
  source: SectionSourceKind;
  /** source=route 时的完整路由路径；非 route 来源为 null */
  route: string | null;
  freshness: "live";
}

/** 技能计数摘要 */
export interface SkillCounts {
  /** 已记录技能条目总数 */
  total: number;
  /** 有进度（progress > 0）的技能数 */
  leveled: number;
}

/** 任务进度计数摘要 */
export interface QuestCounts {
  available: number;
  inProgress: number;
  completed: number;
  failed: number;
}

/** PMC 角色摘要（计数型，不含全量技能/任务枚举） */
export interface PmcSummary {
  level: number;
  experience: number;
  skills: SkillCounts;
  quests: QuestCounts;
}

/** profile section 摘要 */
export interface ProfileSection {
  meta: SectionMeta;
  /** 存档中的 profile 条目数 */
  profileCount: number;
  /** 主 PMC 角色摘要；无 PMC 数据时为 null */
  pmc: PmcSummary | null;
}

/** 单个商人的计数摘要（好感 / 忠诚等级 / assort 条目数） */
export interface TraderSummary {
  /** 商人 id（MongoId 字符串） */
  id: string;
  /** 玩家对该商人的好感度（standing） */
  standing: number;
  /** 玩家忠诚等级 */
  loyaltyLevel: number;
  /** 该商人 assort 条目数 */
  assortItems: number;
}

/** traders section 摘要 */
export interface TradersSection {
  meta: SectionMeta;
  /** 商人总数 */
  traderCount: number;
  /** 全部商人 assort 条目总数 */
  totalAssortItems: number;
  /** 按 id 升序的逐商人摘要 */
  traders: TraderSummary[];
}

/** quests section 摘要（可用 / 进行 / 完成 / 失败计数） */
export interface QuestsSection {
  meta: SectionMeta;
  /** 任务条目总数 */
  questCount: number;
  counts: QuestCounts;
}

/** 单个藏身处区域的等级摘要 */
export interface HideoutAreaSummary {
  /** 区域类型（HideoutAreas 枚举值） */
  type: number;
  /** 当前等级；0 = 未建造 */
  level: number;
}

/** hideout section 摘要 */
export interface HideoutSection {
  meta: SectionMeta;
  /** 区域条目总数 */
  areaCount: number;
  /** 已建造（level > 0）的区域数 */
  builtAreas: number;
  /** 全部区域等级之和 */
  totalLevel: number;
  /** 按 type 升序的逐区域等级 */
  areas: HideoutAreaSummary[];
}

/** inventory section 摘要（物品计数，非全量枚举） */
export interface InventorySection {
  meta: SectionMeta;
  /** 物品条目总数 */
  itemCount: number;
  /** 不同模板（_tpl）数 */
  distinctTemplates: number;
}

/** 快照 sections 容器；字段顺序即输出顺序（确定性） */
export interface SnapshotSections {
  profile?: ProfileSection;
  traders?: TradersSection;
  quests?: QuestsSection;
  hideout?: HideoutSection;
  inventory?: InventorySection;
}

/** 完整快照 */
export interface Snapshot {
  schemaVersion: number;
  sections: SnapshotSections;
}
