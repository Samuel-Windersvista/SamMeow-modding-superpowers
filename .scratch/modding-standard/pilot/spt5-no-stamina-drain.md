# 试点合规报告：SPT5-NoStaminaDrain

> 试点对象：`mods/SPT5-NoStaminaDrain/`（类型：client；目标 SPT 版本：5.0）
> 规则版本：Modding Standard 84 条（commit 635a1c38）
> 报告日期：2026-09-14

## 摘要

- 判定统计：PASS 19 / FAIL 7 / N-A 58（合计 84）
- 关键缺口 Top 3（按严重度）
  1. 构建产物入库：提交 `bin/`、`obj/` 且根目录无 `.gitignore`（STD-STRUCT-001、STD-STRUCT-002，均为 MUST）。
  2. 无授权声明：根目录无 `LICENSE`（STD-STRUCT-006、STD-PKG-006，SHOULD）。
  3. 补丁健壮性与可控性：无 fail-open、无独立开关、补丁未撤销（STD-PERF-004、STD-CFG-006、STD-CLI-007，SHOULD）。
- 规则修订建议数：8
- 判定口径说明：本 mod 为 5.0 IL2CPP 客户端形态；规则集中不存在 `Applies: 4.1.5` 的条目，全部为 `both` 或 `5.0`。对「文本仅描述 4.1 Mono 形态、但 `Applies: both`」的规则，按 5.0 正确形态判定为 PASS 并在证据栏注明版本差异，同时在「规则修订建议」登记（不硬判 FAIL）。

## 逐规则核对

| Rule ID | 判定 | 证据（文件:行）与说明 | 修复建议 |
|---------|------|----------------------|----------|
| STD-STRUCT-001 | FAIL | `mods/SPT5-NoStaminaDrain/` 根目录无 `.gitignore`（`Test-Path` 为 False）。 | 新增 `.gitignore`，至少排除 `bin/`、`obj/`、`*.user`、`.vs/`、`.idea/`。 |
| STD-STRUCT-002 | FAIL | 仓库内存在 `bin/Release/SPT5NoStaminaDrain.dll`、`bin/Release/*.deps.json` 与 `obj/**`（含 `obj/Release/SPT5NoStaminaDrain.dll`）。 | `git rm -r --cached bin obj` 后纳入 `.gitignore`；构建产物仅保留本地或 CI 输出。 |
| STD-STRUCT-003 | PASS | 源码位于 `src/Plugin.cs`、`src/Patches/StaminaConsumePatch.cs`、`src/Patches/StaminaProcessPatch.cs`，工程文件留根目录。 | 无。 |
| STD-STRUCT-004 | N-A | 非 paired：`mods/SPT5-NoStaminaDrain/` 仅含客户端插件，无 `Client/`+`Server/` 分层需求。 | 无。 |
| STD-STRUCT-005 | PASS | 根目录 `README.md`（111 行）含用途、反编译依据、构建、安装、验证步骤。 | 无。 |
| STD-STRUCT-006 | FAIL | 根目录无 `LICENSE`/`LICENSE.md`（`Test-Path` 为 False）。 | 增加 `LICENSE`（如 MIT），明确再分发授权。 |
| STD-STRUCT-007 | N-A | 根目录无 `.editorconfig`；MAY 级为可选实践，未采用不构成违规。 | 可选：补 `.editorconfig` 统一风格。 |
| STD-META-001 | N-A | 客户端 mod，无服务端程序集，`src/` 内无 `IModMetadata` 实现。 | 无。 |
| STD-META-002 | N-A | 无服务端元数据文件（无 `ModMetadata.cs`）。 | 无。 |
| STD-META-003 | PASS | `src/Plugin.cs:13` GUID `com.sammeow.spt5.nostaminadrain`，反向域名、多段、全局唯一。 | 无。 |
| STD-META-004 | N-A | 无服务端元数据，不存在 `SptVersion` 字段。 | 无。 |
| STD-META-005 | PASS | `SPT5NoStaminaDrain.csproj:16` `<Version>1.0.0</Version>`；`src/Plugin.cs:13` 版本 `1.0.0`，均为三段式 semver。 | 无。 |
| STD-META-006 | PASS | `src/Plugin.cs:13` `[BepInPlugin("com.sammeow.spt5.nostaminadrain", "SPT5 No Stamina Drain", "1.0.0")]`，GUID/名称/版本三要素齐备。 | 无。 |
| STD-META-007 | N-A | 非 paired，无两端版本联动。 | 无。 |
| STD-BUILD-001 | N-A | 无服务端工程，无 `net10.0` 目标。 | 无。 |
| STD-BUILD-002 | PASS | `SPT5NoStaminaDrain.csproj:9` `<TargetFramework>net6.0</TargetFramework>`。规则文本面向 4.1 Mono（禁止 .NET Core/5+），5.0 IL2CPP + BepInEx 6 客户端必须 `net6.0`（`README.md:63-71`）；规则意图（使用目标运行时兼容框架）满足，版本差异见修订建议 #1。 | 无（mod 对 5.0 正确）；规则需按版本拆分。 |
| STD-BUILD-003 | PASS | `SPT5NoStaminaDrain.csproj:23-48` 全部运行时程序集经 `<HintPath>$(SPT5Path)\...` 引用并设 `<Private>false</Private>`（BepInEx.Core、BepInEx.Unity.IL2CPP、Il2CppInterop.Runtime、0Harmony、Assembly-CSharp、Il2Cppmscorlib）。 | 无。 |
| STD-BUILD-004 | N-A | 无 `SPTarkov.Server.*` 引用（客户端 mod）。 | 无。 |
| STD-BUILD-005 | PASS | `SPT5NoStaminaDrain.csproj:17` `<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>`。 | 无。 |
| STD-BUILD-006 | PASS | `SPT5NoStaminaDrain.csproj:19-20` `SPT5Path` 带 `Condition="'$(SPT5Path)' == ''"`，支持 `-p:SPT5Path=...` 覆盖，未无条件硬编码。属性名与规则建议的 `SPTInstallPath` 不同，见修订建议 #7。 | 无（语义满足）；建议统一属性名。 |
| STD-SRV-001 | N-A | 客户端 BepInEx 插件，无服务端 DI 组件。 | 无。 |
| STD-SRV-002 | N-A | 无 `[Injectable]`/`TypePriority`（客户端 mod）。 | 无。 |
| STD-SRV-003 | N-A | 无 `IOnLoad` 生命周期。 | 无。 |
| STD-SRV-004 | N-A | 无 `IOnUpdate`。 | 无。 |
| STD-SRV-005 | N-A | 无 `StaticRouter`/`DynamicRouter`。 | 无。 |
| STD-SRV-006 | N-A | 无路由 action。 | 无。 |
| STD-SRV-007 | N-A | 无 Router/Callbacks 分层。 | 无。 |
| STD-SRV-008 | N-A | 无服务端 `ISptLogger<T>` 使用。 | 无。 |
| STD-CLI-001 | PASS | `src/Plugin.cs:13-14` `[BepInPlugin]` + `BasePlugin`；`:18-25` `Load()` 内完成 Harmony 初始化与日志。5.0 IL2CPP 入口基类为 `BasePlugin`、入口为 `Load()`（`README.md:68-69`），规则文本的 `BaseUnityPlugin`/`Awake()` 为 4.1 形态；意图满足，见修订建议 #3。 | 无（mod 对 5.0 正确）。 |
| STD-CLI-002 | PASS | `src/Plugin.cs:13` GUID 反向域名（`com.sammeow.spt5.nostaminadrain`）。 | 无。 |
| STD-CLI-003 | PASS | `StaminaConsumePatch.cs:18`、`StaminaProcessPatch.cs:23` 均以 `[HarmonyPatch]` 标注；补丁类独立并集中置于 `src/Patches/`；`src/Plugin.cs:23` 统一 `PatchAll()`。 | 无。 |
| STD-CLI-004 | PASS | 目标为 5.0/EFT 1.1.5 真实类型 `Stamina` 与 `Physical.Consumption`（`README.md:11-24`，依据 `docs/eft-1.1.5-类名映射重建报告.md`），未使用旧版混淆名。 | 无。 |
| STD-CLI-005 | N-A | 不依赖其它 BepInEx 插件（仅引用 BepInEx/Harmony 运行时库），规则前提不成立。 | 无。 |
| STD-CLI-006 | PASS | `src/Plugin.cs:20,25`、`StaminaConsumePatch.cs:29`、`StaminaProcessPatch.cs:44` 均经 BepInEx `ManualLogSource` 输出（`Log` / `Plugin.Logger`）。5.0 IL2CPP `BasePlugin` 暴露 `Log` 而非 `Logger`（`README.md:69`）；意图满足，见修订建议 #4。 | 无（mod 对 5.0 正确）。 |
| STD-CLI-007 | FAIL | `src/Plugin.cs:22-23` 在 `Load()` 中 `new Harmony(...)` 为局部变量并 `PatchAll()`；全 mod 无 `UnpatchSelf()` 或 `Dispose()` 撤销路径。 | 将 `Harmony` 保存为字段并重写 `Dispose()` 调用 `UnpatchSelf()`（5.0 IL2CPP 对应 4.1 的 `OnDestroy`）；见修订建议 #5。 |
| STD-CFG-001 | N-A | 无服务端配置，无 `ModHelper.GetAbsolutePathToModFolder` 路径拼接。 | 无。 |
| STD-CFG-002 | N-A | 无 `config/config.jsonc` 服务端配置。 | 无。 |
| STD-CFG-003 | N-A | 无 `IOnDIConstruct`/`AddSingleton`。 | 无。 |
| STD-CFG-004 | N-A | 无服务端配置类。 | 无。 |
| STD-CFG-005 | N-A | 无 `defaultConfig.jsonc`。 | 无。 |
| STD-CFG-006 | FAIL | 全 mod 无 `Config.Bind`、无任何客户端配置项（`src/Plugin.cs` 无配置绑定），补丁无法独立开关。 | 用 `Config.Bind` 提供至少 `General/Enabled` 开关并接入补丁判定（与 STD-PERF-004 联动）。 |
| STD-LOG-001 | N-A | 无服务端 `ISptLogger<T>` 使用。 | 无。 |
| STD-LOG-002 | N-A | 规则面向服务端 `ISptLogger` 级别选择；本 mod 无服务端日志（客户端级别由 STD-LOG-003 覆盖）。 | 无。 |
| STD-LOG-003 | PASS | `src/Plugin.cs:20,25`、`StaminaConsumePatch.cs:29`、`StaminaProcessPatch.cs:44` 均用 BepInEx 日志，未引用 `ISptLogger<T>`。 | 无。 |
| STD-LOG-004 | N-A | 全 mod 无 `catch` 块，规则前提（存在捕获）不成立；异常保护缺失另见 STD-PERF-004。 | 无。 |
| STD-LOG-005 | N-A | 无异步/`CancellationToken` 逻辑。 | 无。 |
| STD-DEP-001 | N-A | 无服务端，无 `ModDependencies` 声明。 | 无。 |
| STD-DEP-002 | N-A | 无服务端元数据，不涉及 `ModDependencies` 空值约定。 | 无。 |
| STD-DEP-003 | N-A | 无服务端可选依赖自判场景。 | 无。 |
| STD-DEP-004 | N-A | 无对其它 BepInEx 插件的必需依赖，规则前提不成立。 | 无。 |
| STD-DEP-005 | N-A | 无可选插件依赖。 | 无。 |
| STD-PKG-001 | N-A | 仓库内无发布归档（无 zip/dist），无法核对归档顶层布局。 | 无。 |
| STD-PKG-002 | N-A | 未创建 MO2 overlay，无 `meta.ini`。 | 无。 |
| STD-PKG-003 | N-A | 非 paired，无两端单归档需求。 | 无。 |
| STD-PKG-004 | N-A | 无 `SPT_Runtime/user/mods/` 服务端目录。 | 无。 |
| STD-PKG-005 | N-A | 非 paired。 | 无。 |
| STD-PKG-006 | FAIL | `README.md` 存在；根目录无 `LICENSE`/`LICENSE.md`（`Test-Path` 为 False）。 | 增加 `LICENSE`（授权条款），与 STD-STRUCT-006 同一修复。 |
| STD-PKG-007 | N-A | 单一来源，无多来源合并与旧目录迁移。 | 无。 |
| STD-VERIFY-001 | PASS | `README.md:5,73-78` 声明 Release 构建通过（0 错误 0 警告）；`bin/Release/SPT5NoStaminaDrain.dll` 实际存在。 | 无。 |
| STD-VERIFY-002 | N-A | 未安装、未纳入 MO2 批次（`README.md:80`「需你手动执行」）；受「不得写入游戏安装目录」硬规则限制。 | 无（待人工安装后执行）。 |
| STD-VERIFY-003 | N-A | 客户端 mod，无服务端加载行可断言。 | 无。 |
| STD-VERIFY-004 | N-A | 未安装，未执行 `BepInEx/LogOutput.log` 加载断言；`README.md:92-95` 仅列预期日志文本。 | 无（待人工安装后执行）。 |
| STD-VERIFY-005 | N-A | 客户端插件不出现在 `tarkov_server_status` 服务端 mod 清单；且未执行结构化验证。 | 无。 |
| STD-VERIFY-006 | N-A | 未安装，无 `SPT_Runtime/user/mods` 目录可扫描。 | 无（待安装后执行）。 |
| STD-VERIFY-007 | N-A | 未执行验证，无失败信号可移交。 | 无。 |
| STD-VERIFY-008 | N-A | 未执行实机验证，不涉及 profile/save 边界。 | 无。 |
| STD-VERIFY-009 | N-A | MAY 可选深验，未执行；Level B 亦未进行。 | 无。 |
| STD-VER-002 | N-A | 规则面向服务端骨架（`IModMetadata`/`[Injectable]`/`IOnLoad`/路由/配置注入），本 mod 无服务端组件。 | 无。 |
| STD-VER-003 | N-A | MAY 可选；未使用 `SeasonTable`/`ShopTable` 等 5.0 新扩展点。 | 无。 |
| STD-VER-004 | PASS | 面向 5.0/EFT 1.1.5 按新类型名重绑：目标 `Stamina`/`Physical.Consumption`（`README.md:11-24,63-71`，依据 `docs/eft-1.1.5-类名映射重建报告.md`），未套用 4.1 混淆名。 | 无。 |
| STD-BND-001 | N-A | 无 `bundles/` 与 `bundles.json`。 | 无。 |
| STD-BND-002 | N-A | 无 `bundles.json` manifest。 | 无。 |
| STD-BND-003 | N-A | 无客户端私有 bundle（无 `.bundle` 文件）。 | 无。 |
| STD-BND-004 | N-A | 无 bundle 脚本绑定。 | 无。 |
| STD-BND-005 | N-A | 无 bundle。 | 无。 |
| STD-BND-006 | N-A | 无数据库覆盖（无 `db/`）。 | 无。 |
| STD-PERF-001 | PASS | 补丁仅读写 `Stamina.Current`，无射线/物理查询（`StaminaProcessPatch.cs:29-46`）。 | 无。 |
| STD-PERF-002 | PASS | 每帧 `Stamina.Process` 补丁为 O(1)（记录并可能还原一个 float），无 AI 子系统遍历（`StaminaProcessPatch.cs:29-46`）。 | 无。 |
| STD-PERF-003 | N-A | 无寻路/路径计算。 | 无。 |
| STD-PERF-004 | FAIL | 用 Prefix/Postfix 满足第一分句；但补丁无 try/catch（非 fail-open，异常将阻断原逻辑，`StaminaConsumePatch.cs:24-33`、`StaminaProcessPatch.cs:29-46`），且无独立开关（无配置项）。 | 每个 patch 包 try/catch 并在异常时放行原逻辑；配合 STD-CFG-006 提供独立开关。 |
| STD-PERF-005 | N-A | 无服务端，无请求/启动热路径。 | 无。 |
| STD-PERF-006 | N-A | MAY 可选；无高频查找/排序/反射路径。 | 无。 |
| STD-PERF-007 | N-A | 无外部输入路径拼接。 | 无。 |
| STD-PERF-008 | N-A | 无外部输入校验场景。 | 无。 |

## 规则修订建议（反向校准）

| # | 涉及规则 | 问题类型 | 建议修订 | 理由 |
|---|----------|----------|----------|------|
| 1 | STD-BUILD-002 | 与机制矛盾 | 按版本拆分：`Applies: 4.1.5` → Mono 兼容框架（`netstandard2.1`/`net472`，禁 `net10.0`）；`Applies: 5.0` → IL2CPP/BepInEx 6 客户端必须 `net6.0`。 | 规则标 `Applies: both` 却只描述 4.1 Mono 并禁止 .NET Core/5+；5.0 IL2CPP 客户端必须 `net6.0`（`README.md:63-71`），按原文判定会把正确的 5.0 mod 误判违规。 |
| 2 | STD-BUILD-003 | 与机制矛盾 | 补 5.0 引用路径：IL2CPP 客户端引用 `BepInEx/interop/Assembly-CSharp.dll`（Il2CppInterop 代理）与 `BepInEx/core/*.dll`，而非 `EscapeFromTarkov_Data/Managed/Assembly-CSharp.dll`。 | 规则样例仅给 4.1 Mono 路径，`Applies: both` 未覆盖 5.0 的 interop 代理程序集形态。 |
| 3 | STD-CLI-001 | 表述不清 | 按版本补入口形态：4.1 → `BaseUnityPlugin` + `Awake()`；5.0 IL2CPP → `BepInEx.Unity.IL2CPP.BasePlugin` + `Load()`。 | 规则标 `Applies: both` 但只写 4.1 形态；本 mod 的 `BasePlugin`/`Load()`（`src/Plugin.cs:13-25`）对 5.0 正确，按原文判定会产生假 FAIL。 |
| 4 | STD-CLI-006 | 表述不清 | 按版本补日志属性：4.1 → `BaseUnityPlugin.Logger`；5.0 IL2CPP → `BasePlugin.Log`（`ManualLogSource`）。 | 同上；本 mod 使用 `Log`/`Plugin.Logger`（`src/Plugin.cs:20,25`）是 5.0 正确写法。 |
| 5 | STD-CLI-007 | 与机制矛盾 | 按版本写撤销时机：4.1 → `Awake` 应用、`OnDestroy` 撤销；5.0 IL2CPP → `Load()` 应用、`Dispose()` 撤销。 | 5.0 IL2CPP 无 `Awake`/`OnDestroy`，规则文字对 5.0 不可直接遵守；本 mod 未实现撤销（FAIL 成立），但修复目标须写 `Dispose()`。 |
| 6 | STD-META-001/-002/-004/-006/-007、STD-CFG-001…-006、STD-DEP-001…-005 | 分级不当 | 规则条目增加 `Domain: server / client / both` 字段（现仅文件 frontmatter 有 `domain`）。 | `Applies` 只表达 SPT 版本，不表达服务端/客户端；`02-metadata.md`、`06-config.md`、`08-dependencies.md` 等 `domain: both` 文件混装 server-only 与 client-only 规则，合规报告只能逐条人工判 N-A，检查器也无法机械判定适用性。 |
| 7 | STD-BUILD-006 | 表述不清 | 明确「属性名可自定，但须提供可覆盖的安装根路径属性」；或规定统一属性名（含 5.0 客户端）。 | 规则固定 `SPTInstallPath`，本 mod 用 `SPT5Path`（`csproj:19-20`）语义等价但名称不符，按字面判定会产生命名假 FAIL。 |
| 8 | STD-VERIFY-002…-009 | 表述不清 | 注明这些规则面向 modpack 批次/整合包验证流程，而非单个 mod 的源码合规；单 mod 合规报告默认 N-A。 | 规则文字（MO2 冒烟、批次 mod 清单断言、profile 边界）以「批次」为前提，单 mod 且未安装时无法满足，需靠人工判 N-A。 |

## 已知豁免记录（如有）

无。本 mod 的 7 项 FAIL 均为应修复项，未走 `Waiver: STD-XXX-nnn` 豁免流程；其中 STD-STRUCT-001/-002、STD-STRUCT-006、STD-PKG-006 属仓库卫生类，STD-CFG-006、STD-PERF-004、STD-CLI-007 属补丁健壮性类，均有直接修复方案。
