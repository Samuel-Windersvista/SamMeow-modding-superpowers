# tarkov-runtime-MCP 只适配 SPT 5.x

初版曾规划跨 SPT 3.11/4.1/5.x 全版本线兼容（capability negotiation 或全工具对等方案都已讨论）；调查证实 3.11 server 为 Node/TypeScript（tsyringe DI），4.1 起为 C#/.NET，跨线适配需双语言 server 桥与至多三套 client patch 映射。Overseer 裁决收缩为只做 SPT 5.x：消除全部跨线适配成本，代价是放弃 3.11/4.1 环境（含现有 4.1 整合包主线）的运行时读取能力。版本线内的 5.0 漂移（bleeding-edge）由版本门禁与 capability 自报吸收。若未来确需 4.1 支持，因 4.1↔5.0 框架层无破坏性变更，C# 桥可低成本回 ported。
