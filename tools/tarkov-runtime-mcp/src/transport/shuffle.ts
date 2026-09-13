// =============================================================================
// SPT 5.0 请求/响应 shuffle 编解码
//
// 等价实现自 SP-Tushonka.Server.Core/Utils/RequestEncryptionUtil.cs
// （5.0x-dev, HEAD ff0bf3281）。要点：
//   Entry(index) = (1624453 * (0x65F6D + index) + 1023920427) % 0x8ED7A18D
//   startEntry   = frameLength % 0xAAB (2731)
//   DeShuffleAsync：offset 从 1 升序到 len-1，交换 (offset, Entry(offset+startEntry)%offset)
//   ShuffleInPlace：offset 从 len-1 降序到 1，交换公式相同
// 两者互为逆操作（同一交换序列倒序执行可还原），故：
//   发送请求体 -> 施加降序交换（与 ShuffleInPlace 相同）
//   接收响应体 -> 施加升序交换（与 DeShuffleAsync 相同）
//
// 帧布局：[4 字节小端 payload 长度][payload][padding]；padding 由 server 随机生成，
// 解码时按头部长度截断即可。我们发送的请求 padding 为 0（server 会忽略多余 padding）。
//
// 数值安全：所有运算为 32 位无符号整数。JS number 在 < 2^53 内整数精确，
// 1624453 * (0x65F6D + index) 在 index < 5e9 时安全（远大于任何真实 HTTP body）。
// =============================================================================

import { RUNTIME_ERROR_CODES } from "../types.js";
import { SptRuntimeError } from "../errors.js";

const ENTRY_MULTIPLIER = 1624453;
const ENTRY_OFFSET = 0x65f6d; // 417645
const ENTRY_ADDEND = 1023920427;
const ENTRY_MODULUS = 0x8ed7a18d; // 2396496269
const START_ENTRY_MODULUS = 0xaab; // 2731
export const FRAME_HEADER_LENGTH = 4;

/** RequestEncryptionUtil.Entry 的等价实现 */
export function shuffleEntry(index: number): number {
  const value = (ENTRY_MULTIPLIER * (ENTRY_OFFSET + index) + ENTRY_ADDEND) % ENTRY_MODULUS;
  return value >>> 0;
}

/**
 * 把 payload 编码为可发送给 SPT server 的 shuffle 请求帧。
 * 与 server DeShuffleAsync 互为逆操作。
 */
export function shuffleFrame(payload: Buffer): Buffer {
  const frame = Buffer.allocUnsafe(FRAME_HEADER_LENGTH + payload.length);
  frame.writeUInt32LE(payload.length, 0);
  payload.copy(frame, FRAME_HEADER_LENGTH);
  applyPermutation(frame, true);
  return frame;
}

/**
 * 还原 server ShuffleInPlace 混淆过的响应帧，返回 payload 切片。
 * 与 server ShuffleInPlace 互为逆操作。
 */
export function deshuffleFrame(frame: Buffer): Buffer {
  if (frame.length < FRAME_HEADER_LENGTH) {
    return Buffer.alloc(0);
  }
  const copy = Buffer.from(frame);
  applyPermutation(copy, false);
  const size = copy.readUInt32LE(0);
  if (size > copy.length - FRAME_HEADER_LENGTH) {
    throw new SptRuntimeError(
      RUNTIME_ERROR_CODES.INTERNAL_ERROR,
      `deshuffle 帧长度非法：声明 ${size}，帧长 ${frame.length}`,
    );
  }
  return copy.subarray(FRAME_HEADER_LENGTH, FRAME_HEADER_LENGTH + size);
}

/** descending=true 为编码（ShuffleInPlace），false 为解码（DeShuffleAsync） */
function applyPermutation(frame: Buffer, descending: boolean): void {
  const length = frame.length;
  if (length < FRAME_HEADER_LENGTH) {
    return;
  }
  const startEntry = length % START_ENTRY_MODULUS;
  if (descending) {
    for (let offset = length - 1; offset > 0; offset--) {
      const swap = shuffleEntry(offset + startEntry) % offset;
      const tmp = frame[offset];
      frame[offset] = frame[swap];
      frame[swap] = tmp;
    }
  } else {
    for (let offset = 1; offset < length; offset++) {
      const swap = shuffleEntry(offset + startEntry) % offset;
      const tmp = frame[offset];
      frame[offset] = frame[swap];
      frame[swap] = tmp;
    }
  }
}
