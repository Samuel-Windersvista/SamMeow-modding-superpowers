// =============================================================================
// kb/contract.ts — SPT 知识库索引契约（C7）
//
// 单一契约实现：索引形状、严格校验（validateIndex）、查询期结构校验 + version
// 防御归一（parseIndexForQuery）、统计（collectStats）。
//
// 断代：KB_SCHEMA_VERSION = 2。v1 → v2 的差异（数据已迁移）：
//   - version 统一为 string[]（v1 允许 string，查询侧曾因此崩溃）
//   - source 必填非空（v1 有 3 条空值）
//
// 为什么拆出契约模块：
//   - 索引形状此前只存在于「生成器不存在、靠手改」的约定里，没有代码强制；
//   - 运行时唯一的消费者 spt_kb_query 直接 `entry.version.map(...)`，一条 string
//     瑕疵就让 version 过滤 100% 不可用（D4：结构校验响亮失败 + version 防御归一）。
//   - 契约现在可被 vitest 覆盖、被 scripts/spt-kb/validate-index.mjs 复用（经 dist）。
// =============================================================================

/** 索引 schema 版本（v2：version 统一 string[]、source 必填非空） */
export const KB_SCHEMA_VERSION = 2;

/** 条目领域（both = 跨 server/client 通用） */
export type KbDomain = "server" | "client" | "both";

/** 知识库索引条目（v2 单形态） */
export interface KbIndexEntry {
  /** 相对 knowledge/spt-kb/ 的路径（正斜杠） */
  path: string;
  title: string;
  /** 版本标签数组，如 ["4.1"] / ["通用"] */
  version: string[];
  domain: KbDomain;
  topic: string;
  /** 来源分区：curated / wiki / archive */
  source: string;
  keywords?: string[];
  summary?: string;
}

export interface KbIndexStats {
  total: number;
  bySource: Record<string, number>;
  byDomain: Record<string, number>;
  byTopic: Record<string, number>;
  byVersion: Record<string, number>;
  withKeywords: number;
  withSummary: number;
}

const KB_DOMAINS: readonly KbDomain[] = ["server", "client", "both"];

function isDomain(value: unknown): value is KbDomain {
  return typeof value === "string" && (KB_DOMAINS as readonly string[]).includes(value);
}

/** 键名升序排列（统计输出确定性，便于 diff/快照） */
function sortKeys(counts: Record<string, number>): Record<string, number> {
  const sorted: Record<string, number> = {};
  for (const key of Object.keys(counts).sort((a, b) => a.localeCompare(b))) {
    sorted[key] = counts[key];
  }
  return sorted;
}

/** 统计条目分布（total / bySource / byDomain / byTopic / byVersion / withKeywords / withSummary） */
export function collectStats(entries: KbIndexEntry[]): KbIndexStats {
  const bySource: Record<string, number> = {};
  const byDomain: Record<string, number> = {};
  const byTopic: Record<string, number> = {};
  const byVersion: Record<string, number> = {};
  let withKeywords = 0;
  let withSummary = 0;

  for (const entry of entries) {
    bySource[entry.source] = (bySource[entry.source] ?? 0) + 1;
    byDomain[entry.domain] = (byDomain[entry.domain] ?? 0) + 1;
    byTopic[entry.topic] = (byTopic[entry.topic] ?? 0) + 1;
    for (const version of entry.version) {
      byVersion[version] = (byVersion[version] ?? 0) + 1;
    }
    if (entry.keywords !== undefined && entry.keywords.length > 0) withKeywords += 1;
    if (entry.summary !== undefined && entry.summary.length > 0) withSummary += 1;
  }

  return {
    total: entries.length,
    bySource: sortKeys(bySource),
    byDomain: sortKeys(byDomain),
    byTopic: sortKeys(byTopic),
    byVersion: sortKeys(byVersion),
    withKeywords,
    withSummary,
  };
}

/**
 * 严格校验索引（生成器/CI 用）。
 *
 * 规则：schema_version === KB_SCHEMA_VERSION；entries 为数组；path 非空且唯一；
 * title/topic/source 非空；version 为 string[]（strict，**不归一**）；
 * domain ∈ {server, client, both}；keywords?: string[]；summary?: string。
 *
 * 不 fail-fast：收集全部错误后一次性返回，便于迁移/生成时一次看全。
 */
export function validateIndex(
  raw: unknown,
): { ok: true; stats: KbIndexStats } | { ok: false; errors: string[] } {
  const errors: string[] = [];

  if (!raw || typeof raw !== "object" || Array.isArray(raw)) {
    return { ok: false, errors: ["索引根必须是 JSON 对象"] };
  }
  const root = raw as Record<string, unknown>;

  if (root.schema_version !== KB_SCHEMA_VERSION) {
    errors.push(
      `schema_version 必须为 ${KB_SCHEMA_VERSION}（实际：${JSON.stringify(root.schema_version)}）`,
    );
  }

  const rawEntries = root.entries;
  if (!Array.isArray(rawEntries)) {
    errors.push("entries 必须是数组");
    return { ok: false, errors };
  }

  const entries: KbIndexEntry[] = [];
  const seenPaths = new Set<string>();

  for (let i = 0; i < rawEntries.length; i++) {
    const before = errors.length;
    const rawEntry = rawEntries[i];
    if (!rawEntry || typeof rawEntry !== "object" || Array.isArray(rawEntry)) {
      errors.push(`entries[${i}] 必须是对象`);
      continue;
    }
    const entry = rawEntry as Record<string, unknown>;

    if (typeof entry.path !== "string" || entry.path.length === 0) {
      errors.push(`entries[${i}].path 必须是非空字符串`);
    } else if (seenPaths.has(entry.path)) {
      errors.push(`entries[${i}].path 重复：${entry.path}`);
    } else {
      seenPaths.add(entry.path);
    }

    if (typeof entry.title !== "string" || entry.title.length === 0) {
      errors.push(`entries[${i}].title 必须是非空字符串`);
    }
    if (typeof entry.topic !== "string" || entry.topic.length === 0) {
      errors.push(`entries[${i}].topic 必须是非空字符串`);
    }
    if (typeof entry.source !== "string" || entry.source.length === 0) {
      errors.push(`entries[${i}].source 必须是非空字符串`);
    }
    if (
      !Array.isArray(entry.version) ||
      entry.version.some((v) => typeof v !== "string")
    ) {
      errors.push(`entries[${i}].version 必须是 string[]（strict，不归一）`);
    }
    if (!isDomain(entry.domain)) {
      errors.push(`entries[${i}].domain 必须是 server/client/both 之一`);
    }
    if (
      entry.keywords !== undefined &&
      (!Array.isArray(entry.keywords) ||
        entry.keywords.some((k) => typeof k !== "string"))
    ) {
      errors.push(`entries[${i}].keywords 必须是 string[]`);
    }
    if (entry.summary !== undefined && typeof entry.summary !== "string") {
      errors.push(`entries[${i}].summary 必须是字符串`);
    }

    if (errors.length === before) {
      entries.push({
        path: entry.path as string,
        title: entry.title as string,
        version: entry.version as string[],
        domain: entry.domain as KbDomain,
        topic: entry.topic as string,
        source: entry.source as string,
        ...(entry.keywords !== undefined
          ? { keywords: entry.keywords as string[] }
          : {}),
        ...(entry.summary !== undefined ? { summary: entry.summary as string } : {}),
      });
    }
  }

  if (errors.length > 0) return { ok: false, errors };
  return { ok: true, stats: collectStats(entries) };
}

/**
 * 查询期解析：结构校验（entries 数组、条目 path/title/topic/domain/source 类型、
 * domain 值域）失败即返回响亮 reason；**唯一归一**是单条 version 为 string 时
 * 提升为 [string]（D4 防御归一，修复历史 string 瑕疵导致的 version 过滤崩溃）。
 *
 * 刻意不校验 schema_version：查询不该因版本号不符而全盘拒绝（那是 validateIndex
 * 的职责）；也不校验 path 是否真实存在（KB 归档可能未随包分发）。
 *
 * 与 validateIndex 的边界（可选字段）：非法或缺失的 `keywords` / `summary`
 * 会被**静默省略**（不进入返回条目），而不是让整场查询失败——它们不影响过滤
 * 语义，属「一次数据瑕疵不封死查询」的防御面。严格校验（类型不符即报错）是
 * validateIndex 的职责，由生成器 / CI 侧承担。
 */
export function parseIndexForQuery(
  raw: unknown,
): { ok: true; entries: KbIndexEntry[] } | { ok: false; reason: string } {
  if (!raw || typeof raw !== "object" || Array.isArray(raw)) {
    return { ok: false, reason: "索引根不是 JSON 对象" };
  }
  const rawEntries = (raw as Record<string, unknown>).entries;
  if (!Array.isArray(rawEntries)) {
    return { ok: false, reason: "entries 不是数组" };
  }

  const entries: KbIndexEntry[] = [];
  for (let i = 0; i < rawEntries.length; i++) {
    const rawEntry = rawEntries[i];
    if (!rawEntry || typeof rawEntry !== "object" || Array.isArray(rawEntry)) {
      return { ok: false, reason: `entries[${i}] 不是对象` };
    }
    const entry = rawEntry as Record<string, unknown>;

    if (typeof entry.path !== "string" || entry.path.length === 0) {
      return { ok: false, reason: `entries[${i}].path 缺失或非字符串` };
    }
    for (const key of ["title", "topic", "source"] as const) {
      if (typeof entry[key] !== "string") {
        return { ok: false, reason: `entries[${i}].${key} 缺失或非字符串` };
      }
    }
    if (!isDomain(entry.domain)) {
      return { ok: false, reason: `entries[${i}].domain 必须是 server/client/both 之一` };
    }

    let version: string[];
    if (typeof entry.version === "string") {
      // 防御归一：v1 遗留的 string 形态（历史崩溃点）
      version = [entry.version];
    } else if (
      Array.isArray(entry.version) &&
      entry.version.every((v) => typeof v === "string")
    ) {
      version = entry.version as string[];
    } else {
      return { ok: false, reason: `entries[${i}].version 既不是 string 也不是 string[]` };
    }

    entries.push({
      path: entry.path,
      title: entry.title as string,
      version,
      domain: entry.domain,
      topic: entry.topic as string,
      source: entry.source as string,
      ...(Array.isArray(entry.keywords) &&
      entry.keywords.every((k) => typeof k === "string")
        ? { keywords: entry.keywords as string[] }
        : {}),
      ...(typeof entry.summary === "string" ? { summary: entry.summary } : {}),
    });
  }

  return { ok: true, entries };
}
