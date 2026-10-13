using System;
using System.Reflection;
using EFT;
using EFT.HealthSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.Invincibility.Patches
{
    /// <summary>
    /// <c>ActiveHealthController.DestroyBodyPart(EBodyPart, EDamageType)</c> prefix：
    /// 按 CODMode / Keep 1 Health 抑制部位摧毁。移植 DadGamerMode 1.9.3
    /// <c>Patches/DestroyBodyPart.cs</c>。
    /// </summary>
    /// <remarks>
    /// 0.16.9 实测（见 <c>docs/api-touchpoints/dadgamer.md</c> §1.2）：non-virtual public，
    /// prefix <c>return false</c> 短路成立。补丁体 try/catch fail-open。
    /// </remarks>
    internal sealed class DestroyBodyPartPatch : ModulePatch
    {
        /// <summary>CODMode 保留流血时仍不允许被摧毁的致命部位（上游 critBodyParts）。</summary>
        private static readonly EBodyPart[] CriticalBodyParts =
        {
            EBodyPart.Stomach,
            EBodyPart.Head,
            EBodyPart.Chest
        };

        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ActiveHealthController), nameof(ActiveHealthController.DestroyBodyPart));

        [PatchPrefix]
        private static bool Prefix(ActiveHealthController __instance, EBodyPart bodyPart)
        {
            try
            {
                if (!InvincibilityConfig.Ready)
                {
                    return true;
                }

                Player player = __instance.Player;
                if (player == null || !player.IsYourPlayer)
                {
                    return true;
                }

                // CODMode 且不保留流血：完全不允许部位摧毁。
                if (InvincibilityConfig.CODMode.Value && !InvincibilityConfig.BleedingDamage.Value)
                {
                    return false;
                }

                // CODMode 且保留流血：仍保护致命部位。
                if (InvincibilityConfig.CODMode.Value && InvincibilityConfig.BleedingDamage.Value &&
                    IsCritical(bodyPart))
                {
                    return false;
                }

                // Keep 1 Health：按范围抑制。
                if (InvincibilityConfig.Keep1Health.Value)
                {
                    InvincibilityConfig.SelectionMode selection = InvincibilityConfig.NormalizedSelection();

                    if (selection == InvincibilityConfig.SelectionMode.All)
                    {
                        return false;
                    }

                    if (selection == InvincibilityConfig.SelectionMode.HeadAndThorax &&
                        (bodyPart == EBodyPart.Head || bodyPart == EBodyPart.Chest))
                    {
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogError($"[Invincibility] DestroyBodyPart Prefix 异常（fail-open）：{ex}");
            }

            return true;
        }

        private static bool IsCritical(EBodyPart bodyPart)
        {
            for (int i = 0; i < CriticalBodyParts.Length; i++)
            {
                if (CriticalBodyParts[i] == bodyPart)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
