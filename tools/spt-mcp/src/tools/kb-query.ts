import { z } from "zod";

import { KB_INDEX_REL, loadJson } from "../forge-reader.js";
import { getLayout } from "../runtime-layout.js";
import { errEnv, okEnv, SPT_ERROR_CODES, type Envelope, type KbEntry } from "../types.js";

export const KbQueryInput = z
  .object({
    topic: z.string().optional().describe("条目 topic 精确匹配（如 'config'、'recipe'、'migration'）"),
    domain: z.string().optional().describe("领域过滤：server / client / both（'both' 条目始终包含）"),
    version: z.string().optional().describe("版本过滤（如 '4.1'；条目 version 含 '通用' 时始终包含）"),
    keyword: z.string().optional().describe("对标题做不区分大小写的子串匹配"),
  })
  .strict();

export function runKbQuery(args: unknown): Envelope {
  const parsed = KbQueryInput.safeParse(args);
  if (!parsed.success) {
    return errEnv(
      "spt_kb_query",
      "无效输入",
      SPT_ERROR_CODES.INVALID_INPUT,
      { hint: parsed.error.message },
    );
  }
  const { topic, domain, version, keyword } = parsed.data;

  try {
    // 布局检查：知识库索引不可用时显式降级（不再静默返回 0 条匹配）
    const layout = getLayout();
    if (!layout.kb.index.ok) {
      return errEnv(
        "spt_kb_query",
        "知识库不可用",
        SPT_ERROR_CODES.KB_UNAVAILABLE,
        { hint: layout.kb.index.reason },
      );
    }

    const kbRoot = layout.kb.root.path;
    const index = loadJson<{ entries?: KbEntry[] }>(kbRoot, KB_INDEX_REL);
    const entries = index?.entries ?? [];

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

    return okEnv(
      "spt_kb_query",
      `知识库匹配 ${matches.length} 条`,
      {
        matches,
        matchCount: matches.length,
        filters: { topic, domain, version, keyword },
      },
    );
  } catch (error) {
    return errEnv(
      "spt_kb_query",
      `查询失败：${error instanceof Error ? error.message : String(error)}`,
      SPT_ERROR_CODES.INTERNAL_ERROR,
    );
  }
}
