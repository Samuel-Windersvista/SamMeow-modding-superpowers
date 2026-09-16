// =============================================================================
// analyze-conflicts.test.ts — IL helper 降级的可见性
//
// readIlPatches 在 helper 不可用时返回空数组，与「DLL 里没有 patch」不可区分；
// spt_analyze_conflicts 必须把该降级显式带进结果 warnings。
// =============================================================================

import { afterEach, beforeEach, describe, expect, it } from "vitest";
import { mkdirSync, mkdtempSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join } from "node:path";

import { runAnalyzeConflicts } from "../../src/tools/analyze-conflicts.js";
import { resetLayoutForTest } from "../../src/runtime-layout.js";

const ORIGINAL_IL_HELPER = process.env.SPT_IL_HELPER;

let tmp: string;

/** 写文件（自动建父目录） */
function writeFile(fullPath: string, content: string): void {
  mkdirSync(dirname(fullPath), { recursive: true });
  writeFileSync(fullPath, content);
}

beforeEach(() => {
  tmp = mkdtempSync(join(tmpdir(), "spt-mcp-analyze-"));
});

afterEach(() => {
  if (ORIGINAL_IL_HELPER === undefined) {
    delete process.env.SPT_IL_HELPER;
  } else {
    process.env.SPT_IL_HELPER = ORIGINAL_IL_HELPER;
  }
  resetLayoutForTest();
  rmSync(tmp, { recursive: true, force: true });
});

describe("spt_analyze_conflicts: IL helper 降级可见性", () => {
  it("IL helper 不可用 -> 结果 warnings 显式说明降级", () => {
    // 伪造：env 显式指向不存在的 helper（显式无效即报错，不回退）
    process.env.SPT_IL_HELPER = join(tmp, "no-such-il-helper.exe");
    resetLayoutForTest();

    const dllPath = join(tmp, "BepInEx", "plugins", "SomeClientMod.dll");
    writeFile(dllPath, "fake dll bytes");

    const res = runAnalyzeConflicts({ modPaths: [dllPath], sptPath: tmp });
    expect(res.ok).toBe(true);
    if (!res.ok) return;

    const data = res.data as { warnings: string[]; ilPatchCount: number };
    expect(data.ilPatchCount).toBe(0);
    expect(data.warnings.some((w) => w.includes("IL 级检测已降级"))).toBe(true);
    expect(data.warnings.some((w) => w.includes("SPT_IL_HELPER"))).toBe(true);
  });

  it("无 client mod 时不产生 IL 降级噪音（IL 未参与分析）", () => {
    process.env.SPT_IL_HELPER = join(tmp, "no-such-il-helper.exe");
    resetLayoutForTest();

    const modDir = join(tmp, "user", "mods", "SomeServerMod");
    writeFile(join(modDir, "package.json"), JSON.stringify({ name: "Server Mod", guid: "com.test" }));

    const res = runAnalyzeConflicts({ modPaths: [modDir], sptPath: tmp });
    expect(res.ok).toBe(true);
    if (!res.ok) return;

    const data = res.data as { warnings: string[] };
    expect(data.warnings.some((w) => w.includes("IL 级检测已降级"))).toBe(false);
  });
});
