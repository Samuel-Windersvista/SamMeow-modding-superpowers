// =============================================================================
// 会话获取（免会话 launcher 路由 + 可选活跃探针）
//
// SPT server 的 session 受限路由（`/client/game/profile/list`、`traderSettings`、
// `quest/list` 等）要求 `PHPSESSID` cookie 值等于目标 profile 的 profileId
// （源码证据：`HttpServer.cs` 直接以 cookie 值构造 MongoId；
// `LauncherV2Controller.GetSessionId` 表明 sessionId 即 profile id）。
//
// 选择优先级（ADR-0004，ticket 08）：
//   0. 活跃探针 `/spt/runtime/active-profiles` 可用且恰有 1 个活跃 profile
//      -> 跟随玩家正在使用的 profile（渐进增强，探针缺失/多活跃时静默退回）；
//   1. 配置了 username -> 精确匹配（可选 password 先经 `/launcher/v2/login` 校验）；
//   2. 未配置且 server 恰有 1 个 profile -> 零配置自动选用；
//   3. 未配置且有多个 profile -> 结构化 AMBIGUOUS_PROFILE（列出候选）。
//
// 失败一律抛结构化错误，绝不静默返回空数据。
// =============================================================================

import { SptRuntimeError } from "../errors.js";
import { isRecord, readString, unwrapLauncherEnvelope } from "../snapshot/util.js";
import type { SptConnection } from "../transport/connection.js";
import { RUNTIME_ERROR_CODES } from "../types.js";

/** 免会话的 profile 列表路由 */
export const PROFILES_ROUTE = "/launcher/v2/profiles";

/** 免会话的登录校验路由 */
export const LOGIN_ROUTE = "/launcher/v2/login";

/** 可选活跃探针路由（ticket 08 B 部分的微 mod 提供；缺失时静默退回） */
export const ACTIVE_PROFILES_ROUTE = "/spt/runtime/active-profiles";

export interface SessionCredentials {
  /** 目标 profile 的 username；缺省时按规则自动选择（单 profile）或报歧义（多 profile） */
  username?: string;
  /** 可选密码；配置后先经 `/launcher/v2/login` 校验 */
  password?: string;
}

export interface AcquiredSession {
  username: string;
  /** profileId，即后续请求的 PHPSESSID 值 */
  profileId: string;
  /** 会话来源：活跃探针 / 精确匹配 / 单 profile 自动选择 */
  source: "active-probe" | "username" | "auto-single";
}

interface ProfileEntry {
  username: string;
  profileId: string;
}

/** 从 `{Response: [...]}` 的 profile 列表提取 (username, profileId) 对 */
function listProfiles(response: unknown): ProfileEntry[] {
  const entries = Array.isArray(response) ? response : [];
  const result: ProfileEntry[] = [];
  for (const entry of entries.filter(isRecord)) {
    const profileId = readString(entry.profileId);
    if (profileId) {
      result.push({ username: readString(entry.username), profileId });
    }
  }
  return result;
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
 * 探测活跃 profile（可选增强）。探针路由不存在/异常/形状不符一律返回 null（静默退回），
 * 由调用方继续走 A 规则。探针 mod 约定响应为 `{"activeProfiles": ["<profileId>", ...]}`。
 * 注意：server 对未知路由返回 200 + `{"err": ..., "errmsg": "UNHANDLED RESPONSE"}` 信封，
 * 因此以形状而非状态码判定可用性。
 */
async function probeActiveProfileIds(connection: SptConnection): Promise<string[] | null> {
  try {
    const response = await connection.request({
      method: "POST",
      path: ACTIVE_PROFILES_ROUTE,
      body: {},
    });
    const body = response.body;
    if (!isRecord(body) || !Array.isArray(body.activeProfiles)) {
      return null;
    }
    return body.activeProfiles.filter((id): id is string => typeof id === "string" && id.length > 0);
  } catch {
    return null;
  }
}

/**
 * 对给定连接执行会话获取：解析 profileId 并注入 PHPSESSID。
 * 失败抛结构化 SptRuntimeError（PROFILE_NOT_FOUND / AMBIGUOUS_PROFILE / AUTH_FAILED）。
 */
export async function acquireSession(
  connection: SptConnection,
  credentials: SessionCredentials,
): Promise<AcquiredSession> {
  const response = await connection.request({ method: "POST", path: PROFILES_ROUTE, body: {} });
  const profiles = listProfiles(unwrapLauncherEnvelope(response.body));
  if (profiles.length === 0) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.PROFILE_NOT_FOUND,
      `server 上没有任何 profile（${PROFILES_ROUTE} 返回空列表）`,
      { route: PROFILES_ROUTE },
    );
  }

  // 规则 0：活跃探针恰有 1 个活跃 profile -> 跟随玩家
  const activeIds = await probeActiveProfileIds(connection);
  if (activeIds && activeIds.length === 1) {
    const activeId = activeIds[0];
    const known = profiles.find((entry) => entry.profileId === activeId);
    connection.setSessionId(activeId);
    return { username: known?.username ?? "(unknown)", profileId: activeId, source: "active-probe" };
  }

  const username = credentials.username?.trim();

  // 规则 1：配置了 username -> 精确匹配（可选密码校验）
  if (username) {
    const password = credentials.password?.trim();
    if (password) {
      await validateLogin(connection, username, password);
    }
    const matched =
      profiles.find((entry) => entry.username === username) ??
      profiles.find((entry) => entry.username.toLowerCase() === username.toLowerCase());
    if (!matched) {
      throw new SptRuntimeError(
        RUNTIME_ERROR_CODES.PROFILE_NOT_FOUND,
        `未在 ${PROFILES_ROUTE} 中找到 username=${username} 的 profile`,
        { username, route: PROFILES_ROUTE },
      );
    }
    connection.setSessionId(matched.profileId);
    return { username: matched.username, profileId: matched.profileId, source: "username" };
  }

  // 规则 2：单 profile -> 零配置自动选用
  if (profiles.length === 1) {
    connection.setSessionId(profiles[0].profileId);
    return { username: profiles[0].username, profileId: profiles[0].profileId, source: "auto-single" };
  }

  // 规则 3：多 profile 且无法确定目标 -> 结构化歧义错误（静默选择是测试自动化大忌）
  throw new SptRuntimeError(
    RUNTIME_ERROR_CODES.AMBIGUOUS_PROFILE,
    `server 有 ${profiles.length} 个 profile，未配置 username 且无唯一活跃 profile，无法自动选择`,
    {
      candidates: profiles.map((entry) => entry.username),
      env: "TARKOV_RUNTIME_MCP_USERNAME",
    },
  );
}
