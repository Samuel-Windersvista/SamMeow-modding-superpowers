using System;
using System.Reflection;
using SPT.Reflection.Patching;
using DoorBreach.Fika;
using DoorOverhaul.Destruction;
using EFT;
using EFT.Ballistics;
using EFT.Interactive;
using UnityEngine;

// 注：UnityEngine.CoreModule 内也带一份 JetBrains.Annotations 命名空间（含 CanBeNullAttribute），
// 若直接 using JetBrains.Annotations 会与 Unity 内置副本二义（CS0433），故按全限定名引用。

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0007 // Use implicit type
#pragma warning disable CS0169 // The field is never used

namespace DoorBreach.Patches;

/// <summary>
/// 命中分派补丁：后置于 BallisticCollider.ApplyHit，对门/容器/后备箱应用锁血池伤害并在归零时开门。
/// </summary>
internal class ApplyHit : ModulePatch
{
    private static BallisticCollider collider;
    private static bool isDoor;
    private static bool isCarTrunk;
    private static bool isLootableContainer;
    private static bool hasHitPoints;
    private static bool validDamage;
    private static Hitpoints hitpoints;
    private static Door door;
    private static Trunk carTrunk;
    private static LootableContainer lootContainer;

    // 4.1.6：BallisticCollider.ApplyHit 单重载，无需显式选择。
    protected override MethodBase GetTargetMethod() =>
        typeof(BallisticCollider).GetMethod(nameof(BallisticCollider.ApplyHit));


    [PatchPostfix]
    public static void PatchPostFix(DamageInfo damageInfo)
    {
        // 破坏派发（两轨独立、互不阻塞；不受 BB“仅本机玩家”门禁限制，保持 DO 任意射手语义）。
        DispatchDestruction(damageInfo);

        // try/catch：过滤掉随机来源的伤害
        try
        {
            if (ShouldApplyDamage(damageInfo))
            {
                HandleDamageForEntity(damageInfo);
            }
        }
        catch (Exception ex)
        {
            DoorBreachPlugin.Logger.LogError($"[Patches] ApplyHitPatch.PatchPostfix() 异常：{ex}");
        }
    }

    /// <summary>破坏派发：仅按“碰撞体→条目”映射查询，不回落 GetComponentInParent（保持 DO 精确语义）。</summary>
    private static void DispatchDestruction(DamageInfo damageInfo)
    {
        try
        {
            BallisticCollider collider = damageInfo.HittedBallisticCollider;
            if (collider == null)
            {
                return;
            }

            if (DoorDestructionRegistry.TryGetByCollider(collider, out DoorEntry entry))
            {
                DoorDamageIntake.OnHit(entry, damageInfo);
            }
        }
        catch (Exception ex)
        {
            DoorBreachPlugin.Logger.LogError($"[Patches] 破坏派发异常：{ex}");
        }
    }

    private static bool ShouldApplyDamage(DamageInfo damageInfo)
    {

        return damageInfo.Player != null
               && damageInfo.Player.iPlayer.IsYourPlayer
               && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Lamp
               && damageInfo.HittedBallisticCollider.HitType != EFT.NetworkPackets.EHitType.Window
               && damageInfo.DamageType != EDamageType.Explosion;
    }

    private static void HandleDamageForEntity(DamageInfo damageInfo)
    {
        BallisticCollider collider = damageInfo.HittedBallisticCollider;
        if (collider == null) return; // Unity null-safe 检查

        bool validDamage = DoorBreachPlugin.PlebMode;

        bool isCarTrunk = collider.GetComponentInParent<Trunk>() != null;
        bool isLootableContainer = collider.GetComponentInParent<LootableContainer>() != null;
        bool isDoor = collider.GetComponentInParent<Door>() != null;
        bool hasHitPoints = collider.GetComponentInParent<Hitpoints>() != null;

        DoorBreachPlugin.Logger.LogDebug(
            $"[Patches] isCarTrunk: {isCarTrunk}, isLootableContainer: {isLootableContainer}, isDoor: {isDoor}, hasHitPoints: {hasHitPoints}");
        DoorBreachPlugin.Logger.LogDebug($"[Patches] validDamage 初始值：{validDamage}");
        DoorBreachPlugin.Logger.LogDebug($"[Patches] hasHitPoints: {hasHitPoints}");
        if (!hasHitPoints) return;

        if (isCarTrunk) HandleCarTrunkDamage(damageInfo, collider, ref validDamage);
        if (isLootableContainer) HandleLootableContainerDamage(damageInfo, collider, ref validDamage);
        if (isDoor) HandleDoorDamage(damageInfo, collider, ref validDamage);
    }

    #region DamageApplication

    private static void HandleCarTrunkDamage(DamageInfo damageInfo, BallisticCollider collider,
        ref bool validDamage)
    {
        if (!DoorBreachPlugin.PlebMode &&
            DoorBreachPlugin.OpenCarDoors)
            DamageUtility.CheckCarWeaponAndAmmo(damageInfo, ref validDamage);

        HandleDamage(damageInfo, collider, ref validDamage, "Car Trunk", (hitpoints, entity) =>
        {
            if (hitpoints.hitpoints <= 0)
            {
                Trunk carTrunk = entity.GetComponentInParent<Trunk>();
                OpenDoorIfNotAlreadyOpen(carTrunk, damageInfo.Player.AIData.Player, EInteractionType.Open);
            }
        });
    }

    private static void HandleLootableContainerDamage(DamageInfo damageInfo,
                                                    BallisticCollider collider,
                                                    ref bool validDamage)
    {
        if (!DoorBreachPlugin.PlebMode &&
            DoorBreachPlugin.OpenLootableContainers)
            DamageUtility.CheckLootableContainerWeaponAndAmmo(damageInfo, ref validDamage);

        HandleDamage(damageInfo, collider, ref validDamage, "Lootable Container", (hitpoints, entity) =>
        {
            if (!(hitpoints.hitpoints <= 0))
                return;

            LootableContainer lootContainer = entity.GetComponentInParent<LootableContainer>();
            OpenDoorIfNotAlreadyOpen(lootContainer, damageInfo.Player.AIData.Player, EInteractionType.Open);
        });
    }

    internal static void HandleDoorDamage(DamageInfo damageInfo,
                                        BallisticCollider collider,
                                        ref bool validDamage)
    {
        if (!DoorBreachPlugin.PlebMode)
        {
            DamageUtility.CheckDoorWeaponAndAmmo(damageInfo, ref validDamage);

            if (validDamage)
                DamageUtility.IsMarkedRoom(collider.GetComponentInParent<Door>(), ref validDamage);
        }
        else
        {
            // 全放行：放行武器/弹药/材质/黑名单，但锁血池仍只吃锁区（门把手附近）命中。
            // 否则任意命中都在积攒锁血池，门会在破坏（尤其铰链实验）完成前被破门打开。
            validDamage = DamageUtility.IsValidDoorLockHit(damageInfo);
        }

        DoorBreachPlugin.Logger.LogDebug($"[Patches] 调用 HandleDamage() 前 validDamage: {validDamage}");
        HandleDamage(damageInfo, collider, ref validDamage, "Door", (hitpoints, entity) =>
        {
            WorldInteractiveObject door = entity.GetComponentInParent<WorldInteractiveObject>();

            DoorBreachPlugin.Logger.LogDebug("[Patches] 门信息：");
            DoorBreachPlugin.Logger.LogDebug($"[Patches] DoorId: {door.Id}");
            DoorBreachPlugin.Logger.LogDebug($"[Patches] KeyId: {door.KeyId}");
            DoorBreachPlugin.Logger.LogDebug($"[Patches] DoorState: {door.DoorState}");
            DoorBreachPlugin.Logger.LogDebug($"[Patches] InitialDoorState: {door.InitialDoorState}");

            if (hitpoints.hitpoints <= 0)
            {
                OpenDoorIfNotAlreadyOpen(door, damageInfo.Player.AIData.Player, EInteractionType.Breach);
            }
        });


    }

    internal static void HandleDamage(DamageInfo damageInfo, BallisticCollider collider, ref bool validDamage,
        string entityName, Action<Hitpoints, GameObject> onHitpointsZero)
    {
        Hitpoints hitpoints = collider.GetComponentInParent<Hitpoints>();

        if (!validDamage)
            return;

        DoorBreachPlugin.Logger.LogInfo($"[Patches] 对 {entityName} 施加命中伤害 {damageInfo.Damage}");
        hitpoints.hitpoints -= damageInfo.Damage;

        onHitpointsZero?.Invoke(hitpoints, collider.gameObject);

    }

    internal static void OpenDoorIfNotAlreadyOpen<T>(T entity, Player player, EInteractionType interactionType)
        where T : class
    {
        switch (entity)
        {
            case Door door:
            {
                if (door.DoorState != EDoorState.Open)
                {
                    door.DoorState = EDoorState.Shut;
                    bool doorUsesAnim = door.interactWithoutAnimation;

                    door.interactWithoutAnimation = true;
                    player.CurrentManagedState.ExecuteDoorInteraction(door, new InteractionResult(interactionType),
                        null, player);
                    door.interactWithoutAnimation = doorUsesAnim;

                    // 生成所有玩家都需要的同步包
                    FikaBridge.SendSyncOpenStatePacket(player, door.Id, 0);
                }

                break;
            }
            case LootableContainer container:
            {
                if (container.DoorState != EDoorState.Open)
                {
                    container.DoorState = EDoorState.Shut;
                    // 记录容器原本是否使用动画
                    bool containerUsesAnim = container.interactWithoutAnimation;

                    // 开启时关闭动画
                    container.interactWithoutAnimation = true;

                    // 解锁容器
                    container.Open();

                    // 打开容器
                    player.CurrentManagedState.ExecuteDoorInteraction(container,
                        new InteractionResult(interactionType), null, player);

                    // 恢复容器的动画要求
                    container.interactWithoutAnimation = containerUsesAnim;

                    FikaBridge.SendSyncOpenStatePacket(player, container.Id, 1);
                }

                break;
            }
            case Trunk trunk:
            {
                if (trunk.DoorState != EDoorState.Open)
                {
                    trunk.DoorState = EDoorState.Shut;

                    // 记录容器原本是否使用动画
                    bool trunkUsesAnim = trunk.interactWithoutAnimation;

                    // 开启时关闭动画
                    trunk.interactWithoutAnimation = true;

                    trunk.Open();
                    player.CurrentManagedState.ExecuteDoorInteraction(trunk, new InteractionResult(interactionType),
                        null, player);

                    trunk.interactWithoutAnimation = trunkUsesAnim;

                    FikaBridge.SendSyncOpenStatePacket(player, trunk.Id, 2);
                }

                break;
            }
        }
    }

    internal static void CustomExecuteDoorInteraction(WorldInteractiveObject interactive,
        InteractionResult interactionResult, [JetBrains.Annotations.CanBeNull] Action callback, Player user)
    {
        interactive.interactWithoutAnimation = true;
        interactive.SetUser(user);
        interactive.LockForInteraction();
        interactive.Interact(interactionResult);
    }

    #endregion
}
