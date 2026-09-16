// =============================================================================
// logs_recent
//
// 经 BridgeConnection 增量拉取桥进程内日志告警（BepInEx ILogListener 采集的
// Warning 及以上条目）：
//   - 入参 since = 已消费的最后一条条目的 seq（**独占**：只返回 seq > since；
//     缺省从最旧开始）、level（查询侧最小级别，**必须**为 BepInEx 级别名之一
//     `fatal`/`error`/`warning`/`message`/`info`/`debug`，大小写不敏感；归一化为
//     小写后透传；未知取值 → INVALID_INPUT，绝不静默不过滤）、limit（单次最多
//     返回条数；桥侧缺省 100、上限 1000，从 since 之后最旧一侧截断）；
//   - 输出 data = { seq, dropped, entries }（确定性字段序，与桥端点一致）；
//     seq 为桥进程内当前最新序号，仅用于判断是否有新条目；limit 截断时用本次
//     返回的最后一条条目的 seq 续拉；
//   - `dropped > 0` 表示 since 过旧、已被环形缓冲淘汰（桥侧 LogWatchRingSize）。
// 语义：
//   - 日志与 raid 状态无关（非 raid 时照常可用），故**不返回 NOT_IN_RAID**；
//   - 桥不可达：err 信封 BRIDGE_UNREACHABLE；
//   - 协议版本不一致：err 信封 BRIDGE_VERSION_MISMATCH；
//   - 桥为旧版（端点 404）：err 信封 LOGS_ENDPOINT_UNAVAILABLE（提示更新桥 DLL）。
// =============================================================================

import { z } from "zod";

import type { BridgeConnection, BridgeLogEntry } from "../bridge/connection.js";
import { LOG_LEVEL_NAMES, parseLogLevelName } from "../logs/log-level.js";
import { RUNTIME_ERROR_CODES, errEnv, okEnv, type Envelope } from "../types.js";
import { fetchLogsResult } from "./logs-common.js";
import { ensureBridgeInfo } from "./raid-common.js";
import type { ToolHandler } from "./server-status.js";

export const LOGS_RECENT_TOOL_NAME = "logs_recent";

/** level 合法取值的展示串（错误消息与工具描述共用） */
export const LOG_LEVEL_NAMES_TEXT = LOG_LEVEL_NAMES.join(" / ");

export const LogsRecentInput = z
  .object({
    /** 增量游标（独占）：只返回 seq > since 的条目；缺省从最旧开始 */
    since: z.number().int().nonnegative().optional(),
    /** 查询侧最小级别：BepInEx 级别名之一（大小写不敏感）；缺省即不过滤 */
    level: z.string().optional(),
    /** 单次最多返回条数；桥侧缺省 100、上限 1000 */
    limit: z.number().int().positive().optional(),
  })
  .strict();

/** 条目输出归一（字段序稳定：seq/ts/level/source/text） */
function toEntryOutput(entry: BridgeLogEntry): Record<string, unknown> {
  return {
    seq: entry.seq,
    ts: entry.ts,
    level: entry.level,
    source: entry.source,
    text: entry.text,
  };
}

export function createLogsRecentTool(connection: BridgeConnection): ToolHandler {
  return async function runLogsRecent(args: unknown): Promise<Envelope> {
    const parsed = LogsRecentInput.safeParse(args ?? {});
    if (!parsed.success) {
      return errEnv(
        LOGS_RECENT_TOOL_NAME,
        "无效输入",
        RUNTIME_ERROR_CODES.INVALID_INPUT,
        parsed.error.message,
      );
    }
    const { since, level, limit } = parsed.data;

    // level 显式校验：未知取值不得静默落到「不过滤」（那是调用方最难察觉的 footgun）。
    // 严格值域 = BepInEx 级别名（桥侧 `LogWatchLevel` 唯一识别的集合）。
    let normalizedLevel: string | undefined;
    if (level !== undefined) {
      const parsedLevel = parseLogLevelName(level);
      if (parsedLevel === null) {
        return errEnv(
          LOGS_RECENT_TOOL_NAME,
          `无效输入：level 必须为以下之一（大小写不敏感）：${LOG_LEVEL_NAMES_TEXT}`,
          RUNTIME_ERROR_CODES.INVALID_INPUT,
          { level },
        );
      }
      normalizedLevel = parsedLevel;
    }

    // 门禁阶梯：ensureBridgeInfo -> 拉取端点（日志与 raid 状态无关，无 inRaid 判定）。
    const info = await ensureBridgeInfo(LOGS_RECENT_TOOL_NAME, connection);
    if (!info.ok) {
      return info.envelope;
    }

    const fetched = await fetchLogsResult(LOGS_RECENT_TOOL_NAME, () =>
      connection.getLogsRecent(since, normalizedLevel, limit),
    );
    if (!fetched.ok) {
      return fetched.envelope;
    }
    const result = fetched.result;

    const droppedNote = result.dropped > 0 ? `，since 过旧丢失 ${result.dropped} 条` : "";

    return okEnv(
      LOGS_RECENT_TOOL_NAME,
      `日志告警已读取：${result.entries.length} 条（最新 seq ${result.seq}${droppedNote}）`,
      {
        seq: result.seq,
        dropped: result.dropped,
        entries: result.entries.map(toEntryOutput),
      },
    );
  };
}
