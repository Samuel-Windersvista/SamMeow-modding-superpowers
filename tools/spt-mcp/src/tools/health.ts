// =============================================================================
// health.ts — 运行时布局只读自查（spt_health）
//
// 返回共享解析器算出的完整布局报告（mode / pluginRoot / 逐资源 path+source+ok+
// reason / warnings），供 agent 在工具结果里直接定位知识层或 helper 故障。
// =============================================================================

import { z } from "zod";

import { getLayout } from "../runtime-layout.js";
import { okEnv, type Envelope } from "../types.js";

export const HealthInput = z.object({}).strict();

export function runHealth(): Envelope {
  const layout = getLayout();
  const unavailable = layout.warnings.length;
  const summary =
    unavailable === 0
      ? `运行时布局正常（${layout.mode} 模式，全部资源可用）`
      : `运行时布局：${layout.mode} 模式，${unavailable} 项资源不可用`;

  return okEnv("spt_health", summary, layout);
}
