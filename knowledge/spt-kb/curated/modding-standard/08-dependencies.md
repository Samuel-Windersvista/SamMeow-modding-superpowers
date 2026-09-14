---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 08 依赖管理（DEP）

> **Domain slug:** `DEP` · **规则 ID 前缀:** `STD-DEP-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 04，2026-09-14）。

## 维度范围

- `ModDependencies` 硬语义（key=ModGuid、SemVer Range、失败即整批拒载）
- 服务端无软依赖机制（条件逻辑在 `IOnLoad` 自判）
- 客户端 `[BepInDependency]` soft/hard 策略

## 规则

### STD-DEP-001 — 用 ModDependencies 声明服务端硬依赖（key=ModGuid，value=SemVer Range）

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`Libraries/SPTarkov.Server.Core/Models/Spt/Mod/IModMetadata.cs:91`（key=依赖 ModGuid、value=`Range`）、`SPTarkov.Server/Modding/ModValidator.cs:243-287`（EV-GAP-DEP）；语料：0 真实声明（EV-NOCORPUS #1；机制推断，无语料先例）
- **Rule:** 服务端 mod 若硬依赖另一个 mod，必须在 `IModMetadata.ModDependencies` 中以 `["<依赖 ModGuid>"] = new Range("<SemVer 范围>")` 声明；key 是依赖 mod 的 `ModGuid`（不是程序集名、不是 NuGet 包名）。

```csharp
using SemanticVersioning;

public Dictionary<string, Range>? ModDependencies { get; init; } = new()
{
    ["com.example.required-mod"] = new Range("~1.0.0"),
    ["com.example.another-dep"] = new Range(">=2.0.0 <3.0.0"),
};
```

> 深入：[api-notes-4.1/mod-loading.md](../api-notes-4.1/mod-loading.md)

### STD-DEP-002 — 无硬依赖时保持 ModDependencies 为空，不声明可选依赖

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`SPTarkov.Server/Modding/ModValidator.cs:243-287`（任一依赖缺失或版本不满足即 `errorsFound = true`，`ValidateMods` 返回空列表整批拒载）；语料：157 个 `.cs` 文件全部空初始化、392 个 `_source` 目录中 28 个 `= new()`、0 个真实键值（EV-GAP-DEP、EV-CORPUS-META）
- **Rule:** 没有硬依赖时，`ModDependencies` 保持 `null` 或空字典；不要把"可选 / 软依赖"写进 `ModDependencies`——服务端没有软依赖语义，声明即硬约束，依赖缺失会导致全部 mod 拒载。

```csharp
// 无依赖：null 或空字典，二者皆可
public Dictionary<string, Range>? ModDependencies { get; init; }
```

> 深入：[api-notes-4.1/mod-loading.md](../api-notes-4.1/mod-loading.md)

### STD-DEP-003 — 需要可选依赖语义时在 IOnLoad 中自判并降级

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`SPTarkov.Server/Modding/ModValidator.cs:243-287`（服务端仅支持硬依赖，无软依赖机制）、EV-GAP-DEP；语料：无对应计数（机制推断，无语料先例）
- **Rule:** 若功能依赖另一个 mod 但缺失时仍应可运行，不要用 `ModDependencies` 表达；改为在 `IOnLoad` 中检测依赖是否存在（如其程序集是否已加载），缺失时跳过该功能并记日志。

```csharp
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class MyMod(ISptLogger<MyMod> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // 可选依赖：检测其程序集是否随 mod 一同部署（具体判定方式视集成对象而定）
        bool optionalPresent = AppDomain.CurrentDomain.GetAssemblies()
            .Any(a => a.GetName().Name == "OptionalMod.Server");

        if (!optionalPresent)
        {
            logger.Info("可选依赖 OptionalMod 未安装，跳过集成");
            return Task.CompletedTask;
        }

        // 依赖存在：启用集成
        return Task.CompletedTask;
    }
}
```

> 深入：[api-notes-4.1/di-container.md](../api-notes-4.1/di-container.md)

### STD-DEP-004 — 客户端必需依赖用 [BepInDependency] 声明

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`EV-GAP-DEP`（`[BepInDependency]` 默认无 `SoftDependency` 标志即硬依赖）；语料：`[BepInDependency]` 170 处（EV-CORPUS-MECH）
- **Rule:** 客户端 mod 若要求另一 BepInEx 插件存在才能工作，必须在插件类上用 `[BepInDependency("<GUID>", "<最低版本>")]`（默认硬依赖）声明；缺失时 BepInEx 拒绝加载本插件。

```csharp
[BepInPlugin("com.example.my-mod", "My Mod", "1.0.0")]
[BepInDependency("com.SPT.core", "4.1.0")]   // 硬依赖：缺失即不加载
public class MyPlugin : BaseUnityPlugin { }
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)

### STD-DEP-005 — 客户端可选依赖用 SoftDependency 标志并判空降级

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`EV-GAP-DEP`（`BepInDependency.DependencyFlags.SoftDependency`）；语料：1 例（`ReceiveAllChats_2785_source/ReceiveAllChats.Client/Plugin.cs:12`，EV-MECH-COORD）
- **Rule:** 可选依赖用 `[BepInDependency("<GUID>", BepInDependency.DependencyFlags.SoftDependency)]` 声明，使插件在依赖缺失时仍能加载；使用该依赖前必须判空 / 判存在并降级，不得假设其一定存在。

```csharp
[BepInPlugin("com.example.my-mod", "My Mod", "1.0.0")]
[BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]
public class MyPlugin : BaseUnityPlugin
{
    // 使用软依赖前先判空 / 判存在，缺失时跳过相关功能
}
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)
