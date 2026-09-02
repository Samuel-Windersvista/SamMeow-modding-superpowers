using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Animations;
using EFT.HealthSystem;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

using FirearmController = EFT.Player.FirearmController;
using EMask = EFT.InventoryLogic.NightVisionComponent.EMask;
using AbstractHandsController = EFT.Player.AbstractHandsController;

namespace ThatsLit;

public class SeenCoefPatch : ModulePatch
{
	private static PropertyInfo _enemyRel;

	private static readonly float[][] focusLUTs = new float[10][]
	{
		new float[61]
		{
			-27f, -9f, 6f, -42f, 24f, -87f, -12f, 3f, 33f, 30f,
			51f, -66f, 12f, -90f, 72f, 87f, -54f, 42f, -15f, -33f,
			18f, -30f, 90f, -81f, -3f, -57f, 36f, -45f, 48f, 69f,
			-51f, 27f, 0f, -6f, -36f, 81f, -84f, 54f, -78f, 84f,
			-63f, 39f, 60f, -21f, 9f, 57f, 21f, -69f, -75f, -24f,
			-18f, -39f, 78f, 45f, 15f, 75f, -60f, -48f, 63f, 66f,
			-72f
		},
		new float[61]
		{
			9f, -87f, 42f, -63f, -54f, -84f, 0f, -39f, -36f, 54f,
			-69f, -66f, -27f, -33f, -51f, -60f, -90f, -9f, -15f, 75f,
			-75f, -12f, 66f, -78f, 60f, -6f, -45f, 69f, -3f, 24f,
			-81f, 63f, 15f, 84f, 27f, 21f, 72f, 81f, 45f, 30f,
			-57f, -18f, -72f, 87f, 90f, -21f, -30f, 57f, -42f, 18f,
			78f, 39f, -48f, 3f, 48f, 33f, -24f, 6f, 36f, 12f,
			51f
		},
		new float[61]
		{
			54f, 3f, 6f, -21f, 30f, -75f, -48f, -12f, -81f, -54f,
			-15f, -9f, -39f, -45f, 18f, 69f, -78f, 9f, -33f, -51f,
			-69f, -24f, 72f, 33f, -84f, 66f, 21f, 0f, -3f, 36f,
			90f, 39f, -63f, 81f, -72f, 84f, -36f, -42f, 63f, 15f,
			24f, -57f, -30f, 12f, 48f, 60f, 57f, -87f, 27f, -90f,
			51f, -6f, 45f, -66f, 78f, 75f, -60f, -18f, 87f, 42f,
			-27f
		},
		new float[61]
		{
			39f, 51f, -84f, 12f, 9f, -42f, 42f, 30f, -81f, 27f,
			-51f, 69f, -78f, 78f, -21f, 24f, 15f, -9f, -27f, 63f,
			-60f, 72f, -48f, -57f, 48f, -75f, 36f, 66f, -33f, -66f,
			57f, 6f, 21f, -36f, 81f, 90f, -30f, -39f, -45f, 3f,
			84f, -24f, -6f, 60f, 45f, 87f, -72f, -90f, -54f, 18f,
			-15f, -87f, 0f, 33f, 75f, -63f, 54f, -18f, -3f, -12f,
			-69f
		},
		new float[61]
		{
			-51f, -63f, 48f, 18f, 9f, -54f, 33f, -60f, -3f, 36f,
			30f, 51f, -33f, 21f, 12f, -45f, 0f, 57f, -24f, -21f,
			-87f, -69f, -36f, 69f, -81f, 3f, -30f, 27f, -72f, 60f,
			81f, 78f, 75f, -6f, 45f, -42f, 42f, 54f, 63f, 84f,
			90f, -27f, -66f, -48f, -78f, -15f, -75f, 15f, -57f, 87f,
			-90f, -12f, 6f, 72f, -84f, -18f, -9f, -39f, 66f, 24f,
			39f
		},
		new float[61]
		{
			21f, 84f, 9f, 60f, 78f, 45f, -57f, -81f, 57f, -3f,
			3f, 0f, -42f, -75f, -24f, -39f, -36f, -9f, 30f, 75f,
			63f, 66f, 48f, 18f, -72f, 87f, -54f, 51f, -51f, 15f,
			39f, 36f, -66f, -78f, -12f, 69f, 42f, -15f, -48f, 27f,
			-30f, 6f, 90f, -90f, 24f, 33f, 12f, 81f, -63f, -21f,
			-87f, -45f, -84f, -27f, -18f, -60f, 72f, -6f, -69f, 54f,
			-33f
		},
		new float[61]
		{
			-42f, -45f, -24f, -90f, -87f, 33f, -66f, 15f, 42f, -12f,
			-18f, -15f, 90f, 48f, 45f, -63f, -78f, -57f, 63f, -36f,
			84f, 39f, 36f, 75f, -30f, -54f, 3f, 78f, 60f, -39f,
			24f, 30f, -21f, 27f, 57f, -81f, -69f, 66f, 54f, -51f,
			-75f, 0f, 21f, -72f, -27f, -60f, -6f, -3f, 12f, -9f,
			-33f, 87f, 69f, -48f, 6f, -84f, 72f, 51f, 81f, 9f,
			18f
		},
		new float[61]
		{
			-90f, -66f, 42f, 36f, 24f, 9f, 6f, -18f, -6f, 75f,
			-15f, 87f, -48f, 54f, -69f, 84f, -21f, 78f, -81f, -30f,
			-9f, 48f, -24f, -27f, -60f, -42f, -72f, -36f, -78f, 45f,
			90f, -54f, -84f, -51f, 57f, -57f, 12f, -33f, -87f, 30f,
			33f, -3f, 60f, -39f, 3f, -12f, 0f, -45f, -75f, 81f,
			66f, 69f, 72f, 21f, 51f, 39f, 15f, 27f, 18f, 63f,
			-63f
		},
		new float[61]
		{
			33f, 87f, -51f, -18f, -84f, -42f, -39f, 3f, 30f, 39f,
			42f, 21f, 66f, 36f, -48f, -12f, 27f, 81f, 57f, -90f,
			-30f, 60f, 63f, -36f, -60f, -72f, -69f, -54f, -45f, -75f,
			24f, -24f, -9f, -33f, -57f, 0f, -15f, -6f, 69f, -78f,
			-21f, 6f, 54f, 75f, -81f, 45f, 9f, -87f, -66f, 84f,
			90f, -3f, -63f, 72f, 51f, 12f, 15f, 48f, 78f, 18f,
			-27f
		},
		new float[61]
		{
			-63f, 60f, -39f, -78f, -60f, -15f, -48f, 84f, -3f, -30f,
			-33f, 54f, 30f, 9f, -12f, -66f, -18f, 33f, -69f, 87f,
			36f, -6f, 66f, 48f, 78f, 72f, 45f, -27f, -87f, -45f,
			81f, 90f, 18f, 57f, 75f, -36f, 15f, -84f, 12f, -42f,
			39f, -9f, -75f, -81f, 51f, -21f, 21f, 69f, 3f, 27f,
			-54f, -90f, 24f, -51f, 42f, -24f, -57f, 63f, 0f, -72f,
			6f
		}
	};

	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(EnemyInfo), "GetVisibilityChangeSpeedK", (Type[])null, (Type[])null);
	}

	[PatchPostfix]
	[HarmonyAfter(new string[] { "me.sol.sain" })]
	public static void PatchPostfix(EnemyInfo __instance, BotSettings settings, float personalLastSeenTime, Vector3 personalLastSeenPos, float distanceToEnemyNormalized, ref float __result)
	{
		//IL_0127: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_012d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_0210: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Unknown result type (might be due to invalid IL or missing references)
		//IL_025a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026a: Unknown result type (might be due to invalid IL or missing references)
		//IL_026f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_02e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_030f: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b5: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		//IL_047e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0480: Unknown result type (might be due to invalid IL or missing references)
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		//IL_049e: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_04a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_04b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_04bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04d6: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0633: Unknown result type (might be due to invalid IL or missing references)
		//IL_0635: Unknown result type (might be due to invalid IL or missing references)
		//IL_0640: Unknown result type (might be due to invalid IL or missing references)
		//IL_0659: Unknown result type (might be due to invalid IL or missing references)
		//IL_065e: Unknown result type (might be due to invalid IL or missing references)
		//IL_085c: Unknown result type (might be due to invalid IL or missing references)
		//IL_086c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0812: Unknown result type (might be due to invalid IL or missing references)
		//IL_082d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0826: Unknown result type (might be due to invalid IL or missing references)
		//IL_0945: Unknown result type (might be due to invalid IL or missing references)
		//IL_0cec: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dcc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0dce: Unknown result type (might be due to invalid IL or missing references)
		//IL_0e34: Unknown result type (might be due to invalid IL or missing references)
		//IL_138d: Unknown result type (might be due to invalid IL or missing references)
		//IL_13c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_13ce: Unknown result type (might be due to invalid IL or missing references)
		EnemyInfo __instance2 = __instance;
		if (__result >= 8888f || !ThatsLitPlugin.EnabledMod.Value || (ThatsLitPlugin.FinalImpactScaleDelaying.Value == 0f && ThatsLitPlugin.FinalImpactScaleFastening.Value == 0f))
		{
			return;
		}
		if (ThatsLitPlugin.PMCOnlyMode.Value)
		{
			BotOwner owner = __instance2.Owner;
			WildSpawnType? spawnType;
			if (owner == null)
			{
				spawnType = null;
			}
			else
			{
				Profile profile = owner.Profile;
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
				return;
			}
		}
		BotOwner owner2 = __instance2.Owner;
		WildSpawnType? obj;
		if (owner2 == null)
		{
			obj = null;
		}
		else
		{
			Profile profile2 = owner2.Profile;
			if (profile2 == null)
			{
				obj = null;
			}
			else
			{
				ProfileInfo info2 = profile2.Info;
				obj = ((info2 == null) ? ((WildSpawnType?)null) : info2.Settings?.Role);
			}
		}
		WildSpawnType type = (WildSpawnType)(obj ?? (WildSpawnType)1);
		BotImpactType botImpactType = Utility.GetBotImpactType(type);
		if ((botImpactType == BotImpactType.BOSS && !ThatsLitPlugin.IncludeBosses.Value) || Utility.IsExcludedSpawnType(type))
		{
			return;
		}
		ThatsLitPlayer player = null;
		ThatsLitGameworld instance = Singleton<ThatsLitGameworld>.Instance;
		if (instance == null || instance.AllThatsLitPlayers?.TryGetValue(__instance2.Person, out player) != true || (Object)(object)player == (Object)null || (Object)(object)player.Player == (Object)null)
		{
			return;
		}
		float num = __result;
		if (player.DebugInfo != null && ThatsLitPlayer.IsDebugSampleFrame)
		{
			player.DebugInfo.calcedLastFrame = 0;
			player.DebugInfo.IsBushRatting = false;
		}
		ThatsLitPlugin.swSeenCoef.MaybeResume();
		Vector3 val = player.Player.Velocity;
		float num2 = Mathf.Clamp01((val.magnitude - 1f) / 4f);
		int num3 = __instance2.Owner.Id % 10;
		float num4 = Time.time - personalLastSeenTime;
		val = __instance2.Person.Position - __instance2.EnemyLastPositionReal;
		float magnitude = val.magnitude;
		float num5 = magnitude * magnitude;
		val = __instance2.Owner.GetPlayer.Velocity;
		float magnitude2 = val.magnitude;
		float deNullification = 0f;
		Dictionary<BodyPartType, EnemyPart> mainParts = player.Player.MainParts;
		Vector3 val2 = mainParts[(BodyPartType)1].Position - __instance2.Owner.MainParts[(BodyPartType)0].Position;
		Vector3 val3 = __instance2.EnemyLastPositionReal - __instance2.Owner.MainParts[(BodyPartType)0].Position;
		bool isInPronePose = __instance2.Person.AIData.Player.IsInPronePose;
		float poseFactor = Utility.GetPoseFactor(__instance2.Person.AIData.Player.PoseLevel, __instance2.Person.AIData.Player.Physical.MaxPoseLevel, isInPronePose);
		float value = Random.value;
		float value2 = Random.value;
		float value3 = Random.value;
		float value4 = Random.value;
		float value5 = Random.value;
		_ = (value + value3) % 1f;
		_ = (value2 + value4) % 1f;
		_ = mainParts[(BodyPartType)1].Position;
		Vector3 position = __instance2.Owner.MainParts[(BodyPartType)0].Position;
		LightAndLaserState lightAndLaserState = player.LightAndLaserState;
		_ = lightAndLaserState.VisibleLight;
		_ = lightAndLaserState.VisibleLightSub;
		_ = lightAndLaserState.VisibleLaser;
		_ = lightAndLaserState.VisibleLaserSub;
		bool iRLight = lightAndLaserState.IRLight;
		bool iRLaser = lightAndLaserState.IRLaser;
		bool iRLightSub = lightAndLaserState.IRLightSub;
		bool iRLaserSub = lightAndLaserState.IRLaserSub;
		BotOwner owner3 = __instance2.Owner;
		if (owner3 != null)
		{
			Player getPlayer = owner3.GetPlayer;
			if (getPlayer != null)
			{
				_ = getPlayer.HandsController;
			}
		}
		Player player2 = player.Player;
		AbstractHandsController obj2 = ((player2 != null) ? player2.HandsController : null);
		FirearmController playerFC = (FirearmController)(object)((obj2 is FirearmController) ? obj2 : null);
		Vector3 lookDirection = __instance2.Owner.GetPlayer.LookDirection;
		float num6 = Vector3.Angle(lookDirection, val2);
		float visionAngleDelta90Clamped = Mathf.InverseLerp(0f, 90f, num6);
		float visionAngleDeltaHorizontal = Mathf.Abs(Vector3.SignedAngle(lookDirection, val2, Vector3.up));
		float num7 = Vector3.Angle(new Vector3(val2.x, 0f, val2.z), val2);
		float visionAngleDeltaVerticalSigned = num7 * ((val2.y >= 0f) ? 1f : (-1f));
		float num8 = Vector3.Angle(val3, val2);
		if (!__instance2.HaveSeen)
		{
			num8 = 180f;
		}
		float num9 = Mathf.Clamp01(num4 / __instance2.Owner.Settings.FileSettings.Look.SEC_REPEATED_SEEN);
		float num10 = Mathf.Clamp01(num4 / __instance2.Owner.Settings.FileSettings.Look.SEC_REPEATED_SEEN * 2f);
		float num11 = Mathf.Clamp01((float)((double)magnitude / __instance2.Owner.Settings.FileSettings.Look.DIST_REPEATED_SEEN / 4.0));
		num11 = Mathf.Lerp(num11, 0f, Mathf.InverseLerp(45f, 0f, num8));
		num9 *= num9;
		num10 *= num10;
		num11 *= num11;
		float num12 = Mathf.Clamp01(num11 + num10) + num9 / 3f;
		float magnitude3 = val2.magnitude;
		float zoomedDis = magnitude3;
		float disFactor = Mathf.InverseLerp(10f, 110f, magnitude3);
		float disFactorSmooth = 0f;
		bool inThermalView = false;
		bool inNVGView = false;
		bool gearBlocking = false;
		float num13 = Mathf.Max(0f, Time.time - player.lastOutside);
		float num14 = Vector3.Angle(-val2, player.lastShotVector);
		float facingShotFactor = 0f;
		if (player.lastShotVector != Vector3.zero && Time.time - player.lastShotTime < 0.5f)
		{
			facingShotFactor = Mathf.InverseLerp(15f, 0f, num14) * Mathf.InverseLerp(0.5f, 0f, Time.time - player.lastShotTime);
		}
		ThatsLitCompat.ScopeTemplate activeScope = null;
		ThatsLitCompat.GoggleTemplate activeGoggle = null;
		ProcessGearAndVision(__instance2, player, num6, visionAngleDeltaHorizontal, num7, magnitude3, ref inThermalView, ref inNVGView, ref gearBlocking, ref disFactor, ref zoomedDis, ref activeScope, ref activeGoggle, value, num3);
		if (disFactor > 0f)
		{
			disFactorSmooth = (disFactor + disFactor * disFactor) * 0.5f;
			disFactor *= disFactor;
			float num15 = num4 / (8f * (1.2f - disFactor)) / (0.33f + 0.67f * num11 * num9);
			disFactor = Mathf.Lerp(0f, disFactor, num15);
			disFactorSmooth = Mathf.Lerp(0f, disFactorSmooth, num15);
		}
		bool flag = player.LightAndLaserState.VisibleLight;
		if (!flag && inNVGView && iRLight)
		{
			flag = true;
		}
		_ = !player.LightAndLaserState.VisibleLightSub && inNVGView && iRLightSub;
		bool flag2 = player.LightAndLaserState.VisibleLaser;
		if (!flag2 && inNVGView && iRLaser)
		{
			flag2 = true;
		}
		_ = !player.LightAndLaserState.VisibleLaserSub && inNVGView && iRLaserSub;
		if (num6 > 110f)
		{
			flag = false;
		}
		if (num6 > 85f)
		{
			flag2 = false;
		}
		bool nearestAI = false;
		if (!((Object)(object)player.lastNearest == (Object)null))
		{
			Vector3 position2 = player.Player.Position;
			BotOwner lastNearest = player.lastNearest;
			if (!(Vector3.Distance(position2, (lastNearest != null) ? lastNearest.Position : Vector3.zero) > magnitude3))
			{
				goto IL_08ad;
			}
		}
		player.lastNearest = __instance2.Owner;
		float lastNearest2 = Vector3.Distance(player.Player.Position, player.lastNearest.Position);
		if (player.DebugInfo != null)
		{
			player.DebugInfo.lastNearest = lastNearest2;
			player.lastNearest = __instance2.Owner;
		}
		goto IL_08ad;
		IL_08ad:
		nearestAI = (Object)(object)player.lastNearest == (Object)(object)__instance2.Owner;
		if (ShouldLogSeenCoef())
		{
			player.DebugInfo.lastCalcFrom = num;
		}
		ProcessOverheadAndIndoorOverlook(ref __result, flag, visionAngleDeltaVerticalSigned, poseFactor, num12, num2, disFactor, num3, value, value2, value5, __instance2, player, num13, magnitude3, num6, num11, num9, value3);
		float globalOverlookChance = 0f;
		ProcessGlobalOverlook(ref __result, ref globalOverlookChance, flag, poseFactor, zoomedDis, num12, magnitude3, num6, num3, value5, value, player, nearestAI, val2, botImpactType);
		float num16;
		float num17;
		if (player.PlayerLitScoreProfile == null)
		{
			num16 = (num17 = 0f);
		}
		else if (inThermalView)
		{
			num16 = (num17 = 0.7f);
			if (player.CheckEffectDelegate((EStimulatorBuffType)20))
			{
				num16 = (num17 = 0.7f);
			}
		}
		else
		{
			num16 = player.PlayerLitScoreProfile.frame0.multiFrameLitScore;
			if (num16 < 0f && inNVGView)
			{
				float num18 = 1f + (value2 - 0.5f) * 0.2f;
				if (activeGoggle?.nightVision != null)
				{
					if (num16 < -0.85f)
					{
						num16 *= 1f - Mathf.Clamp01(activeGoggle.nightVision.nullificationExtremeDark * num18);
					}
					else if (num16 < -0.65f)
					{
						num16 *= 1f - Mathf.Clamp01(activeGoggle.nightVision.nullificationDarker * num18);
					}
					else if (num16 < 0f)
					{
						num16 *= 1f - Mathf.Clamp01(activeGoggle.nightVision.nullification);
					}
				}
				else if (activeScope?.nightVision != null)
				{
					if (num16 < -0.85f)
					{
						num16 *= 1f - Mathf.Clamp01(activeScope.nightVision.nullificationExtremeDark * num18);
					}
					else if (num16 < -0.65f)
					{
						num16 *= 1f - Mathf.Clamp01(activeScope.nightVision.nullificationDarker * num18);
					}
					else if (num16 < 0f)
					{
						num16 *= 1f - Mathf.Clamp01(activeScope.nightVision.nullification);
					}
				}
			}
			if (inNVGView)
			{
				float num19 = 0f;
				if (iRLight)
				{
					num19 = Mathf.Clamp(0.4f - num16, 0f, 2f) * player.LightAndLaserState.deviceStateCache.irLight;
				}
				else if (iRLaser)
				{
					num19 = Mathf.Clamp(0.2f - num16, 0f, 2f) * player.LightAndLaserState.deviceStateCache.irLaser;
				}
				else if (iRLightSub)
				{
					num19 = Mathf.Clamp(0f - num16, 0f, 2f) * player.LightAndLaserState.deviceStateCacheSub.irLight;
				}
				else if (iRLaserSub)
				{
					num19 = Mathf.Clamp(0f - num16, 0f, 2f) * player.LightAndLaserState.deviceStateCacheSub.irLaser;
				}
				num16 += num19 * Mathf.InverseLerp(0f, -1f, num16);
			}
			num17 = Mathf.Pow(num16, 3f);
			if (num17 < 0f)
			{
				num17 *= 1f + disFactor * Mathf.Clamp01(1.2f - poseFactor) * (flag ? 0.2f : 1f) * (flag2 ? 0.9f : 1f);
			}
			else if (num17 > 0f)
			{
				num17 /= 1f + Mathf.InverseLerp(10f, 100f, magnitude3);
			}
		}
		if (player.DebugInfo != null)
		{
			if (ShouldLogSeenCoef())
			{
				player.DebugInfo.lastScore = num16;
				player.DebugInfo.lastCalcTo0 = __result;
				player.DebugInfo.lastFactor1 = num17;
			}
			player.DebugInfo.nearestCaution = num3;
		}
		ProcessFoliageStealth(ref __result, player, val2, num17, disFactor, poseFactor, visionAngleDelta90Clamped, value2, value4, num3, nearestAI);
		if (ShouldLogSeenCoef())
		{
			player.DebugInfo.lastCalcTo1 = __result;
		}
		float num20 = Mathf.InverseLerp(5f, 0f, magnitude3 - 1f);
		float num21 = Mathf.InverseLerp(15f, 0f, magnitude3 - 1f);
		float num22 = Mathf.InverseLerp(10f, 0f, magnitude3 - 1f);
		num22 *= num22;
		num20 *= Mathf.InverseLerp(100f, 15f, num6);
		num21 *= Mathf.InverseLerp(100f, 15f, num6);
		num22 *= Mathf.InverseLerp(100f, 15f, num6);
		float detailScoreRaw = 0f;
		ProcessTerrainDetailStealth(ref __result, __instance2, player, val2, position, magnitude3, num6, visionAngleDeltaVerticalSigned, poseFactor, isInPronePose, inThermalView, settings, distanceToEnemyNormalized, mainParts, disFactor, disFactorSmooth, flag, flag2, num3, num12, visionAngleDelta90Clamped, value2, value3, value5, nearestAI, num20, num21, num22, ref detailScoreRaw, ref deNullification);
		if (ShouldLogSeenCoef())
		{
			player.DebugInfo.lastCalcTo2 = __result;
		}
		ProcessBushRatting(ref __result, __instance2, player, val2, inThermalView, botImpactType, magnitude, num4, value, value2, value3, poseFactor, num6, visionAngleDeltaVerticalSigned, magnitude3, flag, flag2, num3, nearestAI, ref num20, ref num22);
		if (ShouldLogSeenCoef())
		{
			player.DebugInfo.lastCalcTo3 = __result;
		}
		float num23 = Mathf.InverseLerp(-0.7f, -1f, num16);
		num23 *= num23;
		if (player.PlayerLitScoreProfile == null && ThatsLitPlugin.AlternativeReactionFluctuation.Value)
		{
			float num24 = ((float)num3 / 9f - 0.5f) * (0.05f + 0.5f * value4 * value4);
			__result += num24;
			__result *= 1f + num24 / 2f;
		}
		else if (player.PlayerLitScoreProfile != null && Mathf.Abs(num16) >= 0.05f)
		{
			if (Singleton<ThatsLitGameworld>.Instance.IsWinter && player.Foliage != null)
			{
				float num25 = 1f - player.Foliage.FoliageScore * detailScoreRaw;
				num25 *= 1f - num13;
				num25 = Mathf.Clamp01(num25);
				disFactor *= 0.7f + 0.3f * num25;
			}
			num17 = Mathf.Clamp(num17, -0.975f, 0.975f);
			float num26 = -1f * Mathf.Pow(num17, 2f) * Mathf.Sign(num17) * (Random.Range(0.5f, 1f) - 0.5f * num22);
			num26 += num * (10f + value * 20f) * (0.1f + 0.9f * num9 * num11) * num23 / poseFactor;
			num26 *= ((botImpactType == BotImpactType.DEFAULT) ? 1f : 0.5f);
			num26 *= ((num26 > 0f) ? ThatsLitPlugin.DarknessImpactScale : ThatsLitPlugin.BrightnessImpactScale);
			__result += num26;
			if (__result < 0f)
			{
				__result = 0f;
			}
			if (num17 < 0f)
			{
				float num27 = 0.5f * num22 * (0.7f + 0.3f * value2 * poseFactor);
				num27 += 0.5f * num20;
				num27 *= 0.9f + 0.4f * num2;
				num27 = Mathf.Clamp01(num27);
				float num28 = 0.2f * value5 * Mathf.InverseLerp(-0.8f, -1f, num16);
				float num29 = num17 * Mathf.Clamp01(1f - num27);
				num29 *= 0.4f + 0.6f * Mathf.Clamp01(num12 + num28);
				if (Random.Range(-1f, 0f) > num29)
				{
					__result *= 100f * ThatsLitPlugin.DarknessImpactScale;
				}
				else
				{
					float num30 = num17 * num17 * 0.5f + 0.5f * Mathf.Abs(num17 * num17 * num17);
					num30 *= 3f;
					num30 *= ThatsLitPlugin.DarknessImpactScale;
					num30 *= 1f - num27;
					num30 *= 0.7f + 0.3f * num12;
					__result *= 1f + num30;
				}
			}
			else if (num17 > 0f)
			{
				if (value5 < num17 * num17)
				{
					__result *= 1f - 0.5f * ThatsLitPlugin.BrightnessImpactScale;
				}
				else
				{
					__result /= 1f + num17 / 4f * ThatsLitPlugin.BrightnessImpactScale;
				}
			}
		}
		if (ShouldLogSeenCoef())
		{
			player.DebugInfo.lastCalcTo4 = __result;
		}
		if (num4 < __instance2.Owner.Settings.FileSettings.Look.SEC_REPEATED_SEEN && (double)num5 < __instance2.Owner.Settings.FileSettings.Look.DIST_SQRT_REPEATED_SEEN && __result < 0.5f)
		{
			__result += (0.5f - __result) * (value * Mathf.Clamp01(num6 / 90f)) * (value3 * Mathf.Clamp01(magnitude / 5f)) * (__instance2.Owner.Mover.Sprinting ? 1f : 0.75f);
		}
		if (ThatsLitPlugin.EnableMovementImpact.Value)
		{
			if (__instance2.Owner.Mover.Sprinting)
			{
				__result *= 1f + value2 / (3f - (float)num3 * 0.1f) * Mathf.InverseLerp(30f, 75f, num6);
			}
			else if (!__instance2.Owner.Mover.IsMoving)
			{
				float num31 = __result * (value4 / (5f + (float)num3 * 0.1f));
				__result -= num31;
			}
			if (num2 > 0.01f)
			{
				float num32 = __result * (value2 / (5f + (float)num3 * 0.1f)) * num2 * (1f - num23) * Mathf.Clamp01(poseFactor);
				__result -= num32;
			}
		}
		if (ShouldLogSeenCoef())
		{
			player.DebugInfo.lastCalcTo5 = __result;
		}
		int upperVisible = 4;
		ProcessVisibleParts(ref __result, ref upperVisible, __instance2, player, num6, num3, zoomedDis, num12, value, value2, nearestAI, botImpactType);
		ProcessSimFreeLook(ref __result, __instance2, player, lookDirection, num3, value, num12, activeScope, activeGoggle, playerFC, nearestAI);
		ApplyFinalScaling(ref __result, num, player, botImpactType, facingShotFactor, num17, num4, value5, deNullification, playerFC, zoomedDis, value4, flag, upperVisible, val2, nearestAI, magnitude3, lookDirection, poseFactor, num13, player.OverheadHaxRatingFactor, magnitude2, visionAngleDeltaHorizontal, num6, value3, detailScoreRaw);
		if (player.DebugInfo != null)
		{
			if (ShouldLogSeenCoef())
			{
				player.DebugInfo.lastCalcTo8 = __result;
				player.DebugInfo.lastFactor2 = num17;
				player.DebugInfo.rawTerrainScoreSample = detailScoreRaw;
			}
			player.DebugInfo.calced++;
			player.DebugInfo.calcedLastFrame++;
		}
		ThatsLitPlugin.swSeenCoef.Stop();
		bool ShouldLogSeenCoef()
		{
			if (player.DebugInfo != null && nearestAI)
			{
				if (__instance2.IsVisible)
				{
					return Time.frameCount % 19 == 18;
				}
				return true;
			}
			return false;
		}
	}

	private static void ProcessGearAndVision(EnemyInfo __instance, ThatsLitPlayer player, float visionAngleDelta, float visionAngleDeltaHorizontal, float visionAngleDeltaVertical, float dis, ref bool inThermalView, ref bool inNVGView, ref bool gearBlocking, ref float disFactor, ref float zoomedDis, ref ThatsLitCompat.ScopeTemplate activeScope, ref ThatsLitCompat.GoggleTemplate activeGoggle, float rand1, int caution)
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		float num = Mathf.Sin(Time.time / (1f + 0.1f * (float)caution) + (__instance.Owner.Position.x + __instance.Owner.Position.z) % 2f);
		activeScope = null;
		BotNightVisionData nightVision = __instance.Owner.NightVision;
		activeGoggle = null;
		if (nightVision != null && nightVision.UsingNow && ThatsLitCompat.Goggles.TryGetValue((string)((EFT.InventoryLogic.ItemComponent)nightVision.NightVisionItem).Item.TemplateId, out var value))
		{
			activeGoggle = value?.TemplateInstance;
		}
		if (activeGoggle != null)
		{
			NightVisionComponent nightVisionItem = nightVision.NightVisionItem;
			if (nightVisionItem != null)
			{
				EFT.InventoryLogic.INightVisionComponentTemplate template = nightVisionItem.Template;
				if (((template != null) ? new EMask?(template.Mask) : ((EMask?)null)) == (EMask?)0 && activeGoggle.thermal != null && (float)activeGoggle.thermal.effectiveDistance > dis)
				{
					if ((float)activeGoggle.thermal.verticalFOV > visionAngleDeltaVertical && (float)activeGoggle.thermal.horizontalFOV > visionAngleDeltaHorizontal)
					{
						inThermalView = true;
					}
					else
					{
						gearBlocking = true;
					}
					return;
				}
			}
			NightVisionComponent nightVisionItem2 = nightVision.NightVisionItem;
			if (nightVisionItem2 != null)
			{
				EFT.InventoryLogic.INightVisionComponentTemplate template2 = nightVisionItem2.Template;
				if (((template2 != null) ? new EMask?(template2.Mask) : ((EMask?)null)) == (EMask?)0)
				{
					return;
				}
			}
			if (activeGoggle.nightVision != null)
			{
				if ((float)activeGoggle.nightVision.verticalFOV > visionAngleDeltaVertical && (float)activeGoggle.nightVision.horizontalFOV > visionAngleDeltaHorizontal)
				{
					inNVGView = true;
				}
				else
				{
					gearBlocking = true;
				}
			}
		}
		else
		{
			if (!(num > 0.5f))
			{
				return;
			}
			BotOwner owner = __instance.Owner;
			object obj;
			if (owner == null)
			{
				obj = null;
			}
			else
			{
				Player getPlayer = owner.GetPlayer;
				if (getPlayer == null)
				{
					obj = null;
				}
				else
				{
					ProceduralWeaponAnimation proceduralWeaponAnimation = getPlayer.ProceduralWeaponAnimation;
					obj = ((proceduralWeaponAnimation != null) ? proceduralWeaponAnimation.CurrentAimingMod : null);
				}
			}
			SightComponent val = (SightComponent)obj;
			if (val != null && ThatsLitCompat.Scopes.TryGetValue((string)((EFT.InventoryLogic.ItemComponent)val).Item.TemplateId, out var value2))
			{
				activeScope = value2?.TemplateInstance;
			}
			if (activeScope == null)
			{
				return;
			}
			if (rand1 < 0.1f)
			{
				val.SetScopeMode(Random.Range(0, val.ScopesCount), Random.Range(0, 2));
			}
			float num2 = val.GetCurrentOpticZoom();
			if (num2 == 0f)
			{
				num2 = 1f;
			}
			if (visionAngleDelta <= 60f / num2)
			{
				disFactor = Mathf.InverseLerp(10f, 110f, dis / num2);
				zoomedDis /= num2;
				if (activeScope?.thermal != null && dis <= (float)activeScope.thermal.effectiveDistance)
				{
					inThermalView = true;
				}
				else if (activeScope?.nightVision != null)
				{
					inNVGView = true;
				}
			}
		}
	}

	private static void ProcessOverheadAndIndoorOverlook(ref float __result, bool canSeeLight, float visionAngleDeltaVerticalSigned, float pPoseFactor, float notSeenRecentAndNear, float pSpeedFactor, float disFactor, int caution, float rand1, float rand2, float rand5, EnemyInfo __instance, ThatsLitPlayer player, float insideTime, float dis, float visionAngleDelta, float seenPosDeltaFactorSqr, float sinceSeenFactorSqr, float rand3)
	{
		if (!canSeeLight)
		{
			float num = Mathf.InverseLerp(15f, 90f, visionAngleDeltaVerticalSigned);
			num *= num;
			num /= Mathf.Clamp(pPoseFactor, 0.2f, 1f);
			num *= notSeenRecentAndNear;
			num *= Mathf.Clamp01(1f - pSpeedFactor * 2f);
			num *= 1f + disFactor;
			switch (caution)
			{
			case 0:
				num /= 2f;
				break;
			case 1:
				num /= 1.4f;
				break;
			case 2:
				num /= 1.15f;
				break;
			case 6:
			case 7:
			case 8:
			case 9:
				num *= 1.2f;
				break;
			}
			if (rand1 < Mathf.Clamp(num, 0f, 0.995f))
			{
				__result += rand5 * 0.1f;
				__result *= 10f + rand2 * 100f;
			}
		}
		if (!canSeeLight)
		{
			bool isInside = __instance.Owner.AIData.IsInside;
			bool isInside2 = player.Player.AIData.IsInside;
			if (!isInside && isInside2 && insideTime >= 1f)
			{
				float num2 = dis * Mathf.Clamp01(visionAngleDeltaVerticalSigned / 40f) * Mathf.Clamp01(visionAngleDelta / 60f) * (0.3f * seenPosDeltaFactorSqr + 0.7f * sinceSeenFactorSqr);
				__result *= 1f + num2 * (0.75f + rand3 * 0.05f * (float)caution);
			}
		}
	}

	private static void ProcessGlobalOverlook(ref float __result, ref float globalOverlookChance, bool canSeeLight, float pPoseFactor, float zoomedDis, float notSeenRecentAndNear, float dis, float visionAngleDelta, int caution, float rand5, float rand1, ThatsLitPlayer player, bool nearestAI, Vector3 eyeToPlayerBody, BotImpactType botImpactType)
	{
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		globalOverlookChance = 0.01f / pPoseFactor;
		globalOverlookChance *= 1f + (6f + 0.5f * (float)caution) * Mathf.InverseLerp(10f, 110f, zoomedDis) * notSeenRecentAndNear;
		if (canSeeLight)
		{
			globalOverlookChance /= 2f - Mathf.InverseLerp(10f, 110f, dis);
		}
		globalOverlookChance *= 0.35f + 0.65f * Mathf.InverseLerp(5f, 90f, visionAngleDelta);
		if (player.DebugInfo != null && nearestAI)
		{
			player.DebugInfo.lastGlobalOverlookChance = globalOverlookChance;
			player.DebugInfo.nearestOffset = eyeToPlayerBody;
		}
		globalOverlookChance *= ((botImpactType != BotImpactType.DEFAULT) ? 0.5f : 1f);
		if (rand5 < globalOverlookChance)
		{
			__result *= 10f + rand1 * 10f;
		}
	}

	private static void ProcessFoliageStealth(ref float __result, ThatsLitPlayer player, Vector3 eyeToPlayerBody, float factor, float disFactor, float pPoseFactor, float visionAngleDelta90Clamped, float rand2, float rand4, int caution, bool nearestAI)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		if (player.Foliage == null)
		{
			return;
		}
		Vector2 val = Vector2.zero;
		float num = 360f;
		for (int i = 0; i < Math.Min(ThatsLitPlugin.FoliageSamples.Value, player.Foliage.FoliageCount); i++)
		{
			FoliageInfo foliageInfo = player.Foliage.Foliage[i];
			if (foliageInfo == default(FoliageInfo))
			{
				break;
			}
			float num2 = Vector2.Angle(new Vector2(0f - eyeToPlayerBody.x, 0f - eyeToPlayerBody.z), foliageInfo.dir);
			if (num2 < num)
			{
				num = num2;
				val = foliageInfo.dir;
			}
		}
		float foliageScore = player.Foliage.FoliageScore;
		foliageScore *= 1f + Mathf.InverseLerp(0f, -1f, factor);
		if (val != Vector2.zero)
		{
			foliageScore *= Mathf.InverseLerp(90f, 0f, num);
		}
		float num3 = Mathf.Clamp01(disFactor * foliageScore * ThatsLitPlugin.FoliageImpactScale.Value * Mathf.Clamp01(2f - pPoseFactor));
		num3 *= 0.5f + 0.5f * visionAngleDelta90Clamped;
		if (Random.Range(0f, 1.01f) < num3)
		{
			__result += rand2;
			__result *= 1f + disFactor + rand4 * (5f + (float)caution);
		}
	}

	private static void ProcessTerrainDetailStealth(ref float __result, EnemyInfo __instance, ThatsLitPlayer player, Vector3 eyeToPlayerBody, Vector3 botHeadPos, float dis, float visionAngleDelta, float visionAngleDeltaVerticalSigned, float pPoseFactor, bool isInPronePose, bool inThermalView, BotSettings settings, float distanceToEnemyNormalized, Dictionary<BodyPartType, EnemyPart> playerParts, float disFactor, float disFactorSmooth, bool canSeeLight, bool canSeeLaser, int caution, float notSeenRecentAndNear, float visionAngleDelta90Clamped, float rand2, float rand3, float rand5, bool nearestAI, float cqb6mTo1m, float cqb16mTo1m, float cqb11mTo1mSquared, ref float detailScoreRaw, ref float deNullification)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_009e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0355: Unknown result type (might be due to invalid IL or missing references)
		//IL_0356: Unknown result type (might be due to invalid IL or missing references)
		//IL_035b: Unknown result type (might be due to invalid IL or missing references)
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		detailScoreRaw = 0f;
		if (inThermalView || player.TerrainDetails == null)
		{
			return;
		}
		TerrainDetailScore terrainScore = Singleton<ThatsLitGameworld>.Instance.CalculateDetailScore(player.TerrainDetails, -eyeToPlayerBody, dis, visionAngleDeltaVerticalSigned);
		if (terrainScore.prone > 0.1f || terrainScore.regular > 0.1f)
		{
			if (isInPronePose)
			{
				Vector3 val = (playerParts[(BodyPartType)4].Position + playerParts[(BodyPartType)5].Position) / 2f;
				Vector3 val2 = playerParts[(BodyPartType)0].Position - val;
				Vector2 val3 = new Vector2(val2.x, val2.z);
				Vector3 val4 = botHeadPos - val;
				Vector2 val5 = default(Vector2);
				val5 = new Vector2(val4.x, val4.z);
				float num4 = Vector2.Angle(val3, val5);
				if (num4 >= 90f)
				{
					num = (180f - num4) / 90f;
				}
				else if (num4 <= 90f)
				{
					num = num4 / 90f;
				}
				num = 1f - num;
				float num5 = default(float);
				__instance.GetAngleK(settings, distanceToEnemyNormalized, out num5);
				num2 = ((num2 >= 90f) ? ((180f - num2) / 15f) : ((!(num2 <= 0f) || !(num2 >= -90f)) ? 0f : (num2 / -15f)));
				num3 = terrainScore.prone * Mathf.Clamp01(1f - num2 * num);
			}
			else
			{
				num3 = Utility.GetPoseWeightedRegularTerrainScore(pPoseFactor, terrainScore);
				num3 *= (1f - cqb11mTo1mSquared) * Mathf.InverseLerp(-25f, 5f, visionAngleDeltaVerticalSigned);
			}
			num3 = (detailScoreRaw = Mathf.Min(num3, 2.5f - pPoseFactor)) * (1f + disFactor / 2f);
			if (canSeeLight)
			{
				num3 /= 2f - disFactor;
			}
			if (canSeeLaser)
			{
				num3 *= 0.8f + 0.2f * disFactor;
			}
			switch (caution)
			{
			case 0:
				num3 /= 1.5f;
				num3 *= 1f - cqb16mTo1m * Mathf.Clamp01((5f - visionAngleDeltaVerticalSigned) / 30f);
				break;
			case 1:
				num3 /= 1.25f;
				num3 *= 1f - cqb16mTo1m * Mathf.Clamp01((5f - visionAngleDeltaVerticalSigned) / 40f);
				break;
			case 2:
			case 3:
			case 4:
				num3 *= 1f - cqb11mTo1mSquared * Mathf.Clamp01((5f - visionAngleDeltaVerticalSigned) / 40f);
				break;
			case 5:
			case 6:
			case 7:
			case 8:
			case 9:
				num3 *= 1.2f;
				num3 *= 1f - cqb6mTo1m * Mathf.Clamp01((5f - visionAngleDeltaVerticalSigned) / 50f);
				break;
			}
			if (Random.Range(0f, 1.001f) < Mathf.Clamp01(num3))
			{
				float num6 = 19f * Mathf.Clamp01(notSeenRecentAndNear + 0.25f * Mathf.Clamp01(num3 - 1f)) * (0.05f + disFactorSmooth);
				if (num3 > 1f && isInPronePose)
				{
					num6 += (2f + rand5 * 3f) * (1f - disFactorSmooth) * (2f - visionAngleDelta90Clamped);
					deNullification = 0.5f;
				}
				__result *= 1f + num6;
				if (player.DebugInfo != null && nearestAI)
				{
					player.DebugInfo.lastTriggeredDetailCoverDirNearest = -eyeToPlayerBody;
				}
			}
		}
		if (player.DebugInfo != null && nearestAI)
		{
			player.DebugInfo.lastFinalDetailScoreNearest = num3;
			player.DebugInfo.lastDisFactorNearest = disFactor;
		}
	}

	private static void ProcessBushRatting(ref float __result, EnemyInfo __instance, ThatsLitPlayer player, Vector3 eyeToPlayerBody, bool inThermalView, BotImpactType botImpactType, float lastSeenPosDelta, float sinceSeen, float rand1, float rand2, float rand3, float pPoseFactor, float visionAngleDelta, float visionAngleDeltaVerticalSigned, float dis, bool canSeeLight, bool canSeeLaser, int caution, bool nearestAI, ref float cqb6mTo1m, ref float cqb11mTo1mSquared)
	{
		if (!ThatsLitPlugin.EnabledBushRatting.Value || inThermalView || player.Foliage == null || botImpactType == BotImpactType.BOSS || (__instance.HaveSeen && !(lastSeenPosDelta > 30f + rand1 * 20f) && (!(sinceSeen > 150f + 150f * rand3) || !(lastSeenPosDelta > 10f + 10f * rand2))))
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		float num4 = 0f;
		float num5 = 1f;
		bool flag = true;
		FoliageInfo foliageInfo = player.Foliage.Foliage[0];
		switch (foliageInfo.name)
		{
		case "filbert_big01":
			num = 1f;
			num2 = Mathf.InverseLerp(1.5f, 0.8f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(2.5f, 0f, dis);
			num3 = Mathf.InverseLerp(1f, 0.45f, pPoseFactor);
			num5 = Mathf.InverseLerp(-60f, 0f, visionAngleDeltaVerticalSigned);
			break;
		case "filbert_big02":
			num = 0.4f + 0.6f * Mathf.InverseLerp(0f, 20f, visionAngleDelta);
			num2 = Mathf.InverseLerp(0.1f, 0.6f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 10f, dis);
			num3 = ((pPoseFactor == 0.05f) ? 0.7f : 1f);
			break;
		case "filbert_big03":
			num = 0.4f + 0.6f * Mathf.InverseLerp(0f, 30f, visionAngleDelta);
			num2 = Mathf.InverseLerp(0.45f, 0.2f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 15f, dis);
			num3 = ((pPoseFactor == 0.05f) ? 0f : (0.1f + 0.9f * Mathf.InverseLerp(0.45f, 1f, pPoseFactor)));
			break;
		case "filbert_01":
			num = 1f;
			num2 = Mathf.InverseLerp(0.6f, 0.25f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 12f, dis);
			Mathf.Clamp01(dis / 12f);
			num3 = Mathf.InverseLerp(0.75f, 0.3f, pPoseFactor);
			break;
		case "filbert_small01":
			num = 0.2f + 0.8f * Mathf.InverseLerp(0f, 35f, visionAngleDelta);
			num2 = Mathf.InverseLerp(0.3f, 0.15f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 10f, dis);
			num3 = ((pPoseFactor == 0.45f) ? 1f : 0f);
			break;
		case "filbert_small02":
			num = 0.2f + 0.8f * Mathf.InverseLerp(0f, 25f, visionAngleDelta);
			num2 = Mathf.InverseLerp(0.3f, 0.15f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 8f, dis);
			num3 = ((pPoseFactor == 0.45f) ? 1f : 0f);
			break;
		case "filbert_small03":
			num = 0.2f + 0.8f * Mathf.InverseLerp(0f, 40f, visionAngleDelta);
			num2 = Mathf.InverseLerp(1f, 0.25f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 10f, dis);
			num3 = ((pPoseFactor == 0.45f) ? 1f : 0f);
			break;
		case "filbert_dry03":
			num = 0.4f + 0.6f * Mathf.InverseLerp(0f, 30f, visionAngleDelta);
			num2 = Mathf.InverseLerp(0.8f, 0.5f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 30f, dis);
			num3 = ((pPoseFactor == 0.05f) ? 0f : (0.1f + Mathf.InverseLerp(0.45f, 1f, pPoseFactor) * 0.9f));
			break;
		case "fibert_hedge01":
			num = Mathf.InverseLerp(0f, 40f, visionAngleDelta);
			num2 = Mathf.InverseLerp(0.2f, 0.1f, foliageInfo.dis);
			num4 = Mathf.Clamp01(dis / 30f);
			num3 = ((pPoseFactor == 0.45f) ? 1f : 0f);
			break;
		case "fibert_hedge02":
			num = 0.2f + 0.8f * Mathf.InverseLerp(0f, 40f, visionAngleDelta);
			num2 = Mathf.InverseLerp(0.3f, 0.1f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 20f, dis);
			num3 = ((pPoseFactor == 0.45f) ? 1f : 0f);
			break;
		case "privet_hedge_2":
		case "privet_hedge":
			num = Mathf.InverseLerp(30f, 90f, visionAngleDelta);
			num2 = Mathf.InverseLerp(1f, 0f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 50f, dis);
			num3 = ((pPoseFactor < 0.45f) ? 1f : 0f);
			break;
		case "bush_dry01":
			num = 0.2f + 0.8f * Mathf.InverseLerp(0f, 35f, visionAngleDelta);
			num2 = Mathf.InverseLerp(0.3f, 0.15f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 25f, dis);
			num3 = ((pPoseFactor == 0.45f) ? 1f : 0f);
			break;
		case "bush_dry02":
			num = 1f;
			num2 = Mathf.InverseLerp(1.5f, 1f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(0f, 15f, dis);
			num3 = Mathf.InverseLerp(0.55f, 0.45f, pPoseFactor);
			num5 = Mathf.InverseLerp(60f, 0f, 0f - visionAngleDeltaVerticalSigned);
			break;
		case "bush_dry03":
			num = 0.4f + 0.6f * Mathf.Clamp01(visionAngleDelta / 20f);
			num2 = 1f - Mathf.Clamp01((foliageInfo.dis - 0.5f) / 0.3f);
			num4 = Mathf.Clamp01(dis / 20f);
			num3 = ((pPoseFactor == 0.05f) ? 0.6f : (1f - Mathf.Clamp01((pPoseFactor - 0.45f) / 0.55f)));
			break;
		case "tree02":
			num5 = 0.7f + 0.5f * Mathf.Clamp01((0f - visionAngleDeltaVerticalSigned - 10f) / 40f);
			num = 0.2f + 0.8f * Mathf.Clamp01(visionAngleDelta / 45f);
			num2 = 1f - Mathf.Clamp01((foliageInfo.dis - 0.5f) / 0.2f);
			num4 = Mathf.Clamp01(dis * num5 / 20f);
			num3 = ((pPoseFactor == 0.05f) ? 0f : (0.1f + (pPoseFactor - 0.45f) / 0.55f * 0.9f));
			break;
		case "pine01":
			num5 = 0.7f + 0.5f * Mathf.Clamp01((0f - visionAngleDeltaVerticalSigned - 10f) / 40f);
			num = 0.2f + 0.8f * Mathf.Clamp01(visionAngleDelta / 30f);
			num2 = Mathf.InverseLerp(1f, 0.35f, foliageInfo.dis);
			num4 = Mathf.InverseLerp(5f, 25f, dis * num5);
			num3 = ((pPoseFactor == 0.05f) ? 0f : (0.5f + 0.5f * Mathf.InverseLerp(0.45f, 1f, pPoseFactor) * 0.5f));
			break;
		case "pine05":
			num = 1f;
			num2 = 1f - Mathf.Clamp01((foliageInfo.dis - 0.5f) / 0.45f);
			num4 = Mathf.Clamp01(dis / 20f);
			num3 = ((pPoseFactor == 0.05f) ? 0f : (0.5f + (pPoseFactor - 0.45f) / 0.55f * 0.5f));
			num5 = Mathf.Clamp01((0f - visionAngleDeltaVerticalSigned - 15f) / 45f);
			break;
		case "fern01":
			num = 0.2f + 0.8f * Mathf.Clamp01(visionAngleDelta / 25f);
			num2 = 1f - Mathf.Clamp01((foliageInfo.dis - 0.1f) / 0.2f);
			num4 = Mathf.Clamp01(dis / 30f);
			num3 = ((pPoseFactor == 0.05f) ? 1f : ((1f - pPoseFactor) / 5f));
			break;
		default:
			flag = false;
			break;
		}
		float num6 = Mathf.Clamp01(num * num2 * num4 * num3 * num5);
		if (Singleton<ThatsLitGameworld>.Instance.IsWinter)
		{
			num6 /= 1.15f;
		}
		if (player.DebugInfo != null && nearestAI)
		{
			player.DebugInfo.lastBushRat = num6;
		}
		if (botImpactType == BotImpactType.FOLLOWER || canSeeLight || (canSeeLaser && rand3 < 0.2f))
		{
			num6 /= 2f;
		}
		if (!flag || !(num6 > 0.01f))
		{
			return;
		}
		if (player.DebugInfo != null && nearestAI)
		{
			player.DebugInfo.IsBushRatting = flag;
		}
		__result = Mathf.Max(__result, dis);
		switch (caution)
		{
		case 0:
		case 1:
			if (rand2 > 0.01f)
			{
				__result *= 1f + 4f * num6 * Random.Range(0.2f, 0.4f);
			}
			cqb6mTo1m *= 1f - num6 * 0.5f;
			cqb11mTo1mSquared *= 1f - num6 * 0.5f;
			break;
		case 2:
		case 3:
		case 4:
			if (rand3 > 0.005f)
			{
				__result *= 1f + 8f * num6 * Random.Range(0.3f, 0.65f);
			}
			cqb6mTo1m *= 1f - num6 * 0.8f;
			cqb11mTo1mSquared *= 1f - num6 * 0.8f;
			break;
		case 5:
		case 6:
		case 7:
		case 8:
		case 9:
			if (rand1 > 0.001f)
			{
				__result *= 1f + 6f * num6 * Random.Range(0.5f, 1f);
			}
			cqb6mTo1m *= 1f - num6;
			cqb11mTo1mSquared *= 1f - num6;
			break;
		}
	}

	private static void ProcessVisibleParts(ref float __result, ref int upperVisible, EnemyInfo __instance, ThatsLitPlayer player, float visionAngleDelta, int caution, float zoomedDis, float notSeenRecentAndNear, float rand1, float rand2, bool nearestAI, BotImpactType botImpactType)
	{
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected I4, but got Unknown
		float num = 6f;
		upperVisible = 4;
		Dictionary<BodyPartType, EnemyPartVision> allPartsVision = (Dictionary<BodyPartType, EnemyPartVision>)AccessTools.Field(typeof(EnemyInfo), "_allPartsVision").GetValue(__instance);
		foreach (BodyPartType bodyPartType in __instance.AllActiveParts)
		{
			EnemyPartVision partVision = null;
			if (allPartsVision != null)
			{
				allPartsVision.TryGetValue(bodyPartType, out partVision);
			}
			if (partVision == null || !partVision.Visible)
			{
				switch ((int)bodyPartType)
				{
				case 0:
					num -= 0.5f;
					upperVisible--;
					break;
				case 1:
					num -= 2f;
					upperVisible--;
					break;
				case 2:
				case 3:
					num -= 1f;
					upperVisible--;
					break;
				default:
					num -= 1f;
					break;
				}
			}
		}
		if (ThatsLitPlugin.EnableBodyPartsRecognition.Value)
		{
			float num2 = Mathf.InverseLerp(6f, 1f, num);
			num2 *= num2;
			if (player.DebugInfo != null && nearestAI)
			{
				player.DebugInfo.lastVisiblePartsFactor = num2;
			}
			num2 *= Mathf.InverseLerp(0f, 45f - (float)caution, visionAngleDelta) * notSeenRecentAndNear * Mathf.InverseLerp(0f, 10f, zoomedDis);
			if (botImpactType != BotImpactType.DEFAULT)
			{
				num2 /= 2f;
			}
			__result += 0.04f * rand2 * num2;
			__result *= 1f + (0.15f + 0.85f * rand1) * num2 * 9f;
		}
	}

	private static void ProcessSimFreeLook(ref float __result, EnemyInfo __instance, ThatsLitPlayer player, Vector3 botVisionDir, int caution, float rand1, float notSeenRecentAndNear, ThatsLitCompat.ScopeTemplate activeScope, ThatsLitCompat.GoggleTemplate activeGoggle, FirearmController playerFC, bool nearestAI)
	{
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_0166: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_016d: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0174: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Unknown result type (might be due to invalid IL or missing references)
		//IL_0187: Unknown result type (might be due to invalid IL or missing references)
		//IL_018c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0241: Unknown result type (might be due to invalid IL or missing references)
		//IL_0242: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0204: Unknown result type (might be due to invalid IL or missing references)
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0222: Unknown result type (might be due to invalid IL or missing references)
		//IL_019d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		float time = Time.time;
		BotOwner owner = __instance.Owner;
		float? obj;
		if (owner == null)
		{
			obj = null;
		}
		else
		{
			EFT.BotMemory memory = owner.Memory;
			if (memory == null)
			{
				obj = null;
			}
			else
			{
				EnemyInfo goalEnemy = memory.GoalEnemy;
				obj = ((goalEnemy != null) ? new float?(goalEnemy.TimeLastSeen) : ((float?)null));
			}
		}
		float valueOrDefault = (time - obj).GetValueOrDefault();
		if (ThatsLitPlugin.EnableSimFreeLook.Value && valueOrDefault > 2f)
		{
			float num = 0.75f * Mathf.Sin(Time.time / (1f + (float)caution)) + 0.25f * Mathf.Sin(Time.time);
			int num2 = (int)((Time.time + num * 0.5f) / (3f + (float)caution / 5f));
			num2 %= 61;
			Vector3 val = botVisionDir;
			float num3 = focusLUTs[caution][num2];
			num3 += (20f - (float)caution) * num;
			num3 = Mathf.Clamp(num3, -90f, 90f);
			float num4 = focusLUTs[(caution + num2) % 10][num2];
			num4 = Mathf.Abs(num4 / 2f);
			num4 = Mathf.Clamp(num4, 0f, 45f);
			val = MyExtensions.RotateAroundPivot(val, Vector3.up, new Vector3(0f, num3));
			val = MyExtensions.RotateAroundPivot(val, Vector3.Cross(Vector3.up, botVisionDir), new Vector3(0f, 0f - num4));
			if (playerFC != null && ((AbstractHandsController)playerFC).IsAiming)
			{
				val = new Vector3(Mathf.Lerp(val.x, botVisionDir.x, 0.5f), Mathf.Lerp(val.y, botVisionDir.y, 0.5f), Mathf.Lerp(val.z, botVisionDir.z, 0.5f));
			}
			if (activeGoggle != null)
			{
				val = new Vector3(Mathf.Lerp(val.x, botVisionDir.x, 0.5f), Mathf.Lerp(val.y, botVisionDir.y, 0.5f), Mathf.Lerp(val.z, botVisionDir.z, 0.5f));
			}
			float num5 = Mathf.InverseLerp(0f, 120f, Vector3.Angle(botVisionDir, val));
			num5 -= 0.5f;
			__result *= 1f + (0.3f + rand1 * 0.2f) * notSeenRecentAndNear * num5 * Mathf.InverseLerp(2f, 6f, valueOrDefault);
			if (player.DebugInfo != null && nearestAI)
			{
				player.DebugInfo.lastNearestFocusAngleX = num3;
				player.DebugInfo.lastNearestFocusAngleY = num4;
			}
		}
	}

	private static void ApplyFinalScaling(ref float __result, float original, ThatsLitPlayer player, BotImpactType botImpactType, float facingShotFactor, float factor, float sinceSeen, float rand5, float deNullification, FirearmController playerFC, float zoomedDis, float rand4, bool canSeeLight, int upperVisible, Vector3 eyeToPlayerBody, bool nearestAI, float dis, Vector3 botVisionDir, float pPoseFactor, float insideTime, float playerOverheadHaxRatingFactor, float botVelocity, float visionAngleDeltaHorizontal, float visionAngleDelta, float rand3, float detailScoreRaw)
	{
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_0225: Unknown result type (might be due to invalid IL or missing references)
		//IL_0227: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Unknown result type (might be due to invalid IL or missing references)
		//IL_015f: Unknown result type (might be due to invalid IL or missing references)
		__result = Mathf.Lerp(__result, original, (botImpactType == BotImpactType.DEFAULT) ? 0f : 0.5f);
		if (__result > original)
		{
			float num = 1f - sinceSeen / 0.2f;
			num *= rand5;
			num -= deNullification;
			__result = Mathf.Lerp(__result, original, Mathf.Clamp01(num));
			__result *= 1f - 0.5f * facingShotFactor * Mathf.InverseLerp(-0.45f, 0.45f, factor);
			if (playerFC != null && playerFC.IsStationaryWeapon)
			{
				__result *= 1f - 0.3f * rand4 * Mathf.InverseLerp(30f, 5f, zoomedDis);
			}
		}
		if (ThatsLitPlugin.EnableExtraFlashLightReaction.Value && canSeeLight && (Object)(object)playerFC != (Object)null)
		{
			float num2 = Vector3.Angle(-eyeToPlayerBody, playerFC.WeaponDirection);
			if (upperVisible >= 3)
			{
				if (num2 < 7.5f)
				{
					__result *= 1f - 0.75f * Mathf.InverseLerp(7.5f, 0f, num2) * Mathf.InverseLerp(40f, 7.5f, zoomedDis);
				}
				else if (nearestAI && dis < 15f)
				{
					__result *= 1f - rand4 * 0.4f * Mathf.InverseLerp(15f, 1f, dis) * Mathf.InverseLerp(90f, 0f, Vector3.Angle(-playerFC.WeaponDirection, botVisionDir));
					if (player.DebugInfo != null)
					{
						player.DebugInfo.flashLightHint++;
					}
				}
			}
		}
		float num3 = pPoseFactor * 0.2f * Mathf.InverseLerp(1f, 0f, playerOverheadHaxRatingFactor) * Mathf.InverseLerp(1f, 0f, insideTime) * Mathf.InverseLerp(250f, 25f, zoomedDis) * Mathf.InverseLerp(15f, 1f, visionAngleDeltaHorizontal) * Mathf.InverseLerp(1f, 0f, botVelocity) * Mathf.InverseLerp(5f, 30f, eyeToPlayerBody.y);
		if (rand3 < num3)
		{
			__result = Mathf.Min(__result, 1f);
			if (player.DebugInfo != null)
			{
				player.DebugInfo.sniperHintOffset = eyeToPlayerBody;
				player.DebugInfo.sniperHintChance = num3;
			}
		}
		if (__result < 0.5f * original)
		{
			__result = 0.5f * original;
		}
		__result = Mathf.Lerp(original, __result, (__result < original) ? ThatsLitPlugin.FinalImpactScaleFastening.Value : ThatsLitPlugin.FinalImpactScaleDelaying.Value);
		__result += ThatsLitPlugin.FinalOffset.Value;
		if (__result < 0.005f)
		{
			__result = 0.005f;
		}
	}
}
