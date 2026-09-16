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
  /** raid.* 未注册前缀兜底：Phase 2 的 BepInEx Client Bridge 尚未安装 */
  CLIENT_BRIDGE_NOT_INSTALLED: "CLIENT_BRIDGE_NOT_INSTALLED",
  /** 连接类失败：桥未安装 / 未运行 / 启动失败（拒绝/超时/非 2xx/响应非法） */
  BRIDGE_UNREACHABLE: "BRIDGE_UNREACHABLE",
  /** 桥自报协议版本与 MCP 期望不一致 */
  BRIDGE_VERSION_MISMATCH: "BRIDGE_VERSION_MISMATCH",
  /** raid 工具：桥在线但当前不在 raid 中 */
  NOT_IN_RAID: "NOT_IN_RAID",
  /**
   * logs 工具：桥在线且协议版本通过，但端点返回 404（桥 DLL 为旧版，未实现 /logs/*）。
   * 与 BRIDGE_VERSION_MISMATCH 刻意区分——端点缺失不是协议门禁失败。
   */
  LOGS_ENDPOINT_UNAVAILABLE: "LOGS_ENDPOINT_UNAVAILABLE",
  /** wait_for 谓词超时（Phase 2 工具使用） */
  WAIT_TIMEOUT: "WAIT_TIMEOUT",
  /** session 受限工具未配置 username（无法获取 PHPSESSID 会话） */
  SESSION_NOT_CONFIGURED: "SESSION_NOT_CONFIGURED",
  /** 已配置 username 但 `/launcher/v2/profiles` 中无匹配 profile */
  PROFILE_NOT_FOUND: "PROFILE_NOT_FOUND",
  /** 未配置 username 且存在多个 profile，无法自动选择（details 含候选 username 列表） */
  AMBIGUOUS_PROFILE: "AMBIGUOUS_PROFILE",
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

// -----------------------------------------------------------------------------
// 信封（canonical 实现已抽到共享内核 tools/mcp-kit）
//
// C2 迁移：信封类型与构造器改为从 kit re-export（facade），使工具文件的
// `import ... from "../types.js"` 路径保持不变；tarkov 专有的
// RUNTIME_ERROR_CODES / RuntimeErrorCode 仍在本模块定义。
//
// 字段名本就是 canonical（err = {message, details}），故本台无字段级 wire delta；
// 唯一形状差异是 hint/details 未提供时不落键（JSON.stringify 下与显式 undefined
// 等价，不影响任何断言）。
// -----------------------------------------------------------------------------

export { errEnv, okEnv } from "../../mcp-kit/dist/index.js";
export type { Envelope, ErrEnvelope, OkEnvelope } from "../../mcp-kit/dist/index.js";

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
  /** 原始 tag，如 "5.0.0-BEM-20260914" */
  raw: string;
  /** 核心版本，如 "5.0.0" */
  core: string;
  /** 构建通道，如 "BEM"；正式 tag 为 null */
  channel: string | null;
  /** 构建日期段，如 "20260914"；正式 tag 为 null */
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
    bridge: "supported";
  };
}
