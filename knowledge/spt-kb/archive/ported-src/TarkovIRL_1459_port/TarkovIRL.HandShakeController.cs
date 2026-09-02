using EFT;
using UnityEngine;

namespace TarkovIRL;

internal class HandShakeController
{
	private static readonly float _HandShakeCurveSpeedMulti = 0.25f;

	private static readonly float _HandShakeMultiGeneral = 0.005f;

	private static float _handShakeLoopTimeX = 0f;

	private static float _handShakeLoopTimeY = 0f;

	private static float _handShakeStrengthLerp = 0f;

	public static Vector3 GetHandsShakePosition(Player player)
	{
		if (!player.ProceduralWeaponAnimation.IsAiming)
		{
			return Vector3.zero;
		}
		float num = _HandShakeCurveSpeedMulti * PrimeMover.ArmShakeRateMulti.Value;
		_handShakeLoopTimeX += player.DeltaTime * num * 0.37f;
		if (_handShakeLoopTimeX >= 1f)
		{
			_handShakeLoopTimeX -= 1f;
		}
		_handShakeLoopTimeY -= player.DeltaTime * num;
		if (_handShakeLoopTimeY <= 0f)
		{
			_handShakeLoopTimeY += 1f;
		}
		float num2 = (WeaponController.IsPistol ? 2f : 1f);
		float num3 = ((!WeaponController.IsPistol && !WeaponController.HasCheekWeld()) ? 1.8f : 1f);
		float num4 = (PlayerMotionController.IsAugmentedBreath ? 0.5f : 1f);
		bool flag = player.HealthController.IsBodyPartBroken(EBodyPart.LeftArm);
		bool flag2 = player.HealthController.IsBodyPartBroken(EBodyPart.RightArm);
		float num5 = 1f;
		num5 *= (flag ? 2f : 1f);
		num5 *= (flag2 ? 2f : 1f);
		float b = EfficiencyController.EfficiencyModifier * PrimeMover.ArmShakeMulti.Value * _HandShakeMultiGeneral * num2 * num3 * WeaponController.CurrentWeaponWeight * num4 * num5;
		_handShakeStrengthLerp = Mathf.Lerp(_handShakeStrengthLerp, b, player.DeltaTime * 7f);
		AnimationCurve handsShakeCurve = PrimeMover.Instance.HandsShakeCurve;
		float x = handsShakeCurve.Evaluate(_handShakeLoopTimeX) * _handShakeStrengthLerp;
		float y = handsShakeCurve.Evaluate(_handShakeLoopTimeY) * _handShakeStrengthLerp;
		return new Vector3(x, y, 0f);
	}
}
