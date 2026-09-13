// =============================================================================
// zlib 压缩/解压
//
// SPT server 对请求体与响应体使用 ZLibStream（RFC1950，带 zlib 头），
// 对应 Node 的 zlib.deflate / zlib.inflate（非 raw deflate）。
// =============================================================================

import { deflate, inflate } from "node:zlib";

/** 压缩为 zlib 帧（RFC1950） */
export function compressPayload(data: Buffer): Promise<Buffer> {
  return new Promise((resolve, reject) => {
    deflate(data, (error, result) => (error ? reject(error) : resolve(result)));
  });
}

/** 解压 zlib 帧 */
export function decompressPayload(data: Buffer): Promise<Buffer> {
  return new Promise((resolve, reject) => {
    inflate(data, (error, result) => (error ? reject(error) : resolve(result)));
  });
}

/**
 * 按 server 同款 CMF/FLG 头判断是否为 zlib 帧
 * （SptHttpListener.cs:90-97：cmf & 0x0F == 8 且 (cmf<<8|flg) % 31 == 0）。
 */
export function looksZlib(data: Buffer): boolean {
  if (data.length < 2) {
    return false;
  }
  const cmf = data[0];
  const flg = data[1];
  return (cmf & 0x0f) === 8 && (((cmf << 8) | flg) % 31) === 0;
}
