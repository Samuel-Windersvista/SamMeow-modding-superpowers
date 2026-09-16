// =============================================================================
// 日志级别归一化与严重度比较（桥侧 `LogWatchLevel` 的 TS 移植）
//
// 服务端日志用 `Debug|Information|Warning|Error`（+ Fatal），桥侧 / BepInEx 用
// `fatal|error|warning|message|info|debug`；本模块统一为后者的小写字面量。
// 严重度排序走显式排名，绝不比较字符串 / 枚举数值。
// =============================================================================

import { describe, expect, it } from "vitest";

import {
  DEFAULT_LOG_MIN_LEVEL,
  LOG_LEVEL_NAMES,
  isAtLeastLevel,
  logLevelRank,
  normalizeLogLevel,
  parseLogLevelName,
} from "../../src/logs/log-level.js";

describe("normalizeLogLevel（级别名归一化）", () => {
  it("服务端日志级别名 → 桥侧字面量（Information → info）", () => {
    expect(normalizeLogLevel("Debug")).toBe("debug");
    expect(normalizeLogLevel("Information")).toBe("info");
    expect(normalizeLogLevel("Warning")).toBe("warning");
    expect(normalizeLogLevel("Error")).toBe("error");
    expect(normalizeLogLevel("Fatal")).toBe("fatal");
  });

  it("大小写不敏感 + 去空白", () => {
    expect(normalizeLogLevel("WARNING")).toBe("warning");
    expect(normalizeLogLevel("  Error ")).toBe("error");
    expect(normalizeLogLevel("error")).toBe("error");
  });

  it("未知 / 空 → 空串（视为无有效级别）", () => {
    expect(normalizeLogLevel("banana")).toBe("");
    expect(normalizeLogLevel("")).toBe("");
    expect(normalizeLogLevel("   ")).toBe("");
  });
});

describe("parseLogLevelName（查询侧严格值域）", () => {
  it("合法值域 = LOG_LEVEL_NAMES（大小写不敏感，归一为小写）", () => {
    expect(LOG_LEVEL_NAMES).toEqual(["fatal", "error", "warning", "message", "info", "debug"]);
    expect(parseLogLevelName("warning")).toBe("warning");
    expect(parseLogLevelName("Warning")).toBe("warning");
    expect(parseLogLevelName("WARNING")).toBe("warning");
    expect(parseLogLevelName("  Fatal ")).toBe("fatal");
    expect(parseLogLevelName("info")).toBe("info");
  });

  it("服务端字面量 Information / Critical 在查询侧不合法（桥侧不识别 → 必须拦下）", () => {
    expect(parseLogLevelName("Information")).toBeNull();
    expect(parseLogLevelName("Critical")).toBeNull();
    expect(parseLogLevelName("banana")).toBeNull();
    expect(parseLogLevelName("")).toBeNull();
    expect(parseLogLevelName("   ")).toBeNull();
  });

  it("与宽松归一化的分工：normalizeLogLevel 接受 Information，parseLogLevelName 拒绝", () => {
    expect(normalizeLogLevel("Information")).toBe("info");
    expect(parseLogLevelName("Information")).toBeNull();
  });
});

describe("logLevelRank（严重度排名）", () => {
  it("fatal 最严重（0）→ debug 最轻（5）", () => {
    expect(logLevelRank("fatal")).toBe(0);
    expect(logLevelRank("error")).toBe(1);
    expect(logLevelRank("warning")).toBe(2);
    expect(logLevelRank("message")).toBe(3);
    expect(logLevelRank("info")).toBe(4);
    expect(logLevelRank("debug")).toBe(5);
  });

  it("大小写不敏感（走归一化）", () => {
    expect(logLevelRank("Error")).toBe(1);
    expect(logLevelRank("Information")).toBe(4);
  });

  it("未知 / 空级别 → 永不满足任何阈值（最大排名）", () => {
    expect(logLevelRank("banana")).toBe(Number.MAX_SAFE_INTEGER);
    expect(logLevelRank("")).toBe(Number.MAX_SAFE_INTEGER);
  });
});

describe("isAtLeastLevel（阈值判定）", () => {
  it("默认采集阈值 = warning（Warning/Error/Fatal 纳入，Information/Debug 不纳入）", () => {
    expect(DEFAULT_LOG_MIN_LEVEL).toBe("warning");
    expect(isAtLeastLevel("warning", DEFAULT_LOG_MIN_LEVEL)).toBe(true);
    expect(isAtLeastLevel("Error", DEFAULT_LOG_MIN_LEVEL)).toBe(true);
    expect(isAtLeastLevel("fatal", DEFAULT_LOG_MIN_LEVEL)).toBe(true);
    expect(isAtLeastLevel("Information", DEFAULT_LOG_MIN_LEVEL)).toBe(false);
    expect(isAtLeastLevel("Debug", DEFAULT_LOG_MIN_LEVEL)).toBe(false);
  });

  it("阈值为空 / 未知 → 不过滤（一律纳入）", () => {
    expect(isAtLeastLevel("debug", "")).toBe(true);
    expect(isAtLeastLevel("banana", "")).toBe(true);
    expect(isAtLeastLevel("info", "banana")).toBe(true);
  });

  it("未知条目级别 → 不纳入（无法证明达到阈值）", () => {
    expect(isAtLeastLevel("banana", "warning")).toBe(false);
  });
});
