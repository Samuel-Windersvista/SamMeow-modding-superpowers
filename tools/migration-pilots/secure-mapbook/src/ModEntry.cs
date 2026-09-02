using System.Text.Json.Serialization;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;

namespace SecureMapbookMod;

/// <summary>mod 配置（3.11 config/config.json 原样搬运，camelCase 显式映射）。</summary>
public class ModConfig
{
    [JsonPropertyName("enableDebugging")] public bool EnableDebugging { get; set; }
    [JsonPropertyName("mapbookItemId")] public string MapbookItemId { get; set; } = "";
    [JsonPropertyName("traderId")] public string TraderId { get; set; } = "";
    [JsonPropertyName("price")] public double Price { get; set; }
    [JsonPropertyName("loyaltyLevel")] public int LoyaltyLevel { get; set; }
    [JsonPropertyName("allowInsurance")] public bool AllowInsurance { get; set; }
    [JsonPropertyName("allowInSecureContainers")] public bool AllowInSecureContainers { get; set; }
    [JsonPropertyName("allowInSpecialSlots")] public bool AllowInSpecialSlots { get; set; }
    [JsonPropertyName("secureContainers")] public Dictionary<string, string> SecureContainers { get; set; } = new();
    [JsonPropertyName("organizationalPouch")] public Dictionary<string, string> OrganizationalPouch { get; set; } = new();
    [JsonPropertyName("maps")] public Dictionary<string, string> Maps { get; set; } = new();
}

/// <summary>
/// SecureMapbookMod 迁移版主入口。
/// 3.11 mod.ts -> 4.1 C#：
///   - container.resolve("CustomItemService") -> 构造注入 CustomItemService
///   - container.resolve("DatabaseServer").getTables() -> 构造注入 TemplateTable + TradersTable
///   - createItemFromClone -> CustomItemService.CreateItemFromClone（同步）
///   - trader.assort.items.push -> TradersTable[id].Assort.Items.Add
///   - bundle: bundles.json + bundles/ 原样搬运（IsBundleMod 移除，4.1 检测 bundles.json）
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class SecureMapbookModEntry(
    ISptLogger<SecureMapbookModEntry> logger,
    JsonUtil jsonUtil,
    CustomItemService customItemService,
    TemplateTable templateTable,
    TradersTable tradersTable) : IOnLoad
{
    private const string ModName = "SecureMapbookMod";
    private const string PocketsInventoryId = "627a4e6b255f7527fb05a0f6";
    private const string HideoutParentId = "hideout";
    private const string RoublesTemplateId = "5449016a4bdc2d6f028b456f";

    private ModConfig _config = new();

    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        // 加载配置（Stage 0 数据搬运）
        var modDir = Path.Combine(Directory.GetCurrentDirectory(), "user", "mods", ModName);
        _config = LoadConfig(Path.Combine(modDir, "config", "config.json"));

        if (_config.EnableDebugging) logger.Info("[SecureMapbookMod] Starting initialization...");

        CreateMapbookItem();
        AddToTraderAssortment();
        ConfigureItemPermissions();
        DisableMapsInsurance();

        if (_config.EnableDebugging) logger.Info("[SecureMapbookMod] Initialization complete");
        return Task.CompletedTask;
    }

    private ModConfig LoadConfig(string path)
    {
        try
        {
            var text = File.ReadAllText(path);
            return jsonUtil.Deserialize<ModConfig>(text) ?? new ModConfig();
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load config: {ex.Message}");
            return new ModConfig();
        }
    }

    private void CreateMapbookItem()
    {
        var clone = new NewItemFromCloneDetails
        {
            ItemTplToClone = "5a9d6d00a2750c5c985b5305",
            OverrideProperties = GetMapbookProperties(),
            ParentId = "55818b224bdc2dde698b456f",
            NewId = _config.MapbookItemId,
            NewItemName = "secure_mapbook",
            FleaPriceRoubles = _config.Price,
            HandbookPriceRoubles = _config.Price,
            HandbookParentId = "5b47574386f77428ca22b2f1",
            Locales = new Dictionary<string, LocaleDetails>
            {
                ["en"] = new()
                {
                    Name = "安全地图册",
                    ShortName = "地图册",
                    Description = "一本精心制作的图册，专用于存放和整理地图…….",
                },
            },
        };

        var result = customItemService.CreateItemFromClone(clone);
        if (_config.EnableDebugging)
        {
            logger.Info($"[SecureMapbookMod] Created item {_config.MapbookItemId} success={result.Success}");
        }
    }

    private TemplateItemProperties? GetMapbookProperties()
    {
        return new TemplateItemProperties
        {
            Name = "Secure Mapbook",
            ShortName = "Mapbook",
            Description = "A meticulously crafted book designed for storing and organizing maps...",
            Prefab = new Prefab
            {
                Path = "assets/content/items/barter/item_mapbook/mapbook.bundle",
                Rcid = "",
            },
            Width = 1,
            Height = 2,
            InsuranceDisabled = !_config.AllowInsurance,
            CanSellOnRagfair = false,
            ItemSound = "item_book",
            Slots = GenerateMapSlots(),
        };
    }

    private List<Slot>? GenerateMapSlots()
    {
        var slots = new List<Slot>();
        var mapEntries = _config.Maps.ToList();
        for (var i = 0; i < mapEntries.Count; i++)
        {
            var mapId = mapEntries[i].Value;
            // 修复 3.11 原 bug：原代码用 (char)(98+i) 生成 'b'..'l'，其中 'g'..'l' 非 hex 导致 MongoId 非法。
            // 迁移版改用合法 hex：0-9a-f 循环。
            var hexChar = "0123456789abcdef"[i % 16];
            slots.Add(new Slot
            {
                Name = $"mod_mount_{(i + 1):D2}",
                Id = $"5d235bb686f77443f433127{hexChar}",
                Parent = "55818b224bdc2dde698b456f",
                Properties = new SlotProperties
                {
                    Filters = new List<SlotFilter>
                    {
                        new() { Filter = new HashSet<MongoId> { mapId } },
                    },
                },
            });
            if (_config.EnableDebugging) logger.Info($"[SecureMapbookMod] Added slot for {mapEntries[i].Key} ({mapId})");
        }
        return slots;
    }

    private void AddToTraderAssortment()
    {
        if (!tradersTable.TryGetValue(_config.TraderId, out var trader) || trader?.Assort is null)
        {
            logger.Error($"[SecureMapbookMod] Trader {_config.TraderId} not found!");
            return;
        }

        trader.Assort.Items.Add(new Item
        {
            Id = _config.MapbookItemId,
            Template = _config.MapbookItemId,
            ParentId = HideoutParentId,
            SlotId = HideoutParentId,
            // 对齐 3.11 的 upd 字段：Upd 必须非 null（4.1 ResetBuyRestrictionCurrentValue 访问 assort.Upd.BuyRestrictionCurrent）
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 99999,
            },
        });

        trader.Assort.BarterScheme[_config.MapbookItemId] =
        [
            new List<BarterScheme>
            {
                new() { Count = _config.Price, Template = RoublesTemplateId },
            },
        ];

        trader.Assort.LoyalLevelItems[_config.MapbookItemId] = _config.LoyaltyLevel;

        if (_config.EnableDebugging)
        {
            logger.Info($"[SecureMapbookMod] Added to trader {_config.TraderId} for {_config.Price} roubles (LL{_config.LoyaltyLevel})");
        }
    }

    private void ConfigureItemPermissions()
    {
        if (_config.AllowInSpecialSlots) AllowInSpecialSlots();
        if (_config.AllowInSecureContainers) AllowInSecureContainers();
    }

    private void AllowInSpecialSlots()
    {
        if (!templateTable.Items.TryGetValue(PocketsInventoryId, out var pockets) || pockets?.Properties is null)
        {
            logger.Error("[SecureMapbookMod] Pockets inventory not found!");
            return;
        }

        var itemsToAdd = new List<string> { _config.MapbookItemId };
        itemsToAdd.AddRange(_config.Maps.Values);

        try
        {
            var slots = pockets.Properties.Slots?.ToList() ?? new List<Slot>();
            for (var i = 0; i < 3 && i < slots.Count; i++)
            {
                var slot = slots[i];
                var filter = slot.Properties?.Filters?.FirstOrDefault();
                if (filter is null)
                {
                    filter = new SlotFilter { Filter = new HashSet<MongoId>() };
                    slot.Properties ??= new SlotProperties();
                    slot.Properties.Filters ??= new List<SlotFilter>();
                    slot.Properties.Filters = slot.Properties.Filters.Append(filter).ToList();
                }
                filter.Filter ??= new HashSet<MongoId>();

                foreach (var itemId in itemsToAdd)
                {
                    filter.Filter.Add(itemId);
                }
            }

            if (_config.EnableDebugging) logger.Info("[SecureMapbookMod] Completed adding items to special slots");
        }
        catch (Exception ex)
        {
            logger.Error($"[SecureMapbookMod] Error modifying pocket slots: {ex.Message}");
        }
    }

    private void AllowInSecureContainers()
    {
        var containerIds = new List<string>();
        containerIds.AddRange(_config.SecureContainers.Values);
        containerIds.AddRange(_config.OrganizationalPouch.Values);

        foreach (var containerId in containerIds)
        {
            try
            {
                if (!templateTable.Items.TryGetValue(containerId, out var container) || container?.Properties?.Grids is null)
                {
                    continue;
                }

                var grids = container.Properties.Grids.ToList();
                if (grids.Count == 0) continue;

                var gridFilters = grids[0].Properties?.Filters?.ToList() ?? new List<GridFilter>();
                if (gridFilters.Count == 0)
                {
                    grids[0].Properties ??= new GridProperties();
                    grids[0].Properties.Filters = new List<GridFilter> { new() { Filter = new HashSet<MongoId> { _config.MapbookItemId } } };
                    container.Properties.Grids = grids;
                }
                else
                {
                    var first = gridFilters.FirstOrDefault();
                    if (first is null)
                    {
                        gridFilters.Add(new GridFilter { Filter = new HashSet<MongoId> { _config.MapbookItemId } });
                    }
                    else
                    {
                        first.Filter ??= new HashSet<MongoId>();
                        first.Filter.Add(_config.MapbookItemId);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error($"[SecureMapbookMod] Failed to modify container {containerId}: {ex.Message}");
            }
        }
    }

    private void DisableMapsInsurance()
    {
        foreach (var mapId in _config.Maps.Values)
        {
            if (templateTable.Items.TryGetValue(mapId, out var mapItem) && mapItem?.Properties is not null)
            {
                mapItem.Properties.InsuranceDisabled = true;
                if (_config.EnableDebugging) logger.Info($"[SecureMapbookMod] Disabled insurance for map: {mapId}");
            }
            else if (_config.EnableDebugging)
            {
                logger.Warning($"[SecureMapbookMod] Map not found in DB: {mapId}");
            }
        }
    }
}
