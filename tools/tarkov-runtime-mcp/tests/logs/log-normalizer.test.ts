// =============================================================================
// 日志文本归一化：与桥侧 `LogSummaryNormalizer` 同构的 TS 移植
//
// 只测外部行为：给定文本，断言聚合键。保守策略——宁可少合并，不可误合并。
// =============================================================================

import { describe, expect, it } from "vitest";

import { normalizeLogText } from "../../src/logs/log-normalizer.js";

describe("normalizeLogText（归一化聚合键）", () => {
  it("GUID → <guid>", () => {
    expect(
      normalizeLogText("Profile 9f2c1e5a-1b2c-4d5e-8f90-123456789abc loaded"),
    ).toBe("Profile <guid> loaded");
  });

  it("恰好 24 位 hex（词边界包围）→ <id>", () => {
    expect(normalizeLogText("Fixed item: 6aa409923c427c1424103c2b undefined")).toBe(
      "Fixed item: <id> undefined",
    );
  });

  it("真实样本：24hex 紧贴字面量 s（`…2bs`）也归一为 <id>", () => {
    // 2026-09-16 采样：SPT 打印 `Fixed item: <24hex>s undefined StackObjectsCount value, now set to 1`
    expect(
      normalizeLogText(
        "Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1",
      ),
    ).toBe("Fixed item: <id>s undefined StackObjectsCount value, now set to <n>");
  });

  it("同错误不同实例（24hex 每次不同）→ 同一聚合键", () => {
    const first = normalizeLogText(
      "Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1",
    );
    const second = normalizeLogText(
      "Fixed item: 5b1c0d7e9f3a2b4c6d8e0f1as undefined StackObjectsCount value, now set to 1",
    );

    expect(first).toBe(second);
  });

  it("0x 十六进制字面量（文件 / IL 偏移）→ <hex>", () => {
    expect(normalizeLogText("crash at 0xc0000005 in module 0X1A2B")).toBe(
      "crash at <hex> in module <hex>",
    );
  });

  it("十进制数字（含小数）→ <n>", () => {
    expect(normalizeLogText("took 12.5 ms over 300 frames")).toBe("took <n> ms over <n> frames");
  });

  it("连续空白折叠为单空格并去首尾空白", () => {
    expect(normalizeLogText("  boom\t\t at\nline  ")).toBe("boom at line");
  });

  it("不折叠大小写（Error 与 error 不同键）", () => {
    expect(normalizeLogText("Error: boom")).not.toBe(normalizeLogText("error: boom"));
  });

  it("幂等：对已归一化文本再次调用结果不变", () => {
    const once = normalizeLogText(
      "Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1",
    );

    expect(normalizeLogText(once)).toBe(once);
  });

  it("空 / 纯空白 → 空键", () => {
    expect(normalizeLogText("")).toBe("");
    expect(normalizeLogText("   \n\t ")).toBe("");
  });

  it("不误合并：更长 hex 串（25/32 位）与内嵌数字不动", () => {
    const hex25 = normalizeLogText("id 6aa409923c427c1424103c2bcd end");
    const hex32 = normalizeLogText("hash 6aa409923c427c1424103c2b6aa409923c427c1 end");
    const embedded = normalizeLogText("prefix6aa409923c427c1424103c2b suffix");
    const digitsInWord = normalizeLogText("foo123bar baz");

    expect(hex25).toBe("id 6aa409923c427c1424103c2bcd end");
    expect(hex32).toBe("hash 6aa409923c427c1424103c2b6aa409923c427c1 end");
    expect(embedded).toBe("prefix6aa409923c427c1424103c2b suffix");
    expect(digitsInWord).toBe("foo123bar baz");
  });

  it("不误合并：形近但语义不同的告警保持不同键", () => {
    const missing = normalizeLogText("Fixed item: 6aa409923c427c1424103c2bs missing StackObjectsCount");
    const undefinedValue = normalizeLogText(
      "Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount",
    );

    expect(missing).not.toBe(undefinedValue);
  });
});
