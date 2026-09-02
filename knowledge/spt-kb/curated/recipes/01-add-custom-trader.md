---
version: [4.1]
domain: server
topic: recipe
recipe_task: add-trader
source: curated
---
# 配方：添加自定义商人 [4.1]

> 状态：已提炼 + 实战验证（2026-08-05，WarsawTrader 试点：商人 + 击杀任务 + 40 种华约武器/弹药/弹匣）
> 模板示例：`E-Mod开发示例/server-mod-examples/13AddTraderWithAssortJson/`（4.0 语法）
> 动态生成 assort 版本：`13.1AddTraderWithDynamicAssorts/`（FluentTraderAssortCreator）
> 完整可运行示例：`tools/warsaw-trader-mod/`（本仓库，含 base.json + assort.json + 任务）

## 目标

往 SPT 4.1 服务器加一个可交易的商人（含头像、上货、本地化、跳蚤可见性）。

## 前置

- 已读 `modding-guide/02-server-mod-anatomy.md`（元数据 + DI + 表注入）
- 商人基础 JSON：`id`（物品 ID 格式）、`avatar` 图片路径、名称等
- assort JSON：三件套 = 物品 + 兑换方案（金钱/以物换物）+ 忠诚等级要求

## 步骤（4.1 实战验证版）

1. **元数据**：`IModMetadata`（4.1 接口，**不是** 4.0 的 `AbstractModMetadata`，不要写 `override`），`SptVersion = new("~4.1.0")`
2. **主类**：`[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]` 实现 `IOnLoad`
   - **坑**：4.0 的 `OnLoadOrder.PostDBModLoader` 在 4.1 **不存在**（4.1 阶段精简为 Watermark/Preload/GameCallbacks/.../PostLoad，见 `DI/OnLoadOrder.cs`），用 `PostLoad` 即可
   - **坑**：`ModHelper` 命名空间是 `SPTarkov.Server.Core.Helpers.Server`（不是 `Helpers`）
   - **坑**：4.1 生命周期方法是 `OnLoadAsync(CancellationToken)` 不是 `OnLoad()`；取消时让 OperationCanceledException 传播
   ```csharp
   [Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
   public class WarsawTraderModEntry(
       ISptLogger<WarsawTraderModEntry> logger,
       ModHelper modHelper,          // SPTarkov.Server.Core.Helpers.Server
       ImageRouter imageRouter,
       TradersTable tradersTable,    // 4.1 表注入（非 DatabaseService）
       LocaleTable localeTable,
       RagfairConfig ragfairConfig,  // 4.1 直接注入具体配置类型（非 configServer.GetConfig）
       TraderConfig traderConfig,
       TimeUtil timeUtil,
       ICloner cloner,
       CustomQuestService customQuestService) : IOnLoad
   ```
3. **图片路由**：`imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg",""), traderImagePath)`；头像文件不存在时跳过（不会崩，只是没图）
4. **上货时间**：`traderConfig.UpdateTime.Add(new UpdateTime { TraderId = base.Id, Seconds = new MinMax<int>(min, max) })`
5. **跳蚤可见**：`ragfairConfig.Traders.TryAdd(traderBase.Id, true)`
6. **入库**（4.1 手写，无官方 CustomTraderService）：
   - `tradersTable.TryAdd(id, trader)`，`Trader` record 需要 `Assort`（空 assort）+ `Base`（cloner.Clone）+ `QuestAssort`（{"Started":{},"Success":{},"Fail":{}}）+ `Dialogue`
   - `TradersTable` 继承 `Dictionary<MongoId, Trader>`，有 `GetTrader(id)` 方法
   - 后覆盖 assort：`tradersTable.GetTrader(id).Assort = newAssort`
7. **本地化**：`localeTable.Global` 遍历 + `localeKvP.AddTransformer(...)` 写入 5 个 key（`{id} FullName/FirstName/Nickname/Location/Description`）
8. **数据文件**：`base.json`、`assort.json`、头像图放 mod 文件夹 `data/`，`modHelper.GetJsonDataFromFile<T>(pathToMod, "data/x.json")` 读

## 验证（Level B + 实机）

- 服务器日志 `Warsaw Pact Trader loaded: ...`（mod 的 OnLoad 成功）
- 游戏内商人出现、头像正常、能买能卖
- 不同语言客户端切语言验证本地化
- 跳蚤市场能搜到该商人货品（若设置了 Traders.TryAdd）

## 坑（实战验证，全部踩过）

1. **`BuyRestrictionMax: 0` = 禁止购买**（不是无限制！）——购买报错 `Unable to purchase: 1 items, this would exceed your purchase limit of 0`。无限制 = `UnlimitedCount: true` + `StackObjectsCount` 大数 + `BuyRestrictionMax` 设大值（如 500）。武器/弹匣限购设 `BuyRestrictionMax: 3~5`
2. **assort 的 barter_scheme 必须三层嵌套**：`{"itemId": [[{"count": N, "_tpl": "..."}]]}` = `Dictionary<MongoId, List<List<BarterScheme>>>`。少一层报 `JsonException ... could not be converted to List<BarterScheme>` 且**服务器直接崩溃**
3. **PowerShell `ConvertTo-Json` 会把单元素嵌套数组扁平化**——`@(@(@{...}))` 生成两层而非三层。用 .NET 泛型 List 强制嵌套，或直接手写 JSON。Node.js 无此问题
4. **武器必须卖 preset 而不是武器模板 ID**——武器模板（`_tpl`）只是机匣/框架，买了不可用。整把裸枪是 globals.json `ItemPresets` 里的 preset（物品树 = 机匣 + 全部配件）。详见 `12-weapon-preset-assort.md`
5. 商人 ID 必须是合法 MongoId（24 位 hex）
6. 头像路径大小写敏感
7. 4.0 示例用 `configServer.GetConfig<TraderConfig>()`——4.1 直接构造注入
8. `ModHelper.GetJsonDataFromFile<T>` 读 base.json 时 `TraderBase` 模型的 `_id`/`name`/`loyaltyLevels` 等字段要齐全（可从原版商人 base.json 抄）

## 来源

- 源码：`Libraries/SPTarkov.Server.Core/Models/Eft/Common/Tables/Trader.cs`（Trader/TraderBase/TraderAssort/BarterScheme）
- 数据参照：本地 SPT `SPT_Data/Server/database/traders/<原版商人id>/`（base.json + assort.json）
- 实战：`tools/warsaw-trader-mod/`（本仓库）
