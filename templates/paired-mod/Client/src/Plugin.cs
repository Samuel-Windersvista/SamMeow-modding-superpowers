using BepInEx;
using HarmonyLib;
using {{ROOT_NAMESPACE}}.Shared;

namespace {{ROOT_NAMESPACE}}.Client;

// STD-CLI-005：依赖其它 BepInEx 插件时用 [BepInDependency] 声明。
// 硬依赖（目标插件缺失/版本不满足则本插件不加载）：
// [BepInDependency("com.example.core-lib", "1.2.0")]
// 软依赖（目标缺失时本插件照常加载，运行时自行判断）：
// [BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]

/// <summary>
/// 客户端半的 BepInEx 插件入口。
/// STD-CLI-001：4.1.5（Mono / BepInEx 5）继承 BaseUnityPlugin，入口为 Awake()；
///              5.0（IL2CPP / BepInEx 6）改为继承 BepInEx.Unity.IL2CPP.BasePlugin、入口 Load()（见 STD-BUILD-002 / STD-CLI-001）。
/// STD-META-006：BepInPlugin 三参数齐备（GUID、显示名、版本）。
/// STD-META-007 / STD-PKG-005：版本取自集中定义的 ModVersion.Value（来源 Directory.Build.props 的 $(Version)），
///                              与 Server 半共用同一版本，不在此另写版本字符串。
/// STD-CLI-002：GUID 用反向域名记法（com.&lt;author&gt;.&lt;mod&gt;），与服务端半共用同一个 {{MOD_GUID}}。
/// 部署时整份 DLL（连同 Shared.dll）放入游戏目录的 BepInEx/plugins/&lt;ModName&gt;/。
/// </summary>
// STD-CLI-001 / STD-CLI-002 / STD-META-006 / STD-META-007
[BepInPlugin("{{MOD_GUID}}", "{{MOD_NAME}}", ModVersion.Value)]
public class {{MOD_CLASS_NAME}}ClientPlugin : BaseUnityPlugin
{
    private Harmony _harmony = null!;

    private void Awake()
    {
        // STD-CFG-006：客户端配置经 BaseUnityPlugin.Config（ConfigFile）的 Config.Bind 声明，
        // 落在 BepInEx/config/<ModGuid>.cfg；不要自建 JSON 配置读取。
        _ = new {{MOD_CLASS_NAME}}Configuration(Config);

        // STD-CLI-007：在入口方法中创建 Harmony 实例并 PatchAll()
        // （扫描本程序集内所有 [HarmonyPatch] 标记的类，含 src/Patches/ExamplePatch.cs）。
        _harmony = new Harmony("{{MOD_GUID}}");
        _harmony.PatchAll();

        // STD-CLI-006 / STD-LOG-003：客户端日志用 BepInEx 日志源（4.1.5 为 Logger，5.0 为 Log）。
        // SharedConstants 来自 Shared 工程，验证客户端半确实引用了共享常量（STD-STRUCT-004）。
        Logger.LogInfo($"{{MOD_NAME}} v{ModVersion.Value} 已加载（协议版本 {SharedConstants.ProtocolVersion}）");
    }

    private void OnDestroy()
    {
        // STD-CLI-007：在对应生命周期回调中 UnpatchSelf()，避免热重载/退出时残留补丁
        // （5.0 对应撤销路径为 Dispose()）。
        _harmony?.UnpatchSelf();
    }
}
