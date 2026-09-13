// =============================================================================
// S2 日志解析器：SPT server 日志 -> 已加载 server mod 清单
//
// 背景（ADR-0003）：server mod 加载清单无路由可查，以解析 server 日志兜底，
// schema 中标注 source: "server-log"。
//
// 日志格式依据（SPT 5.0 源码，2026-09-13 快照 HEAD ff0bf3281）：
//   - 文件格式模板：`[%date% %time%][%level%][%logger%] %message%`
//     - 路径/文件名：`./user/logs/spt/` + `spt%DATE%.log`（`SPTushonka.Server/sptLogger.json`）
//     - 占位符展开：`SPTushonka.Common/Models/Logging/SptLoggerConfiguration.cs`（%logger% = 全名）
//     - 行拼装：`SPTushonka.Common/Logger/Handlers/BaseLogHandler.cs`
//   - 已加载 mod 行：`Mod: {name} version: {version} by: {author} loaded`
//     其中 version = `{Version} (GUID: {ModGuid} | targets SPT: {SptVersion})`
//     （`SPTushonka.Server/Modding/ModValidator.cs` AddMod + `locales/server/en.json`
//      的 `modloader-loaded_mod`）
//   - 加载开始标记：`ModLoader: loading: {n} server mods...`（`modloader-loading_mods`）
//   - 校验失败全部拒载：`Errors were found with mods, NO MODS WILL BE LOADED`
//     （`modloader-no_mods_loaded`）
//
// 容错策略：不匹配前缀的行按「裸 message」处理；无法解析的行直接忽略，绝不抛异常。
// 同一天日志会跨多次 server 启动追加，故以最后一次加载会话为准（遇 loading 标记清空）。
// =============================================================================

/** 从日志中解析出的单个已加载 server mod */
export interface LoadedServerMod {
  name: string;
  /** 语义化版本号；日志未提供时为 null */
  version: string | null;
  /** ModGuid；日志未提供时为 null */
  guid: string | null;
  author: string | null;
  /** mod 声明的 SPT 目标版本区间；日志未提供时为 null */
  targetsSpt: string | null;
}

export interface ParsedServerModList {
  /** 最后一次加载会话中记录的 mod（保持日志顺序） */
  mods: LoadedServerMod[];
  /** 日志声明的加载数量（`ModLoader: loading: N server mods...`）；未提供时为 null */
  declaredCount: number | null;
  /** 是否出现 `NO MODS WILL BE LOADED`（校验失败导致全部 mod 拒载） */
  allModsRejected: boolean;
}

/** `[date time][level][logger] message` 前缀 */
const LOG_PREFIX_RE = /^\[\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3}\]\[[^\]]*\]\[[^\]]*\] (.*)$/;

/** `Mod: {name} version: {version} by: {author} loaded` */
const LOADED_MOD_RE = /^Mod: (.+?) version: (.+?) by: (.+?) loaded$/;

/** `{version} (GUID: {guid} | targets SPT: {range})` */
const VERSION_DETAIL_RE = /^(.+?) \(GUID: (.+?) \| targets SPT: (.+?)\)$/;

/** `ModLoader: loading: {n} server mods...` */
const LOADING_MODS_RE = /^ModLoader: loading: (\d+) server mods\.\.\.$/;

const NO_MODS_MARKER = "NO MODS WILL BE LOADED";

/** 剥离日志行前缀，返回 message；无前缀时按裸 message 处理 */
function stripLogPrefix(line: string): string {
  const match = LOG_PREFIX_RE.exec(line);
  return match ? match[1] : line;
}

function parseLoadedMod(message: string): LoadedServerMod | null {
  const loaded = LOADED_MOD_RE.exec(message);
  if (!loaded) {
    return null;
  }
  const [, name, versionField, author] = loaded;
  const detail = VERSION_DETAIL_RE.exec(versionField);
  if (!detail) {
    return { name, version: versionField, guid: null, author, targetsSpt: null };
  }
  const [, version, guid, targetsSpt] = detail;
  return { name, version, guid, author, targetsSpt };
}

/**
 * 从 server 日志文本解析已加载 server mod 清单。
 * 纯函数：不触碰文件系统，畸形输入不抛异常。
 */
export function parseServerLogMods(content: string): ParsedServerModList {
  let mods: LoadedServerMod[] = [];
  let declaredCount: number | null = null;
  let allModsRejected = false;

  for (const rawLine of content.split(/\r?\n/)) {
    const message = stripLogPrefix(rawLine);

    const loading = LOADING_MODS_RE.exec(message);
    if (loading) {
      // 新的加载会话：清空上一轮结果（同一日志文件跨多次 server 启动追加）
      mods = [];
      declaredCount = Number.parseInt(loading[1], 10);
      allModsRejected = false;
      continue;
    }

    if (message.includes(NO_MODS_MARKER)) {
      mods = [];
      allModsRejected = true;
      continue;
    }

    const mod = parseLoadedMod(message);
    if (mod) {
      mods.push(mod);
    }
  }

  return { mods, declaredCount, allModsRejected };
}
