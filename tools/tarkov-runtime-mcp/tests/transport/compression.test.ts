import { describe, expect, it } from "vitest";

import {
  compressPayload,
  decompressPayload,
  looksZlib,
} from "../../src/transport/compression.js";

describe("compression: zlib 处理", () => {
  it("压缩后解压可还原 JSON 文本", async () => {
    const source = Buffer.from(JSON.stringify({ Version: "SPT 5.0.0 (BEM) ff0bf32" }), "utf8");
    const compressed = await compressPayload(source);
    const restored = await decompressPayload(compressed);
    expect(restored.equals(source)).toBe(true);
  });

  it("压缩帧被识别为 zlib，明文不被识别", async () => {
    const compressed = await compressPayload(Buffer.from("payload", "utf8"));
    expect(looksZlib(compressed)).toBe(true);
    expect(looksZlib(Buffer.from("plain text", "utf8"))).toBe(false);
    expect(looksZlib(Buffer.alloc(0))).toBe(false);
    expect(looksZlib(Buffer.from([0x78]))).toBe(false);
  });
});
