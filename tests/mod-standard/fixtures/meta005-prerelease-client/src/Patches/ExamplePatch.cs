using HarmonyLib;

namespace PrereleaseClient.Patches;

[HarmonyPatch(typeof(GameWorld), nameof(GameWorld.RegisterPlayer))]
public static class ExamplePatch
{
    [HarmonyPostfix]
    private static void Postfix()
    {
    }
}
