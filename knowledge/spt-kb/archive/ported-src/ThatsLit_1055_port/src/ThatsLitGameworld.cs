using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Comfort.Common;
using EFT;
using EFT.Weather;
using GPUInstancer;
using HarmonyLib;
using ThatsLit.Helpers;
using UnityEngine;

namespace ThatsLit;

public class ThatsLitGameworld : MonoBehaviour
{
	[StructLayout(LayoutKind.Sequential, Size = 1)]
	internal struct DoubleIdThrottler
	{
	}

	internal struct SingleIdThrottler
	{
		internal float lastAddedDangerPoint;

		internal float lastSideLook;

		internal float lastForceLook;
	}

	public RaidSettings activeRaidSettings;

	internal bool foliageUnavailable;

	internal bool terrainDetailsUnavailable;

	internal Dictionary<(string, string), DoubleIdThrottler> doubleIdThrottlers;

	internal Dictionary<string, SingleIdThrottler> singleIdThrottlers;

	private LayerMask foliageLayerMask;

	private Dictionary<Terrain, GPUInstancer.GPUInstancerSpatialPartitioningData<GPUInstancer.GPUInstancerCell>> terrainSpatialPartitions = new Dictionary<Terrain, GPUInstancer.GPUInstancerSpatialPartitioningData<GPUInstancer.GPUInstancerCell>>();

	private Dictionary<Terrain, List<int[,]>> terrainDetailMaps = new Dictionary<Terrain, List<int[,]>>();

	private Coroutine gatheringDetailMap;

	public GameWorld GameWorld => Singleton<GameWorld>.Instance;

	public ThatsLitPlayer MainThatsLitPlayer { get; private set; }

	public Dictionary<IPlayer, ThatsLitPlayer> AllThatsLitPlayers { get; private set; }

	public ScoreCalculator ScoreCalculator { get; internal set; }

	public bool IsWinter { get; private set; }

	internal int MaxDetailTypes { get; set; }

	private IEnumerable<int> IterateDetailIndex3x3N => IterateIndex3x3In5x5(0, 1);

	private IEnumerable<int> IterateDetailIndex3x3E => IterateIndex3x3In5x5(1, 0);

	private IEnumerable<int> IterateDetailIndex3x3W => IterateIndex3x3In5x5(-1, 0);

	private IEnumerable<int> IterateDetailIndex3x3S => IterateIndex3x3In5x5(0, -1);

	private IEnumerable<int> IterateDetailIndex3x3NE => IterateIndex3x3In5x5(1, 1);

	private IEnumerable<int> IterateDetailIndex3x3NW => IterateIndex3x3In5x5(-1, 1);

	private IEnumerable<int> IterateDetailIndex3x3SE => IterateIndex3x3In5x5(1, -1);

	private IEnumerable<int> IterateDetailIndex3x3SW => IterateIndex3x3In5x5(-1, -1);

	private IEnumerable<int> IterateDetailIndex2x3InversedTN => IterateIndexCrossShape3x3In5x5Center(horizontal: true, vertical: false, N: true);

	private IEnumerable<int> IterateDetailIndex2x3InversedTE => IterateIndexCrossShape3x3In5x5Center(horizontal: false, vertical: true, N: false, E: false, S: false, W: true);

	private IEnumerable<int> IterateDetailIndex2x3InversedTW => IterateIndexCrossShape3x3In5x5Center(horizontal: false, vertical: true, N: false, E: false, S: false, W: true);

	private IEnumerable<int> IterateDetailIndex2x3InversedTS => IterateIndexCrossShape3x3In5x5Center(horizontal: true, vertical: false, N: false, E: false, S: true);

	private IEnumerable<int> IterateDetailIndex2x2NE => IterateIndex2x2In5x5(2, 1);

	private IEnumerable<int> IterateDetailIndex2x2NW => IterateIndex2x2In5x5(1, 1);

	private IEnumerable<int> IterateDetailIndex2x2SE => IterateIndex2x2In5x5(2, 2);

	private IEnumerable<int> IterateDetailIndex2x2SW => IterateIndex2x2In5x5(1, 2);

	private IEnumerable<int> IterateDetailIndex3x3 => IterateIndex3x3In5x5(0, 0);

	private void Update()
	{
		if (!Singleton<GameWorld>.Instantiated || !ThatsLitPlayer.CanLoad())
		{
			return;
		}
		for (int i = 0; i < GameWorld.AllAlivePlayersList.Count; i++)
		{
			Player val = GameWorld.AllAlivePlayersList[i];
			if (!val.IsAI && !AllThatsLitPlayers.ContainsKey((IPlayer)(object)val))
			{
				TrySetupPlayer(val);
			}
		}
	}

	internal void TrySetupPlayer(Player player)
	{
		if (ThatsLitAPI.ShouldSetupPlayer == null || ThatsLitAPI.ShouldSetupPlayer(player))
		{
			ThatsLitPlayer thatsLitPlayer = ((Component)player).gameObject.AddComponent<ThatsLitPlayer>();
			AllThatsLitPlayers.Add((IPlayer)(object)player, thatsLitPlayer);
			thatsLitPlayer.Player = player;
			ThatsLitAPI.OnBeforePlayerSetup?.Invoke(player);
			ThatsLitAPI.OnBeforePlayerSetupDirect?.Invoke(thatsLitPlayer);
			if (ThatsLitPlugin.DebugProxy.Value)
			{
				ThatsLitAPI.ToggleBrightnessProxyDirect(thatsLitPlayer, toggle: true);
			}
			thatsLitPlayer.Setup(this);
			if (((IPlayer)player).IsYourPlayer)
			{
				MainThatsLitPlayer = thatsLitPlayer;
			}
		}
	}

	private void OnDestroy()
	{
		try
		{
			foreach (KeyValuePair<IPlayer, ThatsLitPlayer> allThatsLitPlayer in AllThatsLitPlayers)
			{
				ComponentHelpers.DestroyComponent<ThatsLitPlayer>(allThatsLitPlayer.Value);
			}
		}
		catch
		{
			Logger.LogError("Dispose Component Error");
		}
		if ((Object)(object)Singleton<ThatsLitGameworld>.Instance == (Object)(object)this)
		{
			Singleton<ThatsLitGameworld>.Instance = null;
		}
		ThatsLitAPI.OnGameWorldDestroyed?.Invoke();
	}

	private void Awake()
	{
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Invalid comparison between Unknown and I4
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b0: Expected O, but got Unknown
		foliageLayerMask = (int)((1 << LayerMask.NameToLayer("Foliage")) | (1 << LayerMask.NameToLayer("Grass")) | (1 << LayerMask.NameToLayer("PlayerSpiritAura")));
		singleIdThrottlers = new Dictionary<string, SingleIdThrottler>();
		doubleIdThrottlers = new Dictionary<(string, string), DoubleIdThrottler>();
		Singleton<ThatsLitGameworld>.Instance = this;
		TarkovApplication val = (TarkovApplication)Singleton<EFT.ClientApplication<EFT.IEftSession>>.Instance;
		if ((Object)(object)val == (Object)null)
		{
			throw new Exception("No session!");
		}
		IsWinter = (int)((EFT.ClientApplication<EFT.IEftSession>)(object)val).Session.Season == 2;
		activeRaidSettings = (RaidSettings)typeof(TarkovApplication).GetField("_raidSettings", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(val);
		string text = activeRaidSettings?.LocationId;
		switch (text)
		{
		default:
			if (text != null)
			{
				foliageUnavailable = false;
				terrainDetailsUnavailable = false;
				break;
			}
			goto case "factory4_night";
		case "factory4_night":
		case "factory4_day":
		case "laboratory":
			foliageUnavailable = true;
			terrainDetailsUnavailable = true;
			break;
		}
		AllThatsLitPlayers = new Dictionary<IPlayer, ThatsLitPlayer>();
		switch (activeRaidSettings?.LocationId)
		{
		case "Lighthouse":
			if (ThatsLitPlugin.EnableLighthouse.Value)
			{
				ScoreCalculator = new LighthouseScoreCalculator();
			}
			break;
		case "Woods":
			if (ThatsLitPlugin.EnableWoods.Value)
			{
				ScoreCalculator = new WoodsScoreCalculator();
			}
			break;
		case "factory4_night":
			if (ThatsLitPlugin.EnableFactoryNight.Value)
			{
				ScoreCalculator = new NightFactoryScoreCalculator();
			}
			break;
		case "factory4_day":
			ScoreCalculator = null;
			break;
		case "bigmap":
			if (ThatsLitPlugin.EnableCustoms.Value)
			{
				ScoreCalculator = new CustomsScoreCalculator();
			}
			break;
		case "RezervBase":
			if (ThatsLitPlugin.EnableReserve.Value)
			{
				ScoreCalculator = new ReserveScoreCalculator();
			}
			break;
		case "Interchange":
			if (ThatsLitPlugin.EnableInterchange.Value)
			{
				ScoreCalculator = new InterchangeScoreCalculator();
			}
			break;
		case "TarkovStreets":
			if (ThatsLitPlugin.EnableStreets.Value)
			{
				ScoreCalculator = new StreetsScoreCalculator();
			}
			break;
		case "Sandbox_high":
		case "Sandbox":
			if (ThatsLitPlugin.EnableGroundZero.Value)
			{
				ScoreCalculator = new GroundZeroScoreCalculator();
			}
			break;
		case "Shoreline":
			if (ThatsLitPlugin.EnableShoreline.Value)
			{
				ScoreCalculator = new ShorelineScoreCalculator();
			}
			break;
		case null:
			if (ThatsLitPlugin.EnableHideout.Value)
			{
				ScoreCalculator = new HideoutScoreCalculator();
			}
			break;
		}
		ThatsLitAPI.OnGameWorldSetup?.Invoke(this);
	}

	internal void GetWeatherStats(out float fog, out float rain, out float cloud)
	{
		WeatherController instance = WeatherController.Instance;
		if (((instance != null) ? instance.WeatherCurve : null) == null)
		{
			fog = (rain = (cloud = 0f));
			return;
		}
		fog = WeatherController.Instance.WeatherCurve.Fog;
		rain = WeatherController.Instance.WeatherCurve.Rain;
		cloud = WeatherController.Instance.WeatherCurve.Cloudiness;
	}

	internal void UpdateFoliageScore(Vector3 bodyPos, PlayerFoliageProfile player)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0102: Unknown result type (might be due to invalid IL or missing references)
		//IL_0297: Unknown result type (might be due to invalid IL or missing references)
		//IL_029e: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_02aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0266: Unknown result type (might be due to invalid IL or missing references)
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0274: Unknown result type (might be due to invalid IL or missing references)
		//IL_0279: Unknown result type (might be due to invalid IL or missing references)
		if (player == null || Time.time < player.LastCheckedTime + 0.45f)
		{
			return;
		}
		Vector3 val = bodyPos - player.LastCheckedPos;
		if (val.magnitude < 0.05f)
		{
			return;
		}
		player.FoliageScore = 0f;
		player.LastCheckedTime = Time.time;
		player.LastCheckedPos = bodyPos;
		player.FoliageCount = 0;
		Array.Clear(player.Foliage, 0, player.Foliage.Length);
		Array.Clear(player.CastedFoliageColliders, 0, player.CastedFoliageColliders.Length);
		if (foliageUnavailable)
		{
			return;
		}
		int num = Physics.OverlapSphereNonAlloc(bodyPos, 4f, player.CastedFoliageColliders, (int)(foliageLayerMask));
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			Collider val2 = player.CastedFoliageColliders[i];
			if (((Component)((Component)val2).gameObject.transform.root).gameObject.layer == 8 || (bool)(Object)(object)((Component)val2).gameObject.GetComponent<Terrain>())
			{
				continue;
			}
			Vector3 val3 = ((Component)val2).transform.position - bodyPos;
			float magnitude = val3.magnitude;
			if (magnitude < 0.25f)
			{
				player.FoliageScore += 1f;
			}
			else if (magnitude < 0.35f)
			{
				player.FoliageScore += 0.9f;
			}
			else if (magnitude < 0.5f)
			{
				player.FoliageScore += 0.8f;
			}
			else if (magnitude < 0.6f)
			{
				player.FoliageScore += 0.7f;
			}
			else if (magnitude < 0.7f)
			{
				player.FoliageScore += 0.5f;
			}
			else if (magnitude < 1f)
			{
				player.FoliageScore += 0.3f;
			}
			else if (magnitude < 2f)
			{
				player.FoliageScore += 0.2f;
			}
			else
			{
				player.FoliageScore += 0.1f;
			}
			string text = ((val2 != null) ? ((Object)((Component)((Component)val2).transform.parent).gameObject).name : null);
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (ThatsLitPlugin.FoliageSamples.Value == 1 && (player.Foliage[0] == default(FoliageInfo) || magnitude < player.Foliage[0].dis))
				{
					player.Foliage[0] = new FoliageInfo(text, (Vector2)(new Vector3(val3.x, val3.z)), magnitude);
					num2 = 1;
				}
				else
				{
					player.Foliage[num2] = new FoliageInfo(text, (Vector2)(new Vector3(val3.x, val3.z)), magnitude);
					num2++;
				}
			}
		}
		for (int j = 0; j < num2; j++)
		{
			FoliageInfo foliageInfo = player.Foliage[j];
			foliageInfo.name = Regex.Replace(foliageInfo.name, "(.+?)\\s?(\\(\\d+\\))?", "$1");
			foliageInfo.dis = foliageInfo.dir.magnitude;
			player.Foliage[j] = foliageInfo;
		}
		player.IsFoliageSorted = false;
		if (player.Foliage.Length == 1 || num2 == 1)
		{
			player.IsFoliageSorted = true;
		}
		switch (num)
		{
		case 1:
			player.FoliageScore /= 3.3f;
			break;
		case 2:
			player.FoliageScore /= 2.8f;
			break;
		case 3:
			player.FoliageScore /= 2.3f;
			break;
		case 4:
			player.FoliageScore /= 1.8f;
			break;
		case 5:
		case 6:
			player.FoliageScore /= 1.2f;
			break;
		case 11:
		case 12:
		case 13:
			player.FoliageScore /= 1.15f;
			break;
		case 14:
		case 15:
		case 16:
			player.FoliageScore /= 1.25f;
			break;
		}
		player.FoliageCount = num2;
	}

	public TerrainDetailScore CalculateDetailScore(PlayerTerrainDetailsProfile player, Vector3 enemyDirection, float dis, float verticalAxisAngle)
	{
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0277: Unknown result type (might be due to invalid IL or missing references)
		//IL_027d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0283: Unknown result type (might be due to invalid IL or missing references)
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0291: Unknown result type (might be due to invalid IL or missing references)
		//IL_0293: Unknown result type (might be due to invalid IL or missing references)
		//IL_0298: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		int num = 5;
		IEnumerable<int> enumerable = null;
		TerrainDetailScore cache = default(TerrainDetailScore);
		float num2 = 1f;
		if (player?.Details5x5 == null)
		{
			return cache;
		}
		Vector2 val;
		if (enemyDirection == Vector3.zero)
		{
			if (TryGetCache(num = 5, out cache))
			{
				return cache;
			}
			enumerable = IterateDetailIndex3x3;
		}
		else if (verticalAxisAngle < -20f)
		{
			if (dis >= 10f)
			{
				if (TryGetCache(num = 5, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3;
			}
			else
			{
				if (TryGetCache(num = 15, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3;
				num2 = 4f / 9f;
			}
		}
		else if (dis < 10f && verticalAxisAngle < -10f)
		{
			val = new Vector2(enemyDirection.x, enemyDirection.z);
			Vector2 normalized = val.normalized;
			float num3 = Vector2.SignedAngle(Vector2.up, normalized);
			if (num3 >= -22.5f && num3 <= 22.5f)
			{
				if (TryGetCache(num = 18, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex2x3InversedTN;
			}
			else if (num3 >= 22.5f && num3 <= 67.5f)
			{
				if (TryGetCache(num = 19, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex2x2NE;
			}
			else if (num3 >= 67.5f && num3 <= 112.5f)
			{
				if (TryGetCache(num = 16, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex2x3InversedTE;
			}
			else if (num3 >= 112.5f && num3 <= 157.5f)
			{
				if (TryGetCache(num = 13, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex2x2SE;
			}
			else if ((num3 >= 157.5f && num3 <= 180f) || (num3 >= -180f && num3 <= -157.5f))
			{
				if (TryGetCache(num = 12, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex2x3InversedTS;
			}
			else if (num3 >= -157.5f && num3 <= -112.5f)
			{
				if (TryGetCache(num = 11, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex2x2SW;
			}
			else if (num3 >= -112.5f && num3 <= -67.5f)
			{
				if (TryGetCache(num = 14, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex2x3InversedTW;
			}
			else if (num3 >= -67.5f && num3 <= -22.5f)
			{
				if (TryGetCache(num = 17, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex2x2NW;
			}
			num2 = 2.25f;
		}
		else
		{
			val = new Vector2(enemyDirection.x, enemyDirection.z);
			Vector2 normalized2 = val.normalized;
			float num4 = Vector2.SignedAngle(Vector2.up, normalized2);
			if (num4 >= -22.5f && num4 <= 22.5f)
			{
				if (TryGetCache(num = 8, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3N;
			}
			else if (num4 >= 22.5f && num4 <= 67.5f)
			{
				if (TryGetCache(num = 9, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3NE;
			}
			else if (num4 >= 67.5f && num4 <= 112.5f)
			{
				if (TryGetCache(num = 6, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3E;
			}
			else if (num4 >= 112.5f && num4 <= 157.5f)
			{
				if (TryGetCache(num = 3, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3SE;
			}
			else if ((num4 >= 157.5f && num4 <= 180f) || (num4 >= -180f && num4 <= -157.5f))
			{
				if (TryGetCache(num = 2, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3S;
			}
			else if (num4 >= -157.5f && num4 <= -112.5f)
			{
				if (TryGetCache(num = 1, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3SW;
			}
			else if (num4 >= -112.5f && num4 <= -67.5f)
			{
				if (TryGetCache(num = 4, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3W;
			}
			else
			{
				if (!(num4 >= -67.5f) || !(num4 <= -22.5f))
				{
					throw new Exception($"[That's Lit] Invalid angle to enemy: {num4}");
				}
				if (TryGetCache(num = 7, out cache))
				{
					return cache;
				}
				enumerable = IterateDetailIndex3x3NW;
			}
		}
		foreach (int item in enumerable)
		{
			for (int i = 0; i < MaxDetailTypes; i++)
			{
				CastedDetailInfo castedDetailInfo = player.Details5x5[item * MaxDetailTypes + i];
				if (castedDetailInfo.casted)
				{
					Utility.CalculateDetailScore(castedDetailInfo.name, castedDetailInfo.count, out var prone, out var crouch);
					prone *= num2;
					crouch *= num2;
					cache.prone += prone;
					cache.regular += crouch;
				}
			}
		}
		if (num < 0 || num >= player.detailScoreCache.Length)
		{
			Logger.LogWarning($"[That's Lit] detailScoreCache index {num} out of range (max {player.detailScoreCache.Length})");
			return cache;
		}
		cache.cached = true;
		player.detailScoreCache[num] = cache;
		return cache;
		bool TryGetCache(int index, out TerrainDetailScore reference)
		{
			reference = player.detailScoreCache[index];
			return reference.cached;
		}
	}

	public TerrainDetailScore CalculateCenterDetailScore(PlayerTerrainDetailsProfile player, bool unscaled = false)
	{
		TerrainDetailScore cache = default(TerrainDetailScore);
		float num = (unscaled ? 1f : 9f);
		if (TryGetCache(0, out cache))
		{
			return cache;
		}
		if (player?.Details5x5 == null)
		{
			return cache;
		}
		for (int i = 0; i < MaxDetailTypes; i++)
		{
			CastedDetailInfo castedDetailInfo = player.Details5x5[12 * MaxDetailTypes + i];
			if (castedDetailInfo.casted)
			{
				Utility.CalculateDetailScore(castedDetailInfo.name, castedDetailInfo.count, out var prone, out var crouch);
				prone *= num;
				crouch *= num;
				cache.prone += prone;
				cache.regular += crouch;
			}
		}
		if (player.detailScoreCache == null || player.detailScoreCache.Length == 0)
		{
			Logger.LogWarning("[That's Lit] detailScoreCache is null or empty");
			return cache;
		}
		cache.cached = true;
		player.detailScoreCache[0] = cache;
		return cache;
		bool TryGetCache(int index, out TerrainDetailScore reference)
		{
			reference = player.detailScoreCache[index];
			return reference.cached;
		}
	}

	private IEnumerable<int> IterateIndex3x3In5x5(int xOffset, int yOffset)
	{
		yield return 5 * (1 + yOffset) + 1 + xOffset;
		yield return 5 * (1 + yOffset) + 2 + xOffset;
		yield return 5 * (1 + yOffset) + 3 + xOffset;
		yield return 5 * (2 + yOffset) + 1 + xOffset;
		yield return 5 * (2 + yOffset) + 2 + xOffset;
		yield return 5 * (2 + yOffset) + 3 + xOffset;
		yield return 5 * (3 + yOffset) + 1 + xOffset;
		yield return 5 * (3 + yOffset) + 2 + xOffset;
		yield return 5 * (3 + yOffset) + 3 + xOffset;
	}

	private IEnumerable<int> IterateIndex2x2In5x5(int xOffset, int yOffset)
	{
		if (xOffset > 3 || yOffset > 3)
		{
			throw new Exception("[That's Lit] Terrain detail grid access out of Bound");
		}
		yield return 5 * yOffset + xOffset;
		yield return 5 * yOffset + xOffset + 1;
		yield return 5 * (yOffset + 1) + xOffset;
		yield return 5 * (yOffset + 1) + xOffset + 1;
	}

	private IEnumerable<int> IterateIndexCrossShape3x3In5x5Center(bool horizontal = false, bool vertical = false, bool N = false, bool E = false, bool S = false, bool W = false)
	{
		if (vertical || N)
		{
			yield return 7;
		}
		if (horizontal || W)
		{
			yield return 11;
		}
		if (horizontal || vertical)
		{
			yield return 12;
		}
		if (horizontal || E)
		{
			yield return 13;
		}
		if (vertical || S)
		{
			yield return 17;
		}
	}

	internal void CheckTerrainDetails(Vector3 position, PlayerTerrainDetailsProfile player)
	{
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Unknown result type (might be due to invalid IL or missing references)
		//IL_015c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0160: Unknown result type (might be due to invalid IL or missing references)
		//IL_0165: Unknown result type (might be due to invalid IL or missing references)
		//IL_016a: Unknown result type (might be due to invalid IL or missing references)
		//IL_016f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0173: Unknown result type (might be due to invalid IL or missing references)
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_018b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a9: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b5: Unknown result type (might be due to invalid IL or missing references)
		if (!terrainDetailsUnavailable)
		{
			List<GPUInstancerManager> activeManagerList = GPUInstancerManager.activeManagerList;
			if (activeManagerList != null && activeManagerList.Count == 0)
			{
				terrainDetailsUnavailable = true;
				Logger.LogInfo("Active detail managers not found, disabling detail check...");
				return;
			}
		}
		if (player == null || terrainDetailsUnavailable)
		{
			return;
		}
		Vector3 val = position - player.LastCheckedPos;
		if (val.magnitude < 0.1f)
		{
			return;
		}
		Array.Clear(player.detailScoreCache, 0, player.detailScoreCache.Length);
		if (player.Details5x5 != null)
		{
			Array.Clear(player.Details5x5, 0, player.Details5x5.Length);
		}
		player.RecentDetailCount3x3 = 0;
		player.RecentDetailCount5x5 = 0;
		player.LastCheckedTime = Time.time;
		player.LastCheckedPos = position;
		RaycastHit val2 = default(RaycastHit);
		if (!Physics.Raycast(new Ray(position, Vector3.down), out val2, 100f, (int)(LayersMaskController.TerrainMask)))
		{
			return;
		}
		Transform transform = val2.transform;
		Terrain val3 = ((transform != null) ? ((Component)transform).GetComponent<Terrain>() : null);
		GPUInstancerDetailManager val4 = ((val3 == null) ? null : ((Component)val3).GetComponent<GPUInstancerTerrainProxy>()?.detailManager);
		if (!(bool)((Object)(object)val3) || !(bool)((Object)(object)val4) || !((GPUInstancerManager)val4).isInitialized)
		{
			return;
		}
		if (!terrainDetailMaps.TryGetValue(val3, out var value))
		{
			if (gatheringDetailMap == null)
			{
				gatheringDetailMap = ((MonoBehaviour)this).StartCoroutine(BuildAllTerrainDetailMapCoroutine(val3));
			}
			return;
		}
		Vector3 point = val2.point;
		Vector3 position2 = ((Component)val3).transform.position;
		Bounds bounds = val3.terrainData.bounds;
		Vector3 val5 = point - (position2 + bounds.min);
		Vector2 val6 = default(Vector2);
		val6 = new Vector2(val5.x / val3.terrainData.size.x, val5.z / val3.terrainData.size.z);
		if (player.Details5x5 == null)
		{
			foreach (GPUInstancerManager activeManager in GPUInstancerManager.activeManagerList)
			{
				if (MaxDetailTypes < activeManager.prototypeList.Count)
				{
					MaxDetailTypes = activeManager.prototypeList.Count + 2;
				}
			}
			player.Details5x5 = new CastedDetailInfo[MaxDetailTypes * 5 * 5];
			Logger.LogInfo($"Set MaxDetailTypes to {MaxDetailTypes}");
		}
		if (MaxDetailTypes < ((GPUInstancerManager)val4).prototypeList.Count)
		{
			MaxDetailTypes = ((GPUInstancerManager)val4).prototypeList.Count + 2;
			player.Details5x5 = new CastedDetailInfo[MaxDetailTypes * 5 * 5];
		}
		Vector2Int val8 = default(Vector2Int);
		for (int i = 0; i < ((GPUInstancerManager)val4).prototypeList.Count; i++)
		{
			GPUInstancerPrototype obj = ((GPUInstancerManager)val4).prototypeList[i];
			GPUInstancerDetailPrototype val7 = (GPUInstancerDetailPrototype)(object)((obj is GPUInstancerDetailPrototype) ? obj : null);
			if ((Object)(object)val7 == (Object)null)
			{
				continue;
			}
			int detailResolution = val7.detailResolution;
			val8 = new Vector2Int((int)(val6.x * (float)detailResolution), (int)(val6.y * (float)detailResolution));
			for (int j = 0; j < 5; j++)
			{
				for (int k = 0; k < 5; k++)
				{
					int num = val8.x - 2 + j;
					int num2 = val8.y - 2 + k;
					int num3 = 0;
					if (num < 0 && (bool)((Object)(object)val3.leftNeighbor) && num2 >= 0 && num2 < detailResolution)
					{
						Terrain leftNeighbor = val3.leftNeighbor;
						if (!terrainDetailMaps.TryGetValue(leftNeighbor, out var value2))
						{
							if (gatheringDetailMap == null)
							{
								gatheringDetailMap = ((MonoBehaviour)this).StartCoroutine(BuildAllTerrainDetailMapCoroutine(leftNeighbor));
							}
							else if (value2.Count > i)
							{
								num3 = value2[i][detailResolution + num, num2];
							}
						}
					}
					else if (num >= detailResolution && (bool)((Object)(object)val3.rightNeighbor) && num2 >= 0 && num2 < detailResolution)
					{
						Terrain rightNeighbor = val3.rightNeighbor;
						if (!terrainDetailMaps.TryGetValue(rightNeighbor, out var value3))
						{
							if (gatheringDetailMap == null)
							{
								gatheringDetailMap = ((MonoBehaviour)this).StartCoroutine(BuildAllTerrainDetailMapCoroutine(rightNeighbor));
							}
							else if (value3.Count > i)
							{
								num3 = value3[i][num - detailResolution, num2];
							}
						}
					}
					else if (num2 >= detailResolution && (bool)((Object)(object)val3.topNeighbor) && num >= 0 && num < detailResolution)
					{
						Terrain topNeighbor = val3.topNeighbor;
						if (!terrainDetailMaps.TryGetValue(topNeighbor, out var value4))
						{
							if (gatheringDetailMap == null)
							{
								gatheringDetailMap = ((MonoBehaviour)this).StartCoroutine(BuildAllTerrainDetailMapCoroutine(topNeighbor));
							}
							else if (value4.Count > i)
							{
								num3 = value4[i][num, num2 - detailResolution];
							}
						}
					}
					else if (num2 < 0 && (bool)((Object)(object)val3.bottomNeighbor) && num >= 0 && num < detailResolution)
					{
						Terrain bottomNeighbor = val3.bottomNeighbor;
						if (!terrainDetailMaps.TryGetValue(bottomNeighbor, out var value5))
						{
							if (gatheringDetailMap == null)
							{
								gatheringDetailMap = ((MonoBehaviour)this).StartCoroutine(BuildAllTerrainDetailMapCoroutine(bottomNeighbor));
							}
							else if (value5.Count > i)
							{
								num3 = value5[i][num, num2 + detailResolution];
							}
						}
					}
					else if (num2 >= detailResolution && num >= detailResolution && (Object)(object)val3.topNeighbor != (Object)null && (Object)(object)val3.topNeighbor.rightNeighbor != (Object)null)
					{
						Terrain rightNeighbor2 = val3.topNeighbor.rightNeighbor;
						if (!terrainDetailMaps.TryGetValue(rightNeighbor2, out var value6))
						{
							if (gatheringDetailMap == null)
							{
								gatheringDetailMap = ((MonoBehaviour)this).StartCoroutine(BuildAllTerrainDetailMapCoroutine(rightNeighbor2));
							}
							else if (value6.Count > i)
							{
								num3 = value6[i][num - detailResolution, num2 - detailResolution];
							}
						}
					}
					else if (num2 >= detailResolution && num < 0 && (Object)(object)val3.topNeighbor != (Object)null && (Object)(object)val3.topNeighbor.leftNeighbor != (Object)null)
					{
						Terrain leftNeighbor2 = val3.topNeighbor.leftNeighbor;
						if (!terrainDetailMaps.TryGetValue(leftNeighbor2, out var value7))
						{
							if (gatheringDetailMap == null)
							{
								gatheringDetailMap = ((MonoBehaviour)this).StartCoroutine(BuildAllTerrainDetailMapCoroutine(leftNeighbor2));
							}
							else if (value7.Count > i)
							{
								num3 = value7[i][num + detailResolution, num2 - detailResolution];
							}
						}
					}
					else if (num2 < 0 && num >= detailResolution && (Object)(object)val3.bottomNeighbor != (Object)null && (Object)(object)val3.bottomNeighbor.rightNeighbor != (Object)null)
					{
						Terrain rightNeighbor3 = val3.bottomNeighbor.rightNeighbor;
						if (!terrainDetailMaps.TryGetValue(rightNeighbor3, out var value8))
						{
							if (gatheringDetailMap == null)
							{
								gatheringDetailMap = ((MonoBehaviour)this).StartCoroutine(BuildAllTerrainDetailMapCoroutine(rightNeighbor3));
							}
							else if (value8.Count > i)
							{
								num3 = value8[i][num - detailResolution, num2 + detailResolution];
							}
						}
					}
					else if (num2 < 0 && num < 0 && (Object)(object)val3.bottomNeighbor != (Object)null && (Object)(object)val3.bottomNeighbor.leftNeighbor != (Object)null)
					{
						Terrain leftNeighbor3 = val3.bottomNeighbor.leftNeighbor;
						if (!terrainDetailMaps.TryGetValue(leftNeighbor3, out var value9))
						{
							if (gatheringDetailMap == null)
							{
								gatheringDetailMap = ((MonoBehaviour)this).StartCoroutine(BuildAllTerrainDetailMapCoroutine(leftNeighbor3));
							}
							else if (value9.Count > i)
							{
								num3 = value9[i][num + detailResolution, num2 + detailResolution];
							}
						}
					}
					else if (value.Count > i)
					{
						num3 = value[i][num, num2];
					}
					player.Details5x5[player.GetDetailInfoIndex(j, k, i, MaxDetailTypes)] = new CastedDetailInfo
					{
						casted = true,
						name = ((Object)((GPUInstancerManager)val4).prototypeList[i]).name,
						count = num3
					};
					if (j >= 1 && j <= 3 && k >= 1 && k <= 3)
					{
						player.RecentDetailCount3x3 += num3;
					}
					player.RecentDetailCount5x5 += num3;
				}
			}
		}
	}

	private IEnumerator BuildAllTerrainDetailMapCoroutine(Terrain priority = null)
	{
		yield return (object)new WaitForSeconds(1f);
		Logger.LogInfo($"[{activeRaidSettings.LocationId}] Starting building terrain detail maps at {Time.time}...");
		bool allDisabled = true;
		GPUInstancerDetailManager val = ((Component)priority).GetComponent<GPUInstancerTerrainProxy>()?.detailManager;
		if ((Object)(object)val != (Object)null && ((Behaviour)val).enabled)
		{
			allDisabled = false;
			if (!terrainDetailMaps.ContainsKey(priority))
			{
				terrainDetailMaps[priority] = new List<int[,]>(((GPUInstancerManager)val).prototypeList.Count);
				yield return BuildTerrainDetailMapCoroutine(priority, terrainDetailMaps[priority]);
			}
		}
		else
		{
			terrainDetailMaps[priority] = null;
		}
		Terrain[] activeTerrains = Terrain.activeTerrains;
		foreach (Terrain val2 in activeTerrains)
		{
			val = ((Component)val2).GetComponent<GPUInstancerTerrainProxy>()?.detailManager;
			if ((Object)(object)val != (Object)null && ((Behaviour)val).enabled)
			{
				allDisabled = false;
				if (!terrainDetailMaps.ContainsKey(val2))
				{
					terrainDetailMaps[val2] = new List<int[,]>(((GPUInstancerManager)val).prototypeList.Count);
					yield return BuildTerrainDetailMapCoroutine(val2, terrainDetailMaps[val2]);
				}
			}
			else
			{
				terrainDetailMaps[val2] = null;
			}
		}
		if (allDisabled)
		{
			terrainDetailsUnavailable = true;
		}
		Logger.LogInfo($"[{activeRaidSettings.LocationId}] Finished building terrain detail maps at {Time.time}... (AllDisabled: {allDisabled})");
	}

	private IEnumerator BuildTerrainDetailMapCoroutine(Terrain terrain, List<int[,]> detailMapData)
	{
		GPUInstancerDetailManager mgr = ((Component)terrain).GetComponent<GPUInstancerTerrainProxy>()?.detailManager;
		if ((Object)(object)mgr == (Object)null || !((GPUInstancerManager)mgr).isInitialized)
		{
			yield break;
		}
		float time = Time.time;
		Logger.LogInfo($"[{activeRaidSettings.LocationId}] Starting building detail map of {((Object)terrain).name} at {time}...");
		if (!terrainSpatialPartitions.TryGetValue(terrain, out var spData))
		{
			GPUInstancer.GPUInstancerSpatialPartitioningData<GPUInstancer.GPUInstancerCell> val = (terrainSpatialPartitions[terrain] = AccessTools.Field(typeof(GPUInstancerDetailManager), "spData").GetValue(mgr) as GPUInstancer.GPUInstancerSpatialPartitioningData<GPUInstancer.GPUInstancerCell>);
			spData = val;
		}
		if (spData == null)
		{
			terrainSpatialPartitions.Remove(terrain);
		}
		WaitForEndOfFrame waitNextFrame = new WaitForEndOfFrame();
		if (detailMapData == null)
		{
			detailMapData = new List<int[,]>(((GPUInstancerManager)mgr).prototypeList.Count);
		}
		else
		{
			detailMapData.Clear();
		}
		int layer = 0;
		GPUInstancer.GPUInstancerCell val4 = default(GPUInstancer.GPUInstancerCell);
		while (layer < ((GPUInstancerManager)mgr).prototypeList.Count)
		{
			GPUInstancerPrototype obj = ((GPUInstancerManager)mgr).prototypeList[layer];
			GPUInstancerDetailPrototype val3 = (GPUInstancerDetailPrototype)(object)((obj is GPUInstancerDetailPrototype) ? obj : null);
			if ((Object)(object)val3 == (Object)null)
			{
				detailMapData.Add(null);
			}
			int[,] detailLayer = new int[val3.detailResolution, val3.detailResolution];
			detailMapData.Add(detailLayer);
			int resolutionPerCell = val3.detailResolution / spData.cellRowAndCollumnCountPerTerrain;
			int num;
			for (int terrainCellX = 0; terrainCellX < spData.cellRowAndCollumnCountPerTerrain; terrainCellX = num)
			{
				for (int terrainCellY = 0; terrainCellY < spData.cellRowAndCollumnCountPerTerrain; terrainCellY = num)
				{
					if (spData.GetCell(GPUInstancer.GPUInstancerCell.CalculateHash(terrainCellX, 0, terrainCellY), out val4))
					{
						GPUInstancer.GPUInstancerDetailCell val5 = (GPUInstancer.GPUInstancerDetailCell)val4;
						if (val5.detailMapData != null)
						{
							for (int i = 0; i < resolutionPerCell; i++)
							{
								for (int j = 0; j < resolutionPerCell; j++)
								{
									detailLayer[i + terrainCellX * resolutionPerCell, j + terrainCellY * resolutionPerCell] = val5.detailMapData[layer][i + j * resolutionPerCell];
								}
							}
						}
					}
					yield return waitNextFrame;
					num = terrainCellY + 1;
				}
				num = terrainCellX + 1;
			}
			num = layer + 1;
			layer = num;
		}
		Logger.LogInfo($"[{activeRaidSettings.LocationId}] Finished building detail map of {((Object)terrain).name} at {Time.time}... Costed {Time.time - time}");
	}
}
