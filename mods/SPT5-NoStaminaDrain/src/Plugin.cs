using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;

namespace SPT5NoStaminaDrain;

/// <summary>
/// SPT 5.0 (EFT 1.1.5 / IL2CPP / BepInEx 6) 插件入口。
/// 与 4.1 的差别：基类是 BepInEx.Unity.IL2CPP.BasePlugin（不是 BaseUnityPlugin），
/// 入口方法是 Load()（不是 Awake()）。
/// </summary>
[BepInPlugin("com.sammeow.spt5.nostaminadrain", "SPT5 No Stamina Drain", "1.0.0")]
public class Plugin : BasePlugin
{
    internal static ManualLogSource Logger;

    public override void Load()
    {
        Logger = Log;

        var harmony = new Harmony("com.sammeow.spt5.nostaminadrain");
        harmony.PatchAll();

        Log.LogInfo("[SPT5-NoStaminaDrain] 已加载：Stamina.Consume 已被补丁为返回 0（体力不再消耗）");
    }
}
