---
version: [4.1]
domain: server
topic: mod-loading
source: curated
---
# Mod 加载流程笔记 [4.1]

> 状态：**已核实（源码实读 2026-08-02）** | 适用：[4.1]
> 源码：`SPTarkov.Server/Modding/ModLoader.cs`、`ModValidator.cs`

## 加载器位置与路径

- 加载器不在 Core，在 `SPTarkov.Server/Modding/ModLoader.cs`
- 常量：`ModPath = "./user/mods/"`、`PatcherPath = "./user/patchers/"`、`PatchedAssemblyName = "./SPTarkov.Server.Core.Patched.dll"`

## 加载时序（源码确认）

```
RunModLoader()
├─ LoadPrepatchesForPrepatchPass()   # 删旧 Patched.dll/.pdb → 遍历 user/patchers/ 读 enum prepatch
├─ 若有 prepatch → ApplyPrepatchesInMemory() → BootPatchedServerInMemory()（新进程跑 patched Core）
│   └─ 宿主进程内再跑 RunModLoader，此时 isHostedPatchedProcess=true，跳过 prepatch 阶段
└─ LoadMods()                        # 遍历 user/mods/ 下每个目录（应含一个 DLL）
    └─ modValidator.ValidateMods()   # 校验版本兼容/依赖/冲突
```

关键点：**prepatch 是在内存中应用后以新进程引导 patched 服务端**；patched 程序集每次启动重建、启动时删除——与 EnumExtensions 文档一致。

## 元数据校验（IModMetadata 源码注释确认）

- 每个 mod 必须恰好一个 `IModMetadata` 实现；所有属性必须实现，可选属性赋 null
- 版本：SemanticVersioning（`new Version("1.0.0")` 合法；**`"1.0.0.0"` 四段式非法**）
- SptVersion 用 Range：`new Range("~4.1.0")`
- 加载顺序：**先 TypePriority，同优先级按 ModGuid 字母序（tiebreaker）**
- `HasPrepatcher`：含 enum prepatch 定义时置 true，定义在 `user/patchers/<ModGuid>/`

## 失败模式（源码/文档确认）

- metadata 类型未映射配置 → 启动失败
- 注入类型无法解析 → 启动失败
- `IProfileMigration` 异常 → 存档无法加载时抛 `InvalidOperationException`

## 已核实坐标

- `SPTarkov.Server/Modding/ModLoader.cs` — 加载全流程
- `SPTarkov.Server/Modding/ModValidator.cs` — 版本/依赖校验
- `Libraries/SPTarkov.Server.Core/Models/Spt/Mod/IModMetadata.cs` — 元数据接口（含 tiebreaker 注释）
- `Libraries/SPTarkov.Server.Core/DI/OnLoadOrder.cs` — 阶段常量（0/100000/…/1000000）
