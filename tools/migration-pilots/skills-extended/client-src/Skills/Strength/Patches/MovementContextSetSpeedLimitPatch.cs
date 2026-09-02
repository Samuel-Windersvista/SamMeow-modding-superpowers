using System.Reflection;
using EFT;
using EFT.Interactive;
using HarmonyLib;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;

namespace SkillsExtended.Skills.Strength.Patches;

/// <summary>
/// 4.1 迁移版：力量技能 - 灌木丛速度限制 patch。
/// 3.11 -> 4.1 适配：
///   MovementContext.method_0(float,string) -> SetPhysicalCondition 相关（patch 目标改为 OnPhysicalConditionChanged）
///   Struct303/method_28 -> SetPhysicalCondition(EPhysicalCondition, bool)
///   逻辑保持：精英力量允许在灌木丛跳跃/冲刺
/// </summary>
public class MovementContextSetSpeedLimitPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        // 4.1: MovementContext.SetPhysicalCondition 是物理条件设置入口
        return AccessTools.Method(typeof(MovementContext), "SetPhysicalCondition");
    }

    [PatchPostfix]
    public static void Postfix(MovementContext __instance, EPhysicalCondition ___physicalCondition, bool ___value)
    {
        var skillData = SkillsPlugin.SkillData;
        var skillMgrExt = SkillManagerExt.Instance(EPlayerSide.Usec);

        if (!skillData.Strength.Enabled) return;

        // 检查是否在灌木丛障碍物中（4.1 通过 PhysicalConditionContainsAny 检查）
        if (!__instance.PhysicalConditionContainsAny(EPhysicalCondition.SprintDisabled)) return;

        // 精英力量：允许在灌木丛中冲刺/跳跃
        if ((float)skillMgrExt.StrengthBushSpeedIncBuffElite > 0)
        {
            __instance.SetPhysicalCondition(EPhysicalCondition.SprintDisabled, false);
            __instance.SetPhysicalCondition(EPhysicalCondition.JumpDisabled, false);
        }

        // 非精英：提供部分速度加成
        if (__instance.PhysicalConditionIs(EPhysicalCondition.SprintDisabled))
        {
            __instance.EnableSprint(false);
        }

        // 灌木丛速度加成（来自技能等级）
        __instance.AddStateSpeedLimit(
            0.2f * (1 + (float)skillMgrExt.StrengthBushSpeedIncBuff),
            Player.ESpeedLimit.Swamp);
    }
}
