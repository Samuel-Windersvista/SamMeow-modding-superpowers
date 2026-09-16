// =============================================================================
// runtime-layout.ts — spt-mcp 侧运行时布局薄包装
//
// 唯一解析点在 shared/runtime-layout.mjs（无构建纯 ESM，插件与 MCP 双端相对
// 导入）；本模块只负责计算插件包根 + 模块级缓存，供各工具与启动健康检查读取。
// =============================================================================

import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

import {
  formatLayoutWarnings,
  resolveRuntimeLayout,
  type RuntimeLayout,
} from "../../../shared/runtime-layout.mjs";

/**
 * 插件包根：src/ 与 dist/ 距包根同为三级（<包根>/tools/spt-mcp/{src|dist}/），
 * 因此两种布局都解析到同一个包根。
 */
export const PLUGIN_ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "..", "..", "..");

let cached: RuntimeLayout | null = null;

/**
 * 取运行时布局（模块级缓存，env 固定读 `process.env`）。
 * 缓存命中后不再重新解析：测试改 env 后必须调用 `resetLayoutForTest()`。
 */
export function getLayout(): RuntimeLayout {
  if (!cached) {
    cached = resolveRuntimeLayout(PLUGIN_ROOT, process.env);
  }
  return cached;
}

/** 清缓存（测试用） */
export function resetLayoutForTest(): void {
  cached = null;
}

export { formatLayoutWarnings };
export type { RuntimeLayout };
