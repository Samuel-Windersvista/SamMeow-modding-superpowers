// =============================================================================
// il-reader.ts — 客户端 DLL IL 读取（spawn il-helper 子进程）
//
// 调用 il-helper（Mono.Cecil）读客户端 DLL 的 Harmony patch 信息，
// 输出 ClientPatchInfo 供 conflict-engine 的 IL 级冲突检测使用。
// =============================================================================

import { execFileSync } from "node:child_process";

import { getLayout } from "./runtime-layout.js";
import type { ClientPatchInfo, PatchBehavior } from "./types.js";

/** il-helper 输出结构（与 il-helper/src/Program.cs 的 JSON 对应） */
interface IlHelperOutput {
  path: string;
  ok: boolean;
  error?: string;
  plugin?: {
    type: string;
    guid?: string;
    name?: string;
    version?: string;
    dependencies?: string[];
  };
  patchCount: number;
  patches?: {
    patchClass: string;
    targetType?: string;
    targetMethod?: string;
    patchTypes: string[];
    behavior?: PatchBehavior;
  }[];
}

/** IL helper 定位：唯一解析点在 shared/runtime-layout.mjs（env 显式无效即报错，不回退） */
function ilHelperPath(): string | null {
  const status = getLayout().helpers.il;
  return status.ok ? status.path : null;
}

/**
 * IL helper 不可用时的 reason（可用时 undefined）。
 *
 * `readIlPatches` 无法用返回值区分「helper 缺失」与「DLL 里没有 patch」，
 * 调用方（spt_analyze_conflicts）必须用本函数把降级显式暴露到结果里。
 */
export function ilHelperUnavailableReason(): string | undefined {
  const status = getLayout().helpers.il;
  return status.ok ? undefined : status.reason;
}

/**
 * 用 il-helper 批量读客户端 DLL 的 Harmony patch 信息。
 * 返回 ClientPatchInfo[]（每 patch 一条），失败返回空数组（不抛异常）。
 *
 * 注意：空数组也可能是「helper 不可用」——调用方用 `ilHelperUnavailableReason()`
 * 区分这两种情形，并在结果里携带警告。
 */
export function readIlPatches(dllPaths: string[]): ClientPatchInfo[] {
  if (dllPaths.length === 0) return [];
  const helper = ilHelperPath();
  if (!helper) {
    return [];
  }
  try {
    const stdout = execFileSync(helper, dllPaths, {
      encoding: "utf8",
      maxBuffer: 128 * 1024 * 1024,
      windowsHide: true,
    });
    const parsed: unknown = JSON.parse(stdout);
    if (!Array.isArray(parsed)) return [];

    const out: ClientPatchInfo[] = [];
    for (const item of parsed as IlHelperOutput[]) {
      if (!item.ok || !item.patches) continue;
      const pluginName = item.plugin?.name ?? item.plugin?.type ?? "unknown";
      for (const p of item.patches) {
        out.push({
          pluginName,
          patchClass: p.patchClass,
          targetType: p.targetType,
          targetMethod: p.targetMethod,
          patchTypes: p.patchTypes,
          behavior: p.behavior,
        });
      }
    }
    return out;
  } catch {
    return [];
  }
}
