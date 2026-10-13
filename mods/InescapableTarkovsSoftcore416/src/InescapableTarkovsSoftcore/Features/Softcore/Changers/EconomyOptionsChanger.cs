using InescapableTarkovsSoftcore.Config;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-C 经济：和平主义跳蚤市场（白名单/任务钥匙/标记钥匙 + 手册类目过滤）、
/// 以物易物经济、其他跳蚤改动。数据来自内嵌 <see cref="FleaMarketResourceLoader"/>。
/// </summary>
public sealed class EconomyOptionsChanger : ISoftcoreChanger
{
    public string Name => "economyOptions";

    /// <summary>弹药箱父类（ragfair 报价数按父类键查询）。</summary>
    private const string AmmoBoxParent = "543be5cb4bdc2deb348b4568";

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var options = context.Config.EconomyOptions;
        if (!options.Enabled)
        {
            return;
        }

        var ragfair = context.Services.Ragfair;
        if (ragfair is null)
        {
            log.Warn("未注入 ragfairConfig，跳过 economyOptions");
            return;
        }

        if (options.DisableFleaMarketCompletely)
        {
            SetFleaMinUserLevel(context, 99);
            log.Changed();
            return;
        }

        if (options.PriceRebalance.Enabled)
        {
            log.Warn("priceRebalance 未迁移（SURV 默认关闭），跳过");
        }

        if (options.PacifistFleaMarket.Enabled)
        {
            ApplyPacifist(context, ragfair, options.PacifistFleaMarket, log);
        }

        if (options.BarterEconomy.Enabled)
        {
            ApplyBarter(context, ragfair, options.BarterEconomy, log);
        }

        if (options.OtherFleaMarketChanges.Enabled)
        {
            ApplyOther(context, ragfair, options.OtherFleaMarketChanges, log);
        }
    }

    private static void ApplyPacifist(
        SoftcoreContext context,
        RagfairConfig ragfair,
        PacifistFleaMarketOptions options,
        SoftcoreChangeLog log)
    {
        var data = FleaMarketResourceLoader.Load();
        var templates = context.Tables.Templates;
        var handbook = templates.Handbook;
        if (handbook is null)
        {
            log.Warn("未找到 handbook，跳过 pacifistFleaMarket");
            return;
        }

        // 除白名单手册类目外全部禁止上架（任务物品同样禁止）。
        var handbookWhitelist = data.FleaListingsWhitelistHandBook.ToHashSet(StringComparer.Ordinal);
        foreach (var handbookItem in handbook.Items)
        {
            var isQuestItem = templates.Items.TryGetValue(handbookItem.Id, out var item)
                              && item.Properties?.QuestItem == true;
            if (!handbookWhitelist.Contains((string)handbookItem.ParentId) || isQuestItem)
            {
                ragfair.Dynamic.Blacklist.Custom.Add(handbookItem.Id);
            }
        }

        if (options.Whitelist.Enabled)
        {
            AllowOnRagfair(context, ragfair, data.Whitelist, options.Whitelist.PriceMultiplier, log);
        }

        if (options.QuestKeys.Enabled)
        {
            AllowOnRagfair(context, ragfair, data.QuestKeys, options.QuestKeys.PriceMultiplier, log);
        }

        if (options.MarkedKeys.Enabled)
        {
            AllowOnRagfair(context, ragfair, data.MarkedKeys, options.MarkedKeys.PriceMultiplier, log);
        }

        log.Changed();
    }

    private static void AllowOnRagfair(
        SoftcoreContext context,
        RagfairConfig ragfair,
        IReadOnlyList<string> itemIds,
        double priceMultiplier,
        SoftcoreChangeLog log)
    {
        var templates = context.Tables.Templates;
        foreach (var id in itemIds)
        {
            if (!templates.Items.TryGetValue(id, out var item))
            {
                log.Warn($"adjustedSellableOnRagfair: 未找到物品 {id}，跳过");
                continue;
            }

            if (templates.Prices.TryGetValue(id, out var price))
            {
                templates.Prices[id] = SoftcoreTime.JsRound(price * priceMultiplier);
            }

            if (item.Properties is not null)
            {
                item.Properties.CanSellOnRagfair = true;
            }

            ragfair.Dynamic.Blacklist.Custom.Remove(id);
        }
    }

    private static void ApplyBarter(
        SoftcoreContext context,
        RagfairConfig ragfair,
        BarterEconomyOptions options,
        SoftcoreChangeLog log)
    {
        var data = FleaMarketResourceLoader.Load();
        var templates = context.Tables.Templates;
        var barter = ragfair.Dynamic.Barter;

        var requestWhitelist = data.FleaBarterRequestWhitelist.ToHashSet(StringComparer.Ordinal);
        barter.ItemTypeBlacklist.Clear();
        barter.ItemTypeBlacklist.UnionWith(
            data.ActualBaseClasses.Where(baseClass => !requestWhitelist.Contains(baseClass)).Select(id => (MongoId)id));
        barter.MinRoubleCostToBecomeBarter = 100;

        var bsgBlacklist = data.BsgBlacklist.ToHashSet(StringComparer.Ordinal);
        var blacklistedBaseClasses = barter.ItemTypeBlacklist;
        foreach (var item in templates.Items.Values)
        {
            if (item.Type != "Item"
                || IsOfBaseClasses(context, item.Id, blacklistedBaseClasses)
                || item.Parent == BaseClasses.MONEY)
            {
                continue;
            }

            if (item.Properties?.QuestItem == true)
            {
                templates.Prices[item.Id] = 0;
            }
            else if (item.Properties?.CanSellOnRagfair != true)
            {
                templates.Prices[item.Id] = 0;
            }
            else if (bsgBlacklist.Contains((string)item.Id))
            {
                log.Warn($"物品 {item.Id} 可在跳蚤购买；启用 Barter Economy 时不应使用 BSG 黑名单解锁器");
            }
        }

        foreach (var (id, price) in data.RequestWhitelist)
        {
            templates.Prices[id] = price;
        }

        // 现金比例：SPT 内部以 barter 概率表示（100 - 现金百分比）。
        barter.ChancePercent = 100 - options.CashOffersPercentage;
        barter.PriceRangeVariancePercent = options.BarterPriceVariance;
        barter.ItemCountMax = options.ItemCountMax;

        ragfair.Dynamic.OfferItemCount.Clear();
        ragfair.Dynamic.OfferItemCount["default"] = new MinMax<int>
        {
            Min = options.OfferItemCount.Min,
            Max = options.OfferItemCount.Max
        };
        // SPT 先按父类键查询报价数、再回退 default；弹药箱等父类键须同步，保证所有物品类同范围。
        ragfair.Dynamic.OfferItemCount[AmmoBoxParent] = new MinMax<int>
        {
            Min = options.OfferItemCount.Min,
            Max = options.OfferItemCount.Max
        };
        ragfair.Dynamic.NonStackableCount = new MinMax<int>
        {
            Min = options.NonStackableCount.Min,
            Max = options.NonStackableCount.Max
        };

        log.Changed();
    }

    private static void ApplyOther(
        SoftcoreContext context,
        RagfairConfig ragfair,
        OtherFleaMarketChangesOptions options,
        SoftcoreChangeLog log)
    {
        if (options.SellingOnFlea)
        {
            ragfair.Sell.Chance.Base = 0;
            ragfair.Sell.Chance.MaxSellChancePercent = 0;
        }

        if (options.OnlyFoundInRaidItemsAllowedForBarters)
        {
            var ragFair = context.Tables.Global?.Configuration?.RagFair;
            if (ragFair is not null)
            {
                ragFair.IsOnlyFoundInRaidAllowed = true;
            }
        }

        if (options.FleaPristineItems)
        {
            foreach (var condition in ragfair.Dynamic.Condition.Values)
            {
                condition.ConditionChance = 0;
            }
        }

        ragfair.Dynamic.PriceRanges.Default.Min *= options.FleaPricesIncreased;
        ragfair.Dynamic.PriceRanges.Default.Max *= options.FleaPricesIncreased;

        SetFleaMinUserLevel(context, options.FleaMarketOpenAtLevel);
        log.Changed();
    }

    private static void SetFleaMinUserLevel(SoftcoreContext context, double level)
    {
        var ragFair = context.Tables.Global?.Configuration?.RagFair;
        if (ragFair is not null)
        {
            // 4.1.6 global RagFair.MinUserLevel 为 int（5.0 为 double）；按取整赋值，保持原取整语义。
            ragFair.MinUserLevel = (int)Math.Round(level, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>沿父类链判断物品是否属于给定 baseclass 集合（等价 ItemHelper.isOfBaseclasses）。</summary>
    private static bool IsOfBaseClasses(SoftcoreContext context, MongoId itemId, HashSet<MongoId> baseClasses)
    {
        var items = context.Tables.Templates.Items;
        var current = itemId;
        for (var depth = 0; depth < 16; depth++)
        {
            if (baseClasses.Contains(current))
            {
                return true;
            }

            if (!items.TryGetValue(current, out var item) || string.IsNullOrEmpty((string)item.Parent))
            {
                return false;
            }

            current = item.Parent;
        }

        return false;
    }
}
