// =============================================================================
// raid.* 工具共享逻辑
//
// raid 工具（raid_status / raid_player / raid_bots / raid_events）在调用具体
// 端点前统一 ensure bridge info：拉取 `/bridge/info` 并校验 protocolVersion
// （不缓存，每次调用都实际拉取）。任一环节失败转换为结构化错误信封：
//   - 连接类失败        -> BRIDGE_UNREACHABLE（含「未安装/未运行/启动失败」提示）
//   - 协议版本不一致    -> BRIDGE_VERSION_MISMATCH（details 含 expected/actual）
//
// 信封风格与既有工具一致（ok/tool/summary 或 ok/code/message/details）。
// =============================================================================

import {
  EXPECTED_BRIDGE_PROTOCOL_VERSION,
  type BridgeConnection,
  type BridgeInfo,
} from "../bridge/connection.js";
import { RUNTIME_ERROR_CODES, errEnv, type ErrEnvelope } from "../types.js";

function errorMessage(error: unknown): string {
  return error instanceof Error ? error.message : String(error);
}

/** 连接类失败的统一信封（T03 拆分：CLIENT_BRIDGE_NOT_INSTALLED -> BRIDGE_UNREACHABLE） */
export function bridgeUnreachableEnvelope(tool: string, error: unknown): ErrEnvelope {
  return errEnv(
    tool,
    `无法连接本地 bridge（可能原因：未安装 / 未运行 / 启动失败）：${errorMessage(error)}。请检查 BepInEx 日志（BepInEx/LogOutput.log）确认桥加载状态`,
    RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE,
    { reason: "bridge_unreachable" },
  );
}

/** 协议版本不一致的结构化信封 */
export function bridgeVersionMismatchEnvelope(tool: string, actual: number): ErrEnvelope {
  return errEnv(
    tool,
    `bridge 协议版本不匹配：MCP 期望 ${EXPECTED_BRIDGE_PROTOCOL_VERSION}，桥自报 ${actual}；请更新 bridge 插件或 MCP`,
    RUNTIME_ERROR_CODES.BRIDGE_VERSION_MISMATCH,
    { expected: EXPECTED_BRIDGE_PROTOCOL_VERSION, actual },
  );
}

/**
 * 不在 raid 的结构化信封。
 * `details` 可选：raid_status 在桥可达且协议通过时附带桥自报，使一次调用
 * 即可诊断「桥活着吗」（US23）；其余 raid 工具不传，行为不变。
 */
export function notInRaidEnvelope(tool: string, message: string, details?: unknown): ErrEnvelope {
  return errEnv(tool, message, RUNTIME_ERROR_CODES.NOT_IN_RAID, details);
}

/** ensureBridgeInfo 结果：成功携带 info，失败携带待返回的错误信封 */
export type EnsureBridgeInfoResult =
  | { ok: true; info: BridgeInfo }
  | { ok: false; envelope: ErrEnvelope };

/**
 * 拉取并校验桥自报（每个 raid 工具调用前执行）。
 * 成功返回 info；连接类失败/协议不匹配返回错误信封。
 */
export async function ensureBridgeInfo(
  tool: string,
  connection: BridgeConnection,
): Promise<EnsureBridgeInfoResult> {
  let info: BridgeInfo;
  try {
    info = await connection.getInfo();
  } catch (error) {
    return { ok: false, envelope: bridgeUnreachableEnvelope(tool, error) };
  }
  if (info.protocolVersion !== EXPECTED_BRIDGE_PROTOCOL_VERSION) {
    return { ok: false, envelope: bridgeVersionMismatchEnvelope(tool, info.protocolVersion) };
  }
  return { ok: true, info };
}

/** 拉取 raid 端点结果的包装：连接类失败转换为 BRIDGE_UNREACHABLE 信封 */
export async function fetchRaidResult<T>(
  tool: string,
  fetch: () => Promise<T>,
): Promise<{ ok: true; result: T } | { ok: false; envelope: ErrEnvelope }> {
  try {
    return { ok: true, result: await fetch() };
  } catch (error) {
    return { ok: false, envelope: bridgeUnreachableEnvelope(tool, error) };
  }
}

/** 桥自报中面向工具输出的稳定子集（字段序固定） */
export function bridgeSelfReport(info: BridgeInfo): {
  pluginVersion: string;
  protocolVersion: number;
  samplingIntervalMs: number;
} {
  return {
    pluginVersion: info.pluginVersion,
    protocolVersion: info.protocolVersion,
    samplingIntervalMs: info.sampling.intervalMs,
  };
}

/** 不在 raid 时的可选 details：桥可达且协议通过时附加桥自报 */
export interface NotInRaidOptions {
  message: string;
  details?: (info: BridgeInfo) => unknown;
}

/**
 * raid 工具的共享编排（门禁阶梯）：
 *   ensureBridgeInfo -> fetchRaidResult -> `!inRaid` 判定
 * 返回错误信封，或收窄为 in-raid 的结果与桥自报；各工具的结果映射保持独立。
 */
export type RaidSampleOutcome<TInRaid> =
  | { ok: false; envelope: ErrEnvelope }
  | { ok: true; info: BridgeInfo; result: TInRaid };

/** 判别收窄：把 `{ inRaid: boolean }` 收窄为 in-raid 成员 */
function isInRaid<T extends { inRaid: boolean }>(
  value: T,
): value is Extract<T, { inRaid: true }> {
  return value.inRaid === true;
}

export async function fetchRaidSample<TResult extends { inRaid: boolean }>(
  tool: string,
  connection: BridgeConnection,
  fetch: () => Promise<TResult>,
  notInRaid: NotInRaidOptions,
): Promise<RaidSampleOutcome<Extract<TResult, { inRaid: true }>>> {
  const info = await ensureBridgeInfo(tool, connection);
  if (!info.ok) {
    return { ok: false, envelope: info.envelope };
  }

  const fetched = await fetchRaidResult(tool, fetch);
  if (!fetched.ok) {
    return { ok: false, envelope: fetched.envelope };
  }

  const result = fetched.result;
  if (!isInRaid(result)) {
    return {
      ok: false,
      envelope: notInRaidEnvelope(tool, notInRaid.message, notInRaid.details?.(info.info)),
    };
  }

  return { ok: true, info: info.info, result };
}
