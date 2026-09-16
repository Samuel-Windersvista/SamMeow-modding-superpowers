// 生成 knowledge/spt-kb/curated/modding-standard/rules.json（C8 车道 A 步骤 1）
//
// 输入：rules-extracted.json（prose 机械提取的 84 条元数据，逐字）
//      CHECK_ORDER（29 条带检查的规则，顺序 = 现检查器输出顺序）
//      CHECKS（规则 ID -> check 规格；所有规则常量（正则/取值/消息）都落在这里）
//
// 输出顺序：带检查的 29 条（检查器输出顺序）+ 其余 55 条（prose 顺序）。
//   —— 检查器按 registry 顺序执行，输出即与改造前逐字一致。
//
// 用法: node .scratch/c8-rules-registry/tools/build-rules-json.mjs [--write]
import { readFileSync, writeFileSync } from "node:fs";
import { resolve } from "node:path";

const OUT = "knowledge/spt-kb/curated/modding-standard/rules.json";

// 13 维度 -> 章节文件（与 prose 的 `**Domain slug:**` 一致）
const DOMAINS = {
  STRUCT: "01-structure.md",
  META: "02-metadata.md",
  BUILD: "03-build.md",
  SRV: "04-server.md",
  CLI: "05-client.md",
  CFG: "06-config.md",
  LOG: "07-logging.md",
  DEP: "08-dependencies.md",
  PKG: "09-packaging.md",
  VERIFY: "10-verification.md",
  VER: "11-version-differences.md",
  BND: "12-bundle-assets.md",
  PERF: "13-perf-security.md",
};

// 检查器输出顺序（29 条带检查的规则；STD-VER-001 由 META-004 的检查一并发射）
const CHECK_ORDER = [
  "STD-STRUCT-002",
  "STD-STRUCT-005",
  "STD-STRUCT-006",
  "STD-META-005",
  "STD-BUILD-005",
  "STD-BUILD-006",
  "STD-LOG-001",
  "STD-BUILD-001",
  "STD-BUILD-004",
  "STD-META-001",
  "STD-META-002",
  "STD-META-003",
  "STD-META-004",
  "STD-VER-002",
  "STD-SRV-001",
  "STD-SRV-002",
  "STD-SRV-003",
  "STD-SRV-008",
  "STD-CFG-003",
  "STD-CFG-004",
  "STD-CFG-002",
  "STD-CFG-005",
  "STD-BUILD-002",
  "STD-CLI-001",
  "STD-META-006",
  "STD-CLI-003",
  "STD-CLI-007",
  "STD-CLI-006",
  "STD-CFG-006",
];

const CHECKS = {
  // ---- both kinds -----------------------------------------------------------
  "STD-STRUCT-002": {
    kind: "both",
    handler: "no-files-by-extension",
    params: {
      extensions: [".ts", ".js", ".tsx", ".jsx"],
      messages: { detail: "forbidden JS/TS files: {count}" },
    },
    // prose 该 ID 的语义是「禁止提交 bin/obj 构建产物」；检查器实际检查的是
    // 「不得存在 TS/JS 源文件」。prose 13 章无任何 TS/JS 规则（已核对）。
    note: "prose↔检查器语义不一致：prose STD-STRUCT-002 = 禁止提交 bin/obj；检查器实现的是「禁 TS/JS 源文件」。为保持 wire ID/顺序不变而沿用该 ID，待 prose 侧决策（补规则或改检查器编号）。",
  },
  "STD-STRUCT-005": {
    kind: "both",
    handler: "repo-file-exists",
    params: { names: ["README.md"] },
  },
  "STD-STRUCT-006": {
    kind: "both",
    handler: "repo-file-exists",
    params: { names: ["LICENSE", "LICENSE.md"] },
  },
  "STD-META-005": {
    kind: "both",
    handler: "meta-005-version-semver",
    params: {
      pattern: "^\\d+\\.\\d+\\.\\d+(-[0-9A-Za-z.-]+)?(\\+[0-9A-Za-z.-]+)?$",
      messages: {
        missing: "no <Version> in csproj or Directory.Build.props",
        placeholder: "placeholder version '{value}' ({source}), instantiate then re-check",
        detail: "version='{value}' ({source})",
      },
    },
  },
  "STD-BUILD-005": {
    kind: "both",
    handler: "proj-regex-present",
    params: {
      patterns: ["<AppendTargetFrameworkToOutputPath>\\s*false\\s*<"],
      sources: ["proj"],
    },
  },
  "STD-BUILD-006": {
    kind: "both",
    handler: "proj-regex-present",
    params: {
      patterns: ["<SPTInstallPath\\s+Condition="],
      sources: ["proj", "props"],
    },
  },
  "STD-LOG-001": {
    kind: "both",
    handler: "src-regex-absent",
    params: { pattern: "Console\\.Write" },
    // prose LOG-001 = 「构造函数注入 ISptLogger<T>…不得使用 Console.WriteLine 或自建静态日志器」。
    // 检查器只实现其否定半段（无 Console.Write）；肯定半段（ISptLogger<T>）由 STD-SRV-008 覆盖。
    note: "部分覆盖：prose STD-LOG-001 的肯定半段（构造函数注入 ISptLogger<T>）由 STD-SRV-008 承担；本检查只覆盖「不得 Console.Write」半段。",
  },

  // ---- server ---------------------------------------------------------------
  "STD-BUILD-001": {
    kind: "server",
    handler: "proj-value-equals",
    params: {
      name: "TargetFramework",
      value: "net10.0",
      messages: { detail: "TargetFramework='{value}' ({source})" },
    },
  },
  "STD-BUILD-004": {
    kind: "server",
    handler: "proj-refs-present",
    params: {
      refs: ["SPTarkov.Server.Core", "SPTarkov.DI", "SPTarkov.Common"],
      messages: { missing: "missing: {list}", ok: "core refs ok" },
    },
  },
  "STD-META-001": {
    kind: "server",
    handler: "src-regex-count-equals",
    params: {
      pattern: ":\\s*IModMetadata",
      count: 1,
      messages: { detail: "IModMetadata implementations={count}" },
    },
  },
  "STD-META-002": {
    kind: "server",
    handler: "meta-file-contains",
    params: { fileName: "ModMetadata.cs", pattern: "IModMetadata" },
  },
  "STD-META-003": {
    kind: "server",
    handler: "meta-003-guid",
    params: {
      pattern: "ModGuid\\s*\\{[^}]*\\}\\s*=\\s*\"([^\"]+)\"",
      guidPattern: "^[a-z0-9][a-z0-9-]*(\\.[a-z0-9][a-z0-9-]*)+$",
      messages: {
        missing: "ModGuid default not found",
        placeholder: "placeholder guid, instantiate then re-check",
        detail: "guid='{value}'",
      },
    },
  },
  "STD-META-004": {
    kind: "server",
    handler: "sptversion-range",
    params: {
      pattern: "SptVersion[\\s\\S]{0,300}?new\\s*(?:Range)?\\s*\\(\"~(\\d+)\\.(\\d+)\\.(\\d+)\"\\)",
      targetPattern: "^(\\d+)\\.(\\d+)\\.(\\d+)$",
      // 本检查发射两条结果：自身的 STD-META-004 + 检查器专用 ID STD-VER-001
      // （prose 无 STD-VER-001；11-version-differences.md 明确 tilde 区间声明指向
      //  STD-META-004 且本维度从 VER-002 起编号）。
      emits: ["STD-META-004", "STD-VER-001"],
      messages: {
        missing: 'no tilde Range("~x.y.z") SptVersion found',
        detail: "range ~{major}.{minor}.{patch}",
        verMissing: "no SptVersion range to compare",
        verUnparseable: "unparseable -TargetSptVersion '{target}'",
        verDetail: "target={target} vs range ~{major}.{minor}.{patch}",
      },
    },
    note: "STD-VER-001 为检查器专用 ID：prose 无对应规则（11-version-differences.md 注：tilde 区间声明见 STD-META-004，本维度自 VER-002 起）。已登记为待决，未在 prose 侧静默补齐。",
  },
  "STD-VER-002": {
    kind: "server",
    handler: "ver-002-version-single-source",
    params: {
      metadataVersionPattern: "Version Version\\s*\\{[^}]*\\}\\s*=\\s*new\\(\"([^\"]+)\"\\)",
      singleSourcePattern: "ModVersion\\.Value",
      messages: {
        placeholder: "placeholder version, instantiate then re-check",
        detail: "csproj='{csproj}' metadata='{metadata}'",
        singleSource: "single-source version via generated ModVersion.Value (paired linkage)",
        missing: "csproj or ModMetadata version not found",
      },
    },
    // prose STD-VER-002 = 「复用 4.1 的 mod 骨架」；检查器实现的是「csproj 与元数据版本同源」。
    note: "prose↔检查器语义不一致：prose STD-VER-002 = 复用 4.1 骨架；检查器实现的是「csproj <Version> 与 ModMetadata 版本同源」。prose 无同源规则（META-007 只覆盖 paired 两端同版本）。为保持 wire ID/顺序沿用，待 prose 侧决策。",
  },
  "STD-SRV-001": {
    kind: "server",
    handler: "src-regex-present",
    params: { pattern: "\\[Injectable" },
  },
  "STD-SRV-002": {
    kind: "server",
    handler: "src-regex-present",
    params: { pattern: "TypePriority\\s*=\\s*OnLoadOrder\\." },
  },
  "STD-SRV-003": {
    kind: "server",
    handler: "src-regex-present",
    params: { pattern: "Task OnLoadAsync\\(\\s*CancellationToken" },
  },
  "STD-SRV-008": {
    kind: "server",
    handler: "src-regex-present",
    params: { pattern: "ISptLogger<" },
  },
  "STD-CFG-003": {
    kind: "server",
    handler: "src-regex-present",
    params: { pattern: "IOnDIConstruct" },
  },
  "STD-CFG-004": {
    kind: "server",
    handler: "cfg-004-injectable-config",
    params: {
      pattern: "(?ms)\\[Injectable[^\\]]*\\]\\s*(?:public\\s+)?(?:sealed\\s+)?(?:class|record)\\s+(\\w+)",
      namePattern: "(Config|Configuration)$",
      messages: {
        bad: "[Injectable] on config class: {list}",
        ok: "config classes clean",
      },
    },
  },
  "STD-CFG-002": {
    kind: "server",
    handler: "mod-file-exists",
    params: { names: ["config/config.jsonc"] },
  },
  "STD-CFG-005": {
    kind: "server",
    handler: "mod-file-exists",
    params: { names: ["config/defaultConfig.jsonc"] },
  },

  // ---- client ---------------------------------------------------------------
  "STD-BUILD-002": {
    kind: "client",
    handler: "proj-value-in",
    params: {
      name: "TargetFramework",
      allowed: ["netstandard2.1", "net472", "net471", "net46", "net461", "net462", "net6.0"],
      messages: { detail: "TargetFramework='{value}' ({source})" },
    },
  },
  "STD-CLI-001": {
    kind: "client",
    handler: "cli-001-plugin-base",
    params: {
      primaryPattern: ": BaseUnityPlugin",
      secondaryPattern: "BasePlugin",
      messages: {
        primary: "BaseUnityPlugin (BepInEx 5 / 4.1.5)",
        secondary: "IL2CPP BasePlugin (BepInEx 6 / 5.0)",
        missing: "no BaseUnityPlugin/BasePlugin inheritance found",
      },
    },
  },
  "STD-META-006": {
    kind: "client",
    handler: "src-regex-present",
    params: { pattern: "\\[BepInPlugin\\([^)]*,[^)]*,[^)]*\\)" },
  },
  "STD-CLI-003": {
    kind: "client",
    handler: "src-regex-present",
    params: { pattern: "\\[HarmonyPatch\\(\\s*typeof\\(" },
  },
  "STD-CLI-007": {
    kind: "client",
    handler: "cli-007-harmony-lifecycle",
    params: {
      usagePatterns: ["new\\s+Harmony\\s*\\(", "HarmonyLib\\.Harmony", "\\bPatchAll\\s*\\("],
      patchPattern: "\\.PatchAll\\s*\\(",
      unpatchPatterns: ["UnpatchSelf", "override\\s+bool\\s+Unload\\s*\\("],
      messages: {
        na: "no Harmony usage; rule not applicable",
        detail: "harmony={harmony} patchAll={patch} unpatch={unpatch}",
      },
    },
  },
  "STD-CLI-006": {
    kind: "client",
    handler: "src-regex-present",
    params: { pattern: "Logger\\.Log(Info|Warning|Error|Debug|Message)" },
  },
  "STD-CFG-006": {
    kind: "client",
    handler: "src-regex-present",
    params: { pattern: "\\.Bind\\s*\\(" },
  },
};

// ---- 组装 -------------------------------------------------------------------

const extracted = JSON.parse(
  readFileSync(resolve(".scratch/c8-rules-registry/rules-extracted.json"), "utf8"),
);
const byId = new Map(extracted.map((r) => [r.id, r]));

const missing = CHECK_ORDER.filter((id) => !byId.has(id));
if (missing.length) throw new Error(`CHECK_ORDER 中的 ID 不在 prose：${missing.join(", ")}`);

const checkIds = new Set(Object.keys(CHECKS));
const orderSet = new Set(CHECK_ORDER);
if (checkIds.size !== orderSet.size) throw new Error("CHECKS 与 CHECK_ORDER 数量不一致");
for (const id of checkIds) if (!orderSet.has(id)) throw new Error(`CHECKS 含未排序 ID: ${id}`);

const ordered = CHECK_ORDER.map((id) => {
  const rule = byId.get(id);
  return {
    id: rule.id,
    domain: rule.domain,
    level: rule.level,
    applies: rule.applies,
    title: rule.title,
    checkable: true,
    check: CHECKS[id],
  };
});

const rest = extracted
  .filter((r) => !checkIds.has(r.id))
  .map((r) => ({
    id: r.id,
    domain: r.domain,
    level: r.level,
    applies: r.applies,
    title: r.title,
    checkable: false,
    check: null,
  }));

const registry = {
  schema_version: 1,
  generated: "2026-09-16",
  source: "knowledge/spt-kb/curated/modding-standard/*.md",
  domains: DOMAINS,
  rules: [...ordered, ...rest],
};

const byLevel = {};
for (const r of registry.rules) byLevel[r.level] = (byLevel[r.level] ?? 0) + 1;
const checkable = registry.rules.filter((r) => r.checkable).length;
const emitted = registry.rules
  .filter((r) => r.checkable)
  .flatMap((r) => r.check?.params?.emits ?? [r.id]);

console.log(`rules=${registry.rules.length} checkable=${checkable} emitted_ids=${emitted.length}`);
console.log(`byLevel=${JSON.stringify(byLevel)}`);
console.log(`emitted=${emitted.join(", ")}`);
console.log(`checker-only emitted ids: ${emitted.filter((id) => !byId.has(id)).join(", ") || "(none)"}`);

if (process.argv.includes("--write")) {
  writeFileSync(resolve(OUT), JSON.stringify(registry, null, 2) + "\n", "utf8");
  console.log(`written -> ${OUT}`);
} else {
  console.log("dry-run（加 --write 落盘）");
}
