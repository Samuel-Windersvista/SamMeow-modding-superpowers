import { createServer, type IncomingMessage, type ServerResponse } from "node:http";
import { afterEach, describe, expect, it } from "vitest";

import { BridgeUnreachableError } from "../../src/bridge/connection.js";
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

afterEach(async () => {
  await Promise.all(servers.splice(0).map((stub) => stub.close()));
});

describe("HttpBridgeConnection（node:http stub server）", () => {
  it("/bridge/info 200：解析全字段并缓存（第二次不再请求）", async () => {
    const { stub, hits } = await routeStub({ "/bridge/info": INFO_PAYLOAD });
    const bridge = new HttpBridgeConnection("127.0.0.1", stub.port);

    const first = await bridge.getInfo();
    const second = await bridge.getInfo();

    expect(first).toEqual(INFO_PAYLOAD);
    expect(second).toEqual(INFO_PAYLOAD);
    expect(hits["/bridge/info"]).toBe(1);
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
