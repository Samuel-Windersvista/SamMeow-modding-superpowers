using System;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using ImmersiveDaylightCycle.Common;
using ImmersiveDaylightCycle.Fika;
using Jehree.ImmersiveDaylightCycle.Helpers;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace Jehree.ImmersiveDaylightCycle.Patches;

internal class OfflineRaidEndedPatch : ModulePatch
{
	private static Type _targetClassType;

	protected override MethodBase GetTargetMethod()
	{
		_targetClassType = PatchConstants.EftTypes.Single((Type targetClass) => !targetClass.IsInterface && !targetClass.IsNested && targetClass.GetMethods().Any((MethodInfo method) => method.Name == "LocalRaidEnded") && targetClass.GetMethods().Any((MethodInfo method) => method.Name == "LocalRaidStarted"));
		return AccessTools.Method((Type)_targetClassType.GetTypeInfo(), "LocalRaidEnded", (Type[])null, (Type[])null);
	}

	[PatchPostfix]
	private static void Postfix(LocalRaidSettings settings, EFT.SessionResult results)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		if (Settings.ModEnabled.Value)
		{
			IDCClientExitInfo data = new IDCClientExitInfo
			{
				RaidId = FikaBridge.GetRaidId(),
				ProfileId = Singleton<GameWorld>.Instance.MainPlayer.ProfileId,
				ExitStatus = results.result,
				IsHost = FikaBridge.IAmHost(),
				IsDedicatedClient = Plugin.IAmDedicatedClient,
				SecondsInRaid = results.playTime
			};
			ModUtils.ServerRoute(ModUtils.ClientLeftRaidURL, data);
		}
	}
}
