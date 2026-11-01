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
using DoorOverhaul.Destruction;
using DoorOverhaul.Destruction.Core;
using EFT.Ballistics;
using NVector2 = System.Numerics.Vector2;

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

    // 降级路径的单次告警标记（物品/模板/音频缺失时不刷屏）。
    private static bool _inventoryErrorLogged;
    private static bool _breachAngleErrorLogged;
    private static bool _placementErrorLogged;
    private static bool _effectsErrorLogged;

    private void Start()
    {
        // 初始化变量
        c4Instances = [];
        componentInstance = this;
        gameWorld = Singleton<GameWorld>.Instance;
        player = gameWorld != null ? gameWorld.MainPlayer : null;
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
        if (player?.Inventory == null)
        {
            return false;
        }

        try
        {
            foreach (Item x in player.Inventory.GetPlayerItems(EPlayerItems.Equipment))
            {
                if (x.TemplateId == C4ExplosiveId)
                {
                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            LogOnce(ref _inventoryErrorLogged, $"[C4] 读取玩家装备栏失败（C4 菜单项将禁用）：{ex.Message}");
        }

        return false;
    }

    public static bool IsValidDoorState(Door door) =>
        door != null &&
        (door.DoorState == EDoorState.Shut || door.DoorState == EDoorState.Locked || door.DoorState == EDoorState.Breaching);

    /// <summary>
    /// 菜单门控（上游语义）：持有 C4 + 门状态合规 + 破门角度合规。
    /// 任意一项失败都返回 true（禁用菜单项），不抛异常、不刷屏。
    /// </summary>
    public static bool IsPlantExplosiveDisabled(Door door, Player player)
    {
        if (door == null || player == null || !HasC4Explosives(player) || !IsValidDoorState(door))
        {
            return true;
        }

        try
        {
            return !door.IsBreachAngle(player.Position);
        }
        catch (Exception ex)
        {
            LogOnce(ref _breachAngleErrorLogged, $"[C4] 破门角度判定失败（菜单项禁用）：{ex.Message}");
            return true;
        }
    }

    /// <summary>放置 C4 并启动倒计时；放置失败返回 false（调用方不得扣除物品 / 发包）。</summary>
    public static bool StartExplosiveBreach(Door door, Player player)
    {
        if (door == null || player == null)
        {
            DoorBreachPlugin.Logger.LogError("[C4] 门或 Player 为 null，无法开始破门。");
            return false;
        }

        if (!TryPlaceC4OnDoor(door, player))
        {
            return false;
        }

        if (gameWorld == null)
        {
            gameWorld = Singleton<GameWorld>.Instance;
        }

        if (gameWorld == null)
        {
            DoorBreachPlugin.Logger.LogError("[C4] GameWorld 不可用，无法启动倒计时。");
            return false;
        }

        // 确保持有 ExplosiveBreachComponent 引用
        if (componentInstance == null)
        {
            componentInstance = gameWorld.GetComponent<ExplosiveBreachComponent>();
            if (componentInstance == null)
            {
                componentInstance = gameWorld.gameObject.AddComponent<ExplosiveBreachComponent>();
            }
        }

        // 为最近放置的 C4 启动协程
        if (c4Instances == null || c4Instances.Count == 0)
        {
            return false;
        }

        C4Instance latestC4Instance = c4Instances.Last();
        StartDelayedExplosionCoroutine(door, player, componentInstance, latestC4Instance);
        return true;
    }
    private static bool TryPlaceC4OnDoor(Door door, Player player)
    {
        if (!Singleton<ItemFactory>.Instantiated)
        {
            LogOnce(ref _placementErrorLogged, "[C4] ItemFactory 未就绪（服务端组件/物品表未加载？），C4 无法放置。");
            return false;
        }

        if (gameWorld == null)
        {
            gameWorld = Singleton<GameWorld>.Instance;
        }

        if (gameWorld == null)
        {
            DoorBreachPlugin.Logger.LogError("[C4] GameWorld 不可用，C4 无法放置。");
            return false;
        }

        if (c4Instances == null)
        {
            c4Instances = new List<C4Instance>();
        }

        Item c4Item;
        try
        {
            c4Item = Singleton<ItemFactory>.Instance.CreateItem(MongoID.Generate(), C4ExplosiveId, null);
        }
        catch (Exception ex)
        {
            LogOnce(ref _placementErrorLogged, $"[C4] 创建 C4 物品失败（服务端组件缺失？）：{ex.Message}");
            return false;
        }

        if (c4Item == null)
        {
            LogOnce(ref _placementErrorLogged, "[C4] 创建 C4 物品返回 null（物品模板未注册？）。");
            return false;
        }

        // 锚定回退链（工单 07）：锁具 → 门把手 → 门中心。
        Transform targetTransform = door.transform.Find("Lock");
        string anchor = "Lock";
        if (targetTransform == null)
        {
            try
            {
                targetTransform = door.Handle?.transform;
            }
            catch (Exception ex)
            {
                LogOnce(ref _placementErrorLogged, $"[C4] 访问门把手失败（回退门中心）：{ex.Message}");
            }

            anchor = "Handle";
        }

        if (targetTransform == null)
        {
            targetTransform = door.transform;
            anchor = "Center";
        }

        if (targetTransform == null)
        {
            DoorBreachPlugin.Logger.LogError("[C4] 找不到适合放置 C4 的门位置。");
            return false;
        }

        Vector3 targetPosition = targetTransform.position;
        Vector3 playerPosition = player.Transform.position;

        // 计算从门（锁或把手）指向玩家的向量
        Vector3 doorToPlayer = playerPosition - targetPosition;
        doorToPlayer.y = 0;

        Vector3 doorForward = doorToPlayer.sqrMagnitude > 1e-6f
            ? doorToPlayer.normalized
            : door.transform.forward;
        float doorThickness = 0.07f; // 需要时调整厚度
        Vector3 c4Position = targetPosition + doorForward * doorThickness; // 略微前置

        Quaternion rotation = Quaternion.LookRotation(doorForward, Vector3.up);
        Quaternion correctionRotation = Quaternion.Euler(90, 0, 0);
        rotation *= correctionRotation;

        // 在世界中放置 C4 物品
        LootItem lootItem = gameWorld.SetupItem(c4Item, player.InteractablePlayer, c4Position, rotation);
        if (lootItem == null)
        {
            DoorBreachPlugin.Logger.LogError("[C4] SetupItem 返回 null，C4 未放置。");
            return false;
        }

        c4Instances.Add(new C4Instance(lootItem, c4Position));
        DoorBreachPlugin.Logger.LogInfo($"[C4] 放置：{door.Id} anchor={anchor} pos={c4Position}");
        return true;
    }

    public static void RemoveItemFromPlayerInventory(Player player)
    {
        if (player?.Inventory == null || player.InventoryController == null)
        {
            return;
        }

        try
        {
            Item foundItem = null;
            foreach (Item item in player.Inventory.GetPlayerItems(EPlayerItems.Equipment))
            {
                if (item.TemplateId != C4ExplosiveId)
                {
                    continue;
                }

                foundItem = item;
                break;
            }

            if (foundItem == null)
            {
                return;
            }

            DoorBreachPlugin.Logger.LogInfo($"[C4] 正在移除 C4，ID：{foundItem.Id}");

            OperationResult discardResult = ItemManipulator.Discard(foundItem, player.InventoryController, true);
            if (discardResult.Failed)
            {
                DoorBreachPlugin.Logger.LogError($"[C4] 无法移除物品：{discardResult.Error}");
                return;
            }

            player.InventoryController.TryRunNetworkTransaction(discardResult);
        }
        catch (Exception ex)
        {
            DoorBreachPlugin.Logger.LogError($"[C4] 移除 C4 异常（物品保留在背包）：{ex.Message}");
        }
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

            // 仅在音效不正确时切换 clip；音频资源缺失（null）时静音运行
            AudioClip intendedClip = (waitTime - timer <= finalToneStart) ? finalToneClip : beepClip;

            if (intendedClip != null &&
                (audioSource.clip != intendedClip ||
                 !audioSource.isPlaying))
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

    private const float DetonationImpulse = 120f;

    private static void TriggerExplosion(Door door, Player player, C4Instance c4Instance)
    {
        if (c4Instance?.LootItem == null || c4Instance.LootItem.gameObject == null)
        {
            return;
        }

        Vector3 explosionPosition = c4Instance.LootItem.transform.position;

        EmitExplosionEffect(explosionPosition);
        if (DoorBreachPlugin.explosionDoesDamage)
        {
            ApplyExplosionDamage(explosionPosition);
        }

        // 先销毁 C4 本体（物品已消耗）
        Destroy(c4Instance.LootItem.gameObject);

        if (door != null && door.gameObject != null)
        {
            bool detonated = TryDetonateDoor(door, explosionPosition);
            if (!detonated && player != null)
            {
                // 不可破坏 / 未注册 / 破坏开关关闭：回退强制开门（上游 C4 永远有效语义）。
                ApplyHit.OpenDoorIfNotAlreadyOpen(door, player, EInteractionType.Breach);
            }

            DoorBreachPlugin.Logger.LogInfo(
                $"[C4] 引爆：{door.Id} -> {(detonated ? "destruction" : "fallback-open")}");
        }

        if (c4Instances.Contains(c4Instance))
        {
            c4Instances.Remove(c4Instance);
        }
    }

    /// <summary>
    /// C4 引爆接入 DO 破坏管线：整扇门板脱离（DoorTakeover 接管，门对象保留）。
    /// 返回 false 时调用方回退强制开门；安全类/高危组开关关闭、物理不可行、破坏关闭都会走到回退。
    /// </summary>
    private static bool TryDetonateDoor(Door door, Vector3 explosionPosition)
    {
        if (!Settings.DestructionEnabled.Value || !Settings.ShowHoles.Value)
        {
            return false;
        }

        if (!DoorDestructionRegistry.TryGet(door, out DoorEntry entry) || !entry.Eligible || entry.Frame == null)
        {
            return false;
        }

        try
        {
            Vector3 center = entry.Frame.ToWorld(entry.LeafTransform, new NVector2(entry.Frame.Width * 0.5f, entry.Frame.Height * 0.5f));
            Vector3 push = center - explosionPosition;
            if (push.sqrMagnitude < 1e-6f)
            {
                push = entry.Frame.NormalWorld(entry.LeafTransform);
            }

            DoorDamageIntake.ApplyDetonation(entry, push.normalized * DetonationImpulse);
            return entry.State != null && entry.State.Leaf == LeafStatus.Detached;
        }
        catch (Exception ex)
        {
            DoorBreachPlugin.Logger.LogError($"[C4] 破坏管线异常 {door.Id}: {ex}");
            return false;
        }
    }

    private static void EmitExplosionEffect(Vector3 position)
    {
        if (effectsInstance == null)
        {
            effectsInstance = Singleton<Effects>.Instance;
        }

        if (effectsInstance == null)
        {
            LogOnce(ref _effectsErrorLogged, "[C4] Effects 单例不可用，爆炸特效跳过。");
            return;
        }

        effectsInstance.EmitGrenade("big_explosion", position, Vector3.forward);
    }

    private static void ApplyExplosionDamage(Vector3 explosionPosition)
    {
        float explosionRadius = DoorBreachPlugin.explosionRadius;
        float baseDamage = DoorBreachPlugin.explosionDamage;
        if (explosionRadius <= 0f || baseDamage <= 0f)
        {
            return;
        }

        Collider[] hitColliders = Physics.OverlapSphere(explosionPosition, explosionRadius);
        var damaged = new HashSet<Player>();
        foreach (Collider hitCollider in hitColliders)
        {
            Player target = hitCollider.GetComponentInParent<Player>();
            if (target == null || !damaged.Add(target))
            {
                continue;
            }

            Vector3 targetPosition = target.Transform.position;
            float distance = Vector3.Distance(targetPosition, explosionPosition);
            if (distance > explosionRadius || !CheckLineOfSight(explosionPosition, targetPosition))
            {
                continue;
            }

            float damageAmount = baseDamage * Mathf.Clamp01(1f - distance / explosionRadius);
            if (damageAmount <= 0f)
            {
                continue;
            }

            var damageInfo = new DamageInfo
            {
                DamageType = EDamageType.Explosion,
                Damage = damageAmount,
                Direction = (targetPosition - explosionPosition).normalized,
                HitPoint = targetPosition,
                HitNormal = -(targetPosition - explosionPosition).normalized,
                Player = null,
                Weapon = null,
                ArmorDamage = damageAmount * 0.5f,
            };

            target.ApplyDamageInfo(damageInfo, EBodyPart.Chest, EBodyPartColliderType.Pelvis, 0f);
        }
    }

    private static bool ExistsInGame(string id)
    {
        return gameWorld != null && gameWorld.FindItemById(id).Value != null;
    }

    private static bool CheckLineOfSight(Vector3 explosionPosition, Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - explosionPosition;
        if (direction.sqrMagnitude < 1e-6f)
        {
            return true;
        }

        if (Physics.Raycast(explosionPosition, direction.normalized, out RaycastHit hit, direction.magnitude))
        {
            return hit.collider.GetComponent<Player>() != null || hit.collider.GetComponentInParent<Player>() != null;
        }

        return false;
    }

    private static void LogOnce(ref bool flag, string message)
    {
        if (flag)
        {
            return;
        }

        flag = true;
        DoorBreachPlugin.Logger.LogWarning($"{message}（后续同类消息已抑制）");
    }

    public static void Enable()
    {
        GameWorld gameWorld = Singleton<GameWorld>.Instance;
        gameWorld.GetOrAddComponent<ExplosiveBreachComponent>();
    }
}
