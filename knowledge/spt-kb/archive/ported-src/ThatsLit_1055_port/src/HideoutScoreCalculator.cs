namespace ThatsLit;

public class HideoutScoreCalculator : ScoreCalculator
{
	protected override float MinBaseAmbienceScore => -0.65f;

	protected override float MaxBaseAmbienceScore => -0.65f;

	protected override float MaxMoonlightScore => 0f;

	protected override float MaxSunlightScore => 0f;

	protected override float MinAmbienceLum => 0.066f;

	protected override float MaxAmbienceLum => 0.07f;
}
