using System;
using Comfort.Common;
using EFT;
using UnityEngine;

namespace ThatsLit;

public static class ThatsLitAPI
{
	public static Action<ThatsLitPlayer> OnBeforePlayerSetupDirect;

	public static Action<Player> OnBeforePlayerSetup;

	public static Func<Player, bool> ShouldSetupPlayer;

	public static Action<ThatsLitGameworld> OnGameWorldSetup;

	public static Action OnGameWorldDestroyed;

	public static Action OnMainPlayerGUI;

	public static Action<ThatsLitPlayer, float, float> OnPlayerBrightnessScoreCalculatedDirect;

	public static Action<Player, float, float> OnPlayerBrightnessScoreCalculated;

	public static Action<ThatsLitPlayer> OnPlayerSurroundingTerrainSampledDirect;

	public static Action<Player> OnPlayerSurroundingTerrainSampled;

	public static bool IsBrightnessProxyDirect(ThatsLitPlayer player)
	{
		return player.PlayerLitScoreProfile?.IsProxy ?? false;
	}

	public static bool IsBrightnessProxy(Player player)
	{
		ThatsLitGameworld instance = Singleton<ThatsLitGameworld>.Instance;
		ThatsLitPlayer value = default(ThatsLitPlayer);
		if (instance == null || instance.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)player, out value) != true)
		{
			return false;
		}
		return value.PlayerLitScoreProfile?.IsProxy ?? false;
	}

	public static void ToggleBrightnessProxyDirect(ThatsLitPlayer player, bool toggle)
	{
		if (player.PlayerLitScoreProfile == null)
		{
			player.PlayerLitScoreProfile = new PlayerLitScoreProfile(player);
		}
		if (player.PlayerLitScoreProfile.IsProxy != toggle)
		{
			player.ToggleBrightnessProxy(toggle);
		}
	}

	public static void ToggleBrightnessProxy(Player player, bool toggle)
	{
		ThatsLitGameworld instance = Singleton<ThatsLitGameworld>.Instance;
		ThatsLitPlayer value = default(ThatsLitPlayer);
		if (instance != null && instance.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)player, out value) == true)
		{
			ToggleBrightnessProxyDirect(value, toggle);
		}
	}

	public static void TrySetProxyBrightnessScoreDirect(ThatsLitPlayer player, float score, float ambienceScore)
	{
		PlayerLitScoreProfile playerLitScoreProfile = player.PlayerLitScoreProfile;
		if (playerLitScoreProfile != null && playerLitScoreProfile.IsProxy)
		{
			player.PlayerLitScoreProfile.frame0.multiFrameLitScore = score;
			player.PlayerLitScoreProfile.frame0.ambienceScore = ambienceScore;
		}
	}

	public static void TrySetProxyBrightnessScore(Player player, float score, float ambienceScore)
	{
		ThatsLitGameworld instance = Singleton<ThatsLitGameworld>.Instance;
		ThatsLitPlayer value = default(ThatsLitPlayer);
		if (instance != null && instance.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)player, out value) == true)
		{
			TrySetProxyBrightnessScoreDirect(value, score, ambienceScore);
		}
	}

	public static float GetBrightnessScore(Player player)
	{
		ThatsLitGameworld instance = Singleton<ThatsLitGameworld>.Instance;
		ThatsLitPlayer value = default(ThatsLitPlayer);
		if (instance == null || instance.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)player, out value) != true)
		{
			return 0f;
		}
		return GetBrightnessScoreDirect(value);
	}

	public static float GetAmbienceBrightnessScore(Player player)
	{
		ThatsLitGameworld instance = Singleton<ThatsLitGameworld>.Instance;
		ThatsLitPlayer value = default(ThatsLitPlayer);
		if (instance == null || instance.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)player, out value) != true)
		{
			return 0f;
		}
		return GetAmbienceBrightnessScoreDirect(value);
	}

	public static float GetBrightnessScoreDirect(ThatsLitPlayer player)
	{
		return player.PlayerLitScoreProfile?.frame0.multiFrameLitScore ?? 0f;
	}

	public static float GetAmbienceBrightnessScoreDirect(ThatsLitPlayer player)
	{
		return player.PlayerLitScoreProfile?.frame0.ambienceScore ?? 0f;
	}

	public static float GetFoliageScore(Player player)
	{
		ThatsLitGameworld instance = Singleton<ThatsLitGameworld>.Instance;
		ThatsLitPlayer value = default(ThatsLitPlayer);
		if (instance == null || instance.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)player, out value) != true)
		{
			return 0f;
		}
		return GetFoliageScoreDirect(value);
	}

	public static float GetFoliageScoreDirect(ThatsLitPlayer player)
	{
		return player.Foliage?.FoliageScore ?? 0f;
	}

	public static int GetTerrainDetailCount3x3Direct(ThatsLitPlayer player)
	{
		return player.TerrainDetails?.RecentDetailCount3x3 ?? 0;
	}

	public static int GetTerrainDetailCount5x5Direct(ThatsLitPlayer player)
	{
		return player.TerrainDetails?.RecentDetailCount5x5 ?? 0;
	}

	public static int GetTerrainDetailCount3x3(Player player)
	{
		ThatsLitGameworld instance = Singleton<ThatsLitGameworld>.Instance;
		ThatsLitPlayer value = default(ThatsLitPlayer);
		if (instance == null || instance.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)player, out value) != true)
		{
			return 0;
		}
		return GetTerrainDetailCount3x3Direct(value);
	}

	public static int GetTerrainDetailCount5x5(Player player)
	{
		ThatsLitGameworld instance = Singleton<ThatsLitGameworld>.Instance;
		ThatsLitPlayer value = default(ThatsLitPlayer);
		if (instance == null || instance.AllThatsLitPlayers?.TryGetValue((IPlayer)(object)player, out value) != true)
		{
			return 0;
		}
		return GetTerrainDetailCount5x5Direct(value);
	}

	public static float GetTerrainDetailScoreCenter3x3Direct(ThatsLitPlayer player)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		if (player.TerrainDetails == null || (Object)(object)player.Player == (Object)null)
		{
			return 0f;
		}
		TerrainDetailScore terrainScore = Singleton<ThatsLitGameworld>.Instance.CalculateDetailScore(player.TerrainDetails, Vector3.zero, 0f, 0f);
		if (player.Player.IsInPronePose)
		{
			return terrainScore.prone;
		}
		return Utility.GetPoseWeightedRegularTerrainScore(Utility.GetPoseFactor(player.Player.PoseLevel, player.Player.Physical.MaxPoseLevel, player.Player.IsInPronePose), terrainScore);
	}

	public static LightAndLaserState GetLightAndLaserStateDirect(ThatsLitPlayer player)
	{
		return player.LightAndLaserState;
	}

	public static bool AnyVisibleLight(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.AnyVisibleLight;
		}
		return false;
	}

	public static bool AnyVisibleLaser(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.AnyVisibleLaser;
		}
		return false;
	}

	public static bool AnyIRLight(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.AnyIRLight;
		}
		return false;
	}

	public static bool AnyIRLaser(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.AnyIRLaser;
		}
		return false;
	}

	public static bool AnyVisibleMain(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.AnyVisibleMain;
		}
		return false;
	}

	public static bool AnyVisibleSub(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.AnyVisibleSub;
		}
		return false;
	}

	public static bool AnyIRMain(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.AnyIRMain;
		}
		return false;
	}

	public static bool AnyIRSub(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.AnyIRSub;
		}
		return false;
	}

	public static float GetMainVisibleLight(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.deviceStateCache.light;
		}
		return 0f;
	}

	public static float GetMainVisibleLaser(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.deviceStateCache.laser;
		}
		return 0f;
	}

	public static float GetMainIRLight(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.deviceStateCache.irLight;
		}
		return 0f;
	}

	public static float GetMainIRLaser(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.deviceStateCache.irLaser;
		}
		return 0f;
	}

	public static float GetSheathedVisibleLight(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.deviceStateCacheSub.light;
		}
		return 0f;
	}

	public static float GetSheathedVisibleLaser(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.deviceStateCacheSub.laser;
		}
		return 0f;
	}

	public static float GetSheathedIRLight(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.deviceStateCacheSub.irLight;
		}
		return 0f;
	}

	public static float GetSheathedIRLaser(Player player)
	{
		if (Singleton<ThatsLitGameworld>.Instance.AllThatsLitPlayers.TryGetValue((IPlayer)(object)player, out var value))
		{
			return value.LightAndLaserState.deviceStateCacheSub.irLaser;
		}
		return 0f;
	}
}
