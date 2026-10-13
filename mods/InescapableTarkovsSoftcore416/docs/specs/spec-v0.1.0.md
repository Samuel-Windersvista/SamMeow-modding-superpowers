# Spec: Inescapable Tarkov's Softcore — SPT 4.1.6 版（v0.1.0）

> 状态：ready-for-agent ｜ 目标版本：v0.1.0 ｜ 日期：2026-10-12
> 发布：本文件为规格快照（项目 `docs/specs/`）；实现工单在 toolkit 仓库 `.scratch/inescapable-softcore-416/issues/`
> 来源：grill-with-docs 设计收敛（8 项决策，2026-10-12）+ 三路只读侦察（5.0 代码 API 触点盘点 / 六件套源 mod 源码清单 / 4.1.6 API 兼容矩阵）
> 术语：以项目 `GLOSSARY.md` 为准
> 锚点：SPT 4.1.6（build 40743 / EFT 0.16.9 / net10.0）｜目标实例：MO2 `Norvinsk_Forever`（profile Default），游戏根 `SPT_41x`
> 移植源：`SamMeow-Inescapable-Tarkovs-Softcore` 主树（HEAD `826150c`）；数值真相 = 其 `docs/specs/delta-table.md`（继承，不复制）

## Problem Statement

玩家在 SPT 4.1.6 的《永远的诺文斯克》（Norvinsk_Forever）整合包中游玩离线塔科夫。旧整合包 Life in Norvinsk v0.3.2 曾提供六组让单机体验更顺手的服务端行为（Samuel's Tweaks、True Items Redux 物品真实堆叠、藏身处免 FIR、反重力臂章、更大的背包、Softcore 经济与制造系统大修），但它们是 SPT 3.11 时代的 TypeScript mod，无法在 4.1.6 上运行；其最终数值还分散在「基础 mod + 沉浸感增强覆盖 + 难度：生存预设」三层目录中。上一目标线（SPT 5.0）已将六组功能重写为单一 C# 服务端 mod，但只面向 5.0 实例。

玩家需要一个 SPT 4.1.6 版：单一、自包含、可一处调整的服务端 mod，在 Norvinsk_Forever 中复现旧包最终游玩态，配置全中文，源码存放于 toolkit 仓库 `mods/`。

## Solution

交付单一 C# 服务端 mod「Inescapable Tarkov's Softcore」（4.1.6 线，v0.1.0）：

- 从 5.0 实现移植：复用其代码转录、内嵌数据表、默认值差异表与测试套件；源 TS 仅作语义参照；
- 自包含实现七组功能（六组 + 战局时长倍率），不依赖任何源 mod、源 mod 目录保持只读；
- 单一 `config.json`（每组独立开关 + 细项参数，中文注释），改后重启 SPT 服务器生效；
- 开箱默认值 = 旧包最终游玩态（以继承差异表为唯一数值真相）；
- 部署为 MO2 overlay `[5]软核大修-InescapableTarkovsSoftcore`，向目标实例投影（服务端 mod 至 `SPT_Runtime\user\mods\`）；部署前做 profile 备份；
- 启动日志按组汇报变更计数与警告；缺失物品 ID 安全跳过；
- 单测（变换层接缝）+ 服务器启动日志 + 玩家进游戏逐组验收；
- 缺陷清单 10 项成果随移植继承；4.1.6 特有的数据形态差异按本 spec 口径处置并登记。

## User Stories

1. 作为玩家，我希望 Norvinsk_Forever 开箱即得旧包「最终游玩态」数值，以便不逐件安装 3.11 时代的旧 mod。
2. 作为玩家，我希望所有数值集中在一个中文注释的 JSON 配置文件中，以便不需要跨多个 mod 目录找配置。
3. 作为玩家，我希望每组功能有独立开关，以便逐组对比或排除故障。
4. 作为玩家，我希望关闭任意一组时其余组不受影响，以便稳定做 A/B 验证。
5. 作为玩家，我希望 mod 只作用于服务端、不写入游戏本体文件，以便卸载后游戏回到原版行为。
6. 作为玩家，我希望有一个全局战局时长倍率，以便自主控制单局长度。
7. 作为玩家，我希望护甲与弹挂甲冲突修复生效，以便配装不受互斥限制。
8. 作为玩家，我希望臂章与近战武器可被掠夺，以便击杀者可搜刮。
9. 作为玩家，我希望三格弹匣（容量 10–50）缩为两格，以便节省背包格位。
10. 作为玩家，我希望堆叠上限为「真实」数值（多数 2–5、注射器 8、配件不堆叠、钥匙卡不堆叠），以便经济与收纳体验与旧包一致。
11. 作为玩家，我希望堆叠可按物品/父类粒度调整（overrides），以便微调个别物品。
12. 作为玩家，我希望藏身处建造/升级不再要求 FIR，以便建造不被战局绑定。
13. 作为玩家，我希望 22 款臂章按色阶减重 6–25kg 且可堆叠 5，以便轻装潜行与收藏。
14. 作为玩家，我希望主流背包扩容（如 6Sh118 8×9）且容器过滤被清空，以便收纳自由。
15. 作为玩家，我希望 4.1.6 数据中缺失的物品 ID 被安全跳过并告警，以便服务器不因数据差异而崩溃。
16. 作为玩家，我希望 Softcore 的渐进式安全容器与仓库（新档从 2×2 腰包起步）生效，以便获得渐进成长曲线。
17. 作为玩家，我希望藏身处加速按最终态生效（制造 3×、建设 50×、燃料 4×、ScavCase 速度 ×0.5、比特币按其终值），以便节奏合适。
18. 作为玩家，我希望跳蚤市场为和平主义形态且 1 级开放、仅全新品、价格 ×1.5，以便经济符合旧体验。
19. 作为玩家，我希望以物易物经济按最终态生效（现金 5%、价差 30%、报价 5–13、最多 4 换 1），以便交易有趣且可行。
20. 作为玩家，我希望保险数值按最终态生效（Prapor 70% 返还 / 240–360 分钟、Therapist 60% / 120–240 分钟），以便风险与回报符合旧体验。
21. 作为玩家，我希望弹药堆叠 ×5、货币堆叠关闭、技能经验增益关闭，以便数值与生存版一致。
22. 作为玩家，我希望收藏家任务要求为「20 级 + 交付 1 张 Gamma」（忠实旧包 SURV 终态），以便收藏家任务线符合旧体验。
23. 作为玩家，我希望任务变更（Crisis、Drip-Out、Collector）按稳定 id 匹配、不受本地化名影响，以便在 4.1.6 上稳定生效。
24. 作为玩家，我希望修改不会让现有存档无法加载，是否开新档由我自己决定，以便按自己的节奏升级。
25. 作为玩家，我希望服务器启动日志按组汇报变更计数与警告，以便确认生效并快速排查。
26. 作为维护者，我希望单测断言差异表关键值，以便对数值回归有自动化防护。
27. 作为维护者，我希望移植源自 5.0 线的缺陷修复成果（10 项）随移植继承，以便质量闭环。
28. 作为维护者，我希望构建脚本一键产出 overlay 内容（DLL + 中文配置），以便快速迭代与部署。
29. 作为维护者，我希望部署经 MO2 overlay 且先做 profile 备份，以便随时回滚。
30. 作为维护者，我希望版本锚定 SPT 4.1.6 build 40743，且升级时以差异表 + 单测回归，以便长期可维护。
31. 作为玩家，我希望配置注释为中文，以便直接读懂每一项。
32. 作为玩家，我希望未知配置键或非法值只产生警告而不崩溃，以便配置试验安全。
33. 作为维护者，我希望未来新增大组 = 增加一节配置 + 一个模块，以便低摩擦扩展。
34. 作为玩家，我希望所有组可同时开启且互相兼容（内部应用顺序确定），以便默认全开即可玩。
35. 作为维护者，我希望 4.1.6 适配与原项目的接缝一一对应，以便两条目标线可平行维护、按需回搬修复。

## Implementation Decisions

**D1 交付形态**：单程序集 C# 服务端 mod（net10.0；`user/mods` 布局；恰一个 `IModMetadata` 实现；无 package.json）。标识：GUID `com.sammeow.inescapable-softcore`、显示名 Inescapable Tarkov's Softcore、作者 SamMeow、版本 0.1.0、`SptVersion ~4.1.6`、许可 MIT。移植源自 5.0 仓库主树（HEAD `826150c`）。

**D2 加载阶段**：`IOnLoad` @ `OnLoadOrder.Preload + 1`。4.1.6 已核验：`OnLoadOrder` 常量同构（Preload=100000），运行时在数据库导入后执行 `[0, 200000)` 段。例外：若实现期发现某变更被 SPT post-DB 覆盖，该组改 `PostLoad` 并记录。

**D3 模块结构**：与 5.0 同构——Core（配置加载与校验、日志、应用编排器）+ 七组变换模块（G1 SamuelTweaks / G2 TrueItems / G3 NoFirHideout / G4 Antigrav / G5 Backpacks / G6 Softcore 含 20 个 changer / G7 RaidDuration）+ 内嵌 JSON 数据资源。不新增架构、不新增接缝。

**D4 变换层接缝**：沿用 5.0 已确认接缝——每组模块暴露纯逻辑入口「（表集合, 组配置）→ 组报告（变更计数 + 警告列表）」；测试直接构造 POCO。零新接缝。

**D5 内部应用顺序**（固定）：G6 → G1 → G4 → G5 → G2 → G3 → G7。理由与 5.0 相同：G2 靠后使逐物品显式值优先于 G6 通用倍率；G1 与 G6 弹挂甲修复同向；G3/G7 独立。

**D6 配置**：单一 `config.json`（随包分发默认文件、全中文注释与每项说明；解析容忍 `//` 注释与尾逗号）。结构 = 5.0 版减去 `samuelTweaks.customBackground` 键（本期移除该功能）。未知键 → 警告；缺键 → 内置默认；类型非法 → 警告回落；字典型叶键（overrides）按对象校验；配置缺失时从内嵌模板重建并告警。不注册 SPT 服务端配置编辑器（v0.2 候选）。

**D7 4.1.6 API 适配清单**（移植技术核心；全部经运行时真值核验，编译级）：

| # | 组件 | 4.1.6 差异 | 改法 |
|---|------|-----------|------|
| 1 | OtherTweaksChanger 弹药堆叠 | `StackMaxSize` 为 `int?` | 空值守卫后计算 |
| 2 | SamuelTweaksModule 可掠夺 | `Unlootable` 为 `bool?` | 判真（`!= true` 跳过） |
| 3 | FasterCraftingTimeChanger（2 处）+ FasterBitcoinFarmingChanger + ScavCaseChanger | `ProductionTime` 为 `double?` | 空值守卫后缩放 |
| 4 | FasterHideoutConstructionChanger | `ConstructionTime` 为 `double?` | 空值守卫后缩放 |
| 5 | TraderChangesChanger Skier 欧元 | `MinSalesSum` 为 `long?` | 空值兜底 0 |
| 6 | EconomyOptionsChanger 跳蚤等级 | `MinUserLevel` 为 `int`（5.0 为 double） | 取整赋值 |
| 7 | NoFirHideoutModule | `IsSpawnedInSession` 为 `bool?` | 判真置 false |
| 8 | ModMetadata | SptVersion 需目标区间 | 改 `~4.1.6`（否则 ModValidator 拒载） |
| 9 | 构建引用 | 运行时路径指向目标实例 | 经 toolkit `SptRoot` 约定指向 `SPT_41x\SPT_Runtime`；引用 DLL 清单不变 |
| 10 | 警告纪律 | 部分站点产生可空警告（CS8602 级） | 审查清零或显式抑制；不开 WarningsAsErrors |

明确不改（已核验同签名，易误判点）：六表构造器注入、五个 Config（Hideout/ScavCase/Ragfair/Trader/Insurance）注入、`ISptLogger<T>`、`ModHelper.GetAbsolutePathToModFolder`、`ListOrT<T>`、`StringToMongoIdConverter`、`MongoId` 隐式转换、全部枚举/静态常量（ItemTpl/BaseClasses/Traders/Money/HideoutAreas/EquipmentSlots/BonusType/CurrencyType/PlayerSideMask）、`RaidTimeAdjustmentService` 组合语义。

**D8 4.1.6 数据形态适配**：

- **任务 id 匹配**：任务变更一律按 id（4.1.6 的任务 `name` 同为 locale-key、且 Drip-Out 存在两组同名任务——与 5.0 结论一致且必要）。
- **Collector（用户裁决：忠实）**：A4S（AvailableForStart）重组为单条 Level 20 条件 + A4F 追加 Gamma 交付（Value=1）；4.1.6 原版 136 条任务前置按 SURV 语义有意丢弃；上线后进游戏复核接取/完成/存档。
- **Crisis / Drip-Out**：按 5.0 代码逻辑执行；以 4.1.6 实际数据实测结果登记于移植记录（Crisis 的 Level 条件在 4.1.6 是否存在以运行时数据为准）。
- **数据缺口**：背包表 9 条 id、Barter 表 1 条 id 在 4.1.6 缺失——告警跳过（继承语义），首启日志登记计数（预期值：背包 9、barter 1）。
- **G7 × RaidTimeAdjustmentService**：4.1.6 语义同构（服务读 master、写 clone）——倍率参与其计算、不被抹除。

**D9 日志与错误隔离**：统一日志接口（`ISptLogger<T>`）、组前缀；每模块一行 summary；缺物品/未知键/非法值 → 警告；单组异常捕获并报错，不阻断其他组（继承）。

**D10 构建与部署**：Release 构建 → `scripts/build.ps1` 幂等组装 overlay（DLL + `config.json` 逐字节拷贝；本期无静态件、无启动器背景）。部署经 MO2 MCP：profile 备份 → 创建 overlay `[5]软核大修-InescapableTarkovsSoftcore` → 填充 → 启用 → 置于 [5] 组顶部；meta.ini 的 comments/notes 按本包中文惯例填写。回滚：禁用/移除 overlay + 恢复 profile 备份。

**D11 验证分工**：agent 负责构建、单测、overlay 组装与部署、服务器日志解析核验；用户负责启动 MO2 + SPT 服务器并进游戏按清单逐组验收（agent 不代启动 MO2/游戏）。

**D12 文档与工单**：本 spec 在项目 `docs/specs/`；实现工单在 `.scratch/inescapable-softcore-416/issues/`；项目含中文 README（完整配置结构与七组说明）、移植记录、`GLOSSARY.md`、`docs/adr/`。

**D13 许可与来源**：新代码 MIT；数据表来源（MIT/NCSA）与移植沿革记录于 README「来源与许可」。

## Testing Decisions

- **好测试的定义**：只测外部行为——给定（配置 + 合成内存表）→ 断言表变化与组报告；不测私有实现细节。
- **主接缝**：变换层（继承移植源；零新接缝，已与用户口径一致）。
- **测试工程**：xUnit（net10.0）；引用目标运行时 DLL（`Private=true` 以支持测试宿主反射加载）+ `FrameworkReference Microsoft.AspNetCore.App` + `Spectre.Console.Ansi` 直引；测试桩沿用（日志桩、表载体桩）。
- **覆盖样例**（继承并适配）：
  - 配置层：默认文件加载 = 全组启用；每个键存在默认值；未知键 → 警告；缺键 → 默认；**新增断言：`customBackground` 键不存在**。
  - G1：带 RigLayoutName 弹挂 → BlocksArmorVest=false；臂章 → Unlootable=false；宽 1/高>2/容量 10–50 弹匣 → 高 2 且 ExtraSizeDown−1；容量 51 → 不变。
  - G2：AA Battery → 5；Physical Bitcoin → 5；LEDX → 3；Crickent 保持 12；medicals StackMult=2（注射器实得 8）；keycards 不堆叠；partsnmods Active=false → 无变化；未命中 ID → 跳过 + 告警。
  - G3：合成需求 `isSpawnedInSession` → false；无该键 → 不变。
  - G4：White → (−6, 5)；Prestige 2 → (−25, 5)；22 条全命中；单品覆盖配置生效。
  - G5：6Sh118 → 8×9 且 filters 清空；仅基础项（9 条）不生效；缺失 ID 跳过。
  - G6：跳蚤等级 → 1；制造 ×3；弹药堆叠 ×5（含边界）；保险 70%/80%；收藏家 20 级 + 需求数 1；开关关闭 → 零变更。
  - G7：倍率 2.0 → 地图时限翻倍；1.0 → 不变。
- **适配专项**：可空守卫路径（D7 表 1–7 对应变更）各至少 1 条「可空为 null 时零变更不崩溃」断言。
- **验收（不进 CI）**：服务器启动日志出现各组 summary；玩家进游戏按核对清单逐组验证（臂章重量与堆叠、背包尺寸与过滤、堆叠上限、藏身处建造、跳蚤入口与等级、商人/保险、战局时长）。
- **先例**：移植源测试工程的组织方式（变换层接缝 + 锚点断言）。

## Out of Scope

- 启动器背景 `customBackground`（本期移除——用户裁决；后续如需另立）。
- 客户端插件 PerformanceTweaks（延后项；需针对 EFT 0.16.9 客户端重新审查，另案处理）。
- 六组以外功能：旧包其余 mod（Realism、barter_economy、PathToTarkov、HomeComforts、战局大修等）。
- SPT 5.0 线的任何改动（5.0 仓库保持现状；移植单向）。
- 热重载（重启生效已确认）；SPT 服务端配置编辑器 UI 集成（v0.2 候选）；F12 客户端配置。
- 直接写入游戏目录（硬规则：只经 MO2 overlay 投影）。
- mod 显示名与服务器日志中文化（保持英文；中文仅限配置文件与项目文档）。

## Further Notes

- **开放验证项**（需真服务器/进游戏）：
  1. G7 端到端（含 Scav 随机缩减与战局时限的组合表现）；
  2. Collector 接取/完成/存档兼容；
  3. 旧存档超上限堆叠 × `InvalidStackObjectsCountFix` 迁移行为；
  4. 数据缺口告警计数（背包 9、barter 1 预期）；
  5. 各项 Config 的实际游戏表现（barter/报价数/保险/Fence 参数等）；
  6. 日志读取路径实况核对（MO2 运行时 `overwrite` 重定向）。
- **已知边界**：`faster*` 倍率方向语义（时间 ÷ 倍率，如 0.3 → 时间 ×3.33）——忠实保留（移植源 T6 结论继承），README 需保留该注释。
- **部署保护**：MO2 关闭状态下离线部署可行（mod 目录 + meta.ini + modlist 行）；**部署前先 profile 备份**；DLL 更新前确认 游戏/服务器/启动器 全关。
- **Windows 编码陷阱**：含中文的 `.ps1` 用 UTF-8 with BOM；避免 `Get-Content -Raw` 管道改写；日志读取指定 UTF-8 并二次核对。
- **产物布局**：`scripts/build.ps1` 幂等产出 `build/overlay/SPT_Runtime/user/mods/com.sammeow.inescapable-softcore/{DLL, config.json}`；发行归档至项目 `release/[5]软核大修-InescapableTarkovsSoftcore-<版本>.zip`。
- **移植记录**（随实现产出）：D7 适配清单逐项执行结果、D8 数据形态实测（Crisis 条件实况、缺口计数）、部署记录与验收结果。
