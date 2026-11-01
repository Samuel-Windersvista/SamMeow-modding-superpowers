using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using EFT.InventoryLogic;
using UnityEngine;

namespace DoorBreach
{
    /// <summary>
    /// 运行时事实提取：锚点距离、弹药分类、武器类别。
    /// 判定逻辑已迁至纯函数 <see cref="Rules.AttackPolicy"/>；本类不再做受理/拒绝决策。
    /// </summary>
    internal static class DamageUtility
    {
        internal static bool IsShrapnel(AmmoTemplate bulletTemplate)
        {
            // 仅认手雷破片（FragmentType 为手雷破片 id），不认子弹破片。
            return bulletTemplate.FragmentType == "5485a8684bdc2da71d8b4567";
        }

        internal static bool IsHEGrenade(AmmoTemplate bulletTemplate)
        {
            // HE 手雷：有爆炸强度且仅一个抛射物。
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
            return (damageInfo.Weapon as Weapon)?.WeapClass == "shotgun";
        }

        /// <summary>门锁区锚点：无 DoorHandle 时视为无锚点（任意命中有效）。</summary>
        internal static bool TryGetDoorAnchor(DamageInfo damageInfo, out bool hasAnchor, out bool isHit)
        {
            hasAnchor = false;
            isHit = false;

            Collider col = damageInfo.HitCollider;
            if (col == null)
            {
                return false;
            }

            Door door = col.GetComponentInParent<Door>();
            if (door == null)
            {
                return false;
            }

            DoorHandle doorHandle = door.GetComponentInChildren<DoorHandle>();
            if (doorHandle == null)
            {
                isHit = true;
                return true;
            }

            hasAnchor = true;
            Vector3 localHitPoint = door.transform.InverseTransformPoint(damageInfo.HitPoint);
            Vector3 localHandlePos = door.transform.InverseTransformPoint(doorHandle.transform.position);
            isHit = Vector3.Distance(localHitPoint, localHandlePos) < 0.25f;
            return true;
        }

        /// <summary>容器锁区锚点：无 DoorHandle 时视为无锚点。</summary>
        internal static bool TryGetContainerAnchor(DamageInfo damageInfo, out bool hasAnchor, out bool isHit)
        {
            hasAnchor = false;
            isHit = false;

            Collider col = damageInfo.HitCollider;
            if (col == null)
            {
                return false;
            }

            LootableContainer container = col.GetComponentInParent<LootableContainer>();
            if (container == null)
            {
                return false;
            }

            if (container.GetComponentInChildren<DoorHandle>() == null)
            {
                isHit = true;
                return true;
            }

            hasAnchor = true;
            Transform lockTransform = container.transform.Find("Lock");
            if (lockTransform == null)
            {
                DoorBreachPlugin.Logger?.LogDebug($"[Anchor] 容器 {container.name} 缺少 Lock 子物体，锁区无法验证（按未命中处理）。");
                return true;
            }

            isHit = Vector3.Distance(damageInfo.HitPoint, lockTransform.position) < 0.25f;
            return true;
        }

        /// <summary>后备箱锁区锚点：无 DoorHandle 时视为无锚点。</summary>
        internal static bool TryGetTrunkAnchor(DamageInfo damageInfo, out bool hasAnchor, out bool isHit)
        {
            hasAnchor = false;
            isHit = false;

            Collider col = damageInfo.HitCollider;
            if (col == null)
            {
                return false;
            }

            Trunk trunk = col.GetComponentInParent<Trunk>();
            if (trunk == null)
            {
                return false;
            }

            if (trunk.GetComponentInChildren<DoorHandle>() == null)
            {
                isHit = true;
                return true;
            }

            hasAnchor = true;
            Transform carLock = trunk.transform.Find("CarLock_Hand");
            if (carLock == null)
            {
                DoorBreachPlugin.Logger?.LogDebug($"[Anchor] 后备箱 {trunk.name} 缺少 CarLock_Hand 子物体，锁区无法验证（按未命中处理）。");
                return true;
            }

            Transform lockTransform = carLock.Find("Lock");
            if (lockTransform == null)
            {
                DoorBreachPlugin.Logger?.LogDebug($"[Anchor] 后备箱 {trunk.name} 缺少 CarLock_Hand/Lock 子物体，锁区无法验证（按未命中处理）。");
                return true;
            }

            isHit = Vector3.Distance(damageInfo.HitPoint, lockTransform.position) < 0.25f;
            return true;
        }
    }
}
