import { describe, expect, it } from "vitest";

import {
  analyzeConflicts,
  DEFAULT_TARGET_SPT_VERSION,
  isCompatibleWithTarget,
  predictLoadOrder,
  type AnalyzeConflictInput,
  type ConfigFileKeys,
} from "../../src/conflict-engine.js";
import type { ModFileEntry, ModMetadata } from "../../src/types.js";

function serverMod(partial: {
  path: string;
  name: string;
  guid?: string;
  version?: string;
  sptVersion?: string;
  compatibleVersion?: string;
  typePriority?: number;
  dependencies?: string[];
}): ModMetadata {
  return {
    type: "server",
    dependencies: partial.dependencies ?? [],
    packageJson: {},
    typePriority: partial.typePriority ?? 0,
    ...partial,
  };
}

function clientMod(path: string, name: string): ModMetadata {
  return { type: "client", path, modPath: path, name, fileName: `${name}.dll`, dependencies: [] };
}

function files(modPath: string, paths: string[]): Record<string, ModFileEntry[]> {
  return {
    [modPath]: paths.map((relativePath, i) => ({ relativePath, sizeBytes: i + 1 })),
  };
}

function configs(modPath: string, list: ConfigFileKeys[]): Record<string, ConfigFileKeys[]> {
  return { [modPath]: list };
}

function run(input: AnalyzeConflictInput) {
  return analyzeConflicts(input);
}

// -----------------------------------------------------------------------------
// GUID 重复检测
// -----------------------------------------------------------------------------

describe("conflict-engine: GUID 重复检测", () => {
  it("相同 GUID（忽略大小写）的两个 server mod -> B 级 guid_duplicate", () => {
    const report = run({
      mods: [
        serverMod({ path: "/mods/a", name: "Mod A", guid: "com.foo.bar" }),
        serverMod({ path: "/mods/b", name: "Mod B", guid: "COM.FOO.BAR" }),
      ],
    });
    const b = report.sections.B.filter((f) => f.kind === "guid_duplicate");
    expect(b).toHaveLength(1);
    expect(b[0].mods).toEqual(["Mod A", "Mod B"]);
    expect(b[0].detail?.guid).toBe("com.foo.bar");
    expect(report.summary.bySeverity.B).toBe(1);
  });

  it("GUID 不同或缺失 -> 无 guid_duplicate", () => {
    const report = run({
      mods: [
        serverMod({ path: "/mods/a", name: "A", guid: "com.a.x" }),
        serverMod({ path: "/mods/b", name: "B", guid: "com.b.y" }),
        serverMod({ path: "/mods/c", name: "C" }),
        clientMod("/mods/d/plug.dll", "Plug"),
      ],
    });
    expect(report.sections.B.filter((f) => f.kind === "guid_duplicate")).toHaveLength(0);
  });
});

// -----------------------------------------------------------------------------
// SPT 版本失配检测
// -----------------------------------------------------------------------------

describe("conflict-engine: SPT 版本失配检测", () => {
  it("sptVersion 3.8 与目标 4.1 不兼容 -> B 级 spt_version_mismatch", () => {
    const report = run({
      mods: [serverMod({ path: "/mods/a", name: "Old Mod", sptVersion: "3.8" })],
    });
    const m = report.sections.B.filter((f) => f.kind === "spt_version_mismatch");
    expect(m).toHaveLength(1);
    expect(m[0].detail?.declared).toBe("3.8");
    expect(m[0].detail?.target).toBe(DEFAULT_TARGET_SPT_VERSION);
  });

  it("兼容声明（~4.1.0 / >=4.0 / 4.0 - 4.2 / ^4.1.0 / 4.x）-> 无失配", () => {
    const mods = [
      serverMod({ path: "/mods/a", name: "A", sptVersion: "~4.1.0" }),
      serverMod({ path: "/mods/b", name: "B", compatibleVersion: ">=4.0" }),
      serverMod({ path: "/mods/c", name: "C", sptVersion: "4.0 - 4.2" }),
      serverMod({ path: "/mods/d", name: "D", sptVersion: "^4.1.0" }),
      serverMod({ path: "/mods/e", name: "E", sptVersion: "4.x" }),
    ];
    const report = run({ mods });
    expect(report.sections.B.filter((f) => f.kind === "spt_version_mismatch")).toHaveLength(0);
  });

  it("未声明版本字段 -> 不误报", () => {
    const report = run({ mods: [serverMod({ path: "/mods/a", name: "A" })] });
    expect(report.sections.B.filter((f) => f.kind === "spt_version_mismatch")).toHaveLength(0);
  });

  it("多版本逗号列表都不含 4.1 -> 失配", () => {
    const report = run({
      mods: [serverMod({ path: "/mods/a", name: "A", sptVersion: "3.9, 4.0" })],
    });
    expect(report.sections.B.filter((f) => f.kind === "spt_version_mismatch")).toHaveLength(1);
  });
});

describe("conflict-engine: isCompatibleWithTarget 版本语义", () => {
  it("支持常用 Range 语法", () => {
    expect(isCompatibleWithTarget("~4.1.0", "4.1")).toBe(true);
    expect(isCompatibleWithTarget("~4.0", "4.1")).toBe(false);
    expect(isCompatibleWithTarget(">=4.0", "4.1")).toBe(true);
    expect(isCompatibleWithTarget("<4.0", "4.1")).toBe(false);
    expect(isCompatibleWithTarget(">4.0", "4.1")).toBe(true);
    expect(isCompatibleWithTarget("<=4.0", "4.1")).toBe(false);
    expect(isCompatibleWithTarget("4.x", "4.1")).toBe(true);
    expect(isCompatibleWithTarget("3.x", "4.1")).toBe(false);
    expect(isCompatibleWithTarget("4.0 - 4.2", "4.1")).toBe(true);
    expect(isCompatibleWithTarget("^4.1.0", "4.1")).toBe(true);
    expect(isCompatibleWithTarget("3.9, 4.0", "4.1")).toBe(false);
    expect(isCompatibleWithTarget("4.1.0", "4.1")).toBe(true);
    expect(isCompatibleWithTarget("4.1.2", "4.1")).toBe(false);
    expect(isCompatibleWithTarget("=4.1", "4.1")).toBe(true);
    expect(isCompatibleWithTarget("v4.1", "4.1")).toBe(true);
  });

  it("无法解析的声明按未知处理（不误报）", () => {
    expect(isCompatibleWithTarget("not-a-version", "4.1")).toBe(true);
  });
});

// -----------------------------------------------------------------------------
// 文件覆盖检测
// -----------------------------------------------------------------------------

describe("conflict-engine: 文件覆盖检测", () => {
  it("两个 mod 含相同相对路径文件 -> O 级 file_overwrite", () => {
    const report = run({
      mods: [
        serverMod({ path: "/mods/a", name: "A" }),
        serverMod({ path: "/mods/b", name: "B" }),
      ],
      filesByModPath: files("/mods/a", ["config/items.json", "unique.txt"]),
    });
    // 注意：filesByModPath 需包含两个 mod 才有重叠
    const report2 = run({
      mods: [
        serverMod({ path: "/mods/a", name: "A" }),
        serverMod({ path: "/mods/b", name: "B" }),
      ],
      filesByModPath: {
        "/mods/a": [{ relativePath: "config/items.json", sizeBytes: 10 }],
        "/mods/b": [{ relativePath: "config/items.json", sizeBytes: 20 }],
      },
    });
    expect(report.sections.O.filter((f) => f.kind === "file_overwrite")).toHaveLength(0);
    const o = report2.sections.O.filter((f) => f.kind === "file_overwrite");
    expect(o).toHaveLength(1);
    expect(o[0].detail?.relativePath).toBe("config/items.json");
    expect(o[0].mods.sort()).toEqual(["A", "B"]);
  });

  it("文件路径互不重叠 -> 无 file_overwrite", () => {
    const report = run({
      mods: [
        serverMod({ path: "/mods/a", name: "A" }),
        serverMod({ path: "/mods/b", name: "B" }),
      ],
      filesByModPath: {
        "/mods/a": [{ relativePath: "a.txt", sizeBytes: 1 }],
        "/mods/b": [{ relativePath: "b.txt", sizeBytes: 2 }],
      },
    });
    expect(report.sections.O.filter((f) => f.kind === "file_overwrite")).toHaveLength(0);
  });
});

// -----------------------------------------------------------------------------
// 配置 key 碰撞检测
// -----------------------------------------------------------------------------

describe("conflict-engine: 配置 key 碰撞检测", () => {
  it("同配置文件 + 重叠 key -> O 级 config_collision", () => {
    const report = run({
      mods: [
        serverMod({ path: "/mods/a", name: "A" }),
        serverMod({ path: "/mods/b", name: "B" }),
      ],
      configsByModPath: configs("/mods/a", [
        { filename: "config.json", keys: ["enabled", "price"] },
      ]),
    });
    const report2 = run({
      mods: [
        serverMod({ path: "/mods/a", name: "A" }),
        serverMod({ path: "/mods/b", name: "B" }),
      ],
      configsByModPath: {
        "/mods/a": [{ filename: "config.json", keys: ["enabled", "price"] }],
        "/mods/b": [{ filename: "config.json", keys: ["enabled", "weight"] }],
      },
    });
    expect(report.sections.O.filter((f) => f.kind === "config_collision")).toHaveLength(0);
    const o = report2.sections.O.filter((f) => f.kind === "config_collision");
    expect(o).toHaveLength(1);
    expect(o[0].detail?.overlappingKeys).toEqual(["enabled"]);
  });

  it("同配置文件但 key 无交集 -> 无碰撞", () => {
    const report = run({
      mods: [
        serverMod({ path: "/mods/a", name: "A" }),
        serverMod({ path: "/mods/b", name: "B" }),
      ],
      configsByModPath: {
        "/mods/a": [{ filename: "config.json", keys: ["a"] }],
        "/mods/b": [{ filename: "config.json", keys: ["b"] }],
      },
    });
    expect(report.sections.O.filter((f) => f.kind === "config_collision")).toHaveLength(0);
  });
});

// -----------------------------------------------------------------------------
// 加载顺序预测
// -----------------------------------------------------------------------------

describe("conflict-engine: 加载顺序预测", () => {
  it("TypePriority 升序，同优先级按 GUID 字母序（忽略大小写）", () => {
    const order = predictLoadOrder([
      serverMod({ path: "/mods/b", name: "B", guid: "com.zzz.b", typePriority: 100 }),
      serverMod({ path: "/mods/a", name: "A", guid: "com.aaa.a", typePriority: 0 }),
      serverMod({ path: "/mods/c", name: "C", guid: "COM.MMM.C", typePriority: 100 }),
    ]);
    expect(order.map((e) => e.name)).toEqual(["A", "C", "B"]);
    expect(order[0].position).toBe(1);
    expect(order[0].typePriority).toBe(0);
  });

  it("client mod 不参与加载顺序", () => {
    const order = predictLoadOrder([
      serverMod({ path: "/mods/a", name: "A", guid: "com.a", typePriority: 0 }),
      clientMod("/mods/d/plug.dll", "Plug"),
    ]);
    expect(order).toHaveLength(1);
    expect(order[0].name).toBe("A");
  });

  it("无 GUID 的 mod 按空串参与排序（排在字母前），并按名字兜底", () => {
    const order = predictLoadOrder([
      serverMod({ path: "/mods/a", name: "Zeta", typePriority: 0 }),
      serverMod({ path: "/mods/b", name: "Alpha", guid: "com.a", typePriority: 0 }),
    ]);
    // 无 GUID -> "" 排最前
    expect(order[0].name).toBe("Zeta");
    expect(order[1].name).toBe("Alpha");
  });
});

// -----------------------------------------------------------------------------
// 报告汇总
// -----------------------------------------------------------------------------

describe("conflict-engine: 报告汇总", () => {
  it("total 与 bySeverity 统计正确", () => {
    const report = run({
      mods: [
        serverMod({ path: "/mods/a", name: "A", guid: "com.dup", sptVersion: "3.8" }),
        serverMod({ path: "/mods/b", name: "B", guid: "COM.DUP", typePriority: 50 }),
      ],
      filesByModPath: {
        "/mods/a": [{ relativePath: "shared.txt", sizeBytes: 1 }],
        "/mods/b": [{ relativePath: "shared.txt", sizeBytes: 2 }],
      },
    });
    expect(report.summary.bySeverity.B).toBe(2); // guid_duplicate + version_mismatch
    expect(report.summary.bySeverity.O).toBe(1); // file_overwrite
    expect(report.summary.total).toBe(3);
    expect(report.sections.S).toHaveLength(0);
    expect(report.sections.C).toHaveLength(0);
    expect(report.sections.Unknown).toHaveLength(0);
  });
});
