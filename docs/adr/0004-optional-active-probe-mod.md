# 可选活跃探针：零桥策略的渐进增强例外

ADR-0003 确立局外状态零桥（不写任何 server mod）。ticket 08 引入一个例外：为实现"跟随玩家活跃 profile"（`ProfileActivityService` 仅被需独立鉴权的 Blazor 管理页消费，无公开路由），写一个**接口面积极小**的 C# server mod，仅暴露一个只读路由 `/spt/runtime/active-profiles`。与当初否决的聚合桥不同：该探针不承担 schema 归一职责，缺失时 MCP 静默退回自动选择规则（渐进增强而非硬依赖），且 `StaticRouter` 机制在 4.1→5.0 无破坏性变更，即使 5.x mod 接口大改，重写成本以小时计。快照主体仍保持零桥。
