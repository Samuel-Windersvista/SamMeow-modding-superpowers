// 谓词引擎模块契约测试：解析（最小语法）与求值（外部可观察结果）。

import { describe, expect, it } from "vitest";

import {
  evaluatePredicate,
  parsePredicate,
  type ParsedPredicate,
} from "../../src/predicate/predicate.js";

/** 解析成功时取出谓词；失败时直接抛断言错误 */
function parsed(raw: string): ParsedPredicate {
  const result = parsePredicate(raw);
  if (!result.ok) {
    throw new Error(`期望解析成功，实际失败：${result.reason}`);
  }
  return result.predicate;
}

describe("parsePredicate", () => {
  it("解析 equals：路径 / 运算符 / 值", () => {
    const predicate = parsed("version.core equals 5.0.0");
    expect(predicate).toMatchObject({ path: "version.core", op: "equals", value: "5.0.0" });
  });

  it("解析 contains：值可含空格（取运算符后的剩余文本）", () => {
    const predicate = parsed("capabilities.sections contains server_status");
    expect(predicate).toMatchObject({
      path: "capabilities.sections",
      op: "contains",
      value: "server_status",
    });
  });

  it("解析 matches：值保留正则原文（含空格与反斜杠）", () => {
    const predicate = parsed("version.raw matches ^SPT 5\\.0");
    expect(predicate).toMatchObject({ path: "version.raw", op: "matches", value: "^SPT 5\\.0" });
  });

  it("解析数值比较：四个运算符", () => {
    for (const op of [">", ">=", "<", "<="] as const) {
      const predicate = parsed(`connection.port ${op} 6000`);
      expect(predicate).toMatchObject({ path: "connection.port", op, value: "6000" });
    }
  });

  it("缺运算符或值：结构化失败（不抛异常）", () => {
    expect(parsePredicate("garbage").ok).toBe(false);
    expect(parsePredicate("a.b").ok).toBe(false);
    expect(parsePredicate("a.b equals").ok).toBe(false);
  });

  it("未知运算符：结构化失败", () => {
    const result = parsePredicate("a.b ~ c");
    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.reason).toContain("~");
  });

  it("非法正则：结构化失败", () => {
    expect(parsePredicate("a.b matches [").ok).toBe(false);
  });

  it("数值比较的右值非数值：结构化失败", () => {
    expect(parsePredicate("a.b > abc").ok).toBe(false);
  });

  it("非法路径段：结构化失败", () => {
    expect(parsePredicate(".a equals b").ok).toBe(false);
    expect(parsePredicate("a..b equals c").ok).toBe(false);
    expect(parsePredicate("a b.c equals d").ok).toBe(false);
  });
});

describe("evaluatePredicate", () => {
  const root = {
    version: { core: "5.0.0", raw: "SPT 5.0.0 (BEM) ff0bf32", channel: "BEM" },
    capabilities: { sections: ["server_status", "instances"] },
    connection: { port: 6969, count: 2 },
  };

  it("equals：字符串相等", () => {
    expect(evaluatePredicate(parsed("version.core equals 5.0.0"), root)).toMatchObject({
      satisfied: true,
      pathFound: true,
      actual: "5.0.0",
    });
    expect(evaluatePredicate(parsed("version.core equals 4.1.5"), root).satisfied).toBe(false);
  });

  it("equals：数值与布尔值宽松比较", () => {
    expect(evaluatePredicate(parsed("connection.count equals 2"), root).satisfied).toBe(true);
    expect(evaluatePredicate(parsed("connection.count equals 3"), root).satisfied).toBe(false);
  });

  it("contains：数组元素命中", () => {
    expect(
      evaluatePredicate(parsed("capabilities.sections contains server_status"), root).satisfied,
    ).toBe(true);
    expect(
      evaluatePredicate(parsed("capabilities.sections contains traders"), root).satisfied,
    ).toBe(false);
  });

  it("contains：字符串子串命中", () => {
    expect(evaluatePredicate(parsed("version.raw contains (BEM)"), root).satisfied).toBe(true);
    expect(evaluatePredicate(parsed("version.raw contains 4.1"), root).satisfied).toBe(false);
  });

  it("matches：正则命中", () => {
    expect(evaluatePredicate(parsed("version.raw matches ^SPT 5\\.0"), root).satisfied).toBe(true);
    expect(evaluatePredicate(parsed("version.raw matches ^SPT 4\\."), root).satisfied).toBe(false);
  });

  it("数值比较：四个运算符", () => {
    expect(evaluatePredicate(parsed("connection.port > 6000"), root).satisfied).toBe(true);
    expect(evaluatePredicate(parsed("connection.port >= 6969"), root).satisfied).toBe(true);
    expect(evaluatePredicate(parsed("connection.port < 6000"), root).satisfied).toBe(false);
    expect(evaluatePredicate(parsed("connection.port <= 6969"), root).satisfied).toBe(true);
  });

  it("数值比较：字段非数值时不满足且不抛异常", () => {
    const evaluation = evaluatePredicate(parsed("version.core > 1"), root);
    expect(evaluation).toMatchObject({ satisfied: false, pathFound: true, actual: "5.0.0" });
  });

  it("路径缺失：pathFound=false 且不满足", () => {
    expect(evaluatePredicate(parsed("nope.missing equals x"), root)).toMatchObject({
      satisfied: false,
      pathFound: false,
      actual: undefined,
    });
  });

  it("数组下标路径可用", () => {
    expect(
      evaluatePredicate(parsed("capabilities.sections.1 equals instances"), root).satisfied,
    ).toBe(true);
  });
});
