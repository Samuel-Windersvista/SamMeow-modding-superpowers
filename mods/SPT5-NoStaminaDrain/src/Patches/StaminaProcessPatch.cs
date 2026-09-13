using System;
using HarmonyLib;

namespace SPT5NoStaminaDrain.Patches;

/// <summary>
/// 目标：<c>Stamina.Process(float dt)</c> —— **逐帧**体力扣减的真正入口。
///
/// 依据（ISIL 反汇编，见 docs/eft-1.1.5-il2cpp-逆向可行性报告.md）：
/// <code>
///   141 Call Stamina.get_NormalValue
///   148 Move xmm6, [rbx+16]          ; xmm6 = Current
///   149 Multiply xmm9, xmm9, xmm11   ; consumption * dt
///   150 Multiply xmm9, xmm9, [rbx+64]
///   151 Subtract xmm6, xmm6, xmm9    ; Current -= amount
///   161 Move [rbx+16], xmm6          ; 写回 Current
/// </code>
/// （`Consume` 只在「添加消耗项」时一次性扣减；持续扣减由本方法每帧执行。）
///
/// 补丁：Prefix 记录扣减前的 `Current`，Postfix 若发现变小则还原
/// → **阻止净扣减，但保留自然回复**（爬升不受影响）。
/// </summary>
[HarmonyPatch(typeof(Stamina), "Process", new Type[] { typeof(float) })]
internal static class StaminaProcessPatch
{
    private static bool _logged;

    [HarmonyPrefix]
    private static void Prefix(Stamina __instance, out float __state)
    {
        __state = __instance.Current;
    }

    [HarmonyPostfix]
    private static void Postfix(Stamina __instance, float __state)
    {
        if (__instance.Current < __state)
        {
            __instance.Current = __state; // 还原，抵消本帧扣减

            if (!_logged)
            {
                _logged = true;
                Plugin.Logger.LogInfo("[SPT5-NoStaminaDrain] Process() 逐帧扣减已被拦截并还原（体力不再下降）");
            }
        }
    }
}
