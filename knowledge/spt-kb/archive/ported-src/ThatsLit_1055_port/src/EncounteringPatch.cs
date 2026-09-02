using System;
using System.Diagnostics;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using SPT.Reflection.Patching;
using UnityEngine;

namespace ThatsLit.Patches.Vision;

public class EncounteringPatch : ModulePatch
{
	public struct State
	{
		public bool triggered;

		public bool unexpected;

		public bool botSprinting;

		public float visionDeviation;
	}

	internal static Stopwatch _benchmarkSW;

	protected override MethodBase GetTargetMethod()
	{
		return AccessTools.Method(typeof(EnemyInfo), "SetVisible", (Type[])null, (Type[])null);
	}

	[HarmonyAfter(new string[] { "me.sol.sain" })]
	[PatchPrefix]
	public static bool PatchPrefix(EnemyInfo __instance, bool value, ref State __state)
	{
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_012f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0145: Unknown result type (might be due to invalid IL or missing references)
		//IL_014a: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_016b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0170: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_018d: Unknown result type (might be due to invalid IL or missing references)
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0233: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_0349: Unknown result type (might be due to invalid IL or missing references)
		//IL_0353: Unknown result type (might be due to invalid IL or missing references)
		//IL_036d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0372: Unknown result type (might be due to invalid IL or missing references)
		//IL_0374: Unknown result type (might be due to invalid IL or missing references)
		//IL_038e: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_05e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0448: Unknown result type (might be due to invalid IL or missing references)
		//IL_044b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0456: Expected O, but got Unknown
		//IL_04d9: Unknown result type (might be due to invalid IL or missing references)
		__state = default(State);
		if (!value)
		{
			return true;
		}
		if (__instance.IsVisible)
		{
			return true;
		}
		if (!ThatsLitPlugin.EnabledMod.Value || !ThatsLitPlugin.EnabledEncountering.Value)
		{
			return true;
		}
		if (ThatsLitPlugin.PMCOnlyMode.Value)
		{
			BotOwner owner = __instance.Owner;
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
				return true;
			}
		}
		if ((Object)(object)__instance.Owner == (Object)null)
		{
			return true;
		}
		ThatsLitPlayer player = null;
		Singleton<ThatsLitGameworld>.Instance?.AllThatsLitPlayers?.TryGetValue(__instance.Person, out player);
		if ((Object)(object)player == (Object)null)
		{
			return true;
		}
		ThatsLitPlugin.swEncountering.MaybeResume();
		_ = __instance.Owner.Position;
		Vector3 lookDirection = __instance.Owner.GetPlayer.LookDirection;
		Vector3 botEyeToPlayerBody = __instance.Person.MainParts[(BodyPartType)1].Position - __instance.Owner.MainParts[(BodyPartType)0].Position;
		Vector3 val = __instance.EnemyLastPositionReal - __instance.Owner.MainParts[(BodyPartType)0].Position;
		float distance = botEyeToPlayerBody.magnitude;
		float visionDeviation = Vector3.Angle(lookDirection, botEyeToPlayerBody);
		float visionDeviationToLast = Vector3.Angle(botEyeToPlayerBody, val);
		if (!__instance.HaveSeen)
		{
			visionDeviationToLast = 180f;
		}
		Random.Range(-1f, 1f);
		float num = Random.Range(-1f, 1f);
		float rand3 = Random.Range(0f, 1f);
		float rand4 = Random.Range(0f, 1f);
		float sinceLastSeen = Time.time - __instance.PersonalSeenTime;
		Vector3 val2 = __instance.Person.Position - __instance.EnemyLastPosition;
		float magnitude = val2.magnitude;
		BotOwner owner2 = __instance.Owner;
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
		if (Utility.GetBotImpactType((WildSpawnType)(obj ?? (WildSpawnType)1)) != BotImpactType.BOSS)
		{
			float num2 = Mathf.InverseLerp(0f, 3.5f, distance) * Mathf.InverseLerp(70f, 100f, visionDeviation);
			num2 *= 1f + Mathf.InverseLerp(10f, 25f, botEyeToPlayerBody.y);
			if (rand3 < num2 || rand3 < GetSurpriseChanceInFront())
			{
				if (player.DebugInfo != null)
				{
					player.DebugInfo.vagueHint++;
				}
				Vector3 val3 = Random.insideUnitSphere * 50f * Mathf.InverseLerp(3.5f, 100f, distance);
				if (val3.y < 0f)
				{
					val3.y = 0f;
				}
				val3 += __instance.Person.MainParts[(BodyPartType)1].Position;
				if (__instance.Owner?.Memory != null)
				{
					BotOwner owner3 = __instance.Owner;
					if (((owner3 != null) ? owner3.Covers : null) != null)
					{
						Singleton<ThatsLitGameworld>.Instance.singleIdThrottlers.TryGetValue(__instance.Owner.ProfileId, out var value2);
						if (Time.time - value2.lastAddedDangerPoint > 1f)
						{
							if (player.DebugInfo != null)
							{
								player.DebugInfo.signalDanger++;
							}
							BotOwner owner4 = __instance.Owner;
							if (owner4 != null)
							{
								BotDangerPointsData dangerPointsData = owner4.DangerPointsData;
								if (dangerPointsData != null)
								{
									dangerPointsData.AddPointOfDanger(new PlaceForCheck(val3, (PlaceForCheckType)0), true);
								}
							}
							value2.lastAddedDangerPoint = Time.time;
							Singleton<ThatsLitGameworld>.Instance.singleIdThrottlers[__instance.Owner.ProfileId] = value2;
						}
					}
				}
				BotOwner owner5 = __instance.Owner;
				object obj2;
				if (owner5 == null)
				{
					obj2 = null;
				}
				else
				{
					BotsGroup botsGroup = owner5.BotsGroup;
					obj2 = ((botsGroup != null) ? botsGroup.CoverPointMaster : null);
				}
				if (obj2 != null && __instance.Owner?.Memory?.BotCurrentCoverInfo != null)
				{
					if (__instance != null)
					{
						BotOwner owner6 = __instance.Owner;
						if (owner6 != null)
						{
							EFT.BotMemory memory = owner6.Memory;
							if (memory != null)
							{
								memory.Spotted(false, (Vector3?)val3, (float?)null);
							}
						}
					}
					ThatsLitPlugin.swEncountering.Stop();
					return false;
				}
				if (player.DebugInfo != null)
				{
					player.DebugInfo.vagueHintCancel++;
				}
			}
		}
		if (player.DebugInfo != null)
		{
			player.DebugInfo.encounter++;
		}
		float num3 = 0.5f * Mathf.InverseLerp(0f, 9f + num * 5f, sinceLastSeen) + 0.5f * Mathf.InverseLerp(0f, 5f, magnitude);
		BotOwner owner7 = __instance.Owner;
		object obj3;
		if (owner7 == null)
		{
			obj3 = null;
		}
		else
		{
			EFT.BotMemory memory2 = owner7.Memory;
			if (memory2 == null)
			{
				obj3 = null;
			}
			else
			{
				EnemyInfo goalEnemy = memory2.GoalEnemy;
				obj3 = ((goalEnemy != null) ? goalEnemy.Person : null);
			}
		}
		if (obj3 == player.Player)
		{
			num3 *= 0.65f;
		}
		float num4 = rand4;
		val2 = player.Player.Velocity;
		if (num4 - 0.2f * Mathf.InverseLerp(0f, 5f, val2.magnitude) < num3)
		{
			State state = new State
			{
				triggered = true
			};
			BotOwner owner8 = __instance.Owner;
			state.unexpected = ((owner8 != null) ? owner8.Memory.GoalEnemy : null) != __instance && sinceLastSeen > rand3 * 10f;
			BotOwner owner9 = __instance.Owner;
			bool? obj4;
			if (owner9 == null)
			{
				obj4 = null;
			}
			else
			{
				BotMover mover = owner9.Mover;
				obj4 = ((mover != null) ? new bool?(mover.Sprinting) : ((bool?)null));
			}
			bool? flag = obj4;
			state.botSprinting = flag == true;
			state.visionDeviation = visionDeviation;
			__state = state;
		}
		ThatsLitPlugin.swEncountering.Stop();
		return true;
		float GetSurpriseChanceInFront()
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			float num5 = 0f;
			if (player.lastShotVector != Vector3.zero && Time.time - player.lastShotTime < 0.5f)
			{
				float num6 = Vector3.Angle(-botEyeToPlayerBody, player.lastShotVector);
				if (player.DebugInfo != null)
				{
					player.DebugInfo.lastEncounterShotAngleDelta = num6;
				}
				num5 = Mathf.InverseLerp(0.5f, 0f, Time.time - player.lastShotTime);
				num5 *= Mathf.InverseLerp(15f, 0f, num6);
				if (player.DebugInfo != null)
				{
					player.DebugInfo.lastEncounteringShotCutoff = num5;
				}
			}
			return (0.5f * Mathf.InverseLerp(30f + 15f * rand3, 100f, sinceLastSeen) + 0.5f * Mathf.InverseLerp(0f, 60f, visionDeviationToLast)) * Mathf.InverseLerp(10f, 110f, distance) * Mathf.InverseLerp(0f, 15f, visionDeviation) * (1f - num5 * (0.5f + 0.5f * rand4));
		}
	}

	[PatchPostfix]
	[HarmonyAfter(new string[] { "me.sol.sain" })]
	public static void PatchPostfix(EnemyInfo __instance, State __state)
	{
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_01de: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Unknown result type (might be due to invalid IL or missing references)
		if (!ThatsLitPlugin.EnabledMod.Value || !ThatsLitPlugin.EnabledEncountering.Value || !__state.triggered)
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
			EFT.BotMemory memory = owner.Memory;
			obj = ((memory != null) ? memory.GoalEnemy : null);
		}
		if (obj != __instance)
		{
			return;
		}
		BotOwner owner2 = __instance.Owner;
		IBotAiming val = ((owner2 != null) ? owner2.AimingManager.CurrentAiming : null);
		if (val == null)
		{
			return;
		}
		ThatsLitPlugin.swEncountering.MaybeResume();
		int num = __instance.Owner.Id % 10;
		BotOwner owner3 = __instance.Owner;
		WildSpawnType? obj2;
		if (owner3 == null)
		{
			obj2 = null;
		}
		else
		{
			Profile profile = owner3.Profile;
			if (profile == null)
			{
				obj2 = null;
			}
			else
			{
				ProfileInfo info = profile.Info;
				obj2 = ((info == null) ? ((WildSpawnType?)null) : info.Settings?.Role);
			}
		}
		BotImpactType botImpactType = Utility.GetBotImpactType((WildSpawnType)(obj2 ?? (WildSpawnType)1));
		float num2 = Random.Range(0f, 1f);
		num2 *= num2;
		float num3 = Random.Range(0f, 1f);
		num3 *= num3;
		Vector3 velocity;
		if (__state.botSprinting)
		{
			val.SetNextAimingDelay(((float)num * 0.01f + num2 * (0.25f + (float)num * 0.01f)) * (__state.unexpected ? 1f : 0.5f) * ((float)num * 0.01f + Mathf.InverseLerp(0f, 25f, __state.visionDeviation)) * botImpactType switch
			{
				BotImpactType.FOLLOWER => 0.5f, 
				BotImpactType.BOSS => 0.25f, 
				_ => 1f, 
			});
			float num4 = num3;
			float num5 = 0.225f * (__state.unexpected ? 1f : 0.5f) * Mathf.InverseLerp(0f, 30f, __state.visionDeviation);
			IPlayer person = __instance.Person;
			float num6;
			if (person == null)
			{
				num6 = 0f;
			}
			else
			{
				velocity = person.Velocity;
				num6 = velocity.magnitude;
			}
			if (num4 < num5 + 0.2f * Mathf.InverseLerp(0f, 5f, num6))
			{
				val.NextShotMiss(1);
			}
		}
		else if (__state.unexpected)
		{
			val.SetNextAimingDelay(num2 * (0.18f + (float)num * 0.01f) * Mathf.InverseLerp(0f, 25f, __state.visionDeviation) * botImpactType switch
			{
				BotImpactType.FOLLOWER => 0.5f, 
				BotImpactType.BOSS => 0.25f, 
				_ => 1f, 
			});
			float num7 = num3;
			float num8 = 0.225f * Mathf.InverseLerp(0f, 40f, __state.visionDeviation);
			IPlayer person2 = __instance.Person;
			float num9;
			if (person2 == null)
			{
				num9 = 0f;
			}
			else
			{
				velocity = person2.Velocity;
				num9 = velocity.magnitude;
			}
			if (num7 < num8 + 0.2f * Mathf.InverseLerp(0f, 5f, num9))
			{
				val.NextShotMiss(1);
			}
		}
		ThatsLitPlugin.swEncountering.Stop();
	}
}
