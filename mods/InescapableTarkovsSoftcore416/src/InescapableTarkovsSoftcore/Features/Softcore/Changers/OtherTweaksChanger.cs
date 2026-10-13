using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using Item = SPTarkov.Server.Core.Models.Eft.Common.Tables.Item;

namespace InescapableTarkovsSoftcore.Features.Softcore.Changers;

/// <summary>
/// G6-D 杂项（SURV 终值）：弹药堆叠 ×5、弹挂甲与护甲不冲突、移除背包/丢弃/战局物品限制、
/// Reshala 金 TT、任务变更（仅 Crisis + Drip-Out）、信号枪特殊槽、检视 0.2s、
/// 撤销默认已检视、货币堆叠、小型容器特殊槽。
/// <para>
/// 相对 BASE 的 SURV 覆盖差异已落地：Boss 弹药补偿（botConfig）移除、<c>skipUnexaminedIDs</c> 移除、
/// 任务仅 Crisis + Drip-Out（circulate / colleagues3 不迁移）。
/// </para>
/// </summary>
public sealed class OtherTweaksChanger : ISoftcoreChanger
{
    public string Name => "otherTweaks";

    /// <summary>金色 TT 手枪（Reshala 装备，源 TS 硬编码）。</summary>
    private const string GoldenTt = "5b3b713c5acfc4330140bd8d";

    /// <summary>危机任务 id（Crisis）。</summary>
    private const string CrisisQuestId = "60e71c48c1bfa3050473b8e5";

    /// <summary>
    /// Drip-Out 系列任务 id（源 TS 按 <c>QuestName</c> 匹配；5.0 生产 DB 的 quests.name 是本地化键
    /// 「&lt;id&gt; name」、人类名在 locales，故按 id 匹配以免静默失效）。
    /// </summary>
    private static readonly string[] DripOutQuestIds =
    [
        "6613f3007f6666d56807c929",
        "6613f307fca4f2f386029409",
        "66151401efb0539ae10875ae",
        "6615141bfda04449120269a7"
    ];

    /// <summary>Drip-Out 交付数目标（源终值；5.0 原版为 50）。</summary>
    private const double DripOutHandoverCount = 10;

    /// <summary>Drip-Out 计数目标（源终值；5.0 原版为 100）。</summary>
    private const double DripOutCounterCount = 20;

    /// <summary>撤销「默认已检视」时保持原样的父类。</summary>
    private static readonly HashSet<MongoId> SkipUnexaminedParents =
    [
        BaseClasses.BUILT_IN_INSERTS,
        BaseClasses.MAGAZINE,
        BaseClasses.CYLINDER_MAGAZINE,
        BaseClasses.ARMOR_PLATE
    ];

    /// <summary>特殊槽（口袋）中的信号手枪与小型容器。</summary>
    private static readonly string[] SpecialSlotPockets = [ItemTpl.POCKETS_1X4_SPECIAL, ItemTpl.POCKETS_1X4_TUE];

    private static readonly string[] SmallContainers =
    [
        ItemTpl.CONTAINER_DOGTAG_CASE,
        ItemTpl.CONTAINER_INJECTOR_CASE,
        ItemTpl.CONTAINER_KEY_TOOL,
        ItemTpl.CONTAINER_KEYCARD_HOLDER_CASE,
        ItemTpl.CONTAINER_SIMPLE_WALLET,
        ItemTpl.CONTAINER_WZ_WALLET
    ];

    public void Apply(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var options = context.Config.OtherTweaks;
        if (!options.Enabled)
        {
            return;
        }

        if (options.SkillExpBuffs)
        {
            ApplySkillExpBuffs(context, log);
        }

        if (options.SignalPistolInSpecialSlots)
        {
            PushToSpecialSlots(context, ItemTpl.SIGNALPISTOL_ZID_SP81_26X75_SIGNAL_PISTOL, log);
        }

        if (options.UnexaminedItemsAreBack || options.FasterExamineTime || options.RemoveDiscardLimit)
        {
            ApplyItemLevelTweaks(context, options, log);
        }

        if (options.RemoveBackpackRestrictions)
        {
            ApplyRemoveBackpackRestrictions(context, log);
        }

        if (options.ReshalaAlwaysHasGoldenTT)
        {
            ApplyReshalaGoldenTt(context, log);
        }

        if (options.BiggerAmmoStacks.Enabled)
        {
            ApplyBiggerAmmoStacks(context, options.BiggerAmmoStacks.StackMultiplier, log);
        }

        // SURV 终值 vestsBlockArmor=false ⇒ 弹挂与护甲不冲突（与 G1 修复同向）。
        if (!options.VestsBlockArmor)
        {
            ApplyVestsBlockArmor(context, log);
        }

        if (options.QuestChanges)
        {
            ApplyQuestChanges(context, log);
        }

        if (options.RemoveRaidItemLimits)
        {
            ApplyRemoveRaidItemLimits(context, log);
        }

        if (options.BiggerCurrencyStacks)
        {
            ApplyCurrencyStacks(context, log);
        }

        if (options.SmallContainersInSpecialSlots)
        {
            PushToSpecialSlots(context, SmallContainers, log);
        }
    }

    private static void ApplySkillExpBuffs(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var globals = context.Tables.Global;
        if (globals is null)
        {
            log.Warn("未注入 global 表，跳过 skillExpBuffs");
            return;
        }

        var skills = globals.Configuration.SkillsSettings;
        skills.Vitality.DamageTakenAction *= 10;
        skills.Sniper.WeaponShotAction *= 10;
        skills.Surgery.SurgeryAction *= 10;
        skills.WeaponTreatment.SkillPointsPerRepair *= 100;
        // 源 TS 的 MagDrills 使用 Object.values(...).forEach(x => x * 10)（结果被丢弃）= 无效操作，忠实保留不改。
        log.Changed(4);
    }

    private static void ApplyItemLevelTweaks(
        SoftcoreContext context,
        Config.OtherTweaksOptions options,
        SoftcoreChangeLog log)
    {
        foreach (var item in context.Tables.Templates.Items.Values)
        {
            if (options.UnexaminedItemsAreBack
                && item.Properties?.ExaminedByDefault == true
                && !SkipUnexaminedParents.Contains(item.Parent))
            {
                item.Properties.ExaminedByDefault = false;
                log.Changed();
            }

            // 源 TS：if (... && item._props.ExamineTime) → 仅「有检视耗时」的物品；用 > 0 表达非零正数语义。
            if (options.FasterExamineTime && item.Properties is { ExamineTime: > 0 } properties)
            {
                properties.ExamineTime = 0.2;
                log.Changed();
            }

            if (options.RemoveDiscardLimit && item.Type == "Item" && item.Properties is { } discardProperties)
            {
                discardProperties.DiscardLimit = -1;
                log.Changed();
            }
        }
    }

    private static void ApplyRemoveBackpackRestrictions(SoftcoreContext context, SoftcoreChangeLog log)
    {
        foreach (var item in context.Tables.Templates.Items.Values.Where(item => item.Type == "Item"))
        {
            var excluded = item.Properties?.Grids?.FirstOrDefault()?.Properties?.Filters?.FirstOrDefault()?.ExcludedFilter;
            if (excluded is null || !excluded.Contains(ItemTpl.CONTAINER_AMMUNITION_CASE))
            {
                continue;
            }

            excluded.Clear();
            log.Changed();
        }
    }

    private static void ApplyReshalaGoldenTt(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var bots = context.Tables.Bots;
        if (bots is null)
        {
            log.Warn("未注入 bots 表，跳过 reshalaAlwaysHasGoldenTT");
            return;
        }

        if (!bots.Types.TryGetValue("bossbully", out var reshala))
        {
            log.Warn("未找到 bossbully，跳过 reshalaAlwaysHasGoldenTT");
            return;
        }

        if (reshala is null || reshala.BotChances is not { } chances || reshala.BotInventory is not { } inventory)
        {
            log.Warn("bossbully 缺少 chances/inventory，跳过 reshalaAlwaysHasGoldenTT");
            return;
        }

        chances.EquipmentChances["Holster"] = 100;
        inventory.Equipment[EquipmentSlots.Holster] = new Dictionary<MongoId, double>
        {
            [(MongoId)GoldenTt] = 1
        };
        log.Changed(2);
    }

    private static void ApplyBiggerAmmoStacks(SoftcoreContext context, double multiplier, SoftcoreChangeLog log)
    {
        if (!SoftcoreTime.TryMultiplier(multiplier, "otherTweaks.biggerAmmoStacks.stackMultiplier", log))
        {
            return;
        }

        foreach (var item in context.Tables.Templates.Items.Values
                     .Where(item => item.Parent == BaseClasses.AMMO))
        {
            // 4.1.6 StackMaxSize 为可空 int?；null（未设置）与 0（无堆叠）均跳过。
            if (item.Properties is not { StackMaxSize: { } size } || size == 0)
            {
                continue;
            }

            item.Properties.StackMaxSize = SoftcoreTime.RoundInt(size * multiplier);
            log.Changed();
        }
    }

    private static void ApplyVestsBlockArmor(SoftcoreContext context, SoftcoreChangeLog log)
    {
        foreach (var item in context.Tables.Templates.Items.Values
                     .Where(item => item.Properties?.RigLayoutName is not null))
        {
            if (item.Properties!.BlocksArmorVest == false)
            {
                continue;
            }

            item.Properties.BlocksArmorVest = false;
            log.Changed();
        }
    }

    private static void ApplyQuestChanges(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var quests = context.Tables.Templates.Quests;

        // Crisis：源 TS 设 AvailableForStart[1].value=30；5.0 该任务 A4S 仅 1 条 GlobalVariableValue
        // （Level 条件已被 BSG 移除），结构不符 → 告警跳过（记录于 delta-table §4c）。
        if (quests.TryGetValue(CrisisQuestId, out var crisis))
        {
            // 4.1.6 AvailableForStart 为可空列表；null 或不足 2 项均走告警跳过分支。
            var start = crisis.Conditions.AvailableForStart;
            if (start is { Count: > 1 })
            {
                start[1].Value = 30;
                log.Changed();
            }
            else
            {
                log.Warn("Crisis 任务 AvailableForStart 结构不符（<2 项，5.0 已移除 Level 条件），跳过");
            }
        }
        else
        {
            log.Warn($"未找到 Crisis 任务 {CrisisQuestId}，跳过");
        }

        // Drip-Out：按 id 匹配（5.0 name 为本地化键，按名匹配会静默失效）；A4F 设源值。
        foreach (var questId in DripOutQuestIds)
        {
            if (!quests.TryGetValue(questId, out var quest))
            {
                log.Warn($"未找到 Drip-Out 任务 {questId}，跳过");
                continue;
            }

            // 4.1.6 AvailableForFinish 为可空列表；缺失时无变更。
            foreach (var condition in quest.Conditions.AvailableForFinish ?? [])
            {
                if (condition.ConditionType == "HandoverItem")
                {
                    condition.Value = DripOutHandoverCount;
                    log.Changed();
                }
                else if (condition.ConditionType == "CounterCreator")
                {
                    condition.Value = DripOutCounterCount;
                    log.Changed();
                }
            }
        }
    }

    private static void ApplyRemoveRaidItemLimits(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var globals = context.Tables.Global;
        if (globals is null)
        {
            log.Warn("未注入 global 表，跳过 removeRaidItemLimits");
            return;
        }

        globals.Configuration.RestrictionsInRaid = [];
        log.Changed();
    }

    private static void ApplyCurrencyStacks(SoftcoreContext context, SoftcoreChangeLog log)
    {
        var items = context.Tables.Templates.Items;
        SetStack(items, ItemTpl.MONEY_EUROS, 100000, log);
        SetStack(items, ItemTpl.MONEY_DOLLARS, 100000, log);
        SetStack(items, ItemTpl.MONEY_GP_COIN, 100, log);
        SetStack(items, ItemTpl.MONEY_ROUBLES, 1000000, log);
    }

    private static void SetStack(
        Dictionary<MongoId, SPTarkov.Server.Core.Models.Eft.Common.Tables.TemplateItem> items,
        string template,
        int stack,
        SoftcoreChangeLog log)
    {
        if (items.TryGetValue(template, out var item) && item.Properties is not null)
        {
            item.Properties.StackMaxSize = stack;
            log.Changed();
        }
        else
        {
            log.Warn($"未找到货币 {template}，跳过堆叠设置");
        }
    }

    private static void PushToSpecialSlots(SoftcoreContext context, string itemId, SoftcoreChangeLog log) =>
        PushToSpecialSlots(context, new[] { itemId }, log);

    /// <summary>把物品 id 追加到两个口袋模板的全部槽位允许列表（源 pushToSpecialSlots）。</summary>
    private static void PushToSpecialSlots(SoftcoreContext context, string[] itemIds, SoftcoreChangeLog log)
    {
        foreach (var pocket in SpecialSlotPockets)
        {
            if (!context.Tables.Templates.Items.TryGetValue(pocket, out var pocketItem)
                || pocketItem.Properties?.Slots is not { } slots)
            {
                log.Warn($"未找到口袋模板 {pocket}，跳过特殊槽");
                continue;
            }

            foreach (var slot in slots)
            {
                var filter = slot.Properties?.Filters?.FirstOrDefault()?.Filter;
                if (filter is null)
                {
                    continue;
                }

                foreach (var itemId in itemIds)
                {
                    if (filter.Add((MongoId)itemId))
                    {
                        log.Changed();
                    }
                }
            }
        }
    }
}
