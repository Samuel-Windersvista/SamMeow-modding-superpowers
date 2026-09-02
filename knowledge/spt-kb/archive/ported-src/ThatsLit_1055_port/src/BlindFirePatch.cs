using System;
using System.Linq;
using System.Reflection;
using EFT;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;

using FirearmController = EFT.Player.FirearmController;
using AbstractHandsController = EFT.Player.AbstractHandsController;

namespace ThatsLit.Patches.Vision;

public class BlindFirePatch : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		return PatchConstants.EftTypes.First((Type t) => t.GetProperty("LastSpreadCount") != null && t.GetProperty("LastAimTime") != null && t.GetProperty("HardAim") != null).GetMethod("get_EndTargetPoint");
	}

	[PatchPostfix]
	public static void Postfix(ref BotOwner ___owner, ref BifacialTransform ___myWeapon, ref Vector3 __result)
	{
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_009b: Unknown result type (might be due to invalid IL or missing references)
		if (!ThatsLitPlugin.ForceBlindFireScatter.Value)
		{
			return;
		}
		ThatsLitPlugin.swBlindFireScatter.MaybeResume();
		if (!((Object)(object)___owner.GetPlayer == (Object)null))
		{
			AbstractHandsController handsController = ___owner.GetPlayer.HandsController;
			AbstractHandsController obj = ((handsController is FirearmController) ? handsController : null);
			if (obj != null && ((FirearmController)obj).Blindfire)
			{
				float num = Vector3.Distance(__result, ___owner.GetPlayer.Position);
				__result += Random.insideUnitSphere * 5f * Mathf.InverseLerp(15f, 200f, num);
				ThatsLitPlugin.swBlindFireScatter.Stop();
				return;
			}
		}
		ThatsLitPlugin.swBlindFireScatter.Stop();
	}
}
