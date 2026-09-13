// =============================================================================
// 连接握手
//
// 对一个 SptConnection 执行：GET 版本端点 -> 解析标签 -> BEM tag 门禁。
// 失败抛结构化 SptRuntimeError（SERVER_UNREACHABLE / VERSION_MISMATCH）。
// 测试以 fake SptConnection 驱动（S1 接缝）。
// =============================================================================

import { SptRuntimeError } from "../errors.js";
import { RUNTIME_ERROR_CODES, type HandshakeResult } from "../types.js";
import type { SptConnection, SptResponse } from "../transport/connection.js";
import { VERSION_ENDPOINT, checkVersionGate, parseAnchor, parseVersionLabel } from "./version.js";

/** 从版本端点响应体提取标签（兼容 PascalCase / camelCase） */
export function extractVersionLabel(body: unknown): string | null {
  if (typeof body === "string") {
    return body.trim() || null;
  }
  if (body && typeof body === "object") {
    const record = body as Record<string, unknown>;
    const value = record.Version ?? record.version;
    if (typeof value === "string") {
      return value;
    }
  }
  return null;
}

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

export async function performHandshake(
  connection: SptConnection,
  anchorVersion: string,
): Promise<HandshakeResult> {
  const anchor = parseAnchor(anchorVersion);
  if (!anchor) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.INTERNAL_ERROR,
      `锚定版本常量非法：${anchorVersion}`,
    );
  }

  let response: SptResponse;
  try {
    response = await connection.request({ method: "GET", path: VERSION_ENDPOINT });
  } catch (error) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.SERVER_UNREACHABLE,
      `无法连接 SPT server：${connection.baseUrl}`,
      { baseUrl: connection.baseUrl, cause: errorMessage(error) },
    );
  }

  if (response.status < 200 || response.status >= 300) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.SERVER_UNREACHABLE,
      `SPT server 返回 HTTP ${response.status}：${connection.baseUrl}`,
      { baseUrl: connection.baseUrl, status: response.status },
    );
  }

  const label = extractVersionLabel(response.body);
  const observed = label ? parseVersionLabel(label) : null;
  if (!observed) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.VERSION_MISMATCH,
      `无法从 ${VERSION_ENDPOINT} 解析 SPT 版本标签`,
      { expected: anchor.raw, actual: label ?? response.text },
    );
  }

  const gate = checkVersionGate(observed, anchor);
  if (!gate.passed) {
    throw new SptRuntimeError(RUNTIME_ERROR_CODES.VERSION_MISMATCH, gate.reason, {
      expected: gate.expected,
      actual: gate.actual,
    });
  }

  return {
    host: connection.host,
    port: connection.port,
    baseUrl: connection.baseUrl,
    version: observed,
    anchor,
    gate,
  };
}
