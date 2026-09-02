using BepInEx;
using BepInEx.Logging;

namespace MunitionsExpert
{
    // BepInEx shell: 3.11 AKI used to auto-call MunitionsExpert.Main();
    // 4.1 BepInEx needs a [BepInPlugin] entry point.
    [BepInPlugin("com.munitions.expert", "Munitions Expert", "1.2.1.0")]
    public class MunitionsExpertPlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Logger.LogInfo("Munitions Expert loading");
            MunitionsExpert.Main();
            Logger.LogInfo("Munitions Expert loaded");
        }
    }
}
