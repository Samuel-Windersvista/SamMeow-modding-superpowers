// ROTraderDynamicRouter: SPT 4.0 -> 4.1.2 迁移
// RouteAction 动作签名增加 CancellationToken 参数
// DatabaseService.GetTrader -> TradersTable.GetTrader
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using RaidOverhaulMain.Helpers;
using RaidOverhaulMain.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Callbacks;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Items;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace RaidOverhaulMain.Routers;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class ROTraderDynamicRouter : DynamicRouter
{
	private static long _lastKnownResupply;

	public ROTraderDynamicRouter(JsonUtil jsonUtil, RandomUtil randomUtil, ItemHelper itemHelper, TraderCallbacks traderCallbacks, TradersTable tradersTable, ROHelpers helpers)
		: base(jsonUtil, new RouteAction[]
		{
			new RouteAction("/client/trading/api/getTraderAssort/66f4db5ca4958508883d700c", async (string url, IRequestData info, MongoId sessionId, string? _, CancellationToken _) =>
			{
				string text = helpers.FetchIdFromMap("ReqShop", ClassMaps.TraderMaps);
				Trader? trader = tradersTable.GetTrader((MongoId)text);
				int valueOrDefault = trader.Base.NextResupply.GetValueOrDefault();
				if (_lastKnownResupply == 0L)
				{
					_lastKnownResupply = valueOrDefault;
				}
				if (valueOrDefault != _lastKnownResupply)
				{
					RandomizeStock(trader.Assort.Items, randomUtil, itemHelper);
					_lastKnownResupply = valueOrDefault;
				}
				return await traderCallbacks.GetAssort(url, (info as EmptyRequestData) ?? new EmptyRequestData(), sessionId);
			})
		})
	{
	}

	private static void RandomizeStock(List<Item> assortItems, RandomUtil _randomUtil, ItemHelper _itemHelper)
	{
		foreach (Item assortItem in assortItems)
		{
			if (assortItem.ParentId != "hideout")
			{
				continue;
			}
			if (_itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.AMMO) || _itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.AMMO_BOX))
			{
				assortItem.Upd.StackObjectsCount = _randomUtil.RandInt(50, (int?)300);
			}
			else if (_itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.ARMOR_PLATE))
			{
				assortItem.Upd.StackObjectsCount = _randomUtil.RandInt(0, (int?)10);
			}
			else if (_itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.MEDS) || _itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.MED_KIT) || _itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.MEDICAL_SUPPLIES) || _itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.MEDICAL) || _itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.STIMULATOR))
			{
				assortItem.Upd.StackObjectsCount = _randomUtil.RandInt(0, (int?)10);
			}
			else if (_itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.EQUIPMENT) && !_itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.ARMORED_EQUIPMENT))
			{
				assortItem.Upd.StackObjectsCount = _randomUtil.RandInt(0, (int?)10);
			}
			else if (_itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.MOD))
			{
				assortItem.Upd.StackObjectsCount = _randomUtil.RandInt(0, (int?)10);
			}
			else if (_itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.WEAPON))
			{
				assortItem.Upd.StackObjectsCount = _randomUtil.RandInt(0, (int?)3);
			}
			else if (_itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.BUILDING_MATERIAL) || _itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.BARTER_ITEM) || _itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.FOOD) || _itemHelper.IsOfBaseclass(assortItem.Template, BaseClasses.DRINK))
			{
				assortItem.Upd.StackObjectsCount = _randomUtil.RandInt(0, (int?)10);
			}
			else
			{
				assortItem.Upd.StackObjectsCount = _randomUtil.RandInt(0, (int?)1);
			}
		}
	}
}
