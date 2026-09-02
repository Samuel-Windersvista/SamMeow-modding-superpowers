using EFT;
using UnityEngine;

namespace TarkovIRL;

internal class PlayerMotionController
{
	public enum EPlayerDir
	{
		FWD,
		BWD,
		LEFT,
		RIGHT,
		FWDLEFT,
		FWDRIGHT,
		BWDLEFT,
		BWDRIGHT,
		NONE
	}

	private static EPlayerDir _dir;

	private static Vector2 _playerRotLastFrame = Vector2.zero;

	private static Vector3 _playerPosLastFrame = Vector3.zero;

	private static float _playerRotationHistory = 0f;

	private static float _playerRotationAvg = 0f;

	private static float _horizontalRotationHistory = 0f;

	private static float _horizontalRotationValue = 0f;

	private static float _verticalRotationHistory = 0f;

	private static float _verticalRotationValue = 0f;

	private static bool _playerMoving = false;

	private static float _verticalAvg = 0f;

	private static float _playerSpeed = 0f;

	private static float _leanNormalized = 0f;

	private static float _armStam = 0f;

	public static bool IsAiming = false;

	public static bool IsSprinting = false;

	public static bool IsProne = false;

	public static bool IsHoldingBreath = false;

	public static bool IsAugmentedBreath = false;

	private static Vector3 _lastPositionRecorded = Vector3.zero;

	private static Vector2 _lastRotationRecorded = Vector2.zero;

	public static float VerticalTrend => _verticalAvg;

	public static bool IsPlayerMovement => _playerMoving;

	public static float RotationDelta => _playerRotationAvg;

	public static float HorizontalRotationDelta => _horizontalRotationValue;

	public static float VerticalRotationDelta => _verticalRotationValue;

	public static float LeanNormal => _leanNormalized;

	public static EPlayerDir Direction => _dir;

	public static float ArmStamNorm => _armStam;

	public static void UpdateMovementMeasurementsInFDT(float fdt)
	{
		UpdateIsMovingBool(_lastPositionRecorded);
		UpdateRotationEngine(_lastRotationRecorded, fdt);
	}

	public static void UpdateMovementInformation(Player player)
	{
		_lastPositionRecorded = player.Position;
		_lastRotationRecorded = player.Rotation;
		IsAiming = player.ProceduralWeaponAnimation.IsAiming;
		IsProne = player.IsInPronePose;
		IsSprinting = player.IsSprintEnabled;
		_playerSpeed = player.Speed;
		_leanNormalized = player.MovementContext.Tilt / 5f;
		_armStam = player.Physical.HandsStamina.NormalValue;
		UpdateMovementDirection(player.InputDirection);
	}

	private static void UpdateIsMovingBool(Vector3 position)
	{
		float num = Vector3.Distance(position, _playerPosLastFrame);
		if (num > PrimeMover.MotionTrackingThreshold.Value)
		{
			_playerMoving = true;
		}
		else
		{
			_playerMoving = false;
		}
		_playerPosLastFrame = position;
	}

	private static void UpdateRotationEngine(Vector2 newRot, float dt)
	{
		_verticalAvg = ((newRot.y > _playerRotLastFrame.y) ? 1f : (-1f));
		float num = Vector2.Distance(newRot, _playerRotLastFrame) * dt;
		float num2 = newRot.x - _playerRotLastFrame.x;
		float num3 = newRot.y - _playerRotLastFrame.y;
		num2 *= dt;
		num3 *= dt;
		if (num2 < -1f)
		{
			num2 = 0f;
		}
		if (num2 > 1f)
		{
			num2 = 0f;
		}
		_playerRotationHistory += num;
		_playerRotationHistory -= _playerRotationAvg;
		_playerRotationHistory = Mathf.Clamp(_playerRotationHistory, 0f, PrimeMover.RotationHistoryClamp.Value);
		_playerRotationAvg = _playerRotationHistory * dt * PrimeMover.RotationAverageDTMulti.Value;
		_horizontalRotationHistory += num2;
		_horizontalRotationHistory -= _horizontalRotationValue;
		_horizontalRotationValue = _horizontalRotationHistory * dt * PrimeMover.RotationAverageDTMulti.Value;
		_verticalRotationHistory += num3;
		_verticalRotationHistory -= _verticalRotationValue;
		_verticalRotationValue = _verticalRotationHistory * dt * PrimeMover.RotationAverageDTMulti.Value;
		_playerRotLastFrame = newRot;
	}

	public static float GetNormalSpeed()
	{
		return _playerSpeed / 0.6f;
	}

	private static void UpdateMovementDirection(Vector3 playerInput)
	{
		bool flag = playerInput.y > 0f;
		bool flag2 = playerInput.y < 0f;
		bool flag3 = playerInput.x < 0f;
		bool flag4 = playerInput.x > 0f;
		if (!_playerMoving)
		{
			_dir = EPlayerDir.NONE;
		}
		else if (flag && !flag3 && !flag4)
		{
			_dir = EPlayerDir.FWD;
		}
		else if (flag && flag3)
		{
			_dir = EPlayerDir.FWDLEFT;
		}
		else if (flag && flag4)
		{
			_dir = EPlayerDir.FWDRIGHT;
		}
		else if (flag2 && !flag3 && !flag4)
		{
			_dir = EPlayerDir.BWD;
		}
		else if (flag2 && flag3)
		{
			_dir = EPlayerDir.BWDLEFT;
		}
		else if (flag2 && flag4)
		{
			_dir = EPlayerDir.BWDRIGHT;
		}
		else if (flag3)
		{
			_dir = EPlayerDir.LEFT;
		}
		else if (flag4)
		{
			_dir = EPlayerDir.RIGHT;
		}
		else
		{
			_dir = EPlayerDir.NONE;
		}
	}
}
