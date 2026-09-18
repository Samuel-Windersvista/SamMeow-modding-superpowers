import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { existsSync, mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import {
  bestSptMap,
  loadCatalog,
  loadHotIndex,
  resetForgeCache,
  searchForge,
} from "../../src/forge-reader.js";
import { getLayout, resetLayoutForTest } from "../../src/runtime-layout.js";

/** 仓库内真实知识库根（与 src 同级的 ../../../knowledge/spt-kb） */
function repoKbRoot(): string {
  const here = dirname(fileURLToPath(import.meta.url));
  return resolve(here, "..", "..", "..", "..", "knowledge", "spt-kb");
}

// -----------------------------------------------------------------------------
// 真实 Forge 快照可用性门（条件跳过）
//
// archive/forge 是 gitignored 的本机快照，按 scripts/spt-kb 流程填充。API 快照
// 文件缺失时（目录可能仍在，但只有源码克隆），依赖真实快照的断言**条件跳过**
// 而不是红：无快照机器 npm test 全绿，有快照机器全量断言。
// -----------------------------------------------------------------------------
const realCatalogPath = join(repoKbRoot(), "archive", "forge", "api", "mods-catalog.json");
const realHotIndexPath = join(repoKbRoot(), "archive", "forge", "hot-index.json");
const hasRealForgeSnapshot = existsSync(realCatalogPath) && existsSync(realHotIndexPath);
const skipNote =
  "[forge-reader.test] 本机缺少 Forge API 快照（gitignored），依赖真实快照的断言已跳过。" +
  "刷新流程见 scripts/spt-kb/（fetch -> clone -> finalize MANIFEST）。";

if (!hasRealForgeSnapshot) {
  console.warn(skipNote);
}

let fixtureRoot: string;

beforeAll(() => {
  fixtureRoot = mkdtempSync(join(tmpdir(), "spt-mcp-forge-"));
  const apiDir = join(fixtureRoot, "archive", "forge", "api");
  mkdirSync(apiDir, { recursive: true });
  // 带 BOM 的小型 fixture 目录
  writeFileSync(
    join(apiDir, "mods-catalog.json"),
    "\uFEFF" +
      JSON.stringify([
        {
          id: 1,
          name: "Weapon Overhaul",
          slug: "weapon-overhaul",
          teaser: "Rebalances all weapons",
          downloads: 500,
          category: { title: "Weapons", slug: "weapons" },
        },
        {
          id: 2,
          name: "Trader Plus",
          slug: "trader-plus",
          teaser: "Adds new traders",
          downloads: 900,
          category: { title: "Traders", slug: "traders" },
        },
        {
          id: 3,
          name: "Bot Brains",
          slug: "bot-brains",
          teaser: "Smarter AI bots",
          downloads: 1200,
          category: { title: "Bots", slug: "bots" },
        },
      ]),
  );
  writeFileSync(
    join(fixtureRoot, "archive", "forge", "hot-index.json"),
    "\uFEFF" +
      JSON.stringify([
        { id: 1, best_spt: "~3.11.0", best_version: "1.0.0" },
        { id: 2, best_spt: ">=4.0", best_version: "2.0.0" },
      ]),
  );
});

afterAll(() => {
  rmSync(fixtureRoot, { recursive: true, force: true });
});

// -----------------------------------------------------------------------------
// 目录加载（真实知识库）
// -----------------------------------------------------------------------------

describe("forge-reader: 目录加载", () => {
  // 需要本机 archive/forge API 快照；缺失则跳过（见文件头 skipNote）
  it.skipIf(!hasRealForgeSnapshot)("真实 mods-catalog.json 加载 1830 个 mod（含 BOM 兼容）", () => {
    const kbRoot = repoKbRoot();
    const catalog = loadCatalog(kbRoot);
    expect(catalog.length).toBe(1830);
    expect(catalog[0].name).toBe("FikaSync");
  });

  it.skipIf(!hasRealForgeSnapshot)("真实 hot-index.json 加载 94 条（含 BOM 兼容）", () => {
    const kbRoot = repoKbRoot();
    const hot = loadHotIndex(kbRoot);
    expect(hot.length).toBe(94);
    expect(hot[0].best_spt).toBeTruthy();
  });

  it("默认 KB 根（env 未设）指向仓库 knowledge/spt-kb", () => {
    const saved = process.env.SPT_KB_ROOT;
    delete process.env.SPT_KB_ROOT;
    resetLayoutForTest();
    try {
      expect(getLayout().kb.root.path).toBe(repoKbRoot());
    } finally {
      if (saved !== undefined) process.env.SPT_KB_ROOT = saved;
      resetLayoutForTest();
    }
  });
});

// -----------------------------------------------------------------------------
// 搜索过滤（fixture 目录）
// -----------------------------------------------------------------------------

describe("forge-reader: 搜索过滤（fixture）", () => {
  it("无过滤条件返回全部", () => {
    const result = searchForge({}, fixtureRoot);
    expect(result.total).toBe(3);
    // 按下载量降序
    expect(result.matches.map((m) => m.title)).toEqual(["Bot Brains", "Trader Plus", "Weapon Overhaul"]);
  });

  it("query 匹配标题/简介子串（不区分大小写）", () => {
    const result = searchForge({ query: "weapon" }, fixtureRoot);
    expect(result.total).toBe(1);
    expect(result.matches[0].title).toBe("Weapon Overhaul");
    const bots = searchForge({ query: "AI" }, fixtureRoot);
    expect(bots.total).toBe(1);
    expect(bots.matches[0].title).toBe("Bot Brains");
  });

  it("category 匹配 slug 或标题", () => {
    const result = searchForge({ category: "weapons" }, fixtureRoot);
    expect(result.total).toBe(1);
    expect(result.matches[0].category).toBe("Weapons");
    const byTitle = searchForge({ category: "Traders" }, fixtureRoot);
    expect(byTitle.total).toBe(1);
    expect(byTitle.matches[0].slug).toBe("trader-plus");
  });

  it("sptVersion 匹配 hot-index best_spt（合并后的 sptVersion 字段）", () => {
    const result = searchForge({ sptVersion: "3.11" }, fixtureRoot);
    expect(result.total).toBe(1);
    expect(result.matches[0].id).toBe(1);
    expect(result.matches[0].sptVersion).toBe("~3.11.0");

    const v4 = searchForge({ sptVersion: "4.1" }, fixtureRoot);
    expect(v4.total).toBe(1);
    expect(v4.matches[0].id).toBe(2);
    expect(v4.matches[0].sptVersion).toBe(">=4.0");
  });

  it("modType 过滤：快照无 mod_type 字段 -> 0 匹配并附说明", () => {
    const result = searchForge({ modType: "server" }, fixtureRoot);
    expect(result.total).toBe(0);
    expect(result.note).toContain("mod_type");
  });

  it("组合过滤：query + category", () => {
    const result = searchForge({ query: "plus", category: "traders" }, fixtureRoot);
    expect(result.total).toBe(1);
    expect(result.matches[0].title).toBe("Trader Plus");
  });

  it("bestSptMap 返回 id -> best_spt（不在 hot-index 的 id 为 undefined）", () => {
    const map = bestSptMap(fixtureRoot);
    expect(map.get(1)).toBe("~3.11.0");
    expect(map.get(3)).toBeUndefined();
  });
});

// -----------------------------------------------------------------------------
// 真实知识库搜索冒烟（需要本机 archive/forge API 快照；缺失则整组跳过）
// -----------------------------------------------------------------------------

describe.skipIf(!hasRealForgeSnapshot)("forge-reader: 真实知识库搜索冒烟", () => {
  it("真实目录 query 搜索返回非空且字段完整", () => {
    const kbRoot = repoKbRoot();
    const result = searchForge({ query: "weapon" }, kbRoot);
    expect(result.total).toBeGreaterThan(0);
    for (const m of result.matches.slice(0, 10)) {
      expect(typeof m.id).toBe("number");
      expect(typeof m.title).toBe("string");
      expect(typeof m.downloads).toBe("number");
      expect(Array.isArray(m.sptVersion)).toBe(false);
    }
  });

  it("真实目录 category 过滤全部落在该分类", () => {
    const kbRoot = repoKbRoot();
    const result = searchForge({ category: "weapons" }, kbRoot);
    expect(result.total).toBeGreaterThan(0);
    for (const m of result.matches) {
      expect(m.category).toBe("Weapons");
    }
  });

  it("resetForgeCache 后可重新加载", () => {
    resetForgeCache();
    expect(loadCatalog(repoKbRoot()).length).toBe(1830);
    resetForgeCache();
  });
});
