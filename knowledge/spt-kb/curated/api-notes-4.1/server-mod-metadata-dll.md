---
version: [4.1]
domain: server
topic: mod-metadata
source: curated
---
# 4.1 Server Mod 元数据读取（DLL / IModMetadata）[4.1]

> 状态：已提炼 + 实战验证（2026-08-05，spt-mcp helper 实现）
> 用途：spt MCP server 的 `spt_list_mods` / `spt_read_mod_metadata` 识别真实 4.1 server mod 的依据

## 关键事实：4.1 server mod 没有 package.json

SPT 4.1 的 server mod 是**纯 DLL 结构**（`user/mods/<mod>/` 下顶层 DLL），元数据来自 DLL 程序集内的 **IModMetadata 实现**（C# record），**不存在 package.json**。

- ModLoader 扫描：`Directory.GetDirectories("./user/mods/")` -> 每目录顶层 `GetFiles()` 找 `.dll`（`ModLoader.cs`）
- 元数据：`modValidator.ValidateMods` 读 DLL 的 IModMetadata（AsmResolver 加载程序集）
- `package.json` 是 3.x/4.0 旧结构，4.1 已废弃

## IModMetadata 结构（DLL 内）

```csharp
// 任何实现 SPTarkov.Server.Core.Models.Spt.Mod.IModMetadata 的类型
public record XxxMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.xxx.yyy";   // 反向域名
    public string Name { get; init; } = "Xxx";
    public string Author { get; init; } = "Xxx";
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? ModDependencies { get; init; }
    ...
}
```

## 静态读取方法（无运行时加载，AsmResolver）

属性值是 **record 初始化器**，编译进无参构造器 IL（`ldstr` 常量），可用 AsmResolver 静态提取：

```
// 构造器 IL 模式：
ldarg.0
ldstr "com.xxx.yyy"                        <- ModGuid 值
stfld <ModGuid>k__BackingField
ldarg.0
ldstr "1.0.0"                              <- Version 原始串
ldc.i4.0
newobj SemanticVersioning.Version::.ctor   <- 包装成 Version
stfld <Version>k__BackingField
```

**提取规则：**
1. 找实现 `IModMetadata` 接口的类型（`t.Interfaces.Any(i => i.Interface.Name == "IModMetadata")`）
2. 取其**无参构造器**（`.ctor` 且参数 0 个）
3. 扫描 IL：`ldstr X; stfld <Field>k__BackingField` 模式 -> 直接属性值（ModGuid/Name/Author/License）
4. Version/SptVersion 特殊：`ldstr X; ldc.i4.*; newobj SemanticVersioning.X::.ctor; stfld` -> 取那条 ldstr
5. 无 IModMetadata 实现 -> 该 DLL 不是 server mod（可能是依赖库，跳过）

**示例输出**（真实 mod）：
```json
{"name":"Warsaw Pact Trader","guid":"com.sammeow.warsawtrader","version":"1.0.0","sptVersion":"~4.1.0"}
```

## 工具实现

- 完整实现：`tools/spt-mcp/helper/src/Program.cs`（.NET CLI `spt-metadata-reader`）
- 构建：`dotnet build tools/spt-mcp/helper -c Release`
- 调用：`spt-metadata-reader.exe <dll1> <dll2> ...` -> stdout JSON 数组
- spt-mcp 通过子进程同步调用（`SPT_MCP_HELPER` 环境变量定位 helper）

## 坑

1. **AsmResolver 版本 API 差异**：SPT_Runtime 自带的是旧版（`AsmResolver.PE.DotNet.Cil.CilOpCode`，无 `CilOpCodes` 常量类）——用 `op.ToString() == "ldstr"` 字符串比较，不用常量类
2. **typePriority 读不到**：DI TypePriority 在 `[Injectable]` 特性/构造代码里，非元数据——DLL 来源时默认 0
3. **ModDependencies 深解析复杂**（构造器逐条 Add）：当前返回空数组，依赖冲突走文件级 + BepInEx 层面
4. 一个 mod 目录可能有多个 DLL（主 mod + 依赖库）——只取**第一个**顶层 DLL 的 IModMetadata（无 IModMetadata 的 DLL 跳过）

## 来源

- 源码：`SPTarkov.Server/Modding/ModLoader.cs`（扫描逻辑）、`Libraries/SPTarkov.Server.Core/Models/Spt/Mod/`（IModMetadata）
- 实战：`tools/spt-mcp/helper/`（本仓库）、WarsawTrader/SkillsExtended/SptDbDump 三个真实 mod 验证
