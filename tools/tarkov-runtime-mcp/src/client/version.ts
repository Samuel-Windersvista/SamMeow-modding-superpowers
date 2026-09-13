// =============================================================================
// 版本标签解析与 BEM tag 门禁
//
// server 自报端点：GET /singleplayer/settings/version
//   -> GameCallbacks.GetVersion -> HttpResponseUtil.NoBody(new { Version = ... })
//   -> Watermark.GetInGameVersionLabel() = "{ProjectName} {SPT_VERSION}{build} {commit6}"
//   例：BEM 构建 "SPT 5.0.0 (BEM) ff0bf32"；BE 构建 "SPT 5.0.0 (BE) abc1234"；正式版 "SPT 5.0.0"
//
// 锚定 tag 形如 "5.0.0-BEM-20260910"（GitHub workflow 由 tag 推导 BLEEDINGEDGEMODS）。
//
// [限制] 该端点只暴露核心版本 + 构建通道 + commit，不含 tag 的日期段；
// 因此门禁比较核心版本与构建通道，日期段仅作锚点标识、不参与比较（见 ADR-0003 / 工单复核说明）。
// =============================================================================

import type {
  AnchoredVersion,
  ParsedSptVersion,
  VersionGate,
} from "../types.js";

/** server 版本自报端点 */
export const VERSION_ENDPOINT = "/singleplayer/settings/version";

// 期望标签形如：<project> <x.y.z[-suffix]> [(<CHANNEL>)] [<commit>]
const LABEL_PATTERN =
  /^(\S+)\s+(\d+\.\d+\.\d+(?:[-+][0-9A-Za-z.-]+)?)(?:\s+\(([^)]+)\))?(?:\s+(\S+))?$/;

// 锚定 tag 形如：<x.y.z>[-<CHANNEL>-<build>]
const ANCHOR_PATTERN = /^(\d+\.\d+\.\d+)(?:-([A-Za-z]+)-(\w+))?$/;

/** 解析 server 自报版本标签；格式不符返回 null */
export function parseVersionLabel(label: string): ParsedSptVersion | null {
  const trimmed = label.trim();
  if (!trimmed) {
    return null;
  }
  const match = LABEL_PATTERN.exec(trimmed);
  if (!match) {
    return null;
  }
  return {
    raw: trimmed,
    project: match[1],
    core: match[2],
    channel: match[3]?.trim() ?? null,
    commit: match[4] ?? null,
  };
}

/** 解析锚定 tag；格式不符返回 null */
export function parseAnchor(anchor: string): AnchoredVersion | null {
  const trimmed = anchor.trim();
  const match = ANCHOR_PATTERN.exec(trimmed);
  if (!match) {
    return null;
  }
  return {
    raw: trimmed,
    core: match[1],
    channel: match[2] ?? null,
    build: match[3] ?? null,
  };
}

/** 比较 server 自报版本与锚定 tag：核心版本一致且构建通道一致方可通过 */
export function checkVersionGate(
  observed: ParsedSptVersion,
  anchor: AnchoredVersion,
): VersionGate {
  if (observed.core !== anchor.core) {
    return {
      passed: false,
      anchor: anchor.raw,
      expected: anchor.raw,
      actual: observed.raw,
      reason: `SPT 版本线不一致：期望 ${anchor.core}，实际 ${observed.core}`,
    };
  }
  if (anchor.channel) {
    const observedChannel = observed.channel?.toUpperCase() ?? null;
    if (observedChannel !== anchor.channel.toUpperCase()) {
      return {
        passed: false,
        anchor: anchor.raw,
        expected: anchor.raw,
        actual: observed.raw,
        reason: `构建通道不一致：期望 ${anchor.channel}，实际 ${observed.channel ?? "无"}`,
      };
    }
  }
  return { passed: true, anchor: anchor.raw };
}
