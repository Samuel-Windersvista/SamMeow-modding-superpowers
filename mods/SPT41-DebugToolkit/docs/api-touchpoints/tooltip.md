# DebugTooltip — SPT 4.1.6 移植 API 触点核实表（工单 02 子通道 A）

> 结论四态（纯文本）：**[OK]** 同名存在 / **[CHANGED]** 改名、签名或可见性变化（给出新形态）/ **[MISSING]** 缺失（必须给降级预案）/ **[UNCERTAIN]** 无法静态定位（给最佳候选 + 理由）。
> 本表逐条核实上游 DebugTooltip 源码对 EFT / BepInEx 5 / Unity（Mono）API 的每一个调用点，对照目标程序集 EFT 0.16.9.40743（已反混淆真名）。
> 与 5.0 线 `SamMeow-DebugToolkit/docs/api-touchpoints/tooltip.md` 的差异：本表为 **Mono / BepInEx 5 / netstandard2.1**，无 IL2CPP 代理约束——4.1.6 可直接托管调用、原生事件订阅、virtual 属性直取、自定义 MonoBehaviour 直接 `AddComponent`。

---

## 0. 头部

### 0.1 来源 provenance

| 项 | 值 |
|---|---|
| 上游仓库（本仓归档） | `knowledge/spt-kb/archive/forge/mods/DebugTooltip_1542_source/` |
| 上游版本 | `5.0.0`（`DebugTooltip.csproj` `<Version>`；版本号仅为上游自身编号，**工程锚定 `SPT\4.1`**，见 csproj `<SPTPath>`） |
| License | MIT，Copyright (c) 2024 Tyfon |
| 上游命名空间 / 程序集名 | `DebugTooltip`（根）/ `Tyfon.DebugTooltip` |
| 目标框架 | `netstandard2.1`（4.1.6 = Mono / BepInEx 5 / netstandard2.1，与上游一致） |
| 上游混淆锚点 | csproj `<SPTPath>..\..\..\..\SPT\4.1</SPTPath>`；引用 `BepInEx.Core 5.*` + `spt-common` / `spt-reflection` |

读取的源文件（全部）：`Plugin.cs`、`DebugTooltip.cs`、`DebugInfo.cs`、`Settings.cs`、`TooltipCopier.cs`、`R.cs`、`ConfigurationManagerAttributes.cs`、`Patches\TooltipPatch.cs`、`Patches\ItemPatches.cs`、`Patches\QuestPatches.cs`、`Patches\TraderPatches.cs`、`Patches\ShortcutPatch.cs`、`DebugTooltip.csproj`。

### 0.2 目标环境

| 项 | 值 |
|---|---|
| SPT 安装 | `E:\Game\EFT_Offline\SPT_41x\`（SPT 4.1.6 / EFT 0.16.9.40743） |
| 运行时 | Mono（存在 `EscapeFromTarkov_Data\Managed\`，无 `GameAssembly.dll`） |
| EFT 程序集 | `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`（已反混淆真名；勿用同目录 `.spt-bak`） |
| SPT 客户端库 | `BepInEx\plugins\spt\`（`spt-common.dll` / `spt-reflection.dll` 等） |
| BepInEx | `BepInEx\core\BepInEx.dll`（BepInEx 5），`0Harmony.dll` |
| Unity 模块 | `UnityEngine.CoreModule.dll` / `UnityEngine.IMGUIModule.dll` / `UnityEngine.InputLegacyModule.dll` / `UnityEngine.UI.dll` / `Unity.TextMeshPro.dll` |

### 0.3 工具链与使用命令

```powershell
# 工具
ilspycmd --version   # 11.0.0.9375

# 反编译单个类型
ilspycmd -t "<Full.Type.Name>" "<dll>"
# 反编译单个成员（用于二次独立核实；doc-id）
ilspycmd -m "M:<Full.Type>.<Method>(<ParamTypes>)" "<dll>"

# 常量（本表所用）
$CSHARP = "E:\Game\EFT_Offline\SPT_41x\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll"
$IMGUI  = "E:\Game\EFT_Offline\SPT_41x\EscapeFromTarkov_Data\Managed\UnityEngine.IMGUIModule.dll"
$INPUT  = "E:\Game\EFT_Offline\SPT_41x\EscapeFromTarkov_Data\Managed\UnityEngine.InputLegacyModule.dll"
$REFL   = "E:\Game\EFT_Offline\SPT_41x\BepInEx\plugins\spt\spt-reflection.dll"
$BEP    = "E:\Game\EFT_Offline\SPT_41x\BepInEx\core\BepInEx.dll"
```

---

## 1. 主表（触点核实）

### 1.1 Plugin.cs 与工程层

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 1 | `Plugin.cs:6` | `BepInEx.BaseUnityPlugin`（类基） | 存在；`public abstract class BaseUnityPlugin : MonoBehaviour` | 无需改 | 4.1.6 用 BepInEx 5 `BaseUnityPlugin`，`Awake()` 入口照旧 | [OK] |
| 2 | `Plugin.cs:5` | `[BepInPlugin(guid,name,ver)]` | 存在；`AttributeUsage(Class)` | 无需改 | 特性兼容 | [OK] |
| 3 | `Plugin.cs:11` | `Config`（`ConfigFile`） | 存在；`BaseUnityPlugin.Config : ConfigFile`（public get） | 无需改 | `Settings.Init(Config)` 原样 | [OK] |
| 4 | `DebugTooltip.csproj:4` | `netstandard2.1` | 4.1.6 目标即 netstandard2.1 | 无需改 | 引用清单改为 MO2 实例 `SptRoot` 的 HintPath | [OK] |
| 5 | 五个补丁文件 | `SPT.Reflection.Patching.ModulePatch` | 存在；`protected abstract MethodBase GetTargetMethod()` + `public void Enable()`；`namespace SPT.Reflection.Patching`（`spt-reflection.dll`） | 无需改 | 目标 SPT 4.1.6 的 `spt-reflection.dll` 直接可用 | [OK] |

### 1.2 Patches\TooltipPatch.cs

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 6 | `TooltipPatch.cs:26` | `EFT.UI.SimpleTooltip.Show` | 存在；`public CancellationToken Show(string text, Vector2? offset = null, float delay = 0f, float? maxWidth = null)`（**4 参**，Mono `Vector2?`/`float?`，非 `Il2CppSystem.Nullable`） | 无需降级；`AccessTools.DeclaredMethod(typeof(SimpleTooltip), nameof(Show))` 在本类仅一个 `Show` 声明，唯一命中 | prefix `(SimpleTooltip __instance, ref string text, ref float delay)` 按名绑定；`delay = 0f` 立即显示 | [OK] |
| 7 | `TooltipPatch.cs:32` | `EFT.UI.ItemUiContext.Instance` | 存在；`public static ItemUiContext Instance { get; set; }`（**public setter**） | 无需改 | `.Instance` 直取；读前空判 | [OK] |
| 8 | `TooltipPatch.cs:32` | `ItemUiContext.Tooltip` | 存在；`public SimpleTooltip Tooltip => _simpleTooltip;`（属性） | 无需改 | `.Tooltip.Show(string.Empty)` / `.Close()` 可用 | [OK] |
| 9 | `TooltipPatch.cs:99` | `EFT.UI.PriceTooltip.Show` | 存在；`public void Show(EOwnerType ownerType, string text, int price, MongoID? currencyTemplate)`；`PriceTooltip : SimpleTooltip`（**非 sealed**） | 无需降级；prefix 只用 `__instance` | 目标方法唯一，`DeclaredMethod` 命中 | [OK] |
| 10 | `TooltipPatch.cs:125` | `PriceTooltip._price` | 存在；`public TextMeshProUGUI _price`（public 字段） | 无需改 | 与 5.0 线同形，可直接读 | [OK] |
| 11 | `TooltipPatch.cs:125` | `UnityEngine.Object.Instantiate<T>` | 存在（`UnityEngine.CoreModule.dll`） | 无需改 | `Instantiate(__instance._price, __instance._price.transform.parent.parent, false)` | [OK] |
| 12 | `TooltipPatch.cs:45,118` | `GetOrAddComponent<T>()` | 存在；`GameObjectExtensions`（**全局命名空间**，`Assembly-CSharp.dll`）提供 `GetOrAddComponent<T>(this GameObject)` 与 `<T>(this MonoBehaviour)` | 无需改（Mono 无 IL2CPP 注入问题） | 无需 `using`；`GetOrAddComponent<TooltipCopier>()` 直接可用 | [OK] |
| 13 | `TooltipPatch.cs:1` | `TMPro.TextMeshProUGUI` | 存在；`Unity.TextMeshPro.dll`，`TextMeshProUGUI : TMP_Text` | 无需改 | 引用 `Unity.TextMeshPro.dll` | [OK] |
| 14 | `TooltipPatch.cs:105` | `ItemUiContext.PriceTooltip` | 存在；`public PriceTooltip PriceTooltip => _priceTooltip;` | 无需改 | 身份比较 `__instance != ItemUiContext.Instance.PriceTooltip` 有效 | [OK] |

### 1.3 Patches\ItemPatches.cs

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 15 | `ItemPatches.cs:29` | `EFT.UI.DragAndDrop.GridItemView.ShowTooltip` | 存在；`public virtual void ShowTooltip()`（**可见性 protected → public**，1.1.5 为 protected） | 无功能影响；`AccessTools.Method` 命中唯一 `ShowTooltip` 声明（`ShowTooltipArmorPlate` 名字不同） | prefix `(GridItemView __instance)` 注入 `__instance.Item` | [CHANGED] |
| 16 | `ItemPatches.cs:48` | `TradingItemView.ShowTooltip` | 存在；`public override void ShowTooltip()`；`TradingItemView : GridItemView` | 无需改 | `AccessTools.Method` 命中 override 声明 | [OK] |
| 17 | `ItemPatches.cs:67` | `EFT.UI.DragAndDrop.SlotView.Show` | 存在；`public virtual void Show(Slot slot, ItemContext parentItemContext, ItemController itemController, ItemUiContext itemUiContext, SkillManager skills, InsuranceCompany insurance, bool canClickOnHeader = false)` | 无需降级；`SlotView` 仅声明一个 `Show`，`AccessTools.Method` 命中（`ModSlotView.Show` 为不同签名重载，不被误命中） | postfix 只用 `__instance`；`ModSlotView` 经 `base.Show` 亦被覆盖 | [OK] |
| 18 | `ItemPatches.cs:73,165` | `EFT.UI.HoverTrigger` | 存在；`HoverTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler`；`OnHoverStart` / `OnHoverEnd` 为 **event `Action<PointerEventData>`**（Mono 原生委托） | 无需 IL2CPP 的 `DelegateSupport.ConvertDelegate`；`hoverTrigger.OnHoverStart += handler;` 直接编译 | `GetOrAddComponent<HoverTrigger>()` 后直接订阅；`AddDisposable` 用法见 #20 | [OK] |
| 19 | `ItemPatches.cs:77,112,126` | `SlotView.Slot` / `Slot.ContainedItem` | `public Slot Slot { get; set; }`；`Slot.ContainedItem : public virtual Item`；`Slot.ParentItem : public virtual Item` | Mono 下 virtual 属性直取安全（IL2CPP 的 AV 风险在此不存在） | `__instance.Slot.ContainedItem == null` 判定空槽 | [OK] |
| 20 | `ItemPatches.cs:85,181`、`QuestPatches.cs:41,75,109` | `UIElement."UI".AddDisposable(Action)` | `UIElement` 字段 `protected readonly UIParent UI`（**类型为 `UIParent`，非 `UIContext`**）；`UIElement.AddDisposable(Action)` 为 public 方法；`UIParent : CompositeDisposable`，`CompositeDisposable.AddDisposable(Action)` 存在 | `R.UIContext` 包装名不存在（见 1.7）；**推荐降级**：删除 `new R.UIElement(__instance).UI.AddDisposable(...)`，直接 `__instance.AddDisposable(() => ...)`（`__instance` 均为 `UIElement` 派生） | 直接 `__instance.AddDisposable(...)`；保留包装时 `FieldType` 解析为 `UIParent` 亦可 | [CHANGED] |
| 21 | `ItemPatches.cs:101` | `ModSlotView.OnPointerEnter` | 存在；`public void OnPointerEnter(PointerEventData eventData)`（`ModSlotView : SlotView`，非 virtual，`DeclaredMethod` 命中） | 无需改 | prefix/postfix `(ModSlotView __instance)`；槽位空判用 `__instance.Slot.ContainedItem` | [OK] |
| 22 | `ItemPatches.cs:137` | `ModSlotView.TryGetArmorSlot` | 存在；`public bool TryGetArmorSlot(out ArmorSlot outArmorSlot, out ArmorPlate outArmorPlate)`（**可见性 private → public**） | 无需降级；postfix `ref bool __result` 绑定返回值，不受 out 影响 | `if (__result) SetDebugInfo(new ItemDebugInfo(__instance.Slot.ContainedItem))` | [CHANGED] |
| 23 | `ItemPatches.cs:159` | `ModdingScreenSlotView.Start` | 存在；`public void Start()`；`ModdingScreenSlotView : UIElement` | **注意**：0.16.9 的 `Start()` 自身已 `_tooltipHoverArea.gameObject.GetOrAddComponent<HoverTrigger>()` 并订阅游戏自己的 handler；上游 postfix 的 `GetComponent<HoverTrigger>()` 因此必命中（不再新建） | postfix 内 `GetComponent<HoverTrigger>()` 后追加自身 handler，行为仍正确 | [OK] |
| 24 | `ItemPatches.cs:163` | `ModdingScreenSlotView._slot`（私有字段） | 存在；`private Slot _slot` | Harmony 注入参数 `Slot ____slot` 有效（`___` + `_slot`）；**但** 上游在 `Start` postfix 处捕获 `____slot`，若 `Start` 先于 `Show`（`_slot` 尚为 null）则闭包内引用恒为 null | 用 `AccessTools.FieldRefAccess<ModdingScreenSlotView, Slot>("_slot")` 在 hover 回调时实时读取，或在回调内空判 + 跳过 | [OK] |
| 25 | `ItemPatches.cs:165` | `ModdingScreenSlotView._tooltipHoverArea` | 存在；`public RectTransform _tooltipHoverArea`（public 字段） | 无需改 | `.GetComponent<HoverTrigger>()` 可用 | [OK] |
| 26 | `ItemPatches.cs:32,93,187` 等 | `UnityEngine.EventSystems.PointerEventData` | 存在；`UnityEngine.UI.dll` | 无需改 | 引用 `UnityEngine.UI.dll` | [OK] |

### 1.4 Patches\QuestPatches.cs

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 27 | `QuestPatches.cs:23` | `EFT.UI.QuestListItem.Init(Quest)` | **存在，未改名**；`EFT.UI.QuestListItem : UIElement`，`public void Init(Quest quest, Action<QuestListItem> onSelect)`（**2 参**） | 无需降级——0.16.9 早于 1.1.5 的 `QuestListItem → QuestListItemView` 改名（该改名**仅**发生在 SPT 5.0 线）。**4.1.6 必须用 `QuestListItem`** | postfix `(QuestListItem __instance, Quest quest)` 按名绑定 `quest`；`AccessTools.Method` 命中唯一 `Init` | [OK] |
| 28 | `QuestPatches.cs:57` | `EFT.UI.NotesTask.Show` | 存在；`public void Show(Quest quest, IEftSession session, InventoryController inventoryController, QuestController questController, NotesTaskDescriptionShort description, FavoriteQuestManager favoriteQuests, bool availability)`（7 参，参数名 `quest` 保留） | 无需降级；postfix 只绑 `(NotesTask __instance, Quest quest)` | `DeclaredMethod` 命中 unique `Show` | [OK] |
| 29 | `QuestPatches.cs:63` | `__instance.transform.Find("TaskShortInfo")` | 运行期字符串，静态不可核实 | **降级**：`.Find` 结果可能为 null → 补空判 | `var t = __instance.transform.Find("TaskShortInfo"); if (t == null) return; var hoverTrigger = t.gameObject.GetOrAddComponent<HoverTrigger>();` | [UNCERTAIN] |
| 30 | `QuestPatches.cs:91` | `EFT.UI.QuestObjectiveView.Show` | 存在；`public void Show(Quest quest, Condition condition, QuestController questController, ItemController itemController, [CanBeNull] Sprite icon, bool childCondition = false)`（6 参）；`QuestObjectiveView : ObjectiveView<QuestObjectiveView> : UIElement` | **签名较 1.1.5 少 `string prefix = ""` 尾参**（1.1.5 为 7 参）；`AccessTools.Method(type, nameof(Show))` 仍唯一命中（基类只有 `ShowObjective`） | postfix `(QuestObjectiveView __instance, Condition condition)` 按名绑定 `condition` | [OK] |

### 1.5 Patches\TraderPatches.cs

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 31 | `TraderPatches.cs:22` | `TraderCard` 显式接口方法 `"UnityEngine.EventSystems.IPointerEnterHandler.OnPointerEnter"` | 存在；`TraderCard`（**全局命名空间**）`void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)`（显式实现） | 无需改 | `AccessTools.Method(typeof(TraderCard), "UnityEngine.EventSystems.IPointerEnterHandler.OnPointerEnter")` 精确命中 | [OK] |
| 32 | `TraderPatches.cs:43` | `"UnityEngine.EventSystems.IPointerExitHandler.OnPointerExit"` | 存在；`void IPointerExitHandler.OnPointerExit(PointerEventData eventData)` | 无需改 | 同上 | [OK] |
| 33 | `TraderPatches.cs:26` | `_trader` 私有字段 + `Profile.TraderInfo.Id` | `TraderCard._trader` 为 `private Profile.TraderInfo`（postfix 参数 `____trader` 有效）；`EFT.Profile.TraderInfo : GlobalConfiguration.ITraderInfo`，`public readonly string Id` | 无需降级 | postfix `(Profile.TraderInfo ____trader)`；空判 `____trader` | [OK] |

### 1.6 Patches\ShortcutPatch.cs

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 34 | `ShortcutPatch.cs:12` | `EFT.UI.ItemUiContext.Update` | 存在；`public void Update()`（唯一） | 无需改 | postfix 内读 `Settings.ToggleShortcut.Value.IsDown()` 翻转开关 | [OK] |

### 1.7 R.cs 反射包装

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 35 | `R.cs:28` | `EFT.UI.UIElement` 字段 `"UI"` | 存在；字段实际形态 `protected readonly UIParent UI`；类型 = `EFT.UI.UIParent` | 无需改（`AccessTools.Field` 能取 protected/readonly 字段）；类型名与 1.1.5 一致为 `UIParent` | `AccessTools.Field(typeof(EFT.UI.UIElement), "UI")` 照旧 | [OK] |
| 36 | `R.cs:40` | `EFT.UI.UIInputNode` 字段 `"UI"` | 存在；`protected readonly UIParent UI`（`UIInputNode : InputNode`，非 `UIElement`） | 无需改；上游未实际用其 UI 字段做补丁 | `AccessTools.Field(typeof(EFT.UI.UIInputNode), "UI")` 照旧 | [OK] |
| 37 | `R.cs:53` | `UIContext`（= `UI` 字段类型） | **`EFT.UI.UIContext` 类型不存在**（0.16.9 与 1.1.5 相同）；`UI` 字段类型为 `EFT.UI.UIParent : CompositeDisposable` | 结论明确：`UIContext → UIParent`。`R.UIContext.Type` 得到 `UIParent`；`AddDisposable(Action)` 经 `UIParent → CompositeDisposable` 继承链可反射命中 | 包装类名可保留（仅作 holder），或删包装直接调用（见 #20） | [CHANGED] |
| 38 | `R.cs:54,57` | `CompositeDisposable.AddDisposable(Action)` | 存在；`public void AddDisposable(Action destroy)`（`UIParent` 基类） | 无需降级；`AccessTools.Method(UIParent, "AddDisposable", [typeof(Action)])` 命中 | 直接用 `__instance.AddDisposable(Action)` 最稳（`UIElement.AddDisposable` 为 public） | [OK] |

### 1.8 TooltipCopier.cs

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 39 | `TooltipCopier.cs:18,38..49` | `GUIUtility.systemCopyBuffer` | 存在；`public static extern string systemCopyBuffer { get; set; }`（**属性**，`UnityEngine.IMGUIModule.dll`） | Mono 下**可直接属性赋值**；上游 `AccessTools.Property(...).SetValue(null, text, null)` 亦可行 | `UnityEngine.GUIUtility.systemCopyBuffer = text;`（引用 `UnityEngine.IMGUIModule.dll`） | [OK] |
| 40 | `TooltipCopier.cs:30..34` | `Input.GetKeyDown` / `Input.GetKey` | 存在；`UnityEngine.InputLegacyModule.dll` | 无需改 | 引用 `UnityEngine.InputLegacyModule.dll` | [OK] |
| 41 | `TooltipCopier.cs:8` | `TooltipCopier : MonoBehaviour`（自定义组件） | Mono 运行时可直接 `AddComponent`/`GetOrAddComponent<T>` | **无需** `ClassInjector.RegisterTypeInIl2Cpp`（IL2CPP 专属），也**无需** `IntPtr` 构造函数 | `GetOrAddComponent<TooltipCopier>()` 原样；`Awake`/`Update` 正常驱动 | [OK] |
| 42 | `TooltipCopier.cs:32` | `ctrlDown = GetKey(LeftControl) \|\| GetKey(LeftControl)`（上游笔误） | 语义问题（非 API 问题） | 保留上游行为或修正为 `LeftControl \|\| RightControl`（记入 README 偏差表） | 建议与 5.0 线一致：保留行为 + 记录 | [OK] |

### 1.9 DebugInfo.cs 数据模型

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 43 | `DebugInfo.cs:24,37` | `EFT.InventoryLogic.Item.Id` | 存在；`public string Id { get; }` | 无需改 | 直接读 | [OK] |
| 44 | `DebugInfo.cs:26,38` | `Item.TemplateId` | 存在；`public MongoID TemplateId`；另有 `public string StringTemplateId => TemplateId`（MongoID 有隐式转 string） | Mono 下 `TemplateId` 隐式转 string 可靠；沿用上游 `item?.TemplateId` 即可（可加 `StringTemplateId` 优先，同 5.0 线） | `ToAltShortString() => item?.TemplateId` | [OK] |
| 45 | `DebugInfo.cs:39` | `Item.Owner.ContainerName` | 存在；`Item.Owner : IItemOwner`；`IItemOwner : IContainer` 声明 `string ContainerName { get; }` | 无需改 | 直接读 | [OK] |
| 46 | `DebugInfo.cs:40` | `Item.Owner.ID` | 存在；`IContainer.ID : string`（`IItemOwner : IContainer`） | 无需改 | 直接读 | [OK] |
| 47 | `DebugInfo.cs:42,49,55` | `Item.CurrentAddress` + `is GridItemAddress / SlotItemAddress / OwnerItself` | `public ItemAddress CurrentAddress { get; set; }`；**三个基类在 0.16.9 均为 `abstract`**（`GridItemAddress` / `SlotItemAddress` / `OwnerItself : ItemAddress`）；具体运行时实例为嵌套派生类型（`Grid+ProtectedGridItemAddress`、`Slot+ProtectedSlotItemAddress`、`StackSlot+ProtectedStackSlotItemAddress`、`ItemController+ProtectedOwnerItself`） | Mono 的 `is` / 强转对普通托管类型完全可靠（IL2CPP 限制不适用）；`is GridItemAddress` 等按基类匹配派生实例，行为正确 | 保留上游 `is` 模式匹配；无需 `TryCast` | [OK] |
| 48 | `DebugInfo.cs:44` | `GridItemAddress.LocationInGrid`（`.x/.y/.r`） | 存在；`public readonly LocationInGrid LocationInGrid`；`LocationInGrid` 有 `public int x`、`public int y`、`public ItemRotation r` | 无需改 | 直接读 | [OK] |
| 49 | `DebugInfo.cs:45` | `GridItemAddress.Grid.ID` | 存在；`public readonly Grid Grid`；`Grid.ID : public string { get; }` | 无需改 | 直接读 | [OK] |
| 50 | `DebugInfo.cs:46` | `Grid.ParentItem.Name.Localized()` | 存在；`Grid.ParentItem : CompoundItem`；`Item.Name : string`（= `Template.NameLocalizationKey`）；`EFT.LocalizationExtensions.Localized(this string, string prefix = null)` | 无需改（Mono 扩展方法直调） | `gridAddress.Grid.ParentItem.Name.Localized()` | [OK] |
| 51 | `DebugInfo.cs:47` | `Grid.ParentItem.Id` | 存在；`Item.Id : string` | 无需改 | 直接读 | [OK] |
| 52 | `DebugInfo.cs:51` | `SlotItemAddress.Slot.ID` | 存在；`public readonly Slot Slot`；`Slot.ID : public string { get; }` | 无需改 | 直接读 | [OK] |
| 53 | `DebugInfo.cs:52,84` | `Slot.ParentItem.Name.Localized()` | 存在；`Slot.ParentItem : public virtual Item`；扩展方法同上 | Mono 下 virtual 属性直取安全 | 直接读 | [OK] |
| 54 | `DebugInfo.cs:55` | `OwnerItself` | 存在；`public abstract class OwnerItself : ItemAddress` | 无需改 | `is not OwnerItself` 判定明确 | [OK] |
| 55 | `DebugInfo.cs:70,72,83,85` | `Slot.ID` / `Slot.ParentItem` / `Slot.ContainedItem` | 存在；`public virtual Item ContainedItem { get; set; }`、`public virtual Item ParentItem { get; }`、`public string ID { get; }` | Mono 下 virtual 属性直取安全（无 IL2CPP AccessViolation 风险） | 直接读 | [OK] |
| 56 | `DebugInfo.cs:114,125,126` | `Quest.Id` / `Quest.QuestStatus` | `Quest : Conditional<Quest>`；`Conditional<TConditional>` 有 `public string Id` 与 `public EQuestStatus QuestStatus` | 无需改 | `quest?.Id` / `quest.QuestStatus` | [OK] |
| 57 | `DebugInfo.cs:134,143` | `Condition.id` | 存在；`public MongoID id { get; set; }`（**字段 → 属性**，小写名保留） | 无需降级（Mono 属性直读） | `condition?.id` 照旧 | [CHANGED] |
| 58 | `DebugInfo.cs:91..104` | `EFT.Profile.TraderInfo.Id` | 存在；`public readonly string Id` | 无需改 | `traderInfo?.Id` | [OK] |

### 1.10 Settings.cs

| # | 来源文件 | 目标（类型.成员） | 0.16.9 实测 | 适配 / 降级 | 一行实现指引 | 状态 |
|---|---|---|---|---|---|---|
| 59 | `Settings.cs:16,44` | `BepInEx.Configuration.KeyboardShortcut` | 存在；`BepInEx.dll`（BepInEx 5 核心）提供 | 无需改（4.1.6 无需 ConfigurationManager shim DLL） | `config.Bind<KeyboardShortcut>(...)` 直接可用 | [OK] |
| 60 | `Settings.cs:44,18` | `KeyboardShortcut.IsDown()` | 存在；`public bool IsDown()`（另有 `IsPressed()`/`IsUp()`） | 无需改 | `Settings.ToggleShortcut.Value.IsDown()` | [OK] |
| 61 | `Settings.cs:44` | TOML 转换器注册 | `KeyboardShortcut` 静态构造函数注册 `TomlTypeConverter.AddConverter` | 无需改 | 引用 `BepInEx.dll` 即自动注册 | [OK] |
| 62 | `Settings.cs:30,39,48` | `ConfigurationManagerAttributes`（vendored） | 上游自带 `ConfigurationManagerAttributes.cs` | 无需外部依赖 | 保持原样 | [OK] |

---

## 2. 降级与风险（可执行方案）

### R1. `UIContext` 类型不存在（触点 20/35–38）
- **结论**：0.16.9 无 `EFT.UI.UIContext`；`UIElement."UI"` 与 `UIInputNode."UI"` 字段类型均为 `EFT.UI.UIParent : CompositeDisposable`。
- **方案（推荐）**：删除 `new R.UIElement(__instance).UI.AddDisposable(...)`，直接 `__instance.AddDisposable(() => ...)`（`UIElement.AddDisposable(Action)` 为 public）。若保留包装，`R.UIContext.Type` 自然解析为 `UIParent`，`AddDisposable` 经基类 `CompositeDisposable` 反射命中——两路均可编译。

### R2. `ModdingScreenSlotView._slot` 闭包捕获时机（触点 23/24）
- **结论**：游戏自身 `Start()` 已为 `_tooltipHoverArea` 添加 `HoverTrigger`；上游 postfix 在其后追加自身 handler，行为正确。但上游把 `Slot ____slot` 作为 `Start` 的 Harmony 注入参数并按值捕获进 hover 闭包——若 `Start` 先于 `Show` 执行（`_slot` 尚为 null），闭包将永久持有 null。
- **方案**：改用 `AccessTools.FieldRefAccess<ModdingScreenSlotView, Slot>("_slot")`（每次回调实时读取），并在回调内对 `_slot == null` 早退；或直接在 hover 回调里 `ItemUiContext.Instance.Tooltip` 判空。

### R3. `transform.Find("TaskShortInfo")`（触点 29）
- **结论**：运行期字符串，静态不可核实。
- **方案**：`var t = __instance.transform.Find("TaskShortInfo"); if (t == null) return;` 后再 `GetOrAddComponent<HoverTrigger>()`。

### R4. 补丁体异常安全（全局）
- **结论**：Mono 下补丁体异常同样穿透游戏调用链导致报错/崩溃。
- **方案**：每个 `[PatchPrefix]/[PatchPostfix]` 体 `try/catch (Exception e) { Logger.LogWarning(e); }`，fail-open；读 `ItemUiContext.Instance` / `Settings.*.Value` / `Slot` 前先空判。与 spec「fail-open 原则」一致。

### R5. 与 5.0 线的命名差异（防交叉污染）
- **结论**：4.1.6 用 `EFT.UI.QuestListItem.Init(Quest, Action<QuestListItem>)`（2 参，未改名）。5.0 线（1.1.5）为 `QuestListItemView.Init(Quest, Action<QuestListItemView>, QuestController)`（3 参）。
- **方案**：4.1.6 工程**不得**照抄 5.0 线的 `QuestListItemView` / 3 参 `Init`；移植时以本表为准。

### R6. `HoverTrigger` 委托（触点 18）
- **结论**：Mono 原生 `event Action<PointerEventData>`，无 IL2CPP 的 `Il2CppSystem.Action<T>` 封送问题。
- **方案**：直接 `+=` / `-=`；无需 `DelegateSupport.ConvertDelegate`（该降级仅 5.0 线需要）。

### R7. `SlotView.Show` 目标解析（触点 17）
- **结论**：`SlotView` 仅一个 `Show` 声明，`AccessTools.Method(typeof(SlotView), nameof(SlotView.Show))` 唯一命中，不歧义。
- **方案**：保持上游写法。

---

## 3. 二次独立核实（工单要求 ≥5，本表做 7 条）

> 方法：主表用 `ilspycmd -t <类型>`（类型级反编译）；下列 7 条**另用** `ilspycmd -m <成员 doc-id>`（成员级反编译）独立复验，两者输出一致。全部命令输出落 `D:\Temp\opencode\spt41-touchpoints\tooltip\`。

| 序 | 触点 | 二次核实命令（doc-id） | 复验结论 |
|---|---|---|---|
| V1 | #6 `SimpleTooltip.Show` | `M:EFT.UI.SimpleTooltip.Show(System.String,System.Nullable{UnityEngine.Vector2},System.Single,System.Nullable{System.Single})` | 命中：`public CancellationToken Show(string text, Vector2? offset = null, float delay = 0f, float? maxWidth = null)` |
| V2 | #27 `QuestListItem.Init` | `M:EFT.UI.QuestListItem.Init(EFT.Quests.Quest,System.Action{EFT.UI.QuestListItem})` | 命中：`public void Init(Quest quest, Action<QuestListItem> onSelect)`（证明 4.1.6 未改名、2 参） |
| V3 | #23 `ModdingScreenSlotView.Start` | `M:EFT.UI.WeaponModding.ModdingScreenSlotView.Start` | 命中：`public void Start()` |
| V4 | #39 `GUIUtility.systemCopyBuffer` | `P:UnityEngine.GUIUtility.systemCopyBuffer` | 命中：`public static extern string systemCopyBuffer { get; set; }` |
| V5 | #7 `ItemUiContext.Instance` | `M:EFT.UI.ItemUiContext.get_Instance` | 命中：`return _instance;`（静态 getter） |
| V6 | #33 `TraderCard._trader` | `F:TraderCard._trader` | 命中：`private Profile.TraderInfo _trader;` |
| V7 | #35/#36 `UIElement."UI"` / `UIInputNode."UI"` | `F:EFT.UI.UIElement.UI` / `F:EFT.UI.UIInputNode.UI` | 命中：均为 `protected readonly UIParent UI;`（坐实 `UIContext → UIParent`） |

附带成员级复验（超出 5 条要求）：`M:CompositeDisposable.AddDisposable(System.Action)`、`F:EFT.Profile.TraderInfo.Id`。

---

## 4. 证据附录

> 每条给出命令 + ≤5 行输出摘录。反编译产物落 `D:\Temp\opencode\spt41-touchpoints\tooltip\`（临时物，不入库）。

### E1. Tooltip 家族（#6–#14）
```powershell
ilspycmd -t "EFT.UI.SimpleTooltip" $CSHARP
```
```
public class SimpleTooltip : Tooltip
public CancellationToken Show(string text, Vector2? offset = null, float delay = 0f, float? maxWidth = null)
public void SetText(string text)
```
```powershell
ilspycmd -t "EFT.UI.PriceTooltip" $CSHARP
```
```
public class PriceTooltip : SimpleTooltip
public TextMeshProUGUI _price
public void Show(EOwnerType ownerType, string text, int price, MongoID? currencyTemplate)
```
```powershell
ilspycmd -t "EFT.UI.ItemUiContext" $CSHARP
```
```
public class ItemUiContext : UIInputNode
public static ItemUiContext Instance { get; set; }
public SimpleTooltip Tooltip => _simpleTooltip;
public PriceTooltip PriceTooltip => _priceTooltip;
public void Update()
```

### E2. ItemPatches 相关（#15–#26）
```powershell
ilspycmd -t "EFT.UI.DragAndDrop.GridItemView" $CSHARP   # public virtual void ShowTooltip()
ilspycmd -t "EFT.UI.DragAndDrop.TradingItemView" $CSHARP # public override void ShowTooltip()
ilspycmd -t "EFT.UI.DragAndDrop.SlotView" $CSHARP        # public virtual void Show(...7 参); public Slot Slot {get;set;}
ilspycmd -t "EFT.UI.DragAndDrop.ModSlotView" $CSHARP     # public void OnPointerEnter(PointerEventData); public bool TryGetArmorSlot(out ArmorSlot,out ArmorPlate)
ilspycmd -t "EFT.UI.WeaponModding.ModdingScreenSlotView" $CSHARP  # public void Start(); public RectTransform _tooltipHoverArea; private Slot _slot
ilspycmd -t "EFT.UI.HoverTrigger" $CSHARP                # event Action<PointerEventData> OnHoverStart/OnHoverEnd
```

### E3. QuestPatches 相关（#27–#30）
```powershell
ilspycmd -t "EFT.UI.QuestListItem" $CSHARP          # public void Init(Quest quest, Action<QuestListItem> onSelect)
ilspycmd -t "EFT.UI.QuestListItemView" $CSHARP      # Could not find a type named 'EFT.UI.QuestListItemView'
ilspycmd -t "EFT.UI.NotesTask" $CSHARP              # public void Show(Quest quest, ..., bool availability)
ilspycmd -t "EFT.UI.QuestObjectiveView" $CSHARP     # public void Show(Quest quest, Condition condition, ..., bool childCondition = false)
```

### E4. TraderPatches / ShortcutPatch（#31–#34）
```powershell
ilspycmd -t "TraderCard" $CSHARP
```
```
public class TraderCard : UIElement, IPointerClickHandler, IEventSystemHandler, IPointerEnterHandler, IPointerExitHandler
private Profile.TraderInfo _trader;
void IPointerEnterHandler.OnPointerEnter(PointerEventData eventData)
void IPointerExitHandler.OnPointerExit(PointerEventData eventData)
```
（`TraderCard` 位于**全局命名空间**，文件无 `namespace` 声明。）

### E5. R.cs 反射包装（#35–#38）
```powershell
ilspycmd -t "EFT.UI.UIElement" $CSHARP      # protected readonly UIParent UI; public void AddDisposable(Action action)
ilspycmd -t "EFT.UI.UIInputNode" $CSHARP    # protected readonly UIParent UI
ilspycmd -t "EFT.UI.UIParent" $CSHARP       # public class UIParent : CompositeDisposable
ilspycmd -t "EFT.UI.UIContext" $CSHARP      # Could not find a type named 'EFT.UI.UIContext'
ilspycmd -t "CompositeDisposable" $CSHARP   # public void AddDisposable(Action destroy)
```

### E6. 剪贴板 / 输入 / BepInEx（#39–#41、#59–#61）
```powershell
ilspycmd -t "UnityEngine.GUIUtility" $IMGUI   # public static extern string systemCopyBuffer { get; set; }
ilspycmd -t "UnityEngine.Input" $INPUT        # GetKey(KeyCode) / GetKeyDown(KeyCode)
ilspycmd -t "BepInEx.Configuration.KeyboardShortcut" $BEP   # public bool IsDown() / IsPressed()
ilspycmd -t "SPT.Reflection.Patching.ModulePatch" $REFL     # protected abstract MethodBase GetTargetMethod(); public void Enable()
```

### E7. DebugInfo 数据模型（#43–#58）
```powershell
ilspycmd -t "EFT.InventoryLogic.Item" $CSHARP       # string Id; MongoID TemplateId; string StringTemplateId; ItemAddress CurrentAddress; IItemOwner Owner; string Name
ilspycmd -t "EFT.InventoryLogic.GridItemAddress" $CSHARP   # abstract class; readonly LocationInGrid LocationInGrid; readonly Grid Grid
ilspycmd -t "EFT.InventoryLogic.SlotItemAddress" $CSHARP   # abstract class; readonly Slot Slot
ilspycmd -t "EFT.InventoryLogic.OwnerItself" $CSHARP       # abstract class OwnerItself : ItemAddress
ilspycmd -t "Slot" $CSHARP                          # virtual Item ContainedItem; virtual Item ParentItem; string ID
ilspycmd -t "Grid" $CSHARP                          # CompoundItem ParentItem; string ID
ilspycmd -t "LocationInGrid" $CSHARP                # int x; int y; ItemRotation r
ilspycmd -t 'Conditional`1' $CSHARP                 # string Id; EQuestStatus QuestStatus
ilspycmd -t "EFT.Quests.Quest" $CSHARP               # class Quest : Conditional<Quest>
ilspycmd -t "EFT.Quests.Condition" $CSHARP           # MongoID id { get; set; }
ilspycmd -t "EFT.Profile+TraderInfo" $CSHARP         # readonly string Id
ilspycmd -t "EFT.LocalizationExtensions" $CSHARP     # static string Localized(this string id, string prefix = null)
```

### E8. 类型清单（抽象/派生地址类，佐证触点 #47）
```powershell
ilspycmd -l c $CSHARP | Select-String 'GridItemAddress|SlotItemAddress|OwnerItself'
```
```
Class EFT.InventoryLogic.GridItemAddress
Class EFT.InventoryLogic.SlotItemAddress
Class EFT.InventoryLogic.OwnerItself
Class EFT.InventoryLogic.Grid+ProtectedGridItemAddress
Class EFT.InventoryLogic.Slot+ProtectedSlotItemAddress
Class EFT.InventoryLogic.StackSlot+ProtectedStackSlotItemAddress
Class EFT.InventoryLogic.ItemController+ProtectedOwnerItself
```

---

## 5. 验收自检

- [x] 覆盖工单列出的全部触点（Tooltip/PriceTooltip/ItemUiContext、GridItemView/TradingItemView/SlotView/ModSlotView/ModdingScreenSlotView、QuestListItem/NotesTask/QuestObjectiveView、TraderCard 指针事件、Shortcut/ItemUiContext.Update、R.cs 四处反射点、私有字段 `____slot`/`____trader`、DebugInfo/DebugTooltip 引用的 EFT 类型）。
- [x] 每触点含：来源文件、目标签名、0.16.9 实测、适配/降级方案、一行实现指引、四态标记。
- [x] 混淆名映射：本子通道为 Tooltip 面，无「瞬产/瞬建/负重」类混淆名（属 DadGamer 子通道）。
- [x] 无 `[MISSING]`；1 条 `[UNCERTAIN]`（`transform.Find("TaskShortInfo")` 运行期字符串，已给空判降级）。
- [x] 二次独立核实 7 条（V1–V7），均在文档中注明。
- [x] 仅写入本文件（未改工程代码、未跑 git 写操作、未写入游戏目录）。

--- END OF TERMINAL ENTRY ---
