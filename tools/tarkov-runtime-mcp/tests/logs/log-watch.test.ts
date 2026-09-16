// =============================================================================
// LogWatchService：惰性刷新的日志聚合视图（服务器 tail + fatal 通道）
//
// 只测外部行为：snapshot() 的组视图、惰性间隔语义（间隔内返回缓存）、
// 增量累计、并发去重、文件缺失静默降级。
// =============================================================================

import { appendFile, mkdir, mkdtemp, rm, writeFile } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import { fileURLToPath } from "node:url";

import { afterEach, describe, expect, it } from "vitest";

import { LogWatchService } from "../../src/logs/log-watch.js";

const tempDirs: string[] = [];

async function makeLogsRoot(): Promise<string> {
  const dir = await mkdtemp(join(tmpdir(), "tarkov-runtime-watch-"));
  tempDirs.push(dir);
  return dir;
}

async function writeSptLog(logsRoot: string, content: string, name = "spt20260914.log"): Promise<string> {
  await mkdir(join(logsRoot, "spt"), { recursive: true });
  const path = join(logsRoot, "spt", name);
  await writeFile(path, content, "utf8");
  return path;
}

function fixtureLogsRoot(): string {
  return fileURLToPath(new URL("../fixtures/logwatch/logs", import.meta.url));
}

function fixtureErrorLog(): string {
  return fileURLToPath(new URL("../fixtures/logwatch/ErrorLog.log", import.meta.url));
}

/** 可控时钟 */
function clock(start = "2026-09-16T00:00:00.000Z") {
  let current = Date.parse(start);
  return {
    now: () => new Date(current),
    advance: (ms: number) => {
      current += ms;
    },
  };
}

afterEach(async () => {
  await Promise.all(tempDirs.splice(0).map((dir) => rm(dir, { recursive: true, force: true })));
});

describe("LogWatchService（惰性刷新 + 聚合）", () => {
  it("fixture：服务器组（source=server:<文件名>）与 fatal 组（source=fatal）统一可见", async () => {
    const time = clock();
    const watch = new LogWatchService({
      logsRoot: fixtureLogsRoot(),
      errorLogPath: fixtureErrorLog(),
      intervalMs: 5000,
      now: time.now,
    });

    const { groups, overflowDropped, server, fatal } = await watch.snapshot();

    expect(overflowDropped).toBe(0);
    expect(server).toEqual({ available: true });
    expect(fatal).toEqual({ available: true });
    const bySource = groups.map((group) => group.source);
    expect(bySource).toContain("server:spt20260914.log");
    expect(bySource).toContain("server:kestrel20260914.log");
    expect(bySource).toContain("fatal");

    const fixedItem = groups.find((group) => group.key.includes("Fixed item: <id>s"));
    expect(fixedItem).toMatchObject({
      level: "warning",
      source: "server:spt20260914.log",
      count: 2,
    });

    const fatalCrash = groups.find((group) => group.source === "fatal" && group.count === 2);
    expect(fatalCrash?.key).toContain("AccessViolationException");
    expect(fatalCrash?.level).toBe("error");
  });

  it("服务器日志根缺失：server.available=false（logs_root_missing）且服务器组为空，fatal 组照常", async () => {
    const time = clock();
    const watch = new LogWatchService({
      logsRoot: join(await makeLogsRoot(), "does-not-exist"),
      errorLogPath: fixtureErrorLog(),
      intervalMs: 5000,
      now: time.now,
    });

    const { groups, server, fatal } = await watch.snapshot();

    expect(server).toEqual({ available: false, reason: "logs_root_missing" });
    expect(fatal).toEqual({ available: true });
    expect(groups.every((group) => !group.source.startsWith("server:"))).toBe(true);
    expect(groups.some((group) => group.source === "fatal")).toBe(true);
  });

  it("日志根存在但无子目录：server.available=false（no_log_dirs）", async () => {
    const time = clock();
    const watch = new LogWatchService({
      logsRoot: await makeLogsRoot(),
      intervalMs: 5000,
      now: time.now,
    });

    expect((await watch.snapshot()).server).toEqual({ available: false, reason: "no_log_dirs" });
  });

  it("fatal 路径未配置 → path_unresolved；文件缺失 → file_missing（组均为空）", async () => {
    const logsRoot = await makeLogsRoot();
    await writeSptLog(logsRoot, "[2026-09-14 13:58:02.100][Warning][X] only-server\n");
    const time = clock();

    const unconfigured = new LogWatchService({ logsRoot, intervalMs: 5000, now: time.now });
    expect((await unconfigured.snapshot()).fatal).toEqual({
      available: false,
      reason: "path_unresolved",
    });

    const missingFile = new LogWatchService({
      logsRoot,
      errorLogPath: join(logsRoot, "missing", "ErrorLog.log"),
      intervalMs: 5000,
      now: time.now,
    });
    const snapshot = await missingFile.snapshot();
    expect(snapshot.fatal).toEqual({ available: false, reason: "file_missing" });
    expect(snapshot.groups.every((group) => group.source !== "fatal")).toBe(true);
    expect(snapshot.groups.some((group) => group.source === "server:spt20260914.log")).toBe(true);
  });

  it("间隔内再次调用返回缓存（不读增量）", async () => {
    const logsRoot = await makeLogsRoot();
    const path = await writeSptLog(logsRoot, "[2026-09-14 13:58:02.100][Warning][X] first\n");
    const time = clock();
    const watch = new LogWatchService({ logsRoot, intervalMs: 5000, now: time.now });

    const first = await watch.snapshot();
    await appendFile(path, "[2026-09-14 13:58:03.100][Warning][X] second\n", "utf8");
    time.advance(4999);
    const cached = await watch.snapshot();

    expect(first.groups).toHaveLength(1);
    expect(first.groups[0].count).toBe(1);
    expect(cached).toBe(first);
  });

  it("超过间隔后增量读取：新条目并入（count 累计、组数增加）", async () => {
    const logsRoot = await makeLogsRoot();
    const path = await writeSptLog(logsRoot, "[2026-09-14 13:58:02.100][Warning][X] first\n");
    const time = clock();
    const watch = new LogWatchService({ logsRoot, intervalMs: 5000, now: time.now });

    await watch.snapshot();
    await appendFile(
      path,
      "[2026-09-14 13:58:03.100][Warning][X] first\n[2026-09-14 13:58:04.100][Error][X] second\n",
      "utf8",
    );
    time.advance(5000);
    const refreshed = await watch.snapshot();

    expect(refreshed.groups).toHaveLength(2);
    const first = refreshed.groups.find((group) => group.key === "first");
    expect(first).toMatchObject({ count: 2, level: "warning" });
    expect(refreshed.groups.find((group) => group.key === "second")).toMatchObject({ level: "error" });
  });

  it("并发调用共享同一次刷新（不重复计入 count）", async () => {
    const logsRoot = await makeLogsRoot();
    await writeSptLog(logsRoot, "[2026-09-14 13:58:02.100][Warning][X] once\n");
    const time = clock();
    const watch = new LogWatchService({ logsRoot, intervalMs: 5000, now: time.now });

    const [a, b] = await Promise.all([watch.snapshot(), watch.snapshot()]);

    expect(a.groups).toHaveLength(1);
    expect(a.groups[0].count).toBe(1);
    expect(b).toBe(a);
  });

  it("fatal 文件缺失 / 目录缺失：静默降级（不抛出）且可用性可见", async () => {
    const logsRoot = await makeLogsRoot();
    const time = clock();
    const watch = new LogWatchService({
      logsRoot,
      errorLogPath: join(logsRoot, "missing", "ErrorLog.log"),
      intervalMs: 5000,
      now: time.now,
    });

    const snapshot = await watch.snapshot();

    expect(snapshot.groups).toEqual([]);
    expect(snapshot.server).toEqual({ available: false, reason: "no_log_dirs" });
    expect(snapshot.fatal).toEqual({ available: false, reason: "file_missing" });
  });

  it("未配置 fatal 路径：只回服务器组", async () => {
    const logsRoot = await makeLogsRoot();
    await writeSptLog(logsRoot, "[2026-09-14 13:58:02.100][Warning][X] only-server\n");
    const time = clock();
    const watch = new LogWatchService({ logsRoot, intervalMs: 5000, now: time.now });

    const { groups } = await watch.snapshot();

    expect(groups.map((group) => group.source)).toEqual(["server:spt20260914.log"]);
  });

  it("组视图按桥侧同序排序（count 降序 → lastTs 降序 → key 升序）", async () => {
    const logsRoot = await makeLogsRoot();
    await writeSptLog(
      logsRoot,
      [
        "[2026-09-14 13:58:00.000][Warning][X] many",
        "[2026-09-14 13:58:01.000][Warning][X] many",
        "[2026-09-14 13:59:00.000][Warning][X] single-late",
        "[2026-09-14 13:58:30.000][Warning][X] single-early",
      ].join("\n") + "\n",
    );
    const time = clock();
    const watch = new LogWatchService({ logsRoot, intervalMs: 5000, now: time.now });

    const { groups } = await watch.snapshot();

    expect(groups.map((group) => group.key)).toEqual(["many", "single-late", "single-early"]);
  });
});
