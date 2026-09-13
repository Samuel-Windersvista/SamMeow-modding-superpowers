// 测试辅助：fake SptConnection（S1 接缝）与常用 fixture
//
// 免会话的 launcher 路由（/launcher/v2/profiles、/launcher/v2/login）由 FakeConnection
// 内置拦截，模拟真实 server 的 `{Response: ...}` 信封；业务路由仍走注入的 responder。

import { SptClient } from "../../src/client/client.js";
import { DEFAULT_ANCHORED_VERSION } from "../../src/config.js";
import type {
  SptConnection,
  SptRequestOptions,
  SptResponse,
} from "../../src/transport/connection.js";

export type FakeResponder = (
  options: SptRequestOptions,
) => SptResponse | Promise<SptResponse> | Error;

/** 免会话 launcher 路由的模拟配置 */
export interface FakeSessionOptions {
  /** `/launcher/v2/profiles` 返回的 profile 用户名（默认 "Overseer"） */
  username?: string;
  /** 返回的 profileId（默认 "fake-profile-id"） */
  profileId?: string;
  /** `/launcher/v2/login` 的 Response 值（默认 true） */
  loginResponse?: unknown;
  /** 直接覆盖 `/launcher/v2/profiles` 的整个响应体（用于畸形/缺字段场景） */
  profilesResponse?: unknown;
}

export const LAUNCHER_PROFILES_PATH = "/launcher/v2/profiles";
export const LAUNCHER_LOGIN_PATH = "/launcher/v2/login";

function jsonResponse(body: unknown): SptResponse {
  return { status: 200, body, text: JSON.stringify(body) };
}

export class FakeConnection implements SptConnection {
  readonly host: string;
  readonly port: number;
  readonly baseUrl: string;
  readonly requests: SptRequestOptions[] = [];

  private sessionId: string | null = null;

  constructor(
    private readonly responder: FakeResponder,
    host = "127.0.0.1",
    port = 6969,
    private readonly session: FakeSessionOptions = {},
  ) {
    this.host = host;
    this.port = port;
    this.baseUrl = `https://${host}:${port}`;
  }

  getSessionId(): string | null {
    return this.sessionId;
  }

  setSessionId(sessionId: string): void {
    this.sessionId = sessionId;
  }

  async request(options: SptRequestOptions): Promise<SptResponse> {
    this.requests.push(options);

    // 免会话 launcher 路由：内置模拟，不经过业务 responder
    if (options.path === LAUNCHER_PROFILES_PATH) {
      return jsonResponse(
        this.session.profilesResponse ?? {
          Response: [
            {
              username: this.session.username ?? "Overseer",
              profileId: this.session.profileId ?? "fake-profile-id",
            },
          ],
        },
      );
    }
    if (options.path === LAUNCHER_LOGIN_PATH) {
      return jsonResponse({ Response: this.session.loginResponse ?? true });
    }

    const result = this.responder(options);
    if (result instanceof Error) {
      throw result;
    }
    return await result;
  }
}

/** 构造 `/singleplayer/settings/version` 的成功响应（PascalCase，与 server NoBody 一致） */
export function versionResponse(label: string): SptResponse {
  const body = { Version: label };
  return { status: 200, body, text: JSON.stringify(body) };
}

/** 用单个 fake 连接构造 SptClient（默认配置 username=Overseer 以启用会话获取） */
export function fakeClient(
  connection: SptConnection,
  anchorVersion: string = DEFAULT_ANCHORED_VERSION,
  session: { username?: string; password?: string } = { username: "Overseer" },
): SptClient {
  return new SptClient({
    host: connection.host,
    candidatePorts: [connection.port],
    anchorVersion,
    username: session.username,
    password: session.password,
    connect: () => connection,
  });
}
