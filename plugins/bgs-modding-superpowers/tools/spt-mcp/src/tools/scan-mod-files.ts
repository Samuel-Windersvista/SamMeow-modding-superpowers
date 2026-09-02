import { z } from "zod";

import { isDirectory, scanModFiles } from "../mod-reader.js";
import { errEnv, okEnv, SPT_ERROR_CODES, type Envelope } from "../types.js";

export const ScanModFilesInput = z
  .object({
    modPath: z.string().min(1).describe("mod 目录绝对路径"),
  })
  .strict();

export function runScanModFiles(args: unknown): Envelope {
  const parsed = ScanModFilesInput.safeParse(args);
  if (!parsed.success) {
    return errEnv(
      "spt_scan_mod_files",
      "无效输入",
      SPT_ERROR_CODES.INVALID_INPUT,
      parsed.error.message,
    );
  }
  const { modPath } = parsed.data;
  if (!isDirectory(modPath)) {
    return errEnv(
      "spt_scan_mod_files",
      `目录不存在：${modPath}`,
      SPT_ERROR_CODES.NOT_FOUND,
    );
  }
  try {
    const files = scanModFiles(modPath);
    return okEnv("spt_scan_mod_files", `列出 ${files.length} 个文件`, {
      modPath,
      fileCount: files.length,
      files,
    });
  } catch (error) {
    return errEnv(
      "spt_scan_mod_files",
      `扫描失败：${error instanceof Error ? error.message : String(error)}`,
      SPT_ERROR_CODES.INTERNAL_ERROR,
    );
  }
}
