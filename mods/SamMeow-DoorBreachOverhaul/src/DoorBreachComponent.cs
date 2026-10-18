using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using UnityEngine;

#pragma warning disable IDE0044 // Add readonly modifier
#pragma warning disable IDE0007 // Use implicit type

namespace DoorBreach;

/// <summary>
/// 战局铺场组件：为门/容器/后备箱分配随机耐久并按层筛出可破坏对象，合并武器白名单。
/// </summary>
public class DoorBreachComponent : MonoBehaviour
{
    private static int doorCount = 0;
    private static int invalidStateCount = 0;
    private static int inoperableCount = 0;
    private static int invalidLayerCount = 0;
    private static int containerCount = 0;
    private static int invalidContainers = 0;
    private static int inoperatableContainers = 0;
    private static int invalidContainerLayer = 0;
    private static int trunkCount = 0;
    private static int invalidCarTrunks = 0;
    private static int inoperatableTrunks = 0;
    private static int invalidTrunkLayer = 0;

    private static List<string> GrenadeLaunchers;
    public static List<string> MeleeWeapons;
    private static List<string> ShotgunWeapons;
    private static List<string> OtherWeapons;
    public static List<string> ApplicableWeapons;
    public static List<string> MarkedRooms;
    public static List<string> ValidRounds;

    public void Awake()
    {
        doorCount = 0;
        invalidStateCount = 0;
        inoperableCount = 0;
        invalidLayerCount = 0;
        containerCount = 0;
        invalidContainers = 0;
        inoperatableContainers = 0;
        invalidContainerLayer = 0;
        trunkCount = 0;
        invalidCarTrunks = 0;
        inoperatableTrunks = 0;
        invalidTrunkLayer = 0;
        ApplicableWeapons = [];

        GrenadeLaunchers = DoorBreachPlugin.ModConfig.GrenadeLauncherList;
        MeleeWeapons = DoorBreachPlugin.ModConfig.MeleeWeaponList;
        ShotgunWeapons = DoorBreachPlugin.ModConfig.ShotgunList;
        OtherWeapons = DoorBreachPlugin.ModConfig.OtherWeaponList;
        MarkedRooms = DoorBreachPlugin.ModConfig.BlacklistedRooms;
        ValidRounds = DoorBreachPlugin.ModConfig.WhitelistedRounds;

        int interactiveLayer = LayerMask.NameToLayer("Interactive");

        SetupApplicableWeapons();

        ProcessObjectsOfType<Door>("Doors", interactiveLayer);
        ProcessObjectsOfType<LootableContainer>("Containers", interactiveLayer);
        ProcessObjectsOfType<Trunk>("Trunks", interactiveLayer);

        LogStatistics("Doors", doorCount, invalidStateCount, inoperableCount, invalidLayerCount);
        LogStatistics("Containers", containerCount, invalidContainers, inoperatableContainers, invalidContainerLayer);
        LogStatistics("Trunks", trunkCount, invalidCarTrunks, inoperatableTrunks, invalidTrunkLayer);
    }

    private void ProcessObjectsOfType<T>(string objectType, int interactiveLayer) where T : Component
    {
        DoorBreachPlugin.Logger.LogInfo($"[Breach] 正在处理 {objectType}...");
        int count = 0;
        int invalidCount = 0;
        int inoperableCount = 0;
        int invalidLayerCount = 0;

        FindObjectsOfType<T>().ExecuteForEach(obj =>
        {
            count++;

            if (!IsValidObject(obj, ref invalidCount, ref inoperableCount, ref invalidLayerCount, interactiveLayer))
                return;

            int randHitPoints = Random.Range(DoorBreachPlugin.MinHitPoints, DoorBreachPlugin.MaxHitPoints);
            Hitpoints hitpoints = obj.gameObject.GetOrAddComponent<Hitpoints>();

            hitpoints.hitpoints = randHitPoints;

            switch (obj)
            {
                case Door door:
                    door.OnEnable();
                    break;
                case LootableContainer container:
                    container.OnEnable();
                    break;
                case Trunk trunk:
                    trunk.OnEnable();
                    break;
            }
        });

        LogStatistics(objectType, count, invalidCount, inoperableCount, invalidLayerCount);
    }

    private bool IsOperatable<T>(T obj) where T : Component
    {
        switch (obj)
        {
            case Door door:
                return door.Operatable;
            case LootableContainer container:
                return container.Operatable;
            case Trunk trunk:
                return trunk.Operatable;
            default:
                // 默认：非上述具体类型一律视为可操作
                return true;
        }
    }

    private bool IsValidObject<T>(T obj, ref int invalidCount, ref int inoperableCount, ref int invalidLayerCount, int interactiveLayer) where T : Component
    {
        if (obj is Door door &&
            !IsValidDoorState(door) ||
            obj is LootableContainer container &&
            !IsValidContainerState(container) ||
            obj is Trunk trunk && !IsValidTrunkState(trunk))
        {
            invalidCount++;
            return false;
        }

        if (!IsOperatable(obj))
        {
            inoperableCount++;
            return false;
        }

        if (IsValidLayer(obj, interactiveLayer))
            return true;

        invalidLayerCount++;
        return false;

    }

    private bool IsValidDoorState(Door door)
    {
        if (door.DoorState == EDoorState.Shut ||
           door.DoorState == EDoorState.Locked ||
           door.DoorState == EDoorState.Breaching ||
           door.DoorState == EDoorState.Open)
            return true;

        return false;
    }

    private bool IsValidContainerState(LootableContainer container) =>
        container.DoorState == EDoorState.Shut ||
        container.DoorState == EDoorState.Locked ||
        container.DoorState == EDoorState.Breaching;

    private bool IsValidTrunkState(Trunk trunk) =>
        trunk.DoorState == EDoorState.Shut ||
        trunk.DoorState == EDoorState.Locked ||
        trunk.DoorState == EDoorState.Breaching;

    private bool IsValidLayer<T>(T obj, int interactiveLayer) where T : Component =>
        obj.gameObject.layer == interactiveLayer;

    private void LogStatistics(string objectType, int totalCount, int invalidStateCount, int inoperableCount, int invalidLayerCount)
    {
        DoorBreachPlugin.Logger.LogInfo($"[Breach] {objectType} 总数：{totalCount}");
        DoorBreachPlugin.Logger.LogInfo($"[Breach] {objectType} 无效状态数：{invalidStateCount}");
        DoorBreachPlugin.Logger.LogInfo($"[Breach] {objectType} 不可操作数：{inoperableCount}");
        DoorBreachPlugin.Logger.LogInfo($"[Breach] {objectType} 无效层数：{invalidLayerCount}");
    }

    public static void Enable()
    {
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        gameWorld.GetOrAddComponent<DoorBreachComponent>();
    }

    public static void SetupApplicableWeapons()
    {
        ApplicableWeapons.AddRange(MeleeWeapons);
        ApplicableWeapons.AddRange(GrenadeLaunchers);
        ApplicableWeapons.AddRange(ShotgunWeapons);
        ApplicableWeapons.AddRange(OtherWeapons);

#if DEBUG
        // 打印适用武器哈希到控制台
        DoorBreachPlugin.Logger.LogDebug("[Breach] 适用武器：");

        foreach (string weapon in ApplicableWeapons)
        {
            DoorBreachPlugin.Logger.LogDebug($"[Breach] {weapon}");
        }
#endif
    }
}
