using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Comfort.Common;
using Diz.LanguageExtensions;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using Systems.Effects;
using UnityEngine;
using UnityEngine.Networking;
using DoorBreach.Patches;
using EFT.Ballistics;

namespace DoorBreach;

/// <summary>
/// C4 破门组件：在门上放置 C4、倒计时（蜂鸣音效）、引爆并开门。
/// 音频资源以插件自身目录为根解析（bin/audio / release/audio）。
/// </summary>
public class ExplosiveBreachComponent : MonoBehaviour
{
    internal static Player player;
    internal static GameWorld gameWorld;
    internal static List<C4Instance> c4Instances;
    private static ExplosiveBreachComponent componentInstance;
    //private static readonly string TNTTemplateId = "60391b0fb847c71012789415";
    public static readonly string C4ExplosiveId = "6636606320e842b50084e51a";
    private static Vector2 _impactsGagRadius;
    private static Effects effectsInstance;

    internal static AudioClip beepClip;
    internal static AudioClip finalToneClip;

    private void Start()
    {
        // 初始化变量
        c4Instances = [];
        componentInstance = this;
        gameWorld = Singleton<GameWorld>.Instance;
        player = gameWorld.MainPlayer;
        _impactsGagRadius = new Vector2(1f, 3f);
        effectsInstance = Singleton<Effects>.Instance;

        // 预加载音频（以插件自身目录为根；缺失则静音运行，不启动协程）
        string pluginDir = Path.GetDirectoryName(DoorBreachPlugin.Instance.Info.Location);
        string beepPath = Path.Combine(pluginDir, "audio", "Beep.mp3");
        string finalTonePath = Path.Combine(pluginDir, "audio", "FinalBeepTone.mp3");

        if (File.Exists(beepPath))
            StartCoroutine(LoadAudioClip(beepPath, true));
        else
            DoorBreachPlugin.Logger.LogWarning($"[C4] 音频资源缺失：{beepPath}（C4 静音运行）");

        if (File.Exists(finalTonePath))
            StartCoroutine(LoadAudioClip(finalTonePath, false));
        else
            DoorBreachPlugin.Logger.LogWarning($"[C4] 音频资源缺失：{finalTonePath}（C4 静音运行）");
    }

    private IEnumerator LoadAudioClip(string filePath, bool isBeepClip)
    {
        // 用 Uri 生成 file:// URI，支持非 ASCII 路径
        string uri = new Uri(filePath).AbsoluteUri;
        using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.MPEG))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                DoorBreachPlugin.Logger.LogWarning($"[C4] 音频加载失败：{uwr.error}");
            }
            else
            {
                if (isBeepClip)
                    beepClip = DownloadHandlerAudioClip.GetContent(uwr);
                else
                    finalToneClip = DownloadHandlerAudioClip.GetContent(uwr);

                DoorBreachPlugin.Logger.LogInfo($"[C4] 音频加载成功：{filePath}");
            }
        }
    }
    public static bool HasC4Explosives(Player player)
    {
        // 在玩家物品中查找第一个 C4
        Item foundItem = null;

        foreach (var x in player.Inventory.GetPlayerItems(EPlayerItems.Equipment))
        {
            if (x.TemplateId != C4ExplosiveId)
                continue;

            foundItem = x;
            break;
        }

        return foundItem != null;
    }

    public static bool IsValidDoorState(Door door) =>
        door.DoorState == EDoorState.Shut || door.DoorState == EDoorState.Locked || door.DoorState == EDoorState.Breaching;

    public static void StartExplosiveBreach(Door door, Player player)
    {
        if (door == null ||
            player == null)
        {
            DoorBreachPlugin.Logger.LogError("[C4] 门或 Player 为 null，无法开始破门。");
            return;
        }

        TryPlaceC4OnDoor(door, player);

        //RemoveItemFromPlayerInventory(player);

        // 确保持有 ExplosiveBreachComponent 引用
        if (componentInstance == null)
        {
            componentInstance = gameWorld.GetComponent<ExplosiveBreachComponent>();
            if (componentInstance == null)
                componentInstance = gameWorld.gameObject.AddComponent<ExplosiveBreachComponent>();
        }

        // 为最近放置的 C4 启动协程
        if (!c4Instances.Any())
            return;

        C4Instance latestC4Instance = c4Instances.Last();
        StartDelayedExplosionCoroutine(door, player, componentInstance, latestC4Instance);
    }
    private static void TryPlaceC4OnDoor(Door door, Player player)
    {
        ItemFactory itemFactory = Singleton<ItemFactory>.Instance;
        Item c4Item = itemFactory.CreateItem(MongoID.Generate(), C4ExplosiveId, null);

        Transform lockTransform = door.transform.Find("Lock");
        Transform doorHandleTransform = null;

        try
        {
            // 安全访问 door.Handle
            doorHandleTransform = door.Handle?.transform;
        }
        catch (Exception ex)
        {
            DoorBreachPlugin.Logger.LogError($"[C4] 访问门把手失败：{ex.Message}");
        }

        Component lockComponent = door.gameObject.GetComponent("Lock");

        // 依可用组件决定目标 transform
        Transform targetTransform;

        if (lockTransform != null)
        {
            targetTransform = lockTransform;
        }
        else if (doorHandleTransform != null)
        {
            targetTransform = doorHandleTransform;
        }
        else if (lockComponent != null && lockComponent.transform != null)
        {
            targetTransform = lockComponent.transform;
        }
        else
        {
            // 无 Lock 组件与门把手时，默认取门中心
#if DEBUG
            DoorBreachPlugin.Logger.LogInfo("[C4] 未找到 Lock 组件与门把手，默认使用门中心位置");
#endif
            targetTransform = door.transform;
        }

        if (targetTransform == null)
        {
            DoorBreachPlugin.Logger.LogError("[C4] 找不到适合放置 C4 的门位置。");
            return;
        }

        Vector3 targetPosition = targetTransform.position;
        Vector3 playerPosition = player.Transform.position;

        // 计算从门（锁或把手）指向玩家的向量
        Vector3 doorToPlayer = playerPosition - targetPosition;
        doorToPlayer.y = 0;

        Vector3 doorForward = doorToPlayer.normalized;
        float doorThickness = 0.07f; // 需要时调整厚度
        Vector3 c4Position = targetPosition + doorForward * doorThickness; // 略微前置

        Quaternion rotation = Quaternion.LookRotation(doorForward, Vector3.up);
        Quaternion correctionRotation = Quaternion.Euler(90, 0, 0);
        rotation *= correctionRotation;

        // 在世界中放置 C4 物品
        LootItem lootItem = gameWorld.SetupItem(c4Item, player.InteractablePlayer, c4Position, rotation);
        c4Instances.Add(new C4Instance(lootItem, c4Position));
    }

    public static void RemoveItemFromPlayerInventory(Player player)
    {
        DoorBreachPlugin.Logger.LogInfo("[C4] RemoveItemFromPlayerInventory 被调用。");

        if (player == null)
            return;

        IEnumerable<Item> items = player.Inventory.GetPlayerItems(EPlayerItems.Equipment);
        Item foundItem = null;

        foreach (var item in items)
        {
            if (item.TemplateId != C4ExplosiveId)
                continue;

            foundItem = item;
            break;
        }

        InventoryController inventoryController = player.InventoryController;

        DoorBreachPlugin.Logger.LogInfo($"[C4] 尝试从玩家背包移除 C4。Player ID: {player.ProfileId}");

        if (foundItem == null)
            return;

        DoorBreachPlugin.Logger.LogInfo($"[C4] 正在移除 C4，ID：{foundItem?.Id}");

        OperationResult discardResult = ItemManipulator.Discard(foundItem, inventoryController, true);

        if (discardResult.Failed)
        {
            DoorBreachPlugin.Logger.LogError($"[C4] 无法移除物品：{discardResult.Error}");
            return;
        }

        inventoryController.TryRunNetworkTransaction(discardResult);
    }

    private static void StartDelayedExplosionCoroutine(Door door, Player player, MonoBehaviour monoBehaviour, C4Instance c4Instance)
    {
        if (c4Instance?.LootItem == null)
        {
            DoorBreachPlugin.Logger.LogError("[C4] C4 实例或 LootItem 为 null。");
            return;
        }

        c4Instance.ExplosionCoroutine = monoBehaviour.StartCoroutine(DelayedExplosion(door, player, c4Instance));
    }
    private static void StopExplosionCoroutine(C4Instance c4Instance)
    {
        if (componentInstance != null && c4Instance?.ExplosionCoroutine != null)
        {
            componentInstance.StopCoroutine(c4Instance.ExplosionCoroutine);
            c4Instance.ExplosionCoroutine = null;
            //DoorBreachPlugin.Logger.LogInfo("Coroutine stopped successfully.");
        }
        else
        {
            DoorBreachPlugin.Logger.LogError("[C4] 停止协程失败：组件或协程引用为 null。");
        }
    }

    private static IEnumerator DelayedExplosion(Door door, Player player, C4Instance c4Instance)
    {
        float waitTime = DoorBreachPlugin.explosiveTimerInSec;
        float timer = 0;
        float normalBeepInterval = 1.0f;  // 常规蜂鸣间隔
        float rapidBeepStart = 5.0f;      // 进入急促蜂鸣的剩余时间
        float finalRapidBeepInterval = 0.3f;  // 末尾急促蜂鸣间隔
        float finalToneStart = 1.0f;      // 进入连续长音的剩余时间

        AudioSource audioSource = c4Instance.LootItem.gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = c4Instance.LootItem.gameObject.AddComponent<AudioSource>();
            audioSource.volume = 0.8f; //1.0 为最大
            audioSource.spatialBlend = 1.0f; //3d 音效
        }

        while (timer < waitTime)
        {
            // 检查 C4 是否仍存在于世界中
            if (c4Instance == null ||
                c4Instance.LootItem == null ||
                c4Instance.LootItem.Item == null ||
                !ExistsInGame(c4Instance.LootItem.Item.Id))
            {
                StopExplosionCoroutine(c4Instance);
                yield break;
            }

            // 仅在音效不正确时切换 clip
            AudioClip intendedClip = (waitTime - timer <= finalToneStart) ? finalToneClip : beepClip;

            if (audioSource.clip != intendedClip ||
                !audioSource.isPlaying)
            {
                audioSource.clip = intendedClip;
                audioSource.Play();
            }


            float currentBeepInterval;

            // 动态计算蜂鸣间隔
            if (waitTime - timer <= rapidBeepStart &&
                waitTime - timer > finalToneStart)
            {
                float lerpFactor = (waitTime - timer - finalToneStart) / (rapidBeepStart - finalToneStart);
                currentBeepInterval = Mathf.Lerp(finalRapidBeepInterval, normalBeepInterval, lerpFactor);
            }
            else if (waitTime - timer > rapidBeepStart)
            {
                currentBeepInterval = normalBeepInterval;
            }
            else
            {
                currentBeepInterval = finalRapidBeepInterval;
            }

            float timeToNextBeep = Mathf.Min(currentBeepInterval, waitTime - timer);
            yield return new WaitForSeconds(timeToNextBeep);
            timer += timeToNextBeep;
        }

        // 触发爆炸特效、伤害计算与清理
        if (c4Instance.LootItem != null &&
            c4Instance.LootItem.gameObject != null)
        {
            TriggerExplosion(door, player, c4Instance);
        }
    }

    private static void TriggerExplosion(Door door, Player player, C4Instance c4Instance)
    {
        if (c4Instance.LootItem == null ||
            c4Instance.LootItem.gameObject == null)
            return;

        effectsInstance.EmitGrenade("big_explosion",
                                    c4Instance.LootItem.transform.position,
                                    Vector3.forward);

        if (DoorBreachPlugin.explosionDoesDamage)
        {
            float explosionRadius = DoorBreachPlugin.explosionRadius;
            float baseDamage = DoorBreachPlugin.explosionDamage;
            Vector3 explosionPosition = c4Instance.LootItem.transform.position;

            Collider[] hitColliders = Physics.OverlapSphere(explosionPosition, explosionRadius);

            foreach (Collider hitCollider in hitColliders)
            {
                Player tempplayer = hitCollider.GetComponentInParent<Player>();

                if (tempplayer == null)
                    continue;

                float distance = Vector3.Distance(hitCollider.transform.position, explosionPosition);

                if (!CheckLineOfSight(explosionPosition, hitCollider.transform.position))
                    continue;

                float damageMultiplier = Mathf.Clamp01(1 - distance / explosionRadius);
                float damageAmount = baseDamage * damageMultiplier;

                DamageInfo damageInfo = new DamageInfo
                {
                    DamageType = EDamageType.Explosion,
                    Damage = damageAmount,
                    Direction = (tempplayer.Transform.position - explosionPosition).normalized,
                    HitPoint = tempplayer.Transform.position,
                    HitNormal = -(tempplayer.Transform.position - explosionPosition).normalized,
                    Player = null,
                    Weapon = null,
                    ArmorDamage = damageAmount * 0.5f,
                };

                tempplayer.ApplyDamageInfo(damageInfo, EBodyPart.Chest, EBodyPartColliderType.Pelvis, 0f);
            }
        }

        // 确认 C4 存在后销毁
        if (c4Instance != null &&
            c4Instance.LootItem != null)
        {
            Destroy(c4Instance.LootItem.gameObject);
        }

        // 确认门存在且未销毁后开门并销毁
        if (door != null &&
            door.gameObject != null)
        {
            ApplyHit.OpenDoorIfNotAlreadyOpen(door, player, EInteractionType.Breach);

            // 删除门；相关玻璃的联动检查另行处理
            Destroy(door.gameObject);
        }

        // 清理引用
        if (c4Instances.Contains(c4Instance))
        {
            c4Instances.Remove(c4Instance);
        }
    }

    private static bool ExistsInGame(string id)
    {
        return gameWorld.FindItemById(id).Value != null;
    }

    private static bool CheckLineOfSight(Vector3 explosionPosition, Vector3 playerPosition)
    {
        Vector3 direction = playerPosition - explosionPosition;
        if (Physics.Raycast(explosionPosition, direction.normalized, out RaycastHit hit, direction.magnitude))
        {
            return hit.collider.GetComponent<Player>() != null;
        }
        return false;
    }

    public static void Enable()
    {
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        gameWorld.GetOrAddComponent<ExplosiveBreachComponent>();
    }
}
