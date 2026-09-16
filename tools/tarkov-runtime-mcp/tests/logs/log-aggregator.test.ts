// =============================================================================
// MCP 侧日志聚合器：与桥侧 `LogSummaryStore` 同构（归一化去重 + 组上限淘汰）
//
// 只测外部行为：observe 若干条目后断言 snapshot 的组视图。
// =============================================================================

import { describe, expect, it } from "vitest";

import { LogSummaryAggregator } from "../../src/logs/log-aggregator.js";

/** 真实告警样本（24hex 每次不同） */
function fixedItemEntry(id: string, ts: string) {
  return {
    ts,
    level: "warning",
    source: "server:spt20260914.log",
    text: `Fixed item: ${id}s undefined StackObjectsCount value, now set to 1`,
  };
}

describe("LogSummaryAggregator（归一化去重聚合）", () => {
  it("同一错误的不同实例合并为 1 组（count 累计、sampleText 取首个原文）", () => {
    const aggregator = new LogSummaryAggregator();

    aggregator.observe(fixedItemEntry("6aa409923c427c1424103c2b", "2026-09-14T05:58:00.159Z"));
    aggregator.observe(fixedItemEntry("5b1c0d7e9f3a2b4c6d8e0f1a", "2026-09-14T05:58:01.000Z"));

    const { groups, overflowDropped } = aggregator.snapshot(null);

    expect(overflowDropped).toBe(0);
    expect(groups).toHaveLength(1);
    expect(groups[0]).toEqual({
      key: "Fixed item: <id>s undefined StackObjectsCount value, now set to <n>",
      level: "warning",
      source: "server:spt20260914.log",
      count: 2,
      firstTs: "2026-09-14T05:58:00.159Z",
      lastTs: "2026-09-14T05:58:01.000Z",
      sampleText:
        "Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1",
    });
  });

  it("不同错误不合并（键不同 → 不同组）", () => {
    const aggregator = new LogSummaryAggregator();

    aggregator.observe(fixedItemEntry("6aa409923c427c1424103c2b", "2026-09-14T05:58:00.159Z"));
    aggregator.observe({
      ts: "2026-09-14T05:58:02.000Z",
      level: "error",
      source: "server:spt20260914.log",
      text: "KeyNotFoundException: loot patch",
    });

    expect(aggregator.snapshot(null).groups).toHaveLength(2);
  });

  it("level 升级为组内最高严重度（组内出现 Error 时不再标 Warning）", () => {
    const aggregator = new LogSummaryAggregator();

    aggregator.observe({ ts: "2026-09-14T05:58:00.000Z", level: "warning", source: "s", text: "boom" });
    aggregator.observe({ ts: "2026-09-14T05:58:01.000Z", level: "error", source: "s", text: "boom" });
    aggregator.observe({ ts: "2026-09-14T05:58:02.000Z", level: "info", source: "s", text: "boom" });

    expect(aggregator.snapshot(null).groups[0].level).toBe("error");
  });

  it("firstTs / lastTs 取极值（多线程到达顺序不保证单调）", () => {
    const aggregator = new LogSummaryAggregator();

    aggregator.observe({ ts: "2026-09-14T05:58:05.000Z", level: "warning", source: "s", text: "boom" });
    aggregator.observe({ ts: "2026-09-14T05:58:01.000Z", level: "warning", source: "s", text: "boom" });
    aggregator.observe({ ts: "2026-09-14T05:58:09.000Z", level: "warning", source: "s", text: "boom" });

    const group = aggregator.snapshot(null).groups[0];
    expect(group.firstTs).toBe("2026-09-14T05:58:01.000Z");
    expect(group.lastTs).toBe("2026-09-14T05:58:09.000Z");
  });

  it("source 取首个观测来源", () => {
    const aggregator = new LogSummaryAggregator();

    aggregator.observe({ ts: "2026-09-14T05:58:00.000Z", level: "warning", source: "server:a.log", text: "boom" });
    aggregator.observe({ ts: "2026-09-14T05:58:01.000Z", level: "warning", source: "server:b.log", text: "boom" });

    expect(aggregator.snapshot(null).groups[0].source).toBe("server:a.log");
  });

  it("组数达上限后淘汰最久未更新（lastTs 最小）的组并累计 overflowDropped", () => {
    const aggregator = new LogSummaryAggregator(2);

    aggregator.observe({ ts: "2026-09-14T05:58:00.000Z", level: "warning", source: "s", text: "one" });
    aggregator.observe({ ts: "2026-09-14T05:58:01.000Z", level: "warning", source: "s", text: "two" });
    // 更新 one（lastTs 变为最新），使 two 成为最久未更新者
    aggregator.observe({ ts: "2026-09-14T05:58:05.000Z", level: "warning", source: "s", text: "one" });
    aggregator.observe({ ts: "2026-09-14T05:58:06.000Z", level: "warning", source: "s", text: "three" });

    const { groups, overflowDropped } = aggregator.snapshot(null);

    expect(overflowDropped).toBe(1);
    expect(groups.map((group) => group.key).sort()).toEqual(["one", "three"]);
    expect(groups.find((group) => group.key === "one")?.count).toBe(2);
  });

  it("淘汰并列（lastTs 相同）时取 key 序最小者（确定性）", () => {
    const aggregator = new LogSummaryAggregator(2);

    aggregator.observe({ ts: "2026-09-14T05:58:00.000Z", level: "warning", source: "s", text: "bbb" });
    aggregator.observe({ ts: "2026-09-14T05:58:00.000Z", level: "warning", source: "s", text: "aaa" });
    aggregator.observe({ ts: "2026-09-14T05:58:07.000Z", level: "warning", source: "s", text: "ccc" });

    expect(aggregator.snapshot(null).groups.map((group) => group.key)).toEqual(["ccc", "bbb"]);
  });

  it("count 不受组上限影响（同键重复观测始终累计）", () => {
    const aggregator = new LogSummaryAggregator(1);

    for (let index = 0; index < 50; index += 1) {
      aggregator.observe({
        ts: "2026-09-14T05:58:00.000Z",
        level: "warning",
        source: "s",
        text: `boom ${index}`,
      });
    }

    expect(aggregator.snapshot(null).groups[0].count).toBe(50);
  });

  it("snapshot since：只回 lastTs 严格晚于 since 的组", () => {
    const aggregator = new LogSummaryAggregator();

    aggregator.observe({ ts: "2026-09-14T05:58:00.000Z", level: "warning", source: "s", text: "old" });
    aggregator.observe({ ts: "2026-09-14T05:58:10.000Z", level: "warning", source: "s", text: "new" });

    const since = Date.parse("2026-09-14T05:58:00.000Z");
    expect(aggregator.snapshot(since).groups.map((group) => group.key)).toEqual(["new"]);
    expect(aggregator.snapshot(null).groups).toHaveLength(2);
  });

  it("组序固定：count 降序 → lastTs 降序 → key 序升序", () => {
    const aggregator = new LogSummaryAggregator();

    // count 降序优先
    aggregator.observe({ ts: "2026-09-14T05:58:00.000Z", level: "warning", source: "s", text: "many" });
    aggregator.observe({ ts: "2026-09-14T05:58:01.000Z", level: "warning", source: "s", text: "many" });
    aggregator.observe({ ts: "2026-09-14T05:59:00.000Z", level: "warning", source: "s", text: "few-later" });
    // count 并列时 lastTs 降序
    aggregator.observe({ ts: "2026-09-14T05:58:30.000Z", level: "warning", source: "s", text: "few-earlier" });
    // count 与 lastTs 均并列时 key 升序
    aggregator.observe({ ts: "2026-09-14T05:58:30.000Z", level: "warning", source: "s", text: "aaa" });

    expect(aggregator.snapshot(null).groups.map((group) => group.key)).toEqual([
      "many",
      "few-later",
      "aaa",
      "few-earlier",
    ]);
  });

  it("snapshot 返回不可变副本（后续 observe 不影响已取快照）", () => {
    const aggregator = new LogSummaryAggregator();

    aggregator.observe({ ts: "2026-09-14T05:58:00.000Z", level: "warning", source: "s", text: "boom" });
    const first = aggregator.snapshot(null);
    aggregator.observe({ ts: "2026-09-14T05:58:01.000Z", level: "warning", source: "s", text: "boom" });

    expect(first.groups[0].count).toBe(1);
    expect(aggregator.snapshot(null).groups[0].count).toBe(2);
  });
});
