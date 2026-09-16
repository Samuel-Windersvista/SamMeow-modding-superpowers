// =============================================================================
// since 游标解析与本地过滤（桥侧 `since` 语义的 TS 侧等价物）
//
// `/logs/summary` 的 since 接受两种形态：端点自己输出的 ISO 8601（`lastTs`
// 原样回填即可往返）或整数 UTC Ticks；缺省 / 非法即不过滤。
// =============================================================================

import { describe, expect, it } from "vitest";

import { filterGroupsSince, isoToEpochMs, parseSinceToEpochMs } from "../../src/logs/log-since.js";

/** C# `DateTime.Ticks`（100ns，UTC）→ epoch ms */
function ticksOf(iso: string): number {
  return (Date.parse(iso) + 62_135_596_800_000) * 10_000;
}

describe("parseSinceToEpochMs", () => {
  it("缺省 → null（不过滤）", () => {
    expect(parseSinceToEpochMs(undefined)).toBeNull();
  });

  it("ISO 8601 字符串（端点输出原样回填）→ epoch ms", () => {
    expect(parseSinceToEpochMs("2026-09-15T10:00:02.000Z")).toBe(
      Date.parse("2026-09-15T10:00:02.000Z"),
    );
  });

  it("C# 风格 7 位小数 ISO 8601 → epoch ms", () => {
    expect(parseSinceToEpochMs("2026-09-15T10:00:02.1234567Z")).toBe(
      Date.parse("2026-09-15T10:00:02.123Z"),
    );
  });

  it("整数 UTC Ticks → epoch ms", () => {
    const iso = "2026-09-15T10:00:02.000Z";
    expect(parseSinceToEpochMs(ticksOf(iso))).toBe(Date.parse(iso));
  });

  it("非正数 Ticks / 非法字符串 → null（桥侧宽松解析：视为不过滤）", () => {
    expect(parseSinceToEpochMs(0)).toBeNull();
    expect(parseSinceToEpochMs(-5)).toBeNull();
    expect(parseSinceToEpochMs("banana")).toBeNull();
    expect(parseSinceToEpochMs("")).toBeNull();
  });
});

describe("isoToEpochMs", () => {
  it("解析 3 位与 7 位小数 ISO（7 位截断到毫秒）", () => {
    expect(isoToEpochMs("2026-09-15T10:00:02.000Z")).toBe(Date.parse("2026-09-15T10:00:02.000Z"));
    expect(isoToEpochMs("2026-09-15T10:00:02.1234567Z")).toBe(
      Date.parse("2026-09-15T10:00:02.123Z"),
    );
  });

  it("非法 → null", () => {
    expect(isoToEpochMs("not-a-date")).toBeNull();
  });
});

describe("filterGroupsSince（since 独占）", () => {
  const groups = [
    { key: "a", lastTs: "2026-09-15T10:00:00.000Z" },
    { key: "b", lastTs: "2026-09-15T10:00:05.000Z" },
    { key: "c", lastTs: "2026-09-15T10:00:10.000Z" },
  ];

  it("只保留 lastTs 晚于 since 的组（严格大于）", () => {
    const filtered = filterGroupsSince(groups, Date.parse("2026-09-15T10:00:05.000Z"));

    expect(filtered.map((group) => group.key)).toEqual(["c"]);
  });

  it("since 为 null → 原样保留（不过滤）", () => {
    expect(filterGroupsSince(groups, null).map((group) => group.key)).toEqual(["a", "b", "c"]);
  });

  it("返回新数组（不修改入参）", () => {
    const filtered = filterGroupsSince(groups, Date.parse("2026-09-15T10:00:00.000Z"));

    expect(filtered).not.toBe(groups);
    expect(groups).toHaveLength(3);
  });

  it("lastTs 非法的组在过滤时视为最旧（被 since 排除）", () => {
    const filtered = filterGroupsSince(
      [{ key: "bad", lastTs: "not-a-date" }],
      Date.parse("2026-09-15T10:00:00.000Z"),
    );

    expect(filtered).toEqual([]);
  });
});
