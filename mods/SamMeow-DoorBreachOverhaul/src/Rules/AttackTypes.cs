#nullable disable
using System;
using System.Collections.Generic;

namespace DoorBreach.Rules
{
    /// <summary>命中目标类别（决定锁血池开关与锚点语义）。</summary>
    public enum TargetKind
    {
        Door,
        LootableContainer,
        Trunk
    }

    /// <summary>EFT 材质向纯规则层的归一化映射；Other = 上游规则不处理、门体保持原版。</summary>
    public enum MaterialKind
    {
        None,
        WoodThin,
        WoodThick,
        MetalThin,
        MetalThick,
        Other
    }

    /// <summary>伤害类型（Explosion 在 BB 锁血池门外，C4 走独立管线）。</summary>
    public enum DamageKind
    {
        Bullet,
        GrenadeFragment,
        Melee,
        Explosion,
        Other
    }

    public enum DoorStateKind
    {
        Shut,
        Open,
        Locked,
        Breaching,
        Interacting,
        Other
    }

    /// <summary>
    /// 门类（与 DO 注册表排除项对齐）。
    /// 安全类：Keycard / Sliding / DoorSwitch / NonOperatable / Double（总开关解除）。
    /// 高危组：Trigger / Extraction（独立子开关，默认关）。
    /// Excluded：物理不可行（注册表在运行时判定，永不可开关化）。
    /// </summary>
    public enum DoorClassKind
    {
        Normal,
        Keycard,
        Sliding,
        DoorSwitch,
        NonOperatable,
        Double,
        Trigger,
        Extraction,
        Excluded
    }

    /// <summary>排除分类：调试面板按类显示，判定按类受理。</summary>
    public enum ExclusionCategory
    {
        None,
        Fika,
        Safety,
        HighRisk,
        Physical
    }

    /// <summary>
    /// 规则层配置快照：只含判定所需字段，全部为纯数据。
    /// 扩展位：后续按材质/门类/命中区拆规则组时，在此追加规则组集合，不改 Evaluate 签名。
    /// </summary>
    public sealed class RuleConfig
    {
        public bool PlebMode;
        public bool SemiPlebMode;
        public bool BreachingRoundsOpenMetalDoors;
        public bool OpenLootableContainers = true;
        public bool OpenCarDoors = true;
        public bool DestructionEnabled = true;

        /// <summary>安全类排除总开关（钥匙卡/滑门/开关门/成对双门/非可操作；默认保持排除）。</summary>
        public bool AllowSafetyExclusions;

        /// <summary>高危组子开关（触发任务门/撤离门；默认保持排除，带警示）。</summary>
        public bool AllowHighRiskExclusions;

        public HashSet<string> ShotgunWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> GrenadeLauncherWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> MeleeWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> OtherWeapons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> WhitelistedRounds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        public HashSet<string> BlacklistedRooms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public int MinHitPoints = 100;
        public int MaxHitPoints = 200;

        public bool IsApplicableWeapon(string templateId)
        {
            return ShotgunWeapons.Contains(templateId)
                || GrenadeLauncherWeapons.Contains(templateId)
                || MeleeWeapons.Contains(templateId)
                || OtherWeapons.Contains(templateId);
        }

        /// <summary>血量区间必须是正数且下限不大于上限。</summary>
        public bool IsValidHitPointRange => MinHitPoints >= 1 && MaxHitPoints >= MinHitPoints;

        public bool IsBlacklistedRoom(string keyId)
        {
            return !string.IsNullOrEmpty(keyId) && BlacklistedRooms.Contains(keyId);
        }
    }

    public sealed class WeaponFacts
    {
        public string TemplateId = string.Empty;
        public bool IsShotgun;
    }

    public sealed class AmmoFacts
    {
        public string TemplateId = string.Empty;
        public bool IsHEGrenade;
        public bool IsShrapnel;
        public bool IsBreachingSlug;
    }

    /// <summary>
    /// 规则层输入：运行时 adapter 从 DamageInfo / 门对象 / 配置快照映射而来。
    /// 不包含玩家归属与 HitType——这两项属于补丁边界过滤，留在 adapter。
    /// </summary>
    public sealed class AttackInput
    {
        public TargetKind Target;
        public MaterialKind Material;
        public DamageKind DamageType;
        public WeaponFacts Weapon = new WeaponFacts();
        public AmmoFacts Ammo;
        public bool HasLockAnchor;
        public bool IsLockAnchorHit;
        public DoorStateKind DoorState;
        public DoorClassKind DoorClass;
        public string KeyId = string.Empty;
        public float DamageAmount;
    }

    /// <summary>
    /// 规则层输出：双轨各自独立；LockDamage 为 0 表示不受理。
    /// DestructionReason / LockReason 供日志与调试面板显示。
    /// </summary>
    public sealed class AttackDecision
    {
        public bool LockAccepted;
        public float LockDamage;
        public string LockReason = string.Empty;

        public bool DestructionAccepted;
        public string DestructionReason = string.Empty;
    }
}
