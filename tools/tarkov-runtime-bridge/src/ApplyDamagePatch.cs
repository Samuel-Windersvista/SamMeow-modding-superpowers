using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using HarmonyLib;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 受伤事件采集：prefix / postfix patch
/// <c>ActiveHealthController.ApplyDamage(EBodyPart bodyPart, float damage, DamageInfo damageInfo)</c>。
///
/// spike 结论（live 验收，2026-09-15）：<c>IHealthController.ApplyDamageEvent</c> 无法经
/// <c>DelegateSupport.ConvertDelegate&lt;Il2CppSystem.Action&lt;EBodyPart, float, DamageInfo&gt;&gt;</c> 订阅 ——
/// <c>DamageInfo</c> 为非 blittable struct，Il2CppInterop 拒绝封送（日志：
/// "Delegate has parameter of type EFT.Ballistics.DamageInfo (non-blittable struct) which is not supported"），
/// 异常被订阅 try 吞掉并连带跳过同块的 <c>DiedEvent</c> 订阅。故受伤改走 Harmony：
/// <c>ApplyDamage</c> 为 non-virtual public，生态 6+ mod（Deminvincibility、Miyako-Carry-Service）均补丁它，
/// 且无子类覆盖。prefix 记录击杀归属（先于伤害结算），postfix 输出受伤事件（伤害已落地）。
///
/// STD-CLI-003：显式 <c>[HarmonyPatch(typeof(...))]</c> 注解（含参数类型重载，精确锁定无重载方法）。
/// STD-CLI-007：Harmony 生命周期在 <see cref="Plugin"/>（new Harmony + 逐类 Patch + Unload 撤销）。
/// 补丁体只读参数并转发到 <see cref="RaidEventCollector"/>，不修改游戏逻辑。
/// </summary>
[HarmonyPatch(typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage),
    new[] { typeof(EBodyPart), typeof(float), typeof(DamageInfo) })]
internal static class ApplyDamagePatch
{
    [HarmonyPrefix]
    internal static void Prefix(ActiveHealthController __instance, float damage, DamageInfo damageInfo)
    {
        RaidEventCollector.NotifyDamageIncoming(__instance, damage, damageInfo);
    }

    [HarmonyPostfix]
    internal static void Postfix(ActiveHealthController __instance, EBodyPart bodyPart, float damage, DamageInfo damageInfo)
    {
        RaidEventCollector.NotifyDamageApplied(__instance, bodyPart, damage, damageInfo);
    }
}
