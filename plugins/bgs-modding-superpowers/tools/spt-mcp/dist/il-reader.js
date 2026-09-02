// =============================================================================
// il-reader.ts — 客户端 DLL IL 读取（spawn il-helper 子进程）
//
// 调用 il-helper（Mono.Cecil）读客户端 DLL 的 Harmony patch 信息，
// 输出 ClientPatchInfo 供 conflict-engine 的 IL 级冲突检测使用。
// =============================================================================
import { execFileSync } from "node:child_process";
import { statSync } from "node:fs";
function ilHelperPath() {
    const env = process.env.SPT_IL_HELPER;
    if (env && env.length > 0)
        return env;
    // 尝试常见相对位置（从 dist/ 或 src/ 上溯到 il-helper/bin/Release）
    const candidates = [
        new URL("../il-helper/bin/Release/spt-il-reader.exe", import.meta.url).pathname,
        new URL("../../il-helper/bin/Release/spt-il-reader.exe", import.meta.url).pathname,
    ];
    for (const c of candidates) {
        const p = c.replace(/^\/([A-Za-z]:)/, "$1");
        try {
            if (statSync(p).isFile())
                return p;
        }
        catch {
            // continue
        }
    }
    return null;
}
/**
 * 用 il-helper 批量读客户端 DLL 的 Harmony patch 信息。
 * 返回 ClientPatchInfo[]（每 patch 一条），失败返回空数组（不抛异常）。
 */
export function readIlPatches(dllPaths) {
    if (dllPaths.length === 0)
        return [];
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
        const parsed = JSON.parse(stdout);
        if (!Array.isArray(parsed))
            return [];
        const out = [];
        for (const item of parsed) {
            if (!item.ok || !item.patches)
                continue;
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
    }
    catch {
        return [];
    }
}
//# sourceMappingURL=il-reader.js.map