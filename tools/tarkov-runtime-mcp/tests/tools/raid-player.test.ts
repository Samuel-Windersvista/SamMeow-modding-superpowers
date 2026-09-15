import { describe, expect, it } from "vitest";

import { createRaidPlayerTool } from "../../src/tools/raid-player.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import {
  defaultBridgeInfo,
  fakeBridge,
  inRaidPlayer,
  unreachable,
} from "../helpers/fake-bridge.js";

describe("raid_player（BridgeConnection 驱动）", () => {
  it("在 raid：返回 ok 信封，data 含 position/rotation/pose/health/weapon/equipment/sampleAgeMs", async () => {
    const tool = createRaidPlayerTool(
      fakeBridge({
        player: inRaidPlayer({
          position: { x: 12.5, y: -3.25, z: 88 },
          rotation: { x: 180, y: -45 },
          pose: "crouch",
          sampleAgeMs: 123,
        }),
      }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.tool).toBe("raid_player");
    expect(result.data).toEqual({
      position: { x: 12.5, y: -3.25, z: 88 },
      rotation: { x: 180, y: -45 },
      pose: "crouch",
      health: {
        alive: true,
        total: 440,
        parts: {
          Head: 35,
          Chest: 85,
          Stomach: 70,
          LeftArm: 60,
          RightArm: 60,
          LeftLeg: 65,
          RightLeg: 65,
        },
      },
      weapon: {
        tpl: "5447a9cd4bdc2dbd208b4567",
        name: "Colt M4A1",
        ammoInMag: 30,
        ammoInChamber: 1,
      },
      equipment: [
        { slot: "Headwear", tpl: "5aa7e276e5b5b000171d0647", name: "Altyn helmet" },
        { slot: "Armor", tpl: "545cdb794bdc2d3a198b456a", name: "6B43 Zabralo" },
      ],
      sampleAgeMs: 123,
    });
  });

  it("无武器：weapon 输出 null；无装备：equipment 输出空数组", async () => {
    const tool = createRaidPlayerTool(
      fakeBridge({ player: inRaidPlayer({ weapon: null, equipment: [] }) }),
    );

    const result = await tool({});

    expect(result.ok).toBe(true);
    if (!result.ok) return;
    expect(result.data).toMatchObject({ weapon: null, equipment: [] });
  });

  it("不在 raid：返回 NOT_IN_RAID 错误信封", async () => {
    const tool = createRaidPlayerTool(fakeBridge({ player: { inRaid: false } }));

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.NOT_IN_RAID);
    expect(result.tool).toBe("raid_player");
  });

  it("bridge 不可达（info 失败）：返回 BRIDGE_UNREACHABLE", async () => {
    const tool = createRaidPlayerTool(fakeBridge({ info: unreachable("connect ECONNREFUSED") }));

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
    expect(result.message).toContain("未安装");
    expect(result.message).toContain("BepInEx");
    expect(result.tool).toBe("raid_player");
  });

  it("bridge 不可达（端点失败）：返回 BRIDGE_UNREACHABLE", async () => {
    const tool = createRaidPlayerTool(
      fakeBridge({ info: defaultBridgeInfo(), player: unreachable("socket hang up") }),
    );

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_UNREACHABLE);
  });

  it("协议版本不一致：返回 BRIDGE_VERSION_MISMATCH（含 expected/actual）", async () => {
    const tool = createRaidPlayerTool(
      fakeBridge({ info: defaultBridgeInfo({ protocolVersion: 2 }) }),
    );

    const result = await tool({});

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.BRIDGE_VERSION_MISMATCH);
    expect(result.details).toEqual({ expected: 1, actual: 2 });
  });

  it("输出确定性：相同输入产生完全相同的 JSON 与稳定字段序", async () => {
    const tool = createRaidPlayerTool(fakeBridge({ player: inRaidPlayer() }));

    const first = await tool({});
    const second = await tool({});

    expect(JSON.stringify(first)).toBe(JSON.stringify(second));
    if (!first.ok) return;
    expect(Object.keys(first.data as object)).toEqual([
      "position",
      "rotation",
      "pose",
      "health",
      "weapon",
      "equipment",
      "sampleAgeMs",
    ]);
    const health = (first.data as { health: object }).health;
    expect(Object.keys(health)).toEqual(["alive", "total", "parts"]);
    expect(Object.keys((health as { parts: object }).parts)).toEqual([
      "Head",
      "Chest",
      "Stomach",
      "LeftArm",
      "RightArm",
      "LeftLeg",
      "RightLeg",
    ]);
    const data = first.data as { weapon: object; equipment: object[] };
    expect(Object.keys(data.weapon)).toEqual(["tpl", "name", "ammoInMag", "ammoInChamber"]);
    expect(Object.keys(data.equipment[0])).toEqual(["slot", "tpl", "name"]);
  });

  it("非法输入：返回 INVALID_INPUT", async () => {
    const tool = createRaidPlayerTool(fakeBridge());

    const result = await tool({ unexpected: true });

    expect(result.ok).toBe(false);
    if (result.ok) return;
    expect(result.code).toBe(RUNTIME_ERROR_CODES.INVALID_INPUT);
  });
});
