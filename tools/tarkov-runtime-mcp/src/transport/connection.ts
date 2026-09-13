// =============================================================================
// S1 接缝：传输层抽象
//
// 工具层与握手层只依赖本接口。所有协议细节（zlib 压缩、PHPSESSID cookie 会话、
// 5.0 shuffle 加解密）都封装在 SptConnection 实现之后，测试用 fake 实现驱动。
// =============================================================================

export type SptHttpMethod = "GET" | "POST" | "PUT";

export interface SptRequestOptions {
  method: SptHttpMethod;
  /** 绝对路径，如 "/singleplayer/settings/version" */
  path: string;
  /** 请求体（对象 -> JSON -> zlib -> shuffle），GET 忽略 */
  body?: unknown;
  /** 额外请求头 */
  headers?: Record<string, string>;
}

export interface SptResponse {
  status: number;
  /** 解析后的响应体：JSON 可解析时为对象，否则为原始文本 */
  body: unknown;
  /** 解压/解 shuffle 后的原始文本 */
  text: string;
}

export interface SptConnection {
  readonly host: string;
  readonly port: number;
  readonly baseUrl: string;
  request(options: SptRequestOptions): Promise<SptResponse>;
}
