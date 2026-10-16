---
version: [4.1]
domain: client
topic: migration
title: "SPT 4.1.6 客户端三源整合移植实战：SamMeow.DebugToolkit（触点先行 / 模块并行 / 独立双轴评审）"
keywords: [DebugToolkit, SPT4.1.6, Mono, BepInEx5, Harmony, ModulePatch, 触点核验, 模块降级, PatchSet, fail-open, 版本门禁, ConfigDescriptions, ADR-0010, ADR-0011, BigBrain, ilspycmd, 反混淆, 双轴评审, 工单依赖链, MO2覆盖层]
summary: "SPT 4.1.6（Mono / BepInEx 5 / netstandard2.1）客户端三源整合移植 SamMeow.DebugToolkit 项目级复盘：8 工单依赖链（01 脚手架曳光弹 → 02 触点核实闸门 → 03/04/05 并行模块移植 → 06 串行 → 07 集成文档部署 → 08 终验）；触点先行策略（ilspycmd 反编译 0.16.9 反混淆 Managed 与 BigBrain DLL，产出三份核实表，状态分 [OK]/[CHANGED]/[MISSING]/[UNCERTAIN]，主控台独立复跑抽查 7/6/12 条）；全部补丁体 fail-open；版本门禁读 FilePrivatePart 比对 40743 仅警告；F12 六节配置（键名英文 + 显示名中文、原生下拉/滑块、枚举走 ConfigEntry<string>+AcceptableValueList<string>、无配置迁移器）；模块降级登记（补丁点不可定位 → 单项禁用 + 日志警告 + README 偏差表）；身份版本线 ADR-0010（与 5.0 线同 AssemblyName/GUID、独立源码独立演进，否决从 5.0 回迁 Mono 与独立新身份）与 BigBrain 硬依赖 ADR-0011（[BepInDependency] 硬门 + 编译引用参数化 + STANDALONE 逃生符号）；部署形态 MO2 覆盖层替代三源（目录保留一键回滚）且 BigBrain 保持启用；独立双轴评审（Standards ora-1 / Spec ora-2，与实现会话隔离）抓到补丁生命周期不可逆、偏差记录制度未落实、ConfigDescription 五处重复与命名歧义等，经共享 PatchSet.cs 与 ConfigDescriptions.cs 修订闭环；三族功能在 4.1.6 几乎逐行可移植（零 MISSING），差异集中在可见性 [CHANGED]、类型改名/值语义、混淆名映射与运行时字符串空判。"
---

# SPT 4.1.6 客户端三源整合移植实战：SamMeow.DebugToolkit（触点先行 / 模块并行 / 独立双轴评审）

> 来源：2026-10-11 会话（spec → 8 工单 → 实现 → 双轴评审 → 修订）；目标环境 SPT 4.1.6 / EFT 0.16.9.40743 / Mono / BepInEx 5 / netstandard2.1；部署目标 MO2 实例 Norvinsk_Forever。
> 工程：`mods/SPT41-DebugToolkit`；工单与评审：`.scratch/spt41-debugtoolkit/`。三源 = DebugTooltip 5.0.0 / BotDebug 1.8.0 / DadGamerMode 1.9.3（本地 Forge 归档，均 MIT）。

## 一、工程范式（可复用）

### P1 工单依赖链：曳光弹先行 + 触点闸门
- 8 单切法：**01 脚手架曳光弹**（先证明构建接缝可通，无部署）→ **02 触点核实**（关键闸门，先于一切实现）→ 03/04/05/06 模块移植 → **07 集成/文档/部署** → **08 终验**。
- 依赖声明写死在工单：02 `Blocked by 01`；03/04/05 `Blocked by 02`；07 `Blocked by 03-06`；08 `Blocked by 07`。
- 曳光弹价值：01 一次跑通 `dotnet build -c Release`、标准检查、SptRoot 参数三态、6 节配置骨架与 4 模块注册点约定，把「构建接缝」风险在动任何补丁前清掉。

### P2 并行策略：按文件域切 lane，共用文件强制串行
- 03/04/05 **可并行**（模块文件域隔离：`src/Modules/Tooltip|BotOverlay|Invincibility`；共享骨架改动最小化，留给 07 收口复核）。
- **05 → 06 必须串行**：两者共用 `ApplyDamagePatch.cs`（06 在其上插对敌伤害倍率），改动面重叠不可并发。
- 教训：并行 lane 的边界是「文件域」而非「功能域」；跨 lane 共享文件是并行的唯一硬堵点。

### P3 触点闸门的内容与强度
- 用 `ilspycmd` 对 `Managed` 反混淆程序集 + BigBrain DLL 逐点反编译，把三源全部补丁目标与反射触点对照到 0.16.9，产出三份核实表（产物为文档，不含实现代码）。
- 每触点四要素：目标签名（或「缺失」）、可达性结论、适配方式或降级预案、一行可直接指导实现的指引。
- 状态分层：`[OK]` / `[CHANGED]` / `[MISSING]` / `[UNCERTAIN]`——把「无法定位」显式建模，而不是留白。
- 主控台独立复跑抽验（与子通道产出交叉）：tooltip V1–V7、bot-overlay V1–V6、dadgamer §4（12 条，ILSpy + 反射双方法），各通道 7/6/12 条交叉命中。

### P4 评审分工：独立双轴、与实现会话隔离
- `ora-1`（Standards 轴）/ `ora-2`（Spec 轴）为**两个独立 oracle 会话**，均在实现会话之外；S1 接缝（构建 + 标准检查）由评审员**独立复跑**取证，不复用实现者结论。
- 无 git 提交场景下（用户裁决），固定点 = `mods/SPT41-DebugToolkit/**` 全树；评审对 greenfield 变更集逐文件读。

### P5 省代码可复用的拆分原则（跨线泛化）
- **行为基准**取 4.x 上游源件（BotDebug 1.8.0 / DebugTooltip 5.0.0 / DadGamerMode 1.9.3），**整合设计**（配置面 / 生命周期 / 模块划分 / README 结构 / fail-open 原则）自 5.0 线平移——两者来源分离，避免把 5.0 的 IL2CPP 形态误当行为基准。
- 上游源码全部取自本地 Forge 归档，provenance 与署名随工程携带（`LICENSE` + `THIRD-PARTY-NOTICES`），合规分发。

## 二、关键决策与理由

### D1 fail-open（补丁体永不穿透游戏调用链）
- 所有补丁体 `try/catch`，异常在插件侧吞掉并记日志，不向游戏调用链外抛——调试工具以「不崩溃」为第一优先。
- 版本门禁自身出错同样 fail-open，绝不 throw。

### D2 版本门禁：读 FilePrivatePart，不匹配仅警告
- 读 `EscapeFromTarkov.exe` 的 `FileVersionInfo.FilePrivatePart`，比对 `40743`；不匹配只 `LogWarning`，**不拒绝加载**——调试工具在版本漂移时必须永不缺席。
- 门禁失败路径也走 fail-open，不因读不到版本而中断加载。

### D3 F12 配置面（6 节，原生控件）
- 键名英文 + 显示名中文；列表型用原生下拉（`AcceptableValueList`）、数值项用范围滑块（4.1.6 Mono 无 IL2CPP 控件限制）。
- 枚举配置不能用 BepInEx 5 的 `AcceptableValueList<T>` 直接接裸枚举（泛型约束不成立），改 `ConfigEntry<string>` + `AcceptableValueList<string>`。
- 无配置迁移器（新线无历史 cfg）；换弹还原范围裁定为「Disable 不复位配置值」（避免退出游戏抹掉用户设置）。

### D4 模块降级登记（单项缺位不阻塞整体）
- 补丁点无法定位或无法等价映射 → 该功能**单项禁用 + 日志警告 + README 偏差表登记**，交付不因此中断。
- 本轮三源在 0.16.9 上零 `[MISSING]`，**未触发任何降级**；机制为纯预留。

### D5 身份版本线（ADR-0010）
- 4.1.6 线保留 5.0 线的 AssemblyName `SamMeow.DebugToolkit` 与 GUID `com.sammeow.debugtoolkit`——**同身份、独立源码、独立演进**。
- 否决两项：从 5.0 代码回迁 Mono（需反向拆除 IL2CPP 适配 + 把 1.1.5 特化改动重映射回 0.16.9，双重改写风险最高）；独立新身份（GUID/程序集名断裂，cfg 键面、日志前缀、README 不再互认）。
- 后果：两线功能面允许有记录在案差异（4.1.6 线含 BigBrain 两模式与 DadGamer QOL 全组，5.0 线无），须各线 README 偏差表登记；上游源件升级需人工对账。

### D6 BigBrain 硬依赖（ADR-0011）
- 两模式（BigBrainLayer / BigBrainLogic）编入，声明 `[BepInDependency("xyz.drakia.bigbrain", "1.5.0")]` 硬门（镜像上游 BotDebug 默认构建）；编译引用取 Norvinsk_Forever 覆盖层内 DLL，路径参数化、缺失响亮失败。
- `STANDALONE` 条件符号保留为逃生门（无 BigBrain 场景可另行构建，本任务不产出该变体）。
- 后果：未装 BigBrain 时本插件整体不加载（与上游行为一致，目标整合包已内置）。

### D7 部署形态：MO2 覆盖层替代三源
- 单覆盖层（游戏根相对布局 DLL + README + LICENSE + THIRD-PARTY-NOTICES），**禁用** `[13]…BotDebug` 与 `[13]…DebugTooltip`（目录保留，一键回滚），`[6]…BigBrain` 保持启用；写入前确保游戏与 MO2 关闭。
- 命名整改：覆盖层由 `工具-…-版本` 形改为沿用目标包命名结构 **`[13]MOD工具-综合调试工具包-SamMeow.DebugToolkit`**，并同步整改命名指引落点。

### D8 有意偏离上游（须登记）
- **GodMode**：改为「除坠落伤害外全伤害清零」（上游代码为无条件全清零，与其自身描述矛盾；与 5.0 线同一偏离）。
- **BotOverlay Chest 行 Broken**：取 `HealthBodyBroken`（上游 1.8.0 错取 Head 的 bug；与 5.0 线同一修正，符合「调试伤害结果」的用户故事口径）。
- 其余（ctrl 键笔误修正、瞬建双路径、`Task.CompletedTask` 短路）随单登记进 README 偏差表。

### D9 不可照抄 5.0 形态的纠偏清单
- `QuestListItem` 在 4.1.6 仍为 2 参（5.0 的 `QuestListItemView` 不适用）；`UIContext` 类型不存在（`UI` 字段真类型为 `UIParent`）；`BackendConfigSettingsClass` 整体移除（→ `EFT.GlobalConfiguration`）；`DamageInfo` 为 struct。照搬 5.0 名会出现编译失败或语义错位。

### D10 工程坐标与打包
- 工程 `mods/SPT41-DebugToolkit`；netstandard2.1、扁平输出、`CopyToRelease` 到 `release/`（`-p:SkipCopyToRelease=true` 可关）；`SptRoot` / `SPTInstallPath` 参数化（默认 `$(SptRoot)\SPT_41x`，路径缺失响亮失败）。
- `scripts/package.ps1` 产出 zip（4 条目：`BepInEx\plugins\SamMeow.DebugToolkit.dll` + README / LICENSE / THIRD-PARTY-NOTICES），版本从 csproj 读取；版本号三处同步（csproj / `Plugin.cs` / README 与覆盖层名）。
- 实现期不产生 git 提交（提交由 Overseer 执行）。

## 三、双轴评审发现与修订闭环

### S1 Spec 轴（ora-2）
- 文档缺口：README 仍占位、偏差表未汇总、`THIRD-PARTY-NOTICES` 无三上游 MIT 全文、US42/44/47 未收口——全部移交工单 07。
- 三项实现疑似有误：① **瞬建双挂缺口**（零时长/AutoUpgrade 区域停 `ReadyToInstall`；`alreadyUnderConstructing` 分支被前缀直接完成）；② **`BotDebugComponent` Awake-only 解析**失败即整局静默失效，弱于工程基线的惰性重试；③ **`TooltipCopier` 停用残留**（ctrl-c 停用后仍写旧文本）。
- 处置：① 不改码（普通区域逐行等价，边界行为入 S3 清单 + 07 行为注记）；②③ 修订轮修复。

### S2 Standards 轴（ora-1）
- 硬违规：**「补丁全撤销 + 逐项降级」未达标**——模块以单一 `try` 顺序装配、`Disable` 以 `_enabled` 早退，补丁点缺失时已装配补丁不可逆；**偏差记录制度未落实**（waiver 理由与现状不符）；根 `.gitignore` 不覆盖子目录 `dist`。
- 判断项：`ConfigDescription` 构造五处同形；四模块补丁管理模板重复且**已分叉**（列表式 vs 字段式）；`BotData` 与 EFT `DebugBotStruct.BotData` 同名异义；死代码（`SetDebugInfo(string)` + `BasicDebugInfo` 零调用）。

### S3 修订闭环（独立 fixer + 编排者新鲜复验）
- 新增共享 **`PatchSet.cs`**：`Enable()` 逐项 `try/catch` + 成功记账（含去重）、`DisableAll()` 逐项撤销 + 清空、幂等、全 fail-open；四模块全部迁移，`Disable` 无条件清理（无早退），配置订阅逐项退订。
- 新增共享 **`ConfigDescriptions.cs`** 工厂（5 处迁移）；`BotData` → `BotOverlayEntry` 改名；删死代码；`TooltipCopier.RemoveAll()`；`.gitignore` 补 `**/dist/`。
- 复验：`dotnet build -c Release` 0 warning / 0 error；标准检查 PASS=13 / FAIL=0 / WAIVED=1。
- **PatchSet 教训**：四模块各自实现的「补丁列表管理」是重复 + 发散式变化的典型；一旦分叉，撤销语义随之失真。共享注册器一次消除「重复」与「补丁生命周期不可逆」两个 finding——重复代码与健壮性缺陷常是同一根因。

### S4 处置分层范式（评审闭环的通用手法）
- 修复（硬性/低危缺陷）/ 移交后续工单（文档类，07 收口）/ 不改码（边界行为入 S3 清单 + README 注记）/ 明示接受（如 `EnumExtension` 微重复：接受不动作）——分层让修订轮边界清晰，避免范围蔓延。

### S5 评审独立反编译核对通过项（可复用证据）
- 评审员独立反编译确认：`EDamageType.Fall = 2`；`WaitUpgradeTime` 存在；瞬产链 `EndTime → Produce → Progress = 1.0 → ProcessDoneItems` 成立；配置键名/默认/范围与 spec 表逐项一致（含与上游范围比对）；全部补丁体 fail-open 齐备。
- 合规面抽检通过：零 `[HarmonyPatch]` 特性、无空 catch / `Console.Write`、键名英文 + 显示名中文、列表下拉 + 数值滑块、单一日志源、中文注释、版本门禁仅警告。
- 意义：双轴的「核对通过项」与「发现项」同等留档——它界定本轮已证事实的边界，后续复验可据此定位回归。

## 四、模块触点核验结果摘要

方法细节见 `curated/operations/client-api-touchpoint-verification.md`；此处只留结论。

- **tooltip（62 触点：56/5/1/0）**：`SimpleTooltip.Show` 4 参（Mono 原生 `Nullable`）；`UIContext → UIParent`；`GridItemView.ShowTooltip` / `ModSlotView.TryGetArmorSlot` 可见性提升；`Condition.id` 由字段转属性；`QuestObjectiveView.Show` 6 参（1.1.5 为 7 参）。陷阱：`ModdingScreenSlotView._slot` 私有字段被 `Start` postfix 按值捕获 → 须 `FieldRefAccess` 实时读；`HoverTrigger` 原生事件直接 `+=`（无需 IL2CPP `DelegateSupport`）；`TooltipCopier` 直接 `GetOrAddComponent`（无需 `ClassInjector`）。
- **bot-overlay（50 触点全 `[OK]`）**：上游锚定 `TarkovVersion 40743` 与本构建精确一致，零 MISSING / 零 CHANGED。`GameWorld.OnGameStarted` 存活且 virtual；`DebugBotStruct` 为 struct、`PlayerOwner` 类型 `IObserverToPlayerBridge`、`ProfileId` 属性可空；`CameraManager.Instance` 懒构造（永不为 null）但 SSAA 相机装配前可能 null，**必须空判**；多类型在全局命名空间（臆造 `EFT.` 前缀会编译失败）；组件宿主是 `GameWorld.gameObject`，清理只能 `Destroy(this)`。
- **dadgamer（37 触点：17/20/0/0）**：`ActiveHealthController.ApplyDamage(EBodyPart, float, DamageInfo)` 非 virtual、prefix `ref float` 可改写；`DamageInfo` 为 struct；`EBodyPart`/`EDamageType` 均在 Assembly-CSharp（4.1.6 未拆 `PlayerEnums.dll`，异于 5.0）；`BackendConfigSettingsClass → GlobalConfiguration`；`FallSafeHeight` 为 virtual 属性（后备字段 private）；`GetBodyPartHealth` 返回值拷贝（写无效，上游既有行为），`BodyState[limb].Health` 为引用类型（写回生效，CODMode 治疗依赖此）；瞬产稳健点 = 置 `_producingProcess.EndTime` 后放行原 `Update`；瞬建挂双补丁点 `InitConstructing` + `WaitUpgradeTime`。三条旧混淆名（瞬产/瞬建/负重）全部定位成功。
- 一句话结论：**4.1.6（Mono / BepInEx 5）三族功能无需 IL2CPP 适配即几乎逐行可移植（零 MISSING）**；差异集中在可见性 `[CHANGED]`、类型改名/值语义、混淆名映射与运行时字符串空判。

## 五、开放项与登记项

- **08 未执行**：Level B（日志断言）+ Level C（人工游戏内清单）未勾；瞬产/瞬建/能量水分等行为待游戏内确认。
- **不改码待确认**：瞬建边界语义（S3 定点验证）；`AtMinimum` 段保留常驻语义（上游与 5.0 线一致，入行为注记）。
- **已裁定保留**：换弹 `Disable` 不复位配置值（换弹还原范围）；`currentHealth.Current = 3f` 原样保留（值拷贝无效写、行为等价，与 5.0 线代码形态差异）。
- **登记项**：`STD-CLI-003` 豁免（统一 `ModulePatch`、零 `[HarmonyPatch]` 特性属有意偏离）；升级对账（上游源件升级需人工对账，ADR-0010）；`_fallSafeHeight` 私有 → 走 `FallSafeHeight` 属性。

## 相关

- `curated/operations/client-api-touchpoint-verification.md`（同批：触点核验方法）
- `curated/migration/pilot-experience-qol-consolidation.md`（5.0 线客户端整合范式）
- `curated/operations/5xx-client-mod-dev-lessons.md`（§14 为 5.0 线同身份产品的实测记录）
- `docs/adr/0010-spt41-debugtoolkit-upstream-baseline.md`、`docs/adr/0011-spt41-bigbrain-hard-dependency.md`
