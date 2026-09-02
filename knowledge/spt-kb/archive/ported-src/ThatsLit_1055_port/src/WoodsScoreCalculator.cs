using UnityEngine;

namespace ThatsLit;

public class WoodsScoreCalculator : ScoreCalculator
{
	protected override float MinBaseAmbienceScore => -0.75f;

	protected override float MinAmbienceLum => 0.015f;

	protected override float MaxAmbienceLum => 0.017f;

	protected override float ThresholdShine => 0.2f;

	protected override float ThresholdHigh => 0.1f;

	protected override float ThresholdHighMid => 0.05f;

	protected override float ThresholdMid => 0.02f;

	protected override float ThresholdMidLow => 0.01f;

	protected override float ThresholdLow => 0.005f;

	protected override float NightTerrainImpactScale => 0.3f;

	protected override float GetMapAmbienceCoef(string locationId, float time)
	{
		if (time >= 5f && time < 7.5f)
		{
			return 0.5f * Mathf.InverseLerp(5f, 7.5f, time);
		}
		if (time >= 7.5f && time < 12f)
		{
			return 0.5f + 0.5f * Mathf.InverseLerp(7.5f, 12f, time);
		}
		if (time >= 12f && time < 15f)
		{
			return 1f;
		}
		if (time >= 15f && time < 18f)
		{
			return 1f - 0.2f * Mathf.InverseLerp(15f, 18f, time);
		}
		if (time >= 18f && time < 20f)
		{
			return 0.8f - 0.4f * Mathf.InverseLerp(18f, 20f, time);
		}
		if (time >= 20f && time < 23f)
		{
			return 0.4f - 0.3f * Mathf.InverseLerp(20f, 23f, time);
		}
		if (time >= 23f && time < 24f)
		{
			return 0.1f;
		}
		if (time >= 0f && time < 3f)
		{
			return 0.1f;
		}
		if (time >= 3f && time < 5f)
		{
			return 0.1f - 0.1f * Mathf.InverseLerp(3f, 5f, time);
		}
		return 0f;
	}
}
