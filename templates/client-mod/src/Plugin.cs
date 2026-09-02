using BepInEx;
using HarmonyLib;

namespace {{ROOT_NAMESPACE}};

/// <summary>
/// BepInEx 插件入口。BepInPlugin 的三个参数：GUID（全局唯一）、显示名、版本（semver 三段式）。
/// 部署时整份 DLL 放入游戏目录的 BepInEx/plugins/。
/// </summary>
[BepInPlugin("{{MOD_GUID}}", "{{MOD_NAME}}", "{{MOD_VERSION}}")]
public class {{MOD_CLASS_NAME}}Plugin : BaseUnityPlugin
{
    private Harmony _harmony = null!;

    private void Awake()
    {
        // BepInEx 配置：Config 属性（ConfigFile）来自 BaseUnityPlugin，绑定到 BepInEx/config/<GUID>.cfg
        _ = new {{MOD_CLASS_NAME}}Configuration(Config);

        // 扫描本程序集内所有 [HarmonyPatch] 标记的类并应用（含 src/Patches/ExamplePatch.cs）
        _harmony = new Harmony("{{MOD_GUID}}");
        _harmony.PatchAll();

        Logger.LogInfo($"{{MOD_NAME}} v{{MOD_VERSION}} 已加载");
    }

    private void OnDestroy()
    {
        // 卸载时撤销全部补丁（可选，Unity 退出前不调用也不影响）
        _harmony?.UnpatchSelf();
    }
}
