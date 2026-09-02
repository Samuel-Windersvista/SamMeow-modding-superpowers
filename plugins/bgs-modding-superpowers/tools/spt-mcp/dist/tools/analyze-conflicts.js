import { z } from "zod";
import { analyzeConflicts, } from "../conflict-engine.js";
import { readIlPatches } from "../il-reader.js";
import { isDirectory, readConfigKeys, readModMetadata, scanModFiles, toPosix, } from "../mod-reader.js";
import { errEnv, okEnv, SPT_ERROR_CODES, } from "../types.js";
export const AnalyzeConflictsInput = z
    .object({
    modPaths: z
        .array(z.string().min(1))
        .min(1)
        .describe("要分析的 mod 路径数组（目录或 DLL 路径）"),
    sptPath: z.string().min(1).describe("SPT 安装根目录绝对路径，用于推断每个 mod 的类型（BepInEx/plugins -> client，user/mods -> server）"),
    targetSptVersion: z
        .string()
        .optional()
        .describe("目标 SPT 版本（如 3.11 / 4.1），用于版本兼容性检测。默认 4.1；分析 3.11 整合包时传 3.11"),
})
    .strict();
/** 依据路径位置推断 mod 类型：BepInEx -> client，user/mods -> server */
export function inferModType(modPath, sptPath) {
    const p = toPosix(modPath).toLowerCase();
    if (p.endsWith(".dll") || p.includes("/bepinex/"))
        return "client";
    if (sptPath && sptPath.length > 0) {
        const s = toPosix(sptPath).toLowerCase().replace(/\/+$/, "");
        if (p.startsWith(`${s}/`)) {
            const rel = p.slice(s.length + 1);
            if (rel.startsWith("bepinex/") || rel.includes("/bepinex/"))
                return "client";
            if (rel.startsWith("user/mods/") || rel.includes("/user/mods/"))
                return "server";
        }
    }
    return "server";
}
export function runAnalyzeConflicts(args) {
    const parsed = AnalyzeConflictsInput.safeParse(args);
    if (!parsed.success) {
        return errEnv("spt_analyze_conflicts", "无效输入", SPT_ERROR_CODES.INVALID_INPUT, parsed.error.message);
    }
    const { modPaths, sptPath, targetSptVersion } = parsed.data;
    try {
        const mods = [];
        const warnings = [];
        const filesByModPath = {};
        const configsByModPath = {};
        for (const modPath of modPaths) {
            const type = inferModType(modPath, sptPath);
            const meta = readModMetadata(modPath, type);
            if (!meta) {
                warnings.push(`无法读取 ${modPath}（${type}）：缺少 package.json 或 DLL`);
                continue;
            }
            mods.push(meta);
            // 文件扫描必须用 mod 根目录（原始 modPath），不能用 meta.path：
            // 3.11 嵌套 server mod 的 meta.path 指向 user/mods/<name> 子目录，
            // 若扫描子目录，relative() 会丢失 user/mods/<name>/ 前缀，导致
            // 不同 mod 的同名文件（如 db/base.json）被错误判为"同路径冲突"。
            const scanDir = isDirectory(modPath) ? modPath : meta.path;
            filesByModPath[meta.path] = scanModFiles(scanDir);
            if (meta.type === "server") {
                configsByModPath[meta.path] = readConfigKeys(scanDir);
            }
        }
        // IL 级冲突检测：对 client mod 批量读 Harmony patch 信息（il-helper）
        const clientDllPaths = mods
            .filter((m) => m.type === "client")
            .map((m) => m.path);
        const ilPatches = readIlPatches(clientDllPaths);
        const report = analyzeConflicts({
            mods,
            filesByModPath,
            configsByModPath,
            ilPatches,
            targetSptVersion,
        });
        return okEnv("spt_analyze_conflicts", `分析 ${mods.length} 个 mod（含 ${ilPatches.length} 个 Harmony patch），发现 ${report.summary.total} 个冲突`, {
            report,
            analyzedMods: mods.map((m) => ({ name: m.name, type: m.type, path: m.path })),
            ilPatchCount: ilPatches.length,
            warnings,
        });
    }
    catch (error) {
        return errEnv("spt_analyze_conflicts", `分析失败：${error instanceof Error ? error.message : String(error)}`, SPT_ERROR_CODES.INTERNAL_ERROR);
    }
}
//# sourceMappingURL=analyze-conflicts.js.map