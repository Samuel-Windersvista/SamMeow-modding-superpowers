using System.Collections.Generic;
using EFT;
using EFT.HealthSystem;
using UnityEngine;

namespace TarkovIRL;

internal static class EfficiencyController
{
	private static readonly float _LerpSpeed = 1f;

	private static readonly int _HeavyBleedHash = -1302691610;

	private static readonly int _LightBleedHash = -1302691609;

	private static readonly int _FreshWoundHash = -2109260636;

	private static readonly int _BoneBreakHash = -1302691612;

	private static readonly int _TremorsHash = -139892197;

	private static readonly int _PainHash = -139892193;

	private static readonly int _FatigueHash = -543176719;

	private static readonly int _ToxicationHash = -1705976133;

	private static readonly int[] _NominalEffectStates = new int[3] { -1705976135, 1022907221, -139892192 };

	private static readonly int[] _KnownEffectStates = new int[7] { -1302691609, -1302691612, -139892197, -139892193, -1302691610, -543176719, -1705976133 };

	private static float _efficiencyLerpTarget = 0f;

	private static float _efficiencyLerpTargetWithoutWeight = 0f;

	private static float _efficiencyLerp = 0f;

	private static float _efficiencyLastFrame = 0f;

	private static float debugTimer = 0f;

	private static Dictionary<Injury, float> _injuryTimes = new Dictionary<Injury, float>();

	private static int _heavyBleedCountLastFrame = 0;

	private static int _lightBleedCountLastFrame = 0;

	private static int _boneBreakCountLastFrame = 0;

	public static float EfficiencyModifier => _efficiencyLerp;

	public static float EfficiencyModifierInverse => 1f / _efficiencyLerp;

	public static void UpdateEfficiencyLerp(float dt)
	{
		float num = ((_efficiencyLerpTarget < _efficiencyLastFrame) ? (1f / _efficiencyLerpTarget) : _efficiencyLerpTarget);
		_efficiencyLerp = Mathf.Lerp(_efficiencyLerp, _efficiencyLerpTarget, dt * _LerpSpeed * PrimeMover.EfficiencyLerpMulti.Value * num);
		_efficiencyLastFrame = _efficiencyLerpTarget;
		debugTimer += dt;
		if (debugTimer > 0.2f)
		{
			debugTimer = 0f;
		}
	}

	private static void CheckInjuryChange(int boneBreakCount, int heavyBleedCount, int lightBleedCount)
	{
		if (boneBreakCount > _boneBreakCountLastFrame)
		{
			int num = boneBreakCount - _boneBreakCountLastFrame;
			for (int i = 0; i < num; i++)
			{
				AddInjuryToEffects(new Injury(Injury.EInjury.BONE_BREAK, Time.time));
			}
		}
		if (boneBreakCount < _boneBreakCountLastFrame)
		{
			RemoveInjuryEffect(Injury.EInjury.BONE_BREAK);
		}
		if (heavyBleedCount > _heavyBleedCountLastFrame)
		{
			int num2 = heavyBleedCount - _heavyBleedCountLastFrame;
			for (int j = 0; j < num2; j++)
			{
				AddInjuryToEffects(new Injury(Injury.EInjury.HEAVY_BLEED, Time.time));
			}
		}
		if (heavyBleedCount < _heavyBleedCountLastFrame)
		{
			RemoveInjuryEffect(Injury.EInjury.HEAVY_BLEED);
		}
		if (lightBleedCount > _lightBleedCountLastFrame)
		{
			int num3 = lightBleedCount - _lightBleedCountLastFrame;
			for (int k = 0; k < num3; k++)
			{
				AddInjuryToEffects(new Injury(Injury.EInjury.LIGHT_BLEED, Time.time));
			}
		}
		if (lightBleedCount < _lightBleedCountLastFrame)
		{
			RemoveInjuryEffect(Injury.EInjury.LIGHT_BLEED);
		}
		_boneBreakCountLastFrame = boneBreakCount;
		_heavyBleedCountLastFrame = heavyBleedCount;
		_lightBleedCountLastFrame = lightBleedCount;
	}

	private static float ReturnInjuryEffectCoef()
	{
		float num = 0f;
		foreach (KeyValuePair<Injury, float> injuryTime in _injuryTimes)
		{
			float num2 = Time.time - injuryTime.Key.TimeInflicted;
			float num3 = Mathf.Clamp01(num2 / injuryTime.Key.TimeUntilEffect);
			num += injuryTime.Key.InjuryWeight * num3;
		}
		float injuryImpact = GetInjuryImpact(num);
		TIRLUtils.Log($"total injury effect is ({injuryImpact})", spam: false);
		return injuryImpact;
	}

	private static void AddInjuryToEffects(Injury injury)
	{
		_injuryTimes.Add(injury, Time.time);
	}

	private static void RemoveInjuryEffect(Injury.EInjury injuryType)
	{
		Injury injury = null;
		foreach (KeyValuePair<Injury, float> injuryTime in _injuryTimes)
		{
			if (injury == null && injuryTime.Key.InjuryType == injuryType)
			{
				injury = injuryTime.Key;
			}
			else if (injury != null && injuryTime.Key.InjuryType == injuryType && injuryTime.Key.TimeInflicted < injury.TimeInflicted)
			{
				injury = injuryTime.Key;
			}
		}
		if (injury != null)
		{
			_injuryTimes.Remove(injury);
		}
	}

	public static void UpdateEfficiency(Player player)
	{
		//IL_03fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0403: Invalid comparison between Unknown and I4
		//IL_0413: Unknown result type (might be due to invalid IL or missing references)
		//IL_0419: Invalid comparison between Unknown and I4
		//IL_0429: Unknown result type (might be due to invalid IL or missing references)
		//IL_042f: Invalid comparison between Unknown and I4
		//IL_043f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0445: Invalid comparison between Unknown and I4
		float normalized = ((IHealthController)player.HealthController).Hydration.Normalized;
		float normalized2 = ((IHealthController)player.HealthController).Energy.Normalized;
		float value = 1f - Mathf.Clamp01(player.Physical.Overweight);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		int num7 = 0;
		int num8 = 0;
		foreach (IHealthEffect allEffect in player.HealthController.GetAllEffects(EBodyPart.Common))
		{
			if (!IsEffectKnown(allEffect, _NominalEffectStates) && !IsEffectKnown(allEffect, _KnownEffectStates))
			{
				TIRLUtils.Log($"effect type {allEffect}({allEffect.Type.FullName.GetHashCode()}) on bodypart {allEffect.BodyPart}", spam: false);
			}
			if (allEffect.Type.FullName.GetHashCode() == _HeavyBleedHash)
			{
				num2++;
			}
			if (allEffect.Type.FullName.GetHashCode() == _LightBleedHash)
			{
				num3++;
			}
			if (allEffect.Type.FullName.GetHashCode() == _FreshWoundHash)
			{
				num++;
			}
			if (allEffect.Type.FullName.GetHashCode() == _BoneBreakHash)
			{
				num4++;
			}
			if (allEffect.Type.FullName.GetHashCode() == _TremorsHash)
			{
				num5++;
			}
			if (allEffect.Type.FullName.GetHashCode() == _PainHash)
			{
				num6++;
			}
			if (allEffect.Type.FullName.GetHashCode() == _FatigueHash)
			{
				num7++;
			}
			if (allEffect.Type.FullName.GetHashCode() == _ToxicationHash)
			{
				num8++;
			}
		}
		TIRLUtils.Log($"heavyBleedCount {num2}, lightBleedCount {num3}, freshWoundCount {num}, boneBreakCount {num4} , tremorCount {num5}, painCount {num6}, fatigueCount {num7}, intoxCount {num8}", spam: false);
		CheckInjuryChange(num4, num2, num3);
		float normalized3 = ((IHealthController)player.HealthController).GetBodyPartHealth(EBodyPart.Common, false).Normalized;
		float normalValue = player.Physical.Stamina.NormalValue;
		float normalValue2 = player.Physical.HandsStamina.NormalValue;
		float injuryImpact = GetInjuryImpact(normalized, 10f);
		float injuryImpact2 = GetInjuryImpact(normalized2, 10f);
		float injuryImpact3 = GetInjuryImpact(value, PrimeMover.EfficiencyOverweightIpmact.Value);
		float injuryImpact4 = GetInjuryImpact(normalized3, 40f);
		float injuryImpact5 = GetInjuryImpact(normalValue, 20f);
		float injuryImpact6 = GetInjuryImpact(normalValue2, 20f);
		float injuryImpact7 = GetInjuryImpact(5f, num);
		float injuryImpact8 = GetInjuryImpact(5f, num5);
		float injuryImpact9 = GetInjuryImpact(5f, num6);
		float injuryImpact10 = GetInjuryImpact(5f, num7);
		float injuryImpact11 = GetInjuryImpact(10f, num8);
		float num9 = ReturnInjuryEffectCoef() * injuryImpact7 * injuryImpact8 * injuryImpact9;
		num9 = (RealismWrapper.IsAdrenaline ? 1f : num9);
		injuryImpact = (RealismWrapper.IsAdrenaline ? 1f : injuryImpact);
		injuryImpact2 = (RealismWrapper.IsAdrenaline ? 1f : injuryImpact2);
		float num10 = (RealismWrapper.IsOverdose ? 1.2f : 1f);
		float num11 = injuryImpact * injuryImpact2 * injuryImpact4 * injuryImpact5 * injuryImpact10 * injuryImpact6 * num9 * injuryImpact11 * num10;
		float num12 = (RealismWrapper.IsAdrenaline ? 0.5f : 1f);
		float num13 = (AnimStateController.IsSideStep ? 1.3f : 1f);
		float num14 = (((int)StanceController.CurrentStance == 2) ? 0.85f : 1f);
		float num15 = (((int)StanceController.CurrentStance == 1) ? 1.1f : 1f);
		float num16 = (((int)StanceController.CurrentStance == 3) ? 0.7f : 1f);
		float num17 = (((int)StanceController.CurrentStance == 4) ? 1.1f : 1f);
		float num18 = (StanceController.IsMounting ? 0.5f : 1f);
		float num19 = (StanceController.IsLeftShoulder ? 1.1f : 1f);
		float num20 = (player.IsSprintEnabled ? 1.5f : 1f);
		float num21 = 0.5f + player.Speed * 0.5f;
		if (!PlayerMotionController.IsPlayerMovement)
		{
			num21 = 0.5f;
		}
		float num22 = 0.5f + player.PoseLevel * 0.5f;
		float num23 = (player.IsInPronePose ? 0.5f : 1f);
		float num24 = num21 * num22 * num23 * num20 * num14 * num15 * num18 * num16 * num19 * num17 * num13 * num12;
		_efficiencyLerpTarget = num11 * num24 * injuryImpact3;
		_efficiencyLerpTargetWithoutWeight = num11 * num24;
		TIRLUtils.Log($"overWeightMulti {injuryImpact3}, healthMulti {injuryImpact4}, stamMulti {injuryImpact5}, injuryMulti {num9} , negativeEffects total {num11}", spam: false);
	}

	private static float GetInjuryImpact(float value, float impactWeightPercent)
	{
		float num = impactWeightPercent * 0.01f * PrimeMover.EfficiencyInjuryImpact.Value;
		return 1f + (1f - value) * num;
	}

	private static float GetInjuryImpact(float impactWeightPercent, int multipleInstances)
	{
		if (multipleInstances == 0)
		{
			return 1f;
		}
		float num = impactWeightPercent * 0.01f * PrimeMover.EfficiencyInjuryImpact.Value;
		return (1f + num) * (float)multipleInstances;
	}

	private static float GetInjuryImpact(float impactWeightPercent)
	{
		float num = impactWeightPercent * 0.01f * PrimeMover.EfficiencyInjuryImpact.Value;
		return 1f + num;
	}

	private static bool IsEffectKnown(IHealthEffect effect, int[] effectsArray)
	{
		foreach (int num in effectsArray)
		{
			if (effect.Type.FullName.GetHashCode() == num)
			{
				return true;
			}
		}
		return false;
	}
}
