// =============================================================================
// 日志条目（MCP 侧三通道统一形状）
//
// 服务器日志 tail 与 fatal 通道解析后都产出本形状；`ts` 一律为 UTC ISO 8601
// （服务器日志由行内本地时间转换，fatal 通道取观测时刻），以便与桥侧 `ts`
// 在同一视图内比较、排序与按 `since` 过滤。
// =============================================================================

/** 已采集日志条目（字段序稳定：ts/level/source/text） */
export interface LogEntry {
  /** 观测时刻（UTC ISO 8601） */
  ts: string;
  /** 归一化小写级别名（fatal/error/warning/message/info/debug） */
  level: string;
  /** 来源标识（服务器日志为 `server:<文件名>`，fatal 通道为 `fatal`） */
  source: string;
  /** 原始文本（多行条目以 \n 拼接） */
  text: string;
}
