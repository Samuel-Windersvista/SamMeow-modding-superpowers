using System;
using System.Collections.Generic;
using DoorBreach.Rules;
using Xunit;

namespace SamMeow.DoorBreachOverhaul.Tests;

/// <summary>
/// S0 规则层表驱动单测：只测纯函数输入 → 判定输出，不测补丁装配/游戏对象。
/// </summary>
public class AttackPolicyTests
{
    private const float DefaultDamage = 42f;

    [Theory]
    [MemberData(nameof(LockCases))]
    public void LockPolicy_Table(
        string scenario,
        AttackInput input,
        RuleConfig config,
        bool expectedAccepted,
        float expectedDamage,
        string expectedReason)
    {
        AttackDecision decision = AttackPolicy.Evaluate(input, config);

        Assert.True(
            expectedAccepted == decision.LockAccepted,
            $"{scenario}: LockAccepted={decision.LockAccepted}, reason={decision.LockReason}");
        Assert.Equal(expectedDamage, decision.LockDamage, 3);
        Assert.True(
            decision.LockReason.Contains(expectedReason, StringComparison.OrdinalIgnoreCase),
            $"{scenario}: reason='{decision.LockReason}' 不含 '{expectedReason}'");
    }

    [Theory]
    [MemberData(nameof(DestructionCases))]
    public void DestructionPolicy_Table(
        string scenario,
        AttackInput input,
        RuleConfig config,
        bool expectedAccepted,
        string expectedReason)
    {
        AttackDecision decision = AttackPolicy.Evaluate(input, config);

        Assert.True(
            expectedAccepted == decision.DestructionAccepted,
            $"{scenario}: DestructionAccepted={decision.DestructionAccepted}, reason={decision.DestructionReason}");
        Assert.True(
            decision.DestructionReason.Contains(expectedReason, StringComparison.OrdinalIgnoreCase),
            $"{scenario}: reason='{decision.DestructionReason}' 不含 '{expectedReason}'");
    }

    [Fact]
    public void LockAndDestruction_AreIndependent()
    {
        AttackInput input = Input(ammo: Ammo(), anchorHit: false);
        AttackDecision decision = AttackPolicy.Evaluate(input, Config());

        Assert.False(decision.LockAccepted);
        Assert.Equal(0f, decision.LockDamage, 3);
        Assert.True(decision.DestructionAccepted);
    }

    [Fact]
    public void DefaultHitPointRange_IsValid()
    {
        Assert.True(Config().IsValidHitPointRange);
    }

    [Fact]
    public void HitPointRange_ZeroMin_IsInvalid()
    {
        RuleConfig config = Config(c => c.MinHitPoints = 0);
        Assert.False(config.IsValidHitPointRange);
    }

    [Fact]
    public void HitPointRange_MaxBelowMin_IsInvalid()
    {
        RuleConfig config = Config(c => c.MaxHitPoints = c.MinHitPoints - 1);
        Assert.False(config.IsValidHitPointRange);
    }

    public static IEnumerable<object[]> LockCases()
    {
        // 上游特例：白名单内非霰弹武器走第一分支，弹药判定被跳过。
        yield return Case("normal rifle + whitelisted ammo + anchor",
            Input(ammo: Ammo()), Config(), true, DefaultDamage, "weapon whitelist");

        // 上游特例：白名单内非霰弹武器跳过弹药判定。
        yield return Case("normal rifle + non-whitelisted ammo + anchor",
            Input(ammo: Ammo(whitelisted: false)), Config(), true, DefaultDamage, "weapon");

        yield return Case("normal shotgun + whitelisted ammo",
            Input(weapon: "shotgun", shotgun: true, ammo: Ammo()), Config(), true, DefaultDamage, "weapon+ammo");

        yield return Case("normal shotgun + non-whitelisted ammo",
            Input(weapon: "shotgun", shotgun: true, ammo: Ammo(whitelisted: false)), Config(), false, 0f, "weapon/ammo/anchor");

        // 证明白名单真正来自 RuleConfig（清空后同一发弹应拒绝）。
        yield return Case("normal shotgun + round but config whitelist cleared",
            Input(weapon: "shotgun", shotgun: true, ammo: Ammo()),
            Config(c => c.WhitelistedRounds.Clear()), false, 0f, "weapon/ammo/anchor");

        yield return Case("grenade fragment + shrapnel + shotgun",
            Input(damage: DamageKind.GrenadeFragment, weapon: "shotgun", shotgun: true,
                ammo: Ammo(whitelisted: false, shrapnel: true)),
            Config(), true, DefaultDamage, "weapon+ammo");

        // GL 武器同样是非霰弹白名单武器，HE 弹药判定被第一分支跳过。
        yield return Case("normal grenade launcher + HE ammo",
            Input(weapon: "gl", ammo: Ammo(whitelisted: false, he: true)), Config(), true, DefaultDamage, "weapon whitelist");

        // 霰弹枪 + HE 弹药才走第二分支（弹药有效性参与判定）。
        yield return Case("normal shotgun + HE ammo",
            Input(weapon: "shotgun", shotgun: true, ammo: Ammo(whitelisted: false, he: true)),
            Config(), true, DefaultDamage, "weapon+ammo");

        yield return Case("normal melee + non-metal + no anchor",
            Input(damage: DamageKind.Melee, weapon: "melee", ammo: null, hasAnchor: false), Config(), true, DefaultDamage, "melee");

        yield return Case("normal melee + metal",
            Input(damage: DamageKind.Melee, weapon: "melee", ammo: null, material: MaterialKind.MetalThin), Config(), false, 0f, "damage type");

        yield return Case("breaching slug + metal + toggle off",
            Input(ammo: Ammo(whitelisted: false, breaching: true), material: MaterialKind.MetalThin), Config(), false, 0f, "breaching slug");

        yield return Case("breaching slug + metal + toggle on + shotgun",
            Input(weapon: "shotgun", shotgun: true, ammo: Ammo(whitelisted: false, breaching: true), material: MaterialKind.MetalThin),
            Config(c => c.BreachingRoundsOpenMetalDoors = true), true, DefaultDamage, "weapon+ammo");

        yield return Case("normal + anchor miss",
            Input(ammo: Ammo(), anchorHit: false), Config(), false, 0f, "weapon/ammo/anchor");

        yield return Case("normal + no handle (anchor not required)",
            Input(ammo: Ammo(), hasAnchor: false), Config(), true, DefaultDamage, "weapon whitelist");

        yield return Case("semi-pleb + non-metal + unknown weapon",
            Input(ammo: Ammo(), weapon: "unknown", material: MaterialKind.WoodThick),
            Config(c => c.SemiPlebMode = true), true, DefaultDamage, "semi-pleb");

        yield return Case("semi-pleb + metal + unknown weapon",
            Input(ammo: Ammo(), weapon: "unknown", material: MaterialKind.MetalThin),
            Config(c => c.SemiPlebMode = true), false, 0f, "weapon/ammo/anchor");

        yield return Case("semi-pleb + blacklisted room",
            Input(ammo: Ammo(), keyId: "black"),
            Config(c => c.SemiPlebMode = true), false, 0f, "blacklisted");

        yield return Case("semi-pleb + anchor miss",
            Input(ammo: Ammo(), anchorHit: false),
            Config(c => c.SemiPlebMode = true), false, 0f, "lock anchor");

        yield return Case("semi-pleb + trunk = normal damage (not 500)",
            Input(target: TargetKind.Trunk, ammo: Ammo()),
            Config(c => c.SemiPlebMode = true), true, DefaultDamage, "semi-pleb");

        yield return Case("pleb + door + anchor",
            Input(ammo: Ammo(whitelisted: false), weapon: "unknown"), Config(c => c.PlebMode = true), true, DefaultDamage, "pleb");

        yield return Case("pleb + door + anchor miss",
            Input(ammo: Ammo(), anchorHit: false), Config(c => c.PlebMode = true), false, 0f, "lock anchor");

        // 上游特例：Pleb 下车门/容器不要求锚定。
        yield return Case("pleb + container + anchor miss",
            Input(target: TargetKind.LootableContainer, ammo: Ammo(), anchorHit: false),
            Config(c => c.PlebMode = true), true, DefaultDamage, "pleb");

        yield return Case("pleb + door + blacklisted room",
            Input(ammo: Ammo(), keyId: "black"), Config(c => c.PlebMode = true), true, DefaultDamage, "pleb");

        yield return Case("normal + door + blacklisted room",
            Input(ammo: Ammo(), keyId: "black"), Config(), false, 0f, "blacklisted");

        // 后备箱 500 仅霰弹枪走第二分支时触发；步枪走第一分支，保持普通伤害。
        yield return Case("normal + trunk + shotgun whitelisted = 500",
            Input(target: TargetKind.Trunk, weapon: "shotgun", shotgun: true, ammo: Ammo()),
            Config(), true, 500f, "weapon+ammo");

        yield return Case("normal + trunk + rifle whitelisted = normal damage",
            Input(target: TargetKind.Trunk, ammo: Ammo()), Config(), true, DefaultDamage, "weapon whitelist");

        yield return Case("normal + trunk + shotgun non-whitelisted",
            Input(target: TargetKind.Trunk, weapon: "shotgun", shotgun: true, ammo: Ammo(whitelisted: false)),
            Config(), false, 0f, "weapon/ammo/anchor");

        yield return Case("normal + container disabled",
            Input(target: TargetKind.LootableContainer, ammo: Ammo()),
            Config(c => c.OpenLootableContainers = false), false, 0f, "container disabled");

        yield return Case("pleb + container disabled (upstream bypass)",
            Input(target: TargetKind.LootableContainer, ammo: Ammo()),
            Config(c => { c.PlebMode = true; c.OpenLootableContainers = false; }), true, DefaultDamage, "pleb");

        yield return Case("normal + trunk disabled",
            Input(target: TargetKind.Trunk, ammo: Ammo()),
            Config(c => c.OpenCarDoors = false), false, 0f, "trunk disabled");

        yield return Case("explosion damage",
            Input(damage: DamageKind.Explosion, ammo: Ammo()), Config(), false, 0f, "explosion");

        yield return Case("normal + melee + trunk",
            Input(target: TargetKind.Trunk, damage: DamageKind.Melee, weapon: "melee", ammo: null),
            Config(), true, DefaultDamage, "melee");

        yield return Case("normal + bullet without ammo facts",
            Input(ammo: null), Config(), false, 0f, "ammo unknown");
    }

    public static IEnumerable<object[]> DestructionCases()
    {
        yield return DestructionCase("normal wood door", Input(ammo: Ammo()), Config(), true, "accepted");

        yield return DestructionCase("locked wood door", Input(ammo: Ammo(), state: DoorStateKind.Locked),
            Config(), true, "locked");

        // 默认关闭态：安全类（总开关关）全部拒绝，原因带 safety 前缀。
        yield return DestructionCase("keycard door (safety off)", Input(ammo: Ammo(), doorClass: DoorClassKind.Keycard),
            Config(), false, "safety");

        yield return DestructionCase("sliding door (safety off)", Input(ammo: Ammo(), doorClass: DoorClassKind.Sliding),
            Config(), false, "safety");

        yield return DestructionCase("door switch (safety off)", Input(ammo: Ammo(), doorClass: DoorClassKind.DoorSwitch),
            Config(), false, "safety");

        yield return DestructionCase("non-operatable door (safety off)", Input(ammo: Ammo(), doorClass: DoorClassKind.NonOperatable),
            Config(), false, "safety");

        yield return DestructionCase("possible double door (safety off)", Input(ammo: Ammo(), doorClass: DoorClassKind.Double),
            Config(), false, "safety");

        // 默认关闭态：高危组（子开关关）拒绝，原因带 high risk 前缀。
        yield return DestructionCase("trigger door (high risk off)", Input(ammo: Ammo(), doorClass: DoorClassKind.Trigger),
            Config(), false, "high risk");

        yield return DestructionCase("extraction door (high risk off)", Input(ammo: Ammo(), doorClass: DoorClassKind.Extraction),
            Config(), false, "high risk");

        // 物理不可行项永不可开关化。
        yield return DestructionCase("excluded door (physical)", Input(ammo: Ammo(), doorClass: DoorClassKind.Excluded),
            Config(), false, "physical");

        yield return DestructionCase("excluded door + both toggles on", Input(ammo: Ammo(), doorClass: DoorClassKind.Excluded),
            Config(c => { c.AllowSafetyExclusions = true; c.AllowHighRiskExclusions = true; }), false, "physical");

        // 仅安全类总开关：安全类受理，高危组与物理仍拒绝。
        yield return DestructionCase("keycard door + safety on", Input(ammo: Ammo(), doorClass: DoorClassKind.Keycard),
            Config(c => c.AllowSafetyExclusions = true), true, "accepted");

        yield return DestructionCase("double door + safety on", Input(ammo: Ammo(), doorClass: DoorClassKind.Double),
            Config(c => c.AllowSafetyExclusions = true), true, "accepted");

        yield return DestructionCase("trigger door + safety on", Input(ammo: Ammo(), doorClass: DoorClassKind.Trigger),
            Config(c => c.AllowSafetyExclusions = true), false, "high risk");

        yield return DestructionCase("extraction door + safety on", Input(ammo: Ammo(), doorClass: DoorClassKind.Extraction),
            Config(c => c.AllowSafetyExclusions = true), false, "high risk");

        // 仅高危组子开关：高危组受理，安全类与物理仍拒绝。
        yield return DestructionCase("trigger door + high risk on", Input(ammo: Ammo(), doorClass: DoorClassKind.Trigger),
            Config(c => c.AllowHighRiskExclusions = true), true, "accepted");

        yield return DestructionCase("extraction door + high risk on", Input(ammo: Ammo(), doorClass: DoorClassKind.Extraction),
            Config(c => c.AllowHighRiskExclusions = true), true, "accepted");

        yield return DestructionCase("keycard door + high risk on", Input(ammo: Ammo(), doorClass: DoorClassKind.Keycard),
            Config(c => c.AllowHighRiskExclusions = true), false, "safety");

        // 两者都开：安全类 + 高危组受理，物理仍拒绝。
        yield return DestructionCase("keycard door + both on", Input(ammo: Ammo(), doorClass: DoorClassKind.Keycard),
            Config(c => { c.AllowSafetyExclusions = true; c.AllowHighRiskExclusions = true; }), true, "accepted");

        yield return DestructionCase("extraction door + both on", Input(ammo: Ammo(), doorClass: DoorClassKind.Extraction),
            Config(c => { c.AllowSafetyExclusions = true; c.AllowHighRiskExclusions = true; }), true, "accepted");

        yield return DestructionCase("material Other", Input(ammo: Ammo(), material: MaterialKind.Other),
            Config(), false, "material");

        // 厚金属没有材质档案（ProfileFor 返回 null），保持原版。
        yield return DestructionCase("material MetalThick", Input(ammo: Ammo(), material: MaterialKind.MetalThick),
            Config(), false, "material");

        yield return DestructionCase("destruction disabled", Input(ammo: Ammo()),
            Config(c => c.DestructionEnabled = false), false, "disabled");

        yield return DestructionCase("non-door target", Input(target: TargetKind.Trunk, ammo: Ammo()),
            Config(), false, "not a door");
    }

    private static object[] Case(
        string scenario,
        AttackInput input,
        RuleConfig config,
        bool expectedAccepted,
        float expectedDamage,
        string expectedReason)
    {
        return new object[] { scenario, input, config, expectedAccepted, expectedDamage, expectedReason };
    }

    private static object[] DestructionCase(
        string scenario,
        AttackInput input,
        RuleConfig config,
        bool expectedAccepted,
        string expectedReason)
    {
        return new object[] { scenario, input, config, expectedAccepted, expectedReason };
    }

    private static RuleConfig Config(Action<RuleConfig>? customize = null)
    {
        var config = new RuleConfig
        {
            ShotgunWeapons = new HashSet<string> { "shotgun" },
            GrenadeLauncherWeapons = new HashSet<string> { "gl" },
            MeleeWeapons = new HashSet<string> { "melee" },
            OtherWeapons = new HashSet<string> { "rifle" },
            WhitelistedRounds = new HashSet<string> { "round" },
            BlacklistedRooms = new HashSet<string> { "black" },
            MinHitPoints = 100,
            MaxHitPoints = 200,
        };

        customize?.Invoke(config);
        return config;
    }

    private static AttackInput Input(
        TargetKind target = TargetKind.Door,
        MaterialKind material = MaterialKind.WoodThin,
        DamageKind damage = DamageKind.Bullet,
        string weapon = "rifle",
        bool shotgun = false,
        AmmoFacts? ammo = null,
        bool hasAnchor = true,
        bool anchorHit = true,
        DoorStateKind state = DoorStateKind.Shut,
        DoorClassKind doorClass = DoorClassKind.Normal,
        string keyId = "",
        float amount = DefaultDamage)
    {
        return new AttackInput
        {
            Target = target,
            Material = material,
            DamageType = damage,
            Weapon = new WeaponFacts { TemplateId = weapon, IsShotgun = shotgun },
            Ammo = ammo,
            HasLockAnchor = hasAnchor,
            IsLockAnchorHit = anchorHit,
            DoorState = state,
            DoorClass = doorClass,
            KeyId = keyId,
            DamageAmount = amount,
        };
    }

    private static AmmoFacts Ammo(
        bool whitelisted = true,
        bool he = false,
        bool shrapnel = false,
        bool breaching = false)
    {
        return new AmmoFacts
        {
            TemplateId = whitelisted ? "round" : "non_whitelisted_round",
            IsHEGrenade = he,
            IsShrapnel = shrapnel,
            IsBreachingSlug = breaching,
        };
    }
}
