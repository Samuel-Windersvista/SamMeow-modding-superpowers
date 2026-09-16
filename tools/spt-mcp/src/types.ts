// =============================================================================
// spt-mcp 类型定义
// =============================================================================

export type ModType = "server" | "client";

export interface ServerModMetadata {
  type: "server";
  /** 绝对路径（mod 目录，含顶层 DLL 或 package.json） */
  path: string;
  name: string;
  version?: string;
  guid?: string;
  dependencies: string[];
  /** 4.1：DLL 来源时 typePriority 无法从元数据静态读出（DI TypePriority 在代码里），默认 0；package.json 来源时读 package.json */
  typePriority: number;
  /** DLL IModMetadata.sptVersion（SemanticVersioning Range，如 "~4.1.0"）或 package.json sptVersion */
  sptVersion?: string;
  /** package.json compatibleVersion（旧式字段，与 sptVersion 二选一） */
  compatibleVersion?: string;
  /** package.json 原始内容（DLL 来源为 null） */
  packageJson: Record<string, unknown> | null;
  /** 4.1 DLL 来源：mod 目录下顶层 DLL 绝对路径 */
  dllPath?: string;
}

export interface ClientModMetadata {
  type: "client";
  /** DLL 文件绝对路径 */
  path: string;
  /** 所在目录（BepInEx/plugins/ 下的目录） */
  modPath: string;
  /** 由 DLL 文件名派生（去掉 .dll 后缀） */
  name: string;
  fileName: string;
  /** 客户端 DLL 的 BepInDependency 需要 .NET helper CLI 读取（本期延后） */
  dependencies: string[];
}

export type ModMetadata = ServerModMetadata | ClientModMetadata;

export interface ModSummary {
  name: string;
  version?: string;
  guid?: string;
  dependencies: string[];
  path: string;
  type: ModType;
}

export interface ModFileEntry {
  relativePath: string;
  sizeBytes: number;
  /** SHA-1 内容指纹（同路径文件内容比较用）；null 表示未计算（大文件/失败） */
  contentHash?: string | null;
}

// -----------------------------------------------------------------------------
// 冲突报告
// -----------------------------------------------------------------------------

/** 严重度分级：B = 阻断（重复 GUID / 版本失配），O = 覆盖（文件/配置），I = IL 级（Harmony patch 同目标冲突），S/C 预留 */
export type SeverityLevel = "B" | "S" | "O" | "C" | "I" | "Unknown";

/** Harmony patch 的 IL 行为特征（il-helper 输出） */
export interface PatchBehavior {
  returnsFalse?: boolean;
  callsOriginal?: boolean;
  writesField?: boolean;
  callsOtherPatch?: boolean;
  isReadOnly?: boolean;
  writesResult?: boolean;
  writesRefParam?: boolean;
}

/** 客户端 mod 的一个 Harmony patch（il-helper 提取） */
export interface ClientPatchInfo {
  /** 所属 mod 名称 */
  pluginName: string;
  /** patch 类全名 */
  patchClass: string;
  /** 目标类型（如 EFT.Player） */
  targetType?: string;
  /** 目标方法（如 Look / method_61） */
  targetMethod?: string;
  /** patch 类型（Prefix / Postfix / Transpiler） */
  patchTypes: string[];
  /** IL 行为特征 */
  behavior?: PatchBehavior;
}

export interface ConflictFinding {
  severity: SeverityLevel;
  /** 检测项种类，如 guid_duplicate / spt_version_mismatch / file_overwrite / config_collision */
  kind: string;
  message: string;
  /** 涉及的 mod 名称 */
  mods: string[];
  detail?: Record<string, unknown>;
}

export interface LoadOrderEntry {
  position: number;
  name: string;
  guid?: string;
  typePriority: number;
  path: string;
}

export interface ConflictReport {
  sections: Record<SeverityLevel, ConflictFinding[]>;
  /** 服务端 mod 预测加载顺序（TypePriority 升序，同优先级按 GUID 字母序） */
  loadOrder: LoadOrderEntry[];
  summary: {
    total: number;
    bySeverity: Record<SeverityLevel, number>;
  };
}

// -----------------------------------------------------------------------------
// Forge 归档 / KB
// -----------------------------------------------------------------------------

export interface ForgeModSummary {
  id: number;
  title: string;
  slug: string;
  category: string | null;
  sptVersion: string | null;
  downloads: number;
  teaser: string;
}

// -----------------------------------------------------------------------------
// 信封（canonical 实现已抽到共享内核 tools/mcp-kit）
//
// C2 迁移：信封类型与构造器改为从 kit re-export（facade），使工具文件的
// `import ... from "../types.js"` 路径保持不变；spt 专有的 SPT_ERROR_CODES
// 仍在本模块定义。
//
// 字段级 delta（已在 C2 spec 登记）：错误信封 `summary` → `message`；
// 可选 `hint` / `details`。OK 信封仍使用 `summary`。
// -----------------------------------------------------------------------------

export { errEnv, okEnv } from "../../mcp-kit/dist/index.js";
export type { Envelope, ErrEnvelope, OkEnvelope } from "../../mcp-kit/dist/index.js";

export const SPT_ERROR_CODES = {
  INVALID_INPUT: "invalid_input",
  NOT_FOUND: "not_found",
  INTERNAL_ERROR: "internal_error",
  INVALID_REQUEST: "invalid_request",
  /** 运行时布局资源（知识库 / Forge 归档）不可用：显式降级，不再静默 0 匹配 */
  KB_UNAVAILABLE: "kb_unavailable",
} as const;
