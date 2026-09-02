namespace ThatsLit;

public class LabScoreCalculator : ScoreCalculator
{
	protected override float BunkerBaseAmbienceTarget => 0f;

	protected override float MinBaseAmbienceScore => -0.5f;

	protected override float MaxBaseAmbienceScore => -0.5f;

	protected override float MaxMoonlightScore => 0f;

	protected override float MaxSunlightScore => 0f;

	protected override float MinAmbienceLum => 0.02f;

	protected override float MaxAmbienceLum => 0.06f;

	protected override float ThresholdShine => 0.2f;

	protected override float ThresholdHigh => 0.1f;

	protected override float ThresholdHighMid => 0.05f;

	protected override float ThresholdMid => 0.025f;

	protected override float ThresholdMidLow => 0.01f;

	protected override float ThresholdLow => 0.005f;

	protected override float PixelLumScoreScale => 5.5f;

	protected override float GetMapAmbienceCoef(string locationId, float time)
	{
		return 0f;
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
