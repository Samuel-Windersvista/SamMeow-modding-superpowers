import { describe, expect, it } from "vitest";

import { TOOL_DEFINITIONS, createDispatcher } from "../../src/index.js";
import { RAID_TOOL_NAMES } from "../../src/tools/raid.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";

function dispatcher() {
  return createDispatcher(
    fakeClient(new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32"))),
  );
}

describe("raid.* 占位命名空间", () => {
  for (const name of RAID_TOOL_NAMES) {
    it(`${name} 返回结构化 CLIENT_BRIDGE_NOT_INSTALLED`, async () => {
      const result = await dispatcher()(name, {});

      expect(result.ok).toBe(false);
      if (result.ok) return;
      expect(result.code).toBe(RUNTIME_ERROR_CODES.CLIENT_BRIDGE_NOT_INSTALLED);
      expect(result.tool).toBe(name);
    });
  }

  it("未注册的 raid.* 前缀调用同样返回 CLIENT_BRIDGE_NOT_INSTALLED", async () => {
    const result = await dispatcher()("raid_teleport", {});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.CLIENT_BRIDGE_NOT_INSTALLED);
  });

  it("工具定义包含全部 raid.* 占位工具", () => {
    const names = TOOL_DEFINITIONS.map((tool) => tool.name);
    for (const name of RAID_TOOL_NAMES) {
      expect(names).toContain(name);
    }
  });

  it("非 raid 的未知工具返回 INVALID_INPUT", async () => {
    const result = await dispatcher()("totally_unknown", {});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
  });
});
