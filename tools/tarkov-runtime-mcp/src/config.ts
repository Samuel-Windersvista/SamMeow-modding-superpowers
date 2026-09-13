// =============================================================================
// 运行时 MCP 配置
//
// 路径/端口不硬编码：host 与候选端口可经环境变量覆盖（仓库硬规则 7）。
// =============================================================================

/**
 * 锚定的 BEM 预发布 tag。
 *
 * 开工复核（2026-09-13）：SP-Tushonka 5.0 源码仓库（`5.0x-dev`，HEAD `ff0bf3281`）
 * 的 git tag 列表中，最新 BEM 标签为 `5.0.0-BEM-20260910`（前一个为 `5.0.0-BEM-20260909`）。
 * 无更新 BEM tag，故沿用工单暂定锚点。
 */
export const DEFAULT_ANCHORED_VERSION = "5.0.0-BEM-20260910";

/** SPT server 默认监听地址 */
export const DEFAULT_HOST = "127.0.0.1";

/** SPT server 默认端口（SPT_Data/configs/http.json: port=6969） */
export const DEFAULT_CANDIDATE_PORTS: readonly number[] = [6969];

export interface TarkovRuntimeConfig {
  host: string;
  candidatePorts: number[];
  anchorVersion: string;
  /** session 受限路由所需的 profile username；缺省 undefined（会话受限工具将报 SESSION_NOT_CONFIGURED） */
  username?: string;
  /** 可选密码；配置后先经 /launcher/v2/login 校验 */
  password?: string;
}

function parsePorts(raw: string | undefined): number[] | null {
  if (!raw) return null;
  const ports = raw
    .split(",")
    .map((part) => Number.parseInt(part.trim(), 10))
    .filter((port) => Number.isInteger(port) && port > 0 && port <= 65535);
  return ports.length > 0 ? ports : null;
}

/** 从环境变量装载配置；缺省回落到常量。 */
export function loadConfig(env: NodeJS.ProcessEnv = process.env): TarkovRuntimeConfig {
  const host = env.TARKOV_RUNTIME_MCP_HOST?.trim() || DEFAULT_HOST;
  const candidatePorts = parsePorts(env.TARKOV_RUNTIME_MCP_PORTS) ?? [...DEFAULT_CANDIDATE_PORTS];
  const anchorVersion = env.TARKOV_RUNTIME_MCP_ANCHOR_VERSION?.trim() || DEFAULT_ANCHORED_VERSION;
  const username = env.TARKOV_RUNTIME_MCP_USERNAME?.trim() || undefined;
  const password = env.TARKOV_RUNTIME_MCP_PASSWORD?.trim() || undefined;
  return { host, candidatePorts, anchorVersion, username, password };
}
