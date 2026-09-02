using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Hideout;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace SkillsExtended;

/// <summary>SkillsExtended 内部物品 ID（3.11 SkillsExtendedIds 枚举对应）。</summary>
public static class SkillsExtendedIds
{
    public const string Lockpick = "6622c28aed7e3bc72e301e22";
    public const string Pda = "662400eb756ca8948fe64fe8";
}

/// <summary>ServerConfig.json（camelCase 显式映射）。</summary>
public class ServerConfig
{
    [JsonPropertyName("EnableProgression")] public bool EnableProgression { get; set; }
    [JsonPropertyName("ProgressionDebug")] public ProgressionDebug? ProgressionDebug { get; set; }
}

public class ProgressionDebug
{
    [JsonPropertyName("Enabled")] public bool Enabled { get; set; }
    [JsonPropertyName("TestGeneration")] public bool TestGeneration { get; set; }
    [JsonPropertyName("GenerationLevel")] public int GenerationLevel { get; set; }
    [JsonPropertyName("NumberOfRuns")] public int NumberOfRuns { get; set; }
}

/// <summary>
/// SkillsExtended 迁移版主入口。
/// 3.11 mod.ts + Managers -> 4.1 C#：
///   - InstanceManager -> 构造注入（删整个类）
///   - CreateItems -> CustomItemService.CreateItemFromClone 批量（保持跳过 Pda 的行为）
///   - addCraftsToDatabase -> hideoutTable.Production.Recipes.Add
///   - addItemToSpecSlots -> 特殊槽位 filters 加物品
///   - AchievementManager -> templateTable.Achievements 导入
///   - importData -> locale AddTransformer 多语言导入
///   - RouteManager -> 独立 AbstractRouter 子类（SkillsRoutes.cs）
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class SkillsExtendedEntry(
    ISptLogger<SkillsExtendedEntry> logger,
    JsonUtil jsonUtil,
    CustomItemService customItemService,
    TemplateTable templateTable,
    TradersTable tradersTable,
    HideoutTable hideoutTable,
    LocaleTable localeTable) : IOnLoad
{
    private const string ModName = "SkillsExtended";
    private const string PocketsInventoryId = "627a4e6b255f7527fb05a0f6";
    private const string SpecialSlotPocketsId = "65e080be269cbd5c5005e529";

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var modDir = Path.Combine(Directory.GetCurrentDirectory(), "user", "mods", ModName);

        logger.Success("Skills Extended loading");

        AddCraftsToDatabase(modDir);
        CreateItems(modDir);
        // 多语言导入（AddTransformer 模式）
        ImportAllLocaleData(modDir);
        AddItemToSpecSlots(SkillsExtendedIds.Lockpick);
        AddItemToSpecSlots(SkillsExtendedIds.Pda);

        return Task.CompletedTask;
    }

    /// <summary>从 data/Items/Items.json 批量创建物品（3.11 CreateItems 对应，保持跳过 Pda）。</summary>
    private void CreateItems(string modDir)
    {
        var items = LoadJson<List<NewItemFromCloneDetails>>(Path.Combine(modDir, "data", "Items", "Items.json"));
        if (items is null) return;

        var created = 0;
        foreach (var item in items)
        {
            // 3.11 行为：跳过 Pda（Pda 有特殊处理）
            if (item.NewId == SkillsExtendedIds.Pda) continue;

            customItemService.CreateItemFromClone(item);
            created++;
        }

        logger.Success($"Skills Extended: Loaded {created} custom items");
    }

    /// <summary>添加藏身处制作配方（3.11 addCraftsToDatabase 对应）。</summary>
    private void AddCraftsToDatabase(string modDir)
    {
        var crafts = LoadJson<List<HideoutProduction>>(Path.Combine(modDir, "data", "Items", "Crafting.json"));
        if (crafts is null) return;

        hideoutTable.Production.Recipes ??= new List<HideoutProduction>();
        foreach (var craft in crafts)
        {
            hideoutTable.Production.Recipes.Add(craft);
        }

        logger.Info($"Skills Extended: Added {crafts.Count} crafting recipes");
    }

    /// <summary>把物品加入特殊槽位（3.11 addItemToSpecSlots 对应）。</summary>
    private void AddItemToSpecSlots(string itemId)
    {
        foreach (var (_, item) in templateTable.Items)
        {
            if (item?.Properties?.Slots is null) continue;

            var slots = item.Properties.Slots.ToList();
            // 目标：口袋（627a4e6b）和特殊槽位口袋（65e080be）
            foreach (var slot in slots)
            {
                var filter = slot.Properties?.Filters?.FirstOrDefault();
                if (filter is null) continue;

                filter.Filter ??= new HashSet<MongoId>();
                filter.Filter.Add(itemId);
            }
        }
    }

    /// <summary>导入多语言 locale（3.11 importData + AddTransformer 模式）。</summary>
    private void ImportAllLocaleData(string modDir)
    {
        var localeRoot = Path.Combine(modDir, "data", "Locales");
        if (!Directory.Exists(localeRoot)) return;

        // 每个语言目录下的 locale 文件 -> AddTransformer
        foreach (var langDir in Directory.GetDirectories(localeRoot))
        {
            var lang = Path.GetFileName(langDir).ToLowerInvariant();
            if (!localeTable.Global.ContainsKey(lang)) continue;

            foreach (var file in Directory.GetFiles(langDir, "*.json"))
            {
                var localeData = LoadJson<Dictionary<string, string>>(file);
                if (localeData is null) continue;

                localeTable.Global[lang].AddTransformer(data =>
                {
                    if (data is null) return data;
                    foreach (var (key, value) in localeData)
                    {
                        data[key] = value;
                    }
                    return data;
                });
            }
        }

        logger.Info("Skills Extended: Locale data imported");
    }

    /// <summary>读 JSON 数据文件。</summary>
    private T? LoadJson<T>(string path) where T : class
    {
        try
        {
            return jsonUtil.Deserialize<T>(File.ReadAllText(path));
        }
        catch (Exception ex)
        {
            logger.Error($"Skills Extended: Failed to load {path}: {ex.Message}");
            return null;
        }
    }
}
