// =============================================================================
// spt-mcp — 基于文件系统的 MCP server（无 daemon、无状态机、全部工具同步）
//
// 对比需要常驻 daemon + named pipe + 生命周期管理的分析型 harness（如 xEdit）：
// SPT mod 分析是纯文件操作 + 纯计算，所有工具同步返回，无需异步状态。
//
// 工具面（9 个）：
//   spt_list_mods            扫描目录列出 mod（server = package.json 子目录，client = .dll）
//   spt_read_mod_metadata    读单个 mod 完整元数据
//   spt_scan_mod_files       列出 mod 目录全部文件
//   spt_analyze_conflicts    冲突分析（GUID 重复 / SPT 版本失配 / 文件覆盖 / 配置 key 碰撞）
//   spt_predict_load_order   预测服务端 mod 加载顺序（TypePriority 升序 + GUID 字母序）
//   spt_forge_search         搜索 Forge 归档（mods-catalog.json + hot-index.json）
//   spt_kb_query             查知识库（多库：kb/topic/domain/version/keyword/limit）
//   spt_kb_list              知识库清单与自检（有哪些库、各管什么、索引是否过期）
//   spt_health               运行时布局只读自查（KB / archive / registry / helper 可用性）
//
// 运行时布局（KB 根 / 归档 / helper 产物）由 shared/runtime-layout.mjs 统一解析：
// 启动时把不可用资源写到 stderr，KB/Forge 工具在资源缺失时返回 kb_unavailable。
//
// C2 迁移（2026-09-16）：schema 管道（schemaFor）/ 结果包装（jsonResult）/
// stdio 引导（runStdioServer、runMain）已抽到共享内核 tools/mcp-kit；
// 本文件只保留工具定义表与 dispatch 逻辑。接线方式为相对 dist 导入（决策 D1）。
// =============================================================================

import { jsonResult, runMain, runStdioServer, schemaFor } from "../../mcp-kit/dist/index.js";

import { formatLayoutWarnings, getLayout } from "./runtime-layout.js";
import {
  errEnv,
  SPT_ERROR_CODES,
  type Envelope,
} from "./types.js";
import { AnalyzeConflictsInput, runAnalyzeConflicts } from "./tools/analyze-conflicts.js";
import { ForgeSearchInput, runForgeSearch } from "./tools/forge-search.js";
import { HealthInput, runHealth } from "./tools/health.js";
import { KbListInput, runKbList } from "./tools/kb-list.js";
import { KbQueryInput, runKbQuery } from "./tools/kb-query.js";
import { ListModsInput, runListMods } from "./tools/list-mods.js";
import { PredictLoadOrderInput, runPredictLoadOrder } from "./tools/predict-load-order.js";
import { ReadModMetadataInput, runReadModMetadata } from "./tools/read-mod-metadata.js";
import { ScanModFilesInput, runScanModFiles } from "./tools/scan-mod-files.js";

const SERVER_NAME = "spt-mcp";
const SERVER_VERSION = "0.2.0";

type ToolHandler = (args: Record<string, unknown>) => Envelope;

export const TOOL_DEFINITIONS = [
  {
    name: "spt_list_mods",
    description:
      "扫描目录列出 SPT mod。server = 遍历子目录读每个 package.json；client = 递归扫 .dll 文件（BepInEx/plugins）。返回 [{ name, version?, guid?, dependencies, path, type }].",
    inputSchema: schemaFor(ListModsInput),
  },
  {
    name: "spt_read_mod_metadata",
    description:
      "读单个 SPT mod 的完整元数据。server = 返回 package.json 内容与提取字段；client = 返回 DLL 文件名派生元数据（程序集级 GUID/版本需 .NET helper，本期延后）。",
    inputSchema: schemaFor(ReadModMetadataInput),
  },
  {
    name: "spt_scan_mod_files",
    description:
      "递归列出 mod 目录内全部文件，返回 [{ relativePath, sizeBytes }]（正斜杠相对路径）。",
    inputSchema: schemaFor(ScanModFilesInput),
  },
  {
    name: "spt_analyze_conflicts",
    description:
      "分析一组 SPT mod 的冲突（核心工具）。检测：B = ModGuid 重复、SPT 版本失配（对比目标 4.1）；O = 同路径文件覆盖、JSON 配置 key 碰撞。同时给出预测加载顺序（TypePriority 升序 + GUID 字母序）。",
    inputSchema: schemaFor(AnalyzeConflictsInput),
  },
  {
    name: "spt_predict_load_order",
    description:
      "预测 server mod 加载顺序：TypePriority 升序，同优先级按 ModGuid 字母序（SPT 4.1 ModLoader tiebreaker）。返回有序列表 [position, name, guid, typePriority, path]。",
    inputSchema: schemaFor(PredictLoadOrderInput),
  },
  {
    name: "spt_forge_search",
    description:
      "搜索 Forge 归档目录（knowledge/spt-kb/archive/forge/）。支持 query（标题/简介子串）、category（slug/标题）、sptVersion（hot-index best_spt 兼容性匹配）、modType（快照无该字段，当前无匹配）。按下载量降序。",
    inputSchema: schemaFor(ForgeSearchInput),
  },
  {
    name: "spt_kb_query",
    description:
      "查知识库（多库）。kb = 库 id（见 spt_kb_list），省略则跨全部已注册库检索；topic / domain / version 过滤与 keyword（匹配 title ∪ keywords）匹配；domain='both' 与 version='通用' 的条目始终包含。keyword 是索引字段，盖不住概念词时缺省会自动按正文兜底检索（matchedBy=content，命中带 contentHits/snippet），content 参数可强制/关闭。返回 data.results[] 逐库分组，每组含 kb / root（与 entry.path 拼绝对路径）/ authority（冲突时据此取舍）/ matchCount / truncated / stale / matchedBy 与 matches[]；limit 为每库返回上限（缺省 30）。库不可用时返回 kb_unavailable。",
    inputSchema: schemaFor(KbQueryInput),
  },
  {
    name: "spt_kb_list",
    description:
      "知识库清单与自检：返回每个已注册库的 id / title（用途）/ authority（权威范围）/ root / envKey / entryCount / generated / topics（可用的 topic 过滤值）/ stale（源文件比索引新 => 索引可能过期）。不知道该查哪个库、或不确定过滤参数时先调它；注册表不可用时退化为单库模式并在 reason 里说明。",
    inputSchema: schemaFor(KbListInput),
  },
  {
    name: "spt_health",
    description:
      "运行时布局只读自查：返回 mode（repo/portable）、pluginRoot，以及知识库根/索引/forge 归档与 metadata/IL helper 的 path、source（env/default）、ok、reason。用于定位知识层或 helper 缺失。",
    inputSchema: schemaFor(HealthInput),
  },
];

const HANDLERS: Record<string, ToolHandler> = {
  spt_list_mods: (args) => runListMods(args),
  spt_read_mod_metadata: (args) => runReadModMetadata(args),
  spt_scan_mod_files: (args) => runScanModFiles(args),
  spt_analyze_conflicts: (args) => runAnalyzeConflicts(args),
  spt_predict_load_order: (args) => runPredictLoadOrder(args),
  spt_forge_search: (args) => runForgeSearch(args),
  spt_kb_query: (args) => runKbQuery(args),
  spt_kb_list: (args) => runKbList(args),
  spt_health: () => runHealth(),
};

function invoke(name: string, args: Record<string, unknown>): Envelope {
  const handler = HANDLERS[name];
  if (!handler) {
    return errEnv(
      name,
      `未知工具：${name}`,
      SPT_ERROR_CODES.INVALID_REQUEST,
      {
        hint: "可用工具：spt_list_mods, spt_read_mod_metadata, spt_scan_mod_files, spt_analyze_conflicts, spt_predict_load_order, spt_forge_search, spt_kb_query, spt_health",
      },
    );
  }
  try {
    return handler(args);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    return errEnv(name, `内部错误：${message}`, SPT_ERROR_CODES.INTERNAL_ERROR, {
      hint: message,
    });
  }
}

export async function main(): Promise<void> {
  // 启动健康检查：布局不可用写 stderr（不退出进程，纯文件系统工具不受影响）
  const layout = getLayout();
  if (layout.warnings.length > 0) {
    process.stderr.write(formatLayoutWarnings(layout) + "\n");
  }

  await runStdioServer({
    name: SERVER_NAME,
    version: SERVER_VERSION,
    listTools: () => TOOL_DEFINITIONS,
    callTool: (name, args) => {
      const envelope = invoke(name, args);
      return jsonResult(envelope, !envelope.ok);
    },
  });
}

runMain(import.meta.url, main, SERVER_NAME);
