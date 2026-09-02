using UnityEngine;

namespace ThatsLit;

public class PlayerDebugInfo
{
	public float lastCalcFrom;

	public float lastScore;

	public float lastFactor1;

	public float lastFactor2;

	public float rawTerrainScoreSample;

	public float lastCalcTo0;

	public float lastCalcTo1;

	public float lastCalcTo2;

	public float lastCalcTo3;

	public float lastCalcTo4;

	public float lastCalcTo5;

	public float lastCalcTo6;

	public float lastCalcTo7;

	public float lastCalcTo8;

	public int calced;

	public int calcedLastFrame;

	public int encounter;

	public int vagueHint;

	public int vagueHintCancel;

	public int signalDanger;

	public Vector3 lastTriggeredDetailCoverDirNearest;

	public float lastTiltAngle;

	public float lastRotateAngle;

	public float lastDisFactorNearest;

	public float lastNearest;

	public float lastFinalDetailScoreNearest;

	internal float scoreRawBase;

	internal float scoreRaw0;

	internal float scoreRaw1;

	internal float scoreRaw2;

	internal float scoreRaw3;

	internal float scoreRaw4;

	internal float shinePixelsRatioSample;

	internal float highLightPixelsRatioSample;

	internal float highMidLightPixelsRatioSample;

	internal float midLightPixelsRatioSample;

	internal float midLowLightPixelsRatioSample;

	internal float lowLightPixelsRatioSample;

	internal float darkPixelsRatioSample;

	internal float lastEncounterShotAngleDelta;

	internal float lastEncounteringShotCutoff;

	internal float lastBushRat;

	internal float lastVisiblePartsFactor;

	internal float lastGlobalOverlookChance;

	internal float lastDisCompThermal;

	internal float lastDisCompNVG;

	internal float lastDisCompDay;

	internal float lastDisComp;

	internal float lastNearestFocusAngleX;

	internal float lastNearestFocusAngleY;

	internal int cancelledSAINNoBush;

	internal int attemptToCancelSAINNoBush;

	internal float lastInterruptChance;

	internal float lastInterruptChanceDis;

	internal int nearestCaution;

	internal Vector3 nearestOffset;

	internal Vector3 sniperHintOffset;

	internal float sniperHintChance;

	internal int flashLightHint;

	internal int forceLooks;

	internal int sideLooks;

	public bool IsBushRatting { get; set; }
}
