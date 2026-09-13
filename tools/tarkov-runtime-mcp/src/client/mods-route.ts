// =============================================================================
// server mod 清单：`/launcher/v2/mods` 路由来源（优先）
//
// 免会话路由，响应为 launcher 信封 `{Response: ...}`（非 `/client/*` 的
// `{err, errmsg, data}`）。5.0 live 实测（2026-09-13）：无 mod 时返回
// `{"Response":{}}`——即空对象是「无 mod」的真相，不是失败。
//
// 形状兼容（上游 5.x 未定型，无 5.0 KB 记载）：
//   - `Response` 为 mod 记录数组；
//   - `Response.mods` 为数组；
//   - `Response` 为「modKey -> 记录」的字典。
// 字段名大小写不敏感，兼容 `Name`/`name`、`SptVersion`/`targetsSpt` 等别名。
// 路由不可用（网络/非 2xx）由调用方回落日志来源（source: "server-log"）。
// =============================================================================

import { SptRuntimeError } from "../errors.js";
import type { LoadedServerMod } from "../logs/mod-list.js";
import { isRecord, unwrapLauncherEnvelope } from "../snapshot/util.js";
import type { SptConnection } from "../transport/connection.js";
import { RUNTIME_ERROR_CODES } from "../types.js";

/** 免会话的 server mod 清单路由 */
export const MODS_ROUTE = "/launcher/v2/mods";

/** 路由来源的 mod 清单结果 */
export interface ServerModsRouteResult {
  source: "route";
  available: true;
  route: string;
  count: number;
  mods: LoadedServerMod[];
}

/** 按候选键取首个非空字符串 */
function pickString(entry: Record<string, unknown>, keys: string[]): string | null {
  for (const key of keys) {
    const value = entry[key];
    if (typeof value === "string" && value.length > 0) {
      return value;
    }
  }
  return null;
}

function toLoadedMod(entry: Record<string, unknown>): LoadedServerMod {
  return {
    name: pickString(entry, ["name", "Name", "modName", "ModName"]) ?? "",
    version: pickString(entry, ["version", "Version"]),
    guid: pickString(entry, ["guid", "Guid", "modGuid", "ModGuid"]),
    author: pickString(entry, ["author", "Author"]),
    targetsSpt: pickString(entry, [
      "targetsSpt",
      "TargetsSpt",
      "sptVersion",
      "SptVersion",
      "targetSptVersion",
    ]),
  };
}

/** 从 launcher 信封提取 mod 记录数组（兼容数组 / `.mods` / 字典三种形状） */
function extractModEntries(response: unknown): Record<string, unknown>[] {
  if (Array.isArray(response)) {
    return response.filter(isRecord);
  }
  if (isRecord(response)) {
    if (Array.isArray(response.mods)) {
      return response.mods.filter(isRecord);
    }
    return Object.values(response).filter(isRecord);
  }
  return [];
}

/**
 * 解 `/launcher/v2/mods` 响应为路由来源结果。
 * 空 `Response`（`{}`）为合法的「无 mod」，返回 count 0。
 */
export function parseModsRouteResponse(body: unknown): ServerModsRouteResult {
  const response = unwrapLauncherEnvelope(body);
  const mods = extractModEntries(response)
    .map(toLoadedMod)
    .filter((mod) => mod.name !== "");
  return { source: "route", available: true, route: MODS_ROUTE, count: mods.length, mods };
}

/** 请求 `/launcher/v2/mods`；网络/非 2xx 抛结构化错误，由调用方回落日志 */
export async function fetchServerModsRoute(
  connection: SptConnection,
): Promise<ServerModsRouteResult> {
  const response = await connection.request({ method: "POST", path: MODS_ROUTE, body: {} });
  if (response.status < 200 || response.status >= 300) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.ROUTE_ERROR,
      `${MODS_ROUTE} 返回 HTTP ${response.status}`,
      { route: MODS_ROUTE, status: response.status },
    );
  }
  return parseModsRouteResponse(response.body);
}
