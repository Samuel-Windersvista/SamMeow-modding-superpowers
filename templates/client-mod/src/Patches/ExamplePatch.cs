using HarmonyLib;

namespace {{ROOT_NAMESPACE}}.Patches;

/// <summary>
/// 示例 Harmony 补丁：在目标方法执行前（Prefix）/后（Postfix）注入逻辑。
/// STD-CLI-003：每个补丁类用 [HarmonyPatch(typeof(目标类型), "方法名")] 标注，补丁类独立，
///              集中放在 Patches/ 目录，由入口统一 Harmony.PatchAll() 应用。
/// STD-CLI-004：补丁目标必须用目标版本程序集的真实类型名与命名空间；4.1 起已反混淆，
///              4.0 的混淆名（如 GClass680）不存在。
///
/// 替换：
///   {{TARGET_CLASS_NAME}}   -> 4.1 反混淆后的真实游戏类型，如 EFT.Player、EFT.GameWorld
///   {{TARGET_METHOD_NAME}}  -> 该类型上的方法名（字符串写法，运行时解析，不必编译期存在）
///
/// 4.1 客户端已反混淆：类型有真名真命名空间，先查
/// knowledge/spt-kb/curated/modding-guide/03-client-mod-anatomy.md 的映射说明，
/// 再用 dnSpy / ILSpy 打开 Assembly-CSharp.dll 确认目标方法签名。
/// </summary>
// STD-CLI-003 / STD-CLI-004
[HarmonyPatch(typeof({{TARGET_CLASS_NAME}}), "{{TARGET_METHOD_NAME}}")]
public class {{MOD_CLASS_NAME}}ExamplePatch
{
    /// <summary>原方法执行前调用；返回 false 时跳过原方法（截断）。</summary>
    [HarmonyPrefix]
    private static bool Prefix()
    {
        // 示例逻辑：插入你自己的代码（可加参数注入 __instance / __result，见 Harmony 文档）
        return true;
    }

    /// <summary>原方法执行后调用；可通过 __result 参数读取/改写返回值。</summary>
    [HarmonyPostfix]
    private static void Postfix()
    {
        // 示例逻辑：原方法执行完后的代码
    }
}
