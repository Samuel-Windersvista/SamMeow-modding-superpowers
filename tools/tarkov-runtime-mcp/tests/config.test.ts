import { describe, expect, it } from "vitest";

import {
  DEFAULT_ANCHORED_VERSION,
  DEFAULT_CANDIDATE_PORTS,
  DEFAULT_HOST,
  loadConfig,
} from "../src/config.js";

describe("loadConfig", () => {
  it("缺省使用常量（默认端口 6969，锚定 BEM tag）", () => {
    const config = loadConfig({});

    expect(config.host).toBe(DEFAULT_HOST);
    expect(config.candidatePorts).toEqual([...DEFAULT_CANDIDATE_PORTS]);
    expect(config.anchorVersion).toBe(DEFAULT_ANCHORED_VERSION);
    expect(config.anchorVersion).toBe("5.0.0-BEM-20260910");
  });

  it("环境变量可覆盖 host / 端口 / 锚定 tag", () => {
    const config = loadConfig({
      TARKOV_RUNTIME_MCP_HOST: "192.168.1.10",
      TARKOV_RUNTIME_MCP_PORTS: "6970, 6971",
      TARKOV_RUNTIME_MCP_ANCHOR_VERSION: "5.0.0-BEM-20260909",
    });

    expect(config.host).toBe("192.168.1.10");
    expect(config.candidatePorts).toEqual([6970, 6971]);
    expect(config.anchorVersion).toBe("5.0.0-BEM-20260909");
  });

  it("非法端口串回落到默认端口", () => {
    const config = loadConfig({ TARKOV_RUNTIME_MCP_PORTS: "not-a-port" });
    expect(config.candidatePorts).toEqual([...DEFAULT_CANDIDATE_PORTS]);
  });

  it("username / password 经环境变量装载；缺省为 undefined", () => {
    const bare = loadConfig({});
    expect(bare.username).toBeUndefined();
    expect(bare.password).toBeUndefined();

    const configured = loadConfig({
      TARKOV_RUNTIME_MCP_USERNAME: "Samuel",
      TARKOV_RUNTIME_MCP_PASSWORD: "secret",
    });
    expect(configured.username).toBe("Samuel");
    expect(configured.password).toBe("secret");
  });
});
