import { describe, expect, it } from "vitest";

import { buildCookieHeader, extractSessionId } from "../../src/transport/cookie.js";

describe("cookie: PHPSESSID 会话提取", () => {
  it("从单个 Set-Cookie 提取", () => {
    expect(extractSessionId("PHPSESSID=abc123; Path=/; HttpOnly")).toBe("abc123");
  });

  it("从多值 Set-Cookie 中提取", () => {
    expect(
      extractSessionId(["foo=bar; Path=/", "PHPSESSID=deadbeef; Path=/; HttpOnly"]),
    ).toBe("deadbeef");
  });

  it("缺少 cookie 时返回 null", () => {
    expect(extractSessionId(undefined)).toBeNull();
    expect(extractSessionId("other=1; Path=/")).toBeNull();
  });

  it("构造 Cookie 头", () => {
    expect(buildCookieHeader("abc123")).toBe("PHPSESSID=abc123");
  });
});
