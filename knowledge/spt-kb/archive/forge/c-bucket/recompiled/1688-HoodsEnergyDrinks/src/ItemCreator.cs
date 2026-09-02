// ItemCreator（4.0 → 4.1.2 重写）
// 4.1.2 变更：
//   1. DatabaseServer.GetTables().Globals → 注入 GlobalTable.Configuration
//   2. NewItemFromCloneDetails 新增必填 NewItemName
//   3. Buff 类型命名空间迁移至 SPTarkov.Server.Core.Models.Spt.Tables
//   4. MongoId.op_Implicit(string) → 字符串隐式转换
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Modding.Custom;

namespace HoodsEnergyDrinks_CSharp;

internal class ItemCreator
{
    public Loot Loot { get; } = new();

    private readonly ModConfig _config;

    private readonly Drink _drinks;

    public ItemCreator(ModConfig config, Drink drinks)
    {
        _config = config;
        _drinks = drinks;
    }

    public void BuildItems(GlobalTable globalTable, CustomItemService customItemService, ModHelper modHelper)
    {
        string absolutePathToModFolder = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
        EnergyDrinkBuffs buffInfo = modHelper.GetJsonDataFromFile<EnergyDrinkBuffs>(absolutePathToModFolder, "EnergyDrinkBuffs.json");

        foreach (KeyValuePair<string, DrinkProps> item in _drinks.Items)
        {
            string key = item.Key;
            DrinkProps drinkProps = item.Value;

            IEnumerable<Buff> buffs = _config.use_alternate_buffs ? buffInfo.buffs["alternate_buffs"] : buffInfo.buffs[key];
            string buffName = _config.use_alternate_buffs ? "alternate_buffs" : key;

            if (_config.instant_energy_and_hydration && _config.use_alternate_buffs)
            {
                globalTable.Configuration.Health.Effects.Stimulator.Buffs[key] = _config.drinks[key].buff_effect_enable
                    ? RemoveEnergyHydration(buffInfo, buffName)
                    : new List<Buff>();
            }
            else
            {
                globalTable.Configuration.Health.Effects.Stimulator.Buffs[key] = _config.drinks[key].buff_effect_enable ? buffs : [];
            }

            NewItemFromCloneDetails details = new()
            {
                ItemTplToClone = "5d40407c86f774318526545a",
                OverrideProperties = new TemplateItemProperties
                {
                    Prefab = new Prefab
                    {
                        Path = "assets/" + key + ".bundle",
                        Rcid = ""
                    },
                    UsePrefab = new Prefab
                    {
                        Path = "assets/" + key + "_container.bundle",
                        Rcid = ""
                    },
                    DiscardLimit = -1.0,
                    Weight = 0.6,
                    FoodUseTime = 5.0,
                    StimulatorBuffs = key,
                    EffectsHealth = _config.instant_energy_and_hydration ? SetInstantEnergyHydration(buffInfo, buffName) : new Dictionary<HealthFactor, EffectsHealthProperties>(),
                    EffectsDamage = new Dictionary<DamageEffectType, EffectsDamageProperties>()
                },
                ParentId = "5448e8d64bdc2dce718b4568",
                NewId = drinkProps._id,
                NewItemName = drinkProps.name,
                FleaPriceRoubles = _config.drinks[key].flea_price,
                HandbookPriceRoubles = _config.drinks[key].handbook_price,
                HandbookParentId = "5b47574386f77428ca22b335",
                Locales = new Dictionary<string, LocaleDetails>
                {
                    {
                        "en",
                        new LocaleDetails
                        {
                            Name = drinkProps.name,
                            ShortName = drinkProps.shortName,
                            Description = drinkProps.desc
                        }
                    }
                }
            };

            Loot.StaticLoot[drinkProps._id] = new StaticLoot
            {
                Weights = new Dictionary<string, float>(_config.drinks[key].loot_multipliers!)
            };
            Loot.LooseLoot[drinkProps._id] = _config.drinks[key].loose_loot_multiplier;

            customItemService.CreateItemFromClone(details);
        }
    }

    public Dictionary<HealthFactor, EffectsHealthProperties>? SetInstantEnergyHydration(EnergyDrinkBuffs buffInfo, string name)
    {
        buffInfo.buffs.TryGetValue(name, out IEnumerable<Buff>? buffs);
        List<Buff> list = buffs!.ToList();
        return new Dictionary<HealthFactor, EffectsHealthProperties>
        {
            [HealthFactor.Energy] = new EffectsHealthProperties
            {
                Value = list[0].Duration * list[0].Value
            },
            [HealthFactor.Hydration] = new EffectsHealthProperties
            {
                Value = list[1].Duration * list[1].Value
            }
        };
    }

    public List<Buff> RemoveEnergyHydration(EnergyDrinkBuffs buffInfo, string name)
    {
        buffInfo.buffs.TryGetValue(name, out IEnumerable<Buff>? buffs);
        List<Buff> list = buffs!.ToList();
        list.RemoveRange(0, 2);
        return list;
    }
}
