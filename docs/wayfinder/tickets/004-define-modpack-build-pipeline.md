# Ticket: Define modpack build pipeline stages

> Label: `wayfinder:grilling`
> Status: **closed** (2026-08-02)
> Blocks: #3
> Blocked by: #1, #2

## Resolution

Pipeline confirmed as 6-stage skeleton:

```
[1] 意图理解        用户输入(一句话/mod清单) -> AI 解析为功能需求列表
       |
[2] mod 匹配        功能需求 -> 检索归档 mod（有就直接用）
       |                          |
       |                    [2b] mod 开发（没有就现写）
       |                          |
[3] 冲突分析        选定的 mod 集合 -> 元数据级冲突检测报告
       |
[4] 人工审查        冲突报告 -> 用户决定取舍
       |
[5] 构建           用户确认后的清单 -> MO2 profile（A 方案为主）
       |
[6] 验证           MO2 启动 SPT -> mod 加载确认（日志解析）
```

**Key decisions:**
1. **Entry**: Both explicit mod list and one-sentence intent are valid; pipeline trunk designed for mod list, intent-driven discovery as optional pre-stage
2. **Skeleton**: 6 stages confirmed; [2] and [2b] are parallel branches, not serial
3. **Build output**: Option A (MO2 VFS) as primary for both development AND distribution; players receive an MO2-loadable modpack and can adjust mods themselves; Option B (self-contained) reserved for rare special cases. **Consequence: usvfs 3-process-chain propagation validation becomes a hard prerequisite.**
4. **Verification**: Level B -- MO2 launches SPT, Server starts, Launcher connects, game reaches main menu, each mod's loading confirmed via server log + BepInEx console output parsing. Level C (raid smoke test) as optional manual deep verification.

**Dependencies surfaced:**
- usvfs process propagation empirical test (blocks all of [5][6])
- spt MCP server tool inventory (needed by [3], design informed by #1 conflict taxonomy)
- SPT-specific MO2 game plugin (needed by [5], design informed by #2 findings)

## Question

What are the stages from "I want a modpack" to "here's a working SPT installation"?

The feasibility report (v3.0, SPT 3.11.4 baseline) proposed:
1. IL decompilation analysis pipeline
2. Functional domain configuration model
3. Self-contained distribution (bypass MO2)

But decisions have changed: MO2 is retained, IL decompilation is deferred, and the target is now SPT 4.1.

**Design the pipeline:**
- What are the stages? (e.g., intent -> mod selection -> conflict analysis -> human review -> build -> verify -> package)
- What is the input and output of each stage?
- Where does the human make decisions?
- Where does the AI execute mechanically?
- What tools does each stage use? (`spt` MCP, MO2 control plane, knowledge base)
- How does MO2 fit into the build? (Is the output an MO2 profile? A self-contained directory? Both?)

**Constraints:**
- Input: local Forge archive only (no live API)
- Human decides inclusion/exclusion; AI provides analysis and executes
- MO2 is the mod management layer
- Target: SPT 4.1
