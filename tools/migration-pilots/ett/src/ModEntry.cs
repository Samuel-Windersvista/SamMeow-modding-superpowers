using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Json;
using Path = System.IO.Path;

namespace ExpandedTaskText;

/// <summary>ETT 配置（3.11 config/config.json 原样搬运）。注意 camelCase 映射。</summary>
public class ExpandedTaskTextConfig
{
    [JsonPropertyName("ShowCollectorRequirements")]
    public bool ShowCollectorRequirements { get; set; } = true;

    [JsonPropertyName("ShowLightKeeperRequirements")]
    public bool ShowLightKeeperRequirements { get; set; } = true;

    [JsonPropertyName("ShowGunsmithRequiredParts")]
    public bool ShowGunsmithRequiredParts { get; set; } = true;

    [JsonPropertyName("ShowNextQuestInChain")]
    public bool ShowNextQuestInChain { get; set; } = true;

    [JsonPropertyName("ShowTimeUntilNextQuest")]
    public bool ShowTimeUntilNextQuest { get; set; } = true;
}

/// <summary>QuestInfo.json 模型（3.11 IQuestInfoModel 对应）。
/// 注意：SPT JsonUtil 默认大小写敏感，必须显式 JsonPropertyName 映射 camelCase。</summary>
public class QuestInfoModel
{
    [JsonPropertyName("wikiLink")]
    public string WikiLink { get; set; } = "";

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("kappaRequired")]
    public bool KappaRequired { get; set; }

    [JsonPropertyName("lightkeeperRequired")]
    public bool LightkeeperRequired { get; set; }

    [JsonPropertyName("objectives")]
    public List<QuestObjective>? Objectives { get; set; }
}

public class QuestObjective
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>requiredKeys 是嵌套数组：List&lt;List&lt;Dictionary&lt;id, name&gt;&gt;&gt;（外层=key 组，内层=keys）</summary>
    [JsonPropertyName("requiredKeys")]
    public List<List<Dictionary<string, string>>>? RequiredKeys { get; set; }
}

/// <summary>
/// ETT 迁移版主入口。
/// 3.11 mod.ts -> 4.1 C#：
///   - postDBLoad + InstanceManager -> [Injectable] + 构造注入
///   - database.locales.global 直写 -> LocaleTable.Global[lang].AddTransformer（官方模式）
///   - vfs.read -> File.ReadAllText + JsonUtil.Deserialize
///   - 数据文件（config/QuestInfo/GunsmithLocaleEN）原样搬运
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class ExpandedTaskTextEntry(
    ISptLogger<ExpandedTaskTextEntry> logger,
    JsonUtil jsonUtil,
    TemplateTable templateTable,
    TradersTable tradersTable,
    LocaleTable localeTable) : IOnLoad
{
    private const string ModName = "ExpandedTaskText";

    private readonly Dictionary<string, Quest> _tasks = new();
    private readonly Dictionary<string, Dictionary<string, string>> _locale = new();

    // GunsmithLocaleEN.json（搬运数据）
    private Dictionary<string, Dictionary<string, object?>> _gsLocale = new();

    private List<QuestInfoModel> _questInfo = new();

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var startTime = Stopwatch.StartNew();

        logger.Info("Expanded Task Text is loading please wait...");

        // 加载搬运数据（Stage 0 数据文件）
        var modDir = Path.Combine(Directory.GetCurrentDirectory(), "user", "mods", ModName);
        _questInfo = LoadJson<List<QuestInfoModel>>(Path.Combine(modDir, "db", "QuestInfo.json")) ?? new();
        _gsLocale = LoadJson<Dictionary<string, Dictionary<string, object?>>>(Path.Combine(modDir, "db", "GunsmithLocaleEN.json")) ?? new();
        var config = LoadJson<ExpandedTaskTextConfig>(Path.Combine(modDir, "config", "config.json")) ?? new ExpandedTaskTextConfig();

        // 收集所有任务（database.templates.quests）
        foreach (var (id, quest) in templateTable.Quests)
        {
            _tasks[id] = quest;
        }

        // 注册 locale transformer（官方 AddTransformer 模式，替代 3.11 直写）
        foreach (var (lang, lazy) in localeTable.Global)
        {
            var langCopy = lang;
            var configCopy = config;
            lazy.AddTransformer(data =>
            {
                if (data is null) return data;
                UpdateLocaleTexts(data, langCopy, configCopy);
                return data;
            });
        }

        startTime.Stop();
        logger.Success($"Expanded Task Text startup took {startTime.Elapsed.TotalSeconds:F2} seconds");
        return Task.CompletedTask;
    }

    /// <summary>读 JSON 数据文件（3.11 vfs.read + JSON.parse 对应）。</summary>
    private T? LoadJson<T>(string path) where T : class
    {
        try
        {
            var text = File.ReadAllText(path, Encoding.UTF8);
            return jsonUtil.Deserialize<T>(text);
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load {path}: {ex.Message}");
            return null;
        }
    }

    /// <summary>改写单个语言的 locale 文本（transformer 主体）。</summary>
    private void UpdateLocaleTexts(Dictionary<string, string> locale, string localeId, ExpandedTaskTextConfig config)
    {
        var modifiedQuestIds = new HashSet<string>();

        foreach (var info in _questInfo)
        {
            if (!locale.TryGetValue($"{info.Id} description", out var originalDesc))
            {
                originalDesc = "";
            }

            var collector = config.ShowCollectorRequirements && info.KappaRequired
                ? "This quest is required for Collector \n \n"
                : "";

            var lightKeeper = config.ShowLightKeeperRequirements && info.LightkeeperRequired
                ? "This quest is required for Lightkeeper \n \n"
                : "";

            var keyDesc = BuildKeyText(info.Objectives, locale, localeId);
            var nextQuest = GetAllNextQuestsInChain(info.Id, locale);
            var leadsTo = config.ShowNextQuestInChain
                ? (nextQuest.Length > 0 ? $"Leads to: {nextQuest} \n \n" : "Leads to: Nothing \n \n")
                : "";

            var durability = "";
            var requiredParts = "";
            if (config.ShowGunsmithRequiredParts && _gsLocale.ContainsKey(info.Id))
            {
                durability = "Required Durability: 60 \n";
                requiredParts = $"{GetAndBuildPartsList(info.Id, locale)} \n \n";
            }

            modifiedQuestIds.Add(info.Id);

            locale[$"{info.Id} description"] =
                collector + lightKeeper + leadsTo + keyDesc + durability + requiredParts + originalDesc;
        }

        // 自定义商人的任务（未被 QuestInfo 覆盖的）
        foreach (var (questId, _) in _tasks)
        {
            if (modifiedQuestIds.Contains(questId)) continue;

            if (locale.TryGetValue($"{questId} description", out var originalDesc))
            {
                var nextQuest = GetAllNextQuestsInChain(questId, locale);
                var leadsTo = config.ShowNextQuestInChain
                    ? (nextQuest.Length > 0 ? $"Leads to: {nextQuest} \n \n" : "Leads to: Nothing \n \n")
                    : "";
                locale[$"{questId} description"] = leadsTo + originalDesc;
            }
        }
    }

    /// <summary>递归找任务链后续（3.11 getAllNextQuestsInChain 对应）。</summary>
    private string GetAllNextQuestsInChain(string currentQuestId, Dictionary<string, string> locale)
    {
        var nextQuests = new List<string>();

        foreach (var (key, quest) in _tasks)
        {
            var conditions = quest.Conditions?.AvailableForStart;
            if (conditions is null) continue;

            foreach (var condition in conditions)
            {
                if (condition.ConditionType == "Quest" && TargetMatches(condition.Target, currentQuestId))
                {
                    if (locale.TryGetValue($"{key} name", out var nextQuestName))
                    {
                        nextQuests.Add(nextQuestName);
                        // 递归
                        nextQuests.AddRange(GetAllNextQuestsInChain(nextQuestName, locale).Split(", ", StringSplitOptions.RemoveEmptyEntries));
                    }
                }
            }
        }

        return string.Join(", ", nextQuests);
    }

    /// <summary>处理 ListOrT&lt;string&gt; Target 与 string 的比较。</summary>
    private static bool TargetMatches(ListOrT<string>? target, string questId)
    {
        if (target is null) return false;
        if (target.IsItem) return target.Item == questId;
        return target.List is { } list && list.Contains(questId);
    }

    /// <summary>构建钥匙文本（3.11 buildKeyText 对应，requiredKeys 为嵌套数组）。</summary>
    private string BuildKeyText(List<QuestObjective>? objectives, Dictionary<string, string> locale, string localeId)
    {
        var keyDesc = "";
        if (objectives is null) return keyDesc;

        foreach (var obj in objectives)
        {
            if (obj.RequiredKeys is null) continue;

            if (locale.TryGetValue(obj.Id, out var objDesc))
            {
                var keys = "";
                foreach (var keysGroup in obj.RequiredKeys)
                {
                    foreach (var keyObj in keysGroup)
                    {
                        if (keyObj.TryGetValue("id", out var keyId))
                        {
                            var localeKey = $"{keyId} Name";
                            if (locale.TryGetValue(localeKey, out var localEntry))
                            {
                                keys += $"    {localEntry}\n";
                            }
                        }
                    }
                }

                if (keys.Length > 0)
                {
                    keyDesc += $"{objDesc}\n Requires key(s):\n{keys}";
                }
            }
        }

        return keyDesc.Length > 0 ? $"{keyDesc} \n" : "";
    }

    /// <summary>构建枪匠零件文本（3.11 getAndBuildPartsList 对应）。</summary>
    private string GetAndBuildPartsList(string taskId, Dictionary<string, string> locale)
    {
        var partIds = _gsLocale.TryGetValue(taskId, out var entry)
            && entry.TryGetValue("RequiredParts", out var parts)
            ? parts as List<string>
            : null;

        if (partIds is null || partIds.Count == 0) return "";

        var localizedParts = new List<string>();
        var loyalLevelItems = GetAllTraderLoyalLevelItems();

        foreach (var part in partIds)
        {
            var partString = locale.TryGetValue($"{part} Name", out var name) ? name : part;

        foreach (var (_, trader) in tradersTable)
        {
            var assortItems = trader?.Assort?.Items;
            if (assortItems is null) continue;

            foreach (var item in assortItems)
            {
                if (part == item.Template.ToString() && loyalLevelItems.TryGetValue(item.Id.ToString(), out var level))
                {
                    var traderId = trader?.Base?.Id.ToString() ?? "";
                    var traderNickname = locale.TryGetValue($"{traderId} Nickname", out var nickname) ? nickname : traderId;
                    partString += $"\n    Sold by ({traderNickname} LL {level})";
                }
            }
        }

            localizedParts.Add(partString);
        }

        return string.Join("\n\n", localizedParts);
    }

    /// <summary>收集所有商人的忠诚等级物品（3.11 getAllTraderLoyalLevelItems 对应）。</summary>
    private Dictionary<string, double> GetAllTraderLoyalLevelItems()
    {
        var result = new Dictionary<string, double>();
        foreach (var (_, trader) in tradersTable)
        {
            var loyal = trader?.Assort?.LoyalLevelItems;
            if (loyal is null) continue;
            foreach (var (itemId, level) in loyal)
            {
                result[itemId] = level;
            }
        }
        return result;
    }
}
