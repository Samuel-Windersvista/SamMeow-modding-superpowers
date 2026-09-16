---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 02 元数据与版本声明（META）

> **Domain slug:** `META` · **规则 ID 前缀:** `STD-META-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 02，2026-09-14）。

## 维度范围

- 服务端 `IModMetadata` 实现与必填字段
- 元数据文件命名与位置约定（`ModMetadata.cs`）
- GUID 反向域名命名（`com.author.name`）
- `SptVersion` tilde 区间写法
- 客户端 `[BepInPlugin]` 版本声明
- paired mod 两端版本号联动

## 规则

### STD-META-001 — 实现 `IModMetadata` 且每个 mod 目录恰好一个实现

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`modding-guide/02-server-mod-anatomy.md`（4.1 起 `IModMetadata` 为接口，`AbstractModMetadata` 已移除）、`api-notes-4.1/server-mod-metadata-dll.md`、`api-notes-5.0/mod-loading.md`（重复实现抛 `ModLoaderException`）；语料：近期 297 个目录的 8066 个 `.cs` 中 `ModMetadata` 出现 271 次，19 个服务端样例均实现该接口（`EV-CORPUS-MECH`、`EV-CORPUS-META`）。
- **Rule:** 服务端 mod 必须在程序集内实现 `IModMetadata`，覆盖全部属性（可选属性赋 `null`）；一个 mod 目录内只能存在一个实现。

```csharp
using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

public record MyModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.example.my-mod";
    public string Name { get; init; } = "My Mod";
    public string Author { get; init; } = "Me";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("1.0.0");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}
```

> 深入：[modding-guide/02-server-mod-anatomy.md](../modding-guide/02-server-mod-anatomy.md)、[api-notes-4.1/server-mod-metadata-dll.md](../api-notes-4.1/server-mod-metadata-dll.md)

### STD-META-002 — 将元数据实现放入 `ModMetadata.cs`

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/src/ModMetadata.cs`；语料：近期 297 个目录中文件名恰为 `ModMetadata.cs` 的有 63 个，其余散布为 `Metadata.cs`、`*Metadata.cs` 等，「文件名与位置不统一」是 `EV-CORPUS-TOP5` 列出的不规范点之一（`EV-CORPUS-CSPROJ`；`EV-CORPUS-META`）。
- **Rule:** 元数据实现应放在独立文件 `ModMetadata.cs` 中，置于 `src/` 或工程根，便于工具与维护者定位。

```text
src/ModMetadata.cs        # 元数据实现（IModMetadata）
src/ModEntry.cs           # 生命周期入口（IOnLoad）
```

> 深入：[evidence-index.md](evidence-index.md) `EV-CORPUS-TOP5`

### STD-META-003 — 用反向域名式全局唯一 `ModGuid`

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`api-notes-5.0/mod-loading.md`（`ModGuid` 为反域名风格，正则 `^[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*$`；重复 GUID 整组移除）；语料：客户端 `[BepInPlugin]` 约 155/183 使用 `com.*`，服务端样例多数为反向域名（`EV-CORPUS-META`）。
- **Rule:** `ModGuid` 必须使用至少两段的反向域名式命名（如 `com.<author>.<mod>`），全局唯一，且与其他已发布 mod 不重复。

```csharp
// 正确：反向域名，全局唯一
public string ModGuid { get; init; } = "com.sammeow.mymod";

// 避免：单段或易撞名
// public string ModGuid { get; init; } = "MyMod";
```

> 深入：[api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md)

### STD-META-004 — 用 tilde 范围声明 `SptVersion`

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/src/ModMetadata.cs`、`api-notes-4.1/mod-loading.md`（`new Range("~4.1.0")`）；语料：19 个服务端样例全部为 tilde 范围（`~4.1.0`、`~4.1.2`、`~4.1.3`、`~4.1`），未见 `^4.1` 或精确版本（`EV-CORPUS-META`）。
- **Rule:** `SptVersion` 必须使用 tilde 范围（`~<major>.<minor>.<patch>`），把兼容性限定在同一 minor 版本内，避免把未验证的次版本纳入。

```csharp
public Range SptVersion { get; init; } = new("~4.1.0"); // >=4.1.0 且 <4.2.0
```

> 深入：[api-notes-4.1/mod-loading.md](../api-notes-4.1/mod-loading.md)

### STD-META-005 — 用三段式 semver 声明 `Version`

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`api-notes-4.1/mod-loading.md`（`new Version("1.0.0")` 合法，四段式 `"1.0.0.0"` 非法）、`templates/server-mod/src/ModMetadata.cs`；语料：本次普查 509 个 csproj 中 291 个含显式 `<Version>`，其中 214 个三段式、仅 5 个四段式，72 个为 MSBuild 属性插值（如 `$(AssemblyVersion)`）（EV-CORPUS-CSPROJ）。
- **Rule:** `Version`（以及 csproj 的 `<Version>`）必须以三段式核心 `major.minor.patch` 声明，允许 semver 预发布/构建后缀（如 `1.3.4-spt5.1`、`1.0.0+build.5`），不得使用四段式（如 `1.0.0.0`）或非 semver 字符串。

```csharp
public Version Version { get; init; } = new("1.0.0"); // 三段式；"1.0.0.0" 非法
```

> 深入：[api-notes-4.1/mod-loading.md](../api-notes-4.1/mod-loading.md)

### STD-META-006 — 用 `[BepInPlugin]` 声明客户端插件 GUID、名称与版本

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`modding-guide/03-client-mod-anatomy.md`、`templates/client-mod/src/Plugin.cs`；语料：近期目录中 `[BepInPlugin]` 出现 251 次、183 个客户端入口采样（`EV-CORPUS-MECH`、`EV-CORPUS-META`）。
- **Rule:** 客户端插件入口类必须标注 `[BepInPlugin(guid, name, version)]`（GUID、名称、版本三者齐备）；GUID 必须全局唯一；推荐反向域名记法（格式约定见 `STD-CLI-002`）；版本为三段式 semver。

```csharp
[BepInPlugin("com.example.my-mod", "My Mod", "1.0.0")]
public class MyModPlugin : BaseUnityPlugin { /* ... */ }
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)
> 交叉引用：`STD-CLI-002`（客户端 GUID 格式约定）。

### STD-META-007 — 让 paired mod 两端共用同一版本号

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`external/spt-archive/wiki/Mod_Types.md:40-41`（未规定版本联动）；语料：`Tushonka-Territories_2942_source` 客户端与服务端 csproj 均为 `<Version>1.3.4</Version>`，`SPT-Casino_2994_source` 用单一 `$version` 同时打包两端（`EV-GAP-PAIRED`、`EV-MECH-COORD`）。
- **Rule:** paired mod 的客户端与服务端程序集应使用同一版本号（可由 MSBuild 属性集中定义），避免玩家看到两端版本不一致。

```xml
<!-- Directory.Build.props（仓库根） -->
<Project>
  <PropertyGroup>
    <Version>1.3.4</Version>
  </PropertyGroup>
</Project>
```

> 深入：[evidence-index.md](evidence-index.md) `EV-GAP-PAIRED`
