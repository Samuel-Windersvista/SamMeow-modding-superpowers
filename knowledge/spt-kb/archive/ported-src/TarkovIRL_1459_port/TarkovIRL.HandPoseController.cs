using EFT;
using UnityEngine;

namespace TarkovIRL;

internal class HandPoseController
{
	private static readonly float _LerpRate = 4f;

	private static readonly float _OffsetTargetMultiY = 0.03f;

	private static readonly float _OffsetTargetMultiZ = -0.05f;

	private static readonly float _ChangePoseModifier = 0.01f;

	private static float _currentLerpRate = 0f;

	private static float _offsetTargetY = 0f;

	private static float _offsetTargetZ = 0f;

	private static float _offsetTargetYLerp = 0f;

	private static float _offsetTargetZLerp = 0f;

	private static Vector3 _poseShiftVectorVertical = Vector3.zero;

	private static Vector3 _poseShiftVectorVerticalLerped = Vector3.zero;

	private static Quaternion _poseShiftVectorRotation = Quaternion.identity;

	private static Quaternion _poseShiftVectorRotationLerped = Quaternion.identity;

	private static float _poseLevelLastFrame = 0f;

	private static float _poseDifference = 0f;

	private static float _changingPoseCycle = 0f;

	private static float _changingPoseCycle2 = 0f;

	private static bool _isChangingPose = false;

	private static void VerticalPoseUpdate(float dt)
	{
		_offsetTargetYLerp = Mathf.Lerp(_offsetTargetYLerp, _offsetTargetY, dt * _currentLerpRate);
		_offsetTargetZLerp = Mathf.Lerp(_offsetTargetZLerp, _offsetTargetZ, dt * _currentLerpRate);
	}

	private static void ChangePoseUpdate(Player player)
	{
		if (_isChangingPose)
		{
			float deltaTime = PrimeMover.Instance.DeltaTime;
			float num = Mathf.Abs(_poseDifference);
			bool flag = player.HealthController.IsBodyPartBroken(EBodyPart.LeftArm);
			bool flag2 = player.HealthController.IsBodyPartBroken(EBodyPart.RightArm);
			float num2 = 1f;
			num2 *= (flag ? 0.5f : 1f);
			num2 *= (flag2 ? 0.5f : 1f);
			float num3 = 1f + (1f - num) * num2;
			bool flag3 = _poseDifference < 0f;
			float num4 = (flag3 ? 1.5f : 2f);
			float weaponMulti = WeaponController.GetWeaponMulti(getInverse: false);
			float num5 = WeaponController.GetWeaponMulti(getInverse: true) * 2.2f;
			float num6 = (flag3 ? 2f : 1.1f);
			float efficiencyModifier = EfficiencyController.EfficiencyModifier;
			_changingPoseCycle += deltaTime * num3 * num6 * num5;
			_changingPoseCycle2 += deltaTime * num3 * num6 * num5 * 0.7f;
			if (_changingPoseCycle >= 1f && _changingPoseCycle2 >= 1f)
			{
				_isChangingPose = false;
				_changingPoseCycle = 0f;
				_changingPoseCycle2 = 0f;
				return;
			}
			float num7 = ((_poseDifference < 0f) ? 1f : 0.5f);
			float num8 = (flag3 ? PrimeMover.Instance.PoseChangeCurve.Evaluate(_changingPoseCycle) : PrimeMover.Instance.PoseChangeCurveUp.Evaluate(_changingPoseCycle));
			float num9 = (flag3 ? (PrimeMover.Instance.PoseChangeDownRadialCurve.Evaluate(_changingPoseCycle) * 5f) : 0f);
			float num10 = (flag3 ? 0f : PrimeMover.Instance.PoseChangeDownRadialCurve.Evaluate(_changingPoseCycle2));
			float num11 = num * _ChangePoseModifier * num7 * num4 * weaponMulti * efficiencyModifier;
			float y = num8 * num11;
			float z = num9 * num11;
			float num12 = 1f - PlayerMotionController.ArmStamNorm;
			float x = num10 * num11 * 7f * num12;
			_poseShiftVectorVertical = new Vector3(0f, y, z);
			_poseShiftVectorRotation = TIRLUtils.GetQuatFromV3(new Vector3(x, 0f, 0f));
		}
		else
		{
			_poseShiftVectorVertical = Vector3.zero;
			_poseShiftVectorRotation = Quaternion.identity;
		}
		_poseShiftVectorVerticalLerped = Vector3.Lerp(_poseShiftVectorVerticalLerped, _poseShiftVectorVertical, PrimeMover.Instance.DeltaTime * 8f);
		_poseShiftVectorRotationLerped = Quaternion.Lerp(_poseShiftVectorRotationLerped, _poseShiftVectorRotation, PrimeMover.Instance.DeltaTime * 8f);
	}

	public static Vector3 GetModifiedHandPosWithPose(Player player)
	{
		VerticalPoseUpdate(player.DeltaTime);
		_currentLerpRate = _LerpRate;
		float poseLevel = player.PoseLevel;
		_offsetTargetY = (1f - poseLevel) * _OffsetTargetMultiY;
		_offsetTargetZ = (1f - poseLevel) * _OffsetTargetMultiZ;
		if (player.ProceduralWeaponAnimation.IsAiming)
		{
			_offsetTargetY = 0f;
			_offsetTargetZ = 0f;
			_currentLerpRate = _LerpRate * 2f;
		}
		return new Vector3(0f, _offsetTargetYLerp, _offsetTargetZLerp);
	}

	public static Vector3 GetModifiedHandPosWithPoseChange(Player player)
	{
		ChangePoseUpdate(player);
		Vector3 poseShiftVectorVerticalLerped = _poseShiftVectorVerticalLerped;
		float poseLevel = player.PoseLevel;
		if (poseLevel != _poseLevelLastFrame)
		{
			_poseDifference = poseLevel - _poseLevelLastFrame;
			if (!_isChangingPose)
			{
				_isChangingPose = true;
			}
		}
		if (!player.ProceduralWeaponAnimation.IsAiming)
		{
			poseShiftVectorVerticalLerped.y *= 3f;
		}
		_poseLevelLastFrame = poseLevel;
		return poseShiftVectorVerticalLerped;
	}

	public static Quaternion GetModifiedHandRotWithPoseChange()
	{
		return _poseShiftVectorRotationLerped;
	}
}
