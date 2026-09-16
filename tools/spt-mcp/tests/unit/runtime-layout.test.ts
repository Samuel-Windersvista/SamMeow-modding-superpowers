// =============================================================================
// runtime-layout.test.ts — 共享运行时布局解析器（shared/runtime-layout.mjs）
//
// 直接对纯 ESM 模块测（不经 TS 包装），env 以参数注入，避免宿主环境变量污染。
// 覆盖规格 §5 的 8 条解析矩阵。
// =============================================================================

import { afterAll, beforeAll, describe, expect, it } from "vitest";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";

import {
  formatLayoutWarnings,
  resolveRuntimeLayout,
} from "../../../../shared/runtime-layout.mjs";

/** 写文件（自动建父目录） */
function writeFile(fullPath: string, content = "{}"): void {
  mkdirSync(dirname(fullPath), { recursive: true });
  writeFileSync(fullPath, content);
}

/** 构造 forge 归档目录 + API 快照文件（archive 校验为文件级，目录不够） */
function writeForgeSnapshot(kbRoot: string): void {
  mkdirSync(join(kbRoot, "archive", "forge", "api"), { recursive: true });
  writeFile(join(kbRoot, "archive", "forge", "api", "mods-catalog.json"), "[]");
  writeFile(join(kbRoot, "archive", "forge", "hot-index.json"), "[]");
}

/** 齐全的临时包根：KB 根 + index + archive（含 API 快照）+ 两个 helper 产物 */
function makeCompleteRoot(): string {
  const root = mkdtempSync(join(tmpdir(), "spt-layout-ok-"));
  const kbRoot = join(root, "knowledge", "spt-kb");
  writeFile(join(kbRoot, "index.json"), JSON.stringify({ entries: [] }));
  writeForgeSnapshot(kbRoot);
  writeFile(
    join(root, "tools", "spt-mcp", "helper", "bin", "Release", "spt-metadata-reader.exe"),
    "MZ",
  );
  writeFile(
    join(root, "tools", "spt-mcp", "il-helper", "bin", "Release", "spt-il-reader.exe"),
    "MZ",
  );
  return root;
}

let completeRoot: string;
let emptyRoot: string;

beforeAll(() => {
  completeRoot = makeCompleteRoot();
  emptyRoot = mkdtempSync(join(tmpdir(), "spt-layout-empty-"));
});

afterAll(() => {
  rmSync(completeRoot, { recursive: true, force: true });
  rmSync(emptyRoot, { recursive: true, force: true });
});

// -----------------------------------------------------------------------------
// 1-2. 默认派生：齐全 / 缺失
// -----------------------------------------------------------------------------

describe("runtime-layout: 默认派生", () => {
  it("env 未设 + 默认路径齐全 -> 各资源 ok，warnings 为空", () => {
    const layout = resolveRuntimeLayout(completeRoot, {});

    expect(layout.mode).toBe("portable");
    expect(layout.pluginRoot).toBe(completeRoot);

    expect(layout.kb.root).toEqual({
      path: join(completeRoot, "knowledge", "spt-kb"),
      source: "default",
      ok: true,
    });
    expect(layout.kb.index.ok).toBe(true);
    expect(layout.kb.index.path).toBe(join(completeRoot, "knowledge", "spt-kb", "index.json"));
    expect(layout.kb.archive.ok).toBe(true);
    expect(layout.kb.archive.path).toBe(
      join(completeRoot, "knowledge", "spt-kb", "archive", "forge"),
    );
    expect(layout.helpers.metadata.ok).toBe(true);
    expect(layout.helpers.il.ok).toBe(true);
    expect(layout.warnings).toEqual([]);
    expect(formatLayoutWarnings(layout)).toBe("");
  });

  it("env 未设 + 默认缺失 -> ok:false 且 reason 含路径", () => {
    const layout = resolveRuntimeLayout(emptyRoot, {});
    const kbRoot = join(emptyRoot, "knowledge", "spt-kb");

    expect(layout.kb.root.ok).toBe(false);
    expect(layout.kb.root.source).toBe("default");
    expect(layout.kb.root.path).toBe(kbRoot);
    expect(layout.kb.root.reason).toContain(kbRoot);
    expect(layout.kb.index.ok).toBe(false);
    expect(layout.kb.index.reason).toContain(join(kbRoot, "index.json"));
    expect(layout.kb.archive.ok).toBe(false);
    expect(layout.kb.archive.reason).toContain(join(kbRoot, "archive", "forge"));
  });
});

// -----------------------------------------------------------------------------
// 3-4. env 显式设置：无效即报错（不回退）/ 有效即采用
// -----------------------------------------------------------------------------

describe("runtime-layout: env SPT_KB_ROOT", () => {
  it("env 指向不存在路径 -> ok:false 且未回退（path === env 值）", () => {
    const bogus = join(emptyRoot, "does-not-exist");
    const layout = resolveRuntimeLayout(completeRoot, { SPT_KB_ROOT: bogus });

    expect(layout.kb.root.ok).toBe(false);
    expect(layout.kb.root.source).toBe("env");
    expect(layout.kb.root.path).toBe(bogus);
    expect(layout.kb.root.reason).toContain(bogus);
    expect(layout.kb.root.reason).toContain("SPT_KB_ROOT");
    // 未回退到包内默认 KB：index/archive 仍派生自 env 值
    expect(layout.kb.index.path).toBe(join(bogus, "index.json"));
    expect(layout.kb.archive.path).toBe(join(bogus, "archive", "forge"));
    expect(layout.kb.index.ok).toBe(false);
  });

  it("env 指向有效临时 KB -> source:'env'、ok:true（index/archive 继承 source）", () => {
    const kbRoot = join(completeRoot, "knowledge", "spt-kb");
    const layout = resolveRuntimeLayout(emptyRoot, { SPT_KB_ROOT: kbRoot });

    expect(layout.kb.root).toEqual({ path: kbRoot, source: "env", ok: true });
    expect(layout.kb.index.source).toBe("env");
    expect(layout.kb.index.ok).toBe(true);
    expect(layout.kb.archive.source).toBe("env");
    expect(layout.kb.archive.ok).toBe(true);
  });
});

// -----------------------------------------------------------------------------
// 5-6. 部分缺失
// -----------------------------------------------------------------------------

describe("runtime-layout: 部分缺失", () => {
  it("index 缺失（根存在）-> kb.index.ok:false、kb.root.ok:true", () => {
    const root = mkdtempSync(join(tmpdir(), "spt-layout-noidx-"));
    try {
      const kbRoot = join(root, "knowledge", "spt-kb");
      writeForgeSnapshot(kbRoot);

      const layout = resolveRuntimeLayout(root, {});
      expect(layout.kb.root.ok).toBe(true);
      expect(layout.kb.index.ok).toBe(false);
      expect(layout.kb.index.path).toBe(join(kbRoot, "index.json"));
      expect(layout.kb.archive.ok).toBe(true);
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });

  it("archive 目录整体缺失 -> kb.archive.ok:false（未随包分发语义）", () => {
    const root = mkdtempSync(join(tmpdir(), "spt-layout-noarch-"));
    try {
      const kbRoot = join(root, "knowledge", "spt-kb");
      writeFile(join(kbRoot, "index.json"), JSON.stringify({ entries: [] }));

      const layout = resolveRuntimeLayout(root, {});
      expect(layout.kb.root.ok).toBe(true);
      expect(layout.kb.index.ok).toBe(true);
      expect(layout.kb.archive.ok).toBe(false);
      expect(layout.kb.archive.reason).toContain("未随包分发或缺失");
      expect(layout.kb.archive.reason).toContain(join(kbRoot, "archive", "forge"));
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });

  it("archive 目录在但 API 快照缺失 -> kb.archive.ok:false + reason 点名缺失文件", () => {
    const root = mkdtempSync(join(tmpdir(), "spt-layout-nodata-"));
    try {
      const kbRoot = join(root, "knowledge", "spt-kb");
      writeFile(join(kbRoot, "index.json"), JSON.stringify({ entries: [] }));
      // 只建目录 + 一个文件，制造「目录在、数据缺」形态
      mkdirSync(join(kbRoot, "archive", "forge", "api"), { recursive: true });
      writeFile(join(kbRoot, "archive", "forge", "api", "mods-catalog.json"), "[]");

      const layout = resolveRuntimeLayout(root, {});
      expect(layout.kb.root.ok).toBe(true);
      expect(layout.kb.index.ok).toBe(true);
      expect(layout.kb.archive.ok).toBe(false);
      expect(layout.kb.archive.reason).toContain("forge 归档数据缺失");
      // 点名缺失的 hot-index.json，且不误报已存在的 catalog
      expect(layout.kb.archive.reason).toContain(join(kbRoot, "archive", "forge", "hot-index.json"));
      expect(layout.kb.archive.reason).not.toContain(
        join(kbRoot, "archive", "forge", "api", "mods-catalog.json"),
      );
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });

  it("archive 目录在但两个 API 快照都缺 -> reason 同时点名两个文件", () => {
    const root = mkdtempSync(join(tmpdir(), "spt-layout-nodata2-"));
    try {
      const kbRoot = join(root, "knowledge", "spt-kb");
      writeFile(join(kbRoot, "index.json"), JSON.stringify({ entries: [] }));
      mkdirSync(join(kbRoot, "archive", "forge"), { recursive: true });

      const layout = resolveRuntimeLayout(root, {});
      expect(layout.kb.archive.ok).toBe(false);
      expect(layout.kb.archive.reason).toContain(
        join(kbRoot, "archive", "forge", "api", "mods-catalog.json"),
      );
      expect(layout.kb.archive.reason).toContain(
        join(kbRoot, "archive", "forge", "hot-index.json"),
      );
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });
});

// -----------------------------------------------------------------------------
// 7. helper 定位
// -----------------------------------------------------------------------------

describe("runtime-layout: helper", () => {
  it("helper env 无效 -> helpers.metadata.ok:false（source:'env'，未回退）", () => {
    const bogus = join(emptyRoot, "no-helper.exe");
    const layout = resolveRuntimeLayout(completeRoot, { SPT_MCP_HELPER: bogus });

    expect(layout.helpers.metadata.ok).toBe(false);
    expect(layout.helpers.metadata.source).toBe("env");
    expect(layout.helpers.metadata.path).toBe(bogus);
    expect(layout.helpers.metadata.reason).toContain("SPT_MCP_HELPER");
    // IL helper 未设 env，默认仍可用
    expect(layout.helpers.il.ok).toBe(true);
  });

  it("helper 默认缺失 -> ok:false + reason 含构建提示", () => {
    const layout = resolveRuntimeLayout(emptyRoot, {});

    expect(layout.helpers.metadata.ok).toBe(false);
    expect(layout.helpers.metadata.source).toBe("default");
    expect(layout.helpers.metadata.reason).toContain("dotnet build tools/spt-mcp/helper");
    expect(layout.helpers.il.ok).toBe(false);
    expect(layout.helpers.il.reason).toContain("dotnet build tools/spt-mcp/il-helper");
  });
});

// -----------------------------------------------------------------------------
// 8. warnings 与 !ok 资源一一对应
// -----------------------------------------------------------------------------

describe("runtime-layout: warnings", () => {
  it("warnings 与 !ok 资源一一对应（顺序：kb.root/index/archive/helpers）", () => {
    const layout = resolveRuntimeLayout(emptyRoot, {});
    const statuses = [
      layout.kb.root,
      layout.kb.index,
      layout.kb.archive,
      layout.helpers.metadata,
      layout.helpers.il,
    ];
    const expected = statuses.filter((s) => !s.ok).map((s) => s.reason);

    expect(expected).toHaveLength(5);
    expect(layout.warnings).toEqual(expected);

    const text = formatLayoutWarnings(layout);
    for (const reason of expected) {
      expect(text).toContain(reason);
    }
  });

  it("mode 标签：含 .git 目录的包根 -> 'repo'", () => {
    const root = mkdtempSync(join(tmpdir(), "spt-layout-repo-"));
    try {
      mkdirSync(join(root, ".git"), { recursive: true });
      expect(resolveRuntimeLayout(root, {}).mode).toBe("repo");
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });

  it("mode 标签：.git 为文件（worktree 形态）-> 'repo'", () => {
    const root = mkdtempSync(join(tmpdir(), "spt-layout-worktree-"));
    try {
      writeFile(join(root, ".git"), "gitdir: ../.git/worktrees/example\n");
      expect(resolveRuntimeLayout(root, {}).mode).toBe("repo");
    } finally {
      rmSync(root, { recursive: true, force: true });
    }
  });
});
