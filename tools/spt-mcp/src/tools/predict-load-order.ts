import { z } from "zod";

import { predictLoadOrder } from "../conflict-engine.js";
import { readModMetadata } from "../mod-reader.js";
import {
  errEnv,
  okEnv,
  SPT_ERROR_CODES,
  type Envelope,
  type ModMetadata,
} from "../types.js";

export const PredictLoadOrderInput = z
  .object({
    modPaths: z
      .array(z.string().min(1))
      .min(1)
      .describe("server mod 目录数组（含 package.json）"),
  })
  .strict();

export function runPredictLoadOrder(args: unknown): Envelope {
  const parsed = PredictLoadOrderInput.safeParse(args);
  if (!parsed.success) {
    return errEnv(
      "spt_predict_load_order",
      "无效输入",
      SPT_ERROR_CODES.INVALID_INPUT,
      parsed.error.message,
    );
  }
  const { modPaths } = parsed.data;

  try {
    const mods: ModMetadata[] = [];
    const skipped: string[] = [];
    for (const modPath of modPaths) {
      const meta = readModMetadata(modPath, "server");
      if (!meta) {
        skipped.push(modPath);
        continue;
      }
      mods.push(meta);
    }
    const loadOrder = predictLoadOrder(mods);
    return okEnv(
      "spt_predict_load_order",
      `预测 ${loadOrder.length} 个 server mod 的加载顺序`,
      {
        loadOrder,
        skipped,
        rule:
          "TypePriority 升序，同优先级按 ModGuid 字母序（SPT 4.1 ModLoader tiebreaker）",
      },
    );
  } catch (error) {
    return errEnv(
      "spt_predict_load_order",
      `预测失败：${error instanceof Error ? error.message : String(error)}`,
      SPT_ERROR_CODES.INTERNAL_ERROR,
    );
  }
}
