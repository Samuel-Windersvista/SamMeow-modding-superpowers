---
version: [4.1]
domain: server
topic: recipe
recipe_task: trader-restock
source: curated
---
# 配方：修改商人补货逻辑 [4.1]

> 状态：已提炼（源自 TraderConfig 源码字段 + 示例 13 + 迁移文档）
> 适用：[4.1]

## 目标

控制商人货品刷新周期、全服价格倍率、Fence 特殊机制、开服重置行为。

## 核心入口：`TraderConfig`（源码实读字段）

```csharp
[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class TraderTiming(TraderConfig traderConfig) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct)
    {
        traderConfig.UpdateTimeDefault = 3600;                 // 默认刷新周期（秒）
        traderConfig.TradersResetFromServerStart = true;       // 开服即重置
        traderConfig.TraderPriceMultiplier = 1.0;              // 全服价格倍率
        traderConfig.PurchasesAreFoundInRaid = false;          // 商人收购物是否算战利品
        return Task.CompletedTask;
    }
}
```

## 按商人定制刷新时间

```csharp
traderConfig.UpdateTime.Add(new TraderConfig.UpdateTime
{
    Name = "MyTrader",
    TraderId = "656f0f98d80a697f902d1b25",   // 商人 ID（MongoId）
    Seconds = new MinMax<int> { Min = 900, Max = 1800 }        // 随机区间
});
```

`UpdateTimeDefault` 是兜底值；`UpdateTime` 列表按商人覆盖。示例 13 用 `SetTraderUpdateTime(_traderConfig, traderBase, ...)` 做同样的事（4.1 直接操作配置对象）。

## Fence（`traderConfig.Fence`）

| 字段 | 作用 |
|------|------|
| `AssortSize` | 货品种类数量 |
| `PartialRefreshTimeSeconds` / `PartialRefreshChangePercent` | 部分刷新周期与变动比例 |
| `DiscountOptions` | 折扣机制 |
| `ItemPriceMult` / `PresetPriceMult` | 物品/预设价格倍率 |
| `ItemStackSizeOverrideMinMax` | 堆叠数覆盖 |

## 生成逻辑钩子（进阶）

- 4.1 生成器：`Generators.Ragfair.RagfairAssortGenerator`、`RagfairOfferGenerator`——替换 assort 生成逻辑时继承/patch（先读源码）
- 加载阶段有 `TraderCallbacks`（OnLoadOrder 中段）——与商人相关的回调时序

## 验证

- 启动后看商人刷新时间是否生效（Fence 部分刷新最快可见）
- 价格倍率改动进游戏对价

## 坑

- `TradersResetFromServerStart = true` 会让商人每次开服满货——对整合包节奏有影响，注意
- Fence 字段多且耦合（AssortSize 与价格倍率互相影响），一次改一个变量验证
- 时间单位是秒；示例 13 的 `timeUtil.GetHoursAsSeconds(n)` 可换算

## 来源

- 源码：`Libraries/SPTarkov.Server.Core/Models/Spt/Config/TraderConfig.cs`
- 示例：`E-Mod开发示例/server-mod-examples/13AddTraderWithAssortJson/`
- 迁移文档第 4 节（表/配置注入）与 Routers 节
