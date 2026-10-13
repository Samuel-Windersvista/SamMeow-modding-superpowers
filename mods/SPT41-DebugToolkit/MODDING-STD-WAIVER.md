# MODDING-STD-WAIVER — SamMeow.DebugToolkit（SPT 4.1.6 线）

本文件登记对本工程检查器 FAIL 的有意豁免。格式：`Waiver: STD-XXX-NNN: <理由>`。

## 已登记豁免

Waiver: STD-CLI-003: 本工程统一 `SPT.Reflection.Patching.ModulePatch` 装配（14+ 补丁类，经共享 `PatchSet` 逐项 Enable/撤销），刻意不使用 `[HarmonyPatch(typeof(...))]` 特性；该规则要求特性形态，与本工程约定冲突。规则的设计意图（显式声明补丁目标、补丁类独立集中、不经 `PatchAll` 盲扫、生命周期可控）已满足：每个补丁类的 `GetTargetMethod()` 显式给出目标类型与方法、补丁类集中在各模块 `Patches/` 目录、装配与撤销由 `Plugin` 生命周期驱动。属有意偏离。
