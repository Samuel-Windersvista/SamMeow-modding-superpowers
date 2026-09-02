---
version: [4.1]
domain: client
topic: migration
source: curated
---

# 客户端混淆名映射（SkillsExtended 实战解析）

> 状态：已验证（2026-08-05，用 AsmResolver 从 4.1 Assembly-CSharp 实读解析）
> 用途：客户端 mod 迁移的混淆名 -> 4.1 真名映射（成员签名匹配法的实战范例）
> 方法：AsmResolver 读 4.1 程序集 + 成员签名匹配（方法名/属性名/事件签名）

---

## 1. 核心映射表（SkillsExtended 8 个混淆名）

| 3.11 混淆名 | 4.1 真名 | 判定依据 |
|------------|---------|---------|
| `SkillManager.GClass2024` | `SkillManager.Buff` | 有 Id + BuffType 属性（elite buff） |
| `SkillManager.SkillBuffClass` | `SkillManager.Buff` | 4.1 类合并（Buff 抽象，有 Id+BuffType） |
| `SkillManager.SkillBuffAbstractClass` | `SkillManager.Buff` | 同上 |
| `SkillManager.SkillActionClass` | `SkillManager.SkillAction` | 动作类 |
| `GClass2823` | `EFT.HealthSystem.EffectsSettings` | 含 GetPersonalBuffSettings 方法 |
| `GClass2823.GClass2848` | `EffectsSettings+StimulatorSettings` | GetPersonalBuffSettings 所在 |
| `GClass2823.GClass2848.GClass2849` | `StimulatorSettings+StimulatorBuffSettings` | GetStringValue 所在 |
| `GStruct264` | `EFT.Trading.Trader+ItemPrice` | GetBarterPrice 返回类型（MongoID? + int） |
| `TraderAssortmentControllerClass` | `EFT.Trading.Assortment` | 有 GetBarterPrice/GetSchemeForItem/SelectedItem |
| `Class1930` | Assortment 嵌套（委托类） | barterScheme 求和（可直接 LINQ 替代） |
| `GClass2064` | `EFT.Trading.Requisite` | 有 RequiredItemsCount 属性 |
| `SkillClass` | `EFT.Skill` | 技能类 |
| `AbstractSkillClass` | `EFT.BaseSkill` | 抽象基类 |
| `MasterClass.SkillClass` | `EFT.Mastering` | OnMasteringExperienceChanged 事件参数 |
| `ISession` | `EFT.IAbstractSession` | 会话接口 |
| `IHealthEffect` | `EFT.HealthSystem.IHealthEffect` | 命名空间化 |
| `DamageInfoStruct` | `EFT.Ballistics.DamageInfo` | 改名 |
| `ActionsReturnClass` | `EFT.Interactive.InteractionResult` | 门交互返回 |

## 2. 关键事实（4.1 反混淆范围）

1. **全量反混淆**：类名 + 嵌套类 + 方法名 + 字段名全部改名。3.11 里看似稳定的 `SkillManager.SkillBuffClass` 在 4.1 也不存在（0 次出现）。
2. **类合并**：3.11 的多个类在 4.1 合并（SkillBuffClass/GClass2024 -> Buff）。
3. **4.0->4.1 映射表不覆盖 3.11**：8 个混淆名都不在官方映射表（映射表是 4.0 的混淆编号，与 3.11 不对齐）。
4. **成员签名匹配可行**：通过方法名（GetPersonalBuffSettings）、属性名（RequiredItemsCount）、事件签名（OnMasteringExperienceChanged 参数类型）精确定位 4.1 对应类。

## 3. 成员签名匹配法（方法论）

对每个混淆名：
1. **读用法**：看 3.11 源码怎么用这个类型（方法/属性/字段/事件）
2. **找特征**：提取唯一成员名（方法名、属性名、事件名）
3. **搜 4.1**：用 AsmResolver 全类型搜索含该成员的类型
4. **验证**：确认基类关系、属性集、构造签名匹配

**成功率**：本案例 8/8 混淆名全部解析成功。工具：AsmResolver（SPT 服务器自带）。

## 4. Prepatcher 机制（mod 自定义 EBuffId）

SkillsExtended 的自定义技能 buff 通过 **Prepatcher**（BepInEx patchers，Mono.Cecil）在启动时注入 3.11 的 `EFT.EBuffId` 枚举（数值从 1000 起）：
- FirstAidHealingSpeed/ResourceCost/MovementSpeedElite
- FieldMedicineSkillCap/DurationBonus/ChanceBonus
- UsecArSystemsRecoil/Ergo、BearAkSystemsRecoil/Ergo
- LockpickingTimeIncrease/ForgivenessAngle/UseElite
- SilentOpsIncMeleeSpeed/RedVolume/SilencerCostRed
- StrengthColliderSpeedBuff/Elite

**4.1 迁移**：Prepatcher 同样可用于 4.1 的 Assembly-CSharp（BepInEx 5.4.23 仍支持 patchers），注入同样的枚举成员。但需确认 4.1 的 EBuffId 枚举结构未变（只追加不冲突）。

## 5. 客户端迁移自动化率（实测修正）

| 阶段 | 自动化 | 说明 |
|------|--------|------|
| 混淆名解析 | **可工具化** | 成员签名匹配，8/8 成功 |
| 稳定类名替换 | 高（脚本） | 68 处替换 |
| API 类型适配 | 中（需判断） | GetBarterPrice/ItemPrice 等结构变化 |
| EBuffId 枚举注入 | 中（Prepatcher 适配） | 需改 Prepatcher 目标 |
| 深层逻辑适配 | 低（人工） | Buff 抽象类选择、SPT API 变化 |

## 6. 剩余工作清单（SkillsExtended 客户端）

已完成：混淆名解析、类名替换、EBuffId 处理、部分 API 适配。编译错误从 40+ 收敛到 ~8 个类型：

| 剩余项 | 3.11 | 4.1 | 状态 |
|--------|------|-----|------|
| 门交互 | `GetActionsClass` / `ActionsTypesClass` | `EFT.Interactive.InteractionResult` 结构不同 | 需适配 |
| 摄像机 | `CameraClass.Instance.Distance()` | `EFT.CameraControl.CameraManager.Instance.Distance()` | 已映射，待验证 |
| 击杀统计 | `LocationStatisticsCollectorAbstractClass.OnEnemyKill` | `EFT.BaseStatisticsManager.OnEnemyKill` | 已映射，待验证 |
| 移动速度 | `MovementContext.method_0/method_28/Struct303` | 4.1 内部方法改名 | **需逻辑重写** |
| 匍匐状态 | `ProneMoveStateClass` | `EFT.ProneMovePlayerState` 等 | 需选择 |
| SPT 工具 | `Utils.Get` | SPT 4.1 客户端模块 API | 需查 spt-common |

**结论**：混淆名解析已完成（核心方法论验证），剩余是 ~8 个类型的深度 API 适配 -- 独立子任务，建议专门会话完成。
