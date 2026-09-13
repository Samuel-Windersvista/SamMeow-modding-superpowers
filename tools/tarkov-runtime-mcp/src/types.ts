// =============================================================================
// tarkov-runtime-mcp 共享类型与信封
//
// 信封与 spt-mcp 同构（ok/tool/summary 或 ok/code/message），但错误码扩展为
// 运行时专用的结构化错误集（见 spec「错误模型」）。
// =============================================================================

/** 运行时 MCP 的结构化错误码（机器可读） */
export const RUNTIME_ERROR_CODES = {
  /** server 自报版本与锚定 BEM tag 不一致 */
  VERSION_MISMATCH: "VERSION_MISMATCH",
  /** 候选端口均无 SPT server 响应（网络层失败/端口关闭/HTTP 非 2xx） */
  SERVER_UNREACHABLE: "SERVER_UNREACHABLE",
  /** 请求的 section 未被本 MCP 实现 */
  UNSUPPORTED_SECTION: "UNSUPPORTED_SECTION",
  /** raid.* 局内工具：Phase 2 的 BepInEx Client Bridge 尚未安装 */
  CLIENT_BRIDGE_NOT_INSTALLED: "CLIENT_BRIDGE_NOT_INSTALLED",
  /** wait_for 谓词超时（Phase 2 工具使用） */
  WAIT_TIMEOUT: "WAIT_TIMEOUT",
  /** session 受限工具未配置 username（无法获取 PHPSESSID 会话） */
  SESSION_NOT_CONFIGURED: "SESSION_NOT_CONFIGURED",
  /** 已配置 username 但 `/launcher/v2/profiles` 中无匹配 profile */
  PROFILE_NOT_FOUND: "PROFILE_NOT_FOUND",
  /** 配置了 password 但 `/launcher/v2/login` 校验失败 */
  AUTH_FAILED: "AUTH_FAILED",
  /** SPT 路由返回 `{err != 0, errmsg}` 业务错误信封 */
  ROUTE_ERROR: "ROUTE_ERROR",
  /** 入参校验失败 */
  INVALID_INPUT: "INVALID_INPUT",
  /** 未归类的内部错误 */
  INTERNAL_ERROR: "INTERNAL_ERROR",
} as const;

export type RuntimeErrorCode = (typeof RUNTIME_ERROR_CODES)[keyof typeof RUNTIME_ERROR_CODES];

export interface OkEnvelope {
  ok: true;
  tool: string;
  summary: string;
  data?: unknown;
}

export interface ErrEnvelope {
  ok: false;
  tool: string;
  /** 机器可读错误码（RUNTIME_ERROR_CODES） */
  code: string;
  /** 人类可读错误说明 */
  message: string;
  /** 结构化细节，例如 VERSION_MISMATCH 的 { expected, actual } */
  details?: unknown;
}

export type Envelope = OkEnvelope | ErrEnvelope;

export function okEnv(tool: string, summary: string, data?: unknown): OkEnvelope {
  return { ok: true, tool, summary, data };
}

export function errEnv(
  tool: string,
  message: string,
  code: string,
  details?: unknown,
): ErrEnvelope {
  return { ok: false, tool, code, message, details };
}

// -----------------------------------------------------------------------------
// 版本与握手
// -----------------------------------------------------------------------------

/** 从 `/singleplayer/settings/version` 标签解析出的 SPT 版本信息 */
export interface ParsedSptVersion {
  /** 原始标签，如 "SPT 5.0.0 (BEM) ff0bf32" */
  raw: string;
  /** 项目名，如 "SPT" */
  project: string;
  /** 语义化核心版本，如 "5.0.0" */
  core: string;
  /** 构建通道，如 "BEM" / "BE"；正式版为 null */
  channel: string | null;
  /** 提交短哈希；标签未携带时为 null */
  commit: string | null;
}

/** 锚定的 BEM 预发布 tag（配置常量）的解析结果 */
export interface AnchoredVersion {
  /** 原始 tag，如 "5.0.0-BEM-20260910" */
  raw: string;
  /** 核心版本，如 "5.0.0" */
  core: string;
  /** 构建通道，如 "BEM"；正式 tag 为 null */
  channel: string | null;
  /** 构建日期段，如 "20260910"；正式 tag 为 null */
  build: string | null;
}

export interface VersionGatePassed {
  passed: true;
  anchor: string;
}

export interface VersionGateFailed {
  passed: false;
  anchor: string;
  /** 期望（锚定 tag） */
  expected: string;
  /** 实际（server 自报标签） */
  actual: string;
  /** 人类可读原因 */
  reason: string;
}

export type VersionGate = VersionGatePassed | VersionGateFailed;

export interface HandshakeResult {
  host: string;
  port: number;
  baseUrl: string;
  /** 解析出的 server 版本 */
  version: ParsedSptVersion;
  /** 锚定 tag 解析结果 */
  anchor: AnchoredVersion;
  /** 门禁结果（握手成功时必为 passed） */
  gate: VersionGate;
}

// -----------------------------------------------------------------------------
// 工具输出
// -----------------------------------------------------------------------------

export interface ConnectionInfo {
  host: string;
  port: number;
  baseUrl: string;
}

export interface DiscoveredInstance extends ConnectionInfo {
  versionLabel: string;
  reachable: true;
}

export interface InstancesData {
  count: number;
  /** 单实例模型：无多实例寻址机制 */
  model: "single-instance";
  instances: DiscoveredInstance[];
}

export interface ServerStatusData {
  connection: ConnectionInfo;
  version: ParsedSptVersion;
  anchor: string;
  gate: VersionGate;
  /** MCP 自身能力自报（吸收 5.0 版本线内部漂移） */
  capabilities: {
    sections: string[];
    bridge: "not_installed";
  };
}
