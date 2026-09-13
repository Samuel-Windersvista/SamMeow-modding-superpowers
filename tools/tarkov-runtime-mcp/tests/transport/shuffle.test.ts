import { describe, expect, it } from "vitest";

import {
  FRAME_HEADER_LENGTH,
  deshuffleFrame,
  shuffleEntry,
  shuffleFrame,
} from "../../src/transport/shuffle.js";

// 独立参考实现：用 BigInt 精确复算 server 的 Entry/置换，避免与被测实现同源。

const REF_MULTIPLIER = 1624453n;
const REF_OFFSET = 417645n;
const REF_ADDEND = 1023920427n;
const REF_MODULUS = 0x8ed7a18dn;
const REF_START_MODULUS = 2731;

function referenceEntry(index: number): number {
  return Number((REF_MULTIPLIER * (REF_OFFSET + BigInt(index)) + REF_ADDEND) % REF_MODULUS);
}

function referencePermute(frame: Buffer, descending: boolean): Buffer {
  const out = Buffer.from(frame);
  const length = out.length;
  const startEntry = length % REF_START_MODULUS;
  const offsets: number[] = [];
  if (descending) {
    for (let offset = length - 1; offset > 0; offset--) offsets.push(offset);
  } else {
    for (let offset = 1; offset < length; offset++) offsets.push(offset);
  }
  for (const offset of offsets) {
    const swap = referenceEntry(offset + startEntry) % offset;
    const tmp = out[offset];
    out[offset] = out[swap];
    out[swap] = tmp;
  }
  return out;
}

function referenceEncode(payload: Buffer): Buffer {
  const frame = Buffer.alloc(FRAME_HEADER_LENGTH + payload.length);
  frame.writeUInt32LE(payload.length, 0);
  payload.copy(frame, FRAME_HEADER_LENGTH);
  return referencePermute(frame, true);
}

function randomBuffer(length: number, seed = 12345): Buffer {
  const buffer = Buffer.allocUnsafe(length);
  let state = seed;
  for (let i = 0; i < length; i++) {
    state = (state * 1103515245 + 12345) & 0x7fffffff;
    buffer[i] = state & 0xff;
  }
  return buffer;
}

describe("shuffle: Entry", () => {
  it("与 BigInt 参考实现对任意 index 一致", () => {
    for (const index of [0, 1, 2, 10, 255, 2730, 2731, 100_000, 1_234_567]) {
      expect(shuffleEntry(index)).toBe(referenceEntry(index));
    }
  });
});

describe("shuffle: 加解密往返", () => {
  it("空 payload 往返", () => {
    const payload = Buffer.alloc(0);
    const restored = deshuffleFrame(shuffleFrame(payload));
    expect(restored.equals(payload)).toBe(true);
  });

  it("短文本 payload 往返", () => {
    const payload = Buffer.from("hello spt 5.0", "utf8");
    expect(deshuffleFrame(shuffleFrame(payload)).equals(payload)).toBe(true);
  });

  it("随机二进制 payload 往返（含跨 0xAAB 边界的长度）", () => {
    for (const length of [1, 3, 4, 17, 1000, 2731, 2732, 40_000]) {
      const payload = randomBuffer(length, length + 7);
      expect(deshuffleFrame(shuffleFrame(payload)).equals(payload)).toBe(true);
    }
  });

  it("编码结果与独立参考实现逐字节一致", () => {
    const payload = randomBuffer(5000, 99);
    expect(shuffleFrame(payload).equals(referenceEncode(payload))).toBe(true);
  });

  it("混淆后与明文帧不同（非平凡 payload）", () => {
    const payload = randomBuffer(256, 42);
    const plain = Buffer.concat([Buffer.from([0, 1, 0, 0]), payload]);
    expect(shuffleFrame(payload).equals(plain)).toBe(false);
  });

  it("解码自参考实现编码的帧可还原 payload", () => {
    const payload = randomBuffer(4096, 2024);
    const encoded = referenceEncode(payload);
    expect(deshuffleFrame(encoded).equals(payload)).toBe(true);
  });

  it("不足 4 字节的帧解码为空", () => {
    expect(deshuffleFrame(Buffer.from([1, 2, 3])).length).toBe(0);
  });
});
