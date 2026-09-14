// =============================================================================
// 运行时 MCP 配置
//
// 路径/端口不硬编码：host 与候选端口可经环境变量覆盖（仓库硬规则 7）。
// =============================================================================

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
  return {
    host,
    candidatePorts,
    anchorVersion,
    username,
    password,
    bridgeHost,
    bridgePort,
    bridgeRecordPath,
  };
}
