using System;
using UnityEngine;

namespace TarkovIRL;

internal class AugmentedReloadController
{
	private static readonly float _IntoReloadX = 17f;

	private static readonly float _IntoReloadY = 4f;

	private static readonly float _IntoReloadZ = 5f;

	private static readonly float _OutReloadX = 7f;

	private static readonly float _OutReloadY = 2f;

	private static readonly float _OutReloadZ = -3f;

	public static bool _AugmentedModeOn = false;

	private static AnimStateController.EWeaponState _state;

	private static AnimStateController.EWeaponState _stateLastFrame;

	private static ObjectInHandsAnimator _animator = null;

	private static readonly int _animatorLayer = 1;

	public static Vector3 GetAugmentedReloadHeadOffset()
	{
		if (!PrimeMover.IsAugmentedReload.Value)
		{
			return Vector3.zero;
		}
		_state = AnimStateController.WeaponState;
		if (!_AugmentedModeOn)
		{
			return Vector3.zero;
		}
		if (_state == AnimStateController.EWeaponState.INTO_RELOAD)
		{
			return new Vector3(_OutReloadX, _OutReloadY, _OutReloadZ);
		}
		if (_state == AnimStateController.EWeaponState.MID_RELOAD)
		{
			return new Vector3(_IntoReloadX, _IntoReloadY, _IntoReloadZ);
		}
		if (_state == AnimStateController.EWeaponState.MID_RELOAD_2)
		{
			return new Vector3(_OutReloadX, _OutReloadY, _OutReloadZ);
		}
		if (_state == AnimStateController.EWeaponState.CHECK_MAG)
		{
			return new Vector3(_OutReloadX, _OutReloadY, _OutReloadZ);
		}
		return Vector3.zero;
	}

	public static void ToggleAugmentedMode()
	{
		if (AugmentedSwitchOpen())
		{
			_AugmentedModeOn = !_AugmentedModeOn;
		}
		else
		{
			_AugmentedModeOn = false;
		}
	}

	public static void RefreshAnimator(ObjectInHandsAnimator animator)
	{
		_animator = animator;
	}

	public static void Update()
	{
		if (PrimeMover.IsAugmentedReload.Value && _animator != null)
		{
			UpdateSpeed();
			if (_stateLastFrame != AnimStateController.EWeaponState.INTO_RELOAD && _state == AnimStateController.EWeaponState.INTO_RELOAD && PrimeMover.IsAugmentedReloadDefault.Value)
			{
				_AugmentedModeOn = true;
			}
			_stateLastFrame = _state;
		}
	}

	private static void UpdateSpeed()
	{
		if (!AugmentedSwitchOpen())
		{
			_AugmentedModeOn = false;
		}
		else
		{
			SetAnimSpeed(GetReloadSpeedFromContext());
		}
	}

	private static void SetAnimSpeed(float speed)
	{
		if (_animator == null)
		{
			return;
		}
		try
		{
			_animator.SetAnimationSpeed(speed);
		}
		catch (Exception)
		{
			_animator = null;
		}
	}

	private static float GetReloadSpeedFromContext()
	{
		float num = (PlayerMotionController.IsSprinting ? PrimeMover.AugmentedReloadSprintingDebuff.Value : 1f);
		float num2 = (_AugmentedModeOn ? PrimeMover.AugmentedReloadSpeed.Value : 1f);
		float num3;
		if (_animator != null && _state == AnimStateController.EWeaponState.MID_RELOAD)
		{
			try
			{
				float normalizedTime = _animator.Animator.GetCurrentAnimatorStateInfo(_animatorLayer).normalizedTime;
				num3 = ((normalizedTime < 1f) ? PrimeMover.Instance.SlowReloadCurve.Evaluate(normalizedTime) : 1f);
			}
			catch (NullReferenceException arg)
			{
				TIRLUtils.LogError($"anim layer {_animatorLayer} returned null -- {arg}");
				num3 = 1f;
			}
		}
		else
		{
			num3 = 1f;
		}
		float num4 = ((_state == AnimStateController.EWeaponState.CHECK_MAG) ? RealismWrapper.GetRealismCheckMagSpeed() : RealismWrapper.GetRealismReloadSpeed());
		return num * num2 * num4 * num3;
	}

	private static bool AugmentedSwitchOpen()
	{
		return _state == AnimStateController.EWeaponState.INTO_RELOAD || _state == AnimStateController.EWeaponState.MID_RELOAD || _state == AnimStateController.EWeaponState.MID_RELOAD_2 || _state == AnimStateController.EWeaponState.OUT_OF_RELOAD || _state == AnimStateController.EWeaponState.RELOAD_FAST || _state == AnimStateController.EWeaponState.CHECK_MAG;
	}
}
