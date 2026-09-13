// =============================================================================
// tarkov_server_status：mods 字段（server-log 来源）与能力自报
//
// 只测外部行为：工具输出 schema、降级不拖垮整体、能力清单内容。
// 日志读取器以注入方式驱动；另有一条用真实 fixture 目录验证默认接线。
// =============================================================================

import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

import type { ServerModListResult } from "../../src/logs/log-reader.js";
import { SUPPORTED_SECTIONS } from "../../src/snapshot/schema.js";
import { createServerStatusTool } from "../../src/tools/server-status.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";

function connectedClient() {
  return fakeClient(new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32")));
}

const AVAILABLE_MODS: ServerModListResult = {
  source: "server-log",
  available: true,
  logFile: "C:/SPT/user/logs/spt/spt20260913.log",
  count: 1,
  declaredCount: 1,
  allModsRejected: false,
  mods: [
    {
      name: "MyMod",
      version: "1.2.3",
      guid: "com.example.mymod",
      author: "Author",
      targetsSpt: "~5.0.0",
    },
  ],
};

const UNAVAILABLE_MODS: ServerModListResult = {
  source: "server-log",
  available: false,
  reason: "log_directory_missing",
  message: "日志目录不存在或不可读：C:/SPT/user/logs/spt",
  logDir: "C:/SPT/user/logs/spt",
  logFile: null,
};

describe("tarkov_server_status.mods", () => {
  it("日志可用：mods 字段返回清单并标注 source=server-log", async () => {
    const tool = createServerStatusTool(connectedClient(), {
      logDir: "C:/SPT/user/logs/spt",
      readModList: async () => AVAILABLE_MODS,
    });

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as { mods: ServerModListResult };
    expect(data.mods).toEqual(AVAILABLE_MODS);
    expect(result.summary).toContain("server mod");
  });

  it("日志缺失：整体仍成功，mods 为结构化降级", async () => {
    const tool = createServerStatusTool(connectedClient(), {
      logDir: "C:/SPT/user/logs/spt",
      readModList: async () => UNAVAILABLE_MODS,
    });

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as { mods: ServerModListResult };
    expect(data.mods.available).toBe(false);
    if (data.mods.available) return;
    expect(data.mods.reason).toBe("log_directory_missing");
    expect(data.mods.source).toBe("server-log");
  });

  it("能力自报：列出已实现工具、sections 与 bridge 状态", async () => {
    const tool = createServerStatusTool(connectedClient(), {
      logDir: "C:/SPT/user/logs/spt",
      readModList: async () => UNAVAILABLE_MODS,
    });

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as {
      capabilities: { tools: string[]; sections: string[]; bridge: string };
    };
    expect(data.capabilities.tools).toEqual(
      expect.arrayContaining(["tarkov_server_status", "tarkov_instances"]),
    );
    expect(data.capabilities.sections).toEqual(expect.arrayContaining(["mods"]));
    expect(data.capabilities.bridge).toBe("not_installed");
  });

  it("能力自报：sections 覆盖全部已实现快照 section（traders/quests/hideout/inventory）", async () => {
    const tool = createServerStatusTool(connectedClient(), {
      logDir: "C:/SPT/user/logs/spt",
      readModList: async () => UNAVAILABLE_MODS,
    });

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as { capabilities: { sections: string[] } };
    expect(data.capabilities.sections).toEqual(expect.arrayContaining([...SUPPORTED_SECTIONS]));
  });

  it("读取器抛异常：整体仍成功，mods 降级为 log_unreadable", async () => {
    const tool = createServerStatusTool(connectedClient(), {
      logDir: "C:/SPT/user/logs/spt",
      readModList: async () => {
        throw new Error("EACCES");
      },
    });

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as { mods: ServerModListResult };
    expect(data.mods.available).toBe(false);
    if (data.mods.available) return;
    expect(data.mods.reason).toBe("log_unreadable");
  });

  it("默认接线：未注入读取器时从 logDir 的真实 fixture 解析", async () => {
    const logDir = fileURLToPath(new URL("../fixtures/logs/normal", import.meta.url));
    const tool = createServerStatusTool(connectedClient(), { logDir });

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    const data = result.data as { mods: ServerModListResult };
    expect(data.mods.available).toBe(true);
    if (!data.mods.available) return;
    expect(data.mods.count).toBe(3);
    expect(data.mods.mods.map((mod) => mod.name)).toEqual(["MyMod", "Other Mod", "SimpleMod"]);
  });
});
