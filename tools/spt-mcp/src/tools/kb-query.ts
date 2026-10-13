// =============================================================================
// kb-query.ts — spt_kb_query（多库版）
//
// 编排：注册表解析 → 逐库 loadIndex + parseIndexForQuery（结构失败响亮报错）
//       → queryIndex（过滤语义在 kb/query.ts）→ 限量切片 → 分组信封。
//
// 形状变更（相对于 C7 单库版）：
//   - 新增可选 `kb`（库 id，见 spt_kb_list）；省略 = 检索全部已注册库；
//   - 新增可选 `limit`（每库返回条目上限，缺省 30）+ 每库 `truncated` 标记，
//     避免「无过滤查询」把整库灌进上下文；
//   - 返回由 data.matches 改为 data.results[]（逐库分组），每组带 kb/root/authority，
//     使模型的引用路径无歧义、冲突时能按 authority 取舍；
//   - 每库带 `stale` 提示（源文件比索引新 => 索引可能过期，只提示不阻断）。
//
// 沿用契约：目标库唯一且不可用时仍返回 kb_unavailable（不回退、不静默 0 匹配）。
// =============================================================================

import { z } from "zod";

import { parseIndexForQuery, queryIndex, type KbIndexEntry } from "../kb/index.js";
import {
  getKbById,
  getRegistryStatus,
  getStaleInfo,
  readIndexFile,
  searchKbContent,
  type RuntimeKb,
} from "../kb/registry.js";
import { errEnv, okEnv, SPT_ERROR_CODES, type Envelope } from "../types.js";

const DEFAULT_LIMIT = 30;
const MAX_LIMIT = 200;

export const KbQueryInput = z
  .object({
    kb: z
      .string()
      .optional()
      .describe("知识库 id（可用 id 见 spt_kb_list）；省略 = 检索全部已注册库"),
    topic: z
      .string()
      .optional()
      .describe("条目 topic 精确匹配（如 'config'、'recipe'、'installation'）"),
    domain: z.string().optional().describe("领域过滤：server / client / both（'both' 条目始终包含）"),
    version: z.string().optional().describe("版本过滤（如 '4.1'；条目 version 含 '通用' 时始终包含）"),
    keyword: z.string().optional().describe("对 title 与 keywords 做不区分大小写的子串匹配"),
    content: z
      .boolean()
      .optional()
      .describe(
        "keywords/title 是索引字段，盖不住概念词时的兜底：true = 强制在白名单目录内按正文检索并与索引结果合并；false = 只用索引；缺省 = 仅在索引 0 命中时自动按正文检索（结果带 contentHits 与 snippet）",
      ),
    limit: z
      .number()
      .int()
      .positive()
      .max(MAX_LIMIT)
      .optional()
      .describe(`每个库最多返回条目数（缺省 ${DEFAULT_LIMIT}，上限 ${MAX_LIMIT}）`),
  })
  .strict();

/** 命中条目：索引字段 + 正文命中证据（如来自正文检索） */
interface KbMatch extends KbIndexEntry {
  contentHits?: number;
  snippet?: string;
}

interface KbResultGroup {
  kb: string;
  kbTitle: string;
  authority: string;
  /** 库根：与 entry.path 拼出绝对路径 */
  root: string;
  matchCount: number;
  truncated: boolean;
  stale: boolean | null;
  /** 命中来源：index = 索引字段命中；content = 索引 0 命中后按正文找回；index+content = 两者合并 */
  matchedBy: "index" | "content" | "index+content";
  matches: KbMatch[];
  error?: string;
}

export function runKbQuery(args: unknown): Envelope {
  const parsed = KbQueryInput.safeParse(args);
  if (!parsed.success) {
    return errEnv("spt_kb_query", "无效输入", SPT_ERROR_CODES.INVALID_INPUT, {
      hint: parsed.error.message,
    });
  }
  const { kb: kbId, topic, domain, version, keyword } = parsed.data;
  const limit = parsed.data.limit ?? DEFAULT_LIMIT;
  const contentMode = parsed.data.content;

  const status = getRegistryStatus();

  // ---- 目标库选择 ----
  let targets: RuntimeKb[];
  if (typeof kbId === "string" && kbId.length > 0) {
    const kb = getKbById(kbId);
    if (kb === null) {
      return errEnv("spt_kb_query", `未知知识库：${kbId}`, SPT_ERROR_CODES.INVALID_INPUT, {
        hint: `已注册：${status.kbs.map((item) => item.id).join(", ") || "（无）"}；用 spt_kb_list 看各库用途`,
      });
    }
    targets = [kb];
  } else {
    targets = status.kbs;
  }

  if (targets.length === 0) {
    return errEnv("spt_kb_query", "没有可检索的知识库", SPT_ERROR_CODES.KB_UNAVAILABLE, {
      hint: `注册表：${status.path}${status.reason ? `（${status.reason}）` : ""}`,
    });
  }

  const filters = { kb: kbId, topic, domain, version, keyword, content: contentMode, limit };
  const groups: KbResultGroup[] = [];
  const hints: string[] = [];

  for (const target of targets) {
    const group: KbResultGroup = {
      kb: target.id,
      kbTitle: target.title,
      authority: target.authority,
      root: target.root.path,
      matchCount: 0,
      truncated: false,
      stale: null,
      matchedBy: "index",
      matches: [],
    };

    if (!target.index.ok) {
      // 目标库唯一时保持历史契约：响亮返回 kb_unavailable（不回退、不静默 0 匹配）
      if (targets.length === 1) {
        return errEnv("spt_kb_query", "知识库不可用", SPT_ERROR_CODES.KB_UNAVAILABLE, {
          hint: target.index.reason,
        });
      }
      group.error = target.index.reason;
      groups.push(group);
      continue;
    }

    const file = readIndexFile(target.index.path);
    if (!file.ok) {
      if (targets.length === 1) {
        return errEnv("spt_kb_query", "知识库不可用", SPT_ERROR_CODES.KB_UNAVAILABLE, {
          hint: file.reason,
        });
      }
      group.error = file.reason;
      groups.push(group);
      continue;
    }

    const parsedIndex = parseIndexForQuery(file.raw);
    if (!parsedIndex.ok) {
      if (targets.length === 1) {
        return errEnv(
          "spt_kb_query",
          `知识库索引结构非法：${parsedIndex.reason}`,
          SPT_ERROR_CODES.KB_UNAVAILABLE,
          { hint: `索引路径：${target.index.path}；请运行 node scripts/kb/build-index.mjs 校验` },
        );
      }
      group.error = `索引结构非法：${parsedIndex.reason}`;
      groups.push(group);
      continue;
    }

    const matched = queryIndex(parsedIndex.entries, { topic, domain, version, keyword });
    let matches: KbMatch[] = matched;

    // ---- 正文兜底：索引字段（title/keywords）盖不住概念词时，按白名单目录检索正文 ----
    const keywordTrimmed = typeof keyword === "string" ? keyword.trim() : "";
    const wantContent =
      keywordTrimmed !== "" &&
      contentMode !== false &&
      (contentMode === true || matched.length === 0);

    if (wantContent) {
      // 结构过滤（topic/domain/version）仍然生效：正文命中也要落在合法条目的路径上
      const structural = new Set(
        queryIndex(parsedIndex.entries, { topic, domain, version }).map((entry) => entry.path),
      );
      const entryByPath = new Map(parsedIndex.entries.map((entry) => [entry.path, entry]));
      const hits = searchKbContent(target.root.path, target.scan, keywordTrimmed).filter((hit) =>
        structural.has(hit.path),
      );

      if (hits.length > 0) {
        const merged: KbMatch[] = [...matches];
        for (const hit of hits) {
          const existing = merged.find((entry) => entry.path === hit.path);
          if (existing !== undefined) {
            existing.contentHits = hit.hits;
            existing.snippet = hit.snippet;
            continue;
          }
          const indexed = entryByPath.get(hit.path);
          const base: KbIndexEntry = indexed ?? {
            path: hit.path,
            title: hit.path.split("/").pop() ?? hit.path,
            version: ["通用"],
            domain: "both",
            topic: "uncategorized",
            source: hit.path.split("/")[0] ?? "unknown",
          };
          merged.push({ ...base, contentHits: hit.hits, snippet: hit.snippet });
        }
        matches = merged;
        group.matchedBy = matched.length > 0 ? "index+content" : "content";
      }
    }

    if (group.matchedBy === "content") {
      hints.push(
        `${target.id}：索引字段（title/keywords）0 命中，已按正文检索白名单目录（结果带 contentHits 与 snippet）`,
      );
    }

    group.matchCount = matches.length;
    group.matches = matches.slice(0, limit);
    group.truncated = matches.length > limit;

    const stale = getStaleInfo(target);
    group.stale = stale.stale;
    if (stale.stale === true) {
      hints.push(
        `${target.id} 索引可能过期（白名单目录内有源文件比索引新）；答案以源文件为准，可跑 node scripts/kb/build-index.mjs --kb ${target.id} --write 刷新`,
      );
    }
    groups.push(group);
  }

  if (status.source === "legacy") {
    hints.push(`注册表不可用，已退化为单库模式（${status.reason}）；注册表：${status.path}`);
  }

  const totalMatches = groups.reduce((sum, group) => sum + group.matchCount, 0);
  const returned = groups.reduce((sum, group) => sum + group.matches.length, 0);
  const truncated = groups.some((group) => group.truncated);

  const summary =
    groups.length === 1
      ? `知识库匹配 ${totalMatches} 条${truncated ? `（返回前 ${returned}）` : ""}`
      : `跨 ${groups.length} 个库匹配 ${totalMatches} 条${truncated ? `（返回前 ${returned}）` : ""}`;

  return okEnv("spt_kb_query", summary, {
    matchCount: totalMatches,
    returned,
    truncated,
    filters,
    registry: {
      path: status.path,
      source: status.source,
      ok: status.ok,
      ...(status.reason !== undefined ? { reason: status.reason } : {}),
    },
    ...(hints.length > 0 ? { hints } : {}),
    results: groups,
  });
}
