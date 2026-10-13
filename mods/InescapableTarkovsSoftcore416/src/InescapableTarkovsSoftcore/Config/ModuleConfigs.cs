using System.Text.Json.Serialization;

namespace InescapableTarkovsSoftcore.Config;

/// <summary>功能组配置段基类：当前仅含启用开关，具体参数由各自工单扩展。</summary>
public class ModuleConfig
{
    /// <summary>该功能组开关。</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;
}

/// <summary>G1 Samuel's Tweaks：护甲弹挂冲突修复 / 可掠夺 / 弹匣缩格 / 启动器背景。</summary>
public sealed class SamuelTweaksConfig : ModuleConfig
{
    /// <summary>护甲弹挂冲突修复：含 RigLayoutName 的弹挂甲 → BlocksArmorVest=false。</summary>
    [JsonPropertyName("armorConflictFix")]
    public bool ArmorConflictFix { get; set; } = true;

    /// <summary>可掠夺物品子节。</summary>
    [JsonPropertyName("lootableItems")]
    public LootableItemsConfig LootableItems { get; set; } = new();

    /// <summary>扩展弹匣缩格子节。</summary>
    [JsonPropertyName("magazineResize")]
    public MagazineResizeConfig MagazineResize { get; set; } = new();
}

/// <summary>G1 可掠夺物品开关（臂章 / 近战武器）。</summary>
public sealed class LootableItemsConfig
{
    /// <summary>使臂章（父类 5447e1d04bdc2dff2f8b4567）可掠夺。</summary>
    [JsonPropertyName("armband")]
    public bool Armband { get; set; } = true;

    /// <summary>使近战武器（类目 5b3f15d486f77432d0509248）可掠夺。</summary>
    [JsonPropertyName("meleeWeapons")]
    public bool MeleeWeapons { get; set; } = true;
}

/// <summary>G1 扩展弹匣缩格参数。</summary>
public sealed class MagazineResizeConfig
{
    /// <summary>子功能开关。</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>容量下界（含）。</summary>
    [JsonPropertyName("minCapacity")]
    public int MinCapacity { get; set; } = 10;

    /// <summary>容量上界（含）。</summary>
    [JsonPropertyName("maxCapacity")]
    public int MaxCapacity { get; set; } = 50;
}

/// <summary>G2 True Items Redux 物品真实堆叠。</summary>
public sealed class TrueItemsConfig : ModuleConfig
{
    /// <summary>
    /// 逐物品 / 父类微调：id → 目标堆叠值。最后应用，覆盖内嵌查找表结果；
    /// key 先按物品 <c>_id</c> 精确匹配，匹配不到再按父类 <c>_parent</c> 批量匹配。
    /// </summary>
    [JsonPropertyName("overrides")]
    public Dictionary<string, int> Overrides { get; set; } = new(StringComparer.Ordinal);
}

/// <summary>G3 藏身处建造免 FIR。</summary>
public sealed class NoFirHideoutConfig : ModuleConfig;

/// <summary>G4 反重力臂章。</summary>
public sealed class AntigravArmbandsConfig : ModuleConfig
{
    /// <summary>全部臂章的堆叠上限（默认 5）。</summary>
    [JsonPropertyName("stackSize")]
    public int StackSize { get; set; } = 5;

    /// <summary>单品覆盖：臂章 id → 重量（最后应用，覆盖内嵌查找表）。</summary>
    [JsonPropertyName("overrides")]
    public Dictionary<string, double> Overrides { get; set; } = new();
}

/// <summary>G5 更大的背包。</summary>
public sealed class BackpacksConfig : ModuleConfig;

/// <summary>G6 Softcore 经济与制造系统大修（子结构对齐源 config.json5）。</summary>
public sealed class SoftcoreModuleConfig : ModuleConfig
{
    /// <summary>安全容器选项（G6-A）。</summary>
    [JsonPropertyName("secureContainersOptions")]
    public SecureContainersOptions SecureContainersOptions { get; set; } = new();

    /// <summary>仓库选项（G6-A）。</summary>
    [JsonPropertyName("stashOptions")]
    public StashOptions StashOptions { get; set; } = new();

    /// <summary>藏身处容器选项（G6-A）。</summary>
    [JsonPropertyName("hideoutContainers")]
    public HideoutContainersOptions HideoutContainers { get; set; } = new();

    /// <summary>制造加速（G6-B）。</summary>
    [JsonPropertyName("fasterCraftingTime")]
    public FasterCraftingTimeOptions FasterCraftingTime { get; set; } = new();

    /// <summary>藏身处建设加速（G6-B）。</summary>
    [JsonPropertyName("fasterHideoutConstruction")]
    public FasterHideoutConstructionOptions FasterHideoutConstruction { get; set; } = new();

    /// <summary>发电机燃料消耗（G6-B）。</summary>
    [JsonPropertyName("fuelConsumption")]
    public FuelConsumptionOptions FuelConsumption { get; set; } = new();

    /// <summary>比特币农场（G6-B）。</summary>
    [JsonPropertyName("fasterBitcoinFarming")]
    public FasterBitcoinFarmingOptions FasterBitcoinFarming { get; set; } = new();

    /// <summary>ScavCase 选项（G6-B）。</summary>
    [JsonPropertyName("scavCaseOptions")]
    public ScavCaseOptions ScavCaseOptions { get; set; } = new();

    /// <summary>允许在严重肌肉疼痛时以 75% 效率健身（G6-B）。</summary>
    [JsonPropertyName("allowGymTrainingWithMusclePain")]
    public bool AllowGymTrainingWithMusclePain { get; set; } = true;

    /// <summary>经济选项（G6-C）。</summary>
    [JsonPropertyName("economyOptions")]
    public EconomyOptions EconomyOptions { get; set; } = new();

    /// <summary>商人更改（G6-C）。</summary>
    [JsonPropertyName("traderChanges")]
    public TraderChangesOptions TraderChanges { get; set; } = new();

    /// <summary>保险更改（G6-C）。</summary>
    [JsonPropertyName("insuranceChanges")]
    public InsuranceChangesOptions InsuranceChanges { get; set; } = new();

    /// <summary>制造更改（G6-D）。</summary>
    [JsonPropertyName("craftingChanges")]
    public CraftingChangesOptions CraftingChanges { get; set; } = new();

    /// <summary>杂项更改（G6-D）。</summary>
    [JsonPropertyName("otherTweaks")]
    public OtherTweaksOptions OtherTweaks { get; set; } = new();
}

/// <summary>整数区间（Min/Max）。</summary>
public sealed class IntRange
{
    [JsonPropertyName("min")]
    public int Min { get; set; }

    [JsonPropertyName("max")]
    public int Max { get; set; }
}

/// <summary>经济选项根（默认值 = SURV 终态）。</summary>
public sealed class EconomyOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("disableFleaMarketCompletely")]
    public bool DisableFleaMarketCompletely { get; set; }

    [JsonPropertyName("priceRebalance")]
    public PriceRebalanceOptions PriceRebalance { get; set; } = new();

    [JsonPropertyName("pacifistFleaMarket")]
    public PacifistFleaMarketOptions PacifistFleaMarket { get; set; } = new();

    [JsonPropertyName("barterEconomy")]
    public BarterEconomyOptions BarterEconomy { get; set; } = new();

    [JsonPropertyName("otherFleaMarketChanges")]
    public OtherFleaMarketChangesOptions OtherFleaMarketChanges { get; set; } = new();
}

/// <summary>价格再平衡（SURV 关闭）。</summary>
public sealed class PriceRebalanceOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("itemFixes")]
    public bool ItemFixes { get; set; } = true;
}

/// <summary>和平主义跳蚤市场选项。</summary>
public sealed class PacifistFleaMarketOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("whitelist")]
    public EconomyToggle Whitelist { get; set; } = new() { PriceMultiplier = 2 };

    [JsonPropertyName("questKeys")]
    public EconomyToggle QuestKeys { get; set; } = new() { PriceMultiplier = 3 };

    [JsonPropertyName("markedKeys")]
    public EconomyToggle MarkedKeys { get; set; } = new() { PriceMultiplier = 5 };
}

/// <summary>带价格倍率的白名单开关。</summary>
public sealed class EconomyToggle
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("priceMultiplier")]
    public double PriceMultiplier { get; set; } = 1;
}

/// <summary>以物易物经济选项。</summary>
public sealed class BarterEconomyOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>允许现金购买的比例（%）；SPT 内部用 100 - 该值 作为 barter 概率。</summary>
    [JsonPropertyName("cashOffersPercentage")]
    public double CashOffersPercentage { get; set; } = 5;

    [JsonPropertyName("barterPriceVariance")]
    public double BarterPriceVariance { get; set; } = 30;

    [JsonPropertyName("offerItemCount")]
    public IntRange OfferItemCount { get; set; } = new() { Min = 5, Max = 13 };

    [JsonPropertyName("nonStackableCount")]
    public IntRange NonStackableCount { get; set; } = new() { Min = 1, Max = 4 };

    [JsonPropertyName("itemCountMax")]
    public int ItemCountMax { get; set; } = 4;
}

/// <summary>其他跳蚤市场选项。</summary>
public sealed class OtherFleaMarketChangesOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("sellingOnFlea")]
    public bool SellingOnFlea { get; set; }

    [JsonPropertyName("fleaMarketOpenAtLevel")]
    public double FleaMarketOpenAtLevel { get; set; } = 1;

    [JsonPropertyName("fleaPricesIncreased")]
    public double FleaPricesIncreased { get; set; } = 1.5;

    [JsonPropertyName("fleaPristineItems")]
    public bool FleaPristineItems { get; set; } = true;

    [JsonPropertyName("onlyFoundInRaidItemsAllowedForBarters")]
    public bool OnlyFoundInRaidItemsAllowedForBarters { get; set; }
}

/// <summary>商人更改选项。</summary>
public sealed class TraderChangesOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("betterSalesToTraders")]
    public bool BetterSalesToTraders { get; set; } = true;

    [JsonPropertyName("alternativeCategories")]
    public bool AlternativeCategories { get; set; } = true;

    [JsonPropertyName("pacifistFence")]
    public PacifistFenceOptions PacifistFence { get; set; } = new();

    [JsonPropertyName("reasonablyPricedCases")]
    public bool ReasonablyPricedCases { get; set; } = true;

    [JsonPropertyName("skierUsesEuros")]
    public bool SkierUsesEuros { get; set; } = true;

    [JsonPropertyName("biggerLimits")]
    public BiggerLimitsOptions BiggerLimits { get; set; } = new();
}

/// <summary>和平主义 Fence 选项。</summary>
public sealed class PacifistFenceOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("numberOfFenceOffers")]
    public int NumberOfFenceOffers { get; set; } = 15;
}

/// <summary>购买上限选项。</summary>
public sealed class BiggerLimitsOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("multiplier")]
    public double Multiplier { get; set; } = 2.0;
}

/// <summary>保险更改选项。</summary>
public sealed class InsuranceChangesOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("praporInsuranceChanges")]
    public TraderInsuranceOptions PraporInsuranceChanges { get; set; } = new()
    {
        ReturnChance = 70,
        ReturnTime = new IntRange { Min = 240, Max = 360 },
        InsuranceCostPercentage = 80
    };

    [JsonPropertyName("therapistInsuranceChanges")]
    public TraderInsuranceOptions TherapistInsuranceChanges { get; set; } = new()
    {
        ReturnChance = 60,
        ReturnTime = new IntRange { Min = 120, Max = 240 },
        InsuranceCostPercentage = 50
    };
}

/// <summary>单个商人的保险更改。</summary>
public sealed class TraderInsuranceOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("returnChance")]
    public double ReturnChance { get; set; } = 70;

    [JsonPropertyName("returnTime")]
    public IntRange ReturnTime { get; set; } = new() { Min = 240, Max = 360 };

    [JsonPropertyName("insuranceCostPercentage")]
    public double InsuranceCostPercentage { get; set; } = 80;
}

/// <summary>制造加速选项（默认值 = SURV 终态）。</summary>
public sealed class FasterCraftingTimeOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>全局基础时间倍率；配方时间 = ceil(原时间 / 倍率)。</summary>
    [JsonPropertyName("baseCraftingTimeMultiplier")]
    public double BaseCraftingTimeMultiplier { get; set; } = 3;

    [JsonPropertyName("hideoutSkillExpFix")]
    public HideoutSkillExpFixOptions HideoutSkillExpFix { get; set; } = new();

    [JsonPropertyName("fasterMoonshineProduction")]
    public FasterProductionOptions FasterMoonshineProduction { get; set; } = new() { BaseCraftingTimeMultiplier = 0.3 };

    [JsonPropertyName("fasterPurifiedWaterProduction")]
    public FasterProductionOptions FasterPurifiedWaterProduction { get; set; } = new() { BaseCraftingTimeMultiplier = 0.3 };

    [JsonPropertyName("fasterCultistCircle")]
    public FasterProductionOptions FasterCultistCircle { get; set; } = new() { BaseCraftingTimeMultiplier = 0.5 };
}

/// <summary>藏身处管理技能经验修复选项。</summary>
public sealed class HideoutSkillExpFixOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("hideoutSkillExpMultiplier")]
    public double HideoutSkillExpMultiplier { get; set; } = 10;
}

/// <summary>单项制造加速选项。</summary>
public sealed class FasterProductionOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("baseCraftingTimeMultiplier")]
    public double BaseCraftingTimeMultiplier { get; set; } = 1.0;
}

/// <summary>藏身处建设加速选项。</summary>
public sealed class FasterHideoutConstructionOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("hideoutConstructionTimeMultiplier")]
    public double HideoutConstructionTimeMultiplier { get; set; } = 50;
}

/// <summary>燃料消耗选项。</summary>
public sealed class FuelConsumptionOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>发电机燃料流量倍率（SURV = 4）。</summary>
    [JsonPropertyName("fuelConsumptionMultiplier")]
    public double FuelConsumptionMultiplier { get; set; } = 4;
}

/// <summary>比特币农场选项（默认值 = SURV 终态）。</summary>
public sealed class FasterBitcoinFarmingOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>将比特币手册价改回 10 万（SURV 关闭）。</summary>
    [JsonPropertyName("setBitcoinPriceTo100k")]
    public bool SetBitcoinPriceTo100k { get; set; }

    [JsonPropertyName("baseBitcoinTimeMultiplier")]
    public double BaseBitcoinTimeMultiplier { get; set; } = 1.3;

    [JsonPropertyName("gpuEfficiency")]
    public double GpuEfficiency { get; set; } = 1.0;
}

/// <summary>ScavCase 选项（默认值 = SURV 终态）。</summary>
public sealed class ScavCaseOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>奖励池过滤（父类黑名单 + 物品黑名单）。</summary>
    [JsonPropertyName("betterRewards")]
    public bool BetterRewards { get; set; } = true;

    [JsonPropertyName("fasterScavcase")]
    public FasterScavcaseOptions FasterScavcase { get; set; } = new();

    /// <summary>奖励池价值区间 + ScavCase 配方重做。</summary>
    [JsonPropertyName("rebalance")]
    public bool Rebalance { get; set; } = true;
}

/// <summary>ScavCase 启动速度选项。</summary>
public sealed class FasterScavcaseOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("speedMultiplier")]
    public double SpeedMultiplier { get; set; } = 0.5;
}

/// <summary>安全容器选项（默认值 = SURV 终态）。</summary>
public sealed class SecureContainersOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("progressiveContainers")]
    public ProgressiveContainersOptions ProgressiveContainers { get; set; } = new();

    /// <summary>放宽容器尺寸（Alpha 3×3 … Kappa 5×5）。</summary>
    [JsonPropertyName("biggerContainers")]
    public bool BiggerContainers { get; set; } = true;
}

/// <summary>渐进式安全容器选项。</summary>
public sealed class ProgressiveContainersOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>收藏家任务重做（20 级可接 + 仅需 1 张 Gamma）。</summary>
    [JsonPropertyName("collectorQuestRedone")]
    public bool CollectorQuestRedone { get; set; } = true;
}

/// <summary>仓库选项（默认值 = SURV 终态）。</summary>
public sealed class StashOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>新档从 LVL1 仓库起步。</summary>
    [JsonPropertyName("progressiveStash")]
    public bool ProgressiveStash { get; set; } = true;

    /// <summary>扩容至 56/100/150/200 行（标准仓 56 行以兼容 4.1.6 开局模板）。</summary>
    [JsonPropertyName("biggerStash")]
    public bool BiggerStash { get; set; } = true;

    /// <summary>仓库建造现金需求 ÷10。</summary>
    [JsonPropertyName("lessCurrencyForConstruction")]
    public bool LessCurrencyForConstruction { get; set; } = true;

    /// <summary>降低阶段建造忠诚度要求（SURV 关闭）。</summary>
    [JsonPropertyName("easierLoyalty")]
    public bool EasierLoyalty { get; set; }
}

/// <summary>藏身处容器选项（默认值 = SURV 终态）。</summary>
public sealed class HideoutContainersOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("biggerHideoutContainers")]
    public bool BiggerHideoutContainers { get; set; } = true;

    /// <summary>SICC 增强（合并 Docs 允许清单并允许钥匙工具）。</summary>
    [JsonPropertyName("siccCaseBuff")]
    public bool SiccCaseBuff { get; set; } = true;
}

/// <summary>制造更改选项（默认值 = SURV 终态）。</summary>
public sealed class CraftingChangesOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>30+ 条配方重平衡（数据 = 内嵌 crafting-rebalance.json）。</summary>
    [JsonPropertyName("craftingRebalance")]
    public bool CraftingRebalance { get; set; } = true;

    /// <summary>新增 12 条配方（数据 = 内嵌 crafting-recipes.json）。</summary>
    [JsonPropertyName("additionalCraftingRecipes")]
    public bool AdditionalCraftingRecipes { get; set; } = true;
}

/// <summary>杂项更改选项（默认值 = SURV 终态）。</summary>
public sealed class OtherTweaksOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    /// <summary>技能经验增益（SURV 关闭）。</summary>
    [JsonPropertyName("skillExpBuffs")]
    public bool SkillExpBuffs { get; set; }

    /// <summary>信号手枪可放入特殊槽。</summary>
    [JsonPropertyName("signalPistolInSpecialSlots")]
    public bool SignalPistolInSpecialSlots { get; set; } = true;

    /// <summary>撤销「默认已检视」（SURV 关闭）。</summary>
    [JsonPropertyName("unexaminedItemsAreBack")]
    public bool UnexaminedItemsAreBack { get; set; }

    /// <summary>检视时间固定 0.2s。</summary>
    [JsonPropertyName("fasterExamineTime")]
    public bool FasterExamineTime { get; set; } = true;

    /// <summary>移除背包/容器过滤限制。</summary>
    [JsonPropertyName("removeBackpackRestrictions")]
    public bool RemoveBackpackRestrictions { get; set; } = true;

    /// <summary>移除丢弃限制。</summary>
    [JsonPropertyName("removeDiscardLimit")]
    public bool RemoveDiscardLimit { get; set; } = true;

    /// <summary>Reshala 必带金色 TT。</summary>
    [JsonPropertyName("reshalaAlwaysHasGoldenTT")]
    public bool ReshalaAlwaysHasGoldenTT { get; set; } = true;

    /// <summary>弹药堆叠放大。</summary>
    [JsonPropertyName("biggerAmmoStacks")]
    public BiggerAmmoStacksOptions BiggerAmmoStacks { get; set; } = new();

    /// <summary>
    /// 弹挂甲是否阻断护甲（true = 阻断）。SURV 终值 false ⇒ 含 RigLayoutName 的弹挂甲
    /// <c>BlocksArmorVest=false</c>（弹挂与护甲不冲突），与 G1 修复同向。
    /// </summary>
    [JsonPropertyName("vestsBlockArmor")]
    public bool VestsBlockArmor { get; set; }

    /// <summary>任务变更（仅 Crisis + Drip-Out）。</summary>
    [JsonPropertyName("questChanges")]
    public bool QuestChanges { get; set; } = true;

    /// <summary>移除战局内物品限制。</summary>
    [JsonPropertyName("removeRaidItemLimits")]
    public bool RemoveRaidItemLimits { get; set; } = true;

    /// <summary>货币堆叠（SURV 关闭）。</summary>
    [JsonPropertyName("biggerCurrencyStacks")]
    public bool BiggerCurrencyStacks { get; set; }

    /// <summary>小型容器可放入特殊槽（SURV 关闭）。</summary>
    [JsonPropertyName("smallContainersInSpecialSlots")]
    public bool SmallContainersInSpecialSlots { get; set; }
}

/// <summary>弹药堆叠选项。</summary>
public sealed class BiggerAmmoStacksOptions
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("stackMultiplier")]
    public double StackMultiplier { get; set; } = 5;
}

/// <summary>G7 战局时长控制。</summary>
public sealed class RaidDurationConfig : ModuleConfig
{
    /// <summary>全局战局时长倍率；1.0 = 原版，2.0 = 翻倍。</summary>
    [JsonPropertyName("multiplier")]
    public double Multiplier { get; set; } = 1.0;
}
