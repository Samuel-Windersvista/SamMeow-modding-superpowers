using BepInEx;
using HarmonyLib;

namespace PrereleaseClient;

[BepInPlugin("com.example.prerelease-client", "Prerelease Client Fixture", "1.3.4-spt5.1")]
public class PrereleaseClientPlugin : BaseUnityPlugin
{
    private Harmony _harmony = null!;

    private void Awake()
    {
        _ = new PrereleaseClientConfiguration(Config);

        _harmony = new Harmony("com.example.prerelease-client");
        _harmony.PatchAll();

        Logger.LogInfo("meta005-prerelease-client fixture loaded");
    }

    private void OnDestroy()
    {
        _harmony?.UnpatchSelf();
    }
}
