// =============================================================================
// 日志文本归一化（与桥侧 `LogSummaryNormalizer` 同构的 TS 实现）
//
// 剥离易变 token 得到聚合键，使「同一错误的数千个实例」塌缩为一组。
//
// 保守策略（宁可少合并，不可误合并——误合并会把不同错误藏进同一组）：
// 1. 只替换**形状明确**的 token：GUID、恰好 24 位 hex（EFT tpl / MongoId 形态）、
//    `0x…` 十六进制字面量（文件 / IL 偏移）、十进制数字；
// 2. 不做大小写折叠（`Error` 与 `error` 保持不同键）；
// 3. 不做词干化 / 模糊匹配，仅把连续空白折叠为单空格并去首尾空白。
//
// 与桥侧 `LogSummaryNormalizer`
// （`tools/tarkov-runtime-bridge/src/LogSummaryNormalizer.cs`）**同构**（2026-09-16 起两侧规则一致）：
// 24 位 hex 的边界为「左边界非词字符（= `\b` 左侧）+ 右边界其后不得再有 hex 字符」——**不是** `\b`。
// 真实告警样本把 24hex id 紧贴字面量 `s`：
//   `Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1`
// `\b` 在该位置不成立（`s` 是词字符）会漏配、同错误不同实例无法合并；按「恰好 24 位连续
// hex 段」识别后两者稳定合并，且不会吞掉 25+ 位 hex 串或内嵌于更长标识符的片段。
//
// 改任一侧（token 集合 / 边界语义 / 占位符 / 替换顺序）必须**同步另一侧**——否则同一错误
// 在桥组（`source` 为客户端来源）与服务器组（`source=server:<文件名>`）会得到不同的聚合键。
//
// 占位符：`<guid>` / `<id>` / `<hex>` / `<n>`。
// 替换顺序固定（GUID → 24-hex → 0x-hex → 数字），保证幂等。
// =============================================================================

/** GUID（8-4-4-4-12 hex，词边界包围） */
const GUID_PATTERN = /\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b/g;

/** 恰好 24 位连续 hex 段：左边界非词字符（= `\b` 左侧），右边界其后不得再有 hex 字符 */
const HEX_ID_PATTERN = /(?<!\w)[0-9a-fA-F]{24}(?![0-9a-fA-F])/g;

/** `0x…` 十六进制字面量（词边界包围） */
const HEX_LITERAL_PATTERN = /\b0[xX][0-9a-fA-F]+\b/g;

/** 十进制数字（含小数，词边界包围） */
const NUMBER_PATTERN = /\b\d+(?:\.\d+)?\b/g;

/** 连续空白 */
const WHITESPACE_PATTERN = /\s+/g;

/**
 * 文本 → 聚合键。空 / 纯空白 → `""`。
 * 幂等：对已归一化文本再次调用结果不变。
 */
export function normalizeLogText(text: string): string {
  if (!text) {
    return "";
  }
  const normalized = text
    .replace(GUID_PATTERN, "<guid>")
    .replace(HEX_ID_PATTERN, "<id>")
    .replace(HEX_LITERAL_PATTERN, "<hex>")
    .replace(NUMBER_PATTERN, "<n>")
    .replace(WHITESPACE_PATTERN, " ");
  return normalized.trim();
}
