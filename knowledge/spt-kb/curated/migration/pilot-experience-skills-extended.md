---
version: [4.1]
domain: both
topic: migration
source: curated
---

# 迁移流水线实战经验（SkillsExtended 三合一试点）

> 状态：已验证（2026-08-05，SkillsExtended 试点）
> 目的：记录"服务端 TS + 客户端 DLL + bundle"三合一 mod 的迁移经验

---

## 1. 试点结论

**SkillsExtended 服务端部分完整迁移验证通过**：

| 验证项 | 结果 |
|--------|------|
| 服务端编译 | 0 错误 0 警告（一次通过） |
| 服务端加载 | mod 加载成功，0 错误 |
| 行为（dbdump） | Lockpick 物品（6622c28a...）创建成功 + 加入特殊槽位 filters |
| 制作配方 | 1 个配方添加成功 |
| 多语言 | locale 导入成功 |
| bundle | 服务端 2 个 + 客户端 2 个 bundle 复制（未实机验证客户端） |

**客户端 DLL 部分：标记为流水线已知边界（半自动）** -- 见下。

## 2. 客户端混淆名解析（核心发现）

### 2.1 有源码但引用混淆名

SkillsExtended 客户端有**完整 C# 源码**（`Plugin/` 目录，51 个文件，25+ Harmony patch），但：
- 引用 8 个混淆名（GClass2024、GClass2823、GClass2848、GClass2849、GStruct264、Class1930、GClass2064）
- 分布在 4 个文件（SkillManagerExt、PersonalBuffPatch、GetBarterPricePatch、PersonalBuffStringPatches）
- 这些混淆名**都不在 4.0->4.1 映射表里**（映射表只覆盖类名层，且编号对齐未验证）

### 2.2 4.1 反混淆是全量的

实机验证：8 个混淆名在 4.1 Assembly-CSharp.dll 里**全部 0 次出现**。连 3.11 里看似稳定的 `SkillManager.SkillBuffClass` 在 4.1 里也不存在（-1）-- **4.1 对嵌套类也全量反混淆**。

### 2.3 结论

客户端混淆名解析是**半自动**：
- 不能用映射表机械替换（映射表不覆盖）
- 需要**成员签名匹配**（反编译 3.11 混淆类看成员 -> 在 4.1 搜索匹配类）
- 需要 dnSpy/ILSpy 级别的逆向工具（不是文本搜索）
- 或等待社区/作者发布 4.1 版

**这是流水线的明确边界** -- 有源码客户端 mod 的混淆名解析需要一个专门的辅助工具（议会 P1 建议的成员签名匹配工具）。

## 3. 关键坑（新增）

### 坑 6：4.1 NewItemFromCloneDetails 需要 newItemName

3.11 的 Items.json 数据没有 `newItemName` 字段，但 4.1 的 `NewItemFromCloneDetails.NewItemName` 是 **required**。反序列化报：
```
JSON deserialization for type 'NewItemFromCloneDetails' was missing required properties including: 'newItemName'
```

**规则**：3.11 的物品 JSON 数据（Items.json）迁移时**必须补 `newItemName` 字段**（从 newId 或语义推导，如 `item_special_lockpick`）。这是数据形状适配，不是代码翻译。

### 坑 7：复杂 mod 的服务端迁移一次编译通过是可能的

SkillsExtended 服务端（mod.ts + 4 Managers + 路由 + 物品 + 制作 + 成就 + locale）**一次编译通过（0 错误）** -- 说明：
- 4.1 的 API 映射表 + 配方已覆盖大部分模式
- 前两个试点积累的模型知识（TemplateItem/Slot/Grid/BarterScheme/HideoutProduction）直接复用
- 编译闸门在复杂场景下效率高（一次通过 = 映射准确）

## 4. 迁移模式确认（复杂 mod 服务端）

3.11 复杂服务端 mod（多 Manager + 路由）迁移要点：
1. **InstanceManager 整个类删除** -> 构造注入（这是最大的结构简化）
2. **RouteManager** -> `StaticRouter` 子类 + `[Injectable(TypePriority = OnLoadOrder.Routers + 1)]`
3. **AchievementManager** -> `templateTable.Achievements`（4.1 是 List<Achievement>）
4. **addCraftsToDatabase** -> `hideoutTable.Production.Recipes.Add`
5. **多语言导入** -> 每个语言文件一个 `AddTransformer`
6. **数据文件**（Achievements/Items/Crafting/Locales）原样搬运 + 形状适配（补 newItemName）

## 5. 客户端迁移路线图（更新）

| 客户端类型 | 迁移路径 | 自动化率 |
|-----------|---------|---------|
| 无混淆引用（源码） | AST 改名 + 重编译 | 80-90% |
| 有混淆引用（源码，少） | 成员签名匹配 + 重编译 | 50-70%（需工具） |
| 无源码 DLL | 反编译 + 重写 | 20-30% |
| **引用已删除/重构的类** | **人工评估功能是否可迁移** | 最低 |

**SkillsExtended 客户端**属于"有混淆引用（少，8 个）+ 完整源码" -- 是成员签名匹配工具的最佳测试对象（待工具落地后）。
