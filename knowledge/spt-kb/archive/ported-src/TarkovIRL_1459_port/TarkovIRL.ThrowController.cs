using UnityEngine;

namespace TarkovIRL;

internal static class ThrowController
{
	private static readonly float _OverhandYExtraMulti = 4f;

	private static float _throwLerp = 1f;

	private static bool _isThrowing = false;

	private static bool _isUnderhand = false;

	public static Vector3 GetThrowOffset
	{
		get
		{
			AnimationCurve animationCurve = (_isUnderhand ? PrimeMover.Instance.ThrowVisualUnderhandCurveX : PrimeMover.Instance.ThrowVisualCurveX);
			AnimationCurve animationCurve2 = (_isUnderhand ? PrimeMover.Instance.ThrowVisualUnderhandCurveY : PrimeMover.Instance.ThrowVisualCurveY);
			float num = animationCurve.Evaluate(_throwLerp) * PrimeMover.ThrowStrengthMulti.Value;
			float num2 = (_isUnderhand ? (-1f) : 1f);
			float num3 = animationCurve2.Evaluate(_throwLerp) * _OverhandYExtraMulti * PrimeMover.ThrowStrengthMulti.Value * num2;
			return new Vector3(0f - num, 0f - num3, 0f);
		}
	}

	public static bool IsThrowing => _isThrowing;

	public static float ThrowProgress => _throwLerp;

	public static void UpdateLerp(float dt)
	{
		if (_isThrowing)
		{
			_throwLerp = Mathf.Lerp(_throwLerp, 1f, dt * PrimeMover.ThrowSpeedMulti.Value);
			if (_throwLerp >= 0.98f)
			{
				_isThrowing = false;
				_throwLerp = 1f;
			}
		}
	}

	public static void NewThrow(bool isUnderhand)
	{
		_isThrowing = true;
		_throwLerp = 0f;
		_isUnderhand = isUnderhand;
	}
}
