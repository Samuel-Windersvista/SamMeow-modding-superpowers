using BepInEx;

namespace FailClientModulePatch;

// STD-CLI-001：4.1.5（Mono / BepInEx 5）继承 BaseUnityPlugin
// STD-META-006：BepInPlugin 三参数齐备（GUID / 名称 / 版本）
[BepInPlugin("com.example.fail-client-modulepatch", "Fail ModulePatch Client Fixture", "1.0.0")]
public class FailModulePatchPlugin : BaseUnityPlugin
{
    private ModulePatch _patch = null!;

    private void Awake()
    {
        // STD-CFG-006：客户端配置经 Config.Bind 声明
        _ = new FailModulePatchConfiguration(Config);

        // STD-CLI-007：ModulePatch 显式 Enable（生态主导形态）
        _patch = new ExampleModulePatch();
        _patch.Enable();

        // STD-CLI-006：客户端日志用 BepInEx 日志源
        Logger.LogInfo("fail-client-modulepatch fixture loaded");
    }

    // 有意省略生命周期撤销声明：用于回归证明 ModulePatch / .Enable() 被识别为
    // Harmony 使用，STD-CLI-007 不再落到 N/A 分支（本夹具预期 FAIL）。
}
