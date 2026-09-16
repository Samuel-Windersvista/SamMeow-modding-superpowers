import { createServer, type IncomingMessage, type ServerResponse } from "node:http";
import { afterEach, describe, expect, it } from "vitest";

import { BridgeEndpointUnavailableError, BridgeUnreachableError } from "../../src/bridge/connection.js";
import { HttpBridgeConnection } from "../../src/bridge/http-bridge-connection.js";

interface StubServer {
  port: number;
  close: () => Promise<void>;
}

const servers: StubServer[] = [];

/**
 * Fetch/undici 规范拒绝的 forbidden port 集合：`listen(0)` 由 OS 随机分配端口，
 * 偶发命中（实测 5061）会让 fetch 直接抛 "bad port" 造成随机失败——命中时换端口重试。
 */
const FORBIDDEN_PORTS = new Set([
  1, 7, 9, 11, 13, 15, 17, 19, 20, 21, 22, 23, 25, 37, 42, 43, 53, 69, 77, 79,
  87, 95, 101, 102, 103, 104, 109, 110, 111, 113, 115, 117, 119, 123, 135, 137,
  139, 143, 161, 179, 389, 427, 465, 512, 513, 514, 515, 526, 530, 531, 532,
  540, 548, 554, 556, 563, 587, 601, 636, 989, 990, 993, 995, 1719, 1720, 1723,
  2049, 3659, 4045, 4190, 5060, 5061, 6000, 6566, 6665, 6666, 6667, 6668, 6669,
  6679, 6697, 10080,
]);

/** 在 127.0.0.1 的随机空闲端口启动本地 stub server（避开 fetch forbidden port） */
async function startStubServer(
  handler: (req: IncomingMessage, res: ServerResponse) => void,
): Promise<StubServer> {
  const server = createServer(handler);
  for (;;) {
    await new Promise<void>((resolve) => server.listen(0, "127.0.0.1", resolve));
    const bound = server.address();
    if (bound === null || typeof bound === "string") {
      throw new Error("stub server 未取得端口");
    }
    if (!FORBIDDEN_PORTS.has(bound.port)) {
      break;
    }
    await new Promise<void>((resolve) => server.close(() => resolve()));
  }
  const address = server.address();
  if (address === null || typeof address === "string") {
    throw new Error("stub server 未取得端口");
  }
  const stub: StubServer = {
    port: address.port,
    close: () =>
      new Promise<void>((resolve) => {
        server.closeAllConnections();
        server.close(() => resolve());
      }),
  };
  servers.push(stub);
  return stub;
}

function jsonServer(payload: unknown, status = 200) {
  return (_req: IncomingMessage, res: ServerResponse): void => {
    res.writeHead(status, { "Content-Type": "application/json" });
    res.end(JSON.stringify(payload));
  };
}

/** 路由 stub：按 pathname 返回不同 payload；记录请求次数 */
async function routeStub(
  routes: Record<string, unknown>,
): Promise<{ stub: StubServer; hits: Record<string, number> }> {
  const hits: Record<string, number> = {};
  const stub = await startStubServer((req, res) => {
    const pathname = (req.url ?? "").split("?")[0];
    hits[pathname] = (hits[pathname] ?? 0) + 1;
    if (pathname in routes) {
      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify(routes[pathname]));
      return;
    }
    res.writeHead(404, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ error: "not found" }));
  });
  return { stub, hits };
}

const INFO_PAYLOAD = {
  pluginVersion: "0.1.0",
  protocolVersion: 1,
  capabilities: {
    endpoints: ["/bridge/info", "/raid/status"],
    sections: ["status", "player", "bots"],
  },
  sampling: { intervalMs: 1000 },
  network: { host: "127.0.0.1", port: 49777 },
};

const PLAYER_PAYLOAD = {
  inRaid: true,
  position: { x: 1.5, y: -2, z: 3 },
  rotation: { x: 180, y: 45 },
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
  equipment: [{ slot: "Headwear", tpl: "5aa7e276e5b5b000171d0647", name: "Altyn helmet" }],
  sampleAgeMs: 42,
};

const BOTS_SUMMARY_PAYLOAD = {
  inRaid: true,
  total: 10,
  alive: 8,
  byCategory: { pmc: 4, scav: 5, boss: 1, other: 0 },
  spawner: { aliveAndLoading: 9, delayed: 1, allWithDelayed: 10 },
  sampleAgeMs: 42,
};

const EVENTS_PAYLOAD = {
  inRaid: true,
  seq: 3,
  dropped: 1,
  events: [
    {
      seq: 1,
      ts: "2026-09-15T10:00:00.000Z",
      type: "damage",
      raidId: "raid-abc",
      payload: {
        victimProfileId: "pmc-local",
        victimIsLocal: true,
        part: "LeftLeg",
        amount: 12.5,
        sourceType: "Bullet",
        unknownExtra: "ignored",
      },
    },
    {
      seq: 2,
      ts: "2026-09-15T10:00:05.000Z",
      type: "death",
      raidId: "raid-abc",
      payload: {
        victimProfileId: "bot-1",
        victimIsLocal: false,
        damageType: "Bullet",
        killer: {
          profileId: "pmc-local",
          name: "LocalPMC",
          side: "Bear",
          role: "pmc",
          isLocal: true,
        },
      },
    },
    {
      seq: 3,
      ts: "2026-09-15T10:05:00.000Z",
      type: "extraction",
      raidId: "raid-abc",
      payload: { exitName: "Crossroads", status: "Success", extra: 1 },
    },
  ],
};

const LOGS_RECENT_PAYLOAD = {
  seq: 7,
  dropped: 2,
  entries: [
    {
      seq: 6,
      ts: "2026-09-15T10:00:00.000Z",
      level: "warning",
      source: "Unity",
      text: "ComboBox: value is null",
      unknownExtra: "ignored",
    },
  ],
};

const LOGS_SUMMARY_PAYLOAD = {
  groups: [
    {
      key: "ComboBox: value is null",
      level: "error",
      source: "Unity",
      count: 6477,
      firstTs: "2026-09-15T10:00:00.000Z",
      lastTs: "2026-09-15T10:00:02.000Z",
      sampleText: "ComboBox: value is null",
      unknownExtra: "ignored",
    },
  ],
  overflowDropped: 3,
};

afterEach(async () => {
  await Promise.all(servers.splice(0).map((stub) => stub.close()));
});

describe("HttpBridgeConnection（node:http stub server）", () => {
  it("/bridge/info 200：每次调用都实际请求（不缓存）", async () => {
    const { stub, hits } = await routeStub({ "/bridge/info": INFO_PAYLOAD });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    const first = await bridge.getInfo();
    const second = await bridge.getInfo();

    expect(first).toEqual(INFO_PAYLOAD);
    expect(second).toEqual(INFO_PAYLOAD);
    expect(hits["/bridge/info"]).toBe(2);
  });

  it("/bridge/info 404：抛 BridgeUnreachableError 且提示可能为旧版桥", async () => {
    const stub = await startStubServer(jsonServer({ error: "not found" }, 404));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getInfo()).rejects.toBeInstanceOf(BridgeUnreachableError);
    await expect(bridge.getInfo()).rejects.toThrow(/旧版桥/);
  });

  it("/bridge/info 非 2xx：抛 BridgeUnreachableError", async () => {
    const stub = await startStubServer(jsonServer({ error: "boom" }, 500));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getInfo()).rejects.toBeInstanceOf(BridgeUnreachableError);
  });

  it("/raid/status 200 in-raid：解析元数据", async () => {
    const { stub } = await routeStub({
      "/raid/status": {
        inRaid: true,
        map: "Woods",
        status: "running",
        remainingSeconds: 1234,
        raidId: "raid-abc",
        sampleAgeMs: 42,
      },
    });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getRaidStatus()).toEqual({
      inRaid: true,
      map: "Woods",
      status: "running",
      remainingSeconds: 1234,
      raidId: "raid-abc",
      sampleAgeMs: 42,
    });
  });

  it("/raid/status 200 not-in-raid：返回 { inRaid: false }", async () => {
    const stub = await startStubServer(jsonServer({ inRaid: false }));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getRaidStatus()).toEqual({ inRaid: false });
  });

  it("/raid/player 200 in-raid：解析 position/rotation/pose/health/sampleAgeMs", async () => {
    const stub = await startStubServer(jsonServer(PLAYER_PAYLOAD));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getRaidPlayer()).toEqual(PLAYER_PAYLOAD);
  });

  it("/raid/player 200 not-in-raid：返回 { inRaid: false }", async () => {
    const stub = await startStubServer(jsonServer({ inRaid: false }));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getRaidPlayer()).toEqual({ inRaid: false });
  });

  it("/raid/player 200 但 schema 非法：抛 BridgeUnreachableError", async () => {
    const stub = await startStubServer(
      jsonServer({ inRaid: true, position: { x: 1 }, sampleAgeMs: 1 }),
    );
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getRaidPlayer()).rejects.toBeInstanceOf(BridgeUnreachableError);
  });

  it("/raid/bots 摘要 200：解析计数与生成器", async () => {
    const { stub, hits } = await routeStub({ "/raid/bots": BOTS_SUMMARY_PAYLOAD });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getRaidBots(false)).toEqual({
      inRaid: true,
      detail: false,
      total: 10,
      alive: 8,
      byCategory: { pmc: 4, scav: 5, boss: 1, other: 0 },
      spawner: { aliveAndLoading: 9, delayed: 1, allWithDelayed: 10 },
      sampleAgeMs: 42,
    });
    expect(hits["/raid/bots"]).toBe(1);
  });

  it("/raid/bots detail=1：请求明细并解析 bots/truncated", async () => {
    const { stub, hits } = await routeStub({
      "/raid/bots": {
        ...BOTS_SUMMARY_PAYLOAD,
        bots: [{ x: 1, y: 0, z: 2, role: "pmc", side: "Bear", alive: true }],
        truncated: true,
      },
    });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getRaidBots(true)).toEqual({
      inRaid: true,
      detail: true,
      total: 10,
      alive: 8,
      byCategory: { pmc: 4, scav: 5, boss: 1, other: 0 },
      spawner: { aliveAndLoading: 9, delayed: 1, allWithDelayed: 10 },
      sampleAgeMs: 42,
      bots: [{ x: 1, y: 0, z: 2, role: "pmc", side: "Bear", alive: true }],
      truncated: true,
    });
    expect(hits["/raid/bots"]).toBe(1);
  });

  it("/raid/bots detail 请求但响应缺 bots：抛 BridgeUnreachableError", async () => {
    const stub = await startStubServer(jsonServer(BOTS_SUMMARY_PAYLOAD));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getRaidBots(true)).rejects.toBeInstanceOf(BridgeUnreachableError);
  });

  it("/raid/player 缺 weapon/equipment：归一为 null 与空数组（缺省语义明确）", async () => {
    const stub = await startStubServer(
      jsonServer({ ...PLAYER_PAYLOAD, weapon: undefined, equipment: undefined }),
    );
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    const result = await bridge.getRaidPlayer();
    expect(result).toMatchObject({ inRaid: true, weapon: null, equipment: [] });
  });

  it("/raid/events 200：解析 seq/dropped/events 并忽略未知字段", async () => {
    const { stub, hits } = await routeStub({ "/raid/events": EVENTS_PAYLOAD });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getRaidEvents()).toEqual({
      inRaid: true,
      seq: 3,
      dropped: 1,
      events: [
        {
          seq: 1,
          ts: "2026-09-15T10:00:00.000Z",
          type: "damage",
          raidId: "raid-abc",
          payload: {
            victimProfileId: "pmc-local",
            victimIsLocal: true,
            part: "LeftLeg",
            amount: 12.5,
            sourceType: "Bullet",
          },
        },
        {
          seq: 2,
          ts: "2026-09-15T10:00:05.000Z",
          type: "death",
          raidId: "raid-abc",
          payload: {
            victimProfileId: "bot-1",
            victimIsLocal: false,
            damageType: "Bullet",
            killer: {
              profileId: "pmc-local",
              name: "LocalPMC",
              side: "Bear",
              role: "pmc",
              isLocal: true,
            },
          },
        },
        {
          seq: 3,
          ts: "2026-09-15T10:05:00.000Z",
          type: "extraction",
          raidId: "raid-abc",
          payload: { exitName: "Crossroads", status: "Success" },
        },
      ],
    });
    expect(hits["/raid/events"]).toBe(1);
  });

  it("/raid/events 带 since/limit：查询参数按固定顺序透传", async () => {
    const seen: string[] = [];
    const stub = await startStubServer((req, res) => {
      seen.push(req.url ?? "");
      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify(EVENTS_PAYLOAD));
    });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await bridge.getRaidEvents(7, 25);

    expect(seen).toEqual(["/raid/events?since=7&limit=25"]);
  });

  it("/raid/events 200 not-in-raid：仍解析 seq/dropped/events（缓冲保留）", async () => {
    const stub = await startStubServer(
      jsonServer({
        inRaid: false,
        seq: 5,
        dropped: 1,
        events: [
          {
            seq: 5,
            ts: "2026-09-15T10:05:00.000Z",
            type: "extraction",
            raidId: "raid-abc",
            payload: { exitName: "Crossroads", status: "Success" },
          },
        ],
      }),
    );
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getRaidEvents()).toEqual({
      inRaid: false,
      seq: 5,
      dropped: 1,
      events: [
        {
          seq: 5,
          ts: "2026-09-15T10:05:00.000Z",
          type: "extraction",
          raidId: "raid-abc",
          payload: { exitName: "Crossroads", status: "Success" },
        },
      ],
    });
  });

  it("/raid/events extraction 字段缺失/非字符串：宽容归一为 ''（不抛错）", async () => {
    const stub = await startStubServer(
      jsonServer({
        inRaid: true,
        seq: 2,
        dropped: 0,
        events: [
          { seq: 1, ts: "t1", type: "extraction", raidId: "r", payload: {} },
          { seq: 2, ts: "t2", type: "extraction", raidId: "r", payload: { exitName: 42, status: null } },
        ],
      }),
    );
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getRaidEvents()).toEqual({
      inRaid: true,
      seq: 2,
      dropped: 0,
      events: [
        { seq: 1, ts: "t1", type: "extraction", raidId: "r", payload: { exitName: "", status: "" } },
        { seq: 2, ts: "t2", type: "extraction", raidId: "r", payload: { exitName: "", status: "" } },
      ],
    });
  });

  it("/raid/events 事件类型非法：抛 BridgeUnreachableError", async () => {
    const stub = await startStubServer(
      jsonServer({
        inRaid: true,
        seq: 1,
        dropped: 0,
        events: [{ seq: 1, ts: "t", type: "teleport", raidId: "r", payload: {} }],
      }),
    );
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getRaidEvents()).rejects.toBeInstanceOf(BridgeUnreachableError);
  });

  it("/logs/recent 200：解析 seq/dropped/entries 并忽略未知字段", async () => {
    const { stub, hits } = await routeStub({ "/logs/recent": LOGS_RECENT_PAYLOAD });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getLogsRecent()).toEqual({
      seq: 7,
      dropped: 2,
      entries: [
        {
          seq: 6,
          ts: "2026-09-15T10:00:00.000Z",
          level: "warning",
          source: "Unity",
          text: "ComboBox: value is null",
        },
      ],
    });
    expect(hits["/logs/recent"]).toBe(1);
  });

  it("/logs/recent 带 level/since/limit：查询参数按固定顺序透传（level 不做大小写改写）", async () => {
    const seen: string[] = [];
    const stub = await startStubServer((req, res) => {
      seen.push(req.url ?? "");
      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify(LOGS_RECENT_PAYLOAD));
    });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await bridge.getLogsRecent(5, "Warning", 10);
    await bridge.getLogsRecent(undefined, "a&b");

    expect(seen).toEqual(["/logs/recent?level=Warning&since=5&limit=10", "/logs/recent?level=a%26b"]);
  });

  it("/logs/summary 200：解析 groups/overflowDropped", async () => {
    const { stub, hits } = await routeStub({ "/logs/summary": LOGS_SUMMARY_PAYLOAD });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    expect(await bridge.getLogsSummary()).toEqual({
      groups: [
        {
          key: "ComboBox: value is null",
          level: "error",
          source: "Unity",
          count: 6477,
          firstTs: "2026-09-15T10:00:00.000Z",
          lastTs: "2026-09-15T10:00:02.000Z",
          sampleText: "ComboBox: value is null",
        },
      ],
      overflowDropped: 3,
    });
    expect(hits["/logs/summary"]).toBe(1);
  });

  it("/logs/summary 带 since：ISO 8601 与整数 UTC Ticks 均转义后透传", async () => {
    const seen: string[] = [];
    const stub = await startStubServer((req, res) => {
      seen.push(req.url ?? "");
      res.writeHead(200, { "Content-Type": "application/json" });
      res.end(JSON.stringify(LOGS_SUMMARY_PAYLOAD));
    });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await bridge.getLogsSummary("2026-09-15T10:00:02.000Z");
    await bridge.getLogsSummary(638000000000000000);

    expect(seen).toEqual([
      "/logs/summary?since=2026-09-15T10%3A00%3A02.000Z",
      "/logs/summary?since=638000000000000000",
    ]);
  });

  it("/logs/recent 404：抛 BridgeEndpointUnavailableError（提示更新桥 DLL），非 BridgeUnreachableError", async () => {
    const stub = await startStubServer(jsonServer({ error: "not_found" }, 404));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getLogsRecent()).rejects.toBeInstanceOf(BridgeEndpointUnavailableError);
    await expect(bridge.getLogsRecent()).rejects.toThrow(/更新桥 DLL/);
    await expect(bridge.getLogsRecent()).rejects.not.toBeInstanceOf(BridgeUnreachableError);
  });

  it("/logs/summary 404：抛 BridgeEndpointUnavailableError（携带端点路径）", async () => {
    const stub = await startStubServer(jsonServer({ error: "not_found" }, 404));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getLogsSummary()).rejects.toBeInstanceOf(BridgeEndpointUnavailableError);
    await expect(bridge.getLogsSummary()).rejects.toMatchObject({ endpoint: "/logs/summary" });
  });

  it("/logs/recent 500：抛 BridgeUnreachableError（非端点缺失）", async () => {
    const stub = await startStubServer(jsonServer({ error: "boom" }, 500));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getLogsRecent()).rejects.toBeInstanceOf(BridgeUnreachableError);
    await expect(bridge.getLogsRecent()).rejects.not.toBeInstanceOf(
      BridgeEndpointUnavailableError,
    );
  });

  it("/logs/recent schema 非法（缺 entries）：抛 BridgeUnreachableError", async () => {
    const stub = await startStubServer(jsonServer({ seq: 1, dropped: 0 }));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getLogsRecent()).rejects.toBeInstanceOf(BridgeUnreachableError);
  });

  it("/logs/summary schema 非法（缺 groups）：抛 BridgeUnreachableError", async () => {
    const stub = await startStubServer(jsonServer({ overflowDropped: 0 }));
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getLogsSummary()).rejects.toBeInstanceOf(BridgeUnreachableError);
  });

  it("非 JSON 响应：抛 BridgeUnreachableError", async () => {
    const stub = await startStubServer((_req, res) => {
      res.writeHead(200, { "Content-Type": "text/plain" });
      res.end("not json");
    });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    await expect(bridge.getRaidPlayer()).rejects.toBeInstanceOf(BridgeUnreachableError);
  });

  it("连接拒绝：抛 BridgeUnreachableError", async () => {
    const stub = await startStubServer(jsonServer({ inRaid: false }));
    const port = stub.port;
    await stub.close();
    servers.splice(servers.indexOf(stub), 1);

    const bridge = new HttpBridgeConnection("127.0.0.1", port);

    await expect(bridge.getInfo()).rejects.toBeInstanceOf(BridgeUnreachableError);
  });

  it("超时：抛 BridgeUnreachableError", async () => {
    // 服务端永不响应；客户端 50ms 超时后中止
    const stub = await startStubServer(() => {
      /* 永不响应 */
    });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port, { timeoutMs: 50 });

    await expect(bridge.getRaidStatus()).rejects.toBeInstanceOf(BridgeUnreachableError);
  });
});
