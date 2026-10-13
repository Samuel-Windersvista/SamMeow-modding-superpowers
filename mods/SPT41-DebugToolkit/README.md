# SamMeow.DebugToolkit（SPT 4.1.6）

> 目标线：**SPT 4.1.6** / EFT 0.16.9.40743 / Mono / BepInEx 5 / netstandard2.1
> 身份：GUID `com.sammeow.debugtoolkit` · 程序集 `SamMeow.DebugToolkit` · 版本 `0.1.0`
> 部署目标：MO2 实例 `Norvinsk_Forever`（SPT 4.1.6 / profile Default）
> 关系：与 5.0 线（`SamMeow-DebugToolkit`，IL2CPP / BepInEx 6 / net6.0）为**同身份版本线**——
> 共享 AssemblyName / GUID，独立源码、独立演进（见 ADR-0010）。

---

## 项目定位

把 SPT 4.1.6 整合包里分散的调试工具（DebugTooltip、DrakiaXYZ-BotDebug、DadGamerMode）
合并为一个原生客户端插件，解决**热键冲突、多份 F12 配置、多个加载日志点**，并填补
DadGamerMode 在 4.1.6 上的缺位（其上游停在 4.0.x 之前）。

四组能力：

| 组 | 来源（MIT 上游） | 能力概述 |
|----|------------------|----------|
| **Tooltip 调试信息** | DebugTooltip 5.0.0 (Tyfon) | 物品/空槽/改装槽/护甲/任务/任务条件/商人 ID 注入，三档剪贴板复制与 F9 快速开关 |
| **Bot 调试覆盖层** | DrakiaXYZ-BotDebug 1.8.0 | 8 档监视模式（含 `Minimized` 隐藏档与 BigBrain 两模式）+ 头顶覆盖层 + F10/F11 循环 |
| **无敌 / 部分无敌组** | DadGamerMode 1.9.3 (Props/dvize) | GodMode / Keep1Health / 忽略爆头 / 百分比伤害 / 无坠落伤害 + CODMode 自动治疗 |
| **便利组 QOL** | DadGamerMode 1.9.3 (Props/dvize) | 无限体力/能量/水分、瞬产、瞬建、换弹卸速、对敌伤害倍率、负重百分比 |

统一 F12 配置（6 节、键名英文、显示名中文、原生下拉/滑块）、统一热键（F9 / F10 / F11，均可配）、
版本门禁**仅警告不抛异常**。

---

## 功能总览

### 1. Tooltip 调试信息（DebugTooltip）

悬停以下 8 类界面元素时，在 tooltip 中追加对应的调试 ID 块：

| # | 界面 | 注入内容 |
|---|------|----------|
| 1 | 物品 tooltip（库存 / 交易） | Item ID、Tpl ID、Owner 容器名/ID、Address（grid 坐标/ID 或 slot ID/父物品名与 ID） |
| 2 | 空装备槽 | 槽位 ID、父物品 ID |
| 3 | 改装槽 | 槽位 ID、父物品 ID |
| 4 | 护甲槽 | 物品调试信息（护甲挂载核对） |
| 5 | 任务列表项 | Quest ID、状态 |
| 6 | 任务详情 | Quest ID、状态 |
| 7 | 任务目标 | Condition ID |
| 8 | 商人卡片 | Trader ID |

- **三档剪贴板复制**（悬停调试块时）：
  - `ctrl-c` 复制短 ID；
  - `ctrl-alt-c` 复制备用 ID（Tpl / 父 ID）；
  - `ctrl-shift-c` 复制完整调试块。
- **快速开关**：`F9` 免开 F12 即切换 tooltip 调试信息（键位可配）。
- 调试块**无延迟立即显示**；`Show Copy Instructions` 可隐藏复制提示文案。
- 全部补丁体 fail-open：补丁异常不穿透游戏调用链。

### 2. Bot 调试覆盖层（DrakiaXYZ-BotDebug）

战局内每个 bot 头顶显示调试覆盖层，`F10` / `F11` 循环切换 8 档监视模式（键位可配）：

| 模式 | 内容 |
|------|------|
| `Minimized` | 隐藏覆盖层（循环中的「关闭」档；不绘制任何文本） |
| `Behaviour` | Brain 名/策略、Layer、Node、EnterBy、PrevNode/ExitBy、距离 |
| `BattleState` | 弹药 Chamber/Mag/Total、Hits/Shoots、Reloading、CoverId、WeaponReady、Malfunction、GoalEnemy |
| `Health` | 分部位 HP + 黑屏 `(BL)` / 骨折 `(Broken)` 标记 |
| `Specials` | Profile ID 分块、WeapSpawn、AxeEnemy、Role、State、StateLc |
| `Custom` | Custom Data 文本（BigBrain 自定义层写入的调试数据，如可用） |
| `BigBrainLayer` | BigBrain 自定义 brain 层调试文本（依赖 BigBrain 1.5.0） |
| `BigBrainLogic` | BigBrain 自定义逻辑调试文本（依赖 BigBrain 1.5.0） |

循环顺序（枚举顺序，对齐上游 `Minimized = 0`）：`Minimized` → `Behaviour` → `BattleState` →
`Health` → `Specials` → `Custom` → `BigBrainLayer` → `BigBrainLogic` →（回到）`Minimized`。
`Minimized` 与 F12 `Enable` 总开关相互独立。

- **敌我颜色编码**：白 = 普通；绿 = 玩家所属；红 = 把我当敌人。
- **最大绘制距离与字号可配**（`MaxDrawDistance` / `FontSize`）。
- 覆盖层**默认关闭**，按需开启；每局自动挂载、退出自动清理、死亡条目 3 秒内移除。
- DLSS/FSR 开启时做屏幕缩放补偿，覆盖层位置保持正确。
- **硬依赖 BigBrain 1.5.0**（`xyz.drakia.bigbrain`）：缺失时插件不加载（见「互斥警示」）。

### 3. 无敌 / 部分无敌组（DadGamerMode）

战局内测试时免死，全部开关**默认关闭**、经 F12 实时生效：

| 能力 | 行为 |
|------|------|
| `God Mode` | 除坠落伤害外的全部伤害清零（坠落豁免为有意偏离上游，见偏差表） |
| `Keep 1 Health` | 选中部位在致死伤害下不再掉血；部位摧毁（流血/骨折致死路径）同步被抑制 |
| `Keep 1 Health Selection` | 作用范围：`All` 或 `Head And Thorax`（头+胸） |
| `Ignore Headshot Damage` | 命中头部时伤害清零 |
| `Percentage Headshot Damage` + `% Damage Received (Only Head)` | 头部独立百分比减伤（作用于全身百分比之后） |
| `% Damage Received (All Body)` | 全身百分比减伤（100 为正常） |
| `No Falling Damage` | 把 `FallSafeHeight` 拉满，地形测试不被摔死 |
| `CODMode` | 受击后延迟自动治疗（含流血/骨折/疼痛清理），可配治疗速率、等待时间、流血保留开关 |

### 4. 便利组 QOL（DadGamerMode）

藏身处与战局内的减负开关，全部**默认关闭**（数值项取上游默认）：

| 能力 | 行为 |
|------|------|
| `Infinite Stamina` | 身体体力 / 手部体力 / 氧气始终维持满值 |
| `Infinite Energy` | 能量始终维持满值（无需进食） |
| `Infinite Hydration` | 水分始终维持满值（无需饮水） |
| `Instant Production` | 藏身处制造即刻完成（排除比特币 / 燃料配方） |
| `Instant Construction` | 藏身处建造 / 升级计时即刻完成 |
| `Enable Reload/Unload Mag Speed` | 启用换弹 / 卸弹速度倍率改写（关闭时恢复默认） |
| `Reload Mag Speed` / `Unload Mag Speed` | 换弹 / 卸弹速度倍率（越小越快） |
| `Enemy Damage Multiplier` | 对非本玩家（敌人 / 其他 bot）造成的伤害倍率（1–20，默认 1） |
| `Item Total Weight %` | 物品总重百分比（100 为正常，越小负重越轻；须战局前设置） |

---

## F12 配置面

六节按数字前缀强制排序（ConfigurationManager 默认按节名排序）。键名保持英文（cfg 兼容），
F12 中**显示名为中文**；列表型配置用**原生下拉**（`AcceptableValueList`），数值项用**范围滑块**
（`AcceptableValueRange`）。4.1.6 = Mono / BepInEx 5，无 IL2CPP 的限制，故列表可安全用原生下拉。
新线无历史配置，**无配置迁移器**。

### 1. 通用

| 键 | 默认 | 范围/说明 |
|----|------|-----------|
| `Plugin Version` | `0.1.0` | 只读信息：本插件版本（F12 显示名「插件版本（只读）」） |

### 2. 提示信息

| 键 | 默认 | 说明 |
|----|------|------|
| `Show Debug Tooltip` | `true` | 是否在物品/任务/商人 tooltip 中追加调试信息块 |
| `Show Copy Instructions` | `true` | 是否显示复制提示文案 |
| `Toggle Shortcut` | `F9` | 免开 F12 切换 tooltip 调试信息 |

### 3. Bot 覆盖层

| 键 | 默认 | 范围/说明 |
|----|------|-----------|
| `Enable` | `false` | 覆盖层总开关（默认关闭，按需开启） |
| `ActiveMode` | `Behaviour` | 监视模式（**原生下拉**）：`Minimized` / `Behaviour` / `BattleState` / `Health` / `Specials` / `Custom` / `BigBrainLayer` / `BigBrainLogic`；`Minimized` = 隐藏覆盖层；无效值自动回退 `Behaviour` |
| `NextModeKey` | `F10` | 切换到下一个监视模式 |
| `PrevModeKey` | `F11` | 切换到上一个监视模式 |
| `MaxDrawDistance` | `1500` | 绘制 bot 调试框的最大距离（0–2000） |
| `FontSize` | `24` | 覆盖层字号（12–36） |

### 4. 无敌

| 键 | 默认 | 范围/说明 |
|----|------|-----------|
| `God Mode` | `false` | 除坠落伤害外的全部伤害清零 |
| `Keep 1 Health` | `false` | 选中部位在致死伤害下不再掉血（部位摧毁同步抑制） |
| `Keep 1 Health Selection` | `All` | **原生下拉**：`All` 或 `Head And Thorax` |
| `Ignore Headshot Damage` | `false` | 忽略爆头伤害（命中头部时清零） |
| `Percentage Headshot Damage` | `false` | 启用独立的头部百分比减伤 |
| `% Damage Received (Only Head)` | `100` | 头部伤害百分比（0–100，需上一项开启） |
| `% Damage Received (All Body)` | `100` | 全身伤害百分比（0–100，100 为正常） |
| `No Falling Damage` | `false` | 无坠落伤害 |

### 5. 恢复

| 键 | 默认 | 范围/说明 |
|----|------|-----------|
| `CODMode` | `false` | 受击后延迟自动治疗（含流血/骨折/疼痛清理） |
| `CODMode Heal Rate` | `10` | 每次治疗节拍为每个可治疗部位恢复的血量（0–100） |
| `CODMode Heal Wait` | `10` | 上次受击后需保持无伤多久（秒）才开始治疗（0–600） |
| `Bleeding Damage` | `false` | 开启后保留流血/骨折等负面效果；关闭时由 CODMode 周期清理 |

### 6. 便利

| 键 | 默认 | 范围/说明 |
|----|------|-----------|
| `Infinite Stamina` | `false` | 身体体力 / 手部体力 / 氧气满值 |
| `Infinite Energy` | `false` | 能量满值 |
| `Infinite Hydration` | `false` | 水分满值 |
| `Instant Production` | `false` | 藏身处制造即刻完成 |
| `Instant Construction` | `false` | 藏身处建造 / 升级即刻完成 |
| `Enable Reload/Unload Mag Speed` | `false` | 启用换弹 / 卸弹速度改写 |
| `Reload Mag Speed` | `0.85` | 弹匣装填速度倍率（0–0.85，越小越快） |
| `Unload Mag Speed` | `0.3` | 弹匣卸弹速度倍率（0–0.3，越小越快） |
| `Enemy Damage Multiplier` | `1` | 对敌伤害倍率（1–20） |
| `Item Total Weight %` | `100` | 物品总重百分比（0–100，100 为正常） |

---

## 构建

依赖 `mods/Directory.Build.props` 提供的 `SptRoot`（默认 `E:\Game\EFT_Offline`），
本工程据此派生 `SPTInstallPath = $(SptRoot)\SPT_41x`。

前置：.NET SDK（可编译 `netstandard2.1` 目标）；本机已安装 SPT 4.1.6 客户端
（含 `BepInEx\core`、`BepInEx\plugins\spt`、反混淆后的 `Managed`），且 BigBrain 1.5.0
覆盖层 DLL 可定位（默认取 Norvinsk_Forever 覆盖层内 DLL）。

```powershell
# 默认路径
dotnet build -c Release

# 覆盖 SPT 安装根（任一）
dotnet build -c Release -p:SptRoot="D:\SPT"
dotnet build -c Release -p:SPTInstallPath="D:\SPT\SPT_41x"

# 覆盖 BigBrain 编译引用（缺失时构建响亮失败）
dotnet build -c Release -p:BigBrainPath="D:\...\DrakiaXYZ-BigBrain.dll"

# 关闭构建后复制到 release/
dotnet build -c Release -p:SkipCopyToRelease=true
```

产物（扁平输出，无 TFM 子目录）：`bin\Release\SamMeow.DebugToolkit.dll`（另复制到 `release\`）。

标准检查（仓库根执行）：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\check-mod-standard.ps1 `
  -ModPath mods\SPT41-DebugToolkit -TargetSptVersion 4.1.6 -Kind client
```

---

## 安装（MO2 覆盖层）

部署为 MO2 覆盖层，**不向游戏安装目录直接写文件**（VFS 投影）。覆盖层名为
`[13]MOD工具-综合调试工具包-SamMeow.DebugToolkit`，内为游戏根相对布局：

```
[13]MOD工具-综合调试工具包-SamMeow.DebugToolkit\
├── BepInEx\plugins\SamMeow.DebugToolkit.dll
├── README.md
├── LICENSE
└── THIRD-PARTY-NOTICES.md
```

步骤：

1. 确认游戏与 MO2 未运行（`Get-Process EscapeFromTarkov` 应为空）。
2. 把 `bin\Release\SamMeow.DebugToolkit.dll` 复制进覆盖层的 `BepInEx\plugins\`；
   或直接使用 `scripts\package.ps1` 产出的 zip，解压到 MO2 `mods\` 目录。
3. 在 profile 的 `modlist.txt` 中启用 `+[13]MOD工具-综合调试工具包-SamMeow.DebugToolkit`。
4. **禁用** Norvinsk_Forever 内被替代的两个源 mod（目录保留，用于一键回滚）：
   - `[13]MOD工具-AI刷新和行为检修-BotDebug`
   - `[13]战局外检修标签-DebugTooltip`
5. **保持启用** `[6]AI拟人MOD的前置框架-BigBrain`（本插件的硬依赖）。
6. 经 MO2 启动，检查 `BepInEx\LogOutput.log` 出现插件加载行。

**卸载 / 回滚**：在 MO2 中禁用（取消勾选）本覆盖层，并**重新启用**被禁用的两个源 mod
（`[13]MOD工具-AI刷新和行为检修-BotDebug`、`[13]战局外检修标签-DebugTooltip`）即可一键回滚。
如需彻底移除，删除覆盖层目录与 `modlist.txt` 中对应行。插件卸载路径会撤销全部补丁、退订配置
事件、销毁战局组件，不残留。

---

## 互斥警示

> [!CAUTION]
> **不要与下列任一源 mod 同时安装**：`DebugTooltip`、`DrakiaXYZ-BotDebug`、`DadGamerMode`。

原因：本插件是它们的**替代整合**（非依赖），会与源 mod 产生**重复补丁**
与**热键冲突**（尤其 F9）。启用本插件前请先禁用上述三个源 mod。

> [!IMPORTANT]
> **BigBrain 1.5.0 为硬依赖**（`[BepInDependency("xyz.drakia.bigbrain", "1.5.0")]`）：
> 缺失时 BepInEx **不会加载本插件**（含 BotOverlay 的 BigBrain 两模式在内的全部功能不可用）。
> `[6]AI拟人MOD的前置框架-BigBrain` 覆盖层必须保持启用。

---

## 上游偏差表

移植以「行为等价优先」为原则；无法等价或有意偏离处记录如下。合并自 03/04/05/06 模块工单
Comments 与双轴代码审查记录。

### 必列裁决项

| # | 模块 | 偏差 | 说明 |
|---|------|------|------|
| ① | Invincibility | **GodMode 坠落豁免（有意偏离上游代码）** | 上游对 GodMode 下所有伤害无条件清零（含坠落），与其自身描述 "Makes You Invincible Except for Fall Damage" 及规格「除坠落外」矛盾。本移植按规格**仅在 `!IsFallDamage` 时清零**（与 5.0 线一致）；坠落仍受 Keep1Health / 百分比约束，另有 `No Falling Damage` 独立开关。 |
| ② | BotOverlay | **Chest 行 Broken 取 `HealthBodyBroken`（修复上游错取）** | Health 模式 Chest 行的骨折标记取胸部自身字段；上游 1.8.0 错取 Head 部位字段。与 5.0 线同一修正（用户故事「调试伤害结果」口径）。 |
| ③ | Tooltip | **`ctrlDown` 笔误修正** | 上游写成 `LeftControl || LeftControl`，修正为 `LeftControl || RightControl`（与 5.0 线一致）。 |

> **逐项降级清单**：无。本次移植全部补丁点均定位成功（Tooltip 62 触点 [OK]56/[CHANGED]5/[UNCERTAIN]1/[MISSING]0；BotOverlay 50 触点全 [OK]；DadGamer 37 触点全部定位），无功能因不可达而禁用。

### Tooltip 模块

| 偏差 | 说明 |
|------|------|
| 不移植 `R.cs`，改 `AddDisposable` 直调 | 上游经 `R.cs` 反射包装层做 UI 上下文注入；本移植直接调用托管 `AddDisposable` 等价物 |
| `_slot` 用 `FieldRefAccess` 实时读 | 私有字段改用 `AccessTools.FieldRefAccess` 在悬停回调时实时读取 + 空判（规避陈旧缓存） |
| `TraderCard._trader` 参数注入 | 商人卡片私有字段经补丁参数注入读取 |
| `ReferenceEquals` 取代 `.Pointer` | IL2CPP 时代的原生指针比较改为托管引用 `ReferenceEquals` |
| `QuestListItem` 2 参未改名 | 0.16.9 仍为 `EFT.UI.QuestListItem`（**未改名**；`QuestListItemView` 改名只发生在 5.0 线），`Init` 为 2 参 `(Quest, Action<QuestListItem>)` |
| `ItemDebugInfo` 空安全 | 物品为空 / 字段缺失时安全降级，不抛异常 |

### BotOverlay 模块

| 偏差 | 说明 |
|------|------|
| `ActiveMode` 字符串承载 + `AcceptableValueList` | BepInEx 5 的 `AcceptableValueList<T>` 有 `where T : IEquatable<T>` 泛型约束，裸枚举不满足；故以 `ConfigEntry<string>` 承载 + `Enum.TryParse` 校验（无效值回退 `Behaviour`） |
| `GetInfoText` 保持 3 参 | 保持上游 `GetInfoText(DebugBotStruct, Player, BotInfoMode)` 签名 |
| `DebugBotStruct` 值语义 | 0.16.9 中 `DebugBotStruct` 及其内层 `BotData` / `HeathsData` 为值类型，按值承接（上游同形） |
| BigBrain 两模式 `#if !STANDALONE` | `BigBrainLayer` / `BigBrainLogic` 编入受 `#if !STANDALONE` 保护；本线**默认编入**（无 `STANDALONE` 定义，与上游一致） |

### Invincibility / QoL 模块

| 偏差 | 说明 |
|------|------|
| CODMode 走原生事件 | 效果清理用原生 `ActiveHealthController.EffectAddedEvent`（0.16.9 触点为 `Action<IHealthEffect>`）+ `RemoveEffectFromList`；较 5.0 线的降级路径更贴近上游 |
| 无坠落写 `FallSafeHeight` 属性 | 后备字段 `_fallSafeHeight` 在 0.16.9 为 private，故写公有属性 `FallSafeHeight`（开启写满值，关闭一次性还原；稳态不覆盖他方设定） |
| 瞬产用 EndTime 稳健点 | 排除比特币 / 燃料配方（上游 scheme id）：置 `_producingProcess.EndTime` 为当前，放行原方法后 `Produce()` 重算 `Progress` 立即达 1.0 |
| 瞬建双路径 | 0.16.9 拆分为 `AreaData.InitConstructing(int, bool)` + `AreaData.WaitUpgradeTime(float, CancellationToken)` 两补丁点，覆盖两条路径 |
| 负重 100 零改写 | `Item Total Weight %` 为 100（正常）时直接放行、不改写装备总重（零副作用） |
| 换弹还原范围收窄 | 模块 `Disable` 时只还原全局默认，**不复位配置值**（编排者认可：避免退出游戏抹用户设置）；用户手动关闭开关时才复位配置值（上游语义） |
| `currentHealth.Current = 3f` 原样保留 | 该写为 `HealthValue` 值拷贝的**无效写**，行为等价；5.0 线省略该行——代码形态差异，行为一致 |
| `AtMinimum` 常驻短路 | 已黑部位受击的短路独立于 `Keep 1 Health` 开关常驻生效——上游 + 5.0 线两处一致（见行为注记） |

---

## 行为注记（Level C / 使用者须知）

以下为编译级已验证、但需游戏内实测确认或属使用者须知的行为语义：

- **`AtMinimum` 常驻短路**：已黑部位（HP 到最低值）受击时的短路逻辑**独立于 `Keep 1 Health` 开关**，
  在非 CODMode 下也常驻生效。此为上游既有语义，上游 + 5.0 线两处一致。使用者须知：全部无敌开关
  关闭时，已黑部位仍可能被短路（表现为该部位不再承伤）。
- **瞬建边界**：前缀跳过整条升级链，零时长 / `AutoUpgrade` 区域可能停在 `ReadyToInstall` 需手动安装
  （普通区域结果态等价）；另 `InitConstructing` 的 `alreadyUnderConstructing=true`「恢复构造中」分支被
  直接完成。待 S3（游戏内）定点确认。
- **负重须战局前设置**：`Item Total Weight %` 在战局载入时读取生效，战局中途修改不回溯。
- **换弹 / 卸弹速度关闭时恢复默认**：开关关闭时全局换弹 / 卸弹速度恢复默认（`0.85` / `0.3`）。

---

## 生命周期与日志

- **单一日志源**：全插件共享 `Plugin.Logger`（`ManualLogSource`），日志前缀统一为
  `[SamMeow.DebugToolkit]` / `[Settings]` / `[Tooltip]` / `[BotOverlay]` / `[Invincibility]` /
  `[QoL]` / `[VersionGate]`。
- **加载顺序**（`Plugin.Awake`）：版本门禁（仅警告）→ 配置面初始化 → Tooltip →
  BotOverlay → Invincibility → QoL。
- **卸载顺序**（`Plugin.OnDestroy`）：QoL → Invincibility → BotOverlay → Tooltip；
  逐模块经共享 `PatchSet` 撤销全部补丁、退订配置事件、销毁注入组件，无残留。
- 版本门禁 fail-open：读 `EscapeFromTarkov.exe` 的 `FileVersionInfo.FilePrivatePart` 比对 40743，
  不匹配仅记警告；门禁自身出错也仅记日志，绝不阻止加载。

---

## 打包（scripts/package.ps1）

产出可发布 zip（游戏根相对布局），版本号从 csproj 读取：

```powershell
powershell -ExecutionPolicy Bypass -File scripts\package.ps1
```

- 默认先执行 `dotnet build -c Release`；加 `-SkipBuild` 可跳过（要求
  `bin\Release\SamMeow.DebugToolkit.dll` 已存在）。
- 输出：`dist\SamMeow.DebugToolkit-<version>.zip`，内容为
  `BepInEx\plugins\SamMeow.DebugToolkit.dll` + `README.md` + `LICENSE` +
  `THIRD-PARTY-NOTICES.md`。
- 任一步失败以非零码退出。

---

## 版本号一致性（维护者）

升级版本时需同步以下三处，保持一致：

1. `SPT41DebugToolkit.csproj` 的 `<Version>`；
2. `src\Plugin.cs` 的 `Plugin.Version`；
3. 本 README 与安装覆盖层名 `工具-SamMeow.DebugToolkit-4.1-<版本>`。

---

## 许可证与署名

本工程为 **MIT**，见 [LICENSE](LICENSE)。版权归 `Copyright (c) 2026 SamMeow`。

整合自三个 MIT 上游项目，完整许可证全文、版权行与来源见
[THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)：

| 上游 | 版权 | 仓库 |
|------|------|------|
| DrakiaXYZ-BotDebug | © 2025-2026 DrakiaXYZ | https://github.com/DrakiaXYZ/SPT-BotDebug |
| DebugTooltip | © 2024 Tyfon | https://github.com/tyfon7/DebugTooltip |
| DadGamerMode | 上游 LICENSE 为占位模板（未填写），按 Props / dvize 署名 | https://github.com/dvize/DadGamerMode |

BigBrain 1.5.0（`xyz.drakia.bigbrain`）为运行期硬依赖，非本插件代码，本工程不分发其二进制。

---

*Vault-Tec 不对任何因调试工具导致的存档污染、实验室事故或存在性恐慌负责。*
