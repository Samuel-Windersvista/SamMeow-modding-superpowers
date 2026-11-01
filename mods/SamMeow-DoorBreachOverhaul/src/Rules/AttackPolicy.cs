#nullable disable
using System;

namespace DoorBreach.Rules
{
    /// <summary>
    /// 纯函数攻击策略（Attack Policy）——本项目唯一单元测试接缝（S0）。
    /// 输入只含纯数据；输出锁损伤/破坏受理/拒绝原因。不得引用 Unity/EFT 类型。
    /// </summary>
    public static class AttackPolicy
    {
        /// <summary>同时评估双轨；两轨互不阻塞。</summary>
        public static AttackDecision Evaluate(AttackInput input, RuleConfig config)
        {
            if (input == null)
            {
                throw new ArgumentNullException(nameof(input));
            }

            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var decision = new AttackDecision();
            EvaluateLock(input, config, decision);
            EvaluateDestruction(input, config, decision);
            return decision;
        }

        private static void EvaluateLock(AttackInput input, RuleConfig config, AttackDecision decision)
        {
            // 爆炸命中不进入 BB 锁血池（C4 有独立管线）。
            if (input.DamageType == DamageKind.Explosion)
            {
                RejectLock(decision, "explosion");
                return;
            }

            // 目标开关：Pleb 模式沿用上游行为，绕过容器/后备箱开关。
            if (!config.PlebMode)
            {
                if (input.Target == TargetKind.LootableContainer && !config.OpenLootableContainers)
                {
                    RejectLock(decision, "container disabled");
                    return;
                }

                if (input.Target == TargetKind.Trunk && !config.OpenCarDoors)
                {
                    RejectLock(decision, "trunk disabled");
                    return;
                }
            }

            // KeyId 黑名单只作用于门；Pleb 沿用上游行为绕过黑名单。
            if (!config.PlebMode && input.Target == TargetKind.Door && config.IsBlacklistedRoom(input.KeyId))
            {
                RejectLock(decision, "blacklisted room");
                return;
            }

            bool anchorOk = !input.HasLockAnchor || input.IsLockAnchorHit;

            if (config.PlebMode)
            {
                // 车门/容器在 Pleb 下沿用上游任何命中都有效的语义；门仍要求锁区锚定（工单 03 修正）。
                if (input.Target == TargetKind.Door && !anchorOk)
                {
                    RejectLock(decision, "lock anchor");
                    return;
                }

                AcceptLock(decision, input.DamageAmount, "pleb");
                return;
            }

            if (config.SemiPlebMode && !IsMetal(input.Material))
            {
                if (!anchorOk)
                {
                    RejectLock(decision, "lock anchor");
                    return;
                }

                AcceptLock(decision, input.DamageAmount, "semi-pleb");
                return;
            }

            // 近战：仅非金属门/容器/后备箱；上游不校验锁区锚点。
            if (input.DamageType != DamageKind.Bullet && input.DamageType != DamageKind.GrenadeFragment)
            {
                if (input.DamageType == DamageKind.Melee
                    && config.MeleeWeapons.Contains(input.Weapon.TemplateId)
                    && !IsMetal(input.Material))
                {
                    AcceptLock(decision, input.DamageAmount, "melee");
                    return;
                }

                RejectLock(decision, "damage type");
                return;
            }

            if (input.Ammo == null)
            {
                RejectLock(decision, "ammo unknown");
                return;
            }

            if (!config.BreachingRoundsOpenMetalDoors
                && input.Ammo.IsBreachingSlug
                && IsMetal(input.Material))
            {
                RejectLock(decision, "breaching slug vs metal disabled");
                return;
            }

            bool applicableWeapon = config.IsApplicableWeapon(input.Weapon.TemplateId);
            bool validAmmo = input.Ammo.IsHEGrenade
                || input.Ammo.IsShrapnel
                || input.Ammo.IsBreachingSlug
                || config.WhitelistedRounds.Contains(input.Ammo.TemplateId);

            // 白名单武器 + 非霰弹：跳过弹药判定（上游特例）。
            if (applicableWeapon && !input.Weapon.IsShotgun && anchorOk)
            {
                AcceptLock(decision, input.DamageAmount, "weapon whitelist");
                return;
            }

            // 白名单武器 + 有效弹药；后备箱保留上游一击 500 语义。
            if (applicableWeapon && validAmmo && anchorOk)
            {
                float damage = input.Target == TargetKind.Trunk ? 500f : input.DamageAmount;
                AcceptLock(decision, damage, "weapon+ammo whitelist");
                return;
            }

            RejectLock(decision, "weapon/ammo/anchor");
        }

        private static void EvaluateDestruction(AttackInput input, RuleConfig config, AttackDecision decision)
        {
            if (!config.DestructionEnabled)
            {
                RejectDestruction(decision, "destruction disabled");
                return;
            }

            // 物理不可行项（无碰撞体/无几何/异常尺寸）由 DO 注册表在运行时排除；
            // 门类与排除开关统一走 ExclusionPolicy（与注册表共用同一判定）。
            if (input.Target != TargetKind.Door)
            {
                RejectDestruction(decision, "not a door");
                return;
            }

            ExclusionDecision exclusion = ExclusionPolicy.Evaluate(input.DoorClass, config);
            if (!exclusion.Allowed)
            {
                RejectDestruction(decision, exclusion.Reason);
                return;
            }

            // ProfileFor 只为薄木/厚木/薄金属提供材质档案；其余（含厚金属）保持原版。
            if (input.Material == MaterialKind.None
                || input.Material == MaterialKind.Other
                || input.Material == MaterialKind.MetalThick)
            {
                RejectDestruction(decision, "material stays vanilla");
                return;
            }

            decision.DestructionAccepted = true;
            decision.DestructionReason = input.DoorState == DoorStateKind.Locked
                ? "accepted (locked)"
                : "accepted";
        }

        private static bool IsMetal(MaterialKind material)
        {
            return material == MaterialKind.MetalThin || material == MaterialKind.MetalThick;
        }

        private static void AcceptLock(AttackDecision decision, float damage, string reason)
        {
            decision.LockAccepted = true;
            decision.LockDamage = damage;
            decision.LockReason = reason;
        }

        private static void RejectLock(AttackDecision decision, string reason)
        {
            decision.LockAccepted = false;
            decision.LockDamage = 0f;
            decision.LockReason = reason;
        }

        private static void RejectDestruction(AttackDecision decision, string reason)
        {
            decision.DestructionAccepted = false;
            decision.DestructionReason = reason;
        }
    }
}
