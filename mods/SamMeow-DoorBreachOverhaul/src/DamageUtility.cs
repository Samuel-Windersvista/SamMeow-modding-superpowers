using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;

namespace DoorBreach
{
    /// <summary>
    /// 命中伤害判定工具：白名单/材质/命中锚点判定与目标类型分派。
    /// 由 ApplyHit 补丁调用；日志统一走 DoorBreachPlugin.Logger。
    /// </summary>
    internal static class DamageUtility
    {
        internal static void CheckWeaponAndAmmo(DamageInfo damageInfo, ref bool validDamage, List<string> validWeapons, Func<AmmoTemplate, bool> isRoundValid, Func<DamageInfo, bool> isValidLockHit)
        {
            MaterialType material = damageInfo.HittedBallisticCollider.TypeOfMaterial;
            MongoID weaponID = damageInfo.Weapon.TemplateId;

            // 半放行模式：除强化门（金属薄/厚）外，放行武器/弹药白名单，但保留锁区锚定
            if (DoorBreachPlugin.SemiPlebMode &&
                material != MaterialType.MetalThin &&
                material != MaterialType.MetalThick)
            {
                validDamage = isValidLockHit(damageInfo);
                return;
            }

            // 常规近战白名单判定
            if (damageInfo.DamageType != EDamageType.Bullet &&
                damageInfo.DamageType != EDamageType.GrenadeFragment)
            {
                if (damageInfo.DamageType == EDamageType.Melee &&
                    DoorBreachComponent.MeleeWeapons.Contains(weaponID) &&
                    material != MaterialType.MetalThin &&
                    material != MaterialType.MetalThick)
                    validDamage = true;

                return;
            }

            AmmoTemplate bulletTemplate = Singleton<ItemFactory>.Instance.ItemTemplates[damageInfo.SourceId] as AmmoTemplate;

            if (bulletTemplate == null)
                return;

#if DEBUG
            DoorBreachPlugin.Logger.LogDebug($"[Patches] ammoTemplate: {bulletTemplate.Name}");
            DoorBreachPlugin.Logger.LogDebug($"[Patches] 实际 DamageType：{damageInfo.DamageType}");
            DoorBreachPlugin.Logger.LogDebug($"[Patches] isValidLockHit: {isValidLockHit(damageInfo)}");
            DoorBreachPlugin.Logger.LogDebug($"[Patches] isRoundValid: {isRoundValid(bulletTemplate)}");
            DoorBreachPlugin.Logger.LogDebug($"[Patches] 使用武器：{damageInfo.Weapon.LocalizedName()}，id: {damageInfo.Weapon.TemplateId}");
#endif

            // 检查是否为霰弹且材质为金属
            if (!DoorBreachPlugin.BreachingRoundsOpenMetalDoors)
            {
                if (IsBreachingSlug(bulletTemplate) &&
                    material is MaterialType.MetalThin or MaterialType.MetalThick)
                {
                    validDamage = false;
                    return;
                }
            }

            // 命中锚点在 validWeapons 列表且非霰弹时，跳过弹药判定
            if (validWeapons.Contains(weaponID) &&
                !IsShotgun(damageInfo) &&
                isValidLockHit(damageInfo))
            {
                validDamage = true;
            }
            // 常规武器与弹药白名单判定
            else if (validWeapons.Contains(weaponID) && isRoundValid(bulletTemplate) && isValidLockHit(damageInfo))
            {
#if DEBUG
                DoorBreachPlugin.Logger.LogDebug("[Patches] 检测到有效弹药。");
#endif
                validDamage = true;

                // 后备箱特例：一击即可开启
                if (isValidLockHit == IsValidCarTrunkLockHit)
                    damageInfo.Damage = 500;
            }
        }

        internal static void IsMarkedRoom(Door door, ref bool validDamage)
        {
            if (DoorBreachComponent.MarkedRooms.Contains(door.KeyId))
            {
                validDamage = false;
                return;
            }
            validDamage = true;
        }
        internal static bool IsValidRound(AmmoTemplate ammo)
        {
            return DoorBreachComponent.ValidRounds.Contains(ammo._id);
        }
        internal static void CheckDoorWeaponAndAmmo(DamageInfo damageInfo, ref bool validDamage)
        {
            CheckWeaponAndAmmo(damageInfo, ref validDamage, DoorBreachComponent.ApplicableWeapons,
               ammo => IsHEGrenade(ammo) ||
                                   IsShrapnel(ammo) ||
                                   IsBreachingSlug(ammo) ||
                                   IsValidRound(ammo), IsValidDoorLockHit);
        }

        internal static void CheckCarWeaponAndAmmo(DamageInfo damageInfo, ref bool validDamage)
        {
            CheckWeaponAndAmmo(damageInfo, ref validDamage, DoorBreachComponent.ApplicableWeapons,
                ammo => IsHEGrenade(ammo) ||
                                    IsShrapnel(ammo) ||
                                    IsBreachingSlug(ammo) ||
                                    IsValidRound(ammo), IsValidCarTrunkLockHit);
        }

        internal static void CheckLootableContainerWeaponAndAmmo(DamageInfo damageInfo, ref bool validDamage)
        {
            CheckWeaponAndAmmo(damageInfo, ref validDamage, DoorBreachComponent.ApplicableWeapons,
                ammo => IsHEGrenade(ammo) ||
                                    IsShrapnel(ammo) ||
                                    IsBreachingSlug(ammo) ||
                                    IsValidRound(ammo), IsValidContainerLockHit);
        }

        internal static bool IsShrapnel(AmmoTemplate bulletTemplate)
        {
            // 仅认手雷破片（FragmentType 为手雷破片 id），不认子弹破片
            // bulletTemplate._id = "5b44e3f4d4351e003562b3f4";
            return bulletTemplate.FragmentType == "5485a8684bdc2da71d8b4567";
        }

        internal static bool IsHEGrenade(AmmoTemplate bulletTemplate)
        {
            // HE 手雷：有爆炸强度且仅一个抛射物
            return bulletTemplate.ExplosionStrength > 0
                && bulletTemplate.ProjectileCount == 1;
        }

        internal static bool IsBreachingSlug(AmmoTemplate bulletTemplate)
        {
            // 破门弹 id: 660249a0712c1005a4a3ab41
            return bulletTemplate._id == "660249a0712c1005a4a3ab41";
        }
        internal static bool IsShotgun(DamageInfo damageInfo)
        {
            // 武器是否为霰弹枪
            return (damageInfo.Weapon as Weapon)?.WeapClass == "shotgun";
        }
        internal static bool IsValidDoorLockHit(DamageInfo damageInfo)
        {
            // 锁区锚定：命中点须在门把手附近（无把手的门任意命中有效）
            Collider col = damageInfo.HitCollider;
            if (col == null)
                return false;

            Door door = col.GetComponentInParent<Door>();
            if (door == null)
                return false;

            DoorHandle doorHandle = door.GetComponentInChildren<DoorHandle>();
            if (doorHandle == null)
                return true;

            // 统一到门局部空间比较（上游混用碰撞体局部空间与把手 localPosition，坐标系不一致）
            Vector3 localHitPoint = door.transform.InverseTransformPoint(damageInfo.HitPoint);
            Vector3 localHandlePos = door.transform.InverseTransformPoint(doorHandle.transform.position);
            return Vector3.Distance(localHitPoint, localHandlePos) < 0.25f;
        }

        internal static bool IsValidCarTrunkLockHit(DamageInfo damageInfo)
        {
            // 检查是否命中后备箱锁区域
            Collider col = damageInfo.HitCollider;

            // 若无门把手则视为有效命中
            if (col.GetComponentInParent<Trunk>().GetComponentInChildren<DoorHandle>() == null)
                return true;

            GameObject gameobj = col.GetComponentInParent<Trunk>().gameObject;

            // 从目标对象上找 CarLock_Hand/Lock 子物体
            GameObject carLockObj = gameobj.transform.Find("CarLock_Hand").gameObject;
            GameObject lockObj = carLockObj.transform.Find("Lock").gameObject;

            float distanceToLock = Vector3.Distance(damageInfo.HitPoint, lockObj.transform.position);

            return distanceToLock < 0.25f;

        }

        internal static bool IsValidContainerLockHit(DamageInfo damageInfo)
        {
            // 检查是否命中容器锁区域
            Collider col = damageInfo.HitCollider;

            // 若无门把手则视为有效命中
            if (col.GetComponentInParent<LootableContainer>().GetComponentInChildren<DoorHandle>() == null)
                return true;

            GameObject gameobj = col.GetComponentInParent<LootableContainer>().gameObject;

            // 从目标对象上找 Lock 子物体
            GameObject lockObj = gameobj.transform.Find("Lock").gameObject;

            float distanceToLock = Vector3.Distance(damageInfo.HitPoint, lockObj.transform.position);
            return distanceToLock < 0.25f;

        }

    }
}
