# 独立 tarkov-runtime-MCP，不并入 spt-MCP

运行时游戏状态读取能力做成新 MCP（tarkov-runtime-MCP），现有 spt-MCP 保持纯离线分析面。理由：两者生命周期（进程依赖 vs 随时可用）、传输语义（HTTPS/zlib/cookie/加密 + 未来 IPC vs 本地文件读取）、版本耦合（桥组件与 SPT 版本硬绑定 vs 跨版本静态分析）完全不同；合并会让故障域与版本升级波及面翻倍，且工具列表长期挂着一半"进程未运行不可用"的工具。证据见 `docs/research/spt-runtime-state-export.md`。
