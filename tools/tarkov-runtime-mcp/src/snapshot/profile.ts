// =============================================================================
// profile section 归一
//
// 数据来源：`POST /client/game/profile/list`（SPT server 现有 `/client/*` 路由，
// 零桥直连；见 ADR-0003）。响应信封 `{err, errmsg, data}` 由 util.unwrapEnvelope
// 统一解开（assembler 调用）。
//
// 5.0 live 实测（2026-09-13）data 为「扁平 profile」数组，字段 PascalCase：
//   `_id` / `savage`（PMC 指向 scav profileId；scav 条目为 null）
//   `Info`（`Level` / `Experience`）、`Skills.Common`（`Id` / `Progress`）、
//   `Quests`（`qid` / `status`）、`Inventory` / `Hideout` / `TradersInfo`。
// 与 KB `api-notes-5.0/save-profile.md` 的存档文件结构（`info`/`characters.pmc`）不同，
// 路由面已扁平化。
//
// 归一为计数型摘要：等级 / 经验 / 技能计数 / 任务进度计数，不做全量枚举。
// =============================================================================

import type { PmcSummary, ProfileSection, SectionMeta, SkillCounts } from "./schema.js";
import { asRecordArray, countQuests, isRecord, readNumber, readString } from "./util.js";

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

/**
 * 选取 PMC 条目：PMC 的 `savage` 指向 scav profileId（非空），scav 条目为 null。
 * 无匹配时回落到首个条目。
 */
function selectPmcProfile(
  profiles: Record<string, unknown>[],
): Record<string, unknown> | null {
  const pmc = profiles.find((entry) => readString(entry.savage) !== "");
  return pmc ?? profiles[0] ?? null;
}

/** 技能计数：total=条目总数，leveled=progress > 0 的条目数（兼容 PascalCase / camelCase） */
function countSkills(skills: unknown): SkillCounts {
  let common: Record<string, unknown>[] = [];
  if (isRecord(skills)) {
    if (Array.isArray(skills.Common)) common = asRecordArray(skills.Common);
    else if (Array.isArray(skills.common)) common = asRecordArray(skills.common);
  }
  let leveled = 0;
  for (const skill of common) {
    const progress = "Progress" in skill ? skill.Progress : skill.progress;
    if (readNumber(progress) > 0) {
      leveled += 1;
    }
  }
  return { total: common.length, leveled };
}

/** 提取 PMC 摘要；条目缺 `Info` 时返回 null */
function extractPmc(profile: Record<string, unknown>): PmcSummary | null {
  const info = profile.Info;
  if (!isRecord(info)) return null;
  return {
    level: readNumber(info.Level),
    experience: readNumber(info.Experience),
    skills: countSkills(profile.Skills),
    quests: countQuests(profile.Quests),
  };
}

/** 把 `/client/game/profile/list` 响应归一为 profile section 摘要 */
export function normalizeProfileSection(body: unknown): ProfileSection {
  const profiles = asProfileList(body);
  const pmc = selectPmcProfile(profiles);
  return {
    meta: profileMeta(),
    profileCount: profiles.length,
    pmc: pmc ? extractPmc(pmc) : null,
  };
}
