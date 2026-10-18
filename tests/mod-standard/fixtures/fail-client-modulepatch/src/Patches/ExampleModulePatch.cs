using System.Reflection;
using HarmonyLib;

namespace FailClientModulePatch.Patches;

// STD-CLI-003：Harmony 补丁用 [HarmonyPatch] 标注；
// STD-CLI-007：补丁类以 ModulePatch 表达，由入口显式 Enable() 应用
[HarmonyPatch(typeof(GameWorld), nameof(GameWorld.RegisterPlayer))]
public class ExampleModulePatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
        => AccessTools.Method(typeof(GameWorld), nameof(GameWorld.RegisterPlayer));

    [HarmonyPostfix]
    private static void Postfix()
    {
    }
}
