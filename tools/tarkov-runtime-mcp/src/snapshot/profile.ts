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

import type { PmcSummary, ProfileSection, SectionMeta, SkillCounts } from "./schema.js";
import { asRecordArray, countQuests, isRecord, readNumber } from "./util.js";

/** profile section 的数据来源路由 */
export const PROFILE_ROUTE = "/client/game/profile/list";

/** profile section 元信息（来源路由 + 新鲜度） */
export function profileMeta(): SectionMeta {
  return { source: "route", route: PROFILE_ROUTE, freshness: "live" };
}

/** 从路由响应体提取 profile 条目数组；无法识别时返回空数组 */
function asProfileList(body: unknown): Record<string, unknown>[] {
  if (Array.isArray(body)) return asRecordArray(body);
  if (isRecord(body) && Array.isArray(body.profiles)) return asRecordArray(body.profiles);
  return [];
}

/** 技能计数：total=条目总数，leveled=progress > 0 的条目数 */
function countSkills(skills: unknown): SkillCounts {
  const common = isRecord(skills) && Array.isArray(skills.common) ? asRecordArray(skills.common) : [];
  let leveled = 0;
  for (const skill of common) {
    if (readNumber(skill.progress) > 0) {
      leveled += 1;
    }
  }
  return { total: common.length, leveled };
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
