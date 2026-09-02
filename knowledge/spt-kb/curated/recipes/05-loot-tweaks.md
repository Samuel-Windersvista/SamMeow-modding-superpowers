---
version: [4.1]
domain: server
topic: recipe
recipe_task: loot-tweaks
source: curated
---
# 配方：修改战利品刷新（Loot）[4.1]

> 状态：已提炼（源自 LootConfig/InventoryConfig 源码字段 + 示例 18.1 + 迁移文档；生成器细节待源码核对）
> 适用：[4.1]

## 目标

控制战利品生成：松散战利品（loose loot）的刷新点与权重、容器内容、静态战利品权重调整。

## 四个切入点（按用途选）

### 1. 松散战利品（地面散落物）

注入 `LootConfig`，字段：
- `LooseLoot` — `Dictionary<string, Spawnpoint[]>`，按地图 ID 分组的刷新点模板（Spawnpoint 结构见源码 `Models.Spt.Loot` 相关模型）
- `LooseLootSpawnPointAdjustments` — `Dictionary<string, Dictionary<string, double>>`：地图 → 刷新点 → 权重调整
- `StaticItemWeightAdjustment` — `Dictionary<string, Dictionary<MongoId, double>>`：地图 → 物品 → 权重调整

```csharp
[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class LootTweaks(LootConfig lootConfig) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct)
    {
        // 提高 bigmap 上某物品的静态刷新权重
        lootConfig.StaticItemWeightAdjustment["bigmap"][new MongoId("5734779624597737e04bf329")] = 5.0;
        return Task.CompletedTask;
    }
}
```

### 2. 随机容器（战利品箱）内容

注入 `InventoryConfig`，用 `RandomLootContainers[crateId] = RewardDetails`（RewardCount / FoundInRaid / RewardTplPool 物品→权重）——完整代码见配方 03 的步骤 5（示例 18.1）。

### 3. 地图内战利品参数（如容器生成概率）

`LocationTable.GetLocation(mapId).Base` 下的 loot 相关字段（以真实 database JSON 为准）。

### 4. 生成逻辑钩子（进阶）

4.1 生成器命名空间：`Generators.Loot.*`（`LocationLootGenerator`、`BotLootGenerator`、`LootGenerator`、`PMCLootGenerator`）。需要替换生成逻辑时 patch 或继承这些类（先读源码确认可注入性）。

## 验证

- 进图检查目标刷新点/容器是否按预期
- 权重调整是相对值，调大 = 更容易刷出

## 坑

- 地图 ID 用内部名（bigmap/factory4_day 等，查 `wiki/modding/references/location-information.md`）
- `Spawnpoint` 结构复杂，改前先看本地 SPT `SPT_Data/Server/database/locations/<map>/` 与源码模型
- 权重调整对已缓存数据不生效——改动要在加载阶段做（Preload）

## 来源

- 源码：`Libraries/SPTarkov.Server.Core/Models/Spt/Config/LootConfig.cs`、`InventoryConfig.cs`
- 示例：`E-Mod开发示例/server-mod-examples/18.1CustomItemServiceLootBox/`
- 迁移文档：`wiki/SPT_41/Server_40_to_41.md`（Generators.Loot 命名空间）
