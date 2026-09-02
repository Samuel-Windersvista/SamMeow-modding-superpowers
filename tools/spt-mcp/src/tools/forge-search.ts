import { z } from "zod";

import { searchForge } from "../forge-reader.js";
import { errEnv, okEnv, SPT_ERROR_CODES, type Envelope } from "../types.js";

export const ForgeSearchInput = z
  .object({
    query: z.string().optional().describe("在标题/简介中做不区分大小写的子串匹配"),
    category: z.string().optional().describe("Forge 分类（slug 或标题，如 'weapons'、'Bots'）"),
    sptVersion: z.string().optional().describe("目标 SPT 版本（如 '4.1'），与 hot-index 的 best_spt 兼容性匹配"),
    modType: z.enum(["server", "client"]).optional().describe("mod 类型过滤（归档快照无 mod_type 字段，当前无匹配项）"),
  })
  .strict();

export function runForgeSearch(args: unknown): Envelope {
  const parsed = ForgeSearchInput.safeParse(args);
  if (!parsed.success) {
    return errEnv(
      "spt_forge_search",
      "无效输入",
      SPT_ERROR_CODES.INVALID_INPUT,
      parsed.error.message,
    );
  }
  try {
    const result = searchForge({
      query: parsed.data.query,
      category: parsed.data.category,
      sptVersion: parsed.data.sptVersion,
      modType: parsed.data.modType,
    });
    return okEnv(
      "spt_forge_search",
      `匹配 ${result.total} 个 mod`,
      {
        matches: result.matches.slice(0, 50),
        matchCount: result.total,
        truncated: result.total > 50,
        note: result.note,
      },
    );
  } catch (error) {
    return errEnv(
      "spt_forge_search",
      `搜索失败：${error instanceof Error ? error.message : String(error)}`,
      SPT_ERROR_CODES.INTERNAL_ERROR,
    );
  }
}
