// =============================================================================
// PHPSESSID cookie 会话
//
// SPT server 从请求 cookie 读取 PHPSESSID 构造 session（HttpServer.cs:35-37），
// 并在响应 Set-Cookie 回写。MCP 作为外部客户端需持有该会话。
// =============================================================================

export const SESSION_COOKIE_NAME = "PHPSESSID";

/** 从 Set-Cookie 响应头提取 PHPSESSID（支持单值或多值） */
export function extractSessionId(
  setCookie: string | string[] | undefined,
): string | null {
  if (!setCookie) {
    return null;
  }
  const values = Array.isArray(setCookie) ? setCookie : [setCookie];
  for (const value of values) {
    const match = new RegExp(`(?:^|;\\s*)${SESSION_COOKIE_NAME}=([^;]+)`).exec(value);
    if (match) {
      return decodeURIComponent(match[1]);
    }
  }
  return null;
}

/** 构造请求用 Cookie 头 */
export function buildCookieHeader(sessionId: string): string {
  return `${SESSION_COOKIE_NAME}=${sessionId}`;
}
