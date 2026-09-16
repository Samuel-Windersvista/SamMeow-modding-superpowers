# Modding Standard Waivers — SPT5-AccurateCircularRadar

本文件供 `scripts/check-mod-standard.ps1` 读取（默认路径 `<mod>/MODDING-STD-WAIVER.md`）。
格式：`Waiver: STD-XXX-NNN: <reason>`。

Waiver: STD-CLI-003: 补丁刻意沿用上游与 SPT 客户端的惯用写法 SPTushonka.Reflection.Patching.ModulePatch（每类显式 GetTargetMethod，等价于 [HarmonyPatch(typeof(T), "method")] 的显式目标声明），而非 HarmonyPatch 注解 + PatchAll。移植任务书要求「补丁仅把 using 换成 SPTushonka.Reflection.Patching，其余不动」，且 ModulePatch 是 SPT 5.0 自带客户端 mod 的一致写法。规则意图（显式声明补丁目标、补丁集中放 Patches/、不用盲扫 PatchAll）已满足。

Waiver: STD-CLI-006: 客户端日志确实统一走 BepInEx 日志源（BasePlugin.Log / ManualLogSource，与 version-matrix 对 5.0 的规定一致）。机检正则要求源码中出现字面量 `Logger.Log*`，而本插件按上游命名使用静态字段 `Log`（`RadarPlugin.Log`，即 `internal static new ManualLogSource Log`），故启发式未命中。规则实质合规，仅标识符名不匹配。
