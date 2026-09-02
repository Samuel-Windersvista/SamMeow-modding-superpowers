namespace ThatsLit;

public class ShorelineScoreCalculator : ScoreCalculator
{
	protected override float MinBaseAmbienceScore => -0.9f;

	protected override float MaxMoonlightScore => base.MaxMoonlightScore * 0.5f;

	protected override float MinAmbienceLum => 0.008f;

	protected override float MaxAmbienceLum => 0.008f;

	protected override float ThresholdShine => 0.5f;

	protected override float ThresholdHigh => 0.35f;

	protected override float ThresholdHighMid => 0.2f;

	protected override float ThresholdMid => 0.1f;

	protected override float ThresholdMidLow => 0.025f;

	protected override float NonCloudinessBaseAmbienceScoreImpact => 0.1f;

	internal override float CalculateSunLightTimeFactor(string locationId, float time)
	{
		if ((double)time >= 5.5 && (double)time < 6.5)
		{
			return GetTimeProgress(time, 5f, 6f) * 0.1f;
		}
		if ((double)time >= 6.5 && (double)time < 7.5)
		{
			return 0.1f + GetTimeProgress(time, 6f, 8f) * 0.2f;
		}
		if ((double)time >= 7.5 && time < 12f)
		{
			return 0.3f + GetTimeProgress(time, 8f, 12f) * 0.7f;
		}
		if (time >= 12f && time < 15.5f)
		{
			return 1f;
		}
		if (time >= 15.5f && time < 20.5f)
		{
			return 1f - 0.35f * GetTimeProgress(time, 15.5f, 20.5f);
		}
		if (time >= 20.5f && time < 21.9f)
		{
			return 0.65f - 0.65f * GetTimeProgress(time, 20.5f, 21.9f);
		}
		return 0f;
	}
}
