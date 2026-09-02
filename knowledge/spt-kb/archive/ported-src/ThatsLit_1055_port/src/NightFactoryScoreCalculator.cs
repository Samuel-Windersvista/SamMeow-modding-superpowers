namespace ThatsLit;

public class NightFactoryScoreCalculator : ScoreCalculator
{
	protected override float MinBaseAmbienceScore => -0.87f;

	protected override float MaxMoonlightScore => 0f;

	protected override float MaxSunlightScore => 0f;

	protected override float MinAmbienceLum => 0.002f;

	protected override float MaxAmbienceLum => 0.002f;

	protected override float PixelLumScoreScale => 6f;

	protected override float ThresholdShine => 0.5f;

	protected override float ThresholdHigh => 0.35f;

	protected override float ThresholdHighMid => 0.2f;

	protected override float ThresholdMid => 0.1f;

	protected override float ThresholdMidLow => 0.025f;

	protected override float ThresholdLow => 0.005f;

	protected override void GetPixelScores(float tlf, out float scoreShine, out float scoreHigh, out float scoreHighMid, out float scoreMid, out float scoreMidLow, out float scoreLow, out float scoreDark)
	{
		scoreShine = 5f;
		scoreHigh = 1.5f;
		scoreHighMid = 0.8f;
		scoreMid = 0.5f;
		scoreMidLow = 0.2f;
		scoreLow = 0.1f;
		scoreDark = 0f;
	}

	protected override float GetMapAmbienceCoef(string locationId, float time)
	{
		return 0.1f;
	}

	protected override float CalculateMoonLight(PlayerLitScoreProfile player, string locationId, float time, float cloudiness)
	{
		return 0f;
	}

	protected override float CalculateMoonLightTimeFactor(string locationId, float time)
	{
		return 0f;
	}

	internal override float CalculateSunLightTimeFactor(string locationId, float time)
	{
		return 0f;
	}
}
