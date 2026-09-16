// =============================================================================
// runtime-layout.mjs — 运行时布局解析（唯一计算点）
//
// 无依赖纯 ESM：OpenCode 插件（.opencode/plugins/）与 spt-mcp（tools/spt-mcp/）
// 各自相对导入本模块，路径解析不再各自计算、不会互相漂移。
//
// 契约（见 docs/adr/0008-runtime-layout-resolution.md）：
//   - 不抛错，返回逐资源报告（path / source / ok / reason）。
//   - env 未设 -> 默认派生（包根相对）+ 存在性校验。
//   - env 显式设置但校验失败 -> ok:false + reason，**不回退**（显式契约不可被
//     悄悄替换）。
//   - mode 仅为标签：包根含 .git 即 'repo'，否则 'portable'。
// =============================================================================

import { existsSync, statSync } from "node:fs";
import { join } from "node:path";

/** 目录是否存在 */
function isDirectory(p) {
  try {
    return statSync(p).isDirectory();
  } catch {
    return false;
  }
}

/** 文件是否存在 */
function isFile(p) {
  try {
    return statSync(p).isFile();
  } catch {
    return false;
  }
}

/** 读 env 键：非空字符串才算「显式设置」，空串/未定义按未设处理 */
function envValue(env, key) {
  const value = env ? env[key] : undefined;
  return typeof value === "string" && value.length > 0 ? value : null;
}

/** env 显式设置但无效的统一 reason（显式契约不可被悄悄替换） */
function envInvalidReason(envKey, path, kind) {
  return `env ${envKey} 指向的${kind}不存在：${path}（显式设置无效即报错，不回退）`;
}

/** 构造资源状态：ok 时无 reason；!ok 时必带 reason */
function makeStatus(path, source, ok, reason) {
  return ok ? { path, source, ok: true } : { path, source, ok: false, reason };
}

/**
 * env 覆盖 + 默认派生 + 存在性校验（KB 根与两个 helper 共用同一形态）。
 * env 非空 -> source 'env'，校验失败即报错（不回退）；未设 -> 默认路径 + 校验。
 *
 * @param {object} spec
 * @param {NodeJS.ProcessEnv} spec.env 环境变量源
 * @param {string} spec.envKey 覆盖用 env 键
 * @param {string} spec.defaultPath 未设时的默认路径
 * @param {string} spec.kind env 文案里的资源量词（'路径' / '文件'）
 * @param {(p: string) => boolean} spec.check 存在性校验
 * @param {(p: string) => string} spec.defaultReason 默认路径校验失败时的 reason
 * @returns {import("./runtime-layout.d.mts").ResourceStatus}
 */
function resolveEnvOrDefault({ env, envKey, defaultPath, kind, check, defaultReason }) {
  const envPath = envValue(env, envKey);
  const source = envPath ? "env" : "default";
  const path = envPath ?? defaultPath;
  if (check(path)) return makeStatus(path, source, true);
  const reason = envPath ? envInvalidReason(envKey, path, kind) : defaultReason(path);
  return makeStatus(path, source, false, reason);
}

/**
 * 解析运行时布局。
 *
 * @param {string} pluginRoot 插件包根绝对路径
 * @param {NodeJS.ProcessEnv} [env] 环境变量源（默认 process.env；测试可注入）
 * @returns {import("./runtime-layout.d.mts").RuntimeLayout}
 */
export function resolveRuntimeLayout(pluginRoot, env = process.env) {
  const warnings = [];

  /** 记录状态：!ok 时把 reason 汇入 warnings（顺序即资源顺序） */
  function record(status) {
    if (!status.ok) warnings.push(status.reason);
    return status;
  }

  // mode 仅为标签：.git 存在即 'repo'（worktree 形态下 .git 是文件，故用 existsSync）
  const mode = existsSync(join(pluginRoot, ".git")) ? "repo" : "portable";

  // ---- 知识库根 ------------------------------------------------------------
  const kbRoot = record(
    resolveEnvOrDefault({
      env,
      envKey: "SPT_KB_ROOT",
      defaultPath: join(pluginRoot, "knowledge", "spt-kb"),
      kind: "路径",
      check: isDirectory,
      defaultReason: (p) => `知识库根目录不存在：${p}（默认派生自插件包根 ${pluginRoot}）`,
    }),
  );

  // ---- KB 索引（source 继承 KB 根） ----------------------------------------
  const kbIndexPath = join(kbRoot.path, "index.json");
  const kbIndex = record(
    makeStatus(
      kbIndexPath,
      kbRoot.source,
      isFile(kbIndexPath),
      kbRoot.ok
        ? `知识库索引缺失：${kbIndexPath}（知识库根：${kbRoot.path}）`
        : `知识库索引缺失：${kbIndexPath}（知识库根不可用：${kbRoot.path}）`,
    ),
  );

  // ---- forge 归档（便携包不携带；目录 + API 快照文件三者齐备才算可用） -----
  // 只校验目录会放过「目录在、数据缺」的形态（本机实测：archive/forge 下只有
  // 源码克隆，api/mods-catalog.json 与 hot-index.json 不在），forge 工具会静默
  // 返回 0 匹配。因此这里做文件级校验，reason 点名缺失项。
  const kbArchivePath = join(kbRoot.path, "archive", "forge");
  const kbArchiveDirOk = isDirectory(kbArchivePath);
  const kbArchiveMissing = [];
  let kbArchiveReason;
  if (!kbArchiveDirOk) {
    kbArchiveReason = `forge 归档未随包分发或缺失：${kbArchivePath}`;
  } else {
    for (const rel of ["api/mods-catalog.json", "hot-index.json"]) {
      const full = join(kbArchivePath, rel);
      if (!isFile(full)) kbArchiveMissing.push(full);
    }
    if (kbArchiveMissing.length > 0) {
      kbArchiveReason =
        `forge 归档数据缺失：${kbArchiveMissing.join("、")}` +
        `（归档目录在但 API 快照不在，请按 scripts/spt-kb 流程刷新）`;
    }
  }
  const kbArchive = record(
    makeStatus(
      kbArchivePath,
      kbRoot.source,
      kbArchiveDirOk && kbArchiveMissing.length === 0,
      kbArchiveReason,
    ),
  );

  // ---- .NET helper 产物（打包前置条件，不自动构建） ------------------------
  const helpers = {};
  const helperSpecs = [
    {
      key: "metadata",
      envKey: "SPT_MCP_HELPER",
      path: join(pluginRoot, "tools", "spt-mcp", "helper", "bin", "Release", "spt-metadata-reader.exe"),
      build: "dotnet build tools/spt-mcp/helper -c Release",
    },
    {
      key: "il",
      envKey: "SPT_IL_HELPER",
      path: join(pluginRoot, "tools", "spt-mcp", "il-helper", "bin", "Release", "spt-il-reader.exe"),
      build: "dotnet build tools/spt-mcp/il-helper -c Release",
    },
  ];

  for (const spec of helperSpecs) {
    helpers[spec.key] = record(
      resolveEnvOrDefault({
        env,
        envKey: spec.envKey,
        defaultPath: spec.path,
        kind: "文件",
        check: isFile,
        defaultReason: (p) => `helper 产物未构建：${p}（构建：${spec.build}）`,
      }),
    );
  }

  return {
    mode,
    pluginRoot,
    kb: { root: kbRoot, index: kbIndex, archive: kbArchive },
    helpers: { metadata: helpers.metadata, il: helpers.il },
    warnings,
  };
}

/**
 * 把布局警告格式化为多行文本（供 stderr 输出）。无警告返回空串。
 *
 * @param {import("./runtime-layout.d.mts").RuntimeLayout} layout
 * @returns {string}
 */
export function formatLayoutWarnings(layout) {
  const warnings = layout && Array.isArray(layout.warnings) ? layout.warnings : [];
  if (warnings.length === 0) return "";
  const lines = [`[spt-runtime-layout] 检测到 ${warnings.length} 项运行时资源不可用：`];
  for (const warning of warnings) {
    lines.push(`  - ${warning}`);
  }
  return lines.join("\n");
}
