// =============================================================================
// mcp-kit 信封（canonical）形状断言
//
// 决策 D4：err 的规范字段为 message（+ 可选 hint / details）。
// 注意：OK 信封仍使用 summary —— 只有错误路径的字段名从 summary 收敛到 message。
// =============================================================================

import { describe, it, expect } from "vitest";
import { errEnv, okEnv } from "../src/envelope.js";

describe("okEnv", () => {
  it("构造 canonical 成功信封（保留 summary 字段，data 键显式存在）", () => {
    const env = okEnv("spt_health", "运行时布局正常");
    expect(env.ok).toBe(true);
    expect(env.tool).toBe("spt_health");
    expect(env.summary).toBe("运行时布局正常");
    // toEqual 对键存在性不敏感（data: undefined 与缺键等价），
    // 故用 hasOwnProperty 显式锁死 data 键的存在。
    expect(Object.prototype.hasOwnProperty.call(env, "data")).toBe(true);
    expect(env.data).toBeUndefined();
  });

  it("携带 data 载荷", () => {
    const env = okEnv("t", "s", { count: 3 });
    expect(env.ok).toBe(true);
    expect(env.data).toEqual({ count: 3 });
  });
});

describe("errEnv", () => {
  it("构造 canonical 错误信封：字段名为 message（不是 summary）", () => {
    const env = errEnv("spt_list_mods", "无效输入", "invalid_input");
    expect(env).toEqual({
      ok: false,
      tool: "spt_list_mods",
      code: "invalid_input",
      message: "无效输入",
    });
    // 错误路径不得再带 summary（已声明 wire delta）
    expect((env as Record<string, unknown>).summary).toBeUndefined();
  });

  it("携带 hint（修复提示）", () => {
    const env = errEnv("t", "m", "c", { hint: "可用工具：a, b" });
    expect(env.hint).toBe("可用工具：a, b");
    expect(env.details).toBeUndefined();
  });

  it("携带 details（结构化细节）", () => {
    const env = errEnv("t", "m", "c", { details: { expected: "5.0.0", actual: "4.1.5" } });
    expect(env.details).toEqual({ expected: "5.0.0", actual: "4.1.5" });
    expect(env.hint).toBeUndefined();
  });

  it("hint + details 可同时携带", () => {
    const env = errEnv("t", "m", "c", { hint: "h", details: { a: 1 } });
    expect(env.hint).toBe("h");
    expect(env.details).toEqual({ a: 1 });
  });

  it("未提供 extra 时不落 hint / details 键", () => {
    const env = errEnv("t", "m", "c");
    expect(Object.prototype.hasOwnProperty.call(env, "hint")).toBe(false);
    expect(Object.prototype.hasOwnProperty.call(env, "details")).toBe(false);
  });
});
