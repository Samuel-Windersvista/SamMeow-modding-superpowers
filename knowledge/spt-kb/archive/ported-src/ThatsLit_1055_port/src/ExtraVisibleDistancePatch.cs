using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.InventoryLogic;
using EFT.Weather;
using HarmonyLib;
using SPT.Reflection.Patching;
using EMask = EFT.InventoryLogic.NightVisionComponent.EMask;
using UnityEngine;

namespace ThatsLit;

public class ExtraVisibleDistancePatch : ModulePatch
{
	internal static Stopwatch _benchmarkSW;

	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(EnemyInfo), "GetAdditionalSensorDistance", (Type[])null, (Type[])null);
	}

	[PatchPostfix]
	[HarmonyAfter(new string[] { "me.sol.sain" })]
	public static void PatchPostfix(EnemyInfo __instance, ref float __result)
	{
		ThatsLitPlugin.swExtraVisDis.MaybeResume();
		if (__instance.Person == null || __instance.Person.IsAI)
		{
			ThatsLitPlugin.swExtraVisDis.Stop();
			return;
		}
		if (!ThatsLitPlugin.EnabledMod.Value || ThatsLitPlugin.ExtraVisionDistanceScale.Value == 0f || !ThatsLitPlugin.EnabledLighting.Value)
		{
			ThatsLitPlugin.swExtraVisDis.Stop();
			return;
		}
		if (ThatsLitPlugin.PMCOnlyMode.Value)
		{
			BotOwner owner2 = __instance.Owner;
			WildSpawnType? spawnType;
			if (owner2 == null)
			{
				spawnType = null;
			}
			else
			{
				Profile profile = owner2.Profile;
				if (profile == null)
				{
					spawnType = null;
				}
				else
				{
					ProfileInfo info = profile.Info;
					spawnType = ((info == null) ? ((WildSpawnType?)null) : info.Settings?.Role);
				}
			}
			if (!Utility.IsPMCSpawnType(spawnType))
			{
				ThatsLitPlugin.swExtraVisDis.Stop();
				return;
			}
		}
		if (Singleton<ThatsLitGameworld>.Instance?.ScoreCalculator != null)
		{
			BotOwner owner3 = __instance.Owner;
			if (((owner3 != null) ? owner3.LookSensor : null) != null)
			{
				ThatsLitPlayer value = null;
				Singleton<ThatsLitGameworld>.Instance?.AllThatsLitPlayers?.TryGetValue(__instance.Person, out value);
				if ((UnityEngine.Object)(object)value == (UnityEngine.Object)null || value.PlayerLitScoreProfile == null)
				{
					ThatsLitPlugin.swExtraVisDis.Stop();
					return;
				}
				bool flag = false;
				if ((UnityEngine.Object)(object)value.lastNearest == (UnityEngine.Object)(object)__instance.Owner)
				{
					flag = true;
					if (value.DebugInfo != null)
					{
						value.DebugInfo.lastDisComp = 0f;
						value.DebugInfo.lastDisCompThermal = 0f;
						value.DebugInfo.lastDisCompNVG = 0f;
						value.DebugInfo.lastDisCompDay = 0f;
					}
				}
				bool flag2 = false;
				bool flag3 = false;
				float num = 0f;
				BotOwner owner4 = __instance.Owner;
				BotNightVisionData val = ((owner4 != null) ? owner4.NightVision : null);
				if (val != null && val.UsingNow)
				{
					NightVisionComponent nightVisionItem = val.NightVisionItem;
					EMask? obj2;
					if (nightVisionItem == null)
					{
						obj2 = null;
					}
					else
					{
						EFT.InventoryLogic.INightVisionComponentTemplate template = nightVisionItem.Template;
						obj2 = ((template != null) ? new EMask?(template.Mask) : ((EMask?)null));
					}
					EMask? val2 = obj2;
					flag2 = val2 == (EMask?)0;
					flag3 = val2.HasValue && val2 != (EMask?)0;
				}
				else
				{
					BotOwner owner5 = __instance.Owner;
					object obj3;
					if (owner5 == null)
					{
						obj3 = null;
					}
					else
					{
						Player getPlayer = owner5.GetPlayer;
						if (getPlayer == null)
						{
							obj3 = null;
						}
						else
						{
							ProceduralWeaponAnimation proceduralWeaponAnimation = getPlayer.ProceduralWeaponAnimation;
							obj3 = ((proceduralWeaponAnimation != null) ? proceduralWeaponAnimation.CurrentAimingMod : null);
						}
					}
					SightComponent val3 = (SightComponent)obj3;
					if (val3 != null)
					{
						Dictionary<string, ThatsLitCompat.Scope> scopes = ThatsLitCompat.Scopes;
						Item item = ((EFT.InventoryLogic.ItemComponent)val3).Item;
						MongoID? val4 = ((item != null) ? new MongoID?(item.TemplateId) : ((MongoID?)null));
						scopes.TryGetValue(val4.HasValue ? (string)(val4.GetValueOrDefault()) : null, out var value2);
						if (value2?.TemplateInstance?.thermal != null)
						{
							flag2 = true;
							num = value2.TemplateInstance.thermal.effectiveDistance;
						}
						else if (value2?.TemplateInstance?.nightVision != null)
						{
							flag3 = true;
						}
					}
				}
				WeatherController instance = WeatherController.Instance;
				float? obj4;
				if (instance == null)
				{
					obj4 = null;
				}
				else
				{
					IWeatherCurve weatherCurve = instance.WeatherCurve;
					obj4 = ((weatherCurve != null) ? new float?(weatherCurve.Fog) : ((float?)null));
				}
				float? num2 = obj4;
				float valueOrDefault = num2.GetValueOrDefault();
				valueOrDefault = Mathf.InverseLerp(0f, 0.35f, valueOrDefault);
				_ = Singleton<ThatsLitGameworld>.Instance.ScoreCalculator;
				FrameStats frameStats = value.PlayerLitScoreProfile?.frame0 ?? default(FrameStats);
				float visibleDist = __instance.Owner.LookSensor.VisibleDist;
				if (flag2)
				{
					float num3 = num - visibleDist;
					if (num3 > 0f)
					{
						__result += Random.Range(0.5f, 1f) * num3 * ThatsLitPlugin.ExtraVisionDistanceScale.Value;
					}
					if (flag && value.DebugInfo != null)
					{
						value.DebugInfo.lastDisCompThermal = num3;
					}
				}
				else if (flag3 && frameStats.ambienceScore < 0f)
				{
					float num4 = (value.LightAndLaserState.IRLight ? 3.5f : ((!value.LightAndLaserState.IRLaser) ? 2.5f : 3f));
					num4 = Mathf.Lerp(1f, num4, Mathf.Clamp01(frameStats.ambienceScore / -1f));
					float num5 = __instance.Owner.Settings.FileSettings.Look.MIDDLE_DIST - visibleDist;
					if (num5 > 0f)
					{
						num5 *= 1f - valueOrDefault;
						num5 *= num4;
						num5 *= value.PlayerLitScoreProfile.litScoreFactor * ThatsLitPlugin.ExtraVisionDistanceScale.Value;
						__result += num5 * Random.Range(0.25f, 1f);
					}
					if (flag && value.DebugInfo != null)
					{
						value.DebugInfo.lastDisCompNVG = num5;
					}
				}
				else if (!flag3 && frameStats.ambienceScore > 0f)
				{
					float num6 = __instance.Owner.Settings.FileSettings.Look.MIDDLE_DIST - visibleDist;
					if (num6 > 0f)
					{
						num6 *= 1f - valueOrDefault;
						num6 *= (1f + frameStats.ambienceScore / 5f) * ThatsLitPlugin.ExtraVisionDistanceScale.Value;
						__result += num6 * Random.Range(0.2f, 1f);
					}
					if (flag && value.DebugInfo != null)
					{
						value.DebugInfo.lastDisCompDay = num6;
					}
				}
				else if (!flag3)
				{
					float num7 = frameStats.multiFrameLitScore - frameStats.baseAmbienceScore;
					num7 = Mathf.InverseLerp(0f, 2f, num7);
					num7 *= 1f - valueOrDefault;
					float num8 = __instance.Owner.Settings.FileSettings.Look.MIDDLE_DIST - visibleDist;
					if (num8 > 0f)
					{
						num8 *= num7;
						__result += num8;
					}
					if (flag && value.DebugInfo != null)
					{
						value.DebugInfo.lastDisComp = num8;
					}
				}
				ThatsLitPlugin.swExtraVisDis.Stop();
				return;
			}
		}
		ThatsLitPlugin.swExtraVisDis.Stop();
	}
}
