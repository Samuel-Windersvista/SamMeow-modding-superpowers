using System;
using Comfort.Common;
using DoorBreach.Rules;
using DoorOverhaul.Destruction;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;

namespace DoorBreach
{
    /// <summary>
    /// 运行时 → 纯规则层的适配器：只负责把 EFT/Unity 数据映射为纯输入，并调用 AttackPolicy。
    /// 判定本身全部在 DoorBreach.Rules（S0 单测覆盖）；本类不做业务分支。
    /// </summary>
    internal static class AttackPolicyAdapter
    {
        public static AttackDecision EvaluateLock(DamageInfo info, TargetKind target)
        {
            return AttackPolicy.Evaluate(BuildInput(info, target, null), Config());
        }

        public static AttackDecision EvaluateDestruction(DamageInfo info, DoorEntry entry)
        {
            RuleConfig config = Config();
            // 破坏开关按实时配置生效（与 README 的“3. 破坏实时读值”一致）；
            // 注册表未构建时仍由 DoorDamageIntake.OnHit 的实时守卫兜底。
            config.DestructionEnabled = Settings.DestructionEnabled?.Value ?? config.DestructionEnabled;
            return AttackPolicy.Evaluate(BuildInput(info, TargetKind.Door, entry), config);
        }

        private static RuleConfig Config()
        {
            RuleConfig config = DoorBreachPlugin.RuleConfig;
            if (config == null)
            {
                throw new InvalidOperationException("RuleConfig 未初始化（RefreshConfigSnapshot 未执行）");
            }

            return config;
        }

        private static AttackInput BuildInput(DamageInfo info, TargetKind target, DoorEntry entry)
        {
            var input = new AttackInput
            {
                Target = target,
                Material = MapMaterial(info.HittedBallisticCollider != null
                    ? info.HittedBallisticCollider.TypeOfMaterial
                    : MaterialType.None),
                DamageType = MapDamage(info.DamageType),
                Weapon = new WeaponFacts
                {
                    TemplateId = info.Weapon != null ? info.Weapon.TemplateId.ToString() : string.Empty,
                    IsShotgun = DamageUtility.IsShotgun(info),
                },
                DamageAmount = info.Damage,
            };

            // 破坏侧不需要弹药事实；仅锁血池评估构建，避免无谓的 ItemTemplates 查询。
            if (entry == null
                && (input.DamageType == DamageKind.Bullet || input.DamageType == DamageKind.GrenadeFragment))
            {
                input.Ammo = BuildAmmo(info.SourceId);
            }

            if (target == TargetKind.Door)
            {
                Door door = entry != null
                    ? entry.Door
                    : info.HitCollider != null ? info.HitCollider.GetComponentInParent<Door>() : null;
                if (door != null)
                {
                    input.DoorState = MapState(door.DoorState);
                    // 已注册门优先取注册表分类（含 Double / Trigger / Extraction / NonOperatable / Excluded），
                    // 避免适配器另写一套残缺映射；仅注册表未命中时回落到类型判断。
                    input.DoorClass = DoorDestructionRegistry.TryGet(door, out DoorEntry registered)
                        ? registered.DoorClass
                        : MapClass(door);
                    input.KeyId = door.KeyId ?? string.Empty;
                }

                if (DamageUtility.TryGetDoorAnchor(info, out bool hasAnchor, out bool anchorHit))
                {
                    input.HasLockAnchor = hasAnchor;
                    input.IsLockAnchorHit = anchorHit;
                }
                else
                {
                    // 锚点提取失败 = 命中信息不完整；fail-closed，避免当作“无把手门”放行。
                    input.HasLockAnchor = true;
                    input.IsLockAnchorHit = false;
                }
            }
            else if (target == TargetKind.LootableContainer)
            {
                if (DamageUtility.TryGetContainerAnchor(info, out bool hasAnchor, out bool anchorHit))
                {
                    input.HasLockAnchor = hasAnchor;
                    input.IsLockAnchorHit = anchorHit;
                }
                else
                {
                    input.HasLockAnchor = true;
                    input.IsLockAnchorHit = false;
                }
            }
            else
            {
                if (DamageUtility.TryGetTrunkAnchor(info, out bool hasAnchor, out bool anchorHit))
                {
                    input.HasLockAnchor = hasAnchor;
                    input.IsLockAnchorHit = anchorHit;
                }
                else
                {
                    input.HasLockAnchor = true;
                    input.IsLockAnchorHit = false;
                }
            }

            // 破坏侧以 DO 注册表的材质/门类为准（与 BB 锁血池使用同一碰撞体的材质通常一致）。
            // 门类必须取注册表判定结果（含 Double / Trigger / Extraction），不能在适配器里重猜。
            if (entry != null)
            {
                input.Material = MapMaterial(entry.Material);
                input.DoorState = MapState(entry.Door.DoorState);
                input.DoorClass = entry.DoorClass;
                input.KeyId = entry.Door.KeyId ?? string.Empty;
            }

            return input;
        }

        private static AmmoFacts BuildAmmo(string sourceId)
        {
            if (string.IsNullOrEmpty(sourceId) || !Singleton<ItemFactory>.Instantiated)
            {
                return null;
            }

            if (!Singleton<ItemFactory>.Instance.ItemTemplates.TryGetValue(sourceId, out ItemTemplate template)
                || !(template is AmmoTemplate ammo))
            {
                return null;
            }

            return new AmmoFacts
            {
                TemplateId = ammo._id,
                IsHEGrenade = DamageUtility.IsHEGrenade(ammo),
                IsShrapnel = DamageUtility.IsShrapnel(ammo),
                IsBreachingSlug = DamageUtility.IsBreachingSlug(ammo),
            };
        }

        private static MaterialKind MapMaterial(MaterialType material)
        {
            switch (material)
            {
                case MaterialType.WoodThin:
                    return MaterialKind.WoodThin;
                case MaterialType.WoodThick:
                    return MaterialKind.WoodThick;
                case MaterialType.MetalThin:
                    return MaterialKind.MetalThin;
                case MaterialType.MetalThick:
                    return MaterialKind.MetalThick;
                case MaterialType.None:
                    return MaterialKind.None;
                default:
                    return MaterialKind.Other;
            }
        }

        private static DamageKind MapDamage(EDamageType damageType)
        {
            switch (damageType)
            {
                case EDamageType.Bullet:
                    return DamageKind.Bullet;
                case EDamageType.GrenadeFragment:
                    return DamageKind.GrenadeFragment;
                case EDamageType.Melee:
                    return DamageKind.Melee;
                case EDamageType.Explosion:
                    return DamageKind.Explosion;
                default:
                    return DamageKind.Other;
            }
        }

        private static DoorStateKind MapState(EDoorState state)
        {
            switch (state)
            {
                case EDoorState.Shut:
                    return DoorStateKind.Shut;
                case EDoorState.Open:
                    return DoorStateKind.Open;
                case EDoorState.Locked:
                    return DoorStateKind.Locked;
                case EDoorState.Breaching:
                    return DoorStateKind.Breaching;
                case EDoorState.Interacting:
                    return DoorStateKind.Interacting;
                default:
                    return DoorStateKind.Other;
            }
        }

        private static DoorClassKind MapClass(Door door)
        {
            if (door is KeycardDoor)
            {
                return DoorClassKind.Keycard;
            }

            if (door is SlidingDoor)
            {
                return DoorClassKind.Sliding;
            }

            if (door is DoorSwitch)
            {
                return DoorClassKind.DoorSwitch;
            }

            return DoorClassKind.Normal;
        }
    }
}
