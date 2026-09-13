// =============================================================================
// SptConnection 的 HTTP 实现
//
// 负责：请求体 zlib 压缩 + 按路径 shuffle、响应体 shuffle 还原 + zlib 解压、
// PHPSESSID cookie 会话保持。协议细节全部封装在此，工具层不可见。
// =============================================================================

import type { SptConnection, SptRequestOptions, SptResponse } from "./connection.js";
import { compressPayload, decompressPayload, looksZlib } from "./compression.js";
import { buildCookieHeader, extractSessionId } from "./cookie.js";
import { deshuffleFrame, shuffleFrame } from "./shuffle.js";
import {
  createNodeHttpsTransport,
  type HttpTransport,
  type NodeHttpsTransportOptions,
} from "./http-transport.js";

/** server 不 shuffle 请求的路径前缀（SptHttpListener.ShouldShuffleRequest） */
const NO_SHUFFLE_REQUEST_PREFIXES = ["/launcher", "/client/metadata", "/v2/shop", "/files"];

/** 按路径段匹配（对齐 ASP.NET Core StartsWithSegments 语义） */
function startsWithSegment(path: string, prefix: string): boolean {
  return path === prefix || path.startsWith(`${prefix}/`);
}

export function shouldShuffleRequest(path: string): boolean {
  return !NO_SHUFFLE_REQUEST_PREFIXES.some((prefix) => startsWithSegment(path, prefix));
}

/** server 响应 shuffle 规则：请求规则基础上再排除 /singleplayer（SptHttpListener.ShouldShuffleResponse） */
export function shouldShuffleResponse(path: string): boolean {
  return shouldShuffleRequest(path) && !startsWithSegment(path, "/singleplayer");
}

function parseJsonOrText(text: string): unknown {
  const trimmed = text.trim();
  if (!trimmed) {
    return null;
  }
  try {
    return JSON.parse(trimmed);
  } catch {
    return text;
  }
}

export interface HttpSptConnectionOptions extends NodeHttpsTransportOptions {
  /** 注入底层 HTTP 传输（单测用）；缺省走 node:https */
  transport?: HttpTransport;
}

export class HttpSptConnection implements SptConnection {
  readonly host: string;
  readonly port: number;
  readonly baseUrl: string;

  private readonly transport: HttpTransport;
  private sessionId: string | null = null;

  constructor(host: string, port: number, options: HttpSptConnectionOptions = {}) {
    this.host = host;
    this.port = port;
    this.baseUrl = `https://${host}:${port}`;
    this.transport = options.transport ?? createNodeHttpsTransport(options);
  }

  /** 当前持有的会话 id（未建立时为 null） */
  getSessionId(): string | null {
    return this.sessionId;
  }

  /** 显式注入会话 id（见 SptConnection.setSessionId 说明） */
  setSessionId(sessionId: string): void {
    this.sessionId = sessionId;
  }

  async request(options: SptRequestOptions): Promise<SptResponse> {
    const { method, path } = options;
    const headers: Record<string, string> = { ...options.headers };

    if (this.sessionId) {
      headers["Cookie"] = buildCookieHeader(this.sessionId);
    }

    let body: Buffer | undefined;
    if (method !== "GET" && options.body !== undefined) {
      const json = Buffer.from(JSON.stringify(options.body), "utf8");
      const compressed = await compressPayload(json);
      body = shouldShuffleRequest(path) ? shuffleFrame(compressed) : compressed;
      headers["Content-Type"] = "application/json";
      headers["requestcompressed"] = "1";
    }

    const raw = await this.transport({
      method,
      url: `${this.baseUrl}${path}`,
      headers,
      body,
    });

    const sessionId = extractSessionId(raw.headers["set-cookie"]);
    if (sessionId) {
      this.sessionId = sessionId;
    }

    const payload = shouldShuffleResponse(path) ? deshuffleFrame(raw.body) : raw.body;
    const text = looksZlib(payload)
      ? (await decompressPayload(payload)).toString("utf8")
      : payload.toString("utf8");

    return { status: raw.status, body: parseJsonOrText(text), text };
  }
}
