using UnityEngine;

namespace TarkovIRL;

internal class DirectionalSwayController
{
	private static float _lateralPosLerp;

	private static float _projectedPosLerp;

	private static float _lateralRotLerp;

	private static float _verticalRotLerp;

	public static void UpdateDirectionalSwayLerp(float dt)
	{
		float value = PrimeMover.DirectionalSwayLateralPosValue.Value;
		float value2 = PrimeMover.DirectionalSwayProjectedPosValue.Value;
		float value3 = PrimeMover.DirectionalSwayLateralRotValue.Value;
		float value4 = PrimeMover.DirectionalSwayVerticalRotValue.Value;
		PlayerMotionController.EPlayerDir direction = PlayerMotionController.Direction;
		float num = 0f;
		float num2 = 0f;
		if (direction == PlayerMotionController.EPlayerDir.FWD || direction == PlayerMotionController.EPlayerDir.BWD)
		{
			num = 0f;
		}
		else
		{
			switch (direction)
			{
			case PlayerMotionController.EPlayerDir.LEFT:
				num = -1f;
				break;
			case PlayerMotionController.EPlayerDir.FWDLEFT:
				num = -0.5f;
				break;
			case PlayerMotionController.EPlayerDir.BWDLEFT:
				num = 0.5f;
				break;
			case PlayerMotionController.EPlayerDir.RIGHT:
				num = 1f;
				break;
			case PlayerMotionController.EPlayerDir.FWDRIGHT:
				num = 0.5f;
				break;
			case PlayerMotionController.EPlayerDir.BWDRIGHT:
				num = -0.5f;
				break;
			case PlayerMotionController.EPlayerDir.NONE:
				num = 0f;
				break;
			}
		}
		switch (direction)
		{
		case PlayerMotionController.EPlayerDir.FWD:
			num2 = 1f;
			break;
		case PlayerMotionController.EPlayerDir.FWDLEFT:
			num2 = 0.5f;
			break;
		case PlayerMotionController.EPlayerDir.FWDRIGHT:
			num2 = 0.5f;
			break;
		case PlayerMotionController.EPlayerDir.BWD:
			num2 = -1f;
			break;
		case PlayerMotionController.EPlayerDir.BWDLEFT:
			num2 = -0.5f;
			break;
		case PlayerMotionController.EPlayerDir.BWDRIGHT:
			num2 = -0.5f;
			break;
		}
		float num3 = Mathf.Clamp(PlayerMotionController.GetNormalSpeed(), 0.1f, 1f);
		float num4 = (PlayerMotionController.IsAiming ? PrimeMover.DirectionalSwayLerpOnAds.Value : 1f);
		num *= num3 * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier * num4;
		num2 *= num3 * WeaponController.GetWeaponMulti(getInverse: false) * EfficiencyController.EfficiencyModifier * num4;
		float b = value * num * PrimeMover.DirectionalSwayMulti.Value;
		float b2 = value2 * num * PrimeMover.DirectionalSwayMulti.Value;
		float b3 = value3 * num * PrimeMover.DirectionalSwayMulti.Value;
		float b4 = value4 * num2 * PrimeMover.DirectionalSwayMulti.Value;
		if (!WeaponController.HasCheekWeld())
		{
			b = 0f;
			b3 = 0f;
			b2 = 0f;
			b4 = 0f;
		}
		float num5 = Mathf.Clamp(PlayerMotionController.GetNormalSpeed(), 0.5f, 1f);
		float t = dt * PrimeMover.DirectionalSwayLerpSpeed.Value * num5;
		_lateralPosLerp = Mathf.Lerp(_lateralPosLerp, b, t);
		_projectedPosLerp = Mathf.Lerp(_projectedPosLerp, b2, t);
		_lateralRotLerp = Mathf.Lerp(_lateralRotLerp, b3, t);
		_verticalRotLerp = Mathf.Lerp(_verticalRotLerp, b4, t);
	}

	public static void GetDirectionalSway(out Vector3 position, out Quaternion rotation)
	{
		if (!PrimeMover.IsDirectionalSway.Value)
		{
			position = Vector3.zero;
			rotation = Quaternion.identity;
			return;
		}
		position = Vector3.zero;
		position.x = _lateralPosLerp;
		position.z = _projectedPosLerp;
		rotation = Quaternion.identity;
		rotation.z = _lateralRotLerp;
		rotation.x = _verticalRotLerp;
	}
}
