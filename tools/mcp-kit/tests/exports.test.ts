// =============================================================================
// exports.test.ts — kit 公共导出面快照
//
// kit 导出面在 C2 wave1 车道 A 完成后冻结：三台 MCP 经相对 dist 导入依赖它，
// 任何增删导出都需另立工单。本测试锁死运行时值导出集合（8 项）。
//
// 仅覆盖运行时值导出；类型导出（Envelope / ErrEnvelope / OkEnvelope /
// ToolResult / McpToolDefinition / StdioServerOptions）编译后不存在，不在快照内。
// =============================================================================

import { describe, it, expect } from "vitest";

/** 冻结的运行时导出清单（8 项，字母序） */
const EXPECTED_RUNTIME_EXPORTS = [
  "errEnv",
  "jsonResult",
  "jsonText",
  "normalizeMcpInputSchema",
  "okEnv",
  "runMain",
  "runStdioServer",
  "schemaFor",
].sort();

describe("kit 公共导出面（快照）", () => {
  it("运行时导出集合严格等于冻结清单", async () => {
    const mod = await import("../src/index.js");
    expect(Object.keys(mod).sort()).toEqual(EXPECTED_RUNTIME_EXPORTS);
  });
});
