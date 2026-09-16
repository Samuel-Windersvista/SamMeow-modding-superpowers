using BepInEx;
using HarmonyLib;

namespace FourPartClient;

[BepInPlugin("com.example.fourpart-client", "Four Part Client Fixture", "1.3.4.5")]
public class FourPartClientPlugin : BaseUnityPlugin
{
    private Harmony _harmony = null!;

    private void Awake()
    {
        _ = new FourPartClientConfiguration(Config);

        _harmony = new Harmony("com.example.fourpart-client");
        _harmony.PatchAll();

        Logger.LogInfo("meta005-fourpart-client fixture loaded");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
