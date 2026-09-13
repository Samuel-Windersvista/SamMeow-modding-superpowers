// =============================================================================
// `/launcher/v2/mods` 路由来源：响应解析与请求
//
// 5.0 live 实测（2026-09-13）：无 mod 时 `{"Response":{}}`（空对象是真相）。
// 有 mod 的形状无 KB 记载，按「数组 / .mods / 字典」三种兼容形状驱动测试。
// =============================================================================

import { describe, expect, it } from "vitest";

import { MODS_ROUTE, fetchServerModsRoute, parseModsRouteResponse } from "../../src/client/mods-route.js";
import { RUNTIME_ERROR_CODES } from "../../src/types.js";
import { FakeConnection } from "../helpers/fake-connection.js";

describe("parseModsRouteResponse", () => {
  it("真实空响应 `{Response:{}}`：返回 count 0 的可用结果（不是失败）", () => {
    const result = parseModsRouteResponse({ Response: {} });

    expect(result).toEqual({
      source: "route",
      available: true,
      route: MODS_ROUTE,
      count: 0,
      mods: [],
    });
  });

  it("`Response` 为记录数组：解析 name/version/guid/author/sptVersion", () => {
    const result = parseModsRouteResponse({
      Response: [
        {
          name: "MyMod",
          version: "1.2.3",
          guid: "com.example.mymod",
          author: "Author",
          sptVersion: "~5.0.0",
        },
      ],
    });

    expect(result.count).toBe(1);
    expect(result.mods[0]).toEqual({
      name: "MyMod",
      version: "1.2.3",
      guid: "com.example.mymod",
      author: "Author",
      targetsSpt: "~5.0.0",
    });
  });

  it("`Response.mods` 为数组：兼容包裹形状", () => {
    const result = parseModsRouteResponse({ Response: { mods: [{ Name: "OtherMod" }] } });

    expect(result.count).toBe(1);
    expect(result.mods[0].name).toBe("OtherMod");
  });

  it("`Response` 为字典（modKey -> 记录）：兼容键值形状", () => {
    const result = parseModsRouteResponse({
      Response: { "com.example.a": { Name: "A", Version: "2.0.0" } },
    });

    expect(result.count).toBe(1);
    expect(result.mods[0]).toMatchObject({ name: "A", version: "2.0.0" });
  });
});

describe("fetchServerModsRoute", () => {
  it("请求 `/launcher/v2/mods` 并解析响应", async () => {
    const connection = new FakeConnection(() => ({
      status: 200,
      body: { Response: {} },
      text: "{}",
    }));

    const result = await fetchServerModsRoute(connection);

    expect(connection.requests[0]).toMatchObject({ method: "POST", path: MODS_ROUTE });
    expect(result.source).toBe("route");
    expect(result.count).toBe(0);
  });

  it("非 2xx：抛结构化 ROUTE_ERROR（由 server_status 回落日志）", async () => {
    const connection = new FakeConnection(() => ({ status: 500, body: null, text: "" }));

    await expect(fetchServerModsRoute(connection)).rejects.toMatchObject({
      code: RUNTIME_ERROR_CODES.ROUTE_ERROR,
    });
  });
});
