// =============================================================================
// S2 日志读取器：定位并读取 SPT server 日志，产出 mod 清单或结构化降级
//
// 目录/文件名约定依据 SPT 5.0 `SPTushonka.Server/sptLogger.json`：
//   filePath = "./user/logs/spt/"，filePattern = "spt%DATE%.log"
// 即日志文件位于 <SPT 安装根>/user/logs/spt/spt<YYYYMMDD>.log；按天滚动，
// 同日超过 maxFileSizeMB 时追加序号后缀（`spt<YYYYMMDD>.1.log`）。
//
// 路径不硬编码（仓库硬规则 7）：优先 TARKOV_RUNTIME_MCP_LOG_DIR（直接指定日志目录），
// 其次 TARKOV_RUNTIME_MCP_SPT_DIR（SPT 安装根，拼出 user/logs/spt），
// 都没有时回落到 <cwd>/user/logs/spt（sane default；MCP 进程 cwd 未必是 SPT 根，
// 故真实使用时建议显式配置）。
//
// 任何文件系统失败都转为结构化降级结果，绝不抛出（工单 04 验收：不拖垮 server_status）。
// =============================================================================

import { readFile, readdir, stat } from "node:fs/promises";
import { join, resolve } from "node:path";

import { parseServerLogMods, type LoadedServerMod } from "./mod-list.js";

/** 相对 SPT 安装根的默认日志目录（见文件头依据） */
export const DEFAULT_SERVER_LOG_DIR = "user/logs/spt";

/** `spt<YYYYMMDD>[.<sequence>].log` */
const LOG_FILE_RE = /^spt(\d{8})(?:\.(\d+))?\.log$/i;

export type ServerModListUnavailableReason =
  | "log_directory_missing"
  | "log_file_missing"
  | "log_unreadable";

export interface ServerModListAvailable {
  source: "server-log";
  available: true;
  /** 实际读取的日志文件绝对路径 */
  logFile: string;
  count: number;
  declaredCount: number | null;
  allModsRejected: boolean;
  mods: LoadedServerMod[];
}

export interface ServerModListUnavailable {
  source: "server-log";
  available: false;
  reason: ServerModListUnavailableReason;
  /** 人类可读的降级原因 */
  message: string;
  logDir: string;
  /** 尝试读取的日志文件路径；未定位到时为 null */
  logFile: string | null;
}

export type ServerModListResult = ServerModListAvailable | ServerModListUnavailable;

export interface ReadServerModListOptions {
  /** 日志目录 */
  logDir: string;
  /** 显式日志文件路径；缺省时在 logDir 中选择日期最新的 spt*.log */
  logFile?: string;
}

/** 从环境变量解析日志目录（见文件头优先级说明） */
export function resolveServerLogDir(
  env: NodeJS.ProcessEnv = process.env,
  cwd: string = process.cwd(),
): string {
  const explicit = env.TARKOV_RUNTIME_MCP_LOG_DIR?.trim();
  if (explicit) {
    return explicit;
  }
  const sptDir = env.TARKOV_RUNTIME_MCP_SPT_DIR?.trim();
  if (sptDir) {
    return join(sptDir, DEFAULT_SERVER_LOG_DIR);
  }
  return resolve(cwd, DEFAULT_SERVER_LOG_DIR);
}

function unavailable(
  reason: ServerModListUnavailableReason,
  message: string,
  logDir: string,
  logFile: string | null,
): ServerModListUnavailable {
  return { source: "server-log", available: false, reason, message, logDir, logFile };
}

interface LogFileKey {
  date: string;
  sequence: number;
}

function logFileKey(name: string): LogFileKey | null {
  const match = LOG_FILE_RE.exec(name);
  if (!match) {
    return null;
  }
  return { date: match[1], sequence: match[2] ? Number.parseInt(match[2], 10) : 0 };
}

/** 在候选文件名中选择日期最新、同日序号最大的日志文件 */
function selectLatestLogFile(names: string[]): string | null {
  const candidates = names.filter((name) => LOG_FILE_RE.test(name));
  if (candidates.length === 0) {
    return null;
  }
  candidates.sort((a, b) => {
    const keyA = logFileKey(a);
    const keyB = logFileKey(b);
    if (!keyA || !keyB) {
      return a.localeCompare(b);
    }
    if (keyA.date !== keyB.date) {
      return keyA.date < keyB.date ? 1 : -1;
    }
    return keyB.sequence - keyA.sequence;
  });
  return candidates[0];
}

/** 读取 server 日志并解析 mod 清单；失败时返回结构化降级结果 */
export async function readServerModList(
  options: ReadServerModListOptions,
): Promise<ServerModListResult> {
  const { logDir } = options;
  let logFile = options.logFile ?? null;

  if (logFile) {
    try {
      const info = await stat(logFile);
      if (!info.isFile()) {
        return unavailable("log_file_missing", `日志文件不存在：${logFile}`, logDir, logFile);
      }
    } catch {
      return unavailable("log_file_missing", `日志文件不存在：${logFile}`, logDir, logFile);
    }
  } else {
    let entries: string[];
    try {
      const info = await stat(logDir);
      if (!info.isDirectory()) {
        return unavailable(
          "log_directory_missing",
          `日志目录不存在或不是目录：${logDir}`,
          logDir,
          null,
        );
      }
      entries = await readdir(logDir);
    } catch {
      return unavailable("log_directory_missing", `日志目录不存在或不可读：${logDir}`, logDir, null);
    }

    const latest = selectLatestLogFile(entries);
    if (!latest) {
      return unavailable(
        "log_file_missing",
        `日志目录中没有 spt*.log 文件：${logDir}`,
        logDir,
        null,
      );
    }
    logFile = join(logDir, latest);
  }

  let content: string;
  try {
    content = await readFile(logFile, "utf8");
  } catch {
    return unavailable("log_unreadable", `日志文件不可读：${logFile}`, logDir, logFile);
  }

  const parsed = parseServerLogMods(content);
  return {
    source: "server-log",
    available: true,
    logFile,
    count: parsed.mods.length,
    declaredCount: parsed.declaredCount,
    allModsRejected: parsed.allModsRejected,
    mods: parsed.mods,
  };
}
