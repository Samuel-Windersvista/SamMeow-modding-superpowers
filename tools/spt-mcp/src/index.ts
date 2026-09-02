// =============================================================================
// spt-mcp — 基于文件系统的 MCP server（无 daemon、无状态机、全部工具同步）
//
// 与 xedit-mcp 的差异：xEdit 需要常驻 daemon + named pipe + 生命周期管理；
// SPT mod 分析是纯文件操作 + 纯计算，所有工具同步返回，无需异步状态。
//
// 工具面（7 个）：
//   spt_list_mods            扫描目录列出 mod（server = package.json 子目录，client = .dll）
//   spt_read_mod_metadata    读单个 mod 完整元数据
//   spt_scan_mod_files       列出 mod 目录全部文件
//   spt_analyze_conflicts    冲突分析（GUID 重复 / SPT 版本失配 / 文件覆盖 / 配置 key 碰撞）
//   spt_predict_load_order   预测服务端 mod 加载顺序（TypePriority 升序 + GUID 字母序）
//   spt_forge_search         搜索 Forge 归档（mods-catalog.json + hot-index.json）
//   spt_kb_query             查询 SPT 知识库（index.json）
// =============================================================================

import { Server } from "@modelcontextprotocol/sdk/server/index.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { CallToolRequestSchema, ListToolsRequestSchema } from "@modelcontextprotocol/sdk/types.js";
import { pathToFileURL } from "node:url";
import { zodToJsonSchema } from "zod-to-json-schema";

import {
  errEnv,
  SPT_ERROR_CODES,
  type Envelope,
} from "./types.js";
import { AnalyzeConflictsInput, runAnalyzeConflicts } from "./tools/analyze-conflicts.js";
import { ForgeSearchInput, runForgeSearch } from "./tools/forge-search.js";
import { KbQueryInput, runKbQuery } from "./tools/kb-query.js";
import { ListModsInput, runListMods } from "./tools/list-mods.js";
import { PredictLoadOrderInput, runPredictLoadOrder } from "./tools/predict-load-order.js";
import { ReadModMetadataInput, runReadModMetadata } from "./tools/read-mod-metadata.js";
import { ScanModFilesInput, runScanModFiles } from "./tools/scan-mod-files.js";

const SERVER_NAME = "spt-mcp";
const SERVER_VERSION = "0.1.0";

type ToolHandler = (args: Record<string, unknown>) => Envelope;

/** zod schema -> JSON Schema（剥离 $schema 顶层键，兼容严格 schema 后端） */
function schemaFor(schema: Parameters<typeof zodToJsonSchema>[0]): Record<string, unknown> {
  const json = zodToJsonSchema(schema, { target: "jsonSchema7" }) as Record<string, unknown>;
  delete json.$schema;
  return json;
}

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
      "查询 SPT 知识库（knowledge/spt-kb/index.json，71 条目）。支持 topic / domain / version 过滤与 keyword 标题匹配；domain='both' 与 version='通用' 的条目始终包含。",
    inputSchema: schemaFor(KbQueryInput),
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
};

function jsonResult(body: unknown, isError = false) {
  return {
    content: [{ type: "text" as const, text: JSON.stringify(body) }],
    isError,
  };
}

function invoke(name: string, args: Record<string, unknown>): Envelope {
  const handler = HANDLERS[name];
  if (!handler) {
    return errEnv(
      name,
      `未知工具：${name}`,
      SPT_ERROR_CODES.INVALID_REQUEST,
      "可用工具：spt_list_mods, spt_read_mod_metadata, spt_scan_mod_files, spt_analyze_conflicts, spt_predict_load_order, spt_forge_search, spt_kb_query",
    );
  }
  try {
    return handler(args);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    return errEnv(name, `内部错误：${message}`, SPT_ERROR_CODES.INTERNAL_ERROR, message);
  }
}

export async function main(): Promise<void> {
  const server = new Server(
    { name: SERVER_NAME, version: SERVER_VERSION },
    { capabilities: { tools: {} } },
  );

  server.setRequestHandler(ListToolsRequestSchema, async () => ({
    tools: TOOL_DEFINITIONS,
  }));

  server.setRequestHandler(CallToolRequestSchema, async (req) => {
    const name = req.params.name;
    const args = (req.params.arguments ?? {}) as Record<string, unknown>;
    const envelope = invoke(name, args);
    return jsonResult(envelope, !envelope.ok);
  });

  const shutdown = (signal: string) => {
    process.stderr.write(`${SERVER_NAME} 收到 ${signal}，正在关闭...\n`);
    process.exit(0);
  };
  process.on("SIGINT", () => shutdown("SIGINT"));
  process.on("SIGTERM", () => shutdown("SIGTERM"));

  await server.connect(new StdioServerTransport());
}

const invokedAsMain = (() => {
  const argv = process.argv[1];
  if (!argv) return false;
  try {
    return import.meta.url === pathToFileURL(argv).href;
  } catch {
    return false;
  }
})();

if (invokedAsMain) {
  main().catch((error) => {
    const message = error instanceof Error ? error.message : String(error);
    process.stderr.write(`${SERVER_NAME} 启动失败：${message}\n`);
    process.exit(1);
  });
}
