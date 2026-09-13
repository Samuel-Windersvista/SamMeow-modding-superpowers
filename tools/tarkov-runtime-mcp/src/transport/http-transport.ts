// =============================================================================
// 底层 HTTP 传输（可注入）
//
// 默认实现走 node:https（SPT server 使用 HTTPS 自签名证书，必须关闭证书校验）。
// 单测注入 fake transport，以便在 S1 接缝上断言 cookie / shuffle / zlib 的外部行为。
// =============================================================================

import { request as httpsRequest } from "node:https";

export interface RawHttpRequest {
  method: string;
  url: string;
  headers: Record<string, string>;
  body?: Buffer;
}

export interface RawHttpResponse {
  status: number;
  headers: Record<string, string | string[] | undefined>;
  body: Buffer;
}

export type HttpTransport = (request: RawHttpRequest) => Promise<RawHttpResponse>;

export interface NodeHttpsTransportOptions {
  timeoutMs?: number;
}

export function createNodeHttpsTransport(
  options: NodeHttpsTransportOptions = {},
): HttpTransport {
  const timeoutMs = options.timeoutMs ?? 5_000;
  return (spec) =>
    new Promise<RawHttpResponse>((resolve, reject) => {
      const url = new URL(spec.url);
      const req = httpsRequest(
        {
          protocol: url.protocol,
          hostname: url.hostname,
          port: url.port || 443,
          path: `${url.pathname}${url.search}`,
          method: spec.method,
          headers: spec.headers,
          // SPT server 使用自签名证书，外部工具必须跳过校验
          rejectUnauthorized: false,
          timeout: timeoutMs,
        },
        (res) => {
          const chunks: Buffer[] = [];
          res.on("data", (chunk: Buffer | string) => {
            chunks.push(Buffer.isBuffer(chunk) ? chunk : Buffer.from(chunk));
          });
          res.on("end", () => {
            resolve({
              status: res.statusCode ?? 0,
              headers: res.headers,
              body: Buffer.concat(chunks),
            });
          });
        },
      );
      req.on("timeout", () => {
        req.destroy(new Error(`请求超时（${timeoutMs}ms）：${spec.url}`));
      });
      req.on("error", reject);
      if (spec.body && spec.body.length > 0) {
        req.write(spec.body);
      }
      req.end();
    });
}
