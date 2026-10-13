// =============================================================================
// kb-list.ts — spt_kb_list（知识库清单与自检）
//
// 作用：让 agent 用一个廉价的固定调用拿到「有哪些库 / 各管什么 / 怎么过滤 /
// 索引是否新鲜」，而不是把每个库的路径与用途写进提示词。
//
// 返回逐库：id / title（一句话用途）/ authority（它权威覆盖什么）/ root / envKey /
// entryCount / generated / topics（topic 键，按出现频次降序：让过滤参数可发现）/
// stale（源文件比索引新 => 索引可能过期）。加载失败只在 reason 里体现，不抛错。
// =============================================================================

import { z } from "zod";

import { parseIndexForQuery } from "../kb/index.js";
import { getRegistryStatus, getStaleInfo, readIndexFile } from "../kb/registry.js";
import { errEnv, okEnv, SPT_ERROR_CODES, type Envelope } from "../types.js";

const MAX_TOPICS = 40;

export const KbListInput = z.object({}).strict();

function topicKeysOf(entries: Array<{ topic: string }>): string[] {
  const counts = new Map<string, number>();
  for (const entry of entries) {
    counts.set(entry.topic, (counts.get(entry.topic) ?? 0) + 1);
  }
  return [...counts.entries()]
    .sort((a, b) => (b[1] - a[1]) || a[0].localeCompare(b[0]))
    .slice(0, MAX_TOPICS)
    .map(([topic]) => topic);
}

export function runKbList(args: unknown): Envelope {
  const parsed = KbListInput.safeParse(args ?? {});
  if (!parsed.success) {
    return errEnv("spt_kb_list", "无效输入", SPT_ERROR_CODES.INVALID_INPUT, {
      hint: parsed.error.message,
    });
  }

  const status = getRegistryStatus();

  const kbs = status.kbs.map((kb) => {
    const base = {
      id: kb.id,
      title: kb.title,
      authority: kb.authority,
      root: kb.root.path,
      rootOk: kb.root.ok,
      envKey: kb.envKey,
      index: kb.index.path,
      indexOk: kb.index.ok,
    };

    if (!kb.index.ok) {
      return {
        ...base,
        reason: kb.index.ok ? undefined : kb.index.reason,
        entryCount: 0,
        generated: null,
        topics: [],
        stale: null,
      };
    }

    const file = readIndexFile(kb.index.path);
    if (!file.ok) {
      return {
        ...base,
        indexOk: false,
        reason: file.reason,
        entryCount: 0,
        generated: null,
        topics: [],
        stale: null,
      };
    }

    const parsedIndex = parseIndexForQuery(file.raw);
    if (!parsedIndex.ok) {
      return {
        ...base,
        indexOk: false,
        reason: `索引结构非法：${parsedIndex.reason}`,
        entryCount: 0,
        generated: null,
        topics: [],
        stale: null,
      };
    }

    const rawGenerated =
      file.raw && typeof file.raw === "object" && !Array.isArray(file.raw)
        ? (file.raw as Record<string, unknown>).generated
        : undefined;

    return {
      ...base,
      entryCount: parsedIndex.entries.length,
      generated: typeof rawGenerated === "string" ? rawGenerated : null,
      topics: topicKeysOf(parsedIndex.entries),
      stale: getStaleInfo(kb).stale,
    };
  });

  const usable = kbs.filter((kb) => kb.indexOk).length;
  const staleCount = kbs.filter((kb) => kb.stale === true).length;
  const summary =
    `已注册 ${status.kbs.length} 个知识库（可用 ${usable}）` +
    (staleCount > 0 ? `，${staleCount} 个索引可能过期` : "");

  return okEnv("spt_kb_list", summary, {
    registry: {
      path: status.path,
      source: status.source,
      ok: status.ok,
      ...(status.reason !== undefined ? { reason: status.reason } : {}),
    },
    usage: {
      query: "spt_kb_query({ kb?, topic?, domain?, version?, keyword?, limit? })；kb 省略 = 跨库检索",
      read: "命中后取该库的 root + entry.path 拼绝对路径读取源文件",
      stale: "stale=true 表示白名单目录内有源文件比索引新：以源文件为准，必要时重跑 scripts/kb/build-index.mjs",
    },
    kbs,
  });
}
