---
version: [4.1]
domain: server
topic: recipe
recipe_task: edit-database
source: curated
---
# 配方：修改数据库数值（globals / bot / hideout / 地图）[4.1]

> 状态：已提炼（源自示例 2 + 迁移文档表注入规则；字段名以源码为准）
> 模板示例：`E-Mod开发示例/server-mod-examples/2EditDatabase/EditDatabaseValues.cs`（4.0 语法）

## 目标

不改游戏文件、用代码改服务器内存数据库里的任意数值：全局规则、bot 装备、藏身处成本、地图撤离/空投/刷 Boss。

## 核心模式（4.1）

把 4.0 的 `databaseService.GetXxx()` 换成构造注入对应 Table：

```csharp
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class EditDb(GlobalTable globalTable, BotTable botTable,
    HideoutTable hideoutTable, LocationTable locationTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct)
    {
        EditGlobals(); EditBots(); EditHideout(); EditLocations();
        return Task.CompletedTask;
    }
}
```

## 常用修改点（来自示例 2，已映射 4.1）

| 想改什么 | 4.1 写法 |
|---------|---------|
| 拾荒冷却 | `globalTable.Configuration.SavagePlayCooldown = 1` |
| 跳蚤解锁等级 | `globalTable.Configuration.RagFair.MinUserLevel = 1` |
| 跳蚤挂单数 | 遍历 `ragFairSettings.MaxActiveOfferCount` 设 `Count` |
| BTR 皮肤（Woods） | `globalTable.Configuration.BTRSettings.MapsConfigs["Woods"].BtrSkin = "Tarcola"` |
| 厕所升级时间/需求 | `hideoutTable.Areas.First(a => a.Type == HideoutAreas.WaterCloset).Stages` 逐 stage 设 `ConstructionTime`、`Requirements.Clear()` |
| scav 必带某背包 | `botTable.Types["assault"].BotInventory.Equipment` 的 `Backpack` 字典 `AddOrUpdate(ItemTpl.BACKPACK_PILGRIM_TOURIST, 999999)` |
| scav 必带 M4A1 | `FirstPrimaryWeapon` 字典 `AddOrUpdate(ItemTpl.ASSAULTRIFLE_COLT_M4A1_556X45_ASSAULT_RIFLE, 999999)` |
| scav 名字 | `assaultBot.FirstNames.Clear(); Add("Gary")` |
| 撤离点必刷 | `locationTable.GetLocation("bigmap").Base.Exits` 循环设 `Chance = 100; ChancePVE = 100` |
| 空投 100% 且提前 | `locationTable.GetLocation("bigmap").Base.AirdropParameters.First()` 设 `PlaneAirdropChance = 1`、`PlaneAirdropStartMin = 1` |
| Boss 必刷 | `locationTable.GetLocation("bigmap").Base.BossLocationSpawn` 找 `BossName == "bossBully"` 设 `BossChance = 100` |

## 验证

- 启动日志见 mod 输出（示例用 `logger.Success("Finished Editing Database!")`）
- 进游戏实测对应数值（进图看撤离点、看 scav 装备、看跳蚤等级）

## 坑

- 物品 ID 用 `ItemTpl` 常量（可读性好）或裸 24 位 hex 字符串；ID 抄错 = 静默无效
- 权重（pick chance）数值 = 相对权重，设极大值即可垄断选择
- `TryGetValue` 失败时对象为 null——先用 `FirstOrDefault`/`TryGetValue` 判空再操作
- 字段名随 EFT 版本变化——以本地 SPT 安装 `SPT_Data/Server/database/` 下真实 JSON 为准
