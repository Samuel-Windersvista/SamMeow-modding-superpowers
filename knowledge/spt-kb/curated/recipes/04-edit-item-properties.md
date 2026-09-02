---
version: [4.1]
domain: server
topic: recipe
recipe_task: edit-item
source: curated
---
# 配方：修改现有物品属性 [4.1]

> 状态：已提炼（源自示例 2 的数据访问模式 + 4.1 表注入；字段名以本地 SPT database 为准）
> 与配方 02 同体系：02 改「全局/地图/藏身处」，本配方改「物品模板」

## 目标

批量/定点修改现有物品的模板属性：重量、价格、类别、可交易性、槽位等。

## 数据入口（4.1）

注入 `TemplateTable`，物品在 `templateTable.Items`（`Dictionary<MongoId, TemplateItem>`）：

```csharp
[Injectable(TypePriority = OnLoadOrder.Preload + 1)]
public class EditItems(ISptLogger<EditItems> logger, TemplateTable templateTable) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct)
    {
        // 按 ID 取物品
        var item = templateTable.Items.GetValueOrDefault(new MongoId("59faf98186f774067d6e2744")); // 某弹药箱
        // ... 改属性
        return Task.CompletedTask;
    }
}
```

4.0 写法对照：`databaseServer.GetTables().Templates.Items` → `templateTable.Items`；`PostDBModLoader` → `Preload`（加载数据阶段）。

## 常用属性（TemplateItem，以真实 database JSON 为准）

| 想改什么 | 属性路径（示意） |
|---------|----------------|
| 重量 | `item._props.Weight` |
| 图鉴价 | `templateTable.Handbook` 中对应条目的 `Price` |
| 跳蚤可见性 | `templateTable.Prices` 条目存在性（删掉即不可上架；`Dictionary<MongoId, double>`） |
| 名称/描述 | **不要直接改 `LocaleTable.Global`**（源码注释：懒加载，改动不保存）——用 `LocaleService`（`Services.Locales.LocaleService`） |
| 分类节点 | `item._parent`（物品树父节点 ID） |
| 允许放进某容器 | 容器物品的 `Grids` 槽位 `_props.filters` |

> 注意：`Items` 字典的 key 与 `_props` 结构随 EFT 版本变化，**一切以本地 SPT 安装 `SPT_Data/Server/database/templates/items/<id>.json` 的真实 JSON 为准**——写码前先打开对应 JSON 看字段名。

## 批量修改模式

```csharp
// 遍历某类物品（如所有 12 号口径弹药）
foreach (var kvp in templateTable.Items)
{
    if (kvp.Value._parent == "5485a8684bdc2da71d8b4567") // 弹药父节点
    {
        kvp.Value._props.Weight *= 0.5f;
    }
}
```

## 验证

- 启动日志正常；游戏内物品工具提示显示新属性
- 图鉴/跳蚤价格改动进游戏核实（注意价格缓存）

## 坑

- 物品 ID 抄错 = 静默无效；用 `ItemTpl` 常量（可读）或先从 database JSON 复制 ID
- 只读属性 vs 可写属性：先看 JSON 结构再写代码，别猜
- 多个 mod 改同一物品时，TypePriority 决定覆盖顺序——后加载者胜（`Preload + 1` vs `Preload + 2`）
- 价格类修改可能被 `Prices` 缓存影响（4.1 有 `InMemoryCacheService`，见 `Services.Modding.InMemoryCacheService`）——必要时清缓存或改服务器配置

## 来源

- `E-Mod开发示例/server-mod-examples/2EditDatabase/`（访问模式）
- 本地 SPT 安装 `SPT_Data/Server/database/templates/`
- `wiki/SPT_41/Server_40_to_41.md`（TemplateTable、命名空间）
