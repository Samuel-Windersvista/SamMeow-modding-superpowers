using InescapableTarkovsSoftcore.Config;
using InescapableTarkovsSoftcore.Features;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>
/// G1 Samuel's Tweaks 变换层测试：给定（组配置 + 合成内存表）→ 断言表变化与组报告。
/// 只测外部行为，不触碰真实 SPT 运行时。
/// </summary>
public class SamuelTweaksModuleTests
{
    // ---------------------------------------------------------------- 规则 1：护甲弹挂冲突修复

    [Fact]
    public void ArmorConflictFix_SetsBlocksArmorVestFalse_OnlyForRigLayoutItems()
    {
        var rig = Rig(10, "SomeRigLayout");
        var plain = Rig(11, null);

        var report = Apply(SamuelConfig(armor: true, armband: false, melee: false, magazines: false), rig, plain);

        Assert.Equal(1, report.ChangedCount);
        Assert.False(rig.Properties.BlocksArmorVest);
        Assert.Null(plain.Properties.BlocksArmorVest);
    }

    [Fact]
    public void ArmorConflictFix_WhenDisabled_LeavesRigUntouched()
    {
        var rig = Rig(10, "SomeRigLayout");

        var report = Apply(SamuelConfig(armor: false, armband: false, melee: false, magazines: false), rig);

        Assert.Equal(0, report.ChangedCount);
        Assert.Null(rig.Properties.BlocksArmorVest);
    }

    // ---------------------------------------------------------------- 规则 2：可掠夺

    [Fact]
    public void MakeLootable_ClearsArmbandAndMelee_ByParent()
    {
        var armband = ParentItem(20, SamuelTweaksModule.ArmbandParentId);
        var melee = ParentItem(21, SamuelTweaksModule.MeleeWeaponParentId);
        var unrelated = ParentItem(22, Hex(99));

        var report = Apply(SamuelConfig(armor: false, armband: true, melee: true, magazines: false), armband, melee, unrelated);

        Assert.Equal(2, report.ChangedCount);
        Assert.False(armband.Properties.Unlootable);
        Assert.Empty(armband.Properties.UnlootableFromSide!);
        Assert.False(melee.Properties.Unlootable);
        Assert.Empty(melee.Properties.UnlootableFromSide!);
        // 未命中父类者不变
        Assert.True(unrelated.Properties.Unlootable);
        Assert.NotEmpty(unrelated.Properties.UnlootableFromSide!);
    }

    [Fact]
    public void MakeLootable_RespectsPerTypeSwitches()
    {
        var armband = ParentItem(20, SamuelTweaksModule.ArmbandParentId);
        var melee = ParentItem(21, SamuelTweaksModule.MeleeWeaponParentId);

        var report = Apply(SamuelConfig(armor: false, armband: true, melee: false, magazines: false), armband, melee);

        Assert.Equal(1, report.ChangedCount);
        Assert.False(armband.Properties.Unlootable);
        Assert.True(melee.Properties.Unlootable);
    }

    [Fact]
    public void MakeLootable_WhenDisabled_LeavesItemsUntouched()
    {
        var armband = ParentItem(20, SamuelTweaksModule.ArmbandParentId);

        var report = Apply(SamuelConfig(armor: false, armband: false, melee: false, magazines: false), armband);

        Assert.Equal(0, report.ChangedCount);
        Assert.True(armband.Properties.Unlootable);
        Assert.NotEmpty(armband.Properties.UnlootableFromSide!);
    }

    [Fact]
    public void MakeLootable_SkipsItemsWithoutUnlootableFlag()
    {
        // 运行时 Unlootable 为非空 bool（缺失默认 false）：未标记为不可掠夺者跳过，UnlootableFromSide 不动。
        var missing = ParentItem(20, SamuelTweaksModule.ArmbandParentId);
        missing.Properties.Unlootable = false;
        var present = ParentItem(21, SamuelTweaksModule.ArmbandParentId);

        var report = Apply(SamuelConfig(armor: false, armband: true, melee: false, magazines: false), missing, present);

        Assert.Equal(1, report.ChangedCount);
        Assert.False(missing.Properties.Unlootable);
        Assert.NotEmpty(missing.Properties.UnlootableFromSide!);
        Assert.False(present.Properties.Unlootable);
    }

    [Fact]
    public void MakeLootable_SkipsArmbandWithNullUnlootableFlag()
    {
        // 4.1.6 Unlootable 为可空 bool?：null（未设置）→ 跳过，不置 false、不清 UnlootableFromSide（D7 表 2）。
        var missing = new TemplateItem
        {
            Id = Hex(20),
            Parent = SamuelTweaksModule.ArmbandParentId,
            Properties = new TemplateItemProperties
            {
                UnlootableFromSide = new[] { default(PlayerSideMask) }
            }
        };

        var report = Apply(SamuelConfig(armor: false, armband: true, melee: false, magazines: false), missing);

        Assert.Equal(0, report.ChangedCount);
        Assert.Null(missing.Properties.Unlootable);
        Assert.NotEmpty(missing.Properties.UnlootableFromSide!);
    }

    // ---------------------------------------------------------------- 规则 3：弹匣缩格

    [Theory]
    [InlineData(10, true)]
    [InlineData(50, true)]
    [InlineData(51, false)]
    [InlineData(9, false)]
    public void MagazineResize_AppliesCapacityBoundaries(double capacity, bool expectedResize)
    {
        var magazine = Magazine(30, width: 1, height: 3, capacity: capacity, extraSizeDown: 3);

        var report = Apply(SamuelConfig(armor: false, armband: false, melee: false, magazines: true), magazine);

        if (expectedResize)
        {
            Assert.Equal(1, report.ChangedCount);
            Assert.Equal(2, magazine.Properties.Height);
            Assert.Equal(2, magazine.Properties.ExtraSizeDown);
        }
        else
        {
            Assert.Equal(0, report.ChangedCount);
            Assert.Equal(3, magazine.Properties.Height);
            Assert.Equal(3, magazine.Properties.ExtraSizeDown);
        }
    }

    [Fact]
    public void MagazineResize_IgnoresNonWideOrShortMagazines()
    {
        var wide = Magazine(32, width: 2, height: 3, capacity: 30, extraSizeDown: 3);
        var shortMag = Magazine(33, width: 1, height: 2, capacity: 30, extraSizeDown: 3);
        var tiny = Magazine(34, width: 1, height: 1, capacity: 30, extraSizeDown: 3);

        var report = Apply(SamuelConfig(armor: false, armband: false, melee: false, magazines: true), wide, shortMag, tiny);

        Assert.Equal(0, report.ChangedCount);
        Assert.Equal(3, wide.Properties.Height);
        Assert.Equal(2, shortMag.Properties.Height);
        Assert.Equal(1, tiny.Properties.Height);
    }

    [Fact]
    public void MagazineResize_IgnoresItemsOutsideMagazineCategory()
    {
        var notAMagazine = Magazine(31, width: 1, height: 3, capacity: 30, extraSizeDown: 3, parent: Hex(99));

        var report = Apply(SamuelConfig(armor: false, armband: false, melee: false, magazines: true), notAMagazine);

        Assert.Equal(0, report.ChangedCount);
        Assert.Equal(3, notAMagazine.Properties.Height);
    }

    [Fact]
    public void MagazineResize_IgnoresMagazineWithoutHeight()
    {
        // 运行时 Height 为非空 int：缺失默认 0 → 已由 Height <= 2 跳过（源 !height || height <= 2）。
        var noHeight = Magazine(35, width: 1, height: 0, capacity: 30, extraSizeDown: 3);

        var report = Apply(SamuelConfig(armor: false, armband: false, melee: false, magazines: true), noHeight);

        Assert.Equal(0, report.ChangedCount);
        Assert.Equal(0, noHeight.Properties.Height);
        Assert.Equal(3, noHeight.Properties.ExtraSizeDown);
    }

    [Fact]
    public void MagazineResize_WhenDisabled_LeavesMagazineUntouched()
    {
        var magazine = Magazine(30, width: 1, height: 5, capacity: 30, extraSizeDown: 3);

        var report = Apply(SamuelConfig(armor: false, armband: false, melee: false, magazines: false), magazine);

        Assert.Equal(0, report.ChangedCount);
        Assert.Equal(5, magazine.Properties.Height);
        Assert.Equal(3, magazine.Properties.ExtraSizeDown);
    }

    [Fact]
    public void MagazineResize_ExtraSizeDownFloor_DoesNotGoNegative()
    {
        var magazine = Magazine(30, width: 1, height: 3, capacity: 30, extraSizeDown: 0);

        var report = Apply(SamuelConfig(armor: false, armband: false, melee: false, magazines: true), magazine);

        Assert.Equal(1, report.ChangedCount);
        Assert.Equal(2, magazine.Properties.Height);
        Assert.Equal(0, magazine.Properties.ExtraSizeDown);
    }

    // ---------------------------------------------------------------- 规则独立开关

    [Fact]
    public void Rules_AreIndependentlySwitchable_OnlyEnabledRuleChanges()
    {
        var rig = Rig(10, "SomeRigLayout");
        var armband = ParentItem(20, SamuelTweaksModule.ArmbandParentId);
        var magazine = Magazine(30, width: 1, height: 3, capacity: 30, extraSizeDown: 3);

        var report = Apply(SamuelConfig(armor: true, armband: false, melee: false, magazines: false), rig, armband, magazine);

        Assert.Equal(1, report.ChangedCount);
        Assert.False(rig.Properties.BlocksArmorVest);
        Assert.True(armband.Properties.Unlootable);
        Assert.Equal(3, magazine.Properties.Height);
    }

    // ---------------------------------------------------------------- 组开关（编排器语义）

    [Fact]
    public void Orchestrator_ZeroChanges_WhenGroupDisabled()
    {
        var rig = Rig(10, "SomeRigLayout");
        var config = new SoftcoreConfig();
        config.SamuelTweaks.Enabled = false;

        var table = Table(rig);
        var orchestrator = new ModuleOrchestrator(
            [new SamuelTweaksModule(new RecordingLogger<SamuelTweaksModule>())],
            new RecordingLogger<ModuleOrchestrator>(),
            new ModTables(table, null!, null!, null!, null!, null!));

        var report = orchestrator.Run(config);

        Assert.Equal(0, report.TotalChanged);
        Assert.Null(rig.Properties.BlocksArmorVest);
        Assert.Contains("samuelTweaks", report.SkippedModuleIds);
    }

    [Fact]
    public void Module_MetadataAndInjectable()
    {
        var module = new SamuelTweaksModule(new RecordingLogger<SamuelTweaksModule>());

        Assert.Equal("samuelTweaks", module.Id);
        Assert.Equal(200, module.Order);
        Assert.True(module.IsEnabled(new SoftcoreConfig()));

        var injectable = typeof(SamuelTweaksModule)
            .GetCustomAttributes(typeof(SPTarkov.DI.Annotations.Injectable), inherit: false)
            .Cast<SPTarkov.DI.Annotations.Injectable>()
            .Single();
        Assert.Equal(SPTarkov.DI.Annotations.InjectionType.Singleton, injectable.InjectionType);
    }

    [Fact]
    public void ParentConstants_MatchDatabaseTruth()
    {
        // DB 真值：5b3f15d486f77432d0509248 = ArmBand 类；5447e1d04bdc2dff2f8b4567 = Melee 类。
        // 来源 mod 原始代码两常量标反，此处字面值断言防回归（D5）。
        Assert.Equal("5b3f15d486f77432d0509248", SamuelTweaksModule.ArmbandParentId);
        Assert.Equal("5447e1d04bdc2dff2f8b4567", SamuelTweaksModule.MeleeWeaponParentId);
    }

    // ---------------------------------------------------------------- 启动器背景静态件
    // 启动器背景（customBackground）为 v0.1.0 移除项（spec Out of Scope）；
    // 5.0 的资产存在性 / build.ps1 拷贝断言随该功能一并移除。

    // ---------------------------------------------------------------- 辅助

    private static ModuleReport Apply(SamuelTweaksConfig section, params TemplateItem[] items)
    {
        var config = new SoftcoreConfig { SamuelTweaks = section };
        var context = new ModContext(config, new ModTables(Table(items), null!, null!, null!, null!, null!));
        return new SamuelTweaksModule(new RecordingLogger<SamuelTweaksModule>()).Apply(context);
    }

    private static SamuelTweaksConfig SamuelConfig(bool armor, bool armband, bool melee, bool magazines) =>
        new()
        {
            ArmorConflictFix = armor,
            LootableItems = new LootableItemsConfig { Armband = armband, MeleeWeapons = melee },
            MagazineResize = new MagazineResizeConfig { Enabled = magazines, MinCapacity = 10, MaxCapacity = 50 }
        };

    private static TemplateItem Rig(int id, string? rigLayoutName) =>
        new()
        {
            Id = Hex(id),
            Parent = Hex(1),
            Properties = new TemplateItemProperties { RigLayoutName = rigLayoutName }
        };

    private static TemplateItem ParentItem(int id, string parentId) =>
        new()
        {
            Id = Hex(id),
            Parent = parentId,
            Properties = new TemplateItemProperties
            {
                Unlootable = true,
                UnlootableFromSide = new[] { default(PlayerSideMask) }
            }
        };

    private static TemplateItem Magazine(int id, int width, int height, double? capacity, int extraSizeDown, string parent = SamuelTweaksModule.MagazineParentId) =>
        new()
        {
            Id = Hex(id),
            Parent = parent,
            Properties = new TemplateItemProperties
            {
                Width = width,
                Height = height,
                ExtraSizeDown = extraSizeDown,
                Cartridges = capacity is null
                    ? null!
                    : new List<Slot>
                    {
                        new() { Id = Hex(2), Parent = Hex(3), Properties = null!, MaxCount = capacity }
                    }
            }
        };

    /// <summary>把序号转成合法 24 位 hex MongoId 字符串，避免手写长 ID。</summary>
    private static string Hex(int id) => id.ToString("x24");

    private static TemplateTable Table(params TemplateItem[] items)
    {
        var dictionary = new Dictionary<MongoId, TemplateItem>();
        foreach (var item in items)
        {
            dictionary[item.Id] = item;
        }

        return new TemplateTable
        {
            Character = null!,
            CustomisationStorage = null!,
            Items = dictionary,
            Prestige = null!,
            Quests = null!,
            RepeatableQuests = null!,
            Handbook = null!,
            Customization = null!,
            Dialogue = null!,
            Profiles = null!,
            Prices = null!,
            DefaultEquipmentPresets = null!,
            Achievements = null!,
            CustomAchievements = null!,
            LocationServices = null!
        };
    }

}
