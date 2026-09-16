// =============================================================================
// kb-contract.test.ts — KB 索引契约（C7）
//
// 覆盖：validateIndex 严格规则（含 version string → 报错）、parseIndexForQuery
// 结构校验 + version 防御归一、collectStats 统计。
// =============================================================================

import { describe, expect, it } from "vitest";

import {
  KB_SCHEMA_VERSION,
  collectStats,
  parseIndexForQuery,
  queryIndex,
  validateIndex,
  type KbIndexEntry,
} from "../../src/kb/index.js";

/** 构造一个合法 v2 条目 */
function entry(overrides: Partial<KbIndexEntry> = {}): KbIndexEntry {
  return {
    path: "curated/recipes/demo.md",
    title: "Demo",
    version: ["4.1"],
    domain: "server",
    topic: "recipe",
    source: "curated",
    ...overrides,
  };
}

/** 构造合法 v2 索引根 */
function index(entries: KbIndexEntry[], schemaVersion: unknown = KB_SCHEMA_VERSION) {
  return { generated: "2026-09-14", schema_version: schemaVersion, entries };
}

describe("KB_SCHEMA_VERSION", () => {
  it("为 2（v1 → v2 断代）", () => {
    expect(KB_SCHEMA_VERSION).toBe(2);
  });
});

describe("validateIndex（严格）", () => {
  it("合法 v2 索引 -> ok + stats", () => {
    const res = validateIndex(index([entry(), entry({ path: "wiki/a.md", source: "wiki" })]));
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    expect(res.stats.total).toBe(2);
    expect(res.stats.bySource).toEqual({ curated: 1, wiki: 1 });
  });

  it("schema_version 1 -> 报错（断代不兼容）", () => {
    const res = validateIndex(index([entry()], 1));
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.errors.join("\n")).toContain("schema_version 必须为 2");
  });

  it("version 为 string -> 报错（strict，不归一）", () => {
    const bad = { ...entry(), version: "4.1" };
    const res = validateIndex(index([bad as unknown as KbIndexEntry]));
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.errors.join("\n")).toContain("version 必须是 string[]");
  });

  it("path 重复 -> 报错", () => {
    const res = validateIndex(index([entry(), entry()]));
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.errors.join("\n")).toContain("path 重复");
  });

  it("path 空 -> 报错", () => {
    const res = validateIndex(index([entry({ path: "" })]));
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.errors.join("\n")).toContain("path 必须是非空字符串");
  });

  it("title / topic / source 空 -> 分别报错", () => {
    const res = validateIndex(
      index([entry({ title: "", topic: "", source: "" })]),
    );
    expect(res.ok).toBe(false);
    if (res.ok) return;
    const joined = res.errors.join("\n");
    expect(joined).toContain("title 必须是非空字符串");
    expect(joined).toContain("topic 必须是非空字符串");
    expect(joined).toContain("source 必须是非空字符串");
  });

  it("domain 非法值 -> 报错", () => {
    const bad = { ...entry(), domain: "banana" };
    const res = validateIndex(index([bad as unknown as KbIndexEntry]));
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.errors.join("\n")).toContain("domain 必须是 server/client/both");
  });

  it("keywords 非 string[] -> 报错", () => {
    const bad = { ...entry(), keywords: "not-an-array" };
    const res = validateIndex(index([bad as unknown as KbIndexEntry]));
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.errors.join("\n")).toContain("keywords 必须是 string[]");
  });

  it("summary 非字符串 -> 报错", () => {
    const bad = { ...entry(), summary: 42 };
    const res = validateIndex(index([bad as unknown as KbIndexEntry]));
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.errors.join("\n")).toContain("summary 必须是字符串");
  });

  it("entries 不是数组 -> 报错", () => {
    const res = validateIndex({ schema_version: 2, entries: {} });
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.errors.join("\n")).toContain("entries 必须是数组");
  });

  it("根不是对象 -> 报错", () => {
    expect(validateIndex(null).ok).toBe(false);
    expect(validateIndex([]).ok).toBe(false);
  });

  it("一次收集全部错误（不 fail-fast）", () => {
    const res = validateIndex(index([entry({ path: "", source: "" })], 1));
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.errors.length).toBeGreaterThanOrEqual(3);
  });
});

describe("parseIndexForQuery（结构校验 + version 归一）", () => {
  it("合法索引 -> entries（不要求 schema_version）", () => {
    const res = parseIndexForQuery({ entries: [entry()] });
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    expect(res.entries).toHaveLength(1);
    expect(res.entries[0].version).toEqual(["4.1"]);
  });

  it("version 为 string -> 归一为 [string]（D4 防御归一）", () => {
    const res = parseIndexForQuery({
      entries: [{ ...entry(), version: "4.1" }],
    });
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    expect(res.entries[0].version).toEqual(["4.1"]);
  });

  it("归一后的条目可被 version 过滤命中（历史崩溃点回归）", () => {
    const res = parseIndexForQuery({
      entries: [
        { ...entry(), path: "curated/migration/legacy.md", version: "4.1" },
        { ...entry(), path: "curated/recipes/other.md", version: ["5.0"] },
      ],
    });
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const hits = queryIndex(res.entries, { version: "4.1" });
    expect(hits.map((h) => h.path)).toEqual(["curated/migration/legacy.md"]);
  });

  it("path 缺失 -> 响亮 reason", () => {
    const res = parseIndexForQuery({ entries: [{ ...entry(), path: undefined }] });
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.reason).toContain("path");
  });

  it("domain 非法 -> 响亮 reason", () => {
    const res = parseIndexForQuery({ entries: [{ ...entry(), domain: "banana" }] });
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.reason).toContain("domain");
  });

  it("version 既非 string 也非 string[] -> 响亮 reason", () => {
    const res = parseIndexForQuery({ entries: [{ ...entry(), version: 41 }] });
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.reason).toContain("version");
  });

  it("entries 不是数组 / 根非对象 -> 响亮 reason", () => {
    expect(parseIndexForQuery({ entries: null }).ok).toBe(false);
    expect(parseIndexForQuery(null).ok).toBe(false);
    expect(parseIndexForQuery("nope").ok).toBe(false);
  });

  it("keywords / summary 透传", () => {
    const res = parseIndexForQuery({
      entries: [{ ...entry(), keywords: ["a", "b"], summary: "s" }],
    });
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    expect(res.entries[0].keywords).toEqual(["a", "b"]);
    expect(res.entries[0].summary).toBe("s");
  });
});

describe("collectStats", () => {
  it("total / bySource / byDomain / byTopic / byVersion / withKeywords / withSummary", () => {
    const stats = collectStats([
      entry(),
      entry({ path: "wiki/a.md", source: "wiki", domain: "both", version: ["通用"] }),
      entry({
        path: "wiki/b.md",
        source: "wiki",
        domain: "client",
        version: ["4.1", "5.0"],
        keywords: ["k"],
        summary: "s",
      }),
    ]);
    expect(stats.total).toBe(3);
    expect(stats.bySource).toEqual({ curated: 1, wiki: 2 });
    expect(stats.byDomain).toEqual({ both: 1, client: 1, server: 1 });
    expect(stats.byTopic).toEqual({ recipe: 3 });
    expect(stats.byVersion).toEqual({ "4.1": 2, "5.0": 1, "通用": 1 });
    expect(stats.withKeywords).toBe(1);
    expect(stats.withSummary).toBe(1);
  });

  it("空 keywords 数组不计入 withKeywords", () => {
    const stats = collectStats([entry({ keywords: [], summary: "" })]);
    expect(stats.withKeywords).toBe(0);
    expect(stats.withSummary).toBe(0);
  });
});
