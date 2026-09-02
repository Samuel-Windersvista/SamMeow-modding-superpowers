---
version: [4.1]
domain: server
topic: recipe
recipe_task: add-quest
source: curated
---
# 配方：添加自定义任务（Quest）[4.1]

> 状态：已提炼 + 实战验证（2026-08-05，WarsawTrader 试点：击杀任务 First Contract）
> 涉及服务：`SPTarkov.Server.Core.Services.Modding.Custom.CustomQuestService`（4.0 是 `Services.Mod.CustomQuestService`）
> 完整可运行示例：`tools/warsaw-trader-mod/src/ModEntry.cs`（CreateKillQuest 方法）

## 目标

往 SPT 4.1 添加一个任务：有名称/描述/目标、可限定阵营（Usec/Bear）、出现在任务列表。

## 核心 API（源码实读确认）

```csharp
// NewQuestDetails（Models.Spt.Mod）
public record NewQuestDetails
{
    public required Quest NewQuest { get; init; }                     // 任务本体（数据库 Quest 模型）
    public required Dictionary<string, Dictionary<string, string>> Locales { get; init; }  // 语言 → (locale键 → 文本)
    public PlayerSide? LockedToSide { get; init; }                    // 仅限 Usec/Bear；null = 双方
}

// CreateQuestResult —— 必须检查 Errors！
public record CreateQuestResult(bool Success, MongoId? QuestId)
{
    public bool Success { get; set; }
    public MongoId? QuestId { get; set; }
    public List<string> Errors { get; } = [];
}
```

## 步骤

1. **元数据**：`IModMetadata`
2. **主类**：`[Injectable(TypePriority = OnLoadOrder.Preload + 1)]` 实现 `IOnLoad`，注入 `CustomQuestService`
3. **组装 Quest 对象**：任务结构参照现有任务——最稳的做法是从 `templateTable.Quests` 挑一个简单任务（如 `5d25e29d86f7740a8e572981` 附近的基础任务）读它的完整 JSON 形状，替换 ID/目标字段。任务模板数据在本地 SPT `SPT_Data/Server/database/templates/quests/<id>.json`
4. **组装 Locales**：`{"en": {"<questId>": "任务名", "<questId>_description": "...", ...}}`——键格式参考现有任务 locale 条目（`LocaleTable.Global` 中 `quest_<id>_name` 之类，从真实数据抄）
5. **调用并检查**：
   ```csharp
   var result = customQuestService.CreateQuest(new NewQuestDetails { ... });
   if (!result.Success) { foreach (var e in result.Errors) { logger.Error(e); } }
   ```
6. **（可选）限定阵营**：`LockedToSide = PlayerSide.Usec`——内部写入 `QuestConfig.UsecOnlyQuests`（源码确认）

## 验证

- 启动日志无 `CreateQuest` 错误（Errors 非空即失败，含本地化错误文案）
- 进游戏任务列表出现该任务，文本正确
- 限定阵营的任务换阵营角色验证不可见

## 坑

- **必须检查 CreateQuestResult.Errors**——Quest ID 已存在、locales 为空、语言键不存在都会导致失败（且是静默的，不抛异常）
- `LockedToSide` 只接受 Usec/Bear；Savage 会报错
- Quest 模型字段极多，抄现有任务改最省事；用任务编辑器（如 SVM 或社区工具）导出的 JSON 更稳
- 任务奖励/条件对象引用物品 ID，写错静默无效——所有 ID 从 database JSON 复制

## 坑（实战验证，客户端 NRE 专项）

### 客户端 NRE：可空字段被序列化省略

**症状**：服务器加载成功（CreateQuest 无错误），任务显示在列表，但**点"接受"时客户端报 `Object reference not set to an instance of an object`**，且服务器日志**无任何请求记录**（NRE 发生在客户端发 accept 请求之前）。

**根因**：C# 模型中可空字段（`string?`/`List<string>?`/`int?`）为 null 时 System.Text.Json 默认**省略该字段**，客户端解析 Quest JSON 时访问缺失字段 → NRE。

**修复**：补齐以下字段（对照原版任务如 Debut 的真实值）：

| 字段 | 示例值 | 说明 |
|---|---|---|
| `acceptanceAndFinishingSource` | `"eft"` | 非 required 但客户端必读 |
| `progressSource` | `"eft"` | |
| `startedMessageText` | `"<questId> startedMessageText"` | 接任务时显示 |
| `status` | `0` | |
| `secretQuest` | `false` | |
| `instantComplete` | `false` | |
| `isKey` / `KeyQuest` | `false` | |
| `gameModes` | `["regular", "pve"]` | |
| `rankingModes` / `arenaLocations` | `[]` | |
| `note` | `"<questId> note"` | |

### 任务目标（objective）文本 = 条件 ID 的 locale key

**症状**：任务描述正常，但**目标栏空白**（"Eliminate Scavs" 之类没显示）。

**机制**：EFT 客户端对 Elimination 类条件，用**主条件的 `Id`**（即 `Type="Elimination"` 且 `conditionType="CounterCreator"` 的那个 QuestCondition 的 Id）作为 locale key 显示目标文本。与 `dynamicLocale` 无关（原版 `dynamicLocale: false` 也生效）。

```csharp
locales["en"]["85b81de879a7e78cb75443fc"] = "Eliminate Scavs on any location";
// 其中 85b81de8... = 主条件（Elimination/CounterCreator）的 Id
```

### Reward 必需字段（否则序列化缺失）

每个 reward 补：
- `gameMode: ["regular", "pve"]`
- `findInRaid: false`（Item 类）
- `isEncoded: false`
- `isHidden: false`
- `unknown: false`
- `availableInGameEditions: []`
- Item 类 reward：`value` 也要设（金额/数量）

### 条件内部结构（Elimination = CounterCreator 包 Kills）

```
QuestCondition (type=Elimination, conditionType=CounterCreator, value=10)
└── Counter (QuestConditionCounter, id=string)
    └── Conditions[0] (QuestConditionCounterCondition, conditionType=Kills, target=ListOrT("Savage"), value=10)
```

- `QuestConditionCounter.Id` 是 **string**（不是 MongoId）
- `QuestConditionCounterCondition.Id` 是 **MongoId?**
- `Target` 是 `ListOrT<string>`（`new ListOrT<string>(null, "Savage")`），**不能直接赋 string**
- 距离/时段：`CounterConditionDistance` / `DaytimeCounter`（不是 CounterDistance/CounterDaytime）
- 枚举 `QuestTypeEnum` / `RewardType` 在 `SPTarkov.Server.Core.Models.Enums`
- `QuestCondition.ConditionType` 是 **required**

## 来源

- 源码：`Libraries/SPTarkov.Server.Core/Services/Modding/Custom/CustomQuestService.cs`、`Models/Eft/Common/Tables/Quest.cs`（Quest/QuestCondition/QuestConditionCounter/Reward）
- 数据参照：本地 SPT `SPT_Data/Server/database/templates/quests/` + `locales/global/en.json`（条件 ID locale key 机制）
- 实战：`tools/warsaw-trader-mod/src/ModEntry.cs`（本仓库）
