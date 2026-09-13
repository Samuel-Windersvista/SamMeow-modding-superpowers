// =============================================================================
// S2 日志解析器（server 日志 -> mod 加载清单）行为测试
//
// 只测外部行为：给定日志文本/fixture 文件，断言解析结果。
// 日志格式依据 SPT 5.0 源码：
//   - 文件格式模板 `[%date% %time%][%level%][%logger%] %message%`
//     （SPTushonka.Server/sptLogger.json，SPTushonka.Common/.../BaseLogHandler.cs）
//   - 每个已加载 mod 一行 `Mod: {name} version: {version} by: {author} loaded`，
//     其中 version = `{Version} (GUID: {ModGuid} | targets SPT: {SptVersion})`
//     （SPTushonka.Server/Modding/ModValidator.cs AddMod + locales/server/en.json）
// =============================================================================

import { readFileSync } from "node:fs";
import { describe, expect, it } from "vitest";

import { parseServerLogMods } from "../../src/logs/mod-list.js";

function fixture(name: string): string {
  return readFileSync(new URL(`../fixtures/logs/${name}/spt20260913.log`, import.meta.url), "utf8");
}

describe("parseServerLogMods", () => {
  it("正常日志：提取 mod 清单（名称/版本/GUID/作者/目标 SPT）与声明数量", () => {
    const result = parseServerLogMods(fixture("normal"));

    expect(result.declaredCount).toBe(3);
    expect(result.allModsRejected).toBe(false);
    expect(result.mods).toEqual([
      {
        name: "MyMod",
        version: "1.2.3",
        guid: "com.example.mymod",
        author: "Author",
        targetsSpt: "~5.0.0",
      },
      {
        name: "Other Mod",
        version: "0.4.0",
        guid: "org.example.other",
        author: "Someone",
        targetsSpt: "~5.0.0",
      },
      {
        name: "SimpleMod",
        version: "2.0.0",
        guid: null,
        author: "Dev",
        targetsSpt: null,
      },
    ]);
  });

  it("空日志：返回空清单且不抛异常", () => {
    const result = parseServerLogMods(fixture("empty"));

    expect(result.mods).toEqual([]);
    expect(result.declaredCount).toBeNull();
    expect(result.allModsRejected).toBe(false);
  });

  it("畸形行：忽略无法解析的行，保留可解析的 mod 行", () => {
    const result = parseServerLogMods(fixture("malformed"));

    expect(result.declaredCount).toBe(2);
    expect(result.mods).toEqual([
      {
        name: "GoodMod",
        version: "1.0.0",
        guid: "com.example.good",
        author: "Author",
        targetsSpt: "~5.0.0",
      },
    ]);
  });

  it("同一天多次启动：只取最后一次加载会话的清单", () => {
    const result = parseServerLogMods(fixture("multi-run"));

    expect(result.declaredCount).toBe(2);
    expect(result.mods.map((mod) => mod.name)).toEqual(["NewModA", "NewModB"]);
  });

  it("校验失败（no_mods_loaded）：清单为空且标记 allModsRejected", () => {
    const result = parseServerLogMods(fixture("rejected"));

    expect(result.declaredCount).toBe(1);
    expect(result.allModsRejected).toBe(true);
    expect(result.mods).toEqual([]);
  });

  it("任意文本不抛异常", () => {
    expect(() => parseServerLogMods("")).not.toThrow();
    expect(() => parseServerLogMods("\u0000\u0001乱码")).not.toThrow();
  });
});
