using UnityEngine;

namespace ThatsLit;

public class GroundZeroScoreCalculator : ScoreCalculator
{
	protected override float MinBaseAmbienceScore => -0.75f;

	protected override float MaxBaseAmbienceScore => -0.15f;

	protected override float MaxSunlightScore => 0.25f;

	protected override float MaxMoonlightScore => 0.1f;

	protected override float MinAmbienceLum => 0.011f;

	protected override float MaxAmbienceLum => 0.111f;

	protected override float ThresholdShine => 0.2f;

	protected override float ThresholdHigh => 0.1f;

	protected override float ThresholdHighMid => 0.05f;

	protected override float ThresholdMid => 0.02f;

	protected override float ThresholdMidLow => 0.01f;

	protected override float ThresholdLow => 0.005f;

	protected override float PixelLumScoreScale => 1.6f;

	protected override float GetMapAmbienceCoef(string locationId, float time)
	{
		if (time >= 6f && time < 7.5f)
		{
			return 0.35f * GetTimeProgress(time, 6f, 7.5f);
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
			return 0.8f - 0.4f * GetTimeProgress(time, 18f, 20f);
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
			return 0.1f * GetTimeProgress(time, 3f, 5f);
		}
		return 0f;
	}

	protected override float CalculateBaseAmbienceScore(string locationId, float time, float cloud, PlayerLitScoreProfile player)
	{
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		float y = player.Player.Player.Transform.Original.position.y;
		float num = Mathf.Clamp01((14.7f - y) / 1.5f) * 0.6f;
		return Mathf.Max(base.CalculateBaseAmbienceScore(locationId, time, cloud, player) - num, MinBaseAmbienceScore);
	}
}
