// =============================================================================
// 会话获取（免会话 launcher 路由）
//
// SPT server 的 session 受限路由（`/client/game/profile/list`、`traderSettings`、
// `quest/list` 等）要求 `PHPSESSID` cookie 值等于目标 profile 的 profileId
// （源码证据：`HttpServer.cs` 直接以 cookie 值构造 MongoId；
// `LauncherV2Controller.GetSessionId` 表明 sessionId 即 profile id）。
//
// 获取流程（全部走免会话的 `/launcher/v2/*` 路由，响应为 `{Response: ...}` 信封）：
//   1. 若配置了 password，先 POST `/launcher/v2/login` 校验凭据；
//   2. POST `/launcher/v2/profiles`，按 username 匹配 profile，取 profileId；
//   3. 把 profileId 作为后续全部请求的 PHPSESSID 注入连接。
//
// 未配置 username / 匹配失败 / 凭据失败一律抛结构化错误，绝不静默返回空数据。
// =============================================================================

import { SptRuntimeError } from "../errors.js";
import { isRecord, readString, unwrapLauncherEnvelope } from "../snapshot/util.js";
import type { SptConnection } from "../transport/connection.js";
import { RUNTIME_ERROR_CODES } from "../types.js";

/** 免会话的 profile 列表路由 */
export const PROFILES_ROUTE = "/launcher/v2/profiles";

/** 免会话的登录校验路由 */
export const LOGIN_ROUTE = "/launcher/v2/login";

export interface SessionCredentials {
  /** 目标 profile 的 username；未配置时无法建立会话 */
  username?: string;
  /** 可选密码；配置后先经 `/launcher/v2/login` 校验 */
  password?: string;
}

export interface AcquiredSession {
  username: string;
  /** profileId，即后续请求的 PHPSESSID 值 */
  profileId: string;
}

/** 从 `{Response: [...]}` 的 profile 列表匹配 username（精确优先，大小写不敏感兜底） */
function matchProfileId(response: unknown, username: string): string | null {
  const entries = Array.isArray(response) ? response : [];
  const records = entries.filter(isRecord);
  const exact = records.find((entry) => readString(entry.username) === username);
  const matched =
    exact ?? records.find((entry) => readString(entry.username).toLowerCase() === username.toLowerCase());
  if (!matched) {
    return null;
  }
  const profileId = readString(matched.profileId);
  return profileId || null;
}

/** 登录校验：`Response` 为 falsy（false/null/缺失）即视为失败 */
async function validateLogin(
  connection: SptConnection,
  username: string,
  password: string,
): Promise<void> {
  const response = await connection.request({
    method: "POST",
    path: LOGIN_ROUTE,
    body: { username, password },
  });
  const result = unwrapLauncherEnvelope(response.body);
  if (!result) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.AUTH_FAILED,
      `凭据校验失败（username=${username}）`,
      { username, route: LOGIN_ROUTE },
    );
  }
}

/**
 * 对给定连接执行会话获取：解析 profileId 并注入 PHPSESSID。
 * 失败抛结构化 SptRuntimeError（SESSION_NOT_CONFIGURED / AUTH_FAILED / PROFILE_NOT_FOUND）。
 */
export async function acquireSession(
  connection: SptConnection,
  credentials: SessionCredentials,
): Promise<AcquiredSession> {
  const username = credentials.username?.trim();
  if (!username) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.SESSION_NOT_CONFIGURED,
      "未配置 profile username（环境变量 TARKOV_RUNTIME_MCP_USERNAME），无法建立会话",
      { env: "TARKOV_RUNTIME_MCP_USERNAME" },
    );
  }

  const password = credentials.password?.trim();
  if (password) {
    await validateLogin(connection, username, password);
  }

  const response = await connection.request({ method: "POST", path: PROFILES_ROUTE, body: {} });
  const profileId = matchProfileId(unwrapLauncherEnvelope(response.body), username);
  if (!profileId) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.PROFILE_NOT_FOUND,
      `未在 ${PROFILES_ROUTE} 中找到 username=${username} 的 profile`,
      { username, route: PROFILES_ROUTE },
    );
  }

  connection.setSessionId(profileId);
  return { username, profileId };
}
