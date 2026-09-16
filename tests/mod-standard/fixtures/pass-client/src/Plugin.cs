using BepInEx;
using HarmonyLib;

namespace PassClient;

// STD-CLI-001：4.1.5（Mono / BepInEx 5）继承 BaseUnityPlugin
// STD-META-006：BepInPlugin 三参数齐备（GUID / 名称 / 版本）
[BepInPlugin("com.example.pass-client", "Pass Client Fixture", "1.0.0")]
public class PassClientPlugin : BaseUnityPlugin
{
    private Harmony _harmony = null!;

    private void Awake()
    {
        // STD-CFG-006：客户端配置经 Config.Bind 声明
        _ = new PassClientConfiguration(Config);

        // STD-CLI-007：入口创建 Harmony 实例并 PatchAll
        _harmony = new Harmony("com.example.pass-client");
        _harmony.PatchAll();

        // STD-CLI-006：客户端日志用 BepInEx 日志源
        Logger.LogInfo("pass-client fixture loaded");
    }

    private void OnDestroy()
    {
        // STD-CLI-007：生命周期回调中撤销补丁
        _harmony?.UnpatchSelf();
    }
}
