# SPT 5.0x-dev 现状报告（SP-Tushonka 线）

> 日期：2026-09-05 | 对象：`E:\云文件\GitHub\SamMeow_SP-Tushonka_source_code`（SP-Tushonka/server-csharp clone）
> 方法：git 历史/分支/tag 实证分析（4619 commits 全览、merge-base、ls-remote、diff --stat）
> 同期动作：本地 main 已对齐 4.1.5（`7d7add55`）；约定：**此后 "4.1.X" 一律指 SP-Tushonka 线**（官方 GitHub 三仓库 2026-08-11 归档，官方最后一个正式版 = 4.1.2）
> 前置：`docs/eft-0.16.9.5-spt412-性能复查报告.md`（4.1.2 客户端性能基线）；本报告为分叉决策文档之一

---

## 0. 一句话结论

**5.0x-dev 不是 SP-Tushonka 的创作，是官方 SPT 5.0 重写线的延续。** 2025-01-03 从 `Initial commit` 原生起步（共 4619 commits），主体开发由官方核心 Chomp（2120 commits）与 cwx/CWXDEV（787）完成；官方归档后由 SP-Tushonka（Archangel）看管。**未完成、无配套、不可用——预览性质，看戏可以，上车不行。4.1.5 是当前唯一值得跟的稳定线。**

## 1. 血统与时间线

| 阶段 | 时间 | 内容 | 主要作者 |
|---|---|---|---|
| 原生重写爆发期 | 2025-01~02 | `Initial commit` → 强类型 Table 体系初步成型（~1800 commits） | Chomp / cwx / Cj / Alex |
| 沉寂期 | 2025-02~2026-06 | 偶尔推进；4.1 线成为官方发布主线 | — |
| 续推期 | 2026-06-13~06-23 | `Changes to get to main menu`、`Implement placeholder quest routes`、`Add new maps + dumps`——把 5.0 推进到能进主菜单 | Chomp 团队 |
| SP-Tushonka 托管期 | 2026-08-09 起 | `Update naming`（品牌化）、Bump version、`Dumpy wumpy, models, code changes, idk anymore`、定期 merge 4.1x-dev | Archangel |

tip：`85c5f906`（2026-09-04，merge 4.1x-dev）；落后 4.1.5 一天（缺 bundle 签名放宽 2 个 commit）。

## 2. 架构现状

| 维度 | 状态 |
|---|---|
| 目标框架 | net10.0（同 4.1.5），SptVersion=5.0.0 |
| 数据层 | **强类型 Table 模型**：`SPTarkov.Server.Core.Models.Spt.Tables`（16 个 `*Table` record：Bots/Hideout/Locales/Locations/Match/Templates/Traders/Globals/Season/Shop/Server/Settings）；实体 `SPT_Data/database/*.json` 仍在 |
| 兼容面 | 命名空间/AssemblyName 全部保留 `SPTarkov.*`（SPTarkov.Server.Core.dll、IMod 接口、DI 注解不变）；compatibleTarkovVersion 仍 0.16.9.40743 |
| 工具链 | Ceciler（JsonExtensionData IL 补丁）流程完整保留；Tools/Testing 目录齐全 |
| 数据库增量 | 相对 4.1.5：+707k 行（quests.json ±27.8万、dialogue.json +33.4万、globals +6.3万、各图 staticLoot +4-5万/图）——主要来自重 dump 的官方数据 |

## 3. 完成度：明确未完成

- `Implement placeholder quest routes`（任务线路还是占位符）
- `DataCallbacks.cs`: "TODO: base implementations to get the game loading" / "Still not sure if this is correct lol, FAFO I guess"
- Chomp 原话：`addded more types, some are unfinished, ill come back to these tomorrow`
- 散布 TODO：Dialogue/Inventory/Hideout/Insurance 各控制器 15+ 条未决项
- **配套缺失**：SP-Tushonka/modules 只有 3.11.x-dev/4.0.x-dev/master；SP-Tushonka/launcher 只有 4.1.x-dev/master/mod-manager——**无任何 5.0 分支**，无法发行、无法独立运行

## 4. 与 4.1.5 的关系

- 4.1x-dev 修复被持续 merge 进 5.0x-dev（6-13 ~ 9-04 多次），两条线并行：4.1.X = 稳定线，5.0x-dev = 实验线
- 5.0x-dev 与 4.1 线**无共同开发祖先**（独立初始提交历史）——cherry-pick 跨线困难
- 未来若有 5.0 正式版：所有 C# server mods 需按 Table 模型重编译，目前无任何社区 mod 适配先例

## 5. 决策指引

- 整合包生产：**不碰 5.0x-dev**
- 独立分叉（搁置中）：基线只能选 4.1.5 稳定线；5.0x-dev 整体放弃（独立历史线 + 未完成 + 无配套）
- 观察建议：每季度瞄一眼 diff（方向预览：强类型 Table 模型可能是 4.1 后续形态）

## 6. 附：审计报告检索记录

用户记忆中的"SPT 源代码审计报告"（可改进点清单）经检索未找到落盘版本：
- `SamMeow-modding-superpowers/docs` 全量 grep "审计/改进点"：仅存在客户端线报告（eft-0.16-性能分析与优化mod可行性报告.md / eft-0.16.9.5-spt412-性能复查报告.md / PerformanceTweaks413-实施计划.md）
- `NorvinskStalker_knowledgeBase/docs`：无匹配
- 结论：疑为会话内产物未落盘，或与上述客户端报告混淆。暂行搁置；后续重启时建议立专档（服务器端 C# 审计 + 可改进点清单），落盘防丢。

---
