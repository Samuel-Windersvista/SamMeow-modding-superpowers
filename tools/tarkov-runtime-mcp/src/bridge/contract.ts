// =============================================================================
// 桥接跨语言契约（C10 单一源，TS 端运行时读取）
//
// 契约工件：shared/bridge-contract/contract.json（协议版本 + 归一化规则 +
// 聚合上限）。C# 端经 EmbeddedResource 编译期嵌入同一文件；两端消费同一数据，
// 任何源码侧字面量都不再是权威（见 docs/adr/0009-bridge-cross-language-contract.md）。
//
// 路径解析：本模块编译后位于 <pkg>/dist/bridge/contract.js，源码位于
// <pkg>/src/bridge/contract.ts——两者同深度，故 "../../../../shared/bridge-contract/contract.json"
// 在两种布局下都成立：
//   - 仓库布局：<repo>/tools/tarkov-runtime-mcp/{src,dist}/bridge/ → <repo>/shared/...
//   - 便携树布局：<plugin>/tools/tarkov-runtime-mcp/{src,dist}/bridge/ → <plugin>/shared/...
//   （便携构建脚本整树复制 shared/，见 scripts/build-portable-plugin.ps1）
//
// 失败语义：文件缺失 / JSON 非法 / 字段校验失败一律**响亮抛错**（绝不静默降级），
// 使契约漂移在启动期即暴露。
// =============================================================================

import { readFileSync } from "node:fs";
import { fileURLToPath } from "node:url";

/** 单条日志归一化规则（pattern 为 JS/.NET 共通子集，顺序即应用顺序） */
export interface BridgeNormalizationRule {
  /** 规则标识（如 guid / id24hex / hex / number） */
  id: string;
  /** 正则源文本（两端同源；C# 侧以 RegexOptions.ECMAScript 编译对齐 JS 语义） */
  pattern: string;
  /** 替换文本（占位符，如 <guid>） */
  replacement: string;
}

/** 日志归一化契约段 */
export interface BridgeLogNormalization {
  /** 规则数组；顺序即应用顺序 */
  rules: BridgeNormalizationRule[];
  /**
   * 空白收敛模式（显式字符类 + 量词，如 `[ \t…]+`）：把连续空白折叠为单空格，
   * `trim` 也使用同一集合。**不含 `\s`**——.NET 的 ECMAScript `\s` 仅 ASCII，
   * 与 JS 的 `\s` 集合不同（见 ADR-0009）。
   */
  whitespacePattern: string;
  /** 是否按 `whitespacePattern` 去除首尾空白 */
  trim: boolean;
}

/** 日志聚合契约段 */
export interface BridgeLogAggregation {
  /** 聚合组数上限 */
  maxGroups: number;
}

/** 桥接契约（与 contract.json 一一对应） */
export interface BridgeContract {
  /** wire 协议版本（握手门禁唯一源） */
  protocolVersion: number;
  logNormalization: BridgeLogNormalization;
  logAggregation: BridgeLogAggregation;
}

/**
 * 契约文件 URL（相对本模块解析；仓库布局与便携树布局同镜像）。
 * 导出以便诊断与测试定位。
 */
export const BRIDGE_CONTRACT_URL = new URL(
  "../../../../shared/bridge-contract/contract.json",
  import.meta.url,
);

let cached: BridgeContract | null = null;

/**
 * 读取 + 解析 + 校验 + 缓存契约（进程内单例）。
 * 任何失败抛 Error（消息含解析后路径与原因）。
 */
export function loadBridgeContract(): BridgeContract {
  if (cached !== null) {
    return cached;
  }

  const path = fileURLToPath(BRIDGE_CONTRACT_URL);

  let raw: string;
  try {
    raw = readFileSync(path, "utf8");
  } catch (error) {
    const reason = error instanceof Error ? error.message : String(error);
    throw new Error(
      `桥接契约不可读（${path}）：${reason}。` +
        "契约工件应位于 <仓库或便携树根>/shared/bridge-contract/contract.json。",
    );
  }

  let parsed: unknown;
  try {
    parsed = JSON.parse(raw);
  } catch (error) {
    const reason = error instanceof Error ? error.message : String(error);
    throw new Error(`桥接契约 JSON 解析失败（${path}）：${reason}`);
  }

  cached = parseBridgeContract(parsed);
  return cached;
}

/**
 * 校验并归一契约对象（供单测直接驱动；非法输入抛 Error，不静默降级）。
 */
export function parseBridgeContract(value: unknown): BridgeContract {
  if (!isRecord(value)) {
    throw new Error("桥接契约根节点必须是 JSON 对象");
  }

  const protocolVersion = requireInteger(value, "protocolVersion");
  if (protocolVersion < 1) {
    throw new Error(`桥接契约 protocolVersion 必须 ≥ 1（实际 ${protocolVersion}）`);
  }

  const normalizationRaw = value.logNormalization;
  if (!isRecord(normalizationRaw)) {
    throw new Error("桥接契约缺少 logNormalization 对象");
  }

  const rulesRaw = normalizationRaw.rules;
  if (!Array.isArray(rulesRaw) || rulesRaw.length === 0) {
    throw new Error("桥接契约 logNormalization.rules 必须是非空数组");
  }

  const rules: BridgeNormalizationRule[] = rulesRaw.map((ruleRaw, index) => {
    if (!isRecord(ruleRaw)) {
      throw new Error(`桥接契约 logNormalization.rules[${index}] 必须是 JSON 对象`);
    }
    const id = requireNonEmptyString(ruleRaw, "id");
    const pattern = requireNonEmptyString(ruleRaw, "pattern");
    // replacement 非空：与 C# 端 ReadString 一致（空替换会静默删掉 token，属契约错误）。
    const replacement = requireNonEmptyString(ruleRaw, "replacement");
    try {
      // 编译期校验：非法正则立即响亮报错（g 标志与两端全量替换语义一致）。
      new RegExp(pattern, "g");
    } catch (error) {
      const reason = error instanceof Error ? error.message : String(error);
      throw new Error(`桥接契约规则 ${id} 的 pattern 非法：${reason}`);
    }
    return { id, pattern, replacement };
  });

  const whitespacePattern = requireNonEmptyString(normalizationRaw, "whitespacePattern");
  try {
    // 编译期校验：非法空白模式立即响亮报错（与 C# 端 Parse 对齐）。
    new RegExp(whitespacePattern);
  } catch (error) {
    const reason = error instanceof Error ? error.message : String(error);
    throw new Error(`桥接契约 whitespacePattern 非法：${reason}`);
  }

  const trim = requireBoolean(normalizationRaw, "trim");

  const aggregationRaw = value.logAggregation;
  if (!isRecord(aggregationRaw)) {
    throw new Error("桥接契约缺少 logAggregation 对象");
  }

  const maxGroups = requireInteger(aggregationRaw, "maxGroups");
  if (maxGroups < 1) {
    throw new Error(`桥接契约 logAggregation.maxGroups 必须 ≥ 1（实际 ${maxGroups}）`);
  }

  return {
    protocolVersion,
    logNormalization: { rules, whitespacePattern, trim },
    logAggregation: { maxGroups },
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function requireString(record: Record<string, unknown>, key: string): string {
  const value = record[key];
  if (typeof value !== "string") {
    throw new Error(`桥接契约字段 ${key} 必须是字符串`);
  }
  return value;
}

function requireNonEmptyString(record: Record<string, unknown>, key: string): string {
  const value = requireString(record, key);
  if (value === "") {
    throw new Error(`桥接契约字段 ${key} 不得为空`);
  }
  return value;
}

function requireInteger(record: Record<string, unknown>, key: string): number {
  const value = record[key];
  if (typeof value !== "number" || !Number.isInteger(value)) {
    throw new Error(`桥接契约字段 ${key} 必须是整数`);
  }
  return value;
}

function requireBoolean(record: Record<string, unknown>, key: string): boolean {
  const value = record[key];
  if (typeof value !== "boolean") {
    throw new Error(`桥接契约字段 ${key} 必须是布尔值`);
  }
  return value;
}
