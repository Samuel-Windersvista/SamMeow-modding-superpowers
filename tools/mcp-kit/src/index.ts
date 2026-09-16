// =============================================================================
// mcp-kit — MCP 共享内核公共导出面（C2）
//
// 冻结说明：本导出面在 C2 wave1 车道 A 完成后即冻结；三台 MCP
// （mo2-mcp / spt-mcp / tarkov-runtime-mcp）通过相对 dist 导入使用：
//   import { ... } from "../../mcp-kit/dist/index.js";
// 任何 API 变更需另立工单。
// =============================================================================

export { normalizeMcpInputSchema, schemaFor } from "./schema.js";

export { errEnv, okEnv } from "./envelope.js";
export type { Envelope, ErrEnvelope, OkEnvelope } from "./envelope.js";

export { jsonResult, jsonText } from "./result.js";
export type { ToolResult } from "./result.js";

export { runMain, runStdioServer } from "./bootstrap.js";
export type { McpToolDefinition, StdioServerOptions } from "./bootstrap.js";
