// =============================================================================
// raid.* 占位命名空间（Phase 2）
//
// 局内实时状态需 BepInEx Client Bridge（MO2 overlay 交付，见 ADR-0003）。
// 首版不实现，任何 raid.* 调用一律返回结构化 CLIENT_BRIDGE_NOT_INSTALLED，
// 固定 Phase 2 接入点。
// =============================================================================

import { z } from "zod";

import { RUNTIME_ERROR_CODES, errEnv, type ErrEnvelope } from "../types.js";
import type { ToolHandler } from "./server-status.js";

/** 首版注册的 raid.* 占位工具名 */
export const RAID_TOOL_NAMES = ["raid_status", "raid_player", "raid_bots"] as const;

export const RaidPlaceholderInput = z.object({}).passthrough();

/** 判断是否为 raid.* 命名空间的工具名（含未注册的前缀调用兜底） */
export function isRaidToolName(name: string): boolean {
  return name === "raid" || name.startsWith("raid_") || name.startsWith("raid.");
}

export function raidPlaceholderEnvelope(tool: string): ErrEnvelope {
  return errEnv(
    tool,
    "raid.* 局内状态需要 Phase 2 的 BepInEx Client Bridge，当前未安装",
    RUNTIME_ERROR_CODES.CLIENT_BRIDGE_NOT_INSTALLED,
  );
}

export function createRaidPlaceholderTool(toolName: string): ToolHandler {
  return async function runRaidPlaceholder(): Promise<ErrEnvelope> {
    return raidPlaceholderEnvelope(toolName);
  };
}
