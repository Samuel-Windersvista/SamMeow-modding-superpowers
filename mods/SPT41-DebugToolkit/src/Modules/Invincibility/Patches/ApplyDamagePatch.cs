using System;
using System.Reflection;
using EFT;
using EFT.Ballistics;
using EFT.HealthSystem;
using HarmonyLib;
using SamMeow.DebugToolkit.Modules.QoL;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.Invincibility.Patches
{
    /// <summary>
    /// <c>ActiveHealthController.ApplyDamage(EBodyPart, float, DamageInfo)</c> prefix：
    /// GodMode（除坠落外清零）/ 忽略爆头 / 百分比减伤 / Keep 1 Health / 部位到下限短路。
    /// 移植 DadGamerMode 1.9.3 <c>Patches/ApplyDamage.cs</c>。
    /// </summary>
    /// <remarks>
    /// 0.16.9 实测（见 <c>docs/api-touchpoints/dadgamer.md</c> §1.1）：该方法为
    /// non-virtual public，<c>ref float damage</c> 改写 + <c>return false</c> 短路成立；
    /// <c>DamageInfo</c> 为 struct，<c>DamageType</c> 含 <c>EDamageType.Fall = 2</c>。
    /// 补丁体 try/catch fail-open（异常穿透游戏调用链 = CTD）。
    /// 含「对敌伤害倍率」（工单 06 并入：仅非本玩家受击分支生效，读 QoL 配置节）。
    /// </remarks>
    internal sealed class ApplyDamagePatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.ApplyDamage));

        [PatchPrefix]
        private static bool Prefix(
            ActiveHealthController __instance,
            ref float damage,
            EBodyPart bodyPart,
            DamageInfo damageInfo)
        {
            try
            {
                Player player = __instance.Player;
                if (player == null)
                {
                    return true;
                }

                // 非本玩家（对敌 / 其他 bot）受击：对敌伤害倍率（工单 06，来自 QoL 配置）。
                // 该分支不依赖无敌配置的绑定状态，独立生效。
                if (!player.IsYourPlayer)
                {
                    ApplyEnemyDamageMultiplier(ref damage);
                    return true;
                }

                if (!InvincibilityConfig.Ready)
                {
                    return true;
                }

                ActiveHealthController healthController = player.ActiveHealthController;
                if (healthController == null)
                {
                    return true;
                }

                // 注意：GetBodyPartHealth 返回 ValueStruct 值拷贝，对副本的写入不会回写控制器
                //（上游既有行为，非移植缺陷）。
                ValueStruct currentHealth = healthController.GetBodyPartHealth(bodyPart, false);
                InvincibilityConfig.SelectionMode selection = InvincibilityConfig.NormalizedSelection();

                // 1) GodMode：除坠落外全伤害清零（规格裁定，与 5.0 线一致）。
                if (InvincibilityConfig.GodMode.Value && !IsFallDamage(damageInfo))
                {
                    damage = 0f;
                    return false;
                }

                // 2) 忽略爆头。
                if (bodyPart == EBodyPart.Head && InvincibilityConfig.IgnoreHeadshotDamage.Value)
                {
                    damage = 0f;
                    return false;
                }

                // 3) 全身百分比（先于头部百分比，与上游一致）。
                if (InvincibilityConfig.PercentDamageAllBody.Value != 100)
                {
                    damage *= InvincibilityConfig.PercentDamageAllBody.Value / 100f;
                }

                // 4) 头部独立百分比（作用于全身百分比之后）。
                if (bodyPart == EBodyPart.Head && InvincibilityConfig.PercentageHeadshotDamage.Value)
                {
                    damage *= InvincibilityConfig.PercentDamageOnlyHead.Value / 100f;
                }

                // 5) Keep 1 Health：致死伤害被短路。
                if (InvincibilityConfig.Keep1Health.Value)
                {
                    bool wouldDropToMinimum = (currentHealth.Current - damage) <= currentHealth.Minimum;
                    if (wouldDropToMinimum)
                    {
                        if (selection == InvincibilityConfig.SelectionMode.All)
                        {
                            damage = 0f;
                            // 上游原样保留：ValueStruct 值拷贝，currentHealth 写入无回写效果。
                            currentHealth.Current = 3f;
                            return false;
                        }

                        if (selection == InvincibilityConfig.SelectionMode.HeadAndThorax &&
                            (bodyPart == EBodyPart.Head || bodyPart == EBodyPart.Chest))
                        {
                            damage = 0f;
                            currentHealth.Current = 3f;
                            return false;
                        }
                    }
                }

                // 6) 部位已在最低血量：非 CODMode 下短路。
                //    上游语义：该保护独立于 Keep 1 Health 开关（上游 SelectionMode 条件恒真，已折叠）；
                //    5.0 线同模块亦如此保留。它阻断已黑部位的二段溢出伤害 / TryToKillAfterDestroyPart 路径。
                //    注意：这是一条「默认即生效」的保护，仅在部位已到最低血量时才触发（正常满血状态下无影响）。
                if (currentHealth.AtMinimum && !InvincibilityConfig.CODMode.Value)
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[Invincibility] ApplyDamage Prefix 异常（fail-open）：{ex}");
            }

            return true;
        }

        /// <summary>
        /// 对敌伤害倍率：仅对非本玩家（敌人 / 其他 bot）受击生效（工单 06，配置节「6. 便利」）。
        /// 上游语义：<c>damage *= enemyDamageMultiplier</c>（默认 1 即不改变）。fail-open。
        /// </summary>
        private static void ApplyEnemyDamageMultiplier(ref float damage)
        {
            try
            {
                if (!QoLSettings.Ready)
                {
                    return;
                }

                float multiplier = QoLSettings.EnemyDamageMultiplier.Value;
                if (multiplier > 1f)
                {
                    damage *= multiplier;
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[Invincibility] 对敌伤害倍率应用失败（已忽略）：{ex.Message}");
            }
        }

        /// <summary>判断伤害类型是否含坠落位（<c>EDamageType.Fall = 2</c>）。</summary>
        private static bool IsFallDamage(DamageInfo damageInfo)
        {
            return (damageInfo.DamageType & EDamageType.Fall) != 0;
        }
    }
}
