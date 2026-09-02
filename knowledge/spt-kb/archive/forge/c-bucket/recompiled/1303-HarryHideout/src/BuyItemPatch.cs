using System.Reflection;
using SPTarkov.Server.Core.Helpers.Commerce;
using SPTarkov.Server.Core.Models.Eft.Trade;
using SPTarkov.Reflection.Patching;

namespace _harryHideout;

public class BuyItemPatch : AbstractPatch
{
	protected override MethodBase? GetTargetMethod()
	{
		return typeof(TradeHelper).GetMethod("BuyItem");
	}

	[PatchPrefix]
	public static void Prefix(ProcessBuyTradeRequestData buyRequestData, ref bool foundInRaid)
	{
		if (buyRequestData.TransactionId == "67419e9d0d4541ce671543bb")
		{
			foundInRaid = true;
		}
	}

	public BuyItemPatch()
		: base(null)
	{
	}
}
