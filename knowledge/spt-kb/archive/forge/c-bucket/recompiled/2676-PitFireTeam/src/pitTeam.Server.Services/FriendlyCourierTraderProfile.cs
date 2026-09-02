using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Services;

namespace pitTeam.Server.Services;

internal static class FriendlyCourierTraderProfile
{
	public const string CourierTraderIdValue = "67d3a28a3d6f4f7dbd09ed13";

	public const int CourierAid = 1113680;

	public const string CourierNickname = "-P|T- Comms";

	public const string CourierLocation = "Friendly Squad Logistics";

	public const string CourierDescription = "Handles item transfers from your squadmates.";

	public const string CourierAvatarFileName = "pitfireteam-courier.png";

	public const string CourierAvatarPath = "/files/trader/avatar/pitfireteam-courier.png";

	public static readonly MongoId CourierTraderId = new MongoId("67d3a28a3d6f4f7dbd09ed13");

	public static void GetLocalizedIdentity(string? locale, out string nickname, out string location, out string description)
	{
		string text = NormalizeLocale(locale);
		if (text == "ru")
		{
			nickname = "袛芯褋褌邪胁泻邪袨褌褉褟写邪";
			location = "袥芯谐懈褋褌懈泻邪 写褉褍卸械褋褌胁械薪薪芯谐芯 芯褌褉褟写邪";
			description = "袙芯蟹胁褉邪褖邪械褌 锌褉械写屑械褌褘, 锌械褉械写邪薪薪褘械 胁邪褕懈屑懈 斜芯泄褑邪屑懈.";
		}
		else
		{
			nickname = "-P|T- Comms";
			location = "Friendly Squad Logistics";
			description = "Handles item transfers from your squadmates.";
		}
	}

	public static Trader CreateTrader()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected O, but got Unknown
		//IL_0117: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_013f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_014d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_0163: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_018f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0194: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0211: Unknown result type (might be due to invalid IL or missing references)
		//IL_0226: Expected O, but got Unknown
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_024e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0259: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_025f: Unknown result type (might be due to invalid IL or missing references)
		//IL_026b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0276: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_028e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0299: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_02bf: Expected O, but got Unknown
		//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_02d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e6: Expected O, but got Unknown
		//IL_02e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0300: Unknown result type (might be due to invalid IL or missing references)
		//IL_030b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0316: Unknown result type (might be due to invalid IL or missing references)
		//IL_0326: Expected O, but got Unknown
		//IL_0326: Unknown result type (might be due to invalid IL or missing references)
		//IL_0331: Unknown result type (might be due to invalid IL or missing references)
		//IL_033c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0347: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Expected O, but got Unknown
		TraderBase val2 = new TraderBase
		{
			Id = CourierTraderId,
			AvailableInRaid = true,
			Avatar = "/files/trader/avatar/pitfireteam-courier.png",
			BalanceDollar = default(decimal),
			BalanceEuro = default(decimal),
			BalanceRub = default(decimal),
			BuyerUp = false,
			Currency = (CurrencyType)0,
			CustomizationSeller = false,
			Discount = default(decimal),
			DiscountEnd = default(decimal),
			GridHeight = 120.0,
			Insurance = new TraderInsurance
			{
				Availability = false,
				ExcludedCategory = new List<MongoId>(),
				MaxReturnHour = 0,
				MaxStorageTime = 48.0,
				MinPayment = 0,
				MinReturnHour = 0
			},
			ItemsBuy = CreateEmptyItemBuyData(),
			ItemsBuyProhibited = CreateEmptyItemBuyData(),
			ItemsSell = new Dictionary<string, ItemSellData>(),
			IsAvailableInPVE = true,
			IsCanTransferItems = false,
			IsCanTransferItemsFromPve = false,
			TransferableItems = CreateEmptyItemBuyData(),
			ProhibitedTransferableItems = CreateEmptyItemBuyData(),
			Location = "Friendly Squad Logistics",
			LoyaltyLevels = new List<TraderLoyaltyLevel>
			{
				new TraderLoyaltyLevel
				{
					BuyPriceCoefficient = 0.0,
					ExchangePriceCoefficient = 0.0,
					HealPriceCoefficient = 0.0,
					InsurancePriceCoefficient = 0.0,
					MinLevel = 1,
					MinSalesSum = 0L,
					MinStanding = 0.0,
					RepairPriceCoefficient = 0.0
				}
			},
			Medic = false,
			Name = "-P|T- Comms",
			NextResupply = 0,
			Nickname = "-P|T- Comms",
			Repair = new TraderRepair
			{
				Availability = false,
				Currency = "5449016a4bdc2d6f028b456f",
				CurrencyCoefficient = 1.0,
				ExcludedCategory = new List<MongoId>(),
				ExcludedIdList = new List<string>(),
				Quality = 0.0,
				PriceRate = 1.0
			},
			SellCategory = new List<string>(),
			Surname = string.Empty,
			UnlockedByDefault = false
		};
		Trader val = new Trader
		{
			Base = val2,
			Assort = new TraderAssort
			{
				NextResupply = 0.0,
				Items = new List<Item>(),
				BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
				LoyalLevelItems = new Dictionary<MongoId, int>()
			},
			Dialogue = new Dictionary<string, List<string>>(),
			QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>(),
			Suits = new List<Suit>(),
			Services = new List<TraderServiceModel>()
		};
		return val;
	}

	private static ItemBuyData CreateEmptyItemBuyData()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Expected O, but got Unknown
		return new ItemBuyData
		{
			Category = new HashSet<MongoId>(),
			IdList = new HashSet<MongoId>()
		};
	}

	private static string NormalizeLocale(string? locale)
	{
		if (string.IsNullOrWhiteSpace(locale))
		{
			return "en";
		}
		string text = locale.Trim().ToLowerInvariant();
		int num = text.IndexOfAny(new char[2] { '-', '_' });
		if (num <= 0)
		{
			return text;
		}
		return text.Substring(0, num);
	}
}
