import { describe, expect, it } from "vitest";

import {
  DEFAULT_ANCHORED_VERSION,
  DEFAULT_BRIDGE_HOST,
  DEFAULT_BRIDGE_PORT,
  DEFAULT_CANDIDATE_PORTS,
  DEFAULT_HOST,
  DEFAULT_LOGWATCH_INTERVAL_MS,
  MAX_LOGWATCH_INTERVAL_MS,
  MIN_LOGWATCH_INTERVAL_MS,
  loadConfig,
  parseLogwatchInterval,
  resolveErrorLogPath,
  resolveLogsRoot,
} from "../src/config.js";

/** 路径断言统一按正斜杠比较（Windows 分隔符差异） */
function slashes(path: string): string {
  return path.replace(/\\/g, "/");
}

describe("loadConfig", () => {
  it("缺省使用常量（默认端口 6969，锚定 BEM tag）", () => {
    const config = loadConfig({});

    expect(config.host).toBe(DEFAULT_HOST);
    expect(config.candidatePorts).toEqual([...DEFAULT_CANDIDATE_PORTS]);
    expect(config.anchorVersion).toBe(DEFAULT_ANCHORED_VERSION);
    expect(config.anchorVersion).toBe("5.0.0-BEM-20260914");
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

  it("bridge 缺省使用常量（127.0.0.1:49777）", () => {
    const config = loadConfig({});

    expect(config.bridgeHost).toBe(DEFAULT_BRIDGE_HOST);
    expect(config.bridgeHost).toBe("127.0.0.1");
    expect(config.bridgePort).toBe(DEFAULT_BRIDGE_PORT);
    expect(config.bridgePort).toBe(49777);
  });

  it("bridge host / 端口可经环境变量覆盖", () => {
    const config = loadConfig({
      TARKOV_RUNTIME_MCP_BRIDGE_HOST: "10.0.0.5",
      TARKOV_RUNTIME_MCP_BRIDGE_PORT: "50001",
    });

    expect(config.bridgeHost).toBe("10.0.0.5");
    expect(config.bridgePort).toBe(50001);
  });

  it("非法 bridge 端口回落到默认", () => {
    expect(loadConfig({ TARKOV_RUNTIME_MCP_BRIDGE_PORT: "not-a-port" }).bridgePort).toBe(
      DEFAULT_BRIDGE_PORT,
    );
    expect(loadConfig({ TARKOV_RUNTIME_MCP_BRIDGE_PORT: "99999" }).bridgePort).toBe(
      DEFAULT_BRIDGE_PORT,
    );
  });
});

describe("logwatch 配置（间隔与路径）", () => {
  it("间隔缺省 5000ms；env 覆盖；越界钳制到 5000–10000", () => {
    expect(DEFAULT_LOGWATCH_INTERVAL_MS).toBe(5000);
    expect(MIN_LOGWATCH_INTERVAL_MS).toBe(5000);
    expect(MAX_LOGWATCH_INTERVAL_MS).toBe(10000);
    expect(loadConfig({}).logwatchIntervalMs).toBe(5000);

    expect(loadConfig({ TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS: "7000" }).logwatchIntervalMs).toBe(
      7000,
    );
    expect(loadConfig({ TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS: "1000" }).logwatchIntervalMs).toBe(
      5000,
    );
    expect(loadConfig({ TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS: "99999" }).logwatchIntervalMs).toBe(
      10000,
    );
    expect(
      loadConfig({ TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS: "not-a-number" }).logwatchIntervalMs,
    ).toBe(5000);
  });

  it("parseLogwatchInterval：空 / 非法 → 默认，其余钳制", () => {
    expect(parseLogwatchInterval(undefined)).toBe(DEFAULT_LOGWATCH_INTERVAL_MS);
    expect(parseLogwatchInterval("")).toBe(DEFAULT_LOGWATCH_INTERVAL_MS);
    expect(parseLogwatchInterval("abc")).toBe(DEFAULT_LOGWATCH_INTERVAL_MS);
    expect(parseLogwatchInterval("5000")).toBe(5000);
    expect(parseLogwatchInterval("6000")).toBe(6000);
    expect(parseLogwatchInterval("12000")).toBe(MAX_LOGWATCH_INTERVAL_MS);
  });

  it("logs 根：缺省为 spt 日志目录的父级（cwd/user/logs）", () => {
    expect(slashes(resolveLogsRoot({}, "C:/cwd"))).toBe("C:/cwd/user/logs");
    expect(slashes(loadConfig({}).logsRoot ?? "")).toContain("/user/logs");
  });

  it("logs 根：LOG_DIR 取父级；SPT_DIR 拼 user/logs；LOGS_ROOT 显式覆盖", () => {
    expect(slashes(resolveLogsRoot({ TARKOV_RUNTIME_MCP_LOG_DIR: "D:/SPT/user/logs/spt" }, "C:/cwd"))).toBe(
      "D:/SPT/user/logs",
    );
    expect(slashes(resolveLogsRoot({ TARKOV_RUNTIME_MCP_SPT_DIR: "C:/SPT" }, "C:/cwd"))).toBe(
      "C:/SPT/user/logs",
    );
    expect(
      slashes(
        resolveLogsRoot(
          {
            TARKOV_RUNTIME_MCP_LOGS_ROOT: "E:/custom/logs",
            TARKOV_RUNTIME_MCP_SPT_DIR: "C:/SPT",
          },
          "C:/cwd",
        ),
      ),
    ).toBe("E:/custom/logs");
  });

  it("fatal 路径：缺省由 SPT_DIR 推导（<SPT_DIR>/../BepInEx/ErrorLog.log），无 SPT_DIR 则 undefined", () => {
    expect(resolveErrorLogPath({})).toBeUndefined();
    expect(slashes(resolveErrorLogPath({ TARKOV_RUNTIME_MCP_SPT_DIR: "C:/game/SPT_Runtime" }) ?? "")).toBe(
      "C:/game/BepInEx/ErrorLog.log",
    );
  });

  it("fatal 路径：ERRORLOG_PATH 显式覆盖（trim）", () => {
    expect(
      resolveErrorLogPath({
        TARKOV_RUNTIME_MCP_ERRORLOG_PATH: " D:/BepInEx/ErrorLog.log ",
        TARKOV_RUNTIME_MCP_SPT_DIR: "C:/game/SPT_Runtime",
      }),
    ).toBe("D:/BepInEx/ErrorLog.log");
  });

  it("loadConfig 装载 logsRoot / errorLogPath / 间隔", () => {
    const config = loadConfig({
      TARKOV_RUNTIME_MCP_LOGS_ROOT: "E:/logs",
      TARKOV_RUNTIME_MCP_ERRORLOG_PATH: "E:/BepInEx/ErrorLog.log",
      TARKOV_RUNTIME_MCP_LOGWATCH_INTERVAL_MS: "8000",
    });

    expect(slashes(config.logsRoot ?? "")).toBe("E:/logs");
    expect(slashes(config.errorLogPath ?? "")).toBe("E:/BepInEx/ErrorLog.log");
    expect(config.logwatchIntervalMs).toBe(8000);
  });
});
