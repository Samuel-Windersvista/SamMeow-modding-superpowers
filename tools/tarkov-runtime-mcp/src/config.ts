// =============================================================================
// 运行时 MCP 配置
//
// 路径/端口不硬编码：host 与候选端口可经环境变量覆盖（仓库硬规则 7）。
// =============================================================================

import { dirname, join } from "node:path";

import { resolveServerLogDir } from "./logs/log-reader.js";

/**
 * 锚定的 BEM 预发布 tag。
 *
 * 2026-09-14 复核：SP-Tushonka 5.0 源码仓库（`5.0x-dev`）git tag 列表中，
 * 最新 BEM 标签为 `5.0.0-BEM-20260914`（前序 `0910` / `0909`）；客户端 modules
 * 与 0910 同 commit、EFT 兼容版本未变。锚点随安装环境更新（2026-09-14 起升至 0914）。
 */
export const DEFAULT_ANCHORED_VERSION = "5.0.0-BEM-20260914";

/** SPT server 默认监听地址 */
export const DEFAULT_HOST = "127.0.0.1";

/** SPT server 默认端口（SPT_Data/configs/http.json: port=6969） */
export const DEFAULT_CANDIDATE_PORTS: readonly number[] = [6969];

/** BepInEx Client Bridge 默认监听地址（仅 127.0.0.1） */
export const DEFAULT_BRIDGE_HOST = "127.0.0.1";

/** BepInEx Client Bridge 默认端口（Phase 2 局内状态） */
export const DEFAULT_BRIDGE_PORT = 49777;

/** logwatch 惰性刷新间隔默认值（ms） */
export const DEFAULT_LOGWATCH_INTERVAL_MS = 5000;

/** logwatch 惰性刷新间隔下限（ms） */
export const MIN_LOGWATCH_INTERVAL_MS = 5000;

/** logwatch 惰性刷新间隔上限（ms） */
export const MAX_LOGWATCH_INTERVAL_MS = 10000;

export interface TarkovRuntimeConfig {
  host: string;
  candidatePorts: number[];
  anchorVersion: string;
  /** session 受限路由所需的 profile username；缺省时自动选择（单 profile）或报 AMBIGUOUS_PROFILE（多 profile） */
  username?: string;
  /** 可选密码；配置后先经 /launcher/v2/login 校验 */
  password?: string;
  /**
   * BepInEx Client Bridge 监听地址（Phase 2 局内状态）。
   * 可选以保持对既有显式 config 字面量的兼容；loadConfig 总是填默认值。
   */
  bridgeHost?: string;
  /** BepInEx Client Bridge 监听端口（Phase 2 局内状态）；同 bridgeHost，可选 */
  bridgePort?: number;
  /**
   * 桥响应录制输出路径（T07，JSONL 追加写）；缺省不录制。
   * env: TARKOV_RUNTIME_MCP_BRIDGE_RECORD（值 = 文件路径）。
   */
  bridgeRecordPath?: string;
  /**
   * MCP 侧日志观测（logwatch）惰性刷新间隔（ms，钳制 5000–10000）。
   * env: TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS。可选以保持对既有显式 config
   * 字面量的兼容；loadConfig 总是填默认值。
   */
  logwatchIntervalMs?: number;
  /**
   * 服务器日志根目录（其下 spt / kestrel / requests 子目录）。
   * env: TARKOV_RUNTIME_MCP_LOGS_ROOT；缺省 = spt 日志目录的父级
   * （即 `<SPT_DIR>/user/logs`，见 resolveLogsRoot）。
   */
  logsRoot?: string;
  /**
   * 进程级崩溃通道路径（`BepInEx/ErrorLog.log`）。
   * env: TARKOV_RUNTIME_MCP_ERRORLOG_PATH；缺省由 SPT_DIR 推导
   * （`<SPT_DIR>/../BepInEx/ErrorLog.log`），SPT_DIR 亦缺失时为 undefined（通道静默为空）。
   */
  errorLogPath?: string;
}

function parsePorts(raw: string | undefined): number[] | null {
  if (!raw) return null;
  const ports = raw
    .split(",")
    .map((part) => Number.parseInt(part.trim(), 10))
    .filter((port) => Number.isInteger(port) && port > 0 && port <= 65535);
  return ports.length > 0 ? ports : null;
}

/** 解析单个端口；非法（非整数/越界）回落到 fallback */
function parsePort(raw: string | undefined, fallback: number): number {
  if (!raw) return fallback;
  const port = Number.parseInt(raw.trim(), 10);
  return Number.isInteger(port) && port > 0 && port <= 65535 ? port : fallback;
}

/**
 * logwatch 刷新间隔：缺省 / 非法 → 默认值；越界钳制到 5000–10000ms。
 * env: TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS。
 */
export function parseLogwatchInterval(raw: string | undefined): number {
  if (!raw) return DEFAULT_LOGWATCH_INTERVAL_MS;
  const parsed = Number.parseInt(raw.trim(), 10);
  if (!Number.isFinite(parsed)) return DEFAULT_LOGWATCH_INTERVAL_MS;
  return Math.min(MAX_LOGWATCH_INTERVAL_MS, Math.max(MIN_LOGWATCH_INTERVAL_MS, parsed));
}

/**
 * 服务器日志根目录（其下 spt / kestrel / requests）。
 * 优先级：`TARKOV_RUNTIME_MCP_LOGS_ROOT` 显式覆盖 → spt 日志目录（
 * `TARKOV_RUNTIME_MCP_LOG_DIR` / `_SPT_DIR` / cwd 回落，见 resolveServerLogDir）的父级。
 */
export function resolveLogsRoot(
  env: NodeJS.ProcessEnv = process.env,
  cwd: string = process.cwd(),
): string {
  const explicit = env.TARKOV_RUNTIME_MCP_LOGS_ROOT?.trim();
  if (explicit) {
    return explicit;
  }
  return dirname(resolveServerLogDir(env, cwd));
}

/**
 * 进程级崩溃通道路径（`BepInEx/ErrorLog.log`）。
 * 优先级：`TARKOV_RUNTIME_MCP_ERRORLOG_PATH` 显式覆盖 →
 * `<SPT_DIR>/../BepInEx/ErrorLog.log`（`..` 由 path.join 词法折叠）→ undefined（静默降级）。
 */
export function resolveErrorLogPath(env: NodeJS.ProcessEnv = process.env): string | undefined {
  const explicit = env.TARKOV_RUNTIME_MCP_ERRORLOG_PATH?.trim();
  if (explicit) {
    return explicit;
  }
  const sptDir = env.TARKOV_RUNTIME_MCP_SPT_DIR?.trim();
  if (!sptDir) {
    return undefined;
  }
  return join(sptDir, "..", "BepInEx", "ErrorLog.log");
}

/** 从环境变量装载配置；缺省回落到常量。 */
export function loadConfig(env: NodeJS.ProcessEnv = process.env): TarkovRuntimeConfig {
  const host = env.TARKOV_RUNTIME_MCP_HOST?.trim() || DEFAULT_HOST;
  const candidatePorts = parsePorts(env.TARKOV_RUNTIME_MCP_PORTS) ?? [...DEFAULT_CANDIDATE_PORTS];
  const anchorVersion = env.TARKOV_RUNTIME_MCP_ANCHOR_VERSION?.trim() || DEFAULT_ANCHORED_VERSION;
  const username = env.TARKOV_RUNTIME_MCP_USERNAME?.trim() || undefined;
  const password = env.TARKOV_RUNTIME_MCP_PASSWORD?.trim() || undefined;
  const bridgeHost = env.TARKOV_RUNTIME_MCP_BRIDGE_HOST?.trim() || DEFAULT_BRIDGE_HOST;
  const bridgePort = parsePort(env.TARKOV_RUNTIME_MCP_BRIDGE_PORT, DEFAULT_BRIDGE_PORT);
  const bridgeRecordPath = env.TARKOV_RUNTIME_MCP_BRIDGE_RECORD?.trim() || undefined;
  const logwatchIntervalMs = parseLogwatchInterval(env.TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS);
  const logsRoot = resolveLogsRoot(env);
  const errorLogPath = resolveErrorLogPath(env);
  return {
    host,
    candidatePorts,
    anchorVersion,
    username,
    password,
    bridgeHost,
    bridgePort,
    bridgeRecordPath,
    logwatchIntervalMs,
    logsRoot,
    errorLogPath,
  };
}
