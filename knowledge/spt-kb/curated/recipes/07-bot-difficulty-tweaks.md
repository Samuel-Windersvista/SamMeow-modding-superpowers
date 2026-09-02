---
version: [4.1]
domain: server
topic: recipe
recipe_task: bot-difficulty
source: curated
---
# 配方：修改 Bot 难度与行为参数 [4.1]

> 状态：已提炼（源自 BotConfig/BotTable 源码字段 + 示例 2 + wiki Bot_Difficulties；难度文件路径以本地安装为准）
> 适用：[4.1]

## 目标

调整 AI 表现：全局难度参数、单 bot 类型行为、装备权重、生成上限。

## 两个数据入口

### 1. `BotConfig`（配置，注入即得）

```csharp
[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class BotTweaks(BotConfig botConfig) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct)
    {
        botConfig.MaxBotCap["assault"] = 15;              // 单类型 bot 上限
        botConfig.ShowTypeInNickname = false;             // 昵称显示类型
        // Boss 名单：botConfig.Bosses
        // 装备过滤器：botConfig.Equipment["assault"]（EquipmentFilters）
        return Task.CompletedTask;
    }
}
```

### 2. `BotTable`（数据库，按类型改）

```csharp
var assaultBot = botTable.Types["assault"];   // 或 TryGetValue
assaultBot.BotInventory.Equipment[EquipmentSlots.Backpack].AddOrUpdate(ItemTpl.BACKPACK_PILGRIM_TOURIST, 999999);
assaultBot.FirstNames.Add("Gary");
```

## 常用字段速查（BotConfig，源码实读）

| 字段 | 作用 |
|------|------|
| `MaxBotCap` | 各 bot 类型最大数量 |
| `Bosses` | Boss 名单（List<string>） |
| `Equipment` | 类型 → 装备过滤器（EquipmentFilters） |
| `LootItemResourceRandomization` | 物品耐久/资源随机化 |
| `ItemSpawnLimits` | 物品生成数量上限 |
| `AssaultBrainType` / `PlayerScavBrainType` | 大脑类型权重（dict 内层是类型→权重） |
| `Durability` | Bot 装备耐久（BotDurability） |
| `WeeklyBoss` / `GoonSpawnSystem` | 周常 Boss 与 Goon 生成系统 |

## 行为细节（进阶）

- 生成器命名空间 4.1：`Generators.Bot.*`（BotGenerator、BotLevelGenerator、BotEquipmentModGenerator 等）
- 难度预设文件（aiming/receivingDamage 等数值）由 SPT 从 database/bot 生成——要改原始难度数值，查本地 SPT `SPT_Data/Server/database/bots/` 与 `wiki/Bot_Difficulties.md` 的说明（生成管线在 `bot-generator` 仓库）
- 完整 AI 行为替换（战斗逻辑）不是改参数能做的——那是 SAIN 这类 mod 的领域（源码在 `archive/forge/mods/SAIN-Solarint-s-AI-Modifications-Full-AI-Combat-System-Replacement_791_source/`）

## 验证

- 进游戏观察对应 bot 类型行为/装备/数量
- 日志无加载错误

## 坑

- `botTable.Types` 用 `TryGetValue` 判空再操作
- 权重是相对值（见配方 02）
- 多个 bot 参数 mod 冲突时以 TypePriority 为准

## 来源

- 源码：`Libraries/SPTarkov.Server.Core/Models/Spt/Config/BotConfig.cs`
- 示例：`E-Mod开发示例/server-mod-examples/2EditDatabase/`（bot 段）
- `wiki/Bot_Difficulties.md`、`wiki/modding/references/bot-types.md`
