---
version: [4.1]
domain: server
topic: database
source: curated
---
# 数据库结构笔记 [4.1]

> 状态：已提炼（源自迁移文档 + 示例 2/13；字段级细节待源码核对）
> 适用：[4.1] | 源码：`E:\云文件\GitHub\SamMeow_SPT410_source_code`

## 访问模型（4.1）

所有数据表在 `SPTarkov.Server.Core.Models.Spt.Tables`，DI 单例注入：

| 表 | 内容 | 典型用途 |
|----|------|---------|
| `GlobalTable` | globals.json：配置节（SavagePlayCooldown、RagFair、BTRSettings 等） | 全局玩法参数 |
| `TemplateTable` | 物品/任务/手册/价格等模板（见属性捷径） | 绝大多数数据读取 |
| `BotTable` | bot 定义（Types["assault"] 等） | bot 装备/名字/难度 |
| `HideoutTable` | 藏身处区域与升级 | 制作/升级修改 |
| `LocationTable` | 地图数据（`GetLocation(id)`） | 撤离点/刷怪/空投 |
| `TradersTable` | 商人数据（`GetTrader(id)`） | 商人修改 |
| `LocaleTable` | 本地化文本 | 多语言 mod |
| `MatchTable` / `ServerTable` / `SettingsTable` | 匹配/服务器/设置 | 低频 |

`TemplateTable` 属性捷径：`Items`、`Quests`、`Handbook`、`Prices`、`Customization`、`Achievements`、`CustomAchievements`、`Profiles`、`LocationServices`

## 典型修改模式（来自示例 2，4.0 写法 → 4.1 注入改写）

```csharp
// [4.1] globals
var globals = globalTable;                       // 原 databaseService.GetGlobals()
globals.Configuration.SavagePlayCooldown = 1;
globals.Configuration.RagFair.MinUserLevel = 1;

// [4.1] bot
bots.Types.TryGetValue("assault", out var assaultBot);   // 原 databaseService.GetBots()

// [4.1] hideout
hideout.Areas.FirstOrDefault(a => a.Type == HideoutAreas.WaterCloset) // 原 GetHideout()

// [4.1] 物品/商人添加（Preload 阶段做）
templateTable.Items[key] = item;
tradersTable.AddTrader(traderBase);  // 方法名以源码为准（示例用 AddCustomTraderHelper）
```

## 数据来源

- 服务器内存数据库初始化自游戏安装的 `SPT_Data/Server/database`（示例 2 注释）
- 客户端数据在 `A-核心服务端/modules/` 与游戏程序集；服务端数据库 ≠ 客户端数据
- 物品 ID 查询：本地 SPT 安装 database 文件夹 / `F-数据与工具/spt-item-finder/`

## 源码坐标（已核实 2026-08-02）

- 表类定义：`Libraries\SPTarkov.Server.Core\Models\Spt\Tables\TemplateTable.cs`（含 `Items`、`Quests`、`Handbook`(HandbookBase)、`Prices`(Dictionary<MongoId,double>)、`Customization`、`Profiles`、`LocationServices` 等）
- `Libraries\SPTarkov.Server.Core\Models\Spt\Tables\LocaleTable.cs` —— **注意**：`Global` 属性源码注释明确「DO NOT USE DIRECTLY, USE LOCALESERVICE INSTEAD」（懒加载，直接改不保存）；改本地化文本必须走 `Services.Locales.LocaleService`
- 数据库加载：服务器启动时读 database 目录的代码（待定位）
- `AddCustomTraderHelper`（示例 13 自带，非官方服务——但展示了 trader 插入所需的全部调用链）
