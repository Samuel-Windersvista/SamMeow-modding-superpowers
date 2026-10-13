using System.Runtime.CompilerServices;
using InescapableTarkovsSoftcore.Config;
using InescapableTarkovsSoftcore.Features.Softcore;
using InescapableTarkovsSoftcore.Features.Softcore.Changers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>T11：G6-D 杂项（SURV 终值）。</summary>
public class SoftcoreOtherTweaksTests
{
    private const string GoldenTt = "5b3b713c5acfc4330140bd8d";
    private const string CrisisQuestId = "60e71c48c1bfa3050473b8e5";

    [Fact]
    public void BiggerAmmoStacks_MultipliesByFive_AndSkipsZero()
    {
        var context = NewContext(out var templates);
        templates.Items[Id(1)] = Item(Id(1), (string)BaseClasses.AMMO, stack: 30);
        templates.Items[Id(2)] = Item(Id(2), (string)BaseClasses.AMMO, stack: 0);
        templates.Items[Id(3)] = Item(Id(3), "0000000000000000000000aa", stack: 30);

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(150, templates.Items[Id(1)].Properties!.StackMaxSize);
        Assert.Equal(0, templates.Items[Id(2)].Properties!.StackMaxSize);
        Assert.Equal(30, templates.Items[Id(3)].Properties!.StackMaxSize);
    }

    [Fact]
    public void BiggerAmmoStacks_NullStackMaxSize_IsSkipped_NoThrow()
    {
        // 4.1.6 StackMaxSize 为可空 int?：null（未设置）→ 跳过、值不变、无异常（D7 表 1）。
        var context = NewContext(out var templates);
        templates.Items[Id(1)] = new SPTarkov.Server.Core.Models.Eft.Common.Tables.TemplateItem
        {
            Id = Id(1),
            Parent = (string)BaseClasses.AMMO,
            Type = "Item",
            Properties = new SPTarkov.Server.Core.Models.Eft.Common.Tables.TemplateItemProperties()
        };

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Null(templates.Items[Id(1)].Properties!.StackMaxSize);
    }

    [Fact]
    public void VestsBlockArmor_DefaultFalse_ClearsBlocksArmorVest()
    {
        var context = NewContext(out var templates);
        var rig = SoftcoreTestData.NewTweakItem(Id(1), "0000000000000000000000aa", rigLayoutName: "SomeRig", blocksArmorVest: true);
        var plain = SoftcoreTestData.NewTweakItem(Id(2), "0000000000000000000000aa", blocksArmorVest: true);
        templates.Items[rig.Id] = rig;
        templates.Items[plain.Id] = plain;

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.False(rig.Properties!.BlocksArmorVest);
        Assert.True(plain.Properties!.BlocksArmorVest);
    }

    [Fact]
    public void VestsBlockArmor_WhenTrue_LeavesItemsUnchanged()
    {
        var context = NewContext(out var templates);
        context.Config.OtherTweaks.VestsBlockArmor = true;
        var rig = SoftcoreTestData.NewTweakItem(Id(1), "0000000000000000000000aa", rigLayoutName: "SomeRig", blocksArmorVest: true);
        templates.Items[rig.Id] = rig;

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.True(rig.Properties!.BlocksArmorVest);
    }

    [Fact]
    public void SignalPistol_IsPushedToBothSpecialPockets()
    {
        var context = NewContext(out var templates);
        templates.Items[ItemTpl.POCKETS_1X4_SPECIAL] = SoftcoreTestData.NewPocket(ItemTpl.POCKETS_1X4_SPECIAL, ["0000000000000000000000aa"]);
        templates.Items[ItemTpl.POCKETS_1X4_TUE] = SoftcoreTestData.NewPocket(ItemTpl.POCKETS_1X4_TUE, ["0000000000000000000000aa"]);

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Contains(
            (MongoId)ItemTpl.SIGNALPISTOL_ZID_SP81_26X75_SIGNAL_PISTOL,
            FilterOf(templates, ItemTpl.POCKETS_1X4_SPECIAL));
        Assert.Contains(
            (MongoId)ItemTpl.SIGNALPISTOL_ZID_SP81_26X75_SIGNAL_PISTOL,
            FilterOf(templates, ItemTpl.POCKETS_1X4_TUE));
    }

    [Fact]
    public void Reshala_GetsGoldenTt()
    {
        var context = NewContext(out _, bots: SoftcoreTestData.NewBotTable("bossbully", SoftcoreTestData.NewBoss()));

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        var boss = context.Tables.Bots!.Types["bossbully"]!;
        Assert.Equal(100d, boss.BotChances.EquipmentChances["Holster"]);
        var holster = boss.BotInventory.Equipment[EquipmentSlots.Holster];
        Assert.Equal(1d, holster[(MongoId)GoldenTt]);
        Assert.Single(holster);
    }

    [Fact]
    public void Reshala_MissingBotsTable_Warns()
    {
        var context = NewContext(out _);
        var log = new SoftcoreChangeLog { Changer = new OtherTweaksChanger().Name };

        new OtherTweaksChanger().Apply(context, log);

        Assert.Contains(log.Warnings, w => w.Contains("bots", StringComparison.Ordinal));
    }

    [Fact]
    public void QuestChanges_AppliesCrisisAndDripOut_ById()
    {
        var context = NewContext(out var templates);

        // Crisis：源设 A4S[1].value=30（夹具保留 2 条以验证 +30 路径）。
        var crisis = SoftcoreTestData.NewQuest(
            CrisisQuestId,
            $"{CrisisQuestId} name",
            [SoftcoreTestData.NewCondition("Level", 55), SoftcoreTestData.NewCondition("Level", 0)],
            []);
        templates.Quests[CrisisQuestId] = crisis;

        // 4 个 Drip-Out：生产形态（id + 本地化键名；A4F HandoverItem=50 / CounterCreator=100）。
        var dripOutIds = new[]
        {
            "6613f3007f6666d56807c929",
            "6613f307fca4f2f386029409",
            "66151401efb0539ae10875ae",
            "6615141bfda04449120269a7"
        };
        foreach (var id in dripOutIds)
        {
            templates.Quests[id] = SoftcoreTestData.NewQuest(
                id,
                $"{id} name",
                [],
                [SoftcoreTestData.NewCondition("HandoverItem", 50), SoftcoreTestData.NewCondition("CounterCreator", 100)]);
        }

        // 名为 "Drip-Out"（旧式人类名）但 id 不符 → 不应被改动（按 id 匹配）。
        var imposter = SoftcoreTestData.NewQuest(
            Id(2), "Drip-Out", [], [SoftcoreTestData.NewCondition("HandoverItem", 50)]);
        templates.Quests[Id(2)] = imposter;

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(30d, crisis.Conditions.AvailableForStart[1].Value);
        foreach (var id in dripOutIds)
        {
            var conditions = templates.Quests[id].Conditions.AvailableForFinish;
            Assert.Equal(10d, conditions.Single(c => c.ConditionType == "HandoverItem").Value);
            Assert.Equal(20d, conditions.Single(c => c.ConditionType == "CounterCreator").Value);
        }

        Assert.Equal(50d, imposter.Conditions.AvailableForFinish[0].Value);
    }

    [Fact]
    public void QuestChanges_MissingDripOutIds_Warn()
    {
        var context = NewContext(out _);
        var log = new SoftcoreChangeLog { Changer = new OtherTweaksChanger().Name };

        new OtherTweaksChanger().Apply(context, log);

        Assert.Contains(log.Warnings, w => w.Contains("6613f3007f6666d56807c929", StringComparison.Ordinal));
        Assert.Contains(log.Warnings, w => w.Contains("6615141bfda04449120269a7", StringComparison.Ordinal));
    }

    [Fact]
    public void FasterExamineTime_SetsPointTwo_OnlyForNonZero()
    {
        var context = NewContext(out var templates);
        var examined = SoftcoreTestData.NewTweakItem(Id(1), "0000000000000000000000aa", examineTime: 2.5);
        var instant = SoftcoreTestData.NewTweakItem(Id(2), "0000000000000000000000aa", examineTime: 0);
        templates.Items[examined.Id] = examined;
        templates.Items[instant.Id] = instant;

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(0.2d, examined.Properties!.ExamineTime);
        Assert.Equal(0d, instant.Properties!.ExamineTime);
    }

    [Fact]
    public void RemoveDiscardLimit_SetsMinusOne_ForItems()
    {
        var context = NewContext(out var templates);
        var item = SoftcoreTestData.NewTweakItem(Id(1), "0000000000000000000000aa", discardLimit: 3);
        var node = SoftcoreTestData.NewTweakItem(Id(2), "0000000000000000000000aa", discardLimit: 3, type: "Node");
        templates.Items[item.Id] = item;
        templates.Items[node.Id] = node;

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(-1d, item.Properties!.DiscardLimit);
        Assert.Equal(3d, node.Properties!.DiscardLimit);
    }

    [Fact]
    public void RemoveBackpackRestrictions_ClearsAmmoCaseExclusion()
    {
        var context = NewContext(out var templates);
        var restricted = SoftcoreTestData.NewTweakItem(
            Id(1),
            "0000000000000000000000aa",
            gridFilters: [SoftcoreTestData.NewGridFilter(excludedFilter: [ItemTpl.CONTAINER_AMMUNITION_CASE, "0000000000000000000000bb"])]);
        var other = SoftcoreTestData.NewTweakItem(
            Id(2),
            "0000000000000000000000aa",
            gridFilters: [SoftcoreTestData.NewGridFilter(excludedFilter: ["0000000000000000000000bb"])]);
        templates.Items[restricted.Id] = restricted;
        templates.Items[other.Id] = other;

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Empty(FirstExcludedFilter(restricted));
        Assert.Contains((MongoId)"0000000000000000000000bb", FirstExcludedFilter(other));
    }

    [Fact]
    public void RemoveRaidItemLimits_EmptiesRestrictions()
    {
        var global = SoftcoreTestData.NewGlobalTable(1);
        global.Configuration.RestrictionsInRaid =
            [new RestrictionsInRaid { TemplateId = "0000000000000000000000aa", MaxInLobby = 1, MaxInRaid = 1 }];
        var context = NewContext(out _, global: global);

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Empty(global.Configuration.RestrictionsInRaid);
    }

    [Fact]
    public void CurrencyStacks_DefaultOff_NoChange()
    {
        var context = NewContext(out var templates);
        templates.Items[ItemTpl.MONEY_ROUBLES] = Item(ItemTpl.MONEY_ROUBLES, "0000000000000000000000aa", stack: 50000);

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(50000, templates.Items[ItemTpl.MONEY_ROUBLES].Properties!.StackMaxSize);
    }

    [Fact]
    public void CurrencyStacks_WhenEnabled_SetsValues()
    {
        var context = NewContext(out var templates);
        context.Config.OtherTweaks.BiggerCurrencyStacks = true;
        templates.Items[ItemTpl.MONEY_ROUBLES] = Item(ItemTpl.MONEY_ROUBLES, "0000000000000000000000aa", stack: 50000);
        templates.Items[ItemTpl.MONEY_GP_COIN] = Item(ItemTpl.MONEY_GP_COIN, "0000000000000000000000aa", stack: 1);

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(1000000, templates.Items[ItemTpl.MONEY_ROUBLES].Properties!.StackMaxSize);
        Assert.Equal(100, templates.Items[ItemTpl.MONEY_GP_COIN].Properties!.StackMaxSize);
    }

    [Fact]
    public void SkillExpBuffs_DefaultOff_NoChange()
    {
        var global = NewGlobalWithSkills();
        var context = NewContext(out _, global: global);

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(2d, global.Configuration.SkillsSettings.Vitality.DamageTakenAction);
    }

    [Fact]
    public void SkillExpBuffs_WhenEnabled_Multiplies()
    {
        var global = NewGlobalWithSkills();
        var context = NewContext(out _, global: global);
        context.Config.OtherTweaks.SkillExpBuffs = true;

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.Equal(20d, global.Configuration.SkillsSettings.Vitality.DamageTakenAction);
        Assert.Equal(20d, global.Configuration.SkillsSettings.Sniper.WeaponShotAction);
        Assert.Equal(20d, global.Configuration.SkillsSettings.Surgery.SurgeryAction);
        Assert.Equal(200d, global.Configuration.SkillsSettings.WeaponTreatment.SkillPointsPerRepair);
        // 源 TS 的 MagDrills forEach 为无效操作 → 忠实保留不改。
        Assert.Equal(3d, global.Configuration.SkillsSettings.MagDrills.RaidLoadedAmmoAction);
    }

    [Fact]
    public void UnexaminedItemsAreBack_DefaultOff_NoChange()
    {
        var context = NewContext(out var templates);
        var item = SoftcoreTestData.NewTweakItem(Id(1), "0000000000000000000000aa", examinedByDefault: true);
        templates.Items[item.Id] = item;

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.True(item.Properties!.ExaminedByDefault);
    }

    [Fact]
    public void SmallContainersInSpecialSlots_DefaultOff_NoChange()
    {
        var context = NewContext(out var templates);
        templates.Items[ItemTpl.POCKETS_1X4_SPECIAL] = SoftcoreTestData.NewPocket(ItemTpl.POCKETS_1X4_SPECIAL, []);

        new OtherTweaksChanger().Apply(context, new SoftcoreChangeLog());

        Assert.DoesNotContain((MongoId)ItemTpl.CONTAINER_KEY_TOOL, FilterOf(templates, ItemTpl.POCKETS_1X4_SPECIAL));
    }

    [Fact]
    public void Disabled_ProducesZeroChanges()
    {
        var context = NewContext(out var templates);
        context.Config.OtherTweaks.Enabled = false;
        templates.Items[Id(1)] = Item(Id(1), (string)BaseClasses.AMMO, stack: 30);

        var log = new SoftcoreChangeLog();
        new OtherTweaksChanger().Apply(context, log);

        Assert.Equal(0, log.ChangedCount);
        Assert.Equal(30, templates.Items[Id(1)].Properties!.StackMaxSize);
    }

    private static string Id(int index) => $"000000000000000000000{index:D3}";

    private static SPTarkov.Server.Core.Models.Eft.Common.Tables.TemplateItem Item(string id, string parent, int stack) =>
        new()
        {
            Id = id,
            Parent = parent,
            Type = "Item",
            Properties = new SPTarkov.Server.Core.Models.Eft.Common.Tables.TemplateItemProperties { StackMaxSize = stack }
        };

    private static System.Collections.Generic.HashSet<MongoId> FilterOf(
        SPTarkov.Server.Core.Models.Spt.Tables.TemplateTable templates,
        string pocket) =>
        templates.Items[pocket].Properties!.Slots!.First().Properties!.Filters!.First().Filter!;

    private static System.Collections.Generic.HashSet<MongoId> FirstExcludedFilter(
        SPTarkov.Server.Core.Models.Eft.Common.Tables.TemplateItem item) =>
        item.Properties!.Grids!.First().Properties!.Filters!.First().ExcludedFilter!;

    private static GlobalTable NewGlobalWithSkills()
    {
        var global = SoftcoreTestData.NewGlobalTable(1);
        var skills = (SkillsSettings)RuntimeHelpers.GetUninitializedObject(typeof(SkillsSettings));
        skills.Vitality = Skill<Vitality>(v => v.DamageTakenAction = 2);
        skills.Sniper = Skill<WeaponSkills>(v => v.WeaponShotAction = 2);
        skills.Surgery = Skill<Surgery>(v => v.SurgeryAction = 2);
        skills.WeaponTreatment = Skill<WeaponTreatment>(v => v.SkillPointsPerRepair = 2);
        skills.MagDrills = Skill<MagDrills>(v => v.RaidLoadedAmmoAction = 3);
        global.Configuration.SkillsSettings = skills;
        return global;
    }

    private static T Skill<T>(Action<T> configure) where T : class
    {
        var instance = (T)RuntimeHelpers.GetUninitializedObject(typeof(T));
        configure(instance);
        return instance;
    }

    private static SoftcoreContext NewContext(
        out SPTarkov.Server.Core.Models.Spt.Tables.TemplateTable templates,
        GlobalTable? global = null,
        BotTable? bots = null)
    {
        templates = SoftcoreTestData.NewTemplates();
        return SoftcoreTestData.NewContext(
            templates, SoftcoreTestData.NewHideout(), SoftcoreTestData.NewTraders(),
            SoftcoreTestData.NewHideoutConfig(), global: global, bots: bots);
    }
}
