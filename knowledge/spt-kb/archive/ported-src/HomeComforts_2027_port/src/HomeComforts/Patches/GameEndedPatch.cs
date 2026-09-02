using System;
using System.Linq;
using System.Reflection;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;

namespace HomeComforts.Patches;

public class GameEndedPatch : ModulePatch
{
	private static Type _targetClassType;

	private static FieldInfo _exitNameInfo;

	protected override MethodBase GetTargetMethod()
	{
		_targetClassType = PatchConstants.EftTypes.Single((Type targetClass) => !targetClass.IsInterface && !targetClass.IsNested && targetClass.GetMethods().Any((MethodInfo method) => method.Name == "LocalRaidEnded") && targetClass.GetMethods().Any((MethodInfo method) => method.Name == "LocalRaidStarted"));
		MethodInfo methodInfo = AccessTools.Method(_targetClassType.GetTypeInfo(), "LocalRaidEnded", (Type[])null, (Type[])null);
		_exitNameInfo = methodInfo.GetParameters()[1].ParameterType.GetField("exitName");
		return methodInfo;
	}

	[PatchPostfix]
	private static void Postfix(LocalRaidSettings settings, object results, ref object lostInsuredItems, object transferItems)
	{
		// 4.1: raid-end flow is handled by LeaveItThere's own GameEndedPatch +
		// LITStaticEvents.OnRaidEnd (see HCSession.OnRaidEnd). This patch is vestigial.
	}
}
