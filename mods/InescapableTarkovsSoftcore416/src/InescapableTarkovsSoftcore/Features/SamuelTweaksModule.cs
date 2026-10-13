using InescapableTarkovsSoftcore.Config;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;

namespace InescapableTarkovsSoftcore.Features;

/// <summary>
/// G1 Samuel's Tweaks：护甲弹挂冲突修复 / 可掠夺臂章与近战 / 扩展弹匣缩格。
/// <para>
/// 启动器背景（customBackground）为移除项（v0.1.0 不随移植源），本模块运行时不再有对应操作。
/// </para>
/// <para>执行顺序 = 200（D5：softcore=100 → samuelTweaks=200 → antigravArmbands=300 → ...）。</para>
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class SamuelTweaksModule(
    ISptLogger<SamuelTweaksModule> logger) : FeatureModule<SamuelTweaksConfig>
{
    /// <summary>D5 顺序常量。</summary>
    public const int OrderValue = 200;

    /// <summary>臂章父类 ID。</summary>
    public const string ArmbandParentId = "5447e1d04bdc2dff2f8b4567";

    /// <summary>近战武器类目 ID。</summary>
    public const string MeleeWeaponParentId = "5b3f15d486f77432d0509248";

    /// <summary>弹匣类目 ID。</summary>
    public const string MagazineParentId = "5448bc234bdc2d3c308b4569";

    public override string Id => "samuelTweaks";

    public override int Order => OrderValue;

    protected override SamuelTweaksConfig Section(SoftcoreConfig root) => root.SamuelTweaks;

    protected override ModuleReport Apply(ModContext context, SamuelTweaksConfig config)
    {
        var items = context.Tables.TemplateTable.Items;

        var armorFixed = 0;
        if (config.ArmorConflictFix)
        {
            armorFixed = FixArmorConflict(items);
        }

        // 子节可能因配置写 null 而回落为默认实例；空引用保护在解析为非法节时并不触发，
        // 此处对 null 采用「默认启用」语义，避免整组异常。
        var lootable = config.LootableItems ?? new LootableItemsConfig();
        var armbands = 0;
        if (lootable.Armband)
        {
            armbands = MakeLootable(items, ArmbandParentId);
        }

        var melee = 0;
        if (lootable.MeleeWeapons)
        {
            melee = MakeLootable(items, MeleeWeaponParentId);
        }

        var magazine = config.MagazineResize ?? new MagazineResizeConfig();
        var magazines = 0;
        if (magazine.Enabled)
        {
            magazines = ResizeMagazines(items, magazine);
        }

        var changed = armorFixed + armbands + melee + magazines;
        logger.Info(
            $"[ITS] samuelTweaks：护甲冲突修复 {armorFixed}，臂章可掠夺 {armbands}，"
            + $"近战可掠夺 {melee}，弹匣缩格 {magazines}，合计 {changed}");

        return ModuleReport.Ok(Id, changed);
    }

    /// <summary>规则 1：含 RigLayoutName 的弹挂甲 → BlocksArmorVest=false。</summary>
    internal static int FixArmorConflict(Dictionary<MongoId, TemplateItem> items)
    {
        var changed = 0;
        foreach (var item in items.Values)
        {
            var props = item.Properties;
            if (props is null || props.RigLayoutName is null)
            {
                continue;
            }

            props.BlocksArmorVest = false;
            changed++;
        }

        return changed;
    }

    /// <summary>
    /// 规则 2：指定父类、且 <c>Unlootable</c> 存在的物品 → <c>Unlootable=false</c> 且
    /// <c>UnlootableFromSide</c> 清空；缺失者跳过。
    /// <para>
    /// 4.1.6 运行时模型 <c>TemplateItemProperties.Unlootable</c> 为可空 <c>bool?</c>；源
    /// <c>hasProperty(item, "Unlootable")</c> 的桥接取等价实现：仅当 <c>Unlootable</c> 为 true
    /// （真实不可掠夺者）时处理；null / false（可掠夺或未设置）者跳过，避免无意义计数。
    /// 目标物品（臂章 / 近战）本即 Unlootable=true，故行为与源一致。
    /// </para>
    /// </summary>
    internal static int MakeLootable(Dictionary<MongoId, TemplateItem> items, string parentId)
    {
        var changed = 0;
        foreach (var item in items.Values)
        {
            if (!item.Parent.Equals(parentId))
            {
                continue;
            }

            var props = item.Properties;
            if (props is null || props.Unlootable != true)
            {
                continue;
            }

            props.Unlootable = false;
            props.UnlootableFromSide = Array.Empty<PlayerSideMask>();
            changed++;
        }

        return changed;
    }

    /// <summary>
    /// 规则 3：弹匣（父类 = MAGAZINE）宽 1、高 &gt;2、容量在 [MinCapacity, MaxCapacity] →
    /// 高置 2；ExtraSizeDown &gt;0 时再减 1（不跌破 0）。
    /// <para>
    /// 源 <c>!height || height &lt;= 2</c>：运行时模型 <c>Height</c> 为非空 <c>int</c>（缺省 0），
    /// 「缺失」即 0，已由 <c>Height &lt;= 2</c> 覆盖（源 <c>!height</c> 分支在 C# 无对应可空语义）。
    /// </para>
    /// </summary>
    internal static int ResizeMagazines(Dictionary<MongoId, TemplateItem> items, MagazineResizeConfig config)
    {
        var changed = 0;
        foreach (var item in items.Values)
        {
            var props = item.Properties;
            if (props is null
                || !item.Parent.Equals(MagazineParentId)
                || props.Width != 1
                || props.Height <= 2)
            {
                continue;
            }

            var capacity = GetMagazineCapacity(props);
            if (capacity is null || capacity < config.MinCapacity || capacity > config.MaxCapacity)
            {
                continue;
            }

            props.Height = 2;
            if (props.ExtraSizeDown > 0)
            {
                props.ExtraSizeDown -= 1;
            }

            changed++;
        }

        return changed;
    }

    /// <summary>取首个带数值容量的 Cartridges 槽位（对应源实现 `_max_count`）。</summary>
    private static double? GetMagazineCapacity(TemplateItemProperties props)
    {
        if (props.Cartridges is null)
        {
            return null;
        }

        foreach (var slot in props.Cartridges)
        {
            if (slot?.MaxCount is { } capacity)
            {
                return capacity;
            }
        }

        return null;
    }
}
