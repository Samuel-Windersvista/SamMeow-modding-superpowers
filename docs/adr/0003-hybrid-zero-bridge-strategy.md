# 混合桥策略：局外零桥直连路由，局内 BepInEx 客户端桥

初版方案（Q8）是"瘦 server bridge"：写 SPT server mod 注册聚合端点，适配逻辑收在桥内。Overseer 指出 SPT 5 的 server mod 扩展接口未完工、预期大改，当前唯一可用的稳定接口在 BepInEx 客户端侧。重新裁决为混合策略：

- **局外状态（首版）：零桥。** MCP 直接编排 SPT server 现有 `/client/*` 路由（profile/traders/quests/hideout），不编写任何 server mod。依据：未完工的是 mod 扩展接口，而 `/client/*` 是游戏客户端赖以运行的通信协议， bleeding-edge 也必须可用。schema 归一逻辑收进 MCP（单版本线 scope 下"MCP 保持版本无关"的论点已不成立）。版本门禁（Q6-a/Q7）相应从桥内挪到 MCP 连接握手时执行。server mod 加载清单无路由可查，以解析 server 日志文件兜底并在 schema 标注来源。
- **局内状态（Phase 2）：BepInEx 客户端桥。** 使用当前唯一稳定的 mod 接口；MO2 overlay 交付（A-1 假设的验证推迟到 Phase 2）。

后果：首版无任何游戏侧组件，`tarkov-runtime-bridge` 目录延到 Phase 2 创建；若 5.x server mod 接口日后稳定且有聚合性能/派生态需求，可再补 server 桥。
