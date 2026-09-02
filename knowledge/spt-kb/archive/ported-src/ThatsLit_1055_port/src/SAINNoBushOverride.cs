using System;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

using FirearmController = EFT.Player.FirearmController;
using AbstractHandsController = EFT.Player.AbstractHandsController;

namespace ThatsLit.Patches.Vision;

public class SAINNoBushOverride : ModulePatch
{
	protected override MethodBase GetTargetMethod()
	{
		Type type = Type.GetType("SAIN.Components.SAINNoBushESP, SAIN");
		if (type == null)
		{
			throw new InvalidOperationException("SAINNoBushESP type not found. SAIN may have been updated with incompatible changes. Update That's Lit or disable 'Interrupt SAIN No Bush' option.");
		}
		return AccessTools.Method(type, "SetCanShoot", new Type[1] { typeof(bool) }, (Type[])null);
	}

	[PatchPrefix]
	public static void PatchPrefix(ref bool blockShoot, ref BotOwner ___BotOwner)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
		if (!ThatsLitPlugin.InterruptSAINNoBush.Value)
		{
			return;
		}
		BotOwner obj = ___BotOwner;
		object obj2;
		if (obj == null)
		{
			obj2 = null;
		}
		else
		{
			EFT.BotMemory memory = obj.Memory;
			obj2 = ((memory != null) ? memory.GoalEnemy : null);
		}
		EnemyInfo val = (EnemyInfo)obj2;
		if (((val != null) ? val.Person : null) == null)
		{
			return;
		}
		Vector3 val2 = val.Person.Position - val.EnemyLastPositionReal;
		float magnitude = val2.magnitude;
		float num = Time.time - val.PersonalSeenTime;
		if (val.Distance > 100f && num > 5f)
		{
			return;
		}
		ThatsLitPlayer value = null;
		Singleton<ThatsLitGameworld>.Instance?.AllThatsLitPlayers?.TryGetValue(val.Person, out value);
		if ((Object)(object)value == (Object)null)
		{
			return;
		}
		ThatsLitPlugin.swNoBushOverride.MaybeResume();
		int num2 = ___BotOwner.Id % 10;
		Vector3 lookDirection = ___BotOwner.GetPlayer.LookDirection;
		Vector3 val3 = value.Player.MainParts[(BodyPartType)1].Position - ___BotOwner.MainParts[(BodyPartType)0].Position;
		float num3 = Vector3.Angle(lookDirection, val3);
		if (num3 > 85f)
		{
			ThatsLitPlugin.swNoBushOverride.Stop();
			return;
		}
		float num4 = Mathf.InverseLerp(35f - (float)num2 * 2f, 10f - (float)num2, val.Distance);
		num4 *= num4 * num4;
		num4 = Mathf.Abs(num4);
		num4 += Mathf.InverseLerp(15f - (float)num2, 10f - (float)num2, val.Distance) * 0.1f;
		num4 *= Mathf.InverseLerp(75f, 10f, num3);
		num4 = ((!value.Player.IsInPronePose) ? (num4 * (0.2f + value.Player.PoseLevel)) : (num4 / 2f));
		float num5 = 1f - 0.5f * Mathf.InverseLerp(5f, 50f, val.Distance);
		if (num4 < num5)
		{
			num4 = Mathf.Lerp(num4, num5, 0.75f * Mathf.InverseLerp(5f, 0.3f, magnitude) + 0.9f * Mathf.InverseLerp(5f, 1f, num));
		}
		Player getPlayer = ___BotOwner.GetPlayer;
		AbstractHandsController obj3 = ((getPlayer != null) ? getPlayer.HandsController : null);
		FirearmController val4 = (FirearmController)(object)((obj3 is FirearmController) ? obj3 : null);
		if (val4 != null && ((AbstractHandsController)val4).IsAiming && num4 < 0.75f)
		{
			num4 = Mathf.Lerp(num4, 0.75f, Mathf.InverseLerp(5f, 0f, num3) * Mathf.InverseLerp(100f, 10f, val.Distance));
		}
		if (value.DebugInfo != null)
		{
			value.DebugInfo.lastInterruptChance = num4;
			value.DebugInfo.lastInterruptChanceDis = val.Distance;
		}
		if (value.DebugInfo != null)
		{
			value.DebugInfo.attemptToCancelSAINNoBush++;
		}
		if (Random.Range(0f, 1f) < num4)
		{
			blockShoot = false;
			if (value.DebugInfo != null)
			{
				value.DebugInfo.cancelledSAINNoBush++;
			}
		}
		ThatsLitPlugin.swNoBushOverride.Stop();
	}
}
