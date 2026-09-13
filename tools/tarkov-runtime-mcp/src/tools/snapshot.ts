// =============================================================================
// tarkov_snapshot
//
// 语义化状态快照工具。首版实现 `profile` section：经 `/client/*` 路由读取
// profile 数据并归一为计数型摘要（等级/技能/任务进度计数）。
//
// 行为：
//   - sections 缺省或空数组 -> 读取全部已实现 section；
//   - 未知 section -> 结构化 UNSUPPORTED_SECTION（原子拒绝，不发起路由请求）；
//   - 读取前先执行握手/版本门禁，快照不建立在错误版本上。
// =============================================================================

import { z } from "zod";

import type { SptClient } from "../client/client.js";
import { toErrorEnvelope } from "../errors.js";
import { assembleSnapshot, isSupportedSection } from "../snapshot/assembler.js";
import { SUPPORTED_SECTIONS, type SnapshotSectionName } from "../snapshot/schema.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";
import type { ToolHandler } from "./server-status.js";

export const SNAPSHOT_TOOL_NAME = "tarkov_snapshot";

export const SnapshotInput = z
  .object({
    /** 要读取的 section 列表；缺省 = 全部已实现 section */
    sections: z.array(z.string()).optional(),
  })
  .strict();

export function createSnapshotTool(client: SptClient): ToolHandler {
  return async function runSnapshot(args: unknown): Promise<Envelope> {
    const parsed = SnapshotInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        SNAPSHOT_TOOL_NAME,
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        parsed.error.message,
      );
    }

    // sections 缺省或空数组 -> 读取全部已实现 section
    const requested = parsed.data.sections?.length
      ? parsed.data.sections
      : [...SUPPORTED_SECTIONS];

    // 原子校验：存在未知 section 时不发起任何路由请求
    const unsupported = requested.filter((name) => !isSupportedSection(name));
    if (unsupported.length > 0) {
      return errEnv(
        SNAPSHOT_TOOL_NAME,
        `不支持的 section：${unsupported.join(", ")}`,
        RUNTIME_ERROR_CODES.UNSUPPORTED_SECTION,
        { unsupported, supported: [...SUPPORTED_SECTIONS] },
      );
    }

    try {
      // 握手/版本门禁：确保 server 可达且版本匹配
      await client.connect();
      const instances = await client.discover();
      const connection = instances.length > 0 ? instances[0].connection : undefined;
      if (!connection) {
        return errEnv(
          SNAPSHOT_TOOL_NAME,
          "未发现可用的 SPT server 连接",
          RUNTIME_ERROR_CODES.SERVER_UNREACHABLE,
        );
      }

      const sections = requested as SnapshotSectionName[];
      const snapshot = await assembleSnapshot(connection, sections);
      return okEnv(SNAPSHOT_TOOL_NAME, `快照完成：${sections.join(", ")}`, snapshot);
    } catch (error) {
      return toErrorEnvelope(SNAPSHOT_TOOL_NAME, error);
    }
  };
}
