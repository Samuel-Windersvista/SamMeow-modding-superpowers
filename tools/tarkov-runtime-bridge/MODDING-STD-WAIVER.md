# Modding Standard 豁免记录 — tarkov-runtime-bridge

> 机检：`scripts/check-mod-standard.ps1 -ModPath tools/tarkov-runtime-bridge -TargetSptVersion 5.0.0`
> 豁免格式：`Waiver: STD-XXX-NNN: <reason>`（检查器解析，见脚本 `MODDING-STD-WAIVER.md` 约定）。

本插件是 tarkov-runtime-MCP Phase 2 的**只读工具桥**（客户端形态、SPT 5.0 / IL2CPP / BepInEx 6、
经 MO2 overlay 交付）。它不 patch 游戏逻辑，故以下客户端规则的条件不适用或与 5.0 形态不符：

Waiver: STD-CLI-003: 只读桥无 Harmony 补丁（不 patch 游戏逻辑），故无 [HarmonyPatch(typeof(...))] 注解；规则条件不适用。
Waiver: STD-CLI-006: 5.0 形态日志经注入的 ManualLogSource（BasePlugin.Log）输出，无 Console（STD-LOG-001 PASS）；检查器 regex 匹配 4.1 的 `Logger.Log*` 变量名形态，5.0 命名不匹配，语义已满足。
Waiver: STD-CLI-007: 无 Harmony 生命周期（无 `new Harmony` / `PatchAll`）；撤销路径为 BepInEx 6 `BasePlugin.Unload()`（停 HTTP 监听 + 销毁采样对象），对应 4.1 的 OnDestroy 语义。
