using HarmonyLib;

namespace {{ROOT_NAMESPACE}}.Client.Patches;

/// <summary>
/// 示例 Harmony 补丁：在目标方法执行前（Prefix）/后（Postfix）注入逻辑。
/// STD-CLI-003：每个补丁类用 [HarmonyPatch(typeof(目标类型), "方法名")] 标注，补丁类独立，
///              集中放在 Patches/ 目录，由入口 TryApplyPatch 逐类应用。
/// STD-CLI-004：补丁目标必须用目标版本程序集的真实类型名与命名空间。
///              5.0（IL2CPP）的游戏程序集是 BepInEx/interop/Assembly-CSharp.dll
///              （Il2CppInterop 代理），用 ilspycmd 反编译确认类型名与方法签名，例如：
///                ilspycmd -l c "&lt;SPT5Path&gt;\BepInEx\interop\Assembly-CSharp.dll"
///              类型存在 + 能编译 ≠ 能调用（见 README「坑」）。
///
/// 替换：
///   {{TARGET_CLASS_NAME}}   -> interop 里的真实游戏类型，如 EFT.Player、EFT.GameWorld
///   {{TARGET_METHOD_NAME}}  -> 该类型上的方法名（字符串写法，运行时解析，不必编译期存在）
///
/// 补丁体纪律：异常穿透补丁进入游戏代码会闪退，补丁体必须自带 try/catch（fail-open）。
/// 本模板在入口处做的是「逐类应用隔离」；运行期异常仍需补丁体自行兜底。
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
