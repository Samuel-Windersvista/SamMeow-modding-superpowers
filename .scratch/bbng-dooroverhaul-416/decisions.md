# 决策记录 —— BB-NG × Door Overhaul 合并项目

> 临时 slug：`bbng-dooroverhaul-416`（正式命名见 R2-Q1）
> 研究依据：同目录 `research.md`（2026-10-17）
> 本文件随 grilling 轮次追加；关键决策已转写为 ADR（项目侧与 hub 侧，见文末落位）。

## Round 1（2026-10-17）已定

- **D1 交付形态**：合并为**单个 mod**；自持维护、独立演进（不跟随上游主线，上游仅作参考与灵感源）。→ 取消"共存版"交付里程碑；工程内部仍保留"先编译 BB 基线、再并入 DO"的工序。
- **D2 C4 去留**：保留；4.1 服务端重建作为**独立工单**（与客户端主线解耦）。采用 BB-NG 自带 C4 体系（不复活 3.11 的 DoorBreacher 物品）。
- **D3 配置载体**：统一**本地 BepInEx 配置（F12）**；弃用 `/backdoorbandit/load` 服务端下发（顺带消除其 null 崩溃隐患）。
- **D4 Fika**：首期不做；保留主插件"无 Fika 依赖事件桥"结构，不合入 Fika 子工程。
- **D5 发布形态**：按**可发布标准**执行（MIT × BSD-3 双许可署名、README、版本号规范）；开发先落 hub `mods/`，稳定后抽独立仓库；发布与否后议。
- **D6 DO 并入范围**：全量并入（破坏全套含实验铰链/调试面板 + 交互全套）；**工程性排除须可由开关解除**（边界见 R2-Q4）。
- **D7 默认游戏性**：宽松路线——KeyId 黑名单默认空、容器/后备箱默认可破、武器白名单与血量沿用 BB 默认；一切限制作为旋钮。
  - 背景注记：3.11 的收紧是"BB 细颗粒度 + 配套 mod 配置"环境下的沉浸式调参结果；当前 4.1.6 环境缺少那些搭配条件，无法复现，故从宽松起步，配置面保留调参能力。
- **D8 基座来源**：基于 BB-NG master（`451c6cb`，含 4.1 迁移）自建。

## 三条合并准则（从既有决策提炼，贯穿全部设计）

1. **锁门不豁免**：DO 的 `locked: untouchable`（`DoorDamageIntake.cs:158/218`）在合并版中删除——上锁门可获得完整"破门 + 破坏"待遇。这是本项目起点诉求。
2. **排除可开关**：一切"作者替玩家做的限制"都要暴露为配置（D6/D7）。
3. **默认宽松**：默认值偏向不限制，收紧是可选旋钮（D7）。

## Round 2（2026-10-17）已定

- **R2-Q1 命名与落位**：GUID `com.sammeow.doorbreach`；程序集名 `DoorBreachOverhaul`；显示名 `Door Breach Overhaul（破门大修）`；开发目录 hub `mods/SamMeow-DoorBreachOverhaul`；README/NOTICE 双上游署名（dvize / v-engelowski / kobethuy / Sage）；git 历史自合并快照起（不 fork 上游仓库）。
- **R2-Q2 命中语义**：双轨并行——锁血池（破门）与破坏模拟（毁伤）同时消费同一命中，互不阻塞。
- **R2-Q3 C4 语义**：引爆映射为完整破坏管线；不可破坏时回退强制开门。
- **R2-Q4 排除开关**：总开关解除全部"安全类"工程性排除；触发任务门/撤离门为高危组子开关（默认关）；物理不可行项保留排除并在调试面板显示原因。
- **R2-Q5 配置颗粒度**：基线全旋钮先行（BB-NG + DO + 新开关），数据驱动、留扩展位；3.11 关键旋钮未点名，不追。

## Frontier 关闭（2026-10-17）

设计树 frontier 已清空（Round 1 + Round 2 全部落定）。领域文档落位：

- 项目术语表：`mods/SamMeow-DoorBreachOverhaul/GLOSSARY.md`
- 项目 ADR：`mods/SamMeow-DoorBreachOverhaul/docs/adr/0001-double-track-attack-semantics.md`、`0002-local-config-carrier.md`
- hub ADR：`docs/adr/0012-sammeow-doorbreachoverhaul-project-baseline.md`
- to-spec 完成：`spec.md`（Status: ready-for-agent）
- to-tickets 完成：`issues/01…09-*.md`（依赖序 + 阻塞边，均已 ready-for-agent）
- 下一步：逐工单 `implement`（当前 frontier = 工单 01）
