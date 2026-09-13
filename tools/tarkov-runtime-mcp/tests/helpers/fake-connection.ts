// 测试辅助：fake SptConnection（S1 接缝）与常用 fixture

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

export class FakeConnection implements SptConnection {
  readonly host: string;
  readonly port: number;
  readonly baseUrl: string;
  readonly requests: SptRequestOptions[] = [];

  constructor(
    private readonly responder: FakeResponder,
    host = "127.0.0.1",
    port = 6969,
  ) {
    this.host = host;
    this.port = port;
    this.baseUrl = `https://${host}:${port}`;
  }

  async request(options: SptRequestOptions): Promise<SptResponse> {
    this.requests.push(options);
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

/** 用单个 fake 连接构造 SptClient */
export function fakeClient(
  connection: SptConnection,
  anchorVersion: string = DEFAULT_ANCHORED_VERSION,
): SptClient {
  return new SptClient({
    host: connection.host,
    candidatePorts: [connection.port],
    anchorVersion,
    connect: () => connection,
  });
}
