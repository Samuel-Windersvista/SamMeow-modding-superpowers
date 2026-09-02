using UnityEngine;

namespace TarkovIRL;

internal class NewSwayController
{
	private static float _lerpPosHorizontal = 0f;

	private static float _lerpPosVertical = 0f;

	private static float _lerpRot = 0f;

	private static float _weaponTiltLerp = 0f;

	private static float _leanVerticalLerp = 0f;

	private static float _vertDropFromRotLerp = 0f;

	private static float _hyperVerticalLerp = 0f;

	private static Vector3 _posSmoothed = Vector3.zero;

	private static Vector3 _rotSmoothed = Vector3.zero;

	private static int _lagginSwaySetSize = 30;

	private static Vector3[] _laggingSwayPoses = new Vector3[30];

	private static Vector3[] _laggingSwayRots = new Vector3[30];

	private static int _laggingSwayIterator = 0;

	private static Vector3 _lagginPos = Vector3.zero;

	private static Vector3 _lagginPosSmoothed = Vector3.zero;

	private static Vector3 _lagginRot = Vector3.zero;

	private static Vector3 _lagginRotSmoothed = Vector3.zero;

	private static Vector3 _finalPos = Vector3.zero;

	private static Vector3 _finalPosSmoothed = Vector3.zero;

	private static Vector3 _finalRot = Vector3.zero;

	private static Vector3 _finalRotSmoothed = Vector3.zero;

	public static void UpdateLerp(float deltaTime)
	{
		bool flag = WeaponController.HasCheekWeld();
		float value = PlayerMotionController.HorizontalRotationDelta * PrimeMover.WeaponSwayMulti.Value;
		float value2 = PrimeMover.NewSwayRotDeltaClamp.Value;
		value = Mathf.Clamp(value, 0f - value2, value2);
		float num = (flag ? (-1f) : 0.5f);
		float num2 = ((flag && PlayerMotionController.IsAiming) ? 0f : 1f);
		float num3 = ((!flag && !WeaponController.IsPistol && PlayerMotionController.IsAiming) ? 0.7f : 1f);
		float num4 = ((flag && !PlayerMotionController.IsAiming) ? 0.7f : 1f);
		float num5 = ((!flag && PlayerMotionController.IsAiming) ? 1.25f : 1f);
		float num6 = ((flag && PlayerMotionController.IsAiming) ? (-0.25f) : 1f);
		float num7 = ((flag && PlayerMotionController.IsAiming) ? 2f : 1f);
		float num8 = ((!flag) ? 0.5f : 1f);
		float weaponMulti = WeaponController.GetWeaponMulti(getInverse: true);
		float efficiencyModifierInverse = EfficiencyController.EfficiencyModifierInverse;
		float num9 = weaponMulti * efficiencyModifierInverse;
		float num10 = ((!flag) ? (-1f) : 0f);
		float num11 = (PlayerMotionController.IsAiming ? 0.5f : 1f);
		float num12 = (WeaponController.IsPistol ? 2f : 1f);
		float b = value * num * num2 * num3 * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier;
		float b2 = Mathf.Abs(value) * num10 * num11 * num12;
		float value3 = PrimeMover.NewSwayPositionDTMulti.Value;
		float value4 = PrimeMover.NewSwayRotationDTMulti.Value;
		_lerpPosHorizontal = Mathf.Lerp(_lerpPosHorizontal, b, deltaTime * num9 * num4 * num5 * value3);
		_lerpPosVertical = Mathf.Lerp(_lerpPosVertical, b2, deltaTime * num9 * PrimeMover.NewSwayWpnUnstockedDropSpeed.Value);
		float value5 = value * num6 * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier;
		float num13 = (PlayerMotionController.IsAiming ? (PrimeMover.NewSwayADSRotClamp.Value * value2) : 1f);
		value5 = Mathf.Clamp(value5, 0f - num13, num13);
		_lerpRot = Mathf.Lerp(_lerpRot, value5, deltaTime * num9 * num7 * num8 * value4);
		float value6 = PrimeMover.NewSwayRotFinalClamp.Value;
		_lerpRot = Mathf.Clamp(_lerpRot, 0f - value6, value6);
		float b3 = (PlayerMotionController.IsAiming ? 0f : (PrimeMover.WeaponCantValue.Value * 0.1f));
		_weaponTiltLerp = Mathf.Lerp(_weaponTiltLerp, b3, deltaTime * 20f);
		float num14 = (PlayerMotionController.IsAiming ? 0f : (PlayerMotionController.LeanNormal * PrimeMover.LeanExtraVerticalMulti.Value * WeaponController.GetWeaponMulti(getInverse: false)));
		num14 *= -1f;
		if (AnimStateController.IsLeftShoulder)
		{
			num14 *= -1f;
		}
		_leanVerticalLerp = Mathf.Lerp(_leanVerticalLerp, num14, deltaTime * 10f * EfficiencyController.EfficiencyModifierInverse);
		float num15 = ((WeaponController.IsStocked && PlayerMotionController.IsAiming) ? 0.2f : 1f);
		float b4 = PlayerMotionController.RotationDelta * PrimeMover.NewSwayWpnDropFromRotMulti.Value * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier * num15;
		_vertDropFromRotLerp = Mathf.Lerp(_vertDropFromRotLerp, b4, deltaTime * PrimeMover.NewSwayWpnUnstockedDropSpeed.Value);
		float verticalRotationDelta = PlayerMotionController.VerticalRotationDelta;
		float num16 = ((verticalRotationDelta < 0f) ? (-1f) : 1f);
		float value7 = (PlayerMotionController.IsAiming ? 0f : (verticalRotationDelta * num16 * PrimeMover.HyperVerticalMulti.Value * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier));
		float value8 = PrimeMover.HyperVerticalClamp.Value;
		value7 = Mathf.Clamp(value7, 0f - value8, value8);
		float num17 = PrimeMover.HyperVerticalDT.Value * RealismWrapper.WeaponBalanceMulti;
		_hyperVerticalLerp = Mathf.Lerp(_hyperVerticalLerp, value7, deltaTime * num17);
		ProcessLagginSway();
	}

	private static void ProcessLagginSway()
	{
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Invalid comparison between Unknown and I4
		_laggingSwayPoses[_laggingSwayIterator] = _posSmoothed;
		_laggingSwayRots[_laggingSwayIterator] = _rotSmoothed;
		_laggingSwayIterator++;
		if (_laggingSwayIterator > 29)
		{
			_laggingSwayIterator = 0;
		}
		float num = Mathf.Clamp(EfficiencyController.EfficiencyModifier, 1f, 10f);
		float num2 = WeaponController.GetWeaponMulti(getInverse: false) * RealismWrapper.WeaponBalanceMulti * num * PrimeMover.LaggingSwayMulti.Value;
		num2 *= (((int)StanceController.CurrentStance == 2) ? 0.5f : 1f);
		num2 = Mathf.Clamp(num2, 1f, PrimeMover.LaggingSwayClamp.Value);
		int num3 = _laggingSwayIterator - Mathf.RoundToInt(num2);
		if (num3 < 0)
		{
			num3 = _lagginSwaySetSize + num3;
		}
		_lagginPos = _laggingSwayPoses[num3];
		_lagginRot = _laggingSwayRots[num3];
	}

	public static Vector3 GetNewSwayPosition()
	{
		float num = 18f;
		_posSmoothed = Vector3.Lerp(b: new Vector3(_lerpPosHorizontal, _lerpPosVertical, 0f), a: _posSmoothed, t: PrimeMover.Instance.DeltaTime * num);
		_lagginPosSmoothed = Vector3.Lerp(_lagginPosSmoothed, _lagginPos, PrimeMover.Instance.DeltaTime * num);
		_finalPos = Vector3.Lerp(_posSmoothed, _lagginPosSmoothed, PrimeMover.LaggingSwayNorm.Value);
		_finalPosSmoothed = Vector3.Lerp(_finalPosSmoothed, _finalPos, PrimeMover.Instance.DeltaTime * PrimeMover.NewSwayFinalLerpSpeed.Value);
		if (!PrimeMover.IsWeaponSway.Value)
		{
			return Vector3.zero;
		}
		if (AnimStateController.IsBlindfire)
		{
			return Vector3.zero;
		}
		Vector3 zero = Vector3.zero;
		zero.x = _finalPosSmoothed.x * PrimeMover.NewSwayPositionMulti.Value * WeaponController.GetWeaponMulti(getInverse: false);
		zero.y = _finalPosSmoothed.y * PrimeMover.NewSwayWpnUnstockedDropValue.Value * WeaponController.GetWeaponMulti(getInverse: false);
		return zero;
	}

	public static Quaternion GetNewSwayRotation()
	{
		float num = 18f;
		Vector3 b = new Vector3(_lerpPosHorizontal, _lerpPosVertical, 0f);
		b.x = _leanVerticalLerp + _vertDropFromRotLerp + _hyperVerticalLerp;
		b.y = _weaponTiltLerp;
		b.z = _lerpRot;
		_rotSmoothed = Vector3.Lerp(_rotSmoothed, b, PrimeMover.Instance.DeltaTime * num);
		_lagginRotSmoothed = Vector3.Lerp(_lagginRotSmoothed, _lagginRot, PrimeMover.Instance.DeltaTime * num);
		_finalRot = Vector3.Lerp(_rotSmoothed, _lagginRotSmoothed, PrimeMover.LaggingSwayNorm.Value);
		_finalRotSmoothed = Vector3.Lerp(_finalRotSmoothed, _finalRot, PrimeMover.Instance.DeltaTime * PrimeMover.NewSwayFinalLerpSpeed.Value);
		if (!PrimeMover.IsWeaponSway.Value)
		{
			return Quaternion.identity;
		}
		if (AnimStateController.IsBlindfire)
		{
			return Quaternion.identity;
		}
		Quaternion identity = Quaternion.identity;
		identity.x = _finalRotSmoothed.x;
		identity.y = _finalRotSmoothed.y;
		identity.z = _finalRotSmoothed.z * PrimeMover.NewSwayRotationMulti.Value * WeaponController.GetWeaponMulti(getInverse: false);
		return identity;
	}
}
