// =============================================================================
// profile section 归一
//
// 数据来源：`POST /client/game/profile/list`（SPT server 现有 `/client/*` 路由，
// 零桥直连；见 ADR-0003）。响应形状依据 KB `api-notes-5.0/save-profile.md`
// 的 SptProfile 结构（info / characters.pmc），字段名采用 SPT JsonUtil 的
// camelCase 序列化约定。
//
// 归一为计数型摘要：等级 / 经验 / 技能计数 / 任务进度计数，不做全量枚举。
// =============================================================================

import type {
  PmcSummary,
  ProfileSection,
  QuestCounts,
  SectionMeta,
  SkillCounts,
} from "./schema.js";

/** profile section 的数据来源路由 */
export const PROFILE_ROUTE = "/client/game/profile/list";

/** profile section 元信息（来源路由 + 新鲜度） */
export function profileMeta(): SectionMeta {
  return { source: "route", route: PROFILE_ROUTE, freshness: "live" };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** 有限数字取值；缺失/非法回落到 0（确定性） */
function readNumber(value: unknown): number {
  return typeof value === "number" && Number.isFinite(value) ? value : 0;
}

/** 从路由响应体提取 profile 条目数组；无法识别时返回空数组 */
function asProfileList(body: unknown): Record<string, unknown>[] {
  const candidate = Array.isArray(body)
    ? body
    : isRecord(body) && Array.isArray(body.profiles)
      ? body.profiles
      : [];
  return candidate.filter(isRecord);
}

/** 技能计数：total=条目总数，leveled=progress > 0 的条目数 */
function countSkills(skills: unknown): SkillCounts {
  const common =
    isRecord(skills) && Array.isArray(skills.common) ? skills.common.filter(isRecord) : [];
  let leveled = 0;
  for (const skill of common) {
    if (readNumber(skill.progress) > 0) {
      leveled += 1;
    }
  }
  return { total: common.length, leveled };
}

/** EFT QuestStatus 名称 -> 计数桶（小写匹配） */
const QUEST_STATUS_BY_NAME: Record<string, keyof QuestCounts> = {
  availableforstart: "available",
  started: "inProgress",
  availableforfinish: "inProgress",
  success: "completed",
  fail: "failed",
  failrestartable: "failed",
  markedasfailed: "failed",
  expired: "failed",
};

/** EFT QuestStatus 枚举值 -> 计数桶（状态被序列化为数字时的兜底） */
const QUEST_STATUS_BY_NUMBER: Record<number, keyof QuestCounts> = {
  1: "available",
  2: "inProgress",
  3: "inProgress",
  4: "completed",
  5: "failed",
  6: "failed",
  7: "failed",
  8: "failed",
};

/** 任务状态归类；未知状态返回 null（不计入任何桶） */
function classifyQuestStatus(status: unknown): keyof QuestCounts | null {
  if (typeof status === "string") {
    return QUEST_STATUS_BY_NAME[status.toLowerCase()] ?? null;
  }
  if (typeof status === "number") {
    return QUEST_STATUS_BY_NUMBER[status] ?? null;
  }
  return null;
}

/** 任务进度计数：可用 / 进行（含可交付）/ 完成 / 失败 */
function countQuests(quests: unknown): QuestCounts {
  const list = Array.isArray(quests) ? quests.filter(isRecord) : [];
  const counts: QuestCounts = { available: 0, inProgress: 0, completed: 0, failed: 0 };
  for (const quest of list) {
    const bucket = classifyQuestStatus(quest.status);
    if (bucket) {
      counts[bucket] += 1;
    }
  }
  return counts;
}

/** 提取 PMC 摘要；无 `characters.pmc` 时返回 null */
function extractPmc(profile: Record<string, unknown>): PmcSummary | null {
  const characters = profile.characters;
  if (!isRecord(characters)) return null;
  const pmc = characters.pmc;
  if (!isRecord(pmc)) return null;
  const info = isRecord(pmc.info) ? pmc.info : {};
  return {
    level: readNumber(info.level),
    experience: readNumber(info.experience),
    skills: countSkills(pmc.skills),
    quests: countQuests(pmc.quests),
  };
}

/** 把 `/client/game/profile/list` 响应归一为 profile section 摘要 */
export function normalizeProfileSection(body: unknown): ProfileSection {
  const profiles = asProfileList(body);
  const first = profiles[0];
  return {
    meta: profileMeta(),
    profileCount: profiles.length,
    pmc: first ? extractPmc(first) : null,
  };
}
