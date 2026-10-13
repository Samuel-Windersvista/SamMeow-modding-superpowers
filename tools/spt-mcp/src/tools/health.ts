// =============================================================================
// health.ts — 运行时布局只读自查（spt_health）
//
// 返回共享解析器算出的完整布局报告（mode / pluginRoot / 逐资源 path+source+ok+
// reason / warnings），另附知识库注册表状态与逐库汇总（消费同一份 kb/registry.ts：
// 注册表路径、来源 registry|legacy、逐库 root/index/条目数/过期标记）。
// =============================================================================

import { z } from "zod";

import { getRegistryStatus, getStaleInfo, readIndexFile } from "../kb/registry.js";
import { getLayout } from "../runtime-layout.js";
import { okEnv, type Envelope } from "../types.js";

export const HealthInput = z.object({}).strict();

export function runHealth(): Envelope {
  const layout = getLayout();
  const registry = getRegistryStatus();

  const kbs = registry.kbs.map((kb) => {
    let entryCount: number | null = null;
    if (kb.index.ok) {
      const file = readIndexFile(kb.index.path);
      if (file.ok) {
        const raw = file.raw;
        if (raw && typeof raw === "object" && !Array.isArray(raw)) {
          const entries = (raw as Record<string, unknown>).entries;
          if (Array.isArray(entries)) entryCount = entries.length;
        }
      }
    }
    return {
      id: kb.id,
      title: kb.title,
      root: kb.root,
      index: kb.index,
      envKey: kb.envKey,
      entryCount,
      stale: getStaleInfo(kb).stale,
    };
  });

  const unavailable = layout.warnings.length;
  const summary =
    unavailable === 0
      ? `运行时布局正常（${layout.mode} 模式，全部资源可用）`
      : `运行时布局：${layout.mode} 模式，${unavailable} 项资源不可用`;

  return okEnv("spt_health", summary, {
    ...layout,
    registry: {
      path: registry.path,
      source: registry.source,
      ok: registry.ok,
      ...(registry.reason !== undefined ? { reason: registry.reason } : {}),
      kbCount: registry.kbs.length,
    },
    kbs,
  });
}
