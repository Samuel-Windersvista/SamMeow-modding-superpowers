using _DisableScavMode_egboggied.Patches;
using BepInEx;
using BepInEx.Configuration;

namespace _DisableScavMode_egboggied;

// PORT-NOTE: 原工程用 BepInEx.PluginInfoProps 包生成 PluginInfo.PLUGIN_NAME / PLUGIN_VERSION。
// 离线引用环境（Refs-410）不含该包，改为字面量，语义不变。
[BepInPlugin("com.egbog.disablescavmode.egboggied", "DisableScavMode-egboggied", "1.1.0")]
[BepInProcess("EscapeFromTarkov.exe")]
public class Plugin : BaseUnityPlugin {
    public static ConfigEntry<bool> InsuranceScreen { get; set; }
    public static ConfigEntry<bool> ScavMode { get; set; }

    private void Awake() {
        InitConfig();
        new ScavModePatch().Enable();
        new InsuranceScreenPatch().Enable();
    }

    private void InitConfig() {
        const string insuranceScreen = "Insurance Screen";
        const string scavMode = "Scav Mode";

        InsuranceScreen = Config.Bind(insuranceScreen, "Is Insurance screen enabled?", true);
        ScavMode        = Config.Bind(scavMode,        "Is Scav mode enabled?", true);
    }
}
