// TGC（Tactical Gear Component）4.1.2 移植版
// 基于反编译源码重写：核心功能（自定义物品注册 + 衣服 + 商人 assort + locale）
// 数据库访问用 4.1 表模型注入（TradersTable/LocaleTable/TemplateTable）
using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Utils.Json;
using TGC.Models;
using WTTServerCommonLib;
using WTTServerCommonLib.Services;

namespace TGC;

[Injectable(InjectionType.Singleton)]
public class TGC(
    ISptLogger<TGC> logger,
    WTTServerCommonLib.WTTServerCommonLib wttCommon,
    ModHelper modHelper,
    TradersTable tradersTable,
    LocaleTable localeTable)
    : IOnLoad
{
    private Dictionary<MongoId, Trader> _traders => (Dictionary<MongoId, Trader>)(object)tradersTable;
    private Dictionary<string, LazyLoad<GlobalLocaleDictionary>> _globalLocales => localeTable.Global;

    public async Task OnLoadAsync(CancellationToken cancellationToken)
    {
        string pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        Assembly assembly = Assembly.GetExecutingAssembly();

        // 注册自定义物品（TGC 装备/衣服，WTT 服务读取 db/CustomItems 目录）
        await wttCommon.CustomItemServiceExtended.CreateCustomItems(assembly, null);

        // 加载 mod 数据
        var modItems = modHelper.GetJsonDataFromFile<Dictionary<MongoId, ModItems>>(pathToMod, "db/CustomItems/modTGC_items.json");
        var modClothing = modHelper.GetJsonDataFromFile<Dictionary<MongoId, ModClothing>>(pathToMod, "db/modTGC_clothes.json");
        var modAssort = modHelper.GetJsonDataFromFile<ModTradersAssort>(pathToMod, "db/traders/668aaff35fd574b6dcc4a686/assort.json");
        var modConfig = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config/config.json");

        // 商人 assort 注册（TraderItems → TraderAssort.Items）
        var traderId = new MongoId("668aaff35fd574b6dcc4a686");
        if (modAssort?.TraderItems is not null && _traders.TryGetValue(traderId, out var trader))
        {
            trader.Assort = new TraderAssort
            {
                Items = modAssort.TraderItems.Select(i => new Item
                {
                    Id = i.Id,
                    Template = i.Tpl,
                    ParentId = i.ParentId is null ? null : new MongoId(i.ParentId),
                    SlotId = i.SlotId
                }).ToList(),
                BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
                LoyalLevelItems = new Dictionary<MongoId, int>()
            };
        }

        // 物品/衣服 locale 注册
        foreach (var (lang, lazyLocale) in _globalLocales)
        {
            lazyLocale.AddTransformer(dict =>
            {
                // ModItems 无 Name 字段（名称由 WTT CustomItemService 处理）
                foreach (var (id, clothing) in modClothing)
                {
                    foreach (var (lang2, localeDetails) in clothing.Locales ?? new Dictionary<string, LocaleDetails>())
                    {
                        if (localeDetails.Name is not null)
                        {
                            dict[$"{id} Name"] = localeDetails.Name;
                        }
                    }
                }
                return dict;
            });
        }

        logger.Success("TGC loaded (custom items + trader + locales)");
    }
}
