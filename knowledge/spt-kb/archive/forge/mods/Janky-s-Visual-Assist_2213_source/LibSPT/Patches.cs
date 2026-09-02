using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.UI;
using SPT.Reflection.Patching;
using UnityEngine;

namespace VisualAssist;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class GrenadeThrowPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player.BaseGrenadeHandsController).GetMethod(nameof(Player.BaseGrenadeHandsController.method_9));
    }

    [PatchPrefix]
    public static bool Prefix(
        Player.BaseGrenadeHandsController __instance, Player ____player, Transform ___transform_1, bool lowThrow, float timeSinceSafetyLevelRemoved,
        GrenadePrefab ___grenadePrefab_0
    )
    {
        var grenadeArc = Singleton<GrenadeArc>.Instance;

        if (grenadeArc is null
            || ____player is null
            || !____player.IsYourPlayer
            || !Plugin.GrenadeArcEnabled.Value)
            return true;

        __instance.vmethod_2(
            timeSinceSafetyLevelRemoved, grenadeArc.GrenadeThrow.ThrowPosition, ___transform_1.rotation, grenadeArc.GrenadeThrow.ThrowForce, lowThrow
        );
        
        return false;
    }
}

public class GrenadeAssistGameWorldStartedPostfixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
    }

    [PatchPostfix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(GameWorld __instance)
    {
        var grenadeArc = Singleton<GrenadeArc>.Instance = __instance.MainPlayer.gameObject.AddComponent<GrenadeArc>();
        grenadeArc.localPlayer = __instance.MainPlayer;
        Plugin.Log.LogInfo("Grenade Assist Initialized");
    }
}

public class GrenadeAssistPlayerDisposePrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.Dispose));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        if (!__instance.IsYourPlayer)
            return;

        var grenadeArc = __instance.gameObject.GetComponent<GrenadeArc>();
        if (grenadeArc == null) return;

        Singleton<GrenadeArc>.Release(grenadeArc);
        Object.DestroyImmediate(grenadeArc);
        Plugin.Log.LogInfo("Player.Dispose: GrenadeArc destroyed successfully");
    }
}

public class GrenadeAssistPlayerOnDeadPrefixPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return typeof(Player).GetMethod(nameof(Player.OnDead));
    }

    [PatchPrefix]
    // ReSharper disable once InconsistentNaming
    public static void Postfix(Player __instance)
    {
        if (!__instance.IsYourPlayer)
            return;

        var grenadeArc = __instance.gameObject.GetComponent<GrenadeArc>();
        if (grenadeArc == null) return;

        Singleton<GrenadeArc>.Release(grenadeArc);
        Object.DestroyImmediate(grenadeArc);
        Plugin.Log.LogInfo("Player.Dispose: GrenadeArc destroyed successfully");
    }
}