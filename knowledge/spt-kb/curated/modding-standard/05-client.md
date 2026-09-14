---
version: [4.1, 5.0]
domain: client
topic: modding-standard
source: curated
---

# 05 客户端机制（CLI）

> **Domain slug:** `CLI` · **规则 ID 前缀:** `STD-CLI-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 03，2026-09-14）。

## 维度范围

- 入口形态：4.1.5 `BaseUnityPlugin` + `Awake`；5.0 `BasePlugin` + `Load()`（均标 `[BepInPlugin]`）
- Harmony patch 组织与目标选择
- `[BepInDependency]` 声明
- 客户端日志

## 规则

### STD-CLI-001 — 客户端入口类继承版本对应的插件基类并标 `[BepInPlugin]`

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)（客户端 mod = BepInEx 插件 DLL，装进 `BepInEx/plugins/`）、模板 `templates/client-mod/src/Plugin.cs`；5.0 机制：`mods/SPT5-NoStaminaDrain/src/Plugin.cs:9-19`（IL2CPP 入口基类 `BepInEx.Unity.IL2CPP.BasePlugin`、入口方法 `Load()`）；语料：`BaseUnityPlugin` 275、`[BepInPlugin]` 251（EV-CORPUS-MECH）
- **Rule:** 客户端 mod 入口类继承版本对应的插件基类并标 `[BepInPlugin(guid, name, version)]`，在入口方法中完成配置绑定、Harmony 初始化与日志；DLL 部署到 `BepInEx/plugins/`。入口形态按版本分支：
  - 4.1.5（Mono / BepInEx 5）：继承 `BaseUnityPlugin`，入口为 `Awake()`；
  - 5.0（IL2CPP / BepInEx 6）：继承 `BepInEx.Unity.IL2CPP.BasePlugin`，入口为 `public override void Load()`。

```csharp
// 4.1.5（Mono / BepInEx 5）
using BepInEx;
using HarmonyLib;

namespace MyMod;

[BepInPlugin("com.author.mymod", "My Mod", "1.0.0")]
public class MyModPlugin : BaseUnityPlugin
{
    private Harmony _harmony = null!;

    private void Awake()
    {
        // 配置绑定到 BepInEx/config/<GUID>.cfg
        _ = new MyModConfiguration(Config);

        _harmony = new Harmony("com.author.mymod");
        _harmony.PatchAll();

        Logger.LogInfo("My Mod v1.0.0 已加载");
    }

    private void OnDestroy() => _harmony?.UnpatchSelf();
}
```

```csharp
// 5.0（IL2CPP / BepInEx 6）：基类与入口方法均不同
using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace MyMod;

[BepInPlugin("com.author.mymod", "My Mod", "1.0.0")]
public class MyModPlugin : BasePlugin
{
    public override void Load()
    {
        var harmony = new Harmony("com.author.mymod");
        harmony.PatchAll();

        Log.LogInfo("My Mod v1.0.0 已加载");
    }
}
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)

### STD-CLI-002 — 客户端插件 GUID 用反向域名记法

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：[api-notes-4.1/mod-loading.md](../api-notes-4.1/mod-loading.md)（`ModGuid` 正则 `^[a-zA-Z0-9-]+(\.[a-zA-Z0-9-]+)*$`）、模板 `templates/client-mod/src/Plugin.cs`（与服务端共用 `{{MOD_GUID}}`）；语料：客户端 `[BepInPlugin]` 中约 155/183 用 `com.*`（EV-CORPUS-META）
- **Rule:** 客户端插件 GUID 采用反向域名记法（`com.<author>.<mod>`）以保证全局唯一、可读且避免撞名；`[BepInPlugin]` 的声明要求与唯一性约束见 `STD-META-006`。

```csharp
// 推荐：反向域名，paired mod 两端同值
[BepInPlugin("com.author.mymod", "My Mod", "1.0.0")]

// 反例：易撞名的短名
// [BepInPlugin("mymod", "My Mod", "1.0.0")]
```

> 深入：[evidence-index.md](evidence-index.md)（EV-CORPUS-META）
> 交叉引用：`STD-META-006`（[BepInPlugin] 声明与唯一性）。

### STD-CLI-003 — Harmony 补丁用 `[HarmonyPatch]` 标注

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)、模板 `templates/client-mod/src/Patches/ExamplePatch.cs`（`PatchAll` 扫描程序集内 `[HarmonyPatch]` 类）；语料：顶层 `Patches` 目录出现 61 次（EV-CORPUS-STRUCT）
- **Rule:** 每个 Harmony 补丁类用 `[HarmonyPatch(typeof(<目标类型>), "<方法名>")]` 标注，补丁类独立，由入口统一 `Harmony.PatchAll()` 应用；建议把补丁类集中放在 `Patches/` 目录便于审计。

```csharp
using HarmonyLib;

namespace MyMod.Patches;

[HarmonyPatch(typeof(EFT.Player), "SomeMethod")]
public class MyModExamplePatch
{
    /// <summary>原方法执行前调用；返回 false 时跳过原方法。</summary>
    [HarmonyPrefix]
    private static bool Prefix() => true;

    /// <summary>原方法执行后调用；可通过 __result 读取/改写返回值。</summary>
    [HarmonyPostfix]
    private static void Postfix() { }
}
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)

### STD-CLI-004 — Harmony 目标使用目标版本程序集的真实类型名

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)（4.1 客户端已反混淆，4.0 混淆名不存在；4.0 构建的客户端 mod 在 4.1 无法加载）、[migration/client-obfuscation-mapping-skills-extended.md](../migration/client-obfuscation-mapping-skills-extended.md)（成员签名匹配法）；语料：无（机制推断，无语料先例，登记 EV-NOCORPUS）
- **Rule:** 补丁目标必须用目标 SPT 版本程序集的真实类型名与命名空间；旧版本混淆名（如 `GClass680`）在 4.1 起不存在，需查官方映射表或用 dnSpy / ILSpy 反编译确认目标签名。

```csharp
// 4.1 反混淆后：真实类型 + 命名空间
[HarmonyPatch(typeof(EFT.Player), "SomeMethod")]

// 反例：4.0 混淆名，4.1 已不存在
// [HarmonyPatch(typeof(GClass680), "method_12")]
```

> 深入：[migration/client-obfuscation-mapping-skills-extended.md](../migration/client-obfuscation-mapping-skills-extended.md)

### STD-CLI-005 — 用 `[BepInDependency]` 声明对其它 BepInEx 插件的依赖

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：[evidence-index.md](evidence-index.md) 的 EV-GAP-DEP（客户端软依赖示例 `[BepInDependency(..., SoftDependency)]`）、模板 `templates/client-mod/src/Plugin.cs`；语料：`[BepInDependency]` 170（EV-CORPUS-MECH，EV-CORPUS-META 记录 soft/hard 混用）
- **Rule:** 客户端 mod 依赖其它 BepInEx 插件时用 `[BepInDependency("<GUID>")]` 声明；硬依赖为默认行为，可选依赖加 `BepInDependency.DependencyFlags.SoftDependency`。

```csharp
using BepInEx;

// 硬依赖：目标插件必须存在且版本满足，否则本插件不加载
[BepInDependency("com.example.core-lib", "1.2.0")]

// 软依赖：目标插件缺失时本插件照常加载，运行时自行判断
[BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]
[BepInPlugin("com.author.mymod", "My Mod", "1.0.0")]
public class MyModPlugin : BaseUnityPlugin { }
```

> 深入：[evidence-index.md](evidence-index.md)（EV-GAP-DEP）
> 交叉引用：`STD-DEP-004`、`STD-DEP-005`（依赖声明策略）。

### STD-CLI-006 — 客户端日志使用 BepInEx 日志源（`Logger` / `Log`）

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：模板 `templates/client-mod/src/Plugin.cs`（`Logger.LogInfo`）、[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)；5.0 机制：`mods/SPT5-NoStaminaDrain/src/Plugin.cs:20,25`（IL2CPP `BasePlugin.Log`，类型 `ManualLogSource`）；语料：`Logger.` 4968（EV-CORPUS-MECH）
- **Rule:** 客户端日志通过 BepInEx 日志源输出（`LogInfo` / `LogWarning` / `LogError`），统一进入 BepInEx 日志。属性名按版本分支：
  - 4.1.5（Mono / BepInEx 5）：`BaseUnityPlugin.Logger`；
  - 5.0（IL2CPP / BepInEx 6）：`BasePlugin.Log`（`ManualLogSource`）。

```csharp
// 4.1.5（Mono / BepInEx 5）
Logger.LogInfo("My Mod 已加载");
Logger.LogWarning("配置值超出预期范围，使用默认值");
Logger.LogError("目标方法签名不匹配，补丁未生效");

// 5.0（IL2CPP / BepInEx 6）：属性名为 Log（ManualLogSource），方法名不变
Log.LogInfo("My Mod 已加载");
Log.LogWarning("配置值超出预期范围，使用默认值");
Log.LogError("目标方法签名不匹配，补丁未生效");
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)
> 交叉引用：`STD-LOG-003`（客户端日志约定）。

### STD-CLI-007 — 在入口方法中应用补丁，在生命周期回调中撤销

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：模板 `templates/client-mod/src/Plugin.cs`（`Awake` 内 `PatchAll`、`OnDestroy` 内 `UnpatchSelf`）；5.0 机制：`mods/SPT5-NoStaminaDrain/src/Plugin.cs:18-23`（`Load` 内 `PatchAll`，对应撤销路径为 `Dispose()`）；语料：无（机制推断，无语料先例，登记 EV-NOCORPUS；EV-CORPUS-MECH 计 `BaseUnityPlugin` 275，未覆盖生命周期用法）
- **Rule:** 在入口方法中创建 `Harmony` 实例并 `PatchAll()`，在对应生命周期回调中调用 `UnpatchSelf()`，避免热重载或退出时残留补丁。时机按版本分支：
  - 4.1.5（Mono / BepInEx 5）：`Awake` 应用、`OnDestroy` 撤销；
  - 5.0（IL2CPP / BepInEx 6）：`Load()` 应用、`Dispose()` 撤销。

```csharp
// 4.1.5（Mono / BepInEx 5）
private void Awake()
{
    _harmony = new Harmony("com.author.mymod");
    _harmony.PatchAll();
}

private void OnDestroy() => _harmony?.UnpatchSelf();
```

```csharp
// 5.0（IL2CPP / BepInEx 6）：Load 应用、Dispose 撤销
public override void Load()
{
    _harmony = new Harmony("com.author.mymod");
    _harmony.PatchAll();
}

public override void Dispose() => _harmony?.UnpatchSelf();
```

> 深入：[modding-guide/03-client-mod-anatomy.md](../modding-guide/03-client-mod-anatomy.md)
