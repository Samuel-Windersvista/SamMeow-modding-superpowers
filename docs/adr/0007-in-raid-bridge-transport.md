# 局内桥传输：插件内嵌 localhost HTTP，MCP 按需拉取

状态：已接受（2026-09-14） | 决策者：Overseer | 范围：tarkov-runtime-MCP Phase 2

## 背景

局内状态只存在于游戏进程内存中，Phase 2 的 Client Bridge 必须把它暴露给外部 MCP（Node.js）。候选形态：

- **A 插件内嵌本机 HTTP 服务（拉取）**：BepInEx 插件监听 `127.0.0.1`，MCP 按需请求。
- **B 写文件**：插件定期写 JSON，MCP 读文件。
- **C 独立 sidecar 进程（推送）**：插件推送状态到中间进程，MCP 查询 sidecar。
- **D 经 SPT server 转手**：插件上报 server（server mod 缓存），MCP 复用 Phase 1 通道读取。

判据：MCP 侧复用、空闲开销（帧率敏感）、新增进程数、可调试性、生态先例。

生态先例（调研结论）：TechHappy Web Minimap 以 BepInEx 插件内嵌 HTTP（EmbedIO）导出玩家/bot 位置；`rkuhn153/bepinex-mcp` 在 BepInEx 6 IL2CPP 下用 `HttpListener`（后台线程监听 + 主线程 tick 采样缓冲）；SPT-RPC 证明「插件导出 → Node 进程消费」链路成立。

## 决策

采用 **A**：

- 插件内嵌 HTTP 服务，仅绑定 `127.0.0.1`（默认端口 `49777`，可配）。
- 主线程采样循环写线程安全缓冲；HTTP 线程只读缓冲，不直接访问游戏对象。
- MCP 按需拉取（pull）：无人请求时零额外计算；调试可直接用浏览器/curl。
- MCP 侧以 `BridgeConnection` 抽象封装传输——B/C/D 作为未来替换路径保留（接缝而非承诺）。

## 后果

- 只新增 1 个组件（插件）；无额外常驻进程；空闲零计算。
- 端口冲突需处理（可配 + 启动失败可诊断）；Windows 下绑 `127.0.0.1` 无需 URL ACL/管理员。
- 推送类能力（事件流实时性、历史缓冲）不在本形态内；若未来需要，按 C 形态演进（sidecar 消费同一 HTTP 端点，插件侧不变）。
- 传输可替换性由 MCP 侧接缝保证；工具层与 schema 不感知传输细节。
