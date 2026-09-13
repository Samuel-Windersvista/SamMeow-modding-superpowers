// =============================================================================
// S2 日志读取器：定位并读取 SPT server 日志文件，产出 mod 清单或结构化降级
//
// 只测外部行为：给定日志目录/文件，断言返回的清单或降级原因。
// 目录约定依据 SPT 5.0 `sptLogger.json`：`./user/logs/spt/` + `spt%DATE%.log`。
// =============================================================================

import { mkdtemp, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";
import { afterEach, describe, expect, it } from "vitest";

import { readServerModList, resolveServerLogDir } from "../../src/logs/log-reader.js";

const tempDirs: string[] = [];

async function makeTempDir(): Promise<string> {
  const dir = await mkdtemp(join(tmpdir(), "tarkov-runtime-logs-"));
  tempDirs.push(dir);
  return dir;
}

function fixtureDir(name: string): string {
  return fileURLToPath(new URL(`../fixtures/logs/${name}`, import.meta.url));
}

afterEach(async () => {
  await Promise.all(tempDirs.splice(0).map((dir) => rm(dir, { recursive: true, force: true })));
});

describe("readServerModList", () => {
  it("正常日志目录：读取日志并返回可用的 mod 清单", async () => {
    const result = await readServerModList({ logDir: fixtureDir("normal") });

    expect(result.available).toBe(true);
    if (!result.available) return;
    expect(result.source).toBe("server-log");
    expect(result.count).toBe(3);
    expect(result.declaredCount).toBe(3);
    expect(result.allModsRejected).toBe(false);
    expect(result.mods.map((mod) => mod.name)).toEqual(["MyMod", "Other Mod", "SimpleMod"]);
    expect(result.logFile.endsWith("spt20260913.log")).toBe(true);
  });

  it("空日志：可用但清单为空", async () => {
    const result = await readServerModList({ logDir: fixtureDir("empty") });

    expect(result.available).toBe(true);
    if (!result.available) return;
    expect(result.count).toBe(0);
    expect(result.mods).toEqual([]);
  });

  it("日志目录缺失：结构化降级为 log_directory_missing", async () => {
    const result = await readServerModList({
      logDir: fixtureDir("does-not-exist"),
    });

    expect(result.available).toBe(false);
    if (result.available) return;
    expect(result.source).toBe("server-log");
    expect(result.reason).toBe("log_directory_missing");
    expect(result.message).toContain("日志目录");
  });

  it("目录存在但无日志文件：结构化降级为 log_file_missing", async () => {
    const dir = await makeTempDir();
    const result = await readServerModList({ logDir: dir });

    expect(result.available).toBe(false);
    if (result.available) return;
    expect(result.reason).toBe("log_file_missing");
  });

  it("显式指定日志文件且文件缺失：结构化降级为 log_file_missing", async () => {
    const dir = await makeTempDir();
    const result = await readServerModList({ logDir: dir, logFile: join(dir, "spt20260913.log") });

    expect(result.available).toBe(false);
    if (result.available) return;
    expect(result.reason).toBe("log_file_missing");
    expect(result.logFile).toBe(join(dir, "spt20260913.log"));
  });

  it("多日日志：选择日期最新的日志文件", async () => {
    const dir = await makeTempDir();
    await writeFile(
      join(dir, "spt20260912.log"),
      "[2026-09-12 09:00:00.000][Information][SPTarkov.Server.Modding.ModValidator] ModLoader: loading: 1 server mods...\n" +
        "[2026-09-12 09:00:01.000][Information][SPTarkov.Server.Modding.ModValidator] Mod: Old version: 1.0.0 (GUID: com.example.old | targets SPT: ~5.0.0) by: A loaded\n",
      "utf8",
    );
    await writeFile(
      join(dir, "spt20260913.log"),
      "[2026-09-13 09:00:00.000][Information][SPTarkov.Server.Modding.ModValidator] ModLoader: loading: 1 server mods...\n" +
        "[2026-09-13 09:00:01.000][Information][SPTarkov.Server.Modding.ModValidator] Mod: Fresh version: 2.0.0 (GUID: com.example.fresh | targets SPT: ~5.0.0) by: B loaded\n",
      "utf8",
    );

    const result = await readServerModList({ logDir: dir });

    expect(result.available).toBe(true);
    if (!result.available) return;
    expect(result.logFile.endsWith("spt20260913.log")).toBe(true);
    expect(result.mods.map((mod) => mod.name)).toEqual(["Fresh"]);
  });
});

describe("resolveServerLogDir", () => {
  it("显式 LOG_DIR 优先", () => {
    expect(resolveServerLogDir({ TARKOV_RUNTIME_MCP_LOG_DIR: "D:/logs" }, "C:/cwd")).toBe("D:/logs");
  });

  it("SPT_DIR 存在时拼出 user/logs/spt", () => {
    const result = resolveServerLogDir({ TARKOV_RUNTIME_MCP_SPT_DIR: "C:/SPT" }, "C:/cwd");
    expect(result.replace(/\\/g, "/")).toBe("C:/SPT/user/logs/spt");
  });

  it("无配置时回落到 cwd/user/logs/spt", () => {
    const result = resolveServerLogDir({}, "C:/cwd");
    expect(result.replace(/\\/g, "/")).toBe("C:/cwd/user/logs/spt");
  });
});
