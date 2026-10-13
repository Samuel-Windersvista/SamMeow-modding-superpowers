using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-B ScavCase：奖励池过滤（父类黑名单 + 买断规则 + 物品黑名单 + 弹药箱定价补丁）、
/// 奖励价值区间与配方重做、启动速度调整（fasterScavcase）。
/// 数据来自内嵌资源 <see cref="ScavCaseResourceLoader"/>。
/// </summary>
public sealed class ScavCaseChanger : ISoftcoreChanger
{
    /// <summary>弹药箱父类（源 TS 硬编码）。</summary>
    public const string AmmoBoxParent = "543be5cb4bdc2deb348b4568";

    /// <summary>内置插板父类（不计入可购集合）。</summary>
    public const string BuiltInInsertsParent = "65649eb40bf0ed77b8044453";

    /// <summary>手册价 ≥ 此值即可保留（源 10000）。</summary>
    public const double RewardKeepMinHandbookPrice = 10000;

    /// <summary>手册价低于此值直接剔除（源 2）。</summary>
    public const double MinHandbookPrice = 2;

    public string Name => "scavCase";

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var options = context.Config.ScavCaseOptions;
        if (!options.Enabled)
        {
            return;
        }

        if (options.BetterRewards)
        {
            ApplyBetterRewards(context, log);
        }

        if (options.Rebalance)
        {
            ApplyRebalance(context, log);
        }

        if (options.FasterScavcase.Enabled)
        {
            ApplyFasterScavcase(context, options.FasterScavcase.SpeedMultiplier, log);
        }
    }

    private static void ApplyBetterRewards(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var config = context.Services.ScavCase;
        if (config is null)
        {
            log.Warn("未注入 scavCaseConfig，跳过 betterRewards");
            return;
        }

        var data = ScavCaseResourceLoader.Load();
        var templates = context.Tables.Templates;

        config.RewardItemParentBlacklist.Clear();
        config.RewardItemParentBlacklist.UnionWith(data.ParentBlacklist.Select(id => (MongoId)id));

        var whitelist = data.Whitelist.Select(id => (MongoId)id).ToHashSet();
        var itemBlacklist = data.ItemBlacklist.Select(id => (MongoId)id).ToHashSet();
        var buyable = BuildBuyableItems(context, templates);
        var handbookIndex = BuildHandbookIndex(templates);

        foreach (var item in templates.Items.Values)
        {
            if (item.Type != "Item")
            {
                continue;
            }

            var price = ResolveHandbookPrice(templates, handbookIndex, item);

            // 源规则：结构过滤通过，且（非在售 / 手册价 ≥ 10000 / 父类在白名单）才保留。
            var keep = PassesStructuralFilter(item, itemBlacklist, price)
                       && (!buyable.Contains(item.Id)
                           || price >= RewardKeepMinHandbookPrice
                           || whitelist.Contains(item.Parent));

            if (!keep)
            {
                config.RewardItemBlacklist.Add(item.Id);
            }
        }

        log.Changed();
    }

    private static void ApplyRebalance(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var config = context.Services.ScavCase;
        if (config is null)
        {
            log.Warn("未注入 scavCaseConfig，跳过 rebalance");
            return;
        }

        var data = ScavCaseResourceLoader.Load();
        config.RewardItemValueRangeRub = data.RewardItemValueRangeRub.ToDictionary(
            pair => pair.Key,
            pair => new MinMax<double> { Min = pair.Value.Min, Max = pair.Value.Max },
            StringComparer.Ordinal);

        if (context.Tables.Hideout.Production is not null)
        {
            context.Tables.Hideout.Production.ScavRecipes = ToRecipes(data);
        }

        log.Changed();
    }

    private static void ApplyFasterScavcase(SoftcoreContext context, double multiplier, SoftcoreChangeLog log)
    {
        if (!SoftcoreTime.TryMultiplier(multiplier, "speedMultiplier", log))
        {
            return;
        }

        var recipes = context.Tables.Hideout.Production?.ScavRecipes;
        if (recipes is null)
        {
            log.Warn("未找到 scavRecipes，跳过 fasterScavcase");
            return;
        }

        // 忠实源 TS 语义：productionTime = round(原时间 / speedMultiplier)。
        foreach (var recipe in recipes)
        {
            // 4.1.6 ProductionTime 为可空 double?；null 跳过、不计数（不虚增变更数）。
            if (recipe.ProductionTime is { } time)
            {
                recipe.ProductionTime = SoftcoreTime.ScaleRound(time, multiplier);
                log.Changed();
            }
        }
    }

    /// <summary>收集所有商人在售物品（排除 LighthouseKeeper 与内置插板）。</summary>
    private static HashSet<MongoId> BuildBuyableItems(SoftcoreContext context, TemplateTable templates)
    {
        var buyable = new HashSet<MongoId>();
        foreach (var (traderId, trader) in context.Tables.Traders)
        {
            if (traderId == Traders.LIGHTHOUSEKEEPER || trader.Assort?.Items is null)
            {
                continue;
            }

            foreach (var item in trader.Assort.Items)
            {
                if (!templates.Items.TryGetValue(item.Template, out var template)
                    || template.Parent != BuiltInInsertsParent)
                {
                    buyable.Add(item.Template);
                }
            }
        }

        return buyable;
    }

    private static Dictionary<MongoId, HandbookItem>? BuildHandbookIndex(TemplateTable templates)
    {
        var items = templates.Handbook?.Items;
        if (items is null)
        {
            return null;
        }

        var index = new Dictionary<MongoId, HandbookItem>();
        foreach (var item in items)
        {
            index[item.Id] = item;
        }

        return index;
    }

    /// <summary>手册价解析；对弹药箱按源补丁以内容弹药价 × 数量重定价。</summary>
    private static double ResolveHandbookPrice(
        TemplateTable templates,
        Dictionary<MongoId, HandbookItem>? handbookIndex,
        TemplateItem item)
    {
        if (handbookIndex is null)
        {
            return 0;
        }

        var price = handbookIndex.TryGetValue(item.Id, out var entry) ? entry.Price ?? 0 : 0;

        if (item.Parent != AmmoBoxParent)
        {
            return price;
        }

        var slot = item.Properties?.StackSlots?.FirstOrDefault();
        var count = slot?.MaxCount;
        var ammo = slot?.Properties?.Filters?.FirstOrDefault()?.Filter?.FirstOrDefault();
        if (count is null || ammo is null)
        {
            return price;
        }

        var ammoPrice = handbookIndex.TryGetValue(ammo.Value, out var ammoEntry) ? ammoEntry.Price ?? 0 : 0;
        var patched = (long)SoftcoreTime.JsRound(ammoPrice * count.Value);

        if (handbookIndex.TryGetValue(item.Id, out var boxEntry))
        {
            boxEntry.Price = patched;
        }
        else
        {
            var newEntry = new HandbookItem { Id = item.Id, ParentId = item.Parent, Price = patched };
            handbookIndex[item.Id] = newEntry;
            templates.Handbook!.Items.Add(newEntry);
        }

        return patched;
    }

    private static bool PassesStructuralFilter(TemplateItem item, HashSet<MongoId> itemBlacklist, double price)
    {
        return !string.IsNullOrEmpty((string)item.Parent)
               && item.Type != "Node"
               && item.Properties?.QuestItem != true
               && !itemBlacklist.Contains(item.Id)
               && price >= MinHandbookPrice;
    }

    private static List<ScavRecipe> ToRecipes(ScavCaseTables data) => data.Recipes
        .Select(entry => new ScavRecipe
        {
            Id = entry.Id,
            Requirements =
            [
                new Requirement
                {
                    TemplateId = entry.RequiredItem,
                    Count = 1,
                    IsFunctional = false,
                    IsEncoded = false,
                    Type = "Item"
                }
            ],
            ProductionTime = entry.ProductionTime,
            EndProducts = new EndProducts
            {
                Common = ToMinMax(entry.Common),
                Rare = ToMinMax(entry.Rare),
                Superrare = ToMinMax(entry.Superrare)
            }
        })
        .ToList();

    private static MinMax<int> ToMinMax(ScavCaseRange range) =>
        new() { Min = (int)range.Min, Max = (int)range.Max };
}
