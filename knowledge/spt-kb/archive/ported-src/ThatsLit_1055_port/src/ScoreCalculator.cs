using Comfort.Common;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace ThatsLit;

public class ScoreCalculator
{
	private readonly int RESOLUTION = 32 * ThatsLitPlugin.ResLevel.Value;

	internal float sunLightScore;

	internal float moonLightScore;

	protected virtual float MinBaseAmbienceScore => -0.9f;

	protected virtual float MaxBaseAmbienceScore => -0.1f;

	protected virtual float NonCloudinessBaseAmbienceScoreImpact => 0.1f;

	protected virtual float BunkerBaseAmbienceTarget => -0.3f;

	protected virtual float MaxMoonlightScore => 0.25f;

	protected virtual float MaxSunlightScore => 0.25f;

	protected virtual float IndoorAmbienceCutoff => 0f;

	protected virtual float MinAmbienceLum => 0.01f;

	protected virtual float MaxAmbienceLum => 0.1f;

	protected virtual float PixelLumScoreScale => 1f;

	protected virtual float ThresholdShine => 0.8f;

	protected virtual float ThresholdHigh => 0.5f;

	protected virtual float ThresholdHighMid => 0.25f;

	protected virtual float ThresholdMid => 0.13f;

	protected virtual float ThresholdMidLow => 0.06f;

	protected virtual float ThresholdLow => 0.02f;

	protected virtual float ScoreShine => 0.2f;

	protected virtual float ScoreHigh => 0.2f;

	protected virtual float ScoreHighMid => 0.25f;

	protected virtual float ScoreMid => 0.2f;

	protected virtual float ScoreMidLow => 0.1f;

	protected virtual float ScoreLow => 0.05f;

	protected virtual float ScoreDark => 0f;

	protected virtual float MultiFrameContrastImpactScale => 1f;

	protected virtual float NightTerrainImpactScale => 0.2f;

	protected internal virtual void OnGUI(PlayerLitScoreProfile player, bool layout = false)
	{
	}

	public void PreCalculate(PlayerLitScoreProfile player, NativeArray<Color32> tex, float time)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		GetThresholds(time, out var thresholdShine, out var thresholdHigh, out var thresholdHighMid, out var thresholdMid, out var thresholdMidLow, out var thresholdLow);
		StartCountPixels(player, tex, thresholdShine, thresholdHigh, thresholdHighMid, thresholdMid, thresholdMidLow, thresholdLow);
	}

	public virtual float CalculateMultiFrameScore(float cloud, float fog, float rain, ThatsLitGameworld gameWorld, PlayerLitScoreProfile player, float time, string locationId)
	{
		//IL_0397: Unknown result type (might be due to invalid IL or missing references)
		player.frame5 = player.frame4;
		player.frame4 = player.frame3;
		player.frame3 = player.frame2;
		player.frame2 = player.frame1;
		player.frame1 = player.frame0;
		FrameStats frameStats = new FrameStats
		{
			cloudiness = cloud
		};
		CompleteCountPixels(player, out frameStats.pxS, out frameStats.pxH, out frameStats.pxHM, out frameStats.pxM, out frameStats.pxML, out frameStats.pxL, out frameStats.pxD, out frameStats.lum, out var _, out frameStats.pixels);
		if (player.IsProxy)
		{
			return 0f;
		}
		if (frameStats.pixels == 0)
		{
			frameStats.pixels = RESOLUTION * RESOLUTION;
		}
		frameStats.avgLum = frameStats.lum / (float)frameStats.pixels;
		frameStats.avgLumNonDark = frameStats.lum / (float)(frameStats.pixels - frameStats.pxD);
		frameStats.avgLumMultiFrames = (frameStats.avgLum + player.frame1.avgLum + player.frame2.avgLum + player.frame3.avgLum + player.frame4.avgLum + player.frame5.avgLum) / 6f;
		player.UpdateLumTrackers(frameStats.avgLumMultiFrames);
		float num = Time.time - player.Player.lastOutside;
		if (num < 0f)
		{
			num = 0f;
		}
		float num2 = Mathf.Clamp01(1f - num);
		float num3 = player.Player.AmbienceShadowFactor;
		if (gameWorld.IsWinter)
		{
			num3 *= 1f - 0.3f * num2;
		}
		float num4 = CalculateBaseAmbienceScore(locationId, time, cloud, player);
		if (gameWorld.IsWinter && num < 2f)
		{
			num4 = Mathf.Lerp(num4, 0.5f, 0.25f * Mathf.InverseLerp(0f, 1.2f, cloud) * Mathf.InverseLerp(1.2f, 0.2f, num));
		}
		num4 += (MinBaseAmbienceScore - num4) * player.Player.OverheadHaxRatingFactor * 0.2f * GetMapAmbienceCoef(locationId, time);
		float num5 = Mathf.Pow(player.Player.bunkerTimeClamped / 10f, 2f);
		num4 = Mathf.Lerp(num4, BunkerBaseAmbienceTarget, num5 * Mathf.Clamp01(num / 10f) * 0.65f);
		if (ThatsLitPlayer.IsDebugSampleFrame && player.Player.DebugInfo != null)
		{
			player.Player.DebugInfo.scoreRawBase = num4;
		}
		float num6 = num4;
		float num7 = Mathf.InverseLerp(2f, 9f, num);
		float num8 = Mathf.Lerp(cloud, 1.3f, num5);
		num6 += Mathf.Clamp01((num8 - 1f) / -2f) * NonCloudinessBaseAmbienceScoreImpact;
		moonLightScore = CalculateMoonLight(player, locationId, time, num8);
		sunLightScore = CalculateSunLight(player, locationId, time, num8);
		num6 += (moonLightScore + sunLightScore) * (1f - num3 - num7 * IndoorAmbienceCutoff * num3);
		if (gameWorld.IsWinter)
		{
			num6 *= 1f + 0.2f * Mathf.Clamp01((sunLightScore + moonLightScore) / 0.5f) * Mathf.InverseLerp(1.2f, 0.2f, num);
		}
		if (player.Player.TerrainDetails != null)
		{
			float prone = Singleton<ThatsLitGameworld>.Instance.CalculateDetailScore(player.Player.TerrainDetails, Vector3.zero, 0f, 0f).prone;
			float prone2 = Singleton<ThatsLitGameworld>.Instance.CalculateCenterDetailScore(player.Player.TerrainDetails).prone;
			float num9 = Mathf.Clamp01(prone) * 0.667f + Mathf.Clamp01(prone2) * 0.333f;
			num9 = Mathf.Clamp01(num9);
			num9 *= Mathf.Clamp01((0.6f * (float)player.Player.TerrainDetails.RecentDetailCount3x3 + 0.4f * (float)player.Player.TerrainDetails.RecentDetailCount5x5) / 60f);
			num9 *= num9;
			num9 *= Mathf.InverseLerp(-0.1f, MinBaseAmbienceScore, num6);
			num9 *= NightTerrainImpactScale;
			if (num9 > player.detailBonusSmooth)
			{
				player.detailBonusSmooth = Mathf.Lerp(player.detailBonusSmooth, num9, Time.fixedDeltaTime * 2.2f);
			}
			else if (num9 < player.detailBonusSmooth)
			{
				player.detailBonusSmooth = Mathf.Lerp(player.detailBonusSmooth, num9, Time.fixedDeltaTime * 6f);
			}
			if (gameWorld.IsWinter)
			{
				player.detailBonusSmooth *= 0.5f;
			}
			num6 -= player.detailBonusSmooth * num2;
			num6 = Mathf.Clamp(num6, MinBaseAmbienceScore, 1f);
		}
		if (ThatsLitPlayer.IsDebugSampleFrame && player.Player.DebugInfo != null)
		{
			player.Player.DebugInfo.scoreRaw0 = num6;
		}
		float num10 = Mathf.Max(0.5f - num6, 0f) / 1.5f;
		float num11 = 0.9f * frameStats.RatioShinePixels + 0.75f * frameStats.RatioHighPixels + 0.4f * frameStats.RatioHighMidPixels + 0.15f * frameStats.RatioMidPixels;
		float num12 = CalculateRawLumScore(frameStats, num10, num11, player);
		if (ThatsLitPlayer.IsDebugSampleFrame && player.Player.DebugInfo != null)
		{
			player.Player.DebugInfo.scoreRaw1 = num12 + num6;
		}
		float num13 = player.FindHighestAvgLumRecentFrame(includeThis: true, frameStats.avgLum);
		float num14 = player.FindLowestAvgLumRecentFrame(includeThis: true, frameStats.avgLum);
		num12 += CalculateChangingLumModifier(frameStats.avgLumMultiFrames, player.lum1s, player.lum3s, num6);
		if (ThatsLitPlayer.IsDebugSampleFrame && player.Player.DebugInfo != null)
		{
			player.Player.DebugInfo.scoreRaw2 = num12 + num6;
		}
		float num15 = num13 - num14;
		num15 -= 0.01f;
		num15 = Mathf.Clamp01(num15);
		float num16 = (num15 * num15 + num10 * 0.5f) * (1f + num11 * num10);
		float num17 = num12 + num6;
		float num18 = Mathf.Clamp(num16 - num17, 0f, 2f);
		num12 += num18 * Mathf.Clamp01(num15 * 10f) * num10 * MultiFrameContrastImpactScale;
		if (ThatsLitPlayer.IsDebugSampleFrame && player.Player.DebugInfo != null)
		{
			player.Player.DebugInfo.scoreRaw3 = num12 + num6;
		}
		if (player.Player.LightAndLaserState.AnyVisible)
		{
			num17 = num12 + num6;
			num18 = (player.Player.LightAndLaserState.VisibleLight ? (Mathf.Clamp(0.4f - num17, 0f, 2f) * player.Player.LightAndLaserState.deviceStateCache.light) : (player.Player.LightAndLaserState.VisibleLaser ? (Mathf.Clamp(0.2f - num17, 0f, 2f) * player.Player.LightAndLaserState.deviceStateCache.laser) : (player.Player.LightAndLaserState.VisibleLightSub ? (Mathf.Clamp(0f - num17, 0f, 2f) * player.Player.LightAndLaserState.deviceStateCacheSub.light) : ((!player.Player.LightAndLaserState.VisibleLaserSub) ? 0f : (Mathf.Clamp(0f - num17, 0f, 2f) * player.Player.LightAndLaserState.deviceStateCacheSub.laser)))));
			num12 += num18 * (num10 + 0.1f);
		}
		if (ThatsLitPlayer.IsDebugSampleFrame && player.Player.DebugInfo != null)
		{
			player.Player.DebugInfo.scoreRaw4 = num12 + num6;
		}
		player.litScoreFactor = Mathf.Pow(Mathf.Clamp(num12, 0f, 2f) / 2f, 2f);
		player.litScoreFactor /= 1f + Mathf.Max(num6, 0f);
		player.litScoreFactor = Mathf.Max(player.litScoreFactor, 0f);
		num12 -= num12 * 0.25f * Mathf.Clamp01(num6 + 0.05f);
		num12 += num6;
		num12 = (frameStats.score = Mathf.Clamp(num12, -1f, 1f));
		frameStats.ambienceScore = num6;
		frameStats.baseAmbienceScore = num4;
		float num19 = player.FindHighestScoreRecentFrame(includeThis: true, num12);
		float num20 = player.FindLowestScoreRecentFrame(includeThis: true, num12);
		frameStats.multiFrameLitScore = (num19 * 2f + frameStats.score + player.frame1.score + player.frame2.score + player.frame3.score + player.frame4.score + player.frame5.score - num20 * 2f) / 6f;
		player.frame0 = frameStats;
		return frameStats.multiFrameLitScore;
	}

	protected virtual float FinalTransformScore(float score)
	{
		return score;
	}

	protected virtual float CalculateChangingLumModifier(float avgLumMultiFrames, float lum1s, float lum3s, float ambienceScore)
	{
		return Mathf.Clamp(Mathf.Abs(avgLumMultiFrames - lum1s), 0f, 0.05f) * 10f * (Mathf.Clamp01(0f - ambienceScore) + 0.2f) + Mathf.Clamp(Mathf.Abs(avgLumMultiFrames - lum3s), 0f, 0.025f) * 3f * (Mathf.Clamp01(0f - ambienceScore) + 0.1f);
	}

	protected virtual float CalculateStaticLumModifier(float score, float avgLumMultiFrames, float envLum, float envLumSlow, PlayerLitScoreProfile player)
	{
		float num = Mathf.Clamp01(Mathf.Abs(avgLumMultiFrames - player.lum3s) / 0.2f);
		if (score > 0f)
		{
			score /= 1f + 0.3f * (1f - num);
		}
		else if (score < 0f)
		{
			score *= 1f + 0.1f * (1f - num);
		}
		num = Mathf.Clamp01(Mathf.Abs(avgLumMultiFrames - player.lum3s) / 0.2f);
		if (score > 0f)
		{
			score /= 1f + 0.1f * (1f - num);
		}
		else if (score < 0f)
		{
			score *= 1f + 0.1f * (1f - num);
		}
		return score;
	}

	protected virtual void GetThresholds(float tlf, out float thresholdShine, out float thresholdHigh, out float thresholdHighMid, out float thresholdMid, out float thresholdMidLow, out float thresholdLow)
	{
		thresholdShine = 0.64f;
		thresholdHigh = 0.32f;
		thresholdHighMid = 0.16f;
		thresholdMid = 0.08f;
		thresholdMidLow = 0.04f;
		thresholdLow = 0.02f;
	}

	protected virtual float CalculateRawLumScore(FrameStats thisFrame, float lowAmbienceScoreFactor, float hightLightedPixelFactor, PlayerLitScoreProfile player)
	{
		return Mathf.Clamp((thisFrame.avgLum - MinAmbienceLum) * PixelLumScoreScale * (1f + 2f * lowAmbienceScoreFactor * lowAmbienceScoreFactor) * (0.1f + lowAmbienceScoreFactor) * (1f + hightLightedPixelFactor), 0f, 2f);
	}

	protected virtual void GetPixelScores(float tlf, out float scoreShine, out float scoreHigh, out float scoreHighMid, out float scoreMid, out float scoreMidLow, out float scoreLow, out float scoreDark)
	{
		scoreShine = ScoreShine;
		scoreHigh = ScoreHigh;
		scoreHighMid = ScoreHighMid;
		scoreMid = ScoreMid;
		scoreMidLow = ScoreMidLow;
		scoreLow = ScoreLow;
		scoreDark = ScoreDark;
	}

	protected virtual float CalculateTotalPixelScore(float time, int pxS, int pxH, int pxHM, int pxM, int pxML, int pxL, int pxD)
	{
		GetPixelScores(time, out var scoreShine, out var scoreHigh, out var scoreHighMid, out var scoreMid, out var scoreMidLow, out var scoreLow, out var scoreDark);
		return (float)pxS * scoreShine + (float)pxH * scoreHigh + (float)pxHM * scoreHighMid + (float)pxM * scoreMid + (float)pxML * scoreMidLow + (float)pxL * scoreLow + (float)pxD * scoreDark;
	}

	protected void StartCountPixels(PlayerLitScoreProfile player, NativeArray<Color32> tex, float thresholdShine, float thresholdHigh, float thresholdHighMid, float thresholdMid, float thresholdMidLow, float thresholdLow)
	{
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		if (player != null && tex.IsCreated)
		{
			NativeArray<float> thresholds = default(NativeArray<float>);
			thresholds = new NativeArray<float>(6, (Allocator)3, (NativeArrayOptions)1);
			thresholds[0] = thresholdShine;
			thresholds[1] = thresholdHigh;
			thresholds[2] = thresholdHighMid;
			thresholds[3] = thresholdMid;
			thresholds[4] = thresholdMidLow;
			thresholds[5] = thresholdLow;
			player.PixelCountingJob = new PlayerLitScoreProfile.CountPixelsJob
			{
				thresholds = thresholds,
				tex = tex,
				counted = new NativeArray<int>(1024, (Allocator)3, (NativeArrayOptions)1),
				lum = new NativeArray<float>(256, (Allocator)3, (NativeArrayOptions)1)
			};
			player.CountingJobHandle = IJobParallelForExtensions.Schedule<PlayerLitScoreProfile.CountPixelsJob>(player.PixelCountingJob, tex.Length, tex.Length / 64, default(JobHandle));
		}
	}

	protected void CompleteCountPixels(PlayerLitScoreProfile player, out int shine, out int high, out int highMid, out int mid, out int midLow, out int low, out int dark, out float lum, out float lumNonDark, out int valid)
	{
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		shine = (high = (highMid = (mid = (midLow = (low = (dark = (valid = 0)))))));
		lum = 0f;
		lumNonDark = 0f;
		JobHandle countingJobHandle = player.CountingJobHandle;
		countingJobHandle.Complete();
		PlayerLitScoreProfile.CountPixelsJob pixelCountingJob = player.PixelCountingJob;
		if (player.IsProxy)
		{
			pixelCountingJob.Dispose();
			player.PixelCountingJob = default(PlayerLitScoreProfile.CountPixelsJob);
			return;
		}
		if (pixelCountingJob.tex.IsCreated)
		{
			pixelCountingJob.tex.Dispose();
		}
		if (pixelCountingJob.thresholds.IsCreated)
		{
			pixelCountingJob.thresholds.Dispose();
		}
		if (pixelCountingJob.lum.IsCreated)
		{
			for (int i = 0; i < pixelCountingJob.lum.Length; i += 2)
			{
				lum += pixelCountingJob.lum[i];
				lumNonDark += pixelCountingJob.lum[i + 1];
			}
		}
		if (pixelCountingJob.lum.IsCreated)
		{
			pixelCountingJob.lum.Dispose();
		}
		if (pixelCountingJob.counted.IsCreated)
		{
			for (int j = 0; j < pixelCountingJob.counted.Length; j += 8)
			{
				if (pixelCountingJob.counted[j + 7] != 0)
				{
					shine += pixelCountingJob.counted[j];
					high += pixelCountingJob.counted[j + 1];
					highMid += pixelCountingJob.counted[j + 2];
					mid += pixelCountingJob.counted[j + 3];
					midLow += pixelCountingJob.counted[j + 4];
					low += pixelCountingJob.counted[j + 5];
					dark += pixelCountingJob.counted[j + 6];
					valid += pixelCountingJob.counted[j + 7];
				}
			}
		}
		if (pixelCountingJob.counted.IsCreated)
		{
			pixelCountingJob.counted.Dispose();
		}
		player.PixelCountingJob = default(PlayerLitScoreProfile.CountPixelsJob);
	}

	protected virtual float CalculateBaseAmbienceScore(string locationId, float time, float cloud, PlayerLitScoreProfile player)
	{
		return Mathf.Lerp(GetMinBaseAmbienceLitScore(locationId, time), GetMaxBaseAmbienceLitScore(locationId, time), GetMapAmbienceCoef(locationId, time));
	}

	protected virtual float GetMinBaseAmbienceLitScore(string locationId, float time)
	{
		return MinBaseAmbienceScore;
	}

	protected virtual float GetMaxBaseAmbienceLitScore(string locationId, float time)
	{
		return MaxBaseAmbienceScore;
	}

	protected virtual float GetMapAmbienceCoef(string locationId, float time)
	{
		if (time >= 5f && time < 7.5f)
		{
			return 0.5f * GetTimeProgress(time, 5f, 7.5f);
		}
		if (time >= 7.5f && time < 12f)
		{
			return 0.5f + 0.5f * GetTimeProgress(time, 7.5f, 12f);
		}
		if (time >= 12f && time < 15f)
		{
			return 1f;
		}
		if (time >= 15f && time < 18f)
		{
			return 1f - 0.2f * GetTimeProgress(time, 18f, 20f);
		}
		if (time >= 18f && time < 20f)
		{
			return 0.8f - 0.8f * GetTimeProgress(time, 18f, 20f);
		}
		if (time >= 20f && time < 21.5f)
		{
			return 0.3f - 0.3f * GetTimeProgress(time, 20f, 21.5f);
		}
		if (time >= 22f && time < 24f)
		{
			return 0.1f * GetTimeProgress(time, 22f, 24f);
		}
		if (time >= 0f && time < 3f)
		{
			return 0.1f;
		}
		if (time >= 3f && time < 5f)
		{
			return 0.1f - 0.1f * GetTimeProgress(time, 3f, 5f);
		}
		return 0f;
	}

	protected virtual float CalculateMoonLight(PlayerLitScoreProfile player, string locationId, float time, float cloudiness)
	{
		float maxMoonlightScore = GetMaxMoonlightScore();
		return CloudnessToAmbienceScale(cloudiness) * maxMoonlightScore * CalculateMoonLightTimeFactor(locationId, time);
	}

	protected virtual float CalculateSunLight(PlayerLitScoreProfile player, string locationId, float time, float cloudiness)
	{
		float maxSunlightScore = GetMaxSunlightScore();
		return CloudnessToAmbienceScale(cloudiness) * maxSunlightScore * CalculateSunLightTimeFactor(locationId, time);
	}

	private float CloudnessToAmbienceScale(float cloudiness)
	{
		float num = Mathf.InverseLerp(1.15f, -1.5f, cloudiness);
		num = num * num * (3f - 2f * num);
		return num * 2.2f;
	}

	internal virtual float CalculateSunLightTimeFactor(string locationId, float time)
	{
		if (time >= 5f && time < 6f)
		{
			return GetTimeProgress(time, 5f, 6f) * 0.1f;
		}
		if (time >= 6f && time < 8f)
		{
			return 0.1f + GetTimeProgress(time, 6f, 8f) * 0.2f;
		}
		if (time >= 8f && time < 12f)
		{
			return 0.3f + GetTimeProgress(time, 8f, 12f) * 0.7f;
		}
		if (time >= 12f && time < 15f)
		{
			return 1f;
		}
		if (time >= 15f && time < 19f)
		{
			return 1f - GetTimeProgress(time, 15f, 19f) * 0.5f;
		}
		if (time >= 19f && time < 22f)
		{
			return 0.5f - GetTimeProgress(time, 19f, 22f) * 0.5f;
		}
		return 0f;
	}

	protected virtual float CalculateMoonLightTimeFactor(string locationId, float time)
	{
		if (time > 0f && time < 3.5f)
		{
			return Mathf.Clamp01(time / 2f);
		}
		if (time >= 3.5f && time < 5f)
		{
			return 1f - Mathf.Clamp01((time - 3.5f) / 1.5f);
		}
		return 0f;
	}

	protected virtual float GetTimeProgress(float now, float from, float to)
	{
		return Mathf.Clamp01((now - from) / (to - from));
	}

	protected internal virtual float GetMinAmbianceLum()
	{
		return MinAmbienceLum;
	}

	protected internal virtual float GetMaxAmbianceLum()
	{
		return MaxAmbienceLum;
	}

	protected internal virtual float GetAmbianceLumRange()
	{
		return GetMaxAmbianceLum() + 0.001f - GetMinAmbianceLum();
	}

	protected internal virtual float GetMaxSunlightScore()
	{
		return MaxSunlightScore;
	}

	protected internal virtual float GetMaxMoonlightScore()
	{
		return MaxMoonlightScore;
	}
}
