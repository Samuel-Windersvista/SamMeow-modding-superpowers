---
version: [4.1]
domain: both
topic: recipe
recipe_task: add-item
source: curated
---
# 配方：添加自定义物品 [4.1]

> 状态：已提炼（源自示例 18 / 18.1 + 4.1 迁移文档；方法签名以源码为准）
> 模板示例：`E-Mod开发示例/server-mod-examples/18CustomItemService/`、`18.1CustomItemServiceLootBox/`（4.0 语法）
> 涉及服务（4.1 命名空间）：`SPTarkov.Server.Core.Services.Modding.Custom.CustomItemService`（4.0 是 `Services.Mod.CustomItemService`）

## 目标

往 SPT 4.1 添加一个全新物品（复制现有物品改参数），可选：让其出现在战利品容器/空投中。

## 核心概念（先读再写）

- `NewItemFromCloneDetails` = 克隆一个现有物品所需的全部信息
- `ItemTplToClone` — 被克隆的原物品 ID
- `ParentId` — 新物品在「物品分类树」下的节点 ID（武器节点 5447b6094bdc2dc3278b4567、容器节点 62f109593b54472778797866 等；用 db.sp-tarkov.com 查）
- `NewId` — 新物品 ID，**必须是合法 MongoId（24 位 hex）**，可用在线 MongoId 生成器
- `HandbookParentId` — 手册（图鉴）分类 ID，与 ParentId 可不同；参照 `SPT_Data/Server/database/templates` 里的 handbook 数据
- `FleaPriceRoubles` / `HandbookPriceRoubles` — 跳蚤价 / 手册价
- `Locales` — 多语言名（缺语言则游戏内显示空白）
- `OverrideProperties` — 可覆盖的模板属性（重量、名称、槽位等）

## 步骤（4.1 改写版）

1. **元数据**：`IModMetadata`（去 override、删 `IsBundleMod`）
2. **主类**：`[Injectable(TypePriority = OnLoadOrder.Preload + 1)]` 实现 `IOnLoad`（4.1 迁移文档明确：往数据库加数据在 Preload 做；4.0 示例用的 `PostDBModLoader + 1` 已不存在）
   ```csharp
   [Injectable(TypePriority = OnLoadOrder.Preload + 1)]
   public class AddCustomItem(ISptLogger<AddCustomItem> logger,
       CustomItemService customItemService) : IOnLoad
   {
       public Task OnLoadAsync(CancellationToken ct)
       {
           var clone = new NewItemFromCloneDetails { /* ...见示例 18... */ };
           customItemService.CreateItemFromClone(clone);
           return Task.CompletedTask;
       }
   }
   ```
3. **组装 `NewItemFromCloneDetails`**：照抄示例 18 的结构（武器要配 `OverrideProperties.Chambers` 槽位过滤器，否则无法装弹——示例 18 有一段完整的 patron_in_weapon_000 槽位配置）
4. **调用** `customItemService.CreateItemFromClone(clone)`
5. **（可选）加入战利品池**：示例 18.1 演示了两件事——
   - 注入 `TemplateTable`（4.1）取刚创建的物品：`templateTable.Items.GetValueOrDefault(crateId)`，改 `Name` 防止被原物品逻辑（空投等）误处理
   - 注入 `InventoryConfig`（4.1 直接注入配置类型，替代 `configServer.GetConfig<InventoryConfig>()`），注册到 `RandomLootContainers[crateId]`，`RewardTplPool` 是 物品ID→权重 字典

## 验证

- 服务器启动日志无错误；物品出现在图鉴对应分类下
- 进游戏用控制台/模组菜单刷出该物品，检查属性（重量、可装弹等）
- 战利品箱配方：开箱子看是否产出池内物品

## 坑

- `NewId` 非法 MongoId → 启动/加载失败；用生成器生成后别再手改
- `ParentId` 写错 → 物品可能不在预期分类（甚至物品树异常）
- `Locales` 缺失 → 物品名空白（尤其非英文客户端）
- 自定义物品是**纯服务端**的（服务器下发物品模型），不需要客户端 mod；但如果物品带新模型/动画，需要 bundle 机制（示例 12）——纯数值复制不需要
- 4.1 里 `MongoId` 被重构过，存量用法（`new MongoId("...")`）不受影响

## 来源

- `E-Mod开发示例/server-mod-examples/18CustomItemService/CustomItemServiceExample.cs`
- `E-Mod开发示例/server-mod-examples/18.1CustomItemServiceLootBox/CustomItemServiceLootBox.cs`
- `wiki/SPT_41/Server_40_to_41.md`（表注入、Preload、命名空间迁移）
