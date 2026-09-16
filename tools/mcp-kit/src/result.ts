// =============================================================================
// mcp-kit/result.ts — MCP 工具调用结果包装
//
// 信封（Envelope）是业务载荷；ToolResult 是 MCP wire 上的 CallTool 返回形状。
// 两者刻意分离：信封可被单测直接断言，wire 形状只在 bootstrap 层组装。
// =============================================================================

export interface ToolResult {
  content: Array<{ type: "text"; text: string }>;
  isError?: boolean;
}

/** 把任意值序列化为单个 text content block */
export function jsonText(value: unknown): { type: "text"; text: string } {
  return { type: "text", text: JSON.stringify(value) };
}

/** 把任意载荷包装为 MCP ToolResult；isError 缺省 false */
export function jsonResult(body: unknown, isError = false): ToolResult {
  return {
    content: [jsonText(body)],
    isError,
  };
}
