# 11: trueItems 无效数据删除（22 条 stim 占位）

**What to build:** `medicals.json` 删除 22 条非 24-hex 的自定义 stim 占位条目（`cheeta_stim`、`boar_stim`、`adrenal_plus_stim`、`gepard_stim`、`glucose_stim`、`elephant_stim`、`ehets_stim`、`bbloodshot_stim`、`Amphetamine_stim`、`Caffeine_Stim`、`cocaine_stim`、`Glucose_shot`、`fastshot`、`mbloodshot_stim`、`meldonin_v13.5`、`Nic_Stim`、`obdolbos_v13.0`、`sbloodshot_stim`、`regen_stim`、`salnrg_stim`、`rebirth_stim`、`radioactiveblood_stim`）——全整合包（基础库 + 内容 mod）无提供者。medicals 43→**21**；测试钉值更新。

**Blocked by:** None（可立即开始）

**Status:** ready-for-agent

- [x] 删除 22 条（清单见上 / `align-report-2026-10-16.txt` §D）
- [x] `TrueItemsResourceTests` 钉值更新（medicals 计数与抽查）；全部测试绿
- [x] barter 的 Vinyl 条目与其余五表保持不变（Vinyl 属 B 类保留）

## Comments

- 已实现（2026-10-16）。删除结果：22 条非 24-hex 占位已删（cheeta_stim、boar_stim、adrenal_plus_stim、gepard_stim、glucose_stim、elephant_stim、ehets_stim、bbloodshot_stim、Amphetamine_stim、Caffeine_Stim、cocaine_stim、Glucose_shot、fastshot、mbloodshot_stim、meldonin_v13.5、Nic_Stim、obdolbos_v13.0、sbloodshot_stim、regen_stim、salnrg_stim、rebirth_stim、radioactiveblood_stim）；medicals List 43→**21**。21 条合法 24-hex 全部原值保留（含 Propital `5c0e530286f7747fa1419862` StackMaxSize=4、表级 StackMult=2、Active=true）。文件头 2 行注释未动；barter（含 Vinyl）、其余五表、src/ 均未改。
- 测试钉值：`TrueItemsResourceTests` medicals List.Count 43→21；Propital 抽查锚点保留（`Load_MedicalsSyringeEntryValue_IsFour_AndStackMultIsTwo` 与 `Load_BarterSamples_MatchDeltaTable` 等其余断言经核对不受影响）。
- grep 抽查确认：测试工程内仅 `TrueItemsModuleTests.cs:245` 引用占位 id（`cheeta_stim`）。该测试 `Apply_RealTemplates_PlaceholderStimIds_DoNotMatch_AndWarn` 的前提 = 真实模板中存在占位 id，被本工单移除后前提消失。处理：重命名为 `Apply_RealTemplates_UnmatchedIds_DoNotMatch_AndWarn`，告警锚点改由真实模板中合法但未命中目标表的 id（SJ1 `5c0e531286f7747fa54205c2`）承接；Propital=8 锚点保留，未弱化抽查。
- 验证：`dotnet test ... --artifacts-path .artifacts/its-11 -c Debug` → 228 通过 / 0 失败 / 0 跳过（基线 .artifacts/its-11-baseline 亦 228 绿，无新增回归）。
- 偏差：仅上述一处测试语义迁移（占位→合法未命中 id），无其他偏差。无遗留问题。
