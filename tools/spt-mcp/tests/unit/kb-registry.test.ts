// =============================================================================
// kb-registry.test.ts — 注册表驱动的多库检索
//
// 覆盖：跨库查询分组 / kb 过滤 / 未知 kb / limit+truncated / keyword 命中 keywords /
//       索引过期检测 / spt_kb_list 清单 / 注册表缺失时的单库退化。
// =============================================================================

import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { mkdirSync, mkdtempSync, rmSync, utimesSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";

import { resetRegistryForTest } from "../../src/kb/registry.js";
import { runKbList } from "../../src/tools/kb-list.js";
import { runKbQuery } from "../../src/tools/kb-query.js";
import { resetLayoutForTest } from "../../src/runtime-layout.js";

const ORIGINAL_KB_ROOT = process.env.SPT_KB_ROOT;
const ORIGINAL_REGISTRY = process.env.KB_REGISTRY;

let tmp: string;

function writeFile(fullPath: string, content: string): void {
  mkdirSync(dirname(fullPath), { recursive: true });
  writeFileSync(fullPath, content);
}

function entry(path: string, title: string, extra: Record<string, unknown> = {}) {
  return {
    path,
    title,
    version: ["4.1"],
    domain: "both",
    topic: "recipe",
    source: "curated",
    ...extra,
  };
}

/** 建一个临时库：index.json + 可选源文件目录 */
function makeKb(dir: string, entries: unknown[], sourceFile?: { rel: string; text: string }) {
  writeFile(join(dir, "index.json"), JSON.stringify({ schema_version: 2, generated: "2026-01-01", entries }));
  if (sourceFile) writeFile(join(dir, sourceFile.rel), sourceFile.text);
}

function writeRegistry(kbs: unknown[]): string {
  const path = join(tmp, "kb-registry.json");
  writeFile(path, JSON.stringify({ schema_version: 1, kbs }));
  process.env.KB_REGISTRY = path;
  resetRegistryForTest();
  return path;
}

beforeEach(() => {
  tmp = mkdtempSync(join(tmpdir(), "spt-mcp-kbregistry-"));
  delete process.env.SPT_KB_ROOT;
  resetLayoutForTest();
});

afterEach(() => {
  if (ORIGINAL_KB_ROOT === undefined) delete process.env.SPT_KB_ROOT;
  else process.env.SPT_KB_ROOT = ORIGINAL_KB_ROOT;
  if (ORIGINAL_REGISTRY === undefined) delete process.env.KB_REGISTRY;
  else process.env.KB_REGISTRY = ORIGINAL_REGISTRY;
  resetLayoutForTest();
  resetRegistryForTest();
  rmSync(tmp, { recursive: true, force: true });
});

describe("spt_kb_query: 注册表驱动的多库检索", () => {
  it("无 kb 参数 -> 逐库分组，各组带 root/authority，matchCount 合计", () => {
    const kbA = join(tmp, "kbA");
    const kbB = join(tmp, "kbB");
    makeKb(kbA, [entry("curated/a.md", "A 配方")]);
    makeKb(kbB, [entry("notes/b.md", "B 玩法"), entry("notes/c.md", "C 玩法")]);
    writeRegistry([
      { id: "kb-a", title: "A 库", authority: "A 权威", root: kbA, index: "./index.json", enabled: true },
      { id: "kb-b", title: "B 库", authority: "B 权威", root: kbB, index: "./index.json", enabled: true },
    ]);

    const res = runKbQuery({});
    expect(res.ok).toBe(true);
    if (!res.ok) return;

    const data = res.data as {
      matchCount: number;
      results: Array<{ kb: string; root: string; authority: string; matchCount: number }>;
    };
    expect(data.matchCount).toBe(3);
    expect(data.results.map((group) => group.kb)).toEqual(["kb-a", "kb-b"]);
    expect(data.results[0].root).toBe(kbA);
    expect(data.results[1].authority).toBe("B 权威");
  });

  it("kb 过滤 -> 只返回该库", () => {
    const kbA = join(tmp, "kbA");
    const kbB = join(tmp, "kbB");
    makeKb(kbA, [entry("curated/a.md", "A 配方")]);
    makeKb(kbB, [entry("notes/b.md", "B 玩法")]);
    writeRegistry([
      { id: "kb-a", title: "A 库", authority: "A", root: kbA, index: "./index.json", enabled: true },
      { id: "kb-b", title: "B 库", authority: "B", root: kbB, index: "./index.json", enabled: true },
    ]);

    const res = runKbQuery({ kb: "kb-b" });
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as { results: Array<{ kb: string; matchCount: number }> };
    expect(data.results).toHaveLength(1);
    expect(data.results[0].kb).toBe("kb-b");
    expect(data.results[0].matchCount).toBe(1);
  });

  it("未知 kb -> invalid_input，hint 列出已注册 id", () => {
    const kbA = join(tmp, "kbA");
    makeKb(kbA, [entry("curated/a.md", "A 配方")]);
    writeRegistry([
      { id: "kb-a", title: "A 库", authority: "A", root: kbA, index: "./index.json", enabled: true },
    ]);

    const res = runKbQuery({ kb: "nope" });
    expect(res.ok).toBe(false);
    if (res.ok) return;
    expect(res.code).toBe("invalid_input");
    expect(res.hint).toContain("kb-a");
  });

  it("limit -> 截断并在 truncated 上可见", () => {
    const kbA = join(tmp, "kbA");
    makeKb(kbA, [
      entry("curated/1.md", "一"),
      entry("curated/2.md", "二"),
      entry("curated/3.md", "三"),
    ]);
    writeRegistry([
      { id: "kb-a", title: "A 库", authority: "A", root: kbA, index: "./index.json", enabled: true },
    ]);

    const res = runKbQuery({ limit: 2 });
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as {
      matchCount: number;
      returned: number;
      truncated: boolean;
      results: Array<{ truncated: boolean; matches: unknown[] }>;
    };
    expect(data.matchCount).toBe(3);
    expect(data.returned).toBe(2);
    expect(data.truncated).toBe(true);
    expect(data.results[0].matches).toHaveLength(2);
  });

  it("keyword 命中 keywords（title 不含关键词）", () => {
    const kbA = join(tmp, "kbA");
    makeKb(kbA, [
      entry("curated/1.md", "无关标题", { keywords: ["以物易物", "经济"] }),
      entry("curated/2.md", "别的条目"),
    ]);
    writeRegistry([
      { id: "kb-a", title: "A 库", authority: "A", root: kbA, index: "./index.json", enabled: true },
    ]);

    const res = runKbQuery({ keyword: "以物易物" });
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as { results: Array<{ matches: Array<{ path: string }> }> };
    expect(data.results[0].matches.map((match) => match.path)).toEqual(["curated/1.md"]);
  });

  it("索引过期：源文件比索引新 -> stale=true 并给刷新提示", () => {
    const kbA = join(tmp, "kbA");
    makeKb(kbA, [entry("docs/a.md", "A")], { rel: "docs/a.md", text: "# A\n" });
    // index 旧一些，源文件新一些
    const now = Date.now() / 1000;
    utimesSync(join(kbA, "index.json"), now - 600, now - 600);
    utimesSync(join(kbA, "docs/a.md"), now - 10, now - 10);
    writeRegistry([
      {
        id: "kb-a",
        title: "A 库",
        authority: "A",
        root: kbA,
        index: "./index.json",
        enabled: true,
        scan: { dirs: ["docs"], exclude: [] },
      },
    ]);

    const res = runKbQuery({});
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as {
      results: Array<{ stale: boolean | null }>;
      hints?: string[];
    };
    expect(data.results[0].stale).toBe(true);
    expect(data.hints?.join(" ")).toContain("索引可能过期");
  });

  it("索引比源文件新 -> stale=false", () => {
    const kbA = join(tmp, "kbA");
    makeKb(kbA, [entry("docs/a.md", "A")], { rel: "docs/a.md", text: "# A\n" });
    const now = Date.now() / 1000;
    utimesSync(join(kbA, "docs/a.md"), now - 600, now - 600);
    utimesSync(join(kbA, "index.json"), now - 10, now - 10);
    writeRegistry([
      {
        id: "kb-a",
        title: "A 库",
        authority: "A",
        root: kbA,
        index: "./index.json",
        enabled: true,
        scan: { dirs: ["docs"], exclude: [] },
      },
    ]);

    const res = runKbQuery({});
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as { results: Array<{ stale: boolean | null }> };
    expect(data.results[0].stale).toBe(false);
  });
});

describe("spt_kb_query: 正文兜底（索引字段盖不住概念词时）", () => {
  it("索引 0 命中 -> 自动按正文找回，带 matchedBy=content / contentHits / snippet", () => {
    const kbA = join(tmp, "kbA");
    makeKb(kbA, [entry("docs/a.md", "无关标题")], {
      rel: "docs/a.md",
      text: "# 无关\n\n支持以物易物：以物易物 只收物，以物易物 不收现金。\n",
    });
    writeRegistry([
      {
        id: "kb-a",
        title: "A 库",
        authority: "A",
        root: kbA,
        index: "./index.json",
        enabled: true,
        scan: { dirs: ["docs"], exclude: [] },
      },
    ]);

    const res = runKbQuery({ keyword: "以物易物" });
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as {
      results: Array<{
        matchedBy: string;
        matches: Array<{ path: string; contentHits?: number; snippet?: string }>;
      }>;
      hints?: string[];
    };
    expect(data.results[0].matchedBy).toBe("content");
    expect(data.results[0].matches[0].path).toBe("docs/a.md");
    expect(data.results[0].matches[0].contentHits).toBe(3);
    expect(data.results[0].matches[0].snippet).toContain("以物易物");
    expect(data.hints?.join(" ")).toContain("正文检索");
  });

  it("content:false -> 不兜底（保持纯索引语义）", () => {
    const kbA = join(tmp, "kbA");
    makeKb(kbA, [entry("docs/a.md", "无关标题")], {
      rel: "docs/a.md",
      text: "# 无关\n\n支持以物易物。\n",
    });
    writeRegistry([
      {
        id: "kb-a",
        title: "A 库",
        authority: "A",
        root: kbA,
        index: "./index.json",
        enabled: true,
        scan: { dirs: ["docs"], exclude: [] },
      },
    ]);

    const res = runKbQuery({ keyword: "以物易物", content: false });
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as { results: Array<{ matchCount: number; matchedBy: string }> };
    expect(data.results[0].matchCount).toBe(0);
    expect(data.results[0].matchedBy).toBe("index");
  });
});

describe("spt_kb_list: 清单与自检", () => {
  it("列出各库用途/权威/条目数/topics/envKey", () => {
    const kbA = join(tmp, "kbA");
    const kbB = join(tmp, "kbB");
    makeKb(kbA, [entry("curated/a.md", "A", { topic: "config" })]);
    makeKb(kbB, [
      entry("notes/b.md", "B", { topic: "gameplay" }),
      entry("notes/c.md", "C", { topic: "gameplay" }),
    ]);
    writeRegistry([
      {
        id: "kb-a",
        title: "A 库",
        authority: "A 权威",
        root: kbA,
        index: "./index.json",
        envKey: "KB_A_ROOT",
        enabled: true,
      },
      { id: "kb-b", title: "B 库", authority: "B 权威", root: kbB, index: "./index.json", enabled: true },
    ]);

    const res = runKbList({});
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as {
      registry: { source: string; ok: boolean };
      kbs: Array<{ id: string; entryCount: number; topics: string[]; envKey: string | null }>;
    };
    expect(data.registry.source).toBe("registry");
    expect(data.kbs.map((kb) => kb.id)).toEqual(["kb-a", "kb-b"]);
    expect(data.kbs[0].entryCount).toBe(1);
    expect(data.kbs[0].envKey).toBe("KB_A_ROOT");
    expect(data.kbs[1].topics).toEqual(["gameplay"]);
  });

  it("库根缺失 -> 该库 indexOk=false 且带 reason，不抛错", () => {
    const missing = join(tmp, "nope");
    writeRegistry([
      { id: "kb-x", title: "X 库", authority: "X", root: missing, index: "./index.json", enabled: true },
    ]);

    const res = runKbList({});
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as {
      kbs: Array<{ id: string; rootOk: boolean; indexOk: boolean; reason?: string }>;
    };
    expect(data.kbs[0].rootOk).toBe(false);
    expect(data.kbs[0].indexOk).toBe(false);
    expect(typeof data.kbs[0].reason).toBe("string");
  });

  it("注册表缺失 -> 退化为单库模式（source=legacy）且不报错", () => {
    process.env.KB_REGISTRY = join(tmp, "no-such-registry.json");
    resetRegistryForTest();

    const res = runKbList({});
    expect(res.ok).toBe(true);
    if (!res.ok) return;
    const data = res.data as {
      registry: { source: string; ok: boolean; reason?: string };
      kbs: Array<{ id: string; source: string }>;
    };
    expect(data.registry.source).toBe("legacy");
    expect(data.registry.ok).toBe(false);
    expect(data.kbs).toHaveLength(1);
    expect(data.kbs[0].id).toBe("spt-kb");
  });
});
