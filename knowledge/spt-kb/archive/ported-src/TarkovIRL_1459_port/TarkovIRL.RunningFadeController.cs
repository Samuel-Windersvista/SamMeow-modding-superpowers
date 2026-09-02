using UnityEngine;

namespace TarkovIRL;

internal class RunningFadeController
{
	private static bool _runningLastFrame = false;

	private static float _fadeTimer1 = 0f;

	private static float _fadeTimer2 = 0f;

	private static float _fadeSmoothed = 0f;

	private static Vector3 _lowerFromRunPos = new Vector3(0.5f, 0f, -0.5f);

	private static Vector3 _lowerFromRunRot = new Vector3(0.5f, -0.5f, -0.5f);

	private static Vector3 _offsetPos = Vector3.zero;

	private static Vector3 _offsetRot = Vector3.zero;

	private static readonly float _FirstCurveTime = 0.05f;

	private static AnimationCurve _fadeCurve1;

	private static AnimationCurve _fadeCurve2 = new AnimationCurve(new Keyframe(0f, 0.9988906f), new Keyframe(0.01f, 0.9998059f), new Keyframe(0.02f, 0.9995718f), new Keyframe(0.03f, 0.9981838f), new Keyframe(0.04f, 0.995638f), new Keyframe(0.05f, 0.9919298f), new Keyframe(0.05999999f, 0.987055f), new Keyframe(0.06999999f, 0.9810095f), new Keyframe(0.07999999f, 0.9737888f), new Keyframe(0.08999999f, 0.9653888f), new Keyframe(0.09999999f, 0.9558052f), new Keyframe(0.11f, 0.9450337f), new Keyframe(0.12f, 0.9330701f), new Keyframe(0.13f, 0.9199101f), new Keyframe(0.14f, 0.9055493f), new Keyframe(0.15f, 0.8899836f), new Keyframe(0.16f, 0.8732086f), new Keyframe(0.17f, 0.8552202f), new Keyframe(0.18f, 0.836014f), new Keyframe(0.19f, 0.8155859f), new Keyframe(0.2f, 0.7939313f), new Keyframe(0.21f, 0.7710463f), new Keyframe(0.22f, 0.7469263f), new Keyframe(0.23f, 0.7215674f), new Keyframe(0.24f, 0.694965f), new Keyframe(0.25f, 0.6671151f), new Keyframe(0.26f, 0.6386222f), new Keyframe(0.27f, 0.6109041f), new Keyframe(0.28f, 0.5840075f), new Keyframe(0.29f, 0.55792f), new Keyframe(0.3f, 0.532629f), new Keyframe(0.31f, 0.5081222f), new Keyframe(0.32f, 0.4843872f), new Keyframe(0.33f, 0.4614114f), new Keyframe(0.3399999f, 0.4391825f), new Keyframe(0.3499999f, 0.4176879f), new Keyframe(0.3599999f, 0.3969153f), new Keyframe(0.3699999f, 0.3768523f), new Keyframe(0.3799999f, 0.3574863f), new Keyframe(0.3899999f, 0.338805f), new Keyframe(0.3999999f, 0.3207959f), new Keyframe(0.4099999f, 0.3034467f), new Keyframe(0.4199999f, 0.2867446f), new Keyframe(0.4299999f, 0.2706775f), new Keyframe(0.4399998f, 0.2552329f), new Keyframe(0.4499998f, 0.2403983f), new Keyframe(0.4599998f, 0.2261612f), new Keyframe(0.4699998f, 0.2125092f), new Keyframe(0.4799998f, 0.19943f), new Keyframe(0.4899998f, 0.186911f), new Keyframe(0.4999998f, 0.1749398f), new Keyframe(0.5099998f, 0.163504f), new Keyframe(0.5199998f, 0.1525912f), new Keyframe(0.5299998f, 0.1421888f), new Keyframe(0.5399998f, 0.1322846f), new Keyframe(0.5499998f, 0.1228659f), new Keyframe(0.5599998f, 0.1139204f), new Keyframe(0.5699998f, 0.1054356f), new Keyframe(0.5799997f, 0.09739923f), new Keyframe(0.5899997f, 0.08979863f), new Keyframe(0.5999997f, 0.08262151f), new Keyframe(0.6099997f, 0.07585531f), new Keyframe(0.6199997f, 0.06948775f), new Keyframe(0.6299997f, 0.06350619f), new Keyframe(0.6399997f, 0.05789834f), new Keyframe(0.6499997f, 0.05265176f), new Keyframe(0.6599997f, 0.04775393f), new Keyframe(0.6699997f, 0.04319245f), new Keyframe(0.6799996f, 0.03895491f), new Keyframe(0.6899996f, 0.03502876f), new Keyframe(0.6999996f, 0.03140169f), new Keyframe(0.7099996f, 0.02806127f), new Keyframe(0.7199996f, 0.02499503f), new Keyframe(0.7299996f, 0.02219039f), new Keyframe(0.7399996f, 0.01963508f), new Keyframe(0.7499996f, 0.01731658f), new Keyframe(0.7599996f, 0.01522255f), new Keyframe(0.7699996f, 0.01334041f), new Keyframe(0.7799996f, 0.01165783f), new Keyframe(0.7899995f, 0.01016223f), new Keyframe(0.7999995f, 0.008841455f), new Keyframe(0.8099995f, 0.007682681f), new Keyframe(0.8199995f, 0.006673813f), new Keyframe(0.8299995f, 0.005802214f), new Keyframe(0.8399995f, 0.005055606f), new Keyframe(0.8499995f, 0.004421234f), new Keyframe(0.8599995f, 0.003887057f), new Keyframe(0.8699995f, 0.003440261f), new Keyframe(0.8799995f, 0.003068745f), new Keyframe(0.8899994f, 0.002759933f), new Keyframe(0.8999994f, 0.002501369f), new Keyframe(0.9099994f, 0.002280474f), new Keyframe(0.9199994f, 0.00208503f), new Keyframe(0.9299994f, 0.001902521f), new Keyframe(0.9399994f, 0.001720548f), new Keyframe(0.9499994f, 0.001526594f), new Keyframe(0.9599994f, 0.001308262f), new Keyframe(0.9699994f, 0.001053035f), new Keyframe(0.9799994f, 0.0007486939f), new Keyframe(0.9899994f, 0.000382483f), new Keyframe(1f, 0f));

	private static bool _fadeActive = false;

	public static void UpdateRunningFadeOffsets(float dt)
	{
		bool isRunning = AnimStateController.IsRunning;
		if (!(!_runningLastFrame && isRunning) && _runningLastFrame && !isRunning)
		{
			FadeOutOfRun();
		}
		_runningLastFrame = isRunning;
		if (!_fadeActive)
		{
		}
		if (_fadeTimer1 < _FirstCurveTime)
		{
			_fadeTimer1 += dt * PrimeMover.RunFadeDTMulti.Value;
		}
		else
		{
			float num = (float.IsInfinity(EfficiencyController.EfficiencyModifierInverse) ? 1f : EfficiencyController.EfficiencyModifierInverse);
			_fadeTimer2 += dt * num;
		}
		if (_fadeTimer2 > 1f)
		{
			_fadeActive = false;
		}
		float b = ((_fadeTimer1 < _FirstCurveTime) ? _fadeTimer1 : _fadeTimer2);
		_fadeSmoothed = Mathf.Lerp(_fadeSmoothed, b, dt * 10f);
	}

	private static void FadeIntoRun()
	{
		_fadeTimer1 = 0f;
	}

	private static void FadeOutOfRun()
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Invalid comparison between Unknown and I4
		if (!WeaponController.IsPistol && (int)StanceController.CurrentStance != 2)
		{
			float firstCurveTime = _FirstCurveTime;
			_fadeCurve1 = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(firstCurveTime, 1f));
			_fadeActive = true;
			_fadeTimer1 = 0f;
			_fadeTimer2 = 0f;
			_offsetPos = _lowerFromRunPos;
			_offsetRot = _lowerFromRunRot;
		}
	}

	public static void GetRunFadeOffsets(out Vector3 pos, out Quaternion rot)
	{
		float t = ((_fadeTimer1 < _FirstCurveTime) ? _fadeCurve1.Evaluate(_fadeSmoothed) : _fadeCurve2.Evaluate(_fadeSmoothed));
		Vector3 offsetPos = _offsetPos;
		Vector3 offsetRot = _offsetRot;
		pos = Vector3.Lerp(Vector3.zero, offsetPos, t);
		rot = TIRLUtils.GetQuatFromV3(Vector3.Lerp(Vector3.zero, offsetRot, t));
	}
}
