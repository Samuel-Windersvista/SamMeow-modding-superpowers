// =============================================================================
// 日志行解析（服务器三目录 + fatal 通道）
//
// 行格式（2026-09-16 采样，三目录统一）：
//   [2026-09-14 13:58:00.159][Warning][SPTarkov.Server.X] message
// fatal 通道（BepInEx ErrorLog.log）：
//   [Error  :     Unity] message + 缩进栈续行（以 `[级别  :` 前缀行为条目边界）
// =============================================================================

import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

import { describe, expect, it } from "vitest";

import {
  buildLogEntries,
  localTimestampToIsoUtc,
  parseErrorLogLine,
  parseErrorLogEntries,
  parseServerLogEntries,
  parseServerLogLine,
} from "../../src/logs/log-parse.js";

function fixture(name: string): string {
  return readFileSync(fileURLToPath(new URL(`../fixtures/logwatch/${name}`, import.meta.url)), "utf8");
}

/** 本地时间（与服务端日志同规则）→ UTC ISO */
function localIso(year: number, month: number, day: number, h: number, m: number, s: number, ms: number) {
  return new Date(year, month - 1, day, h, m, s, ms).toISOString();
}

describe("localTimestampToIsoUtc（行内本地时间 → UTC ISO）", () => {
  it("毫秒 3 位 / 无小数 / 7 位（截断到毫秒）", () => {
    expect(localTimestampToIsoUtc("2026-09-14 13:58:00.159")).toBe(
      localIso(2026, 9, 14, 13, 58, 0, 159),
    );
    expect(localTimestampToIsoUtc("2026-09-14 13:58:00")).toBe(localIso(2026, 9, 14, 13, 58, 0, 0));
    expect(localTimestampToIsoUtc("2026-09-14 13:58:00.1234567")).toBe(
      localIso(2026, 9, 14, 13, 58, 0, 123),
    );
  });

  it("非法 → null", () => {
    expect(localTimestampToIsoUtc("not-a-timestamp")).toBeNull();
    expect(localTimestampToIsoUtc("")).toBeNull();
  });
});

describe("parseServerLogLine", () => {
  it("`[ts][Level][Source] text` → ts/level/text", () => {
    expect(
      parseServerLogLine(
        "[2026-09-14 13:58:02.100][Warning][SPTarkov.Server.Core.Helpers.ItemHelper] Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1",
      ),
    ).toEqual({
      ts: localIso(2026, 9, 14, 13, 58, 2, 100),
      level: "warning",
      text: "Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1",
    });
  });

  it("级别名归一（Information → info）", () => {
    expect(
      parseServerLogLine("[2026-09-14 13:58:00.000][Information][X] hello")?.level,
    ).toBe("info");
  });

  it("无前缀续行（异常栈）→ null", () => {
    expect(parseServerLogLine("  at SPTarkov.Server.Core.Helpers.InventoryHelper.RemoveItem()")).toBeNull();
    expect(parseServerLogLine("")).toBeNull();
  });
});

describe("parseErrorLogLine（BepInEx ErrorLog.log）", () => {
  it("`[Level  : Source] text` → level/text（无时间戳）", () => {
    expect(parseErrorLogLine("[Error  :     Unity] AccessViolationException: boom")).toEqual({
      ts: null,
      level: "error",
      text: "AccessViolationException: boom",
    });
    expect(parseErrorLogLine("[Fatal  :     Unity] Fatal error. boom")).toEqual({
      ts: null,
      level: "fatal",
      text: "Fatal error. boom",
    });
  });

  it("缩进栈续行 → null", () => {
    expect(parseErrorLogLine("  at TrackableTransform.Update () [0x0001a]")).toBeNull();
  });
});

describe("parseServerLogEntries（默认仅 Warning 及以上）", () => {
  it("fixture：只纳入 Warning/Error，续行归入上一条目，source = server:<文件名>", () => {
    const entries = parseServerLogEntries(fixture("logs/spt/spt20260914.log"), {
      source: "server:spt20260914.log",
    });

    expect(entries.map((entry) => entry.level)).toEqual(["warning", "warning", "error"]);
    expect(entries.map((entry) => entry.source)).toEqual([
      "server:spt20260914.log",
      "server:spt20260914.log",
      "server:spt20260914.log",
    ]);
    expect(entries[0].text).toBe(
      "Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1",
    );
    expect(entries[2].text).toContain("KeyNotFoundException: loot patch failed");
    expect(entries[2].text).toContain("at SPTarkov.Server.Core.Helpers.InventoryHelper.RemoveItem");
    expect(entries[2].text.split("\n")).toHaveLength(3);
  });

  it("minLevel=info：Information 纳入，Debug（更轻）仍排除", () => {
    const entries = parseServerLogEntries(fixture("logs/spt/spt20260914.log"), {
      source: "server:spt20260914.log",
      minLevel: "info",
    });

    expect(entries.map((entry) => entry.level)).toEqual([
      "info",
      "warning",
      "warning",
      "error",
      "info",
    ]);
  });

  it("minLevel=debug：全部级别纳入", () => {
    const entries = parseServerLogEntries(fixture("logs/spt/spt20260914.log"), {
      source: "server:spt20260914.log",
      minLevel: "debug",
    });

    expect(entries.map((entry) => entry.level)).toEqual([
      "info",
      "debug",
      "warning",
      "warning",
      "error",
      "info",
    ]);
  });

  it("kestrel fixture：Warning 纳入（多目录通道可用）", () => {
    const entries = parseServerLogEntries(fixture("logs/kestrel/kestrel20260914.log"), {
      source: "server:kestrel20260914.log",
    });

    expect(entries).toHaveLength(1);
    expect(entries[0].source).toBe("server:kestrel20260914.log");
  });

  it("requests fixture：仅 Information/Debug → 无条目", () => {
    expect(
      parseServerLogEntries(fixture("logs/requests/requests20260914.log"), {
        source: "server:requests20260914.log",
      }),
    ).toEqual([]);
  });

  it("首个续行无上一条目 → 跳过（不臆造条目）", () => {
    const content = [
      "  at orphaned frame",
      "[2026-09-14 13:58:02.100][Warning][X] real warning",
    ].join("\n");

    const entries = parseServerLogEntries(content, { source: "server:spt20260914.log" });

    expect(entries).toHaveLength(1);
    expect(entries[0].text).toBe("real warning");
  });

  it("未知级别 → 不纳入（无法证明达到阈值）", () => {
    const content = "[2026-09-14 13:58:02.100][Verbose][X] noisy";

    expect(parseServerLogEntries(content, { source: "server:spt20260914.log" })).toEqual([]);
  });

  it("CRLF 与 BOM 容错（续行不带 \\r）", () => {
    const content =
      "\uFEFF[2026-09-14 13:58:03.000][Error][X] boom\r\n  at Frame ()\r\n  at Frame2 ()\r\n";

    const entries = parseServerLogEntries(content, { source: "server:spt20260914.log" });

    expect(entries).toHaveLength(1);
    expect(entries[0].text).toBe("boom\n  at Frame ()\n  at Frame2 ()");
  });
});

describe("parseErrorLogEntries（fatal 通道：整文件视为错误输出，不过滤级别）", () => {
  it("fixture：AV 栈样本 → 2 组条目（2 条同错误 + 1 条 Fatal），ts 取观测时刻", () => {
    const observedAt = "2026-09-16T00:00:00.000Z";
    const entries = parseErrorLogEntries(fixture("ErrorLog.log"), { fallbackTs: observedAt });

    expect(entries).toHaveLength(3);
    expect(entries.map((entry) => entry.level)).toEqual(["error", "error", "fatal"]);
    expect(entries.every((entry) => entry.source === "fatal")).toBe(true);
    expect(entries.every((entry) => entry.ts === observedAt)).toBe(true);
    expect(entries[0].text).toContain("AccessViolationException");
    expect(entries[0].text).toContain("TrackableTransform.Update");
    expect(entries[0].text).toContain("il2cpp_runtime_invoke");
    expect(entries[0].text).toContain("coreclr.dll!0xc0000005");
  });

  it("未知级别名 → 回退默认级别 error（fatal 通道仍纳入）", () => {
    const entries = parseErrorLogEntries("[Weird  :  Unity] boom", {
      fallbackTs: "2026-09-16T00:00:00.000Z",
    });

    expect(entries).toHaveLength(1);
    expect(entries[0].level).toBe("error");
  });
});

describe("buildLogEntries（通用装配）", () => {
  it("空内容 → 空数组", () => {
    expect(
      buildLogEntries("", parseServerLogLine, { source: "server:x.log", fallbackTs: "t" }),
    ).toEqual([]);
  });
});
