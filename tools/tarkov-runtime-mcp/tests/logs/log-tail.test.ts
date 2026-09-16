// =============================================================================
// 日志 tail 器：{path → offset} 字节游标增量读取（fixture / tmp 目录驱动）
//
// 只测外部行为：poll() 返回的条目、增量不重不漏、半行不推进、截断重置、
// 新文件纳入、目录 / 文件缺失静默降级。
// =============================================================================

import { appendFile, mkdir, mkdtemp, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

import { afterEach, describe, expect, it } from "vitest";

import { FatalLogTailer, ServerLogTailer } from "../../src/logs/log-tail.js";

const tempDirs: string[] = [];

async function makeLogsRoot(): Promise<string> {
  const dir = await mkdtemp(join(tmpdir(), "tarkov-runtime-tail-"));
  tempDirs.push(dir);
  return dir;
}

async function writeLogFile(
  logsRoot: string,
  subdir: string,
  name: string,
  content: string,
): Promise<string> {
  await mkdir(join(logsRoot, subdir), { recursive: true });
  const path = join(logsRoot, subdir, name);
  await writeFile(path, content, "utf8");
  return path;
}

function fixtureLogsRoot(): string {
  return fileURLToPath(new URL("../fixtures/logwatch/logs", import.meta.url));
}

function fixtureErrorLog(): string {
  return fileURLToPath(new URL("../fixtures/logwatch/ErrorLog.log", import.meta.url));
}

const FIXED_NOW = () => new Date("2026-09-16T00:00:00.000Z");

afterEach(async () => {
  await Promise.all(tempDirs.splice(0).map((dir) => rm(dir, { recursive: true, force: true })));
});

describe("ServerLogTailer（spt / kestrel / requests 三目录）", () => {
  it("首次 poll：读取三个子目录中 Warning 及以上（source = server:<文件名>）", async () => {
    const tailer = new ServerLogTailer({ logsRoot: fixtureLogsRoot(), now: FIXED_NOW });

    const entries = await tailer.poll();

    expect(entries.map((entry) => entry.source)).toEqual([
      "server:spt20260914.log",
      "server:spt20260914.log",
      "server:spt20260914.log",
      "server:kestrel20260914.log",
    ]);
    expect(entries.map((entry) => entry.level)).toEqual(["warning", "warning", "error", "warning"]);
    // requests 目录只有 Information/Debug → 无条目
    expect(entries.some((entry) => entry.source.includes("requests"))).toBe(false);
  });

  it("第二次 poll：无新数据 → 空（偏移已推进，不重扫）", async () => {
    const tailer = new ServerLogTailer({ logsRoot: fixtureLogsRoot(), now: FIXED_NOW });

    await tailer.poll();
    const second = await tailer.poll();

    expect(second).toEqual([]);
  });

  it("追加完整行：只回新条目（不重不漏）", async () => {
    const logsRoot = await makeLogsRoot();
    const path = await writeLogFile(
      logsRoot,
      "spt",
      "spt20260914.log",
      "[2026-09-14 13:58:02.100][Warning][X] first\n",
    );
    const tailer = new ServerLogTailer({ logsRoot, now: FIXED_NOW });

    const first = await tailer.poll();
    await appendFile(path, "[2026-09-14 13:58:03.100][Error][X] second\n", "utf8");
    const second = await tailer.poll();

    expect(first.map((entry) => entry.text)).toEqual(["first"]);
    expect(second.map((entry) => entry.text)).toEqual(["second"]);
    expect(await tailer.poll()).toEqual([]);
  });

  it("半行（无换行）不返回且不推进偏移；补上换行后恰好返回一次", async () => {
    const logsRoot = await makeLogsRoot();
    const path = await writeLogFile(logsRoot, "spt", "spt20260914.log", "[2026-09-14 13:58:02.100][Warning][X] par");
    const tailer = new ServerLogTailer({ logsRoot, now: FIXED_NOW });

    expect(await tailer.poll()).toEqual([]);

    await appendFile(path, "tial\n", "utf8");
    const completed = await tailer.poll();

    expect(completed.map((entry) => entry.text)).toEqual(["partial"]);
    expect(await tailer.poll()).toEqual([]);
  });

  it("文件变短（截断 / 轮转）→ 偏移重置 0，从头重读", async () => {
    const logsRoot = await makeLogsRoot();
    const path = await writeLogFile(
      logsRoot,
      "spt",
      "spt20260914.log",
      "[2026-09-14 13:58:02.100][Warning][X] old-old-old\n",
    );
    const tailer = new ServerLogTailer({ logsRoot, now: FIXED_NOW });
    await tailer.poll();

    await writeFile(path, "[2026-09-14 13:59:00.000][Warning][X] rotated\n", "utf8");
    const afterRotation = await tailer.poll();

    expect(afterRotation.map((entry) => entry.text)).toEqual(["rotated"]);
  });

  it("新文件自动纳入（按文件名排序，日期序稳定）", async () => {
    const logsRoot = await makeLogsRoot();
    await writeLogFile(logsRoot, "spt", "spt20260914.log", "[2026-09-14 13:58:02.100][Warning][X] day14\n");
    const tailer = new ServerLogTailer({ logsRoot, now: FIXED_NOW });
    await tailer.poll();

    await writeLogFile(logsRoot, "spt", "spt20260915.log", "[2026-09-15 09:00:00.000][Warning][X] day15\n");
    const next = await tailer.poll();

    expect(next.map((entry) => entry.text)).toEqual(["day15"]);
  });

  it("目录缺失 / 不存在：静默降级为空（绝不抛出）", async () => {
    const tailer = new ServerLogTailer({
      logsRoot: join(await makeLogsRoot(), "does-not-exist"),
      now: FIXED_NOW,
    });

    await expect(tailer.poll()).resolves.toEqual([]);
  });

  it("非 .log 文件不纳入", async () => {
    const logsRoot = await makeLogsRoot();
    await writeLogFile(logsRoot, "spt", "notes.txt", "[2026-09-14 13:58:02.100][Warning][X] nope\n");
    await writeLogFile(logsRoot, "spt", "spt20260914.log", "[2026-09-14 13:58:02.100][Warning][X] yes\n");
    const tailer = new ServerLogTailer({ logsRoot, now: FIXED_NOW });

    expect((await tailer.poll()).map((entry) => entry.text)).toEqual(["yes"]);
  });

  it("UTF-8（中文）按字节偏移增量读取不乱码", async () => {
    const logsRoot = await makeLogsRoot();
    const path = await writeLogFile(
      logsRoot,
      "spt",
      "spt20260914.log",
      "[2026-09-14 13:58:02.100][Warning][X] 战利品移除失败：物品不存在\n",
    );
    const tailer = new ServerLogTailer({ logsRoot, now: FIXED_NOW });

    const first = await tailer.poll();
    await appendFile(path, "[2026-09-14 13:58:03.100][Error][X] 补丁异常：KeyNotFound\n", "utf8");
    const second = await tailer.poll();

    expect(first.map((entry) => entry.text)).toEqual(["战利品移除失败：物品不存在"]);
    expect(second.map((entry) => entry.text)).toEqual(["补丁异常：KeyNotFound"]);
  });

  it("minLevel=debug 时 Information/Debug 也纳入", async () => {
    const tailer = new ServerLogTailer({
      logsRoot: fixtureLogsRoot(),
      minLevel: "debug",
      now: FIXED_NOW,
    });

    const entries = await tailer.poll();

    // spt(2 info + 1 debug) + kestrel(2 info) + requests(2 info + 1 debug)
    expect(entries.filter((entry) => entry.level === "debug")).toHaveLength(2);
    expect(entries.filter((entry) => entry.level === "info")).toHaveLength(6);
    expect(entries.filter((entry) => entry.level === "warning")).toHaveLength(3);
    expect(entries.filter((entry) => entry.level === "error")).toHaveLength(1);
  });
});

describe("ServerLogTailer.probe（通道可用性）", () => {
  it("日志根与子目录存在 → available=true", async () => {
    const tailer = new ServerLogTailer({ logsRoot: fixtureLogsRoot(), now: FIXED_NOW });

    expect(await tailer.probe()).toEqual({ available: true });
  });

  it("日志根缺失 → logs_root_missing", async () => {
    const tailer = new ServerLogTailer({
      logsRoot: join(await makeLogsRoot(), "does-not-exist"),
      now: FIXED_NOW,
    });

    expect(await tailer.probe()).toEqual({ available: false, reason: "logs_root_missing" });
  });

  it("日志根是文件（非目录）→ logs_root_missing", async () => {
    const dir = await makeLogsRoot();
    const path = join(dir, "not-a-dir.log");
    await writeFile(path, "x", "utf8");
    const tailer = new ServerLogTailer({ logsRoot: path, now: FIXED_NOW });

    expect(await tailer.probe()).toEqual({ available: false, reason: "logs_root_missing" });
  });

  it("日志根存在但三个子目录都不存在 → no_log_dirs", async () => {
    const tailer = new ServerLogTailer({ logsRoot: await makeLogsRoot(), now: FIXED_NOW });

    expect(await tailer.probe()).toEqual({ available: false, reason: "no_log_dirs" });
  });

  it("子目录存在但无 .log 文件 → 仍 available=true（可用性只看路径解析）", async () => {
    const logsRoot = await makeLogsRoot();
    await mkdir(join(logsRoot, "spt"), { recursive: true });
    const tailer = new ServerLogTailer({ logsRoot, now: FIXED_NOW });

    expect(await tailer.probe()).toEqual({ available: true });
    expect(await tailer.poll()).toEqual([]);
  });

  it("probe 不影响读取游标（poll 仍从头读全量）", async () => {
    const tailer = new ServerLogTailer({ logsRoot: fixtureLogsRoot(), now: FIXED_NOW });

    await tailer.probe();
    const entries = await tailer.poll();

    expect(entries).toHaveLength(4);
  });
});

describe("FatalLogTailer（BepInEx ErrorLog.log）", () => {
  it("fixture：AV 栈样本 → 3 条（source=fatal，ts 取观测时刻）", async () => {
    const tailer = new FatalLogTailer({ path: fixtureErrorLog(), now: FIXED_NOW });

    const entries = await tailer.poll();

    expect(entries).toHaveLength(3);
    expect(entries.map((entry) => entry.level)).toEqual(["error", "error", "fatal"]);
    expect(entries.every((entry) => entry.source === "fatal")).toBe(true);
    expect(entries.every((entry) => entry.ts === "2026-09-16T00:00:00.000Z")).toBe(true);
    expect(entries[0].text).toContain("AccessViolationException");
    expect(entries[0].text).toContain("TrackableTransform.Update");
    expect(entries[2].text).toContain("Fatal error.");
  });

  it("未配置路径 / 路径不存在：静默降级为空", async () => {
    expect(await new FatalLogTailer({ now: FIXED_NOW }).poll()).toEqual([]);
    expect(
      await new FatalLogTailer({
        path: join(await makeLogsRoot(), "missing", "ErrorLog.log"),
        now: FIXED_NOW,
      }).poll(),
    ).toEqual([]);
  });

  it("增量：追加新的崩溃块只回新增条目", async () => {
    const logsRoot = await makeLogsRoot();
    const path = await writeLogFile(
      logsRoot,
      ".",
      "ErrorLog.log",
      "[Error  :     Unity] first crash\n  at Frame ()\n",
    );
    const tailer = new FatalLogTailer({ path, now: FIXED_NOW });

    const first = await tailer.poll();
    await appendFile(path, "[Fatal  :     Unity] second crash\n  at Frame2 ()\n", "utf8");
    const second = await tailer.poll();

    expect(first.map((entry) => entry.level)).toEqual(["error"]);
    expect(first[0].text).toBe("first crash\n  at Frame ()");
    expect(second.map((entry) => entry.level)).toEqual(["fatal"]);
    expect(second[0].text).toBe("second crash\n  at Frame2 ()");
    expect(await tailer.poll()).toEqual([]);
  });
});

describe("FatalLogTailer.probe（通道可用性）", () => {
  it("文件存在 → available=true", async () => {
    const tailer = new FatalLogTailer({ path: fixtureErrorLog(), now: FIXED_NOW });

    expect(await tailer.probe()).toEqual({ available: true });
  });

  it("未配置路径 → path_unresolved", async () => {
    expect(await new FatalLogTailer({ now: FIXED_NOW }).probe()).toEqual({
      available: false,
      reason: "path_unresolved",
    });
  });

  it("文件缺失 → file_missing", async () => {
    const tailer = new FatalLogTailer({
      path: join(await makeLogsRoot(), "missing", "ErrorLog.log"),
      now: FIXED_NOW,
    });

    expect(await tailer.probe()).toEqual({ available: false, reason: "file_missing" });
  });

  it("路径是目录 → file_missing", async () => {
    const tailer = new FatalLogTailer({ path: await makeLogsRoot(), now: FIXED_NOW });

    expect(await tailer.probe()).toEqual({ available: false, reason: "file_missing" });
  });

  it("空文件（0 字节）→ available=true 且无条目", async () => {
    const logsRoot = await makeLogsRoot();
    const path = await writeLogFile(logsRoot, ".", "ErrorLog.log", "");
    const tailer = new FatalLogTailer({ path, now: FIXED_NOW });

    expect(await tailer.probe()).toEqual({ available: true });
    expect(await tailer.poll()).toEqual([]);
  });
});
