using System.Text.Json.Serialization;

namespace InescapableTarkovsSoftcore.Features.TrueItems;

/// <summary>
/// 单张 True Items 查找表（对应源 mod 的一个 <c>config/*.json</c>）。
/// 字段名与源 JSON 大小写一致（Active / StackMult / List / ParentList）。
/// </summary>
public sealed class TrueItemsTable
{
    /// <summary>该文件是否生效；false 时整表跳过（零变更）。</summary>
    [JsonPropertyName("Active")]
    public bool Active { get; set; } = true;

    /// <summary>最终堆叠 = 条目值 × StackMult。</summary>
    [JsonPropertyName("StackMult")]
    public int StackMult { get; set; } = 1;

    /// <summary>按 <c>_id</c> 精确匹配的条目列表。</summary>
    [JsonPropertyName("List")]
    public List<TrueItemsListEntry> List { get; set; } = [];

    /// <summary>按 <c>_parent</c> 批量匹配的父类条目列表。</summary>
    [JsonPropertyName("ParentList")]
    public List<TrueItemsParentEntry> ParentList { get; set; } = [];
}

/// <summary>List 条目：物品 <c>_id</c> + 目标堆叠值。</summary>
public sealed class TrueItemsListEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("_props")]
    public TrueItemsPropEntry Props { get; set; } = new();
}

/// <summary>List 条目的 <c>_props</c> 子对象：源格式仅含 StackMaxSize。</summary>
public sealed class TrueItemsPropEntry
{
    /// <summary>目标堆叠值；缺失（源 <c>undefined</c>）时为 null，应用时跳过该条目。</summary>
    [JsonPropertyName("StackMaxSize")]
    public int? StackMaxSize { get; set; }
}

/// <summary>ParentList 条目：父类 <c>_id</c> + 目标堆叠值。</summary>
public sealed class TrueItemsParentEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>目标堆叠值；缺失（源 <c>undefined</c>）时为 null，应用时跳过该条目。</summary>
    [JsonPropertyName("StackMaxSize")]
    public int? StackMaxSize { get; set; }
}

/// <summary>六张查找表的聚合（数据源 = 旧包 IMM 覆盖层）。</summary>
public sealed class TrueItemsTables
{
    public TrueItemsTable Barter { get; set; } = new();

    public TrueItemsTable Clothing { get; set; } = new();

    public TrueItemsTable Keycards { get; set; } = new();

    public TrueItemsTable Medicals { get; set; } = new();

    public TrueItemsTable PartsnMods { get; set; } = new();

    public TrueItemsTable Provisions { get; set; } = new();
}
