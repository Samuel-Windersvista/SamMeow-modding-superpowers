// =============================================================================
// kb/query.ts — 知识库条目过滤（自 tools/kb-query.ts 抽出，语义逐字保持）
//
// 过滤语义（与抽取前完全一致）：
//   - topic   ：精确匹配，不区分大小写
//   - domain  ：精确匹配，不区分大小写；条目 domain 为 "both" 时始终命中
//   - version ：条目 version 数组包含目标值（不区分大小写），或含 "通用" 时始终命中
//   - keyword ：对 title 做不区分大小写的子串匹配
//   - 输出按 path 升序（localeCompare）
// =============================================================================

import type { KbIndexEntry } from "./contract.js";

export interface KbQueryFilters {
  topic?: string;
  domain?: string;
  version?: string;
  keyword?: string;
}

/** 按过滤条件筛选条目（无副作用；返回新数组，已按 path 排序） */
export function queryIndex(
  entries: KbIndexEntry[],
  filters: KbQueryFilters,
): KbIndexEntry[] {
  const { topic, domain, version, keyword } = filters;

  const keywordLower = keyword?.trim().toLowerCase();
  const topicLower = topic?.trim().toLowerCase();
  const domainLower = domain?.trim().toLowerCase();
  const versionTrim = version?.trim();

  const matches = entries.filter((entry) => {
    if (topicLower) {
      if (entry.topic.toLowerCase() !== topicLower) return false;
    }
    if (domainLower) {
      const d = entry.domain.toLowerCase();
      if (d !== domainLower && d !== "both") return false;
    }
    if (versionTrim) {
      const versions = entry.version.map((v) => v.toLowerCase());
      const want = versionTrim.toLowerCase();
      if (!versions.includes(want) && !versions.includes("通用")) return false;
    }
    if (keywordLower) {
      if (!entry.title.toLowerCase().includes(keywordLower)) return false;
    }
    return true;
  });

  matches.sort((a, b) => a.path.localeCompare(b.path));
  return matches;
}
