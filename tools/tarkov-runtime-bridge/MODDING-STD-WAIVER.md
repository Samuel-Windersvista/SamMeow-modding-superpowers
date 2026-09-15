# Modding Standard 豁免记录 — tarkov-runtime-bridge

> 机检：`scripts/check-mod-standard.ps1 -ModPath tools/tarkov-runtime-bridge -TargetSptVersion 5.0.0`
> 豁免格式：`Waiver: STD-XXX-NNN: <reason>`（检查器解析，见脚本 `MODDING-STD-WAIVER.md` 约定）。

本插件是 tarkov-runtime-MCP Phase 2 的**只读工具桥**（客户端形态、SPT 5.0 / IL2CPP / BepInEx 6、
经 MO2 overlay 交付）。撤离事件检测引入了一处 Harmony postfix（`LocalGame.Stop`）——STD-CLI-003/007
条件已适用且满足（`[HarmonyPatch]` 注解 + `new Harmony`/`PatchAll`/`Unload()` 撤销）；其余读取路径不
patch 游戏逻辑。以下为仍存的豁免：

Waiver: STD-CLI-006: 5.0 形态日志经注入的 ManualLogSource（BasePlugin.Log）输出，无 Console（STD-LOG-001 PASS）；检查器 regex 匹配 4.1 的 `Logger.Log*` 变量名形态，5.0 命名不匹配，语义已满足。
