---
version: [4.1]
domain: server
topic: config
source: curated
---
# 配置系统笔记 [4.1]

> 状态：已提炼（源自迁移文档 + Mod_Web_Pages 文档；ConfigServer 内部待源码核对）
> 适用：[4.1] | 源码：`E:\云文件\GitHub\SamMeow_SPT410_source_code`

## 现状（已核实 2026-08-02）

- `ConfigServer` / `configServer.GetConfig<T>()` **已移除**（迁移文档 + 源码无此类）
- 加载机制：`Core/Loaders/ConfigLoader.cs`（static）扫描 `SPT_Data/configs/*.json|.jsonc` → 反序列化为 `IReadOnlyDictionary<Type, BaseConfig>`（**类型为 key**）→ 注册为 DI 单例 → mod 按具体类型注入
- `BaseConfig` = abstract record，`Kind` 属性标识配置种类（如 `spt-loot`、`spt-trader`）
- 反序列化支持：JSONC 注释、MongoId/枚举/ListOrT 等自定义转换器（`Converters` 列表）
- DEBUG 构建下未知字段会报错（`JsonUnmappedMemberHandling.Disallow`）
- 类型 ↔ 配置文件映射缺失 → 启动失败（类型不在字典中时注入解析失败）

## 配置键与类型示例（源码实读）

| Kind | 类型 | 文件（SPT_Data/configs/） |
|------|------|--------------------------|
| `spt-loot` | `LootConfig` | loot.json（推断） |
| `spt-trader` | `TraderConfig` | trader.json（推断） |
| `spt-bot` | `BotConfig` | bot.json（推断） |
| （其余以 `Kind` 值反查） | `*Config` | 每配置一个文件 |

> 文件具体名以 `SPT_Data/configs/` 目录实况为准，上表文件名仅推断。

## mod 自己的配置

推荐链路（4.1 标准做法）：

1. **定义**：纯 POCO，属性全 get/set（apply 只拷贝可读可写属性），`[JsonPropertyName]` 稳定文件名
2. **加载 + 注册**：`IOnDIConstruct` 中读文件 → `serviceCollection.AddSingleton(config)`；不要加 `[Injectable]`
3. **消费**：任何 mod 类构造函数注入 `MyModConfig`
4. **（可选）网页编辑**：`IConfigEditorConfigProvider` 返回 `ConfigEditorConfigRegistration.Create(guid, 显示名, config实例, 相对路径)`；编辑器按 config 形状自动生成 UI

## ConfigEditorConfigRegistration 细节

- `Create` 覆盖常见场景；直接构造可自定义 load/save/apply 钩子（**替换**默认行为，不是并行）
- apply = 拷值到内存实例；save = 写文件。两者独立，UI 可只做其一
- 路径相对服务器文件夹

## 源码坐标（已核实 2026-08-02）

- `Libraries/SPTarkov.Server.Core/Loaders/ConfigLoader.cs` — 配置加载（SPT_Data/configs → Dictionary<Type, BaseConfig>）
- `Libraries/SPTarkov.Server.Core/Models/Spt/Config/` — 各配置类（LootConfig/TraderConfig/BotConfig/InventoryConfig/QuestConfig 等）
- `Libraries/SPTarkov.Server.Web/Services/` — `IConfigEditorConfigProvider` 收集逻辑（待精确定位文件）
- 配置编辑写回：`ConfigEditorConfigRegistration`（`SPTarkov.Server.Web` 模型，Mod_Web_Pages 文档）
