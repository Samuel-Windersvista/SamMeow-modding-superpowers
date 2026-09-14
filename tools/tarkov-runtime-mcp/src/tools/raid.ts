// =============================================================================
// raid.* 命名空间元数据
//
// Phase 2 起三个 raid 工具（raid_status / raid_player / raid_bots）全部真实化，
// 占位集合清空。本模块仅保留：
//   - RAID_TOOL_NAMES：已注册的 raid.* 工具名；
//   - isRaidToolName：raid.* 前缀判定（含未注册调用的兜底）；
//   - raidPlaceholderEnvelope：未注册 raid.* 调用的 CLIENT_BRIDGE_NOT_INSTALLED
//     兜底信封（保留语义，避免旧客户端调用静默失败）。
// =============================================================================

import { RUNTIME_ERROR_CODES, errEnv, type ErrEnvelope } from "../types.js";

/** 已注册的 raid.* 工具名（Phase 2 全部真实化，无占位） */
export const RAID_TOOL_NAMES = ["raid_status", "raid_player", "raid_bots"] as const;

/** 判断是否为 raid.* 命名空间的工具名（含未注册的前缀调用兜底） */
export function isRaidToolName(name: string): boolean {
  return name === "raid" || name.startsWith("raid_") || name.startsWith("raid.");
}

/** 未注册的 raid.* 前缀调用兜底：仍返回结构化 CLIENT_BRIDGE_NOT_INSTALLED */
export function raidPlaceholderEnvelope(tool: string): ErrEnvelope {
  return errEnv(
    tool,
    "raid.* 局内状态需要 BepInEx Client Bridge；该工具未注册，请检查工具名或更新 MCP",
    RUNTIME_ERROR_CODES.CLIENT_BRIDGE_NOT_INSTALLED,
  );
}
