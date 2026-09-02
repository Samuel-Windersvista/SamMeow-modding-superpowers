using EFT;
using EFT.InputSystem;
using EFT.InventoryLogic;
using UnityEngine;

namespace TarkovIRL;

internal class WeaponSelectionController
{
	public enum EWeaponSelection
	{
		SLING,
		SHOULDER,
		PISTOL,
		OTHER
	}

	private static EWeaponSelection _lastSelectedWeapon;

	private static Vector3 _posLerp = Vector3.zero;

	private static Vector3 _orderEndPos = Vector3.zero;

	private static Vector3 _presentStartPos = Vector3.zero;

	private static Vector3 _rotLerp = Vector3.zero;

	private static Vector3 _orderEndRot = Vector3.zero;

	private static Vector3 _presentStartRot = Vector3.zero;

	private static Vector3 _posLerpHistory = Vector3.zero;

	private static Vector3 _rotLerpHistory = Vector3.zero;

	private static Vector3 _posLerpSmoothed = Vector3.zero;

	private static Vector3 _rotLerpSmoothed = Vector3.zero;

	private static readonly Vector3 Pistol_Start_Pos = new Vector3(0.05f, 0.1f, -0.2f);

	private static readonly Vector3 Pistol_Start_Rot = new Vector3(0.1f, -0.2f, 0.05f);

	private static readonly Vector3 Pistol_End_Pos = Vector3.zero;

	private static readonly Vector3 Pistol_End_Rot = Vector3.zero;

	private static readonly Vector3 Shoulder_Start_Pos = new Vector3(0.27f, -0.5f, -0.12f);

	private static readonly Vector3 Shoulder_Start_Rot = new Vector3(-0.4f, -0.56f, 0f);

	private static readonly Vector3 Shoulder_End_Pos = new Vector3(0.25f, -0.5f, -0.19f);

	private static readonly Vector3 Shoulder_End_Rot = new Vector3(-0.4f, -0.56f, 0f);

	private static readonly Vector3 Sling_Start_Pos = new Vector3(0.06f, -0.16f, -0.1f);

	private static readonly Vector3 Sling_Start_Rot = new Vector3(0f, -0.75f, -0.24f);

	private static readonly Vector3 Sling_End_Pos = new Vector3(0.37f, -0.16f, -0.46f);

	private static readonly Vector3 Sling_End_Rot = new Vector3(0f, 1f, -0.24f);

	private static readonly float TransformSmoothingDTMulti = 12f;

	private static Player _player = null;

	private static AnimationCurve _animSpeedCurvePhase1 = new AnimationCurve();

	private static AnimationCurve _animSpeedCurvePhase2 = new AnimationCurve();

	private static AnimationCurve _Flatcurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));

	private static bool _transitionFirstFrame = false;

	private static bool _transitionWindowOpen = false;

	private static AnimStateController.EWeaponState _previousState;

	public static void UpdateAnimationPump(float dt)
	{
		if (!PrimeMover.IsWeaponTrans.Value)
		{
			return;
		}
		_posLerpHistory.x += _posLerp.x;
		_posLerpHistory.y += _posLerp.y;
		_posLerpHistory.z += _posLerp.z;
		_posLerpHistory.x -= _posLerpSmoothed.x;
		_posLerpHistory.y -= _posLerpSmoothed.y;
		_posLerpHistory.z -= _posLerpSmoothed.z;
		_rotLerpHistory.x += _rotLerp.x;
		_rotLerpHistory.y += _rotLerp.y;
		_rotLerpHistory.z += _rotLerp.z;
		_rotLerpHistory.x -= _rotLerpSmoothed.x;
		_rotLerpHistory.y -= _rotLerpSmoothed.y;
		_rotLerpHistory.z -= _rotLerpSmoothed.z;
		_posLerpSmoothed.x = _posLerpHistory.x * dt * TransformSmoothingDTMulti;
		_posLerpSmoothed.y = _posLerpHistory.y * dt * TransformSmoothingDTMulti;
		_posLerpSmoothed.z = _posLerpHistory.z * dt * TransformSmoothingDTMulti;
		_rotLerpSmoothed.x = _rotLerpHistory.x * dt * TransformSmoothingDTMulti;
		_rotLerpSmoothed.y = _rotLerpHistory.y * dt * TransformSmoothingDTMulti;
		_rotLerpSmoothed.z = _rotLerpHistory.z * dt * TransformSmoothingDTMulti;
		if (_transitionFirstFrame)
		{
			_transitionFirstFrame = false;
			_transitionWindowOpen = true;
		}
		if (_transitionWindowOpen)
		{
			AnimStateController.EWeaponState weaponState = AnimStateController.WeaponState;
			float normalizedTime = _player.HandsAnimator.Animator.GetCurrentAnimatorStateInfo(1).normalizedTime;
			float num = Mathf.Clamp(EfficiencyController.EfficiencyModifierInverse, 0.5f, 1.5f);
			float num2 = (PlayerMotionController.IsProne ? 0.5f : 1f);
			float num3 = (WeaponController.IsStockFolded ? 1.5f : 1f);
			float num4 = num * num2 * WeaponController.GetWeaponMulti(getInverse: true) * PrimeMover.TransitionSpeedMulti.Value * num3;
			switch (weaponState)
			{
			case AnimStateController.EWeaponState.ORDER_ARM:
			{
				float speed2 = _animSpeedCurvePhase1.Evaluate(normalizedTime) * num4;
				SetAnimSpeed(_player, speed2);
				_posLerp = Vector3.Lerp(Vector3.zero, _orderEndPos, normalizedTime);
				_rotLerp = Vector3.Lerp(Vector3.zero, _orderEndRot, normalizedTime);
				break;
			}
			case AnimStateController.EWeaponState.PRESENT_ARM:
			{
				float speed = _animSpeedCurvePhase2.Evaluate(normalizedTime) * num4;
				SetAnimSpeed(_player, speed);
				_posLerp = Vector3.Lerp(_presentStartPos, Vector3.zero, normalizedTime);
				_rotLerp = Vector3.Lerp(_presentStartRot, Vector3.zero, normalizedTime);
				break;
			}
			}
			if (_previousState == AnimStateController.EWeaponState.PRESENT_ARM && weaponState == AnimStateController.EWeaponState.IDLE)
			{
				SetAnimSpeed(_player, 1f);
				_transitionWindowOpen = false;
				_posLerp = Vector3.zero;
				_rotLerp = Vector3.zero;
			}
			_previousState = weaponState;
		}
	}

	public static void GetWeaponSelectionTransforms(out Vector3 pos, out Quaternion rot)
	{
		pos = _posLerpSmoothed;
		rot = TIRLUtils.GetQuatFromV3(_rotLerpSmoothed);
	}

	public static void Process(ECommand command, Player player)
	{
		if (player == null)
		{
			return;
		}
		_player = player;
		if (AnimStateController.WeaponState == AnimStateController.EWeaponState.UNKNOWN_STATE)
		{
			return;
		}
		Item lastEquippedWeaponOrKnifeItem = player.LastEquippedWeaponOrKnifeItem;
		Item containedItem = player.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem;
		Item containedItem2 = player.Inventory.Equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem;
		Item containedItem3 = player.Inventory.Equipment.GetSlot(EquipmentSlot.Holster).ContainedItem;
		if (lastEquippedWeaponOrKnifeItem == null)
		{
			player.TrySetLastEquippedWeapon(true);
			if (lastEquippedWeaponOrKnifeItem == null)
			{
				return;
			}
		}
		if (containedItem != null && lastEquippedWeaponOrKnifeItem.Equals(containedItem))
		{
			if (command == ECommand.SelectFirstPrimaryWeapon)
			{
				return;
			}
			_lastSelectedWeapon = EWeaponSelection.SLING;
		}
		if (containedItem2 != null && lastEquippedWeaponOrKnifeItem.Equals(containedItem2))
		{
			if (command == ECommand.SelectSecondPrimaryWeapon)
			{
				return;
			}
			_lastSelectedWeapon = EWeaponSelection.SHOULDER;
		}
		if (containedItem3 != null)
		{
			if (lastEquippedWeaponOrKnifeItem.Equals(containedItem3))
			{
				if (command == ECommand.SelectSecondaryWeapon)
				{
					return;
				}
				_lastSelectedWeapon = EWeaponSelection.PISTOL;
			}
			if (lastEquippedWeaponOrKnifeItem.Equals(containedItem3))
			{
				if (command == ECommand.QuickSelectSecondaryWeapon && containedItem == null)
				{
					return;
				}
				_lastSelectedWeapon = EWeaponSelection.PISTOL;
			}
		}
		if ((command != ECommand.SelectFirstPrimaryWeapon || containedItem != null) && (command != ECommand.SelectSecondPrimaryWeapon || containedItem2 != null) && (command != ECommand.SelectSecondaryWeapon || containedItem3 != null))
		{
			_transitionFirstFrame = true;
			if (command == ECommand.SelectFirstPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.SHOULDER)
			{
				ProcessShoulderToSling();
			}
			if (command == ECommand.SelectSecondPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.SLING)
			{
				ProcessSlingToShoulder();
			}
			if (command == ECommand.SelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.SLING)
			{
				ProcessSlingToPistol();
			}
			if (command == ECommand.QuickSelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.SLING)
			{
				ProcessQuickSlingToPistol();
			}
			if (command == ECommand.SelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.SHOULDER)
			{
				ProcessShoulderToPistol();
			}
			if (command == ECommand.QuickSelectSecondaryWeapon && _lastSelectedWeapon == EWeaponSelection.SHOULDER)
			{
				ProcessShoulderToPistol();
			}
			if (command == ECommand.SelectSecondPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.PISTOL)
			{
				ProcessPistolToShoulder();
			}
			if (command == ECommand.SelectFirstPrimaryWeapon && _lastSelectedWeapon == EWeaponSelection.PISTOL)
			{
				ProcessPistolToSling();
			}
			bool flag = command == ECommand.PressSlot0;
			flag = flag && command == ECommand.PressSlot4;
			flag = flag && command == ECommand.PressSlot5;
			flag = flag && command == ECommand.PressSlot6;
			flag = flag && command == ECommand.PressSlot7;
			flag = flag && command == ECommand.PressSlot8;
			flag = flag && command == ECommand.PressSlot9;
			flag = flag && command == ECommand.SelectFastSlot0;
			flag = flag && command == ECommand.SelectFastSlot4;
			flag = flag && command == ECommand.SelectFastSlot5;
			flag = flag && command == ECommand.SelectFastSlot6;
			flag = flag && command == ECommand.SelectFastSlot7;
			flag = flag && command == ECommand.SelectFastSlot8;
			if (flag && command == ECommand.SelectFastSlot9 && _lastSelectedWeapon == EWeaponSelection.SHOULDER)
			{
				ProcessShoulderToSlot();
			}
		}
	}

	private static void ProcessShoulderToSling()
	{
		_animSpeedCurvePhase1 = PrimeMover.Instance.OrderShoulderCurve;
		_animSpeedCurvePhase2 = _Flatcurve;
		_orderEndPos = Shoulder_End_Pos;
		_orderEndRot = Shoulder_End_Rot;
		_presentStartPos = Sling_Start_Pos;
		_presentStartRot = Sling_Start_Rot;
	}

	private static void ProcessSlingToShoulder()
	{
		_animSpeedCurvePhase1 = _Flatcurve;
		_animSpeedCurvePhase2 = PrimeMover.Instance.PresentShoulderCurve;
		_orderEndPos = Sling_End_Pos;
		_orderEndRot = Sling_End_Rot;
		_presentStartPos = Shoulder_Start_Pos;
		_presentStartRot = Shoulder_Start_Rot;
	}

	private static void ProcessSlingToPistol()
	{
		_animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(1f, 1f));
		_animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(1f, 0.3f));
		_orderEndPos = Sling_End_Pos;
		_orderEndRot = Sling_End_Rot;
		_presentStartPos = Pistol_Start_Pos;
		_presentStartRot = Pistol_Start_Rot;
	}

	private static void ProcessQuickSlingToPistol()
	{
		_animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0f, 1.25f), new Keyframe(1f, 0.75f));
		_animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0f, 1.2f), new Keyframe(1f, 2f));
		_orderEndPos = Sling_End_Pos;
		_orderEndRot = Sling_End_Rot;
		_presentStartPos = Pistol_Start_Pos;
		_presentStartRot = Pistol_Start_Rot;
	}

	private static void ProcessShoulderToPistol()
	{
		_animSpeedCurvePhase1 = PrimeMover.Instance.OrderShoulderCurve;
		_animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(1f, 0.3f));
		_orderEndPos = Shoulder_End_Pos;
		_orderEndRot = Shoulder_End_Rot;
		_presentStartPos = Pistol_Start_Pos;
		_presentStartRot = Pistol_Start_Rot;
	}

	private static void ProcessPistolToShoulder()
	{
		_animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0f, 0.25f), new Keyframe(1f, 0.25f));
		_animSpeedCurvePhase2 = PrimeMover.Instance.PresentShoulderCurve;
		_orderEndPos = Pistol_End_Pos;
		_orderEndRot = Pistol_End_Pos;
		_presentStartPos = Shoulder_Start_Pos;
		_presentStartRot = Shoulder_Start_Rot;
	}

	private static void ProcessPistolToSling()
	{
		_animSpeedCurvePhase1 = new AnimationCurve(new Keyframe(0f, 0.35f), new Keyframe(1f, 0.25f));
		_animSpeedCurvePhase2 = new AnimationCurve(new Keyframe(0f, 1.75f), new Keyframe(1f, 1.75f));
		_orderEndPos = Pistol_End_Pos;
		_orderEndRot = Pistol_End_Pos;
		_presentStartPos = Sling_Start_Pos;
		_presentStartRot = Sling_Start_Rot;
	}

	private static void ProcessShoulderToSlot()
	{
		_animSpeedCurvePhase1 = PrimeMover.Instance.OrderShoulderCurve;
		_animSpeedCurvePhase2 = _Flatcurve;
		_orderEndPos = Shoulder_End_Pos;
		_orderEndRot = Shoulder_End_Rot;
		_presentStartPos = Vector3.zero;
		_presentStartRot = Vector3.zero;
	}

	private static void SetAnimSpeed(Player player, float speed)
	{
		if (!(player == null))
		{
			player.HandsAnimator.SetAnimationSpeed(speed);
		}
	}
}
