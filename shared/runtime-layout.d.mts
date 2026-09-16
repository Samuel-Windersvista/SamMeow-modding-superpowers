// =============================================================================
// runtime-layout.d.mts — shared/runtime-layout.mjs 的类型声明
//
// 与 .mjs 同目录同名（仅扩展名不同），供 TS（NodeNext）解析导入类型；
// 声明文件不参与 tsc 输出，故不触发 rootDir 限制。
// =============================================================================

export type ResourceSource = "env" | "default";

/**
 * 资源状态（可辨识联合，编码布局契约）：
 *   - ok:true  可用（无 reason 字段）
 *   - ok:false 不可用，**reason 必有值**（调用方无需 fallback / 非空断言）
 */
export type ResourceStatus =
  | { path: string; source: ResourceSource; ok: true }
  | { path: string; source: ResourceSource; ok: false; reason: string };

export interface RuntimeLayout {
  /** 仅标签：pluginRoot/.git 存在 => 'repo'，否则 'portable' */
  mode: "repo" | "portable";
  pluginRoot: string;
  kb: {
    root: ResourceStatus;
    index: ResourceStatus;
    archive: ResourceStatus;
  };
  helpers: {
    metadata: ResourceStatus;
    il: ResourceStatus;
  };
  /** = 各 !ok 资源的 reason（顺序：kb.root / kb.index / kb.archive / helpers） */
  warnings: string[];
}

export function resolveRuntimeLayout(
  pluginRoot: string,
  env?: NodeJS.ProcessEnv,
): RuntimeLayout;

/** 多行文本（供 stderr）；无警告返回空串 */
export function formatLayoutWarnings(layout: RuntimeLayout): string;
