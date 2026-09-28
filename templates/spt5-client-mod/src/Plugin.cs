using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using {{ROOT_NAMESPACE}}.Patches;

namespace {{ROOT_NAMESPACE}};

// STD-CLI-005：依赖其它 BepInEx 插件时用 [BepInDependency] 声明。
// 硬依赖（目标插件缺失/版本不满足则本插件不加载）：
// [BepInDependency("com.example.core-lib", "1.2.0")]
// 软依赖（目标缺失时本插件照常加载，运行时自行判断）：
// [BepInDependency("com.tyfon.uifixes", BepInDependency.DependencyFlags.SoftDependency)]

/// <summary>
/// BepInEx 插件入口（SPT 5.0 / EFT 1.1.5 / IL2CPP / BepInEx 6）。
/// STD-CLI-001：5.0 继承 BepInEx.Unity.IL2CPP.BasePlugin，入口为 Load()
///              （4.1.5 为 BaseUnityPlugin + Awake()）。
/// STD-CLI-002：GUID 用反向域名记法（com.&lt;author&gt;.&lt;mod&gt;），全局唯一。
/// STD-META-005：版本 semver 三段式，与 csproj &lt;Version&gt; 一致。
/// STD-META-006：BepInPlugin 三参数齐备（GUID、显示名、版本）。
/// STD-CLI-006：客户端日志用 BasePlugin.Log（ManualLogSource）；4.1.5 为 Logger。
/// STD-CLI-007：Load() 应用补丁，Unload() 撤销
///              （BepInEx 6 IL2CPP BasePlugin 无 IDisposable.Dispose()，撤销走 Unload()）。
/// 部署形态：DLL 放入 BepInEx/plugins/&lt;ModName&gt;/（建议子目录，避免与其它插件混淆；见 README）。
/// </summary>
// STD-CLI-001 / STD-CLI-002 / STD-META-005 / STD-META-006
[BepInPlugin("{{MOD_GUID}}", "{{MOD_NAME}}", "{{MOD_VERSION}}")]
public class {{MOD_CLASS_NAME}}Plugin : BasePlugin
{
    // STD-CLI-006：静态日志源（ManualLogSource），供补丁类与工具类复用；在 Load() 中赋值。
    // BasePlugin 自带实例属性 Log，这里用 new 隐藏为静态字段（CS0108 规避）。
    internal static new ManualLogSource Log;

    private Harmony _harmony;

    public override void Load()
    {
        Log = base.Log;

        // STD-CFG-006：客户端配置经 BasePlugin.Config（ConfigFile）的 Config.Bind 声明，
        // 落在 BepInEx/config/<ModGuid>.cfg；不要自建 JSON 配置读取。
        _ = new {{MOD_CLASS_NAME}}Configuration(Config);

        // 逐补丁类独立应用：一类失败不影响其它类（fail-open）。
        TryApplyHarmonyPatches();

        // STD-CLI-006 / STD-LOG-003：加载日志，用于 LogOutput.log 验证。
        Log.LogInfo($"{{MOD_NAME}} {{MOD_VERSION}} 已加载");
    }

    /// <summary>
    /// STD-CLI-007：BepInEx 6 IL2CPP 的撤销路径（对应 4.1.5 的 OnDestroy）。
    /// </summary>
    public override bool Unload()
    {
        if (_harmony != null)
        {
            try
            {
                _harmony.UnpatchSelf();
            }
            catch (System.Exception e)
            {
                Log.LogWarning($"Harmony 撤销失败：{e.Message}");
            }

            _harmony = null;
        }

        return true;
    }

    private void TryApplyHarmonyPatches()
    {
        try
        {
            _harmony = new Harmony("{{MOD_GUID}}");
        }
        catch (System.Exception e)
        {
            _harmony = null;
            Log.LogError($"Harmony 初始化失败，全部补丁禁用：{e.Message}");
            return;
        }

        // 每个补丁类一行：新增补丁类时在此登记（见 src/Patches/）。
        TryApplyPatch(typeof({{MOD_CLASS_NAME}}ExamplePatch), "{{MOD_CLASS_NAME}}ExamplePatch");
    }

    /// <summary>
    /// 应用单个补丁类；失败只记该类的 error，不影响其它补丁类。
    /// HarmonyX 的 PatchAll(Type) 等价于 CreateClassProcessor(type).Patch()
    /// （已由 runtime-bridge 用参考程序集 IL 确认），故按类调用即可实现逐类隔离。
    /// </summary>
    private void TryApplyPatch(System.Type patchType, string label)
    {
        try
        {
            _harmony.PatchAll(patchType);
        }
        catch (System.Exception e)
        {
            Log.LogError($"补丁 {label} 应用失败，对应功能禁用：{e.Message}");
        }
    }
}
