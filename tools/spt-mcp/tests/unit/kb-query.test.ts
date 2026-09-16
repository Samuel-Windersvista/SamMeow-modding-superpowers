// =============================================================================
// kb-query.test.ts — KB / Forge 工具的布局降级行为
//
// 经 resetLayoutForTest() + process.env 操纵 spt-mcp 侧布局缓存，
// 验证资源不可用时返回结构化错误（kb_unavailable）而非静默 0 匹配。
// =============================================================================

import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";

import { runKbQuery } from "../../src/tools/kb-query.js";
import { runForgeSearch } from "../../src/tools/forge-search.js";
import { resetLayoutForTest } from "../../src/runtime-layout.js";

const ORIGINAL_KB_ROOT = process.env.SPT_KB_ROOT;

let tmp: string;

/** 写文件（自动建父目录） */
function writeFile(fullPath: string, content: string): void {
  mkdirSync(dirname(fullPath), { recursive: true });
  writeFileSync(fullPath, content);
}

beforeEach(() => {
  tmp = mkdtempSync(join(tmpdir(), "spt-mcp-kbquery-"));
});

afterEach(() => {
  if (ORIGINAL_KB_ROOT === undefined) {
    delete process.env.SPT_KB_ROOT;
  } else {
    process.env.SPT_KB_ROOT = ORIGINAL_KB_ROOT;
  }
  resetLayoutForTest();
  rmSync(tmp, { recursive: true, force: true });
});

// NIT-2：锁死已声明 wire delta 1（错误信封字段 summary → message）。
// canonical err 的 message 必须存在，且不得再携带 summary。
describe("spt_kb_query: 错误信封字段（delta 1）", () => {
  it("invalid input -> message 存在、summary 为 undefined", () => {
    // KbQueryInput 为 .strict()：未知键触发 invalid_input
    const res = runKbQuery({ bogus: true });
    expect(res.ok).toBe(false);
    if (res.ok) return;

    expect(res.tool).toBe("spt_kb_query");
    expect(res.code).toBe("invalid_input");
    expect(typeof res.message).toBe("string");
    expect(res.message.length).toBeGreaterThan(0);
    // summary 字段已从错误路径移除（wire delta 1）
    expect((res as Record<string, unknown>).summary).toBeUndefined();
  });
});

describe("spt_kb_query: 布局降级", () => {
  it("SPT_KB_ROOT 指向不存在路径 -> errEnv + code kb_unavailable", () => {
    const bogus = join(tmp, "no-such-kb");
    process.env.SPT_KB_ROOT = bogus;
    resetLayoutForTest();

    const res = runKbQuery({});
    expect(res.ok).toBe(false);
    if (res.ok) return;

    expect(res.tool).toBe("spt_kb_query");
    expect(res.code).toBe("kb_unavailable");
    expect(res.hint).toContain(bogus);
  });

  it("SPT_KB_ROOT 指向含 index.json 的临时 KB -> ok（filters 回显）", () => {
    const kbRoot = join(tmp, "kb");
    writeFile(
      join(kbRoot, "index.json"),
      JSON.stringify({
        entries: [
          {
            path: "curated/recipes/demo.md",
            title: "Demo Recipe",
            version: ["4.1"],
            domain: "both",
            topic: "recipe",
            source: "curated",
          },
        ],
      }),
    );
    process.env.SPT_KB_ROOT = kbRoot;
    resetLayoutForTest();

    const res = runKbQuery({});
    expect(res.ok).toBe(true);
    if (!res.ok) return;

    expect(res.tool).toBe("spt_kb_query");
    const data = res.data as {
      matchCount: number;
      filters: Record<string, unknown>;
    };
    expect(data.matchCount).toBe(1);
    expect(data.filters).toEqual({
      topic: undefined,
      domain: undefined,
      version: undefined,
      keyword: undefined,
    });
  });
});

describe("spt_forge_search: 布局降级（同类）", () => {
  it("归档缺失 -> errEnv + code kb_unavailable", () => {
    // 有效 KB 根但无 archive/forge 目录（便携包形态）
    const kbRoot = join(tmp, "kb-no-archive");
    writeFile(join(kbRoot, "index.json"), JSON.stringify({ entries: [] }));
    process.env.SPT_KB_ROOT = kbRoot;
    resetLayoutForTest();

    const res = runForgeSearch({});
    expect(res.ok).toBe(false);
    if (res.ok) return;

    expect(res.tool).toBe("spt_forge_search");
    expect(res.code).toBe("kb_unavailable");
    expect(res.hint).toContain("forge");
  });
});
