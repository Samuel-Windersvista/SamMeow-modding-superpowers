// =============================================================================
// SptClient：候选端口探测 + 握手缓存 + 工具层数据组装
//
// 单实例模型（无多实例寻址）。connect() 首次调用执行探测与门禁并缓存；
// instances() 返回探测到的可达实例信息。
// =============================================================================

import { SptRuntimeError } from "../errors.js";
import {
  RUNTIME_ERROR_CODES,
  type DiscoveredInstance,
  type HandshakeResult,
  type InstancesData,
  type ServerStatusData,
} from "../types.js";
import type { SptConnection } from "../transport/connection.js";
import { extractVersionLabel, performHandshake } from "./handshake.js";
import { VERSION_ENDPOINT } from "./version.js";

export interface SptClientOptions {
  host: string;
  candidatePorts: number[];
  anchorVersion: string;
  /** 端口 -> 连接工厂；测试注入 fake SptConnection */
  connect: (host: string, port: number) => SptConnection;
}

/** 探测结果（connection 仅内部使用，不进入工具输出） */
interface DiscoveryEntry extends DiscoveredInstance {
  connection: SptConnection;
}

export class SptClient {
  private readonly options: SptClientOptions;
  private cachedHandshake: HandshakeResult | null = null;
  private cachedDiscovery: DiscoveryEntry[] | null = null;

  constructor(options: SptClientOptions) {
    this.options = options;
  }

  get host(): string {
    return this.options.host;
  }

  get candidatePorts(): number[] {
    return [...this.options.candidatePorts];
  }

  get anchorVersion(): string {
    return this.options.anchorVersion;
  }

  /** 探测候选端口，返回可达实例（不执行版本门禁） */
  async discover(): Promise<DiscoveryEntry[]> {
    if (this.cachedDiscovery) {
      return this.cachedDiscovery;
    }
    const found: DiscoveryEntry[] = [];
    for (const port of this.options.candidatePorts) {
      const connection = this.options.connect(this.options.host, port);
      try {
        const response = await connection.request({ method: "GET", path: VERSION_ENDPOINT });
        if (response.status < 200 || response.status >= 300) {
          continue;
        }
        const label = extractVersionLabel(response.body) ?? response.text;
        found.push({
          host: this.options.host,
          port,
          baseUrl: connection.baseUrl,
          versionLabel: label,
          reachable: true,
          connection,
        });
      } catch {
        // 端口不可达，继续探测下一个候选
      }
    }
    this.cachedDiscovery = found;
    return found;
  }

  /** 探测 + 版本门禁；成功结果缓存。失败抛结构化错误 */
  async connect(): Promise<HandshakeResult> {
    if (this.cachedHandshake) {
      return this.cachedHandshake;
    }
    const found = await this.discover();
    if (found.length === 0) {
      throw new SptRuntimeError(
        RUNTIME_ERROR_CODES.SERVER_UNREACHABLE,
        `未在候选端口发现 SPT server（host=${this.options.host}）`,
        { host: this.options.host, ports: this.options.candidatePorts },
      );
    }
    const handshake = await performHandshake(found[0].connection, this.options.anchorVersion);
    this.cachedHandshake = handshake;
    return handshake;
  }

  async instances(): Promise<InstancesData> {
    const found = await this.discover();
    return {
      count: found.length,
      model: "single-instance",
      instances: found.map(({ host, port, baseUrl, versionLabel, reachable }) => ({
        host,
        port,
        baseUrl,
        versionLabel,
        reachable,
      })),
    };
  }

  async serverStatus(): Promise<ServerStatusData> {
    const handshake = await this.connect();
    return {
      connection: {
        host: handshake.host,
        port: handshake.port,
        baseUrl: handshake.baseUrl,
      },
      version: handshake.version,
      anchor: handshake.anchor.raw,
      gate: handshake.gate,
      capabilities: {
        sections: ["server_status", "instances"],
        bridge: "not_installed",
      },
    };
  }
}
