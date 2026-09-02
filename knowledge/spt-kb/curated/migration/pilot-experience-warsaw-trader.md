---
version: [4.1]
domain: server
topic: migration
source: curated
---
# 从零写商人 mod 实战经验（WarsawTrader 试点）

> 状态：已验证（2026-08-05，SPT 4.1.1 实机验证）
> 目的：记录"从零写一个 server mod（商人 + 击杀任务）"的完整经验，作为 writing-spt-mod 流水线的首个端到端验证

## 1. 试点结论

**writing-spt-mod 服务端流水线端到端跑通**：模板脚手架 -> 4.1 API 改写 -> 编译 -> 部署 -> 启动验证 -> 实机验证。

| 验证项 | 结果 |
|--------|------|
| 模板复用 | 用 `templates/server-mod/` 脚手架，占位符替换后改 |
| 编译 | 0 错误（多轮修正后） |
| 服务器加载 | mod 注册 + OnLoad 成功（日志确认） |
| 商人出现 | 实机可见 Voron 商人 |
| 购买武器 | 修正 preset + BuyRestrictionMax 后可用 |
| 接任务 | 修正 Quest 字段后可用 |

## 2. 踩坑时间线（按严重度）

### 2.1 崩溃级：assort.json barter_scheme 少一层嵌套

PowerShell `ConvertTo-Json` 把 `@(@(@{...}))` 扁平化为两层，`TraderAssort.BarterScheme`（`List<List<BarterScheme>>`）反序列化失败 → **服务器直接崩溃**（Critical exception）。

教训：JSON 生成用 Node.js 或 .NET 泛型 List 强制三层；生成后**用原版商人 assort.json 对照形状**。

### 2.2 功能级：BuyRestrictionMax=0 禁止购买

`purchase limit of 0` 错误。0 不是"无限制"而是"限购 0 个"。

### 2.3 功能级：武器卖模板 ID 不可用

武器模板只是机匣。必须用 globals.json ItemPresets 的完整物品树。

### 2.4 崩溃级（客户端）：Quest 缺字段 NRE

服务器正常但客户端接任务 NRE。可空字段序列化省略 + 客户端必须字段缺失。

### 2.5 显示级：任务目标文本空白

目标文本 = 主条件 ID 的 locale key，不是 description。

## 3. 4.1 API 关键事实（源码实读 + 实测）

### 3.1 商人（Trader）

- 无官方 CustomTraderService（4.1 移除了）——手写 `tradersTable.TryAdd` + `Trader` record
- `OnLoadOrder.PostDBModLoader` 不存在，用 `PostLoad`
- `ModHelper` 在 `Helpers.Server` 命名空间
- 配置类型（RagfairConfig/TraderConfig）直接构造注入

### 3.2 任务（Quest）

- `CustomQuestService.CreateQuest(NewQuestDetails)`，**必须检查 result.Errors**
- 可空字段 null 会被序列化省略 -> 客户端 NRE
- 目标文本 = 主条件 ID locale key
- Elimination = CounterCreator 包 Kills

## 4. 可复用工具

- 武器模板 -> preset 映射：`globals.json ItemPresets._encyclopedia`
- assort 生成：Node.js 脚本（`D:\Temp\opencode` 有参考实现）
- Quest 条件 locale 机制：原版任务 JSON + locales 对照

## 5. 对 writing-spt-mod 流水线的改进建议

1. **模板应包含 TraderAssort 生成辅助类**（FluentTraderAssortCreator 的 4.1 版）
2. **assort 生成脚本进仓库**（Node.js，避免 PowerShell JSON 坑）
3. **Quest 配方应给出字段完整性 checklist**（防客户端 NRE）
4. 验证清单加"实机购买武器 + 接任务"两项

## 6. 关键文件

- Mod：`tools/warsaw-trader-mod/`（base.json / assort.json / ModEntry.cs）
- 配方：`recipes/01-add-custom-trader.md`、`recipes/06-add-custom-quest.md`、`recipes/12-weapon-preset-assort.md`
- 知识：`api-notes-4.1/` 相关
