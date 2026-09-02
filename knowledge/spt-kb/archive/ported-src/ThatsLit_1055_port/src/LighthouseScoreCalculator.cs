namespace ThatsLit;

public class LighthouseScoreCalculator : ScoreCalculator
{
	protected override float MinBaseAmbienceScore => -0.88f;

	protected override float NonCloudinessBaseAmbienceScoreImpact => 0.05f;

	protected override float PixelLumScoreScale => 2.5f;

	protected override float ThresholdShine => 0.4f;

	protected override float ThresholdHigh => 0.3f;

	protected override float ThresholdHighMid => 0.2f;

	protected override float ThresholdMid => 0.1f;

	protected override float ThresholdMidLow => 0.04f;

	protected override float ThresholdLow => 0.015f;

	internal override float CalculateSunLightTimeFactor(string locationId, float time)
	{
		if (time >= 15f && time < 20f)
		{
			return 1f - 0.3f * GetTimeProgress(time, 15f, 20f);
		}
		if (time >= 20f && time < 21.5f)
		{
			return 0.7f * (1f - GetTimeProgress(time, 20f, 21.5f));
		}
		return base.CalculateSunLightTimeFactor(locationId, time);
	}
}
