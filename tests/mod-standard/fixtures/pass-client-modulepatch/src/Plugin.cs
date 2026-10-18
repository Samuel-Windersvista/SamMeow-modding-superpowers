using BepInEx;
using HarmonyLib;

namespace PassClientModulePatch;

// STD-CLI-001：4.1.5（Mono / BepInEx 5）继承 BaseUnityPlugin
// STD-META-006：BepInPlugin 三参数齐备（GUID / 名称 / 版本）
[BepInPlugin("com.example.pass-client-modulepatch", "Pass ModulePatch Client Fixture", "1.0.0")]
public class PassModulePatchPlugin : BaseUnityPlugin
{
    private Harmony _harmony = null!;
    private ModulePatch _patch = null!;

    private void Awake()
    {
        // STD-CFG-006：客户端配置经 Config.Bind 声明
        _ = new PassModulePatchConfiguration(Config);

        // STD-CLI-007：ModulePatch 显式 Enable（生态主导形态）
        _harmony = new Harmony("com.example.pass-client-modulepatch");
        _patch = new ExampleModulePatch();
        _patch.Enable();

        // STD-CLI-006：客户端日志用 BepInEx 日志源
        Logger.LogInfo("pass-client-modulepatch fixture loaded");
    }

    private void OnDestroy()
    {
        // STD-CLI-007：生命周期回调中撤销补丁。
        // 本夹具用 UnpatchSelf 撤销；ModulePatch 生态亦可用 Harmony.UnpatchID(patchType.Name)。
        _harmony?.UnpatchSelf();
    }
}
