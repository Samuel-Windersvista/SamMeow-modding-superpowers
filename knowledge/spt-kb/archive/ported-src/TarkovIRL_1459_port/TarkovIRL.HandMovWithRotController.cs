using EFT;
using UnityEngine;

namespace TarkovIRL;

public static class HandMovWithRotController
{
	private static readonly float _RotPullInValue = 0.07f;

	private static readonly float _LerpRate = 8f;

	private static readonly float _StockMovementAddedPosValue = 0.005f;

	private static float _rotPullInTarget = 0f;

	private static float _rotPullInLerp = 0f;

	private static float _stockedMovementAddedPosTarget = 0f;

	private static float _stockedMovementAddedPosLerp = 0f;

	private static float _stockedMovementAddedPosSmoothed = 0f;

	public static Vector3 GetModifiedHandPosZMovement(Player player)
	{
		float deltaTime = player.DeltaTime;
		if (player.ProceduralWeaponAnimation.IsAiming)
		{
			_rotPullInTarget = 0f;
		}
		else if (AnimStateController.IsBlindfire)
		{
			_rotPullInTarget = 0f;
		}
		else
		{
			float value = Mathf.Abs(PlayerMotionController.HorizontalRotationDelta * 100f);
			value = Mathf.Clamp01(value);
			float num = (WeaponController.HasCheekWeld() ? (_RotPullInValue * 0.75f) : _RotPullInValue);
			num *= value;
			_rotPullInTarget = num;
		}
		_rotPullInLerp = Mathf.Lerp(_rotPullInLerp, _rotPullInTarget, deltaTime * 0.5f);
		float num2 = PrimeMover.Instance.SmoothEdgesCurve.Evaluate(_rotPullInLerp / _RotPullInValue) * _RotPullInValue;
		return new Vector3(0f, 0f, 0f - num2);
	}

	public static Vector3 GetModifiedHandPosForLoweredWeapon(Player player)
	{
		float deltaTime = player.DeltaTime;
		if (AnimStateController.IsSideStep)
		{
			_stockedMovementAddedPosTarget = _StockMovementAddedPosValue * 0.2f * WeaponController.GetWeaponMulti(getInverse: false);
		}
		else if (!WeaponController.HasCheekWeld() && !WeaponController.IsPistol && PlayerMotionController.IsPlayerMovement)
		{
			_stockedMovementAddedPosTarget = _StockMovementAddedPosValue * WeaponController.GetWeaponMulti(getInverse: false);
		}
		else
		{
			_stockedMovementAddedPosTarget = 0f;
		}
		_stockedMovementAddedPosLerp = Mathf.Lerp(_stockedMovementAddedPosLerp, _stockedMovementAddedPosTarget, deltaTime * _LerpRate);
		_stockedMovementAddedPosSmoothed = Mathf.Lerp(_stockedMovementAddedPosSmoothed, _stockedMovementAddedPosLerp, deltaTime);
		return new Vector3(0f, 0f - _stockedMovementAddedPosSmoothed, 0f);
	}
}
