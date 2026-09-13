import { describe, expect, it } from "vitest";

import { HttpSptConnection } from "../../src/transport/http-connection.js";
import { compressPayload, decompressPayload } from "../../src/transport/compression.js";
import { deshuffleFrame, shuffleFrame } from "../../src/transport/shuffle.js";
import type {
  HttpTransport,
  RawHttpRequest,
  RawHttpResponse,
} from "../../src/transport/http-transport.js";

function rawResponse(
  body: Buffer,
  headers: Record<string, string | string[] | undefined> = {},
): RawHttpResponse {
  return { status: 200, headers, body };
}

/** 记录请求并按调用顺序返回预设响应 */
function scriptedTransport(
  responses: RawHttpResponse[],
): { transport: HttpTransport; requests: RawHttpRequest[] } {
  const requests: RawHttpRequest[] = [];
  const transport: HttpTransport = async (spec) => {
    requests.push(spec);
    const response = responses[requests.length - 1];
    if (!response) {
      throw new Error("测试脚本未提供更多响应");
    }
    return response;
  };
  return { transport, requests };
}

describe("HttpSptConnection: 响应解码", () => {
  it("GET /singleplayer 响应为 zlib（不 shuffle）时可解析 JSON", async () => {
    const payload = Buffer.from(JSON.stringify({ Version: "SPT 5.0.0 (BEM) ff0bf32" }), "utf8");
    const { transport, requests } = scriptedTransport([
      rawResponse(await compressPayload(payload)),
    ]);
    const connection = new HttpSptConnection("127.0.0.1", 6969, { transport });

    const response = await connection.request({
      method: "GET",
      path: "/singleplayer/settings/version",
    });

    expect(response.status).toBe(200);
    expect(response.body).toEqual({ Version: "SPT 5.0.0 (BEM) ff0bf32" });
    expect(requests[0].url).toBe("https://127.0.0.1:6969/singleplayer/settings/version");
  });

  it("GET /client 响应为 shuffle + zlib 时可解析 JSON", async () => {
    const payload = Buffer.from(JSON.stringify({ ok: true, traders: [] }), "utf8");
    const { transport } = scriptedTransport([
      rawResponse(shuffleFrame(await compressPayload(payload))),
    ]);
    const connection = new HttpSptConnection("127.0.0.1", 6969, { transport });

    const response = await connection.request({
      method: "GET",
      path: "/client/trading/api/traderSettings",
    });

    expect(response.body).toEqual({ ok: true, traders: [] });
  });

  it("明文 JSON 响应（/files）直接解析", async () => {
    const { transport } = scriptedTransport([
      rawResponse(Buffer.from(JSON.stringify({ plain: true }), "utf8")),
    ]);
    const connection = new HttpSptConnection("127.0.0.1", 6969, { transport });

    const response = await connection.request({ method: "GET", path: "/files/list" });
    expect(response.body).toEqual({ plain: true });
  });
});

describe("HttpSptConnection: cookie 会话", () => {
  it("捕获 Set-Cookie 并在后续请求回带 PHPSESSID", async () => {
    const payload = Buffer.from("{}", "utf8");
    const { transport, requests } = scriptedTransport([
      rawResponse(await compressPayload(payload), { "set-cookie": "PHPSESSID=session-1; Path=/" }),
      rawResponse(await compressPayload(payload)),
    ]);
    const connection = new HttpSptConnection("127.0.0.1", 6969, { transport });

    await connection.request({ method: "GET", path: "/singleplayer/settings/version" });
    expect(connection.getSessionId()).toBe("session-1");
    expect(requests[0].headers["Cookie"]).toBeUndefined();

    await connection.request({ method: "GET", path: "/singleplayer/settings/version" });
    expect(requests[1].headers["Cookie"]).toBe("PHPSESSID=session-1");
  });
});

describe("HttpSptConnection: 请求编码", () => {
  it("POST 到 shuffle 路径：请求体为 shuffle + zlib，响应 shuffle + zlib 可解析", async () => {
    const responsePayload = Buffer.from(JSON.stringify({ Data: { level: 5 } }), "utf8");
    const { transport, requests } = scriptedTransport([
      rawResponse(shuffleFrame(await compressPayload(responsePayload))),
    ]);
    const connection = new HttpSptConnection("127.0.0.1", 6969, { transport });

    const response = await connection.request({
      method: "POST",
      path: "/client/game/profile/list",
      body: { action: "list" },
    });

    const sent = requests[0];
    expect(sent.method).toBe("POST");
    expect(sent.headers["requestcompressed"]).toBe("1");
    expect(sent.headers["Content-Type"]).toBe("application/json");
    expect(sent.body).toBeInstanceOf(Buffer);

    const decoded = (await decompressPayload(deshuffleFrame(sent.body!))).toString("utf8");
    expect(JSON.parse(decoded)).toEqual({ action: "list" });
    expect(response.body).toEqual({ Data: { level: 5 } });
  });

  it("POST 到 no-shuffle 路径（/client/metadata）：请求体仅 zlib", async () => {
    const responsePayload = Buffer.from("{}", "utf8");
    const { transport, requests } = scriptedTransport([
      rawResponse(await compressPayload(responsePayload)),
    ]);
    const connection = new HttpSptConnection("127.0.0.1", 6969, { transport });

    await connection.request({
      method: "POST",
      path: "/client/metadata",
      body: { hello: "world" },
    });

    const decoded = (await decompressPayload(requests[0].body!)).toString("utf8");
    expect(JSON.parse(decoded)).toEqual({ hello: "world" });
  });
});
