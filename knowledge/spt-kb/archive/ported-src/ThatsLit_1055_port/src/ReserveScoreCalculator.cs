namespace ThatsLit;

public class ReserveScoreCalculator : ScoreCalculator
{
	protected override float MinBaseAmbienceScore => -0.82f;

	protected override float MaxBaseAmbienceScore => -0.1f;

	protected override float MinAmbienceLum => 0.011f;

	protected override float MaxAmbienceLum => 0.015f;

	protected override float ThresholdShine => 0.3f;

	protected override float ThresholdHigh => 0.2f;

	protected override float ThresholdHighMid => 0.12f;

	protected override float ThresholdMid => 0.06f;

	protected override float ThresholdMidLow => 0.03f;

	protected override float ThresholdLow => 0.015f;

	protected override float PixelLumScoreScale => 2.5f;

	protected override float BunkerBaseAmbienceTarget => -0.5f;
}
