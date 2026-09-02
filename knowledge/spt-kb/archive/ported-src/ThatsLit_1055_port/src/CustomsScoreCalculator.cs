namespace ThatsLit;

public class CustomsScoreCalculator : ScoreCalculator
{
	protected override float MinBaseAmbienceScore => -0.7f;

	protected override float NonCloudinessBaseAmbienceScoreImpact => 0.15f;

	protected override float MaxMoonlightScore => 0.2f;

	protected override float PixelLumScoreScale => 2.2f;

	protected override float BunkerBaseAmbienceTarget => -0.45f;
}
