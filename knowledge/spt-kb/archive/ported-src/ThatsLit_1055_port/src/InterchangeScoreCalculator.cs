using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThatsLit;

public class InterchangeScoreCalculator : ScoreCalculator
{
	public class Data
	{
		private bool isPlayerInParking;

		public bool IsPlayerInParking
		{
			get
			{
				return isPlayerInParking;
			}
			set
			{
				if (isPlayerInParking != value)
				{
					TimeEnterOrLeaveParking = Time.time;
				}
				isPlayerInParking = value;
			}
		}

		public float TimeEnterOrLeaveParking { get; set; }

		public float ParkingTransitionFactor => Mathf.Clamp01((Time.time - TimeEnterOrLeaveParking) / 5f);

		public bool IsOverhead { get; set; }
	}

	protected override float MinBaseAmbienceScore => -0.8f;

	protected override float MaxBaseAmbienceScore => -0.15f;

	protected override float MaxMoonlightScore => base.MaxMoonlightScore * 0.75f;

	protected override float MinAmbienceLum => 0.008f;

	protected override float MaxAmbienceLum => 0.008f;

	protected override float PixelLumScoreScale => 2.5f;

	protected override float IndoorAmbienceCutoff => 1f;

	protected override float ThresholdShine => 0.5f;

	protected override float ThresholdHigh => 0.35f;

	protected override float ThresholdHighMid => 0.2f;

	protected override float ThresholdMid => 0.1f;

	protected override float ThresholdMidLow => 0.025f;

	protected override float ThresholdLow => 0.005f;

	protected override float MultiFrameContrastImpactScale => 0.65f;

	protected override float CalculateMoonLight(PlayerLitScoreProfile player, string locationId, float time, float cloudiness)
	{
		Data data = player.ScoreCalcData as Data;
		if (data.IsPlayerInParking)
		{
			cloudiness = Mathf.Lerp(cloudiness, 1.3f, data.ParkingTransitionFactor);
		}
		return base.CalculateMoonLight(player, locationId, time, cloudiness);
	}

	protected override float CalculateSunLight(PlayerLitScoreProfile player, string locationId, float time, float cloudiness)
	{
		Data data = player.ScoreCalcData as Data;
		if (data.IsPlayerInParking)
		{
			cloudiness = Mathf.Lerp(cloudiness, 1.3f, data.ParkingTransitionFactor);
		}
		return base.CalculateSunLight(player, locationId, time, cloudiness);
	}

	public override float CalculateMultiFrameScore(float cloud, float fog, float rain, ThatsLitGameworld gameWorld, PlayerLitScoreProfile player, float time, string locationId)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0051: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		if (!(player.ScoreCalcData is Data))
		{
			player.ScoreCalcData = new Data();
		}
		Data data = player.ScoreCalcData as Data;
		bool flag = false;
		data.IsOverhead = false;
		RaycastHit val = default(RaycastHit);
		if (Physics.Raycast(new Ray(player.Player.Player.MainParts[(BodyPartType)0].Position, Vector3.up), out val, 5f, (int)(LayersMaskController.HighPolyCollider)))
		{
			data.IsOverhead = true;
			Scene scene = ((Component)val.transform).gameObject.scene;
			if (scene.name == "Shopping_Mall_parking_work" && ((Component)player.Player).transform.position.y < 23.5f)
			{
				flag = true;
			}
		}
		if (data.IsPlayerInParking != flag)
		{
			data.IsPlayerInParking = flag;
		}
		return base.CalculateMultiFrameScore(cloud, fog, rain, gameWorld, player, time, locationId);
	}

	protected override float CalculateBaseAmbienceScore(string locationId, float time, float cloud, PlayerLitScoreProfile player)
	{
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		if (!(player.ScoreCalcData is Data))
		{
			player.ScoreCalcData = new Data();
		}
		Data data = player.ScoreCalcData as Data;
		if (data.IsPlayerInParking)
		{
			return Mathf.Lerp(base.CalculateBaseAmbienceScore(locationId, time, cloud, player), MinBaseAmbienceScore + 0.15f * cloud, player.Player.OverheadHaxRatingFactor * player.Player.OverheadHaxRatingFactor * data.ParkingTransitionFactor);
		}
		if (data.IsOverhead)
		{
			if (CalculateSunLightTimeFactor(locationId, Utility.GetInGameDayTime()) > 0.05f)
			{
				return Mathf.Lerp(base.CalculateBaseAmbienceScore(locationId, time, cloud, player), -0.54f, 0.7f * Mathf.InverseLerp(2f, 7f, Time.time - player.Player.lastOutside) * player.Player.AmbienceShadowFactor);
			}
			float num = Mathf.Clamp01((((Component)player.Player).transform.position.y - 22f) / 2.5f);
			return Mathf.Lerp(base.CalculateBaseAmbienceScore(locationId, time, cloud, player), -0.35f + 0.2f * cloud, player.Player.OverheadHaxRatingFactor * player.Player.OverheadHaxRatingFactor * 0.9f * num);
		}
		return base.CalculateBaseAmbienceScore(locationId, time, cloud, player);
	}

	protected override float CalculateRawLumScore(FrameStats thisFrame, float lowAmbienceScoreFactor, float hightLightedPixelFactor, PlayerLitScoreProfile player)
	{
		Data data = player.ScoreCalcData as Data;
		if (!data.IsPlayerInParking)
		{
			return base.CalculateRawLumScore(thisFrame, lowAmbienceScoreFactor, hightLightedPixelFactor, player);
		}
		return Mathf.Clamp((thisFrame.avgLum - MinAmbienceLum) * (PixelLumScoreScale * 1.2f) * (1f + 2f * lowAmbienceScoreFactor * lowAmbienceScoreFactor) * (0.1f + lowAmbienceScoreFactor) * (1f - (thisFrame.RatioDarkPixels + thisFrame.RatioLowPixels * 0.75f + thisFrame.RatioMidLowPixels * 0.5f) * (1f - thisFrame.cloudiness * 0.15f) * (player.Player.OverheadHaxRatingFactor * player.Player.OverheadHaxRatingFactor) * data.ParkingTransitionFactor) * (1f + hightLightedPixelFactor), 0.1f, 2f);
	}

	protected override float CalculateMoonLightTimeFactor(string locationId, float time)
	{
		if (time > 23.9f || time < 0f)
		{
			return 0.1f * (24f - time);
		}
		if (time >= 0f && time < 3.5f)
		{
			return 0.1f + 0.9f * GetTimeProgress(time, 0f, 2f);
		}
		if (time >= 3.5f && (double)time < 4.33)
		{
			return 1f - 0.5f * GetTimeProgress(time, 3.5f, 4.33f);
		}
		if (time >= 4.33f && time < 4.416f)
		{
			return 0.5f - 0.3f * GetTimeProgress(time, 4.33f, 4.416f);
		}
		if (time >= 4.416f && time < 5f)
		{
			return 0.3f - GetTimeProgress(time, 4.416f, 5f);
		}
		return 0f;
	}

	protected internal override void OnGUI(PlayerLitScoreProfile player, bool layout = false)
	{
		if (!(player.ScoreCalcData is Data))
		{
			player.ScoreCalcData = new Data();
		}
		Data data = player.ScoreCalcData as Data;
		base.OnGUI(player, layout);
		if (data.IsPlayerInParking)
		{
			GUILayout.Label($"  Parking: {data.ParkingTransitionFactor}", Array.Empty<GUILayoutOption>());
		}
	}
}
