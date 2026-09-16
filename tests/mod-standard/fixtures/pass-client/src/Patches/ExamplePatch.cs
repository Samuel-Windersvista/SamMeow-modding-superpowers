using HarmonyLib;

namespace PassClient.Patches;

// STD-CLI-003：Harmony 补丁用 [HarmonyPatch] 标注，补丁类集中在 Patches/
[HarmonyPatch(typeof(GameWorld), nameof(GameWorld.RegisterPlayer))]
public static class ExamplePatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
    }
}
