// =============================================================================
// conflict-engine.ts — 冲突分析引擎（纯函数，无文件 IO，可独立测试）
//
// 检测项（M 级，元数据可检测）：
//   B  GUID 重复（server mod package.json guid 字段）
//   B  SPT 版本失配（sptVersion / compatibleVersion 与目标版本不兼容）
//   O  文件覆盖（多个 mod 含相同相对路径文件）
//   O  配置 key 碰撞（相同配置文件名 + 重叠 JSON 顶层 key）
//
// 加载顺序预测：TypePriority 升序，同优先级按 ModGuid 字母序（SPT 4.1
// ModLoader tiebreaker，见 curated/api-notes-4.1/mod-loading.md）。
// =============================================================================
export const DEFAULT_TARGET_SPT_VERSION = "4.1";
export function parseVersion(raw) {
    const cleaned = raw.trim().replace(/^[vV]/, "");
    const m = cleaned.match(/^(\d+)(?:\.(\d+))?(?:\.(\d+))?/);
    if (!m)
        return null;
    return {
        major: parseInt(m[1], 10),
        minor: m[2] !== undefined ? parseInt(m[2], 10) : 0,
        patch: m[3] !== undefined ? parseInt(m[3], 10) : 0,
    };
}
/** 目标版本字符串是否显式声明了 patch 段（如 "3.11.4" true，"3.11" false） */
export function hasExplicitPatch(raw) {
    const cleaned = raw.trim().replace(/^[vV]/, "");
    return cleaned.split(".").length >= 3;
}
export function compareVersions(a, b) {
    if (a.major !== b.major)
        return a.major - b.major;
    if (a.minor !== b.minor)
        return a.minor - b.minor;
    return a.patch - b.patch;
}
/** 裸版本/等号比较：按声明精度比较（有 patch 全等，否则 major.minor 相等） */
function exactVersionMatch(raw, target) {
    const cleaned = raw.trim().replace(/^[vV]/, "");
    if (/^x$/i.test(cleaned))
        return true;
    const wildcard = cleaned.match(/^(\d+)[.]x$/i);
    if (wildcard)
        return target.major === parseInt(wildcard[1], 10);
    const v = parseVersion(cleaned);
    if (!v)
        return false;
    if (cleaned.split(".").length >= 3) {
        return compareVersions(target, v) === 0;
    }
    return target.major === v.major && target.minor === v.minor;
}
/**
 * 判断声明版本（SPT Range 语法子集）是否与目标版本兼容。
 * 支持：>= > <= < ~ ^ = 裸版本、"a - b" 区间、逗号分隔多约束、"4.x" 通配。
 * 解析失败按未知处理（返回 true，不误报）。
 */
export function isCompatibleWithTarget(declared, target) {
    const t = parseVersion(target);
    if (!t)
        return true;
    const parts = declared
        .split(",")
        .map((s) => s.trim())
        .filter(Boolean);
    if (parts.length === 0)
        return true;
    return parts.some((part) => {
        // 区间形式："a - b"（两侧都必须以数字开头，避免误吞 "not-a-version" 这类文本）
        const range = part.match(/^\s*(\d[^\s-]*)\s*-\s*(\d.*)$/);
        if (range) {
            const lo = parseVersion(range[1]);
            const hi = parseVersion(range[2]);
            if (lo && hi) {
                return compareVersions(t, lo) >= 0 && compareVersions(t, hi) <= 0;
            }
        }
        const m = part.match(/^(>=|<=|>|<|~|\^|=)?\s*(.+)$/);
        if (m) {
            const op = m[1] ?? "";
            const v = parseVersion(m[2]);
            if (v) {
                const cmp = compareVersions(t, v);
                switch (op) {
                    case ">=":
                        return cmp >= 0;
                    case ">":
                        return cmp > 0;
                    case "<=":
                        return cmp <= 0;
                    case "<":
                        return cmp < 0;
                    case "~":
                        // ~4.1.0 = >=4.1.0 <4.2.0
                        // 目标只有 major.minor（如 "3.11"）时视为 3.11.x 系列，patch 下界不限制
                        if (t.patch === 0 && !hasExplicitPatch(target)) {
                            return t.major === v.major && t.minor === v.minor;
                        }
                        return t.major === v.major && t.minor === v.minor && cmp >= 0;
                    case "^":
                        return t.major === v.major && cmp >= 0;
                    default:
                        return exactVersionMatch(m[2], t);
                }
            }
        }
        // 未知格式 -> 视为兼容（解析失败不误报）
        return true;
    });
}
// -----------------------------------------------------------------------------
// 加载顺序预测（纯函数，供 analyze 与 predict 两个工具复用）
// -----------------------------------------------------------------------------
export function serverModsOf(mods) {
    return mods.filter((m) => m.type === "server");
}
export function predictLoadOrder(mods) {
    const servers = serverModsOf(mods).slice();
    servers.sort((a, b) => {
        const p = a.typePriority - b.typePriority;
        if (p !== 0)
            return p;
        const ga = (a.guid ?? "").toLowerCase();
        const gb = (b.guid ?? "").toLowerCase();
        if (ga !== gb)
            return ga < gb ? -1 : 1;
        return a.name.localeCompare(b.name);
    });
    return servers.map((m, i) => ({
        position: i + 1,
        name: m.name,
        guid: m.guid,
        typePriority: m.typePriority,
        path: m.path,
    }));
}
// -----------------------------------------------------------------------------
// 冲突检测
// -----------------------------------------------------------------------------
function detectGuidDuplicates(mods) {
    const byGuid = new Map();
    for (const m of serverModsOf(mods)) {
        if (!m.guid)
            continue;
        const key = m.guid.toLowerCase();
        const list = byGuid.get(key) ?? [];
        list.push(m);
        byGuid.set(key, list);
    }
    const findings = [];
    for (const [guid, group] of byGuid) {
        if (group.length < 2)
            continue;
        findings.push({
            severity: "B",
            kind: "guid_duplicate",
            message: `${group.length} 个 server mod 声明了相同的 ModGuid "${group[0].guid}"，SPT 会拒绝重复 GUID 的 mod`,
            mods: group.map((m) => m.name),
            detail: { guid: group[0].guid, paths: group.map((m) => m.path) },
        });
    }
    return findings;
}
function detectVersionMismatch(mods, target) {
    const findings = [];
    for (const m of serverModsOf(mods)) {
        const declared = m.sptVersion ?? m.compatibleVersion;
        if (!declared)
            continue;
        if (isCompatibleWithTarget(declared, target))
            continue;
        findings.push({
            severity: "B",
            kind: "spt_version_mismatch",
            message: `"${m.name}" 声明的 SPT 兼容版本 ${declared} 与目标版本 ${target} 不兼容`,
            mods: [m.name],
            detail: { declared, target, field: m.sptVersion ? "sptVersion" : "compatibleVersion" },
        });
    }
    return findings;
}
/**
 * 结构性公共文件忽略名单：这些文件是每个 mod 的"脚手架"组成部分，
 * 几乎所有 mod 都有且内容无关（MO2 元数据、TS 构建产物、标准清单），
 * 不应作为文件覆盖冲突报告。
 */
const IGNORED_OVERWRITE_PATHS = new Set([
    "package.json",
    "meta.ini",
    "bundles.json",
    "src/mod.ts",
    "src/mod.js",
    "src/mod.js.map",
    "src/types.ts",
    "src/InstanceManager.ts",
    ".buildignore",
    "LICENSE",
    "LICENSE.txt",
    "README.md",
    ".gitignore",
]);
function isIgnoredOverwritePath(relPath) {
    if (IGNORED_OVERWRITE_PATHS.has(relPath))
        return true;
    // 构建产物/源码目录内的重复文件（src/、dist/、obj/、bin/）跳过
    const lower = relPath.toLowerCase();
    if (lower.startsWith("src/") || lower.startsWith("dist/") || lower.startsWith("obj/") || lower.startsWith("bin/"))
        return true;
    return false;
}
function detectFileOverwrites(mods, filesByModPath) {
    if (!filesByModPath)
        return [];
    const byPath = new Map();
    for (const m of mods) {
        const files = filesByModPath[m.path];
        if (!files)
            continue;
        for (const f of files) {
            if (isIgnoredOverwritePath(f.relativePath))
                continue;
            const list = byPath.get(f.relativePath) ?? [];
            list.push({ name: m.name, modPath: m.path, sizeBytes: f.sizeBytes, hash: f.contentHash });
            byPath.set(f.relativePath, list);
        }
    }
    const findings = [];
    for (const [relPath, owners] of byPath) {
        const distinct = new Map(owners.map((o) => [o.modPath, o]));
        if (distinct.size < 2)
            continue;
        const ownerList = [...distinct.values()];
        // 内容指纹比较：全部 hash 相同 -> 无害重复（降级）；hash 缺失或不同 -> 真冲突
        const hashes = ownerList.map((o) => o.hash).filter((h) => typeof h === "string");
        const allSame = hashes.length > 0 && hashes.length === ownerList.length && new Set(hashes).size === 1;
        if (allSame) {
            // 同路径 + 内容完全一致：无害重复（如多个 mod 共用同一模板文件），提示但不阻断
            findings.push({
                severity: "O",
                kind: "duplicate_file",
                message: `${distinct.size} 个 mod 包含相同文件 "${relPath}"（内容一致，无害重复）`,
                mods: ownerList.map((o) => o.name),
                detail: { relativePath: relPath, paths: [...distinct.keys()], contentIdentical: true },
            });
            continue;
        }
        const knownHashes = new Set(hashes);
        findings.push({
            severity: "O",
            kind: "file_overwrite",
            message: `${distinct.size} 个 mod 包含相同文件 "${relPath}" 且内容不同（${knownHashes.size > 1 ? `${knownHashes.size} 种内容` : "部分 hash 未知"}），MO2 优先级仲裁谁覆盖谁，未胜出的内容被遮蔽`,
            mods: ownerList.map((o) => o.name),
            detail: { relativePath: relPath, paths: [...distinct.keys()], contentIdentical: false, hashCount: knownHashes.size },
        });
    }
    return findings;
}
function detectConfigCollisions(mods, configsByModPath) {
    if (!configsByModPath)
        return [];
    const byFile = new Map();
    for (const m of mods) {
        const configs = configsByModPath[m.path];
        if (!configs)
            continue;
        for (const c of configs) {
            const list = byFile.get(c.filename) ?? [];
            list.push({ name: m.name, modPath: m.path, keys: c.keys });
            byFile.set(c.filename, list);
        }
    }
    const findings = [];
    for (const [filename, owners] of byFile) {
        const distinct = new Map(owners.map((o) => [o.modPath, o]));
        if (distinct.size < 2)
            continue;
        const ownersList = [...distinct.values()];
        const overlap = intersectAll(ownersList.map((o) => o.keys));
        if (overlap.length === 0)
            continue;
        findings.push({
            severity: "O",
            kind: "config_collision",
            message: `${ownersList.length} 个 mod 的配置文件 "${filename}" 存在重叠 key：${overlap.join(", ")}`,
            mods: ownersList.map((o) => o.name),
            detail: { filename, overlappingKeys: overlap, paths: ownersList.map((o) => o.modPath) },
        });
    }
    return findings;
}
function intersectAll(keyLists) {
    if (keyLists.length === 0)
        return [];
    const [first, ...rest] = keyLists;
    const seen = new Set(first);
    for (const list of rest) {
        const next = new Set();
        for (const k of list) {
            if (seen.has(k))
                next.add(k);
        }
        seen.clear();
        for (const k of next)
            seen.add(k);
    }
    return [...seen].sort();
}
// -----------------------------------------------------------------------------
// IL 级冲突检测（Harmony patch 同目标方法被 2+ 插件截断）
// -----------------------------------------------------------------------------
/**
 * 检测 IL 级冲突：客户端 mod 的 Harmony patch 同目标方法被 2+ 插件截断。
 * 高危 = 同一 (targetType + targetMethod) 被 2+ 插件的 Prefix 返回 false 截断；
 * 中危 = 同方法被 2+ 插件 writesField / writesResult；
 * 低危 = 同方法被 2+ 插件 patch（Postfix / 只读，可共存）。
 */
export function detectIlConflicts(patches) {
    if (!patches || patches.length === 0)
        return [];
    // 按 (targetType, targetMethod) 分组
    const byTarget = new Map();
    for (const p of patches) {
        const key = `${p.targetType ?? "?"}|${p.targetMethod ?? "?"}`;
        const list = byTarget.get(key) ?? [];
        list.push(p);
        byTarget.set(key, list);
    }
    const findings = [];
    for (const [target, ps] of byTarget) {
        const plugins = [...new Set(ps.map((p) => p.pluginName))];
        if (plugins.length < 2)
            continue;
        const prefixRetFalse = ps.filter((p) => p.patchTypes.includes("Prefix") && p.behavior?.returnsFalse).length;
        const transpiler = ps.filter((p) => p.patchTypes.includes("Transpiler")).length;
        const writesField = ps.filter((p) => p.behavior?.writesField).length;
        const writesResult = ps.filter((p) => p.behavior?.writesResult).length;
        const [targetType, targetMethod] = target.split("|");
        const targetStr = targetMethod && targetMethod !== "?" ? `${targetType}.${targetMethod}` : targetType;
        if (prefixRetFalse >= 2 || transpiler > 0) {
            // 高危：多插件争相截断同一方法
            findings.push({
                severity: "I",
                kind: "il_high_truncation_race",
                message: `高危：${plugins.length} 个 mod 对同一方法 "${targetStr}" 有 ${prefixRetFalse} 个截断 patch（Prefix 返回 false），后执行者截断后，前面 patch 的修改可能失效`,
                mods: plugins,
                detail: { targetType, targetMethod, plugins, prefixRetFalse, transpiler },
            });
        }
        else if (writesField >= 2 || writesResult >= 2) {
            // 中危：多插件改同一字段/结果
            findings.push({
                severity: "I",
                kind: "il_medium_field_race",
                message: `中危：${plugins.length} 个 mod 对同一方法 "${targetStr}" 修改字段/结果（${writesField} 写字段 / ${writesResult} 写结果），可能互相覆盖`,
                mods: plugins,
                detail: { targetType, targetMethod, plugins, writesField, writesResult },
            });
        }
        else {
            // 低危：同方法被多插件 patch（Postfix / 只读，可共存）
            findings.push({
                severity: "I",
                kind: "il_low_shared_target",
                message: `低危：${plugins.length} 个 mod 都 patch 同一方法 "${targetStr}"（Postfix / 只读，顺序执行可共存）`,
                mods: plugins,
                detail: { targetType, targetMethod, plugins },
            });
        }
    }
    return findings;
}
// -----------------------------------------------------------------------------
// 主入口
// -----------------------------------------------------------------------------
export function analyzeConflicts(input) {
    const target = input.targetSptVersion ?? DEFAULT_TARGET_SPT_VERSION;
    const sections = {
        B: [],
        S: [],
        O: [],
        C: [],
        I: [],
        Unknown: [],
    };
    sections.B.push(...detectGuidDuplicates(input.mods));
    sections.B.push(...detectVersionMismatch(input.mods, target));
    sections.O.push(...detectFileOverwrites(input.mods, input.filesByModPath));
    sections.O.push(...detectConfigCollisions(input.mods, input.configsByModPath));
    sections.I.push(...detectIlConflicts(input.ilPatches));
    const all = [
        ...sections.B,
        ...sections.S,
        ...sections.O,
        ...sections.C,
        ...sections.I,
        ...sections.Unknown,
    ];
    return {
        sections,
        loadOrder: predictLoadOrder(input.mods),
        summary: {
            total: all.length,
            bySeverity: {
                B: sections.B.length,
                S: sections.S.length,
                O: sections.O.length,
                C: sections.C.length,
                I: sections.I.length,
                Unknown: sections.Unknown.length,
            },
        },
    };
}
//# sourceMappingURL=conflict-engine.js.map