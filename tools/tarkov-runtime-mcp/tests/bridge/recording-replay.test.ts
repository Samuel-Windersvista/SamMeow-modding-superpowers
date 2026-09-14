// T07 录制/回放：JSONL 录制器 + 回放连接的确定性回归。
//
// 接缝：BridgeConnection（Phase 2 唯一新接缝）。录制器装饰任意 BridgeConnection，
// 回放器从 JSONL 构造 BridgeConnection，工具层无需感知。

import { mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join } from "node:path";

import { afterEach, describe, expect, it } from "vitest";

import { EXPECTED_BRIDGE_PROTOCOL_VERSION } from "../../src/bridge/connection.js";
import { RecordingBridgeConnection } from "../../src/bridge/recording.js";
import { loadReplayConnection, parseRecordingLine } from "../../src/bridge/replay.js";
import { loadConfig } from "../../src/config.js";
import { createDispatcher, createRuntime } from "../../src/index.js";
import { defaultBridgeInfo, fakeBridge, inRaidPlayer, inRaidStatus, unreachable } from "../helpers/fake-bridge.js";
import { FakeConnection, fakeClient, versionResponse } from "../helpers/fake-connection.js";

const RECORDED_TS = "2026-09-14T12:00:00.000Z";

const tempDirs: string[] = [];

function tempPath(name = "recording.jsonl"): string {
  const dir = mkdtempSync(join(tmpdir(), "tarkov-runtime-rec-"));
  tempDirs.push(dir);
  return join(dir, name);
}

afterEach(() => {
  for (const dir of tempDirs.splice(0)) {
    rmSync(dir, { recursive: true, force: true });
  }
});

function readEntries(path: string): Array<Record<string, unknown>> {
  return readFileSync(path, "utf8")
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter(Boolean)
    .map((line) => JSON.parse(line) as Record<string, unknown>);
}

function writeRecording(path: string, entries: unknown[]): void {
  writeFileSync(path, `${entries.map((entry) => JSON.stringify(entry)).join("\n")}\n`, "utf8");
}

function serverDispatcher(bridge = fakeBridge()) {
  return createDispatcher(
    fakeClient(new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32"))),
    bridge,
  );
}

describe("录制（RecordingBridgeConnection）", () => {
  it("每次调用追加一行 JSONL，含 ts / method / args / ok / result", async () => {
    const path = tempPath();
    const recorder = new RecordingBridgeConnection(
      fakeBridge({ status: inRaidStatus({ map: "Customs", raidId: "raid-7" }) }),
      path,
      { now: () => new Date(RECORDED_TS) },
    );

    await recorder.getInfo();
    await recorder.getRaidStatus();
    await recorder.getRaidBots(true);

    const entries = readEntries(path);
    expect(entries).toHaveLength(3);
    expect(entries[0]).toMatchObject({ ts: RECORDED_TS, method: "getInfo", args: null, ok: true });
    expect(entries[0].result).toMatchObject({
      protocolVersion: EXPECTED_BRIDGE_PROTOCOL_VERSION,
    });
    expect(entries[1]).toMatchObject({ ts: RECORDED_TS, method: "getRaidStatus", args: null, ok: true });
    expect(entries[1].result).toMatchObject({ inRaid: true, map: "Customs", raidId: "raid-7" });
    expect(entries[2]).toMatchObject({
      method: "getRaidBots",
      args: { detail: true },
      ok: true,
    });
  });

  it("失败调用记录 ok:false 与错误信息，并原样抛出异常", async () => {
    const path = tempPath();
    const recorder = new RecordingBridgeConnection(
      fakeBridge({ info: unreachable("connect ECONNREFUSED") }),
      path,
      { now: () => new Date(RECORDED_TS) },
    );

    await expect(recorder.getInfo()).rejects.toThrow(/ECONNREFUSED/);

    const entries = readEntries(path);
    expect(entries).toHaveLength(1);
    expect(entries[0]).toMatchObject({ method: "getInfo", args: null, ok: false });
    expect((entries[0].result as { error: string }).error).toContain("ECONNREFUSED");
  });

  it("父目录不存在时自动创建，录制不中断调用", async () => {
    const path = join(tempPath("nested"), "deeper", "recording.jsonl");

    const recorder = new RecordingBridgeConnection(fakeBridge(), path);
    const info = await recorder.getInfo();

    expect(info.protocolVersion).toBe(EXPECTED_BRIDGE_PROTOCOL_VERSION);
    expect(readEntries(path)).toHaveLength(1);
  });
});

describe("回放（loadReplayConnection）", () => {
  it("以录制驱动 raid_status / raid_player：输出与录制一致且确定性", async () => {
    const path = tempPath();
    writeRecording(path, [
      {
        ts: RECORDED_TS,
        method: "getInfo",
        args: null,
        ok: true,
        result: defaultBridgeInfo({ pluginVersion: "9.9.9", sampling: { intervalMs: 250 } }),
      },
      {
        ts: RECORDED_TS,
        method: "getRaidStatus",
        args: null,
        ok: true,
        result: inRaidStatus({ map: "Customs", raidId: "raid-rec", remainingSeconds: 321 }),
      },
      {
        ts: RECORDED_TS,
        method: "getRaidPlayer",
        args: null,
        ok: true,
        result: inRaidPlayer({ position: { x: 11, y: 22, z: 33 }, pose: "crouch" }),
      },
    ]);

    const invoke = serverDispatcher(loadReplayConnection(path));

    const status = await invoke("raid_status", {});
    const player = await invoke("raid_player", {});

    expect(status.ok).toBe(true);
    if (!status.ok) return;
    expect(status.data).toEqual({
      map: "Customs",
      status: "running",
      remainingSeconds: 321,
      raidId: "raid-rec",
      sampleAgeMs: 100,
      bridge: {
        pluginVersion: "9.9.9",
        protocolVersion: EXPECTED_BRIDGE_PROTOCOL_VERSION,
        samplingIntervalMs: 250,
      },
    });

    expect(player.ok).toBe(true);
    if (!player.ok) return;
    expect(player.data).toMatchObject({
      position: { x: 11, y: 22, z: 33 },
      pose: "crouch",
    });

    // 确定性：重复调用（getInfo 复用最后一条记录）逐字节稳定
    const statusAgain = await invoke("raid_status", {});
    expect(JSON.stringify(statusAgain)).toBe(JSON.stringify(status));
  });

  it("录制中缺某 method 条目：抛 BridgeUnreachableError", async () => {
    const path = tempPath();
    writeRecording(path, [
      { ts: RECORDED_TS, method: "getInfo", args: null, ok: true, result: defaultBridgeInfo() },
    ]);

    const replay = loadReplayConnection(path);

    await expect(replay.getRaidStatus()).rejects.toThrow(/缺少 getRaidStatus/);
  });

  it("录制中的失败条目回放为 BridgeUnreachableError", async () => {
    const path = tempPath();
    writeRecording(path, [
      { ts: RECORDED_TS, method: "getInfo", args: null, ok: false, result: { error: "ECONNREFUSED" } },
    ]);

    const replay = loadReplayConnection(path);

    await expect(replay.getInfo()).rejects.toThrow(/ECONNREFUSED/);
  });

  it("非法 JSONL 行：解析报错并带行号", () => {
    expect(() => parseRecordingLine("{not json", 3)).toThrow(/第 3 行/);
  });
});

describe("录制开关（默认关）", () => {
  it("loadConfig 缺省不启用录制", () => {
    expect(loadConfig({}).bridgeRecordPath).toBeUndefined();
  });

  it("env TARKOV_RUNTIME_MCP_BRIDGE_RECORD 提供录制路径（trim）", () => {
    expect(
      loadConfig({ TARKOV_RUNTIME_MCP_BRIDGE_RECORD: " C:/tmp/rec.jsonl " }).bridgeRecordPath,
    ).toBe("C:/tmp/rec.jsonl");
  });

  it("createRuntime 未配置录制路径：原样使用注入连接（不包装、不落盘）", () => {
    const inner = fakeBridge();
    const { bridge } = createRuntime({
      config: { host: "127.0.0.1", candidatePorts: [6969], anchorVersion: "5.0.0-BEM-20260914" },
      bridge: inner,
    });

    expect(bridge).toBe(inner);
  });

  it("createRuntime 配置录制路径：包装注入连接并写盘", async () => {
    const path = tempPath();
    const { invoke } = createRuntime({
      config: {
        host: "127.0.0.1",
        candidatePorts: [6969],
        anchorVersion: "5.0.0-BEM-20260914",
        bridgeRecordPath: path,
      },
      connect: () => new FakeConnection(() => versionResponse("SPT 5.0.0 (BEM) ff0bf32")),
      bridge: fakeBridge({ status: inRaidStatus() }),
    });

    const result = await invoke("raid_status", {});
    expect(result.ok).toBe(true);

    const entries = readEntries(path);
    expect(entries.map((entry) => entry.method)).toEqual(["getInfo", "getRaidStatus"]);
    expect(entries[0]).toMatchObject({ ok: true, args: null });
  });
});
