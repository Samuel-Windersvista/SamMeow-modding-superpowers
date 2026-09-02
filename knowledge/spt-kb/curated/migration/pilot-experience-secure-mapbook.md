---
version: [4.1]
domain: server
topic: migration
source: curated
---

# 迁移流水线实战经验（SecureMapbookMod bundle 试点）

> 状态：已验证（2026-08-05，SecureMapbookMod 端到端试点）
> 目的：记录 TS->C# + bundle 迁移的第二轮实战经验

---

## 1. 试点结论

**SecureMapbookMod 完整迁移验证通过** -- 3.11 TS mod + 物品 bundle -> 4.1 C# mod：

| 验证项 | 结果 |
|--------|------|
| 编译 | 0 错误 0 警告 |
| 加载 | 模组注册成功，bundle 注册，0 错误 |
| 行为（dbdump） | mapbook 物品（6621a2e3a8d8b1a9f0e3b4c5）成功创建，含槽位 filters |
| bundle | mapbook.bundle（4.94 MB，Unity 2022.3.43f1）零改动直接加载 |

## 2. Bundle 迁移验证（关键结论）

1. **bundle 文件零改动直接可用** -- 本 mod 的 bundle 是 Unity 2022.3.43f1 构建（与 4.1 游戏引擎一致），复制即加载
2. **bundles.json 零改动** -- 3.11 的 manifest 格式与 4.1 兼容（key + dependencyKeys）
3. **IsBundleMod 移除验证** -- 3.11 package.json 的 `"isBundleMod": true` 在 4.1 不需要（4.1 检测 bundles.json 存在性），直接删字段即可
4. **bundle 部署路径** -- `user/mods/<ModName>/bundles/` + `bundles.json` 与 mod DLL 同目录

## 3. 关键坑（新增）

### 坑 4：3.11 mod 自带的 ID 生成 bug

SecureMapbookMod 原版用 `String.fromCharCode(98 + i)` 生成 slot ID：
- i=0..4 -> 'b','c','d','e','f'（合法 hex）
- i>=5 -> 'g','h','i','j','k','l'（**非 hex！**）

4.1 的 MongoId 构造函数严格校验 hex，**第 6 个槽位起抛 `ObjectId contains invalid hex characters`**。

**规则**：迁移时如遇 MongoId 校验失败，检查 3.11 的 ID 生成逻辑是否本身有 bug（charCode 生成非 hex）。迁移版应修复为合法 hex（`"0123456789abcdef"[i % 16]`）。

### 坑 5：4.1 模型差异（第二轮补充）

| 3.11 概念 | 4.1 模型 | 备注 |
|-----------|---------|------|
| `slot._props` | `Slot.Properties` | 属性名不同 |
| `filter.Filter` | `HashSet<MongoId>` | 不是 List<string> |
| `assort.barter_scheme` | `Dictionary<MongoId, List<List<BarterScheme>>>` | BarterScheme 类型 + 双层 List |
| `assort.loyal_level_items` | `Dictionary<MongoId, int>` | |
| `Slots`/`Grids` | `IEnumerable<Slot>` / `IEnumerable<Grid>` | 需 ToList 处理，且 setter 可整体替换 |
| `barterSchemeItem._tpl` | `BarterScheme.Template` | MongoId |
| `item._id`/`item._tpl` | `Item.Id` / `Item.Template` | MongoId |

## 4. 验证方法（dbdump diff，bundle 场景）

**已验证有效**：dbdump dump templateItems.json，检查目标物品 ID 是否存在：
- `6621a2e3a8d8b1a9f0e3b4c5` 出现在 items dump = 物品创建成功
- 检查槽位 filters 是否正确继承/设置

**bundle 验证**：服务器日志出现 `/singleplayer/bundles` 请求 + mod 加载 0 错误 = bundle 分发正常。实机进游戏看模型为最终验证。

## 5. 迁移模式确认（CustomItemService）

3.11 的 `container.resolve("CustomItemService").createItemFromClone(details)` 迁移为：

```csharp
var clone = new NewItemFromCloneDetails
{
    ItemTplToClone = "...",
    OverrideProperties = props,
    ParentId = "...",
    NewId = config.ItemId,
    NewItemName = "unique_name",  // 4.1 required，3.11 没有
    Locales = new() { ["en"] = new() { Name = "...", ShortName = "...", Description = "..." } },
};
customItemService.CreateItemFromClone(clone);  // 同步
```

**注意**：4.1 的 `NewItemFromCloneDetails` 有 required 的 `NewItemName` 字段（3.11 没有），必须补。
