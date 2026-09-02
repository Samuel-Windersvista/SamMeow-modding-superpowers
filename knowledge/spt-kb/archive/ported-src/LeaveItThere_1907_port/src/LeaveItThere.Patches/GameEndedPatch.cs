using System;
using System.Linq;
using System.Reflection;
using EFT;
using HarmonyLib;
using LeaveItThere.Addon;
using LeaveItThere.Components;
using LeaveItThere.Fika;
using LeaveItThere.Helpers;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace LeaveItThere.Patches;

internal class GameEndedPatch : ModulePatch
{
	private static Type _targetClassType;

	private static FieldInfo _exitNameInfo;

	protected override MethodBase GetTargetMethod()
	{
		_targetClassType = PatchConstants.EftTypes.Single((Type targetClass) => !targetClass.IsInterface && !targetClass.IsNested && targetClass.GetMethods().Any((MethodInfo method) => method.Name == "LocalRaidEnded") && targetClass.GetMethods().Any((MethodInfo method) => method.Name == "LocalRaidStarted"));
		MethodInfo methodInfo = AccessTools.Method((Type)_targetClassType.GetTypeInfo(), "LocalRaidEnded", (Type[])null, (Type[])null);
		_exitNameInfo = methodInfo.GetParameters()[1].ParameterType.GetField("exitName");
		return methodInfo;
	}

	[PatchPrefix]
	private static void Prefix(LocalRaidSettings settings, object results, ref object lostInsuredItems, object transferItems)
	{
		LITStaticEvents.InvokeOnRaidEnd(settings, results, lostInsuredItems, transferItems, _exitNameInfo.GetValue(results) as string);
		LITSession instance = LITSession.Instance;
		lostInsuredItems = ItemHelper.RemoveLostInsuredItemsByIds(lostInsuredItems as object[], instance.GetPlacedItemInstanceIds());
		if (FikaBridge.IAmHost())
		{
			instance.SendPlacedItemDataToServer();
		}
		instance.DestroyAllFakeItems();
	}
}
