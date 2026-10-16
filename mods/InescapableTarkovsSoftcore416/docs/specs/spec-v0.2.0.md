# Spec: Inescapable Tarkov's Softcore v0.2.0 —— 配置外置化与数据全量对齐（SPT 4.1.6 线）

> **Status:** ready-for-agent ｜ 目标版本：v0.2.0 ｜ 日期：2026-10-16
> 来源：grill-with-docs（两轮，2026-10-16，全部决策经用户确认）+ 5 路只读侦察 + 全量对齐枚举
> 关联：v0.1.0 规格快照 `mods/InescapableTarkovsSoftcore416/docs/specs/spec-v0.1.0.md`；本目录 `align-report-2026-10-16.txt`（全量对齐枚举报告）与 `align_enum.py`（复跑脚本）
> 锚点：SPT 4.1.6（build 40743 / EFT 0.16.9 / net10.0）｜ 目标实例：MO2 `Norvinsk_Forever`（profile Default），游戏根 `SPT_41x`

## Problem Statement

玩家在《永远的诺文斯克》里使用 `[5]软核大修-InescapableTarkovsSoftcore` v0.1.0（由旧包四组数据来源重组而成：Softcore / True Items Redux / 反重力臂章 / 更大背包）。三个问题：

1. **可编辑面不足**：只有一份 softcore 风格的 `config.json` 暴露在外；True Items 六表、臂章表、背包表、Softcore 四张数据表全部内嵌 DLL，玩家无法查看与编辑，也无法按组微调。
2. **数据与当前整合包不一致**：
   - **无效**：22 条旧包自定义 stim 占位 + 1 条背包引用在全整合包（基础库 + 内容 mod）中不存在；
   - **读不到（阶段过早）**：8 条背包 + 1 条 Vinyl 记录由包内内容 mod（WTT-Artem / TGC-NG / Epic AIO）在运行时注入，但本 mod 处理阶段过早（日志实证：本 mod 14:39:02 完成时，Croupier/TGC 尚未完成装载 14:39:05/14:39:08），持续告警且不生效；
   - **覆盖缺口**：臂章 4（基础库）+ 11（WTT）款、背包 11（基础库）+ 3（WTT）款未纳入。
3. **潜伏缺陷**：臂章/近战父类 ID 互换（来源 mod 原始代码同样互换）——默认全开时行为等价，但单开关语义与日志标签错位。

## Solution

v0.2.0 = 「配置外置化」+「数据全量对齐（零缺失）」+「阶段修正」+「缺陷修复」：

- **外置化**：新增随包分发的可编辑文件（`config.json` 保持现状、不拆分）——`config/trueitems/*.json`（6）、`config/armbands.json`、`config/backpacks.json`、`config/softcore/*.json`（4）。
- **外置语义**（与 `config.json` 同构）：外部优先；缺失→从内嵌模板重建（告警）；损坏→告警 + 回落内嵌；支持 JSONC 注释与尾随逗号。
- **数据对齐（零缺失）**：A 类无效删除；B 类阶段后移使其生效（不删）；C 类按既有风格补档；softcore 表引用清理。
- **缺陷修复**：臂章/近战父类互换纠正（含注释与测试标签）。
- **版本与验收**：0.1.0 → 0.2.0；重部署后服务器日志中本 mod 的「未找到/未命中/跳过」类告警 = 0。

## User Stories

1. 作为玩家，我希望 True Items 六张表以独立文件暴露在 mod 目录，以便直接编辑堆叠数值。
2. 作为玩家，我希望臂章表（减重/堆叠）以文件暴露，以便微调。
3. 作为玩家，我希望背包表（网格尺寸/过滤）以文件暴露，以便微调。
4. 作为玩家，我希望 Softcore 四张数据表（scavcase / fleamarket / crafting）一并暴露，以便调整奖励池与配方。
5. 作为玩家，我希望外置文件带中文注释说明（JSONC），以便读懂每一项。
6. 作为玩家，我希望删除某个外置文件后重启会自动重建默认文件，以便随时"恢复出厂"。
7. 作为玩家，我希望外置文件写坏时给出告警并继续用内嵌默认运行，服务器不崩溃。
8. 作为玩家，我希望修改外置文件后重启服务器即生效，无需重装。
9. 作为玩家，我希望外部文件存在时优先生效，内嵌数据仅作兜底。
10. 作为玩家，我希望不再出现任何"未找到/未命中，已跳过"的告警（零缺失）。
11. 作为玩家，我希望 WTT/TGC 等内容 mod 注入的 8 条背包条目真实生效（扩容）。
12. 作为玩家，我希望 Vinyl record（Epic AIO）的堆叠修改真实生效。
13. 作为玩家，我希望新增臂章（prestige_3/4/5、discord 及 WTT 11 款）按既有色阶风格获得减重与堆叠。
14. 作为玩家，我希望补档背包（基础库 11 款 + WTT 3 款）获得符合既有风格的尺寸。
15. 作为玩家，我希望整合包中新增的同类物品（食品饮料/医疗/贵重杂物/衣物等）按既有风格纳入 True Items 表。
16. 作为玩家，我希望无效的旧包 stim 占位与失效背包条目被删除，不再残留死引用。
17. 作为玩家，我希望 softcore 四表的引用与当前整合包一致（无效项清理、品类引用核实）。
18. 作为玩家，我希望臂章/近战开关语义正确（关"臂章"只影响臂章；反之亦然）。
19. 作为玩家，我希望日志中模块计数与标签一一对应、可核对。
20. 作为维护者，我希望外置化不改变 `config.json` 的结构与既有心智模型（追加式，不拆分）。
21. 作为维护者，我希望新增"共享外部文件解析"这一个高位置接缝（单缝），不散落新接缝。
22. 作为维护者，我希望单测覆盖：外部覆盖生效 / 缺失重建 / 损坏回落 / JSONC 解析 / 条目级容错。
23. 作为维护者，我希望数据钉值测试随对齐更新（条目数、抽查值）。
24. 作为维护者，我希望 `build.ps1` 幂等产出包含全部外置模板（overlay 结构新增 `config/` 子树）。
25. 作为维护者，我希望版本号升至 0.2.0，README / 移植记录同步更新。
26. 作为维护者，我希望部署仍经 MO2 overlay + profile 备份，并以服务器日志验收（同类告警 = 0）。
27. 作为玩家，我希望出现问题时可按既有方式回滚（禁用/移除 overlay + 恢复 profile 备份）。
28. 作为玩家，我希望本版不引入存档破坏性变更，不必开新档。
29. 作为维护者，我希望处理阶段后移经日志实测核验，且内部模块固定顺序（G6→G1→G4→G5→G2→G3→G7）不变。
30. 作为维护者，我希望对齐清单有据可查（枚举报告与脚本随规格归档于本目录）。
31. 作为玩家，我希望外置文件命名清晰、注释为中文，便于社区分享配置。

## Implementation Decisions

**D1 外置文件集合与布局（追加式）**
- `config.json` 保持单文件（总开关 + softcore 配置 + overrides），**不拆分**。
- 新增 `config/` 子树（与内嵌资源同名）：
  - `config/trueitems/{barter,clothing,keycards,medicals,partsnmods,provisions}.json`
  - `config/armbands.json`（22+15 条目标结构：id → weight/name；stackSize 仍由 `config.json` 承载）
  - `config/backpacks.json`（43−1+14 条目标结构：id → 尺寸/清过滤）
  - `config/softcore/{scavcase,fleamarket,crafting-rebalance,crafting-recipes}.json`
- 数据主体保持原结构（不改 schema）；文件头加中文 JSONC 注释（来源/编辑须知）。
- 语义：文件即权威；同名外置文件存在即取代内嵌资源。

**D2 外置加载语义（与 config.json 同构）**
- 外部优先；缺失 → 从内嵌模板重建（一行告警）后加载；解析失败 → 告警 + 回落内嵌（不崩溃）。
- 条目级容错：单条目非法仅告警跳过该条目，不整体失败。
- 实现：新增一个共享的「外部文件解析」助手（模目录定位沿用既有 `ResolveModDirectory` 模式）；三套既有数据加载器（TrueItems 资源加载器 / FeatureTables / Softcore 资源加载器 + 共享嵌入 JSON 缓存）统一改走该助手；config 加载器保持现状。
- JSONC（注释 + 尾逗号）支持与 `config.json` 一致。

**D3 数据全量对齐（A/B/C；零缺失）**
- **A 删除**（包中不存在）：22 条 stim 占位（medicals）+ 1 条背包（`6621b28d9411498998d408c3`）；softcore 三组待分类引用（scavcase 5 / fleamarket 20 / crafting-rebalance 5）逐条分类（品类引用 / 物品引用 / 内部 id）后清理或保留并注明；crafting-recipes 的 12 条系配方自身 `_id`（保留，非死引用）。
- **B 生效**（阶段后移，不删）：8 条背包（WTT-Artem 2 / TGC-NG 6）+ Vinyl 记录 1 条（Epic AIO）——由内容注入后可达并真实生效。
- **C 补档**（按既有风格）：臂章 **+15**（基础 4 + WTT 11）；背包 **+14**（基础 11 + WTT 3）。值分配：臂章按既有减重档位（-6/-8/-10/-15/-20/-25，按稀有度/系列归档）；背包按同类尺寸风格，且对旧包曾被移除的 9 条先做体积/网格核查。trueItems 同类新增：按品类纳入口径（食品饮料→provisions、医疗→medicals、贵重杂物→barter、衣物→clothing）补条目，由工单基于合并物品清单枚举；`partsnmods` 维持 `Active=false` 现状。
- 值真相：以「当前整合包实际内容 + 既有风格」为唯一依据；上游 mod 无 4.x 新版本（已双重证否），不引入上游数据刷新。

**D4 处理阶段后移（B 类生效的关键）**
- 目标：模块处理发生在包内内容 mod（WTT-ContentBackport / WTT-Armory / WTT-Artem / Epic AIO / TGC-NG / Russian-SF-Pack 等注入者）完成物品注入之后。
- 4.1 事实：阶段常量 Watermark(0) / Preload(100000) / GameCallbacks(200000) / TraderRegistration(300000) / Routers(400000) / HandbookCallbacks(500000) / … / PostLoad；数据库编辑的常规窗口为 `OnLoadOrder.PostLoad + n`；加载排序 = TypePriority 升序 + ModGuid 字母序（无 loadBefore/loadAfter）。
- 现状证据：v0.1.0 在 `Preload + 1` 处理（日志 14:39:02 vs Croupier 14:39:05 / TGC 14:39:08）→ 内容物品不可见。
- 决策：入口 TypePriority 后移至 `PostLoad + n`（候选；以实现期运行时枚举与实测为准）；内部模块固定顺序不变；如个别组需保留早相位，逐组论证并记录。
- 验收：重部署日志中 B 类物品的「未找到」告警 = 0，且模块变更计数覆盖相应条目。

**D5 父类互换修复**
- 纠正 armband / melee 父类常量（`5b3f15d486f77432d0509248` = ArmBand；`5447e1d04bdc2dff2f8b4567` = Melee）；README / `config.json` 注释、测试标签同步；默认行为不变（两族仍全解锁）。

**D6 构建与部署**
- `build.ps1` 增加 `config/` 模板拷贝（幂等；overlay 结构新增 `config/` 子树）。
- 部署同 v0.1.0 流程：profile 备份 → overlay 更新 → 服务器日志核验；release zip 依例归档。

**D7 版本与文档**
- 0.1.0 → 0.2.0；README 配置章节重写（外置文件集合 + 语义）；移植记录（port-notes）增补 v0.2.0 节（对齐结果 / 阶段实测 / 验收登记）；GLOSSARY / ADR 按需更新。

**D8 测试缝**
- 沿用现有加载器缝（各模块纯函数入口）；新增 **1 个**共享「外部文件解析」缝；不新增其他接缝。

## Testing Decisions

- **好测试的定义**：只测外部行为——给定（外部文件布局 + 合成内存表）→ 断言表变化与组报告；不测私有实现细节。
- **外置化专项**：外部覆盖生效（改值→行为变化）；缺失重建（文件出现且内容 = 模板）；损坏回落 + 告警；JSONC（注释/尾逗号）解析；条目级容错（单条坏→其余照常）。
- **数据对齐专项**：A 类删除缺席断言；C 类新增存在断言；钉值更新（barter 175 不变 / medicals 43→21 / armbands 22→37 / backpacks 43→56；抽查锚点值）。
- **回归**：现有 215 项更新后全绿；`build.ps1` 产物断言（含 `config/` 子树）。
- **不依赖游戏库的边界**：「无死引用」的端到端验证由部署期服务器日志验收承担（单测不引入游戏库）。
- **先例**：既有 resource tests（TrueItemsResourceTests / Softcore*ResourceTests / Antigrav/BackpacksModuleTests）与 ConfigLoaderTests 模式。

## Out of Scope

- SPT 5.0 线的回搬（v0.2.0 仅 4.1.6 线；5.0 线保持现状，后续按需）。
- 旧包自定义 stim（22 条）的移植/重建（本版删除；如需另行立项）。
- `config.json` 的拆分（保持单文件、追加式外置）。
- SPT 服务端配置编辑器 UI 集成；热重载（保持重启生效）；客户端 F12。
- 对整合包内其他 mod 的任何修改。

## Further Notes

- **对齐枚举报告**：本目录 `align-report-2026-10-16.txt`（BASE=4673 / MOD=3318，来自 387 个 CustomItems 文件 / 10 个提供者 mod）；复跑脚本 `align_enum.py`。
- **A 类清单（删除）**：
  - medicals 22 条：`cheeta_stim, boar_stim, adrenal_plus_stim, gepard_stim, glucose_stim, elephant_stim, ehets_stim, bbloodshot_stim, Amphetamine_stim, Caffeine_Stim, cocaine_stim, Glucose_shot, fastshot, mbloodshot_stim, meldonin_v13.5, Nic_Stim, obdolbos_v13.0, sbloodshot_stim, regen_stim, salnrg_stim, rebirth_stim, radioactiveblood_stim`
  - 背包 1 条：`6621b28d9411498998d408c3`
  - softcore 待分类：scavcase 5 条（`62710974e71632321e5afd5f / 62710a8c403346379e3de9be / 62710a69adfbd4354d79c58e / 6271093e621b0a76055cd61e / 62710a0e436dcc0b9c55f4ec`）；fleamarket 20 条（`5b47574386f77428ca22b2xx` 系列）；crafting-rebalance 5 条（`61c77cc6fcc1673f08540e9b / 5dc1f4d9e078d303d91b44c7 / 63966fccac6f8f3c677b9d89 / 5dd3c5a67da3785e63275437 / 6179ad0a6e9dd54ac275e3f2`）
- **B 类清单（后移生效）**：
  - 背包 8 条：`668bc5cd834c88e06b08b928 / 6673b1ac5cae0610f1079d71`（WTT-Artem）、`672e2e75b9082dbf88dd1dbd / 672e2e75c076d2093b05c764 / 672e2e7563b1a22a3c7b3895 / 672e2e754544ab54214fd56c / 672e2e75d276dfa8dd76770c / 672e2e75592eb3c91e248717`（TGC-NG）
  - Vinyl 1 条：`e44b40d309fa123643258996`（Epic AIO）
- **C 类清单（补档）**：
  - 臂章 15 条：基础 `6841b2506c1fcc41ed0db319 / 6841b3463fcc417de40a6768 / 6841b3ab322db20d190b4b9b / 688b2e574172ca83e70cf868`；WTT `68f25be683ec644ebf046787 / 68d41fde2937f3f17c04864d / 68f8ccb11cc53ada67036e27 / 68f8ccbc4c78615ba6079b20 / 68d41fb0a1c275dbaa0798bf / 68d65766916a108d7a023c98 / 68f25b772605ccea240dd684 / 68f8ccf06a6869fe97026c3b / 68d41e65a706eba9a204ed27 / 68d41f017cb8c03da8079004 / 68fb9480e6ad35a767066feb`
  - 背包 14 条：基础 `56e294cdd2720b603a8b4575 / 5c0e805e86f774683f3dd637 / 5d5d940f86f7742797262046 / 5f5e46b96bdad616ad46d613 / 6034d2d697633951dc245ea6 / 656ddcf0f02d7bcea90bf395 / 656e0436d44a1bb4220303a0 / 656f198fb27298d6fd005466 / 67458730df3c1da90b0b052b / 674da107c512807d1a0e7436 / 674da9cf0cb4bcde7103c07b`；WTT `68947ab5a733b1602007e2fe / 68947ad3e4bf255d1b0ca75c / 68947a8ce4bf255d1b0ca759`
- **风险与约束**：阶段后移的跨 mod 交互需以实现期实测为准（内部 D5 顺序不变）；softcore 品类引用需对照 4.1.6 跳蚤分类表核实；WTT 注入的精确相位以实现期 DLL/日志核验为准。
- **部署验收清单**：同类告警 = 0；各模块变更计数；抽查（新增臂章重量、WTT 背包尺寸、Vinyl 堆叠）；用户进游戏渐进验证。
- **快照惯例**：实现开始时将本规格镜像至项目 `docs/specs/spec-v0.2.0.md`（同 v0.1.0 惯例）。
