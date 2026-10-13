using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-C 商人：收价上调（忠诚 +5%/级）、类别调整、和平主义 Fence、藏匿箱合理定价、
/// Skier 欧元化、购买上限 ×2。数据来自内嵌 <see cref="FleaMarketResourceLoader"/>。
/// </summary>
public sealed class TraderChangesChanger : ISoftcoreChanger
{
    public string Name => "traderChanges";

    /// <summary>弹药箱父类：ragfair 报价数按父类查询，须与 default 同范围。</summary>
    private const string AmmoBoxParent = "543be5cb4bdc2deb348b4568";

    /// <summary>静态商人 id 列表（忽略自定义商人）。</summary>
    private static readonly string[] StaticTraders =
    [
        "54cb50c76803fa8b248b4571", // PRAPOR
        "54cb57776803fa99248b456e", // THERAPIST
        "579dc571d53a0658a154fbec", // FENCE
        "58330581ace78e27b8b10cee", // SKIER
        "5935c25fb3acc3127c3d8cd9", // PEACEKEEPER
        "5a7c2eca46aef81a7ca2145d", // MECHANIC
        "5ac3b934156ae10c4430e83c", // RAGMAN
        "5c0647fdd443bc2504c2d371", // JAEGER
        "6617beeaa9cfa777ca915b7c" // REF
    ];

    /// <summary>收价系数调整（相对基准 35 的偏移）。</summary>
    private static readonly Dictionary<string, int> BuyPriceAdjustment = new(StringComparer.Ordinal)
    {
        ["5935c25fb3acc3127c3d8cd9"] = 7, // PEACEKEEPER
        ["58330581ace78e27b8b10cee"] = 6, // SKIER
        ["54cb50c76803fa8b248b4571"] = 5, // PRAPOR
        ["5a7c2eca46aef81a7ca2145d"] = 4, // MECHANIC
        ["5c0647fdd443bc2504c2d371"] = 3, // JAEGER
        ["5ac3b934156ae10c4430e83c"] = 2, // RAGMAN
        ["54cb57776803fa99248b456e"] = 1 // THERAPIST
    };

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var options = context.Config.TraderChanges;
        if (!options.Enabled)
        {
            return;
        }

        if (options.BetterSalesToTraders)
        {
            ApplyBetterSales(context, log);
        }

        if (options.AlternativeCategories)
        {
            ApplyAlternativeCategories(context, log);
        }

        if (options.PacifistFence.Enabled)
        {
            ApplyPacifistFence(context, options.PacifistFence.NumberOfFenceOffers, log);
        }

        if (options.ReasonablyPricedCases)
        {
            ApplyReasonablyPricedCases(context, log);
        }

        if (options.SkierUsesEuros)
        {
            ApplySkierUsesEuros(context, log);
        }

        if (options.BiggerLimits.Enabled)
        {
            ApplyBiggerLimits(context, options.BiggerLimits.Multiplier, log);
        }
    }

    private static void ApplyBetterSales(SoftcoreContext context, SoftcoreChangeLog log)
    {
        foreach (var traderId in StaticTraders)
        {
            if (!BuyPriceAdjustment.TryGetValue(traderId, out var adjustment)
                || !context.Tables.Traders.TryGetValue(traderId, out var trader)
                || trader.Base?.LoyaltyLevels is null)
            {
                continue;
            }

            var buyPriceCoef = 35;
            foreach (var loyaltyLevel in trader.Base.LoyaltyLevels)
            {
                loyaltyLevel.BuyPriceCoefficient = buyPriceCoef + adjustment;
                buyPriceCoef -= 5;
            }
        }

        log.Changed();
    }

    private static void ApplyAlternativeCategories(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var traders = context.Tables.Traders;

        if (traders.TryGetValue(Traders.THERAPIST, out var therapist) && therapist.Base?.ItemsBuy is not null)
        {
            therapist.Base.ItemsBuy.Category.Add(BaseClasses.MEDICAL_SUPPLIES);
            therapist.Base.ItemsBuy.Category.Add(BaseClasses.HOUSEHOLD_GOODS);
            therapist.Base.ItemsBuy.Category.Remove(BaseClasses.BARTER_ITEM);
        }

        if (traders.TryGetValue(Traders.RAGMAN, out var ragman) && ragman.Base?.ItemsBuy is not null)
        {
            ragman.Base.ItemsBuy.Category.Add(BaseClasses.JEWELRY);
        }

        if (traders.TryGetValue(Traders.SKIER, out var skier) && skier.Base?.ItemsBuy is not null)
        {
            skier.Base.ItemsBuy.Category.Add(BaseClasses.INFO);
        }

        log.Changed();
    }

    private static void ApplyPacifistFence(SoftcoreContext context, int numberOfFenceOffers, SoftcoreChangeLog log)
    {
        var fence = context.Services.Trader?.Fence;
        if (fence is null)
        {
            log.Warn("未注入 traderConfig.fence，跳过 pacifistFence");
            return;
        }

        var data = FleaMarketResourceLoader.Load();
        var fenceWhitelist = data.PacifistFenceItemBaseWhitelist.ToHashSet(StringComparer.Ordinal);
        var fenceBlacklist = data.ItemBaseClasses.Where(baseClass => !fenceWhitelist.Contains(baseClass)).ToHashSet(StringComparer.Ordinal);

        fence.ItemTypeLimits.Clear();
        foreach (var baseClass in data.ItemBaseClasses)
        {
            fence.ItemTypeLimits[baseClass] = numberOfFenceOffers;
        }

        // SPT 缺陷规避：MedicalSupplies 同时出现在 itemTypeLimits / preventDuplicate 会导致 Fence 失效。
        fence.ItemTypeLimits.Remove("57864c8c245977548867e7f1");

        var questItemIds = context.Tables.Templates.Items.Values
            .Where(item => item.Properties?.QuestItem == true)
            .Select(item => item.Id);

        fence.Blacklist.UnionWith(questItemIds.Select(id => (MongoId)id));
        fence.Blacklist.UnionWith(data.BsgBlacklist.Select(id => (MongoId)id));
        fence.Blacklist.UnionWith(fenceBlacklist.Select(id => (MongoId)id));
        fence.Blacklist.Add(ItemTpl.INFO_ENCRYPTED_FLASH_DRIVE);

        fence.PreventDuplicateOffersOfCategory.Clear();
        fence.PreventDuplicateOffersOfCategory.UnionWith(
            fenceWhitelist.Where(id => id != "57864c8c245977548867e7f1").Select(id => (MongoId)id));

        fence.AssortSize = numberOfFenceOffers;
        fence.EquipmentPresetMinMax.Min = 0;
        fence.EquipmentPresetMinMax.Max = 0;
        fence.WeaponPresetMinMax.Min = 0;
        fence.WeaponPresetMinMax.Max = 0;
        fence.ItemPriceMult = 1;
        fence.DiscountOptions.AssortSize = numberOfFenceOffers * 2;
        fence.DiscountOptions.ItemPriceMult = 0.82;
        fence.DiscountOptions.WeaponPresetMinMax.Min = 0;
        fence.DiscountOptions.WeaponPresetMinMax.Max = 0;
        fence.DiscountOptions.EquipmentPresetMinMax.Min = 0;
        fence.DiscountOptions.EquipmentPresetMinMax.Max = 0;

        log.Changed();
    }

    private static void ApplyReasonablyPricedCases(SoftcoreContext context, SoftcoreChangeLog log)
    {
        ModifyBarter(context, Traders.PEACEKEEPER, ItemTpl.CONTAINER_THICC_ITEM_CASE,
            [(ItemTpl.INFO_TERRAGROUP_BLUE_FOLDERS_MATERIALS, BarterAdjustment.DivideAddOne, 5)]);
        ModifyBarter(context, Traders.SKIER, ItemTpl.CONTAINER_WEAPON_CASE,
            [(ItemTpl.DRINK_BOTTLE_OF_FIERCE_HATCHLING_MOONSHINE, BarterAdjustment.Set, 4)]);
        ModifyBarter(context, Traders.THERAPIST, ItemTpl.CONTAINER_ITEM_CASE,
        [
            (ItemTpl.MONEY_EUROS, BarterAdjustment.Set, 7256),
            (ItemTpl.BARTER_OPHTHALMOSCOPE, BarterAdjustment.Set, 8),
            (ItemTpl.BARTER_DOGTAG_USEC, BarterAdjustment.Set, 20)
        ]);
        ModifyBarter(context, Traders.THERAPIST, ItemTpl.CONTAINER_LUCKY_SCAV_JUNK_BOX,
        [
            (ItemTpl.MONEY_ROUBLES, BarterAdjustment.Set, 961138),
            (ItemTpl.BARTER_DOGTAG_USEC, BarterAdjustment.Set, 15)
        ]);
        ModifyBarter(context, Traders.THERAPIST, ItemTpl.CONTAINER_MEDICINE_CASE,
            [(ItemTpl.MONEY_ROUBLES, BarterAdjustment.Set, 290610)]);
        ModifyBarter(context, Traders.THERAPIST, ItemTpl.BARTER_LEDX_SKIN_TRANSILLUMINATOR,
            [(ItemTpl.BARTER_DOGTAG_USEC, BarterAdjustment.DivideInt, 10)]);
        ModifyBarter(context, Traders.THERAPIST, ItemTpl.CONTAINER_THICC_ITEM_CASE,
        [
            (ItemTpl.BARTER_LEDX_SKIN_TRANSILLUMINATOR, BarterAdjustment.Set, 5),
            (ItemTpl.DRINK_BOTTLE_OF_FIERCE_HATCHLING_MOONSHINE, BarterAdjustment.Set, 10)
        ]);

        log.Changed();
    }

    private enum BarterAdjustment
    {
        Set,
        DivideInt,
        DivideAddOne
    }

    private static void ModifyBarter(
        SoftcoreContext context,
        string traderId,
        string targetTemplate,
        (string RequirementTemplate, BarterAdjustment Mode, double Value)[] adjustments)
    {
        if (!context.Tables.Traders.TryGetValue(traderId, out var trader) || trader.Assort?.Items is null)
        {
            return;
        }

        var assort = trader.Assort;
        var barterIds = assort.Items.Where(item => item.Template == targetTemplate).Select(item => item.Id).ToList();

        foreach (var (requirementTemplate, mode, value) in adjustments)
        {
            foreach (var barterId in barterIds)
            {
                if (assort.BarterScheme is null || !assort.BarterScheme.TryGetValue(barterId, out var schemes))
                {
                    continue;
                }

                // 精确定位目标需求（避免把调整串扰到同 scheme 的其他需求），并覆盖全部 scheme。
                foreach (var requirement in schemes
                             .SelectMany(scheme => scheme)
                             .Where(requirement => requirement.Template == requirementTemplate))
                {
                    ApplyAdjustment(requirement, mode, value);
                }
            }
        }
    }

    private static void ApplyAdjustment(BarterScheme requirement, BarterAdjustment mode, double value)
    {
        switch (mode)
        {
            case BarterAdjustment.Set:
                requirement.Count = (int)value;
                break;
            case BarterAdjustment.DivideInt:
                requirement.Count = (int)((requirement.Count ?? 0) / value);
                break;
            case BarterAdjustment.DivideAddOne:
                requirement.Count = SoftcoreTime.RoundInt((requirement.Count ?? 0) / value) + 1;
                break;
        }
    }

    private static void ApplySkierUsesEuros(SoftcoreContext context, SoftcoreChangeLog log)
    {
        if (!context.Tables.Traders.TryGetValue(Traders.SKIER, out var skier) || skier.Base is null)
        {
            log.Warn("未找到 Skier，跳过 skierUsesEuros");
            return;
        }

        var handbookItems = context.Tables.Templates.Handbook?.Items;
        var euroPrice = handbookItems?.FirstOrDefault(item => item.Id == ItemTpl.MONEY_EUROS)?.Price;
        if (euroPrice is null or 0)
        {
            log.Warn("未找到欧元手册价，跳过 skierUsesEuros");
            return;
        }

        var baseInfo = skier.Base;
        baseInfo.Currency = CurrencyType.EUR;
        baseInfo.BalanceEuro = 700000;

        if (baseInfo.LoyaltyLevels is not null)
        {
            foreach (var loyaltyLevel in baseInfo.LoyaltyLevels)
            {
                // 4.1.6 MinSalesSum 为可空 long?；null 兜底 0。
                loyaltyLevel.MinSalesSum = SoftcoreTime.RoundInt((loyaltyLevel.MinSalesSum ?? 0) / euroPrice.Value);
            }
        }

        if (skier.Assort?.Items is not null && skier.Assort.BarterScheme is not null)
        {
            var euroBarterId = skier.Assort.Items.FirstOrDefault(item => item.Template == ItemTpl.MONEY_EUROS)?.Id;
            foreach (var (id, schemes) in skier.Assort.BarterScheme)
            {
                if (id == euroBarterId || schemes.Count == 0 || schemes[0].Count == 0)
                {
                    continue;
                }

                var first = schemes[0][0];
                if (first.Template == ItemTpl.MONEY_ROUBLES)
                {
                    // 源：Math.round((count / euroPrice) * 100) / 100 → 保留 2 位小数（Count 为 double?）。
                    first.Count = SoftcoreTime.JsRound(((first.Count ?? 0) / euroPrice.Value) * 100) / 100;
                    first.Template = ItemTpl.MONEY_EUROS;
                }
            }
        }

        ApplySkierQuestRewards(context, euroPrice.Value, log);
        log.Changed();
    }

    /// <summary>Skier 任务奖励欧元化：RUB→EUR，同步 StackObjectsCount 与 reward.Value（源用 Math.ceil）。</summary>
    private static void ApplySkierQuestRewards(SoftcoreContext context, double euroPrice, SoftcoreChangeLog log)
    {
        foreach (var quest in context.Tables.Templates.Quests.Values)
        {
            if (quest.TraderId != Traders.SKIER
                || quest.Rewards is null
                || !quest.Rewards.TryGetValue("Success", out var rewards)
                || rewards is null)
            {
                continue;
            }

            foreach (var reward in rewards)
            {
                if (reward.Items is null)
                {
                    continue;
                }

                foreach (var item in reward.Items.Where(item => item.Template == ItemTpl.MONEY_ROUBLES).ToList())
                {
                    item.Template = ItemTpl.MONEY_EUROS;

                    if (item.Upd?.StackObjectsCount is null)
                    {
                        log.Warn($"任务 {quest.Id} 奖励缺少 StackObjectsCount，跳过该奖励换算");
                        continue;
                    }

                    item.Upd.StackObjectsCount = Math.Ceiling(item.Upd.StackObjectsCount.Value / euroPrice);

                    if (reward.Value is null)
                    {
                        log.Warn($"任务 {quest.Id} 奖励缺少 value，跳过该奖励换算");
                        continue;
                    }

                    reward.Value = Math.Ceiling(reward.Value.Value / euroPrice);
                }
            }
        }
    }

    private static void ApplyBiggerLimits(SoftcoreContext context, double multiplier, SoftcoreChangeLog log)
    {
        if (!SoftcoreTime.TryMultiplier(multiplier, "biggerLimits.multiplier", log))
        {
            return;
        }

        foreach (var traderId in StaticTraders)
        {
            if (!context.Tables.Traders.TryGetValue(traderId, out var trader) || trader.Assort?.Items is null)
            {
                continue;
            }

            foreach (var item in trader.Assort.Items)
            {
                if (item.Upd?.BuyRestrictionMax is { } max)
                {
                    item.Upd.BuyRestrictionMax = SoftcoreTime.RoundInt(max * multiplier);
                }
            }
        }

        log.Changed();
    }
}
