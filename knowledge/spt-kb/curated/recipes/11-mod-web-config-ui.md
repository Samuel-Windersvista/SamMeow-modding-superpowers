---
version: [4.1]
domain: both
topic: recipe
recipe_task: web-config-ui
source: curated
---
# 配方：Mod 配置界面（Mod Web Pages / 浏览器编辑器）[4.1]

> 状态：已提炼（源自 Mod_Web_Pages 官方文档 + 迁移文档 + 源码命名空间）
> 适用：[4.1] | 文档原文：`wiki/SPT_41/modding/server/Mod_Web_Pages.md`

## 目标

让玩家在 SPT 服务器自带的 Web 界面里直接改你的 mod 配置（免手改 JSON），并可加自定义页面/静态资源/API。

## 前置

- 项目改为 `Microsoft.NET.Sdk.Web` + `<OutputType>Library</OutputType>`，目标 `net10.0`
- 引用 `SPTarkov.Server.Core` 与 `SPTarkov.Server.Web`（均 4.1.0）
- 全部 opt-in：不实现 `IModBlazorMetadata` 就完全不受影响

## 步骤

### 1. 元数据加标记

```csharp
public sealed class MyModMetadata : IModMetadata, IModBlazorMetadata
{
    // ...IModMetadata 原有成员
    public string? WWWRootUrl { get; init; }                 // null = 用程序集名
    public string? HomePage { get; init; } = "/my-mod";      // 必须与 @page 路由一致
    public string? HomePageDescription { get; init; } = "Settings for My Mod";
}
```

### 2. 配置类（可编辑的 POCO）

```csharp
public class MyModConfig
{
    [JsonPropertyName("enabled")] public bool Enabled { get; set; } = true;
    [JsonPropertyName("spawnMultiplier")] public double SpawnMultiplier { get; set; } = 1.25;
    // 属性必须 get+set（apply 只拷可读写属性）；init/只读会被静默跳过
}
```

### 3. 加载并注册进 DI（**不要**加 [Injectable]）

`IOnDIConstruct` 中读文件 → `AddSingleton(config)`（见 api-notes config-system.md）

### 4. 注册到配置编辑器

```csharp
[Injectable(InjectionType.Singleton)]
public class MyModConfigEditorProvider(MyModConfig config) : IConfigEditorConfigProvider
{
    public IEnumerable<ConfigEditorConfigRegistration> GetConfigs()
    {
        yield return ConfigEditorConfigRegistration.Create(
            "com.example.my-mod",            // id（用 mod GUID）
            "My Mod Config",                 // 编辑器显示名
            config,                          // 配置实例
            Path.Combine("user", "mods", "MyMod", "config.json")  // 持久化路径（相对服务器）
        );
    }
}
```

### 5.（可选）自定义页面 / 静态资源 / API

- Blazor 页面：`@page "/my-mod"` 路由匹配 `HomePage`；路由加 mod 名前缀防撞
- `wwwroot/` 文件夹 → 服务在 `/<WWWRootUrl>/` 下；**两 mod 撞 URL = 启动硬失败**
- MVC Controller：注册自己的端点（游戏流量仍走 Router！）

## 验证

- 服务器 Web 界面 SIC 区出现你的 mod 卡片 → 进入配置页 → 改值 Apply → 游戏内生效；Save → 写回 JSON
- 检查 `user/mods/<guid>/config.json` 文件内容已更新
- 重启服务器确认配置从文件加载（不是默认值）

## 坑

- `@page` 路由与 `HomePage` 不一致 → 卡片点了没反应
- 忘拷 `wwwroot` 到输出目录 → 静态资源 404（构建后检查输出）
- apply 与 save 独立：用户可只 Apply 不 Save（内存改了文件没改）——不要假设两者同步
- 误给配置类加 `[Injectable]` → JSON 永不加载
- 高级场景（隐藏字段、自定义 load/save/apply）直接构造 `ConfigEditorConfigRegistration` 覆盖默认行为——注意钩子是**替换**而非并行

## 来源

- `wiki/SPT_41/modding/server/Mod_Web_Pages.md`（全文）
- `wiki/SPT_41/Server_40_to_41.md`（Web pages 节、IOnDIConstruct 节）
- `../api-notes-4.1/config-system.md`
