// =============================================================================
// kb-query.ts — spt_kb_query（C7 改薄）
//
// 编排：布局检查 → loadJson → parseIndexForQuery（结构失败响亮报错）
//       → queryIndex（过滤语义在 kb/query.ts）→ okEnv。
//
// 工具信封形状与抽取前保持一致：ok 信封 data = { matches, matchCount, filters }。
// 行为修复（已声明 delta）：version 过滤此前因 string 形态 entry.version.map
// 抛错而 100% 不可用；现由 parseIndexForQuery 防御归一为 [string]。
// =============================================================================

import { z } from "zod";

import { KB_INDEX_REL, loadJson } from "../forge-reader.js";
import { parseIndexForQuery, queryIndex } from "../kb/index.js";
import { getLayout } from "../runtime-layout.js";
import { errEnv, okEnv, SPT_ERROR_CODES, type Envelope } from "../types.js";

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
    const index = loadJson<unknown>(kbRoot, KB_INDEX_REL);

    // 结构校验响亮失败：索引形状非法时明确报错，而不是静默 0 匹配或抛栈
    const parsedIndex = parseIndexForQuery(index);
    if (!parsedIndex.ok) {
      return errEnv(
        "spt_kb_query",
        `知识库索引结构非法：${parsedIndex.reason}`,
        SPT_ERROR_CODES.KB_UNAVAILABLE,
        { hint: `索引路径：${kbRoot}/${KB_INDEX_REL}；请运行 node scripts/spt-kb/validate-index.mjs 校验` },
      );
    }

    const matches = queryIndex(parsedIndex.entries, { topic, domain, version, keyword });

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
