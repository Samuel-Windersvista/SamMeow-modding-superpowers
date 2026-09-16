// =============================================================================
// 日志文本归一化（与桥侧 `LogSummaryNormalizer` 同构的 TS 实现）
//
// 剥离易变 token 得到聚合键，使「同一错误的数千个实例」塌缩为一组。
//
// C10 起规则集**不再硬编码**：从共享契约
// `shared/bridge-contract/contract.json` 的 `logNormalization` 读取（见
// `src/bridge/contract.ts`），顺序应用替换 → 按 `whitespacePattern` 折叠空白
// → 按 `trim` 去首尾（同一空白集合）。契约是两端（C# / TS）唯一规则源，改规则须改契约
// （见 docs/adr/0009-bridge-cross-language-contract.md）。
//
// 保守策略（宁可少合并，不可误合并——误合并会把不同错误藏进同一组）：
// 1. 只替换**形状明确**的 token：GUID、恰好 24 位 hex（EFT tpl / MongoId 形态）、
//    `0x…` 十六进制字面量（文件 / IL 偏移）、十进制数字；
// 2. 不做大小写折叠（`Error` 与 `error` 保持不同键）；
// 3. 不做词干化 / 模糊匹配，仅把连续空白折叠为单空格并去首尾空白。
//
// 24 位 hex 的边界为「左边界非词字符（= `\b` 左侧）+ 右边界其后不得再有 hex 字符」——**不是** `\b`。
// 真实告警样本把 24hex id 紧贴字面量 `s`：
//   `Fixed item: 6aa409923c427c1424103c2bs undefined StackObjectsCount value, now set to 1`
// `\b` 在该位置不成立（`s` 是词字符）会漏配、同错误不同实例无法合并；按「恰好 24 位连续
// hex 段」识别后两者稳定合并，且不会吞掉 25+ 位 hex 串或内嵌于更长标识符的片段。
//
// 改任一侧（token 集合 / 边界语义 / 占位符 / 替换顺序）必须**改契约**——否则同一错误
// 在桥组（`source` 为客户端来源）与服务器组（`source=server:<文件名>`）会得到不同的聚合键。
//
// 占位符：`<guid>` / `<id>` / `<hex>` / `<n>`。
// 替换顺序固定（GUID → 24-hex → 0x-hex → 数字），保证幂等。
// =============================================================================

import { loadBridgeContract } from "../bridge/contract.js";

/** 契约的归一化段（进程内单例，加载即校验） */
const normalizationContract = loadBridgeContract().logNormalization;

/** 已编译规则（顺序即应用顺序；g 标志 = 全量替换） */
const NORMALIZATION_RULES = normalizationContract.rules.map((rule) => ({
  id: rule.id,
  pattern: new RegExp(rule.pattern, "g"),
  replacement: rule.replacement,
}));

/** 连续空白折叠模式：契约的显式字符类（**不含 `\s`**，两端逐码点一致） */
const WHITESPACE_PATTERN = new RegExp(normalizationContract.whitespacePattern, "g");

/** 首尾空白去除模式：同一契约空白集合（不使用 `String.prototype.trim` 的默认集合） */
const TRIM_PATTERN = new RegExp(
  `^(?:${normalizationContract.whitespacePattern})|(?:${normalizationContract.whitespacePattern})$`,
  "g",
);

/**
 * 文本 → 聚合键。空 / 纯空白 → `""`。
 * 幂等：对已归一化文本再次调用结果不变。
 */
export function normalizeLogText(text: string): string {
  if (!text) {
    return "";
  }

  let normalized = text;
  for (const rule of NORMALIZATION_RULES) {
    normalized = normalized.replace(rule.pattern, rule.replacement);
  }

  normalized = normalized.replace(WHITESPACE_PATTERN, " ");

  return normalizationContract.trim ? normalized.replace(TRIM_PATTERN, "") : normalized;
}

