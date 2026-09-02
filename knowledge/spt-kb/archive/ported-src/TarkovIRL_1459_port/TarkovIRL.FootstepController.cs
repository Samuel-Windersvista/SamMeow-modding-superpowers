using EFT;
using UnityEngine;

namespace TarkovIRL;

internal static class FootstepController
{
	private static readonly float _UpdateMulti = 12f;

	private static readonly float _StepIntensityMulti = 0.0025f;

	private static float _stepLerp = 0f;

	private static float _stepLerpSmoothed = 0f;

	private static float _currentSpeed = 0f;

	private static float _sideToSideRotationLerp = 0f;

	private static float _sideToSidePositionLerp = 0f;

	private static float _sideToSideRotationSmoothingLerp = 0f;

	private static float _sideToSidePositionSmoothingLerp = 0f;

	private static bool _currentStepLeft = false;

	private static bool _movementLastFrame = false;

	public static Vector3 GetModifiedHandPosFootstep
	{
		get
		{
			float num = 0.2f + _currentSpeed;
			float y = PrimeMover.Instance.FootStepCurve.Evaluate(_stepLerpSmoothed) * _StepIntensityMulti * PrimeMover.FootstepIntesnityMulti.Value * (EfficiencyController.EfficiencyModifier * 0.5f) * num;
			return new Vector3(0f, y, 0f);
		}
	}

	public static void UpdateStep(float dt)
	{
		PlayerMotionController.EPlayerDir direction = PlayerMotionController.Direction;
		float num = ((direction == PlayerMotionController.EPlayerDir.FWD) ? 1f : ((direction != PlayerMotionController.EPlayerDir.FWDLEFT && direction != PlayerMotionController.EPlayerDir.FWDRIGHT) ? 0f : 0.35f));
		float num2 = 1f + _currentSpeed;
		_stepLerp = Mathf.Lerp(_stepLerp, 1f, dt * _UpdateMulti * num2 * PrimeMover.FootstepLerpMulti.Value);
		_sideToSideRotationLerp += dt * PrimeMover.SideToSideRotationDTMulti.Value * num2;
		_sideToSidePositionLerp += dt * PrimeMover.SideToSidePositionDTMulti.Value * num2;
		float num3 = PrimeMover.Instance.SideToSideCurve.Evaluate(_sideToSideRotationLerp) * PrimeMover.SideToSideSwayMulti.Value * num;
		num3 *= (_currentStepLeft ? 1f : (-1f));
		float num4 = PrimeMover.Instance.FootStepCurve.Evaluate(_sideToSidePositionLerp) * PrimeMover.SideToSideSwayMulti.Value * num;
		num4 *= (_currentStepLeft ? (-1f) : 1f);
		_stepLerpSmoothed = Mathf.Lerp(_stepLerpSmoothed, _stepLerp, dt * 13f);
		_sideToSideRotationSmoothingLerp = Mathf.Lerp(_sideToSideRotationSmoothingLerp, num3, dt * 7f);
		_sideToSidePositionSmoothingLerp = Mathf.Lerp(_sideToSidePositionSmoothingLerp, num4, dt * 7f);
	}

	public static void NewStep(Player player)
	{
		_stepLerp = 0f;
		_currentSpeed = player.Speed;
		_sideToSideRotationLerp = 0f;
		_sideToSidePositionLerp = 0f;
		if (!PlayerMotionController.IsPlayerMovement && !_movementLastFrame)
		{
			_currentStepLeft = true;
		}
		else
		{
			_currentStepLeft = !_currentStepLeft;
		}
		_movementLastFrame = PlayerMotionController.IsPlayerMovement;
	}

	public static Quaternion GetSideToSideRotation()
	{
		Quaternion identity = Quaternion.identity;
		identity.z = _sideToSideRotationSmoothingLerp * PlayerMotionController.GetNormalSpeed();
		identity.z *= (PlayerMotionController.IsAiming ? 0.4f : 1f);
		return identity;
	}

	public static Vector3 GetSideToSidePosition()
	{
		Vector3 zero = Vector3.zero;
		zero.x = _sideToSidePositionSmoothingLerp * PlayerMotionController.GetNormalSpeed();
		zero.x *= (PlayerMotionController.IsAiming ? 0.4f : 1f);
		return zero;
	}
}
