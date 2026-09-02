using EFT.Console.Core;
using EFT.UI;
using Jehree.ImmersiveDaylightCycle.Helpers;

namespace ImmersiveDaylightCycle.Common;

internal class CommandGroup
{
	[ConsoleCommand("idc_status", "", null, "Print ImmersiveDaylightCycle raid session status to console", new string[] { })]
	public static void LogRaidSessionStatus()
	{
		IDCCommand iDCCommand = ModUtils.ServerRoute<IDCCommand>(ModUtils.ConsoleCommandURL, new IDCCommand("idc_status"));
		ConsoleScreen.Log(iDCCommand.Message);
	}

	[ConsoleCommand("idc_clear", "", null, "Clear ImmersiveDaylightCycle raid session status if a crash caused it to not auto clear", new string[] { })]
	public static void ClearRaidSession()
	{
		IDCCommand iDCCommand = ModUtils.ServerRoute<IDCCommand>(ModUtils.ConsoleCommandURL, new IDCCommand("idc_clear"));
		ConsoleScreen.Log(iDCCommand.Message);
	}
}
