using System;
using HarmonyLib;

namespace SPT5NoStaminaDrain.Patches;

/// <summary>
/// 目标：<c>Stamina.Consume(Physical.Consumption, bool)</c>
///
/// 依据（反编译实证，见 docs/eft-1.1.5-il2cpp-逆向可行性报告.md 与 ISIL dump）：
/// <code>
///   amount = consumptionValue * this.ConsumptionMultiplier(偏移 0x3C) * this.Multiplier(偏移 0x40)
///   this.Current(偏移 0x10) -= amount
/// </code>
/// 即：<c>Consume</c> 是体力扣减的唯一入口。
///
/// 补丁：Prefix 直接把返回值置 0 并跳过原方法 → <c>Current</c> 永不减少 → 体力无限。
/// </summary>
[HarmonyPatch(typeof(Stamina), "Consume", new Type[] { typeof(Physical.Consumption), typeof(bool) })]
internal static class StaminaConsumePatch
{
    private static bool _logged;

    [HarmonyPrefix]
    private static bool Prefix(ref float __result)
    {
        if (!_logged)
        {
            _logged = true;
            Plugin.Logger.LogInfo("[SPT5-NoStaminaDrain] Stamina.Consume() 已拦截（首次触发，体力扣减被置 0）");
        }

        __result = 0f;
        return false; // 跳过原方法
    }
}
