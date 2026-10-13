using InescapableTarkovsSoftcore.Config;
using InescapableTarkovsSoftcore.Features.Softcore;
using System.Runtime.CompilerServices;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums.Hideout;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Json;
using Xunit;

namespace InescapableTarkovsSoftcore.Tests;

/// <summary>
/// Softcore 测试夹具：只构造子变换器实际读取的成员，其余必填成员以 null!/空集合占位，
/// 从而无需运行 SPT 服务端即可构造真实 POCO 图。
/// </summary>
internal static class SoftcoreTestData
{
    /// <summary>MongoId 必须是 24 位 hex；以下为测试专用占位 id。</summary>
    public const string CollectorQuestId = "5c51aac186f77432ea65c552";

    public const string SecuredItemId = "000000000000000000000001";

    public const string StashItemId = "000000000000000000000002";

    public const string AssortItemId = "000000000000000000000003";

    public static TemplateTable NewTemplates(HandbookBase? handbook = null) => new()
    {
        Items = new Dictionary<MongoId, TemplateItem>(),
        Profiles = new Dictionary<string, ProfileSides>(),
        Quests = new Dictionary<MongoId, Quest>(),
        Character = null!,
        CustomisationStorage = null!,
        Prestige = null!,
        RepeatableQuests = null!,
        Handbook = handbook!,
        Customization = null!,
        Dialogue = null!,
        Prices = new Dictionary<MongoId, double>(),
        DefaultEquipmentPresets = null!,
        Achievements = null!,
        CustomAchievements = null!,
        LocationServices = null!
    };

    public static HideoutTable NewHideout(HideoutSettingsBase? settings = null) => new()
    {
        Areas = [],
        Production = new HideoutProductionData { Recipes = [], ScavRecipes = null!, CultistRecipes = null! },
        CustomAreas = null!,
        Customisation = null!,
        Settings = settings!,
        Qte = null!
    };

    public static TradersTable NewTraders() => new();

    public static HideoutConfig NewHideoutConfig(params string[] cultistRewardItems) => new()
    {
        CultistCircle = new CultistCircleSettings
        {
            DirectRewards =
            [
                new DirectRewardSettings
                {
                    RequiredItems = cultistRewardItems.Select(item => (MongoId)item).ToList(),
                    Reward = null!,
                    CraftTimeSeconds = 0,
                    Repeatable = false
                }
            ],
            RewardPriceMultiplierMinMax = null!,
            CraftTimeThresholds = null!,
            DirectRewardStackSize = null!,
            RewardItemBlacklist = null!,
            AdditionalRewardItemPool = null!,
            CurrencyRewards = null!
        },
        RunIntervalValues = null!,
        HideoutCraftsToAdd = null!
    };

    public static TemplateItem NewContainer(string template, int cellsH, int cellsV, params string[] filter) => new()
    {
        Id = template,
        Properties = new TemplateItemProperties
        {
            Grids =
            [
                new Grid
                {
                    Properties = new GridProperties
                    {
                        CellsH = cellsH,
                        CellsV = cellsV,
                        Filters = [new GridFilter { Filter = [.. filter.Select(item => (MongoId)item)] }]
                    }
                }
            ]
        }
    };

    public static TemplateItem NewStash(string template, int cellsV) => new()
    {
        Id = template,
        Properties = new TemplateItemProperties
        {
            Grids = [new Grid { Properties = new GridProperties { CellsH = 10, CellsV = cellsV } }]
        }
    };

    public static ProfileSides NewProfile(string securedContainerTemplate, string stashTemplate, int stashLevel) => new()
    {
        Bear = NewSide(securedContainerTemplate, stashTemplate, stashLevel),
        Usec = NewSide(securedContainerTemplate, stashTemplate, stashLevel)
    };

    public static TemplateSide NewSide(string securedContainerTemplate, string stashTemplate, int stashLevel) => new()
    {
        Character = new PmcData
        {
            Inventory = new BotBaseInventory
            {
                Items =
                [
                    new Item { Id = SecuredItemId, SlotId = "SecuredContainer", Template = securedContainerTemplate },
                    new Item { Id = StashItemId, Template = stashTemplate }
                ]
            },
            Hideout = new Hideout
            {
                Areas = [new BotHideoutArea { Type = HideoutAreas.Stash, Level = stashLevel }]
            },
            Bonuses = []
        }
    };

    /// <summary>收藏家任务夹具：生产形态 —— 5.0 的 quests.name 是本地化键「&lt;id&gt; name」。</summary>
    public static Quest NewCollectorQuest() => new()
    {
        Id = CollectorQuestId,
        Name = $"{CollectorQuestId} name",
        Conditions = new QuestConditionTypes
        {
            AvailableForFinish = [],
            AvailableForStart = [],
            Fail = []
        },
        CanShowNotificationsInGame = false,
        Description = null!,
        FailMessageText = null!,
        Note = null!,
        TraderId = "54cb50c76803fa8b248b4571",
        Location = null!,
        Image = null!,
        Type = default,
        IsKey = false,
        Restartable = false,
        InstantComplete = false,
        SecretQuest = false,
        StartedMessageText = null!,
        SuccessMessageText = null!,
        DeclinePlayerMessage = null!,
        CompletePlayerMessage = null!,
        Status = default,
        ChangeQuestMessageText = null!,
        Side = null!,
        ProgressSource = null!,
        RankingModes = null!,
        GameModes = null!,
        ArenaLocations = null!
    };

    public static HideoutArea NewStashArea(params StageRequirement[] requirements) => new()
    {
        Type = HideoutAreas.Stash,
        IsEnabled = true,
        NeedsFuel = false,
        Requirements = [],
        IsTakeFromSlotLocked = false,
        CraftGivesExperience = false,
        DisplayLevel = false,
        EnableAreaRequirements = false,
        Stages = new Dictionary<string, Stage>
        {
            ["1"] = new Stage
            {
                Requirements = [.. requirements],
                AutoUpgrade = false,
                Bonuses = [],
                ConstructionTime = 0,
                Container = default,
                Description = null!,
                DisplayInterface = false,
                Improvements = [],
                Slots = 0
            }
        }
    };

    public static Trader NewPeacekeeper(string betaTemplate) => new()
    {
        Assort = new TraderAssort { Items = [new Item { Id = AssortItemId, Template = betaTemplate }] },
        Base = null!,
        Dialogue = null!,
        QuestAssort = null!
    };

    public static SoftcoreContext NewContext(
        TemplateTable templates,
        HideoutTable hideout,
        TradersTable traders,
        HideoutConfig hideoutConfig,
        SoftcoreModuleConfig? config = null,
        ScavCaseConfig? scavCase = null,
        GlobalTable? global = null,
        RagfairConfig? ragfair = null,
        TraderConfig? trader = null,
        InsuranceConfig? insurance = null,
        BotTable? bots = null) => new()
    {
        Config = config ?? new SoftcoreModuleConfig(),
        Tables = new SoftcoreTables
        {
            Templates = templates,
            Hideout = hideout,
            Traders = traders,
            Global = global,
            Bots = bots
        },
        Services = new SoftcoreServices
        {
            HideoutConfig = hideoutConfig,
            ScavCase = scavCase,
            Ragfair = ragfair,
            Trader = trader,
            Insurance = insurance
        }
    };

    public static ListOrT<string> ListOf(params string[] items) => new([.. items], null);

    // ---- G6-C 夹具（仅构造子变换器读取的成员，其余必填成员 null! 占位） ----

    public static RagfairConfig NewRagfairConfig() => new()
    {
        Sell = new SPTarkov.Server.Core.Models.Spt.Config.Sell
        {
            Chance = new SPTarkov.Server.Core.Models.Spt.Config.Chance(),
            Time = new MinMax<double>()
        },
        Dynamic = new SPTarkov.Server.Core.Models.Spt.Config.Dynamic
        {
            Barter = new BarterDetails { ItemTplBlacklist = [], ItemTypeBlacklist = [] },
            Pack = null!,
            OfferAdjustment = null!,
            OfferItemCount = new Dictionary<string, MinMax<int>>(),
            PriceRanges = new PriceRanges { Default = new MinMax<double>(), Preset = new MinMax<double>(), Pack = new MinMax<double>() },
            IgnoreQualityPriceVarianceBlacklist = [],
            EndTimeSeconds = new MinMax<int>(),
            Condition = new Dictionary<MongoId, SPTarkov.Server.Core.Models.Spt.Config.Condition>(),
            StackablePercent = new MinMax<double>(),
            NonStackableCount = new MinMax<int>(),
            Rating = new MinMax<double>(),
            Armor = null!,
            OfferCurrencyChangePercent = null!,
            ShowAsSingleStack = [],
            Blacklist = new RagfairBlacklist { Custom = [], ArmorPlate = null!, CustomItemCategoryList = [] },
            UnreasonableModPrices = null!,
            ItemPriceOverrideRouble = new Dictionary<MongoId, double>()
        },
        RunIntervalValues = null!,
        Traders = new Dictionary<MongoId, bool>(),
        // 4.1.6 RagfairConfig 新增 required 成员 TieredFlea；测试不涉及，空表占位。
        TieredFlea = new TieredFlea { UnlocksTpl = new Dictionary<MongoId, int>(), UnlocksType = new Dictionary<MongoId, int>() }
    };

    public static TraderConfig NewTraderConfig() => new()
    {
        Fence = new FenceConfig
        {
            DiscountOptions = new DiscountOptions
            {
                WeaponPresetMinMax = new MinMax<int>(),
                EquipmentPresetMinMax = new MinMax<int>()
            },
            WeaponPresetMinMax = new MinMax<int>(),
            EquipmentPresetMinMax = new MinMax<int>(),
            ArmorMaxDurabilityPercentMinMax = null!,
            WeaponDurabilityPercentMinMax = null!,
            ChancePlateExistsInArmorPercent = null!,
            ItemStackSizeOverrideMinMax = null!,
            ItemTypeLimits = new Dictionary<MongoId, int>(),
            PreventDuplicateOffersOfCategory = [],
            ItemCategoryRoublePriceLimit = null!,
            PresetSlotsToRemoveChancePercent = null!,
            Blacklist = [],
            CoopExtractGift = null!
        }
    };

    public static InsuranceConfig NewInsuranceConfig() => new()
    {
        ReturnChancePercent = new Dictionary<MongoId, double>()
    };

    public static TemplateItem NewTemplateItem(string id, string parent, bool questItem = false, string type = "Item") => new()
    {
        Id = id,
        Parent = parent,
        Type = type,
        Properties = new TemplateItemProperties { QuestItem = questItem }
    };

    public static HandbookItem NewHandbookItem(string id, double price) => new()
    {
        Id = id,
        ParentId = "",
        Price = price
    };

    public static Trader NewTraderWithAssort(params string[] templates) => new()
    {
        Assort = new TraderAssort
        {
            Items = [.. templates.Select((template, index) => new Item
            {
                Id = $"00000000000000000000e{index:D3}",
                Template = template
            })]
        },
        Base = null!,
        Dialogue = null!,
        QuestAssort = null!
    };

    // ---- G6-B 夹具 ----

    public static HideoutSettingsBase NewHideoutSettings(double? generatorFuelFlowRate = null, double? gpuBoostRate = null) => new()
    {
        GeneratorFuelFlowRate = generatorFuelFlowRate,
        GpuBoostRate = gpuBoostRate,
        GeneratorSpeedWithoutFuel = null,
        AirFilterUnitFlowRate = null,
        CultistAmuletBonusPercent = null
    };

    public static HideoutProduction NewRecipe(string id, string endProduct, double? productionTime) => new()
    {
        Id = id,
        AreaType = HideoutAreas.Workbench,
        Requirements = [],
        ProductionTime = productionTime,
        EndProduct = endProduct,
        Count = 1,
        IsEncoded = false,
        Locked = false,
        NeedFuelForAllProductionTime = false,
        Continuous = false,
        ProductionLimitCount = 0,
        IsCodeProduction = false
    };

    public static ScavRecipe NewScavRecipe(string id, double? productionTime) => new()
    {
        Id = id,
        Requirements = [],
        ProductionTime = productionTime,
        EndProducts = new EndProducts
        {
            Common = new MinMax<int> { Min = 1, Max = 1 },
            Rare = new MinMax<int> { Min = 0, Max = 0 },
            Superrare = new MinMax<int> { Min = 0, Max = 0 }
        }
    };

    public static HideoutArea NewAreaWithStage(HideoutAreas type, double? constructionTime) => new()
    {
        Type = type,
        IsEnabled = true,
        NeedsFuel = false,
        Requirements = [],
        IsTakeFromSlotLocked = false,
        CraftGivesExperience = false,
        DisplayLevel = false,
        EnableAreaRequirements = false,
        Stages = new Dictionary<string, Stage>
        {
            ["1"] = new Stage
            {
                Requirements = [],
                AutoUpgrade = false,
                Bonuses = [],
                ConstructionTime = constructionTime,
                Container = default,
                Description = null!,
                DisplayInterface = false,
                Improvements = [],
                Slots = 0
            }
        }
    };

    public static CraftTimeThreshold NewCraftTimeThreshold(int craftTimeSeconds) => new()
    {
        CraftTimeSeconds = craftTimeSeconds,
        Type = "x",
        Min = 0,
        Max = 0
    };

    public static ScavCaseConfig NewScavCaseConfig() => new()
    {
        RewardItemValueRangeRub = new Dictionary<string, MinMax<double>>(),
        MoneyRewards = new MoneyRewards { RubCount = null!, UsdCount = null!, EurCount = null!, GpCount = null! },
        AmmoRewards = new AmmoRewards { AmmoRewardBlacklist = null!, AmmoRewardValueRangeRub = null! },
        RewardItemParentBlacklist = [],
        RewardItemBlacklist = []
    };

    /// <summary>构造健身断言所需的最小 Global 链（其余必填成员以 null! 占位）。</summary>
    /// <summary>
    /// 构造健身断言所需的最小 Global 链。4.1.6 类型形态与 5.0 不同：
    /// <c>GlobalConfig.Health</c> 为 <see cref="Health"/>（非 HealthGlobals），其 <c>Effects</c>
    /// 为 <see cref="Effects"/>，<c>SevereMusclePain</c> 为 <see cref="MusclePainEffect"/>；
    /// 跳蚤全局为 <see cref="RagFair"/>（非 RagfairGlobals）。仅设置被测链路，
    /// 4.1.6 的 required 成员（Mastering/ArenaEftTransferSettings/RestrictionsInRaid/EventType）
    /// 以空集合/null 占位，其余成员不在初始值设定项中列出。
    /// </summary>
    public static GlobalTable NewGlobalTable(double gymEffectivity) => new()
    {
        Configuration = new GlobalConfig
        {
            Mastering = [],
            ArenaEftTransferSettings = null!,
            RestrictionsInRaid = [],
            EventType = [],
            Health = new Health
            {
                Effects = new Effects
                {
                    SevereMusclePain = new MusclePainEffect { GymEffectivity = gymEffectivity }
                }
            },
            RagFair = new RagFair()
        },
        LocationInfection = null!,
        BotPresets = null!,
        BotWeaponScatterings = null!,
        ItemPresets = null!
    };

    /// <summary>最小商人（Assort + Base 忠诚等级 + 保险）。</summary>
    public static Trader NewTrader(double buyPriceCoefficient = 45) => new()
    {
        Assort = new TraderAssort { Items = [], BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>() },
        Base = new TraderBase
        {
            Insurance = new TraderInsurance
            {
                Availability = true,
                ExcludedCategory = [],
                MaxReturnHour = 0,
                MaxStorageTime = 0,
                MinPayment = 0,
                MinReturnHour = 0
            },
            ItemsBuy = new ItemBuyData { Category = [], IdList = [] },
            LoyaltyLevels =
            [
                new TraderLoyaltyLevel
                {
                    BuyPriceCoefficient = buyPriceCoefficient,
                    ExchangePriceCoefficient = 0,
                    HealPriceCoefficient = 0,
                    InsurancePriceCoefficient = 0,
                    MinLevel = 1,
                    MinSalesSum = 100000,
                    MinStanding = 0,
                    RepairPriceCoefficient = 0
                }
            ]
        },
        Dialogue = null!,
        QuestAssort = null!
    };

    /// <summary>断言模板首个网格的 (cellsV, cellsH)。</summary>
    public static void AssertSize(TemplateTable templates, string template, int cellsV, int cellsH)
    {
        var grid = templates.Items[template].Properties!.Grids!.First().Properties!;
        Assert.Equal(cellsV, grid.CellsV);
        Assert.Equal(cellsH, grid.CellsH);
    }

    // ---- G6-D 夹具 ----

    /// <summary>含单个 bot 角色的机器人表（Types 为 required+init，须在初始化器中设置）。</summary>
    public static BotTable NewBotTable(string role, BotType bot) => new()
    {
        Types = new Dictionary<string, BotType?>(StringComparer.Ordinal) { [role] = bot },
        Base = null!,
        Core = null!
    };

    /// <summary>最小 boss：chances.equipment 与 inventory.equipment 均为空字典。</summary>
    public static BotType NewBoss()
    {
        var bot = (BotType)RuntimeHelpers.GetUninitializedObject(typeof(BotType));

        var chances = (Chances)RuntimeHelpers.GetUninitializedObject(typeof(Chances));
        chances.EquipmentChances = new Dictionary<string, double>(StringComparer.Ordinal) { ["Holster"] = 25 };
        bot.BotChances = chances;

        var inventory = (BotTypeInventory)RuntimeHelpers.GetUninitializedObject(typeof(BotTypeInventory));
        inventory.Equipment = new Dictionary<SPTarkov.Server.Core.Models.Enums.EquipmentSlots, Dictionary<MongoId, double>>
        {
            [SPTarkov.Server.Core.Models.Enums.EquipmentSlots.Holster] = new() { [(MongoId)"5b3b713c5acfc4330140bd8c"] = 1 }
        };
        bot.BotInventory = inventory;
        return bot;
    }

    /// <summary>含 AvailableForStart / AvailableForFinish 的任务（绕开 required 校验）。</summary>
    public static Quest NewQuest(string id, string name, QuestCondition[] start, QuestCondition[] finish)
    {
        var quest = (Quest)RuntimeHelpers.GetUninitializedObject(typeof(Quest));
        quest.Id = id;
        quest.Name = name;
        quest.TraderId = "54cb50c76803fa8b248b4571";
        quest.Conditions = new QuestConditionTypes
        {
            Started = [],
            AvailableForStart = [.. start],
            AvailableForFinish = [.. finish],
            Success = [],
            Fail = []
        };
        return quest;
    }

    /// <summary>任务条件（ConditionType + Value）。</summary>
    public static QuestCondition NewCondition(string conditionType, double? value = null, string? id = null)
    {
        var condition = (QuestCondition)RuntimeHelpers.GetUninitializedObject(typeof(QuestCondition));
        condition.Id = id ?? "000000000000000000000099";
        condition.ConditionType = conditionType;
        condition.Value = value;
        return condition;
    }

    /// <summary>含 Grids/Slots 属性的物品（用于杂项测试）。</summary>
    public static TemplateItem NewTweakItem(
        string id,
        string parent,
        string? rigLayoutName = null,
        bool? blocksArmorVest = null,
        bool examinedByDefault = false,
        double examineTime = 0,
        double discardLimit = 0,
        string type = "Item",
        GridFilter[]? gridFilters = null,
        Slot[]? slots = null) => new()
    {
        Id = id,
        Parent = parent,
        Type = type,
        Properties = new TemplateItemProperties
        {
            RigLayoutName = rigLayoutName,
            BlocksArmorVest = blocksArmorVest,
            ExaminedByDefault = examinedByDefault,
            ExamineTime = examineTime,
            DiscardLimit = discardLimit,
            StackMaxSize = 1,
            Grids = gridFilters is null
                ? []
                : [new Grid { Properties = new GridProperties { Filters = gridFilters } }],
            Slots = slots ?? []
        }
    };

    /// <summary>含首个过滤器 Filter/ExcludedFilter 的网格过滤器。</summary>
    public static GridFilter NewGridFilter(string[]? filter = null, string[]? excludedFilter = null) => new()
    {
        Filter = [.. (filter ?? []).Select(id => (MongoId)id)],
        ExcludedFilter = excludedFilter is null ? null : [.. excludedFilter.Select(id => (MongoId)id)]
    };

    /// <summary>含单个槽位过滤器列表的口袋模板。</summary>
    public static TemplateItem NewPocket(string id, string[] allowed) => new()
    {
        Id = id,
        Type = "Item",
        Properties = new TemplateItemProperties
        {
            Slots =
            [
                new Slot
                {
                    Id = id,
                    Parent = id,
                    Properties = new SlotProperties
                    {
                        Filters = [new SlotFilter { Filter = [.. allowed.Select(item => (MongoId)item)] }]
                    }
                }
            ]
        }
    };
}
