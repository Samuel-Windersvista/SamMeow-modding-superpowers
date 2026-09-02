using System;
using BepInEx.Configuration;
using EFT.InputSystem;
using EFT.Interactive;
using Helpers.CursorHelper;
using LeaveItThere.Common;
using LeaveItThere.CustomUI;
using LeaveItThere.Fika;
using LeaveItThere.Helpers;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;

namespace LeaveItThere.Components;

internal class ItemMover : MonoBehaviour
{
	public class EnterMoveModeInteraction : CustomInteraction
	{
		public override string Name
		{
			get
			{
				if (MoveModeDisallowed(base.FakeItem, out var reason))
				{
					return "Move: " + reason;
				}
				return "Move";
			}
		}

		public override bool Enabled
		{
			get
			{
				string reason;
				return !MoveModeDisallowed(base.FakeItem, out reason);
			}
		}

		public EnterMoveModeInteraction(FakeItem fakeItem)
			: base(fakeItem)
		{
		}

		public override void OnInteract()
		{
			Instance.Enable(base.FakeItem);
		}
	}

	private static ItemMover _instance;

	private FakeItem _target;

	private Vector3 _undoPosition;

	private Quaternion _undoRotation;

	private Quaternion _targetCachedRotation;

	private bool _lmbDown;

	private bool _rmbDown;

	private Vector3 _playerPositionLastFrame;

	public static ItemMover Instance
	{
		get
		{
			if ((UnityEngine.Object)(object)_instance == (UnityEngine.Object)null)
			{
				_instance = GameObjectExtensions.GetOrAddComponent<ItemMover>(((Component)LITSession.Instance.Player).gameObject);
			}
			return _instance;
		}
		private set
		{
			_instance = value;
		}
	}

	public FakeItem Target
	{
		get
		{
			return _target;
		}
		private set
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			_target = value;
			if ((UnityEngine.Object)(object)_target != (UnityEngine.Object)null)
			{
				_targetCachedRotation = _targetTransform.rotation;
				TargetMoveable = GameObjectExtensions.GetOrAddComponent<Moveable>(((Component)Target).gameObject);
			}
			else
			{
				TargetMoveable = null;
			}
		}
	}

	public Moveable TargetMoveable { get; private set; }

	private Transform _targetTransform => ((Component)Target).gameObject.transform;

	public MoveModeUI UI => MoveModeUI.Instance;

	public MoveModeUI.ETabType CurrentMode => UI.SelectedTabType;

	public bool WillFloat => UI.PhysTab.ItemFloats.isOn;

	public bool LMBDown
	{
		get
		{
			return _lmbDown;
		}
		private set
		{
			if (value)
			{
				CursorHelper.ReturnCursorControlToEFT();
			}
			else if (!RMBDown)
			{
				CursorHelper.ForceUnlockCursor();
			}
			_lmbDown = value;
		}
	}

	public bool RMBDown
	{
		get
		{
			return _rmbDown;
		}
		private set
		{
			if (value)
			{
				CursorHelper.ReturnCursorControlToEFT();
			}
			else if (!LMBDown)
			{
				CursorHelper.ForceUnlockCursor();
			}
			if (value)
			{
				InteractionHelper.SetCameraRotationLocked(enabled: false);
			}
			else
			{
				InteractionHelper.SetCameraRotationLocked(enabled: true);
			}
			_rmbDown = value;
		}
	}

	private float _mouseX => Input.GetAxis("Mouse X");

	private float _mouseY => Input.GetAxis("Mouse Y");

	private float _scrollInput => Input.GetAxis("Mouse ScrollWheel");

	private float _precisionMultiplier
	{
		get
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			KeyboardShortcut value = Settings.PrecisionKey.Value;
			if (!value.IsPressed())
			{
				return 1f;
			}
			return Settings.PrecisionMultiplier.Value;
		}
	}

	private float _mouseRepositionSpeedMultiplier => Settings.RepositionSpeed.Value * _precisionMultiplier;

	private float _scrollRepositionSpeedMultiplier => Settings.RepositionScrollSpeed.Value * _precisionMultiplier;

	private float _mouseRotationSpeedMultipier => Settings.RotationSpeed.Value * _precisionMultiplier;

	private float _scrollRotationSpeedMultiplier => Settings.RotationScrollSpeed.Value * _precisionMultiplier;

	private Vector3 _cameraForward
	{
		get
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			Vector3 forward = ((Component)Camera.main).transform.forward;
			forward.y = 0f;
			return forward;
		}
	}

	private Quaternion _cameraRotation => Quaternion.LookRotation(_cameraForward);

	public Vector3 PlayerMovementDelta => LITSession.Instance.Player.Transform.position - _playerPositionLastFrame;

	private void UpdatePlayerLastFrameDeltas()
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		_playerPositionLastFrame = LITSession.Instance.Player.Transform.position;
	}

	private void OnMovedToPlayerClicked()
	{
		TargetMoveable.MoveToPlayer();
	}

	private void OnUndoMoveClicked()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		_targetTransform.position = _undoPosition;
	}

	private void OnResetRotationClicked()
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		_targetTransform.rotation = Quaternion.identity;
	}

	private void OnUndoRotationClicked()
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		_targetTransform.rotation = _undoRotation;
	}

	private void OnSaveButtonClicked()
	{
		Disable(movementSaved: true);
	}

	private void OnCancelButtonClicked()
	{
		Disable(movementSaved: false);
	}

	public ItemMover()
	{
		((Behaviour)this).enabled = false;
	}

	private void Awake()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Expected O, but got Unknown
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Expected O, but got Unknown
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Expected O, but got Unknown
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f1: Expected O, but got Unknown
		UI.TabSwitched += OnTabSwitched;
		((UnityEvent)UI.PosTab.MoveToPlayerButton.onClick).AddListener(new UnityAction(OnMovedToPlayerClicked));
		((UnityEvent)UI.PosTab.UndoMoveButton.onClick).AddListener(new UnityAction(OnUndoMoveClicked));
		((UnityEvent)UI.RotTab.ResetRotationButton.onClick).AddListener(new UnityAction(OnResetRotationClicked));
		((UnityEvent)UI.RotTab.UndoRotationButton.onClick).AddListener(new UnityAction(OnUndoRotationClicked));
		((UnityEvent)UI.SaveButton.onClick).AddListener(new UnityAction(OnSaveButtonClicked));
		((UnityEvent)UI.CancelButton.onClick).AddListener(new UnityAction(OnCancelButtonClicked));
	}

	private void OnEnable()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		UpdatePlayerLastFrameDeltas();
		UI.PhysTab.ItemFloats.isOn = !Settings.ImmersivePhysics.Value;
		UI.ChangeTabs<MoveModeUI.PositionTab>();
		ItemHelper.SetItemColor(Color.green, ((Component)Target).gameObject);
	}

	private void Update()
	{
		if (MoveModeDisallowed(Target, out var reason))
		{
			Disable(movementSaved: true);
			InteractionHelper.ErrorPlayerFeedback("Move Mode cancelled! Reason: " + reason);
			return;
		}
		MouseInputProcess();
		UIHotkeyInputProcess(out var cancelRemainingFrameLogic);
		if (!cancelRemainingFrameLogic)
		{
			if (CurrentMode == MoveModeUI.ETabType.Position)
			{
				PositionProcess();
			}
			if (CurrentMode == MoveModeUI.ETabType.Rotation)
			{
				RotationProcess();
			}
			if (CurrentMode == MoveModeUI.ETabType.Physics)
			{
				PhysicsProcess();
			}
			UpdatePlayerLastFrameDeltas();
		}
	}

	private void MouseInputProcess()
	{
		if (!EventSystem.current.IsPointerOverGameObject())
		{
			if (Input.GetMouseButtonDown(0))
			{
				LMBDown = true;
			}
			if (Input.GetMouseButtonUp(0))
			{
				LMBDown = false;
			}
			if (Input.GetMouseButtonDown(1))
			{
				RMBDown = true;
			}
			if (Input.GetMouseButtonUp(1))
			{
				RMBDown = false;
			}
		}
	}

	private void UIHotkeyInputProcess(out bool cancelRemainingFrameLogic)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		KeyboardShortcut value = Settings.SaveHotkey.Value;
		if (value.IsDown())
		{
			Disable(movementSaved: true);
			cancelRemainingFrameLogic = true;
			return;
		}
		value = Settings.CancelHotkey.Value;
		if (value.IsDown())
		{
			Disable(movementSaved: false);
			cancelRemainingFrameLogic = true;
			return;
		}
		value = Settings.RepositionTabHotkey.Value;
		if (value.IsDown())
		{
			UI.ChangeTabs<MoveModeUI.PositionTab>();
		}
		value = Settings.RotationTabHotkey.Value;
		if (value.IsDown())
		{
			UI.ChangeTabs<MoveModeUI.RotationTab>();
		}
		value = Settings.PhysicsTabHotkey.Value;
		if (value.IsDown())
		{
			UI.ChangeTabs<MoveModeUI.PhysicsTab>();
		}
		cancelRemainingFrameLogic = false;
	}

	private void OnTabSwitched(MoveModeUI.ETabType tabType)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		switch (tabType)
		{
		case MoveModeUI.ETabType.Position:
			TargetMoveable.DisablePhysics();
			ItemHelper.SetItemColor(Color.green, ((Component)Target).gameObject);
			break;
		case MoveModeUI.ETabType.Rotation:
			TargetMoveable.DisablePhysics();
			ItemHelper.SetItemColor(Color.red, ((Component)Target).gameObject);
			break;
		case MoveModeUI.ETabType.Physics:
			ItemHelper.SetItemColor(Color.magenta, ((Component)Target).gameObject);
			break;
		}
	}

	public void Enable(FakeItem target)
	{
		if (!((Behaviour)this).enabled)
		{
			LITSession.Instance.SetInteractionsEnabled(enabled: false);
			LITSession.Instance.GamePlayerOwner.ClearInteractionState();
			UI.SetActive(isActive: true);
			Target = target;
			_undoPosition = _targetTransform.position;
			_undoRotation = _targetTransform.rotation;
			Target.SetPlayerAndBotCollisionEnabled(enabled: false);
			InteractionHelper.SetCameraRotationLocked(enabled: true);
			InteractionHelper.SetMostInputsIgnored(ignored: true, new ECommand[] { ECommand.Jump, ECommand.ToggleSprinting, ECommand.EndSprinting, ECommand.ToggleDuck, ECommand.ResetLookDirection });
			CursorHelper.ForceUnlockCursor();
			((Behaviour)this).enabled = true;
		}
	}

	public void Disable(bool movementSaved)
	{
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_007e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		if (((Behaviour)this).enabled)
		{
			((Behaviour)this).enabled = false;
			if (movementSaved)
			{
				Target.PlaceAtPosition(((Component)Target).gameObject.transform.position, ((Component)Target).gameObject.transform.rotation);
				FikaBridge.SendPlacedStateChangedPacket(Target, isPlaced: true, WillFloat);
				InteractionHelper.NotificationLong("Placement edit saved!");
			}
			else
			{
				_targetTransform.position = _undoPosition;
				_targetTransform.rotation = _undoRotation;
				InteractionHelper.NotificationLongWarning("Move Mode cancelled.");
			}
			if (movementSaved && !WillFloat)
			{
				TargetMoveable.EnablePhysics(pausable: true);
			}
			else
			{
				TargetMoveable.DisablePhysics();
			}
			Target.SetPlayerAndBotCollisionEnabled(Settings.PlacedItemsHaveCollision.Value);
			ItemHelper.SetItemColor(Settings.PlacedItemTint.Value, ((Component)Target).gameObject);
			((Component)Target).gameObject.transform.SetParent((Transform)null);
			Target = null;
			LITSession.Instance.SetInteractionsEnabled(enabled: true);
			UI.SetActive(isActive: false);
			LMBDown = false;
			RMBDown = false;
			InteractionHelper.SetCameraRotationLocked(enabled: false);
			CursorHelper.ReturnCursorControlToEFT();
			InteractionHelper.SetMostInputsIgnored(ignored: false);
			InteractionHelper.RefreshPrompt();
		}
	}

	public static bool MoveModeDisallowed(FakeItem fakeItem, out string reason)
	{
		if (fakeItem.Flags.MoveModeDisabled)
		{
			reason = fakeItem.Flags.MoveModeDisabledReason;
			return true;
		}
		if (Settings.MoveModeRequiresInventorySpace.Value && !ItemHelper.ItemCanBePickedUp(((LootItem)fakeItem.LootItem).Item))
		{
			reason = "No Space";
			return true;
		}
		if (Settings.MoveModeCancelsSprinting.Value && LITSession.Instance.Player.Physical.Sprinting)
		{
			reason = "Sprinting";
			return true;
		}
		reason = "";
		return false;
	}

	private void RotationProcess()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0071: Unknown result type (might be due to invalid IL or missing references)
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		_ = Vector3.zero;
		int num = (Settings.InvertHorizontalRotation.Value ? 1 : (-1));
		int num2 = ((!Settings.InvertVerticalRotation.Value) ? 1 : (-1));
		Vector3 val = ((UI.RotationReference == MoveModeUI.ESpaceReference.Player) ? (_cameraRotation * Vector3.right) : Vector3.right);
		Vector3 val2 = ((UI.RotationReference == MoveModeUI.ESpaceReference.Player) ? (_cameraRotation * Vector3.forward) : Vector3.forward);
		Vector3 up = Vector3.up;
		Space val3 = (UI.RotationReference == MoveModeUI.ESpaceReference.Item) ? Space.Self : Space.World;
		if (LMBDown && !UI.RotTab.LockX.isOn)
		{
			_targetTransform.Rotate(val, (float)num2 * _mouseY * _mouseRotationSpeedMultipier, val3);
		}
		if (LMBDown && !UI.RotTab.LockY.isOn)
		{
			_targetTransform.Rotate(up, (float)num * _mouseX * _mouseRotationSpeedMultipier, val3);
		}
		if (_scrollInput != 0f && !UI.RotTab.LockZ.isOn)
		{
			_targetTransform.Rotate(val2, _scrollInput * _scrollRotationSpeedMultiplier, val3);
		}
	}

	private void PositionProcess()
	{
		if (!TryBothMouseButtonItemHoldReturnsSuccess())
		{
			MouseDragPositionProcess();
		}
	}

	private bool TryBothMouseButtonItemHoldReturnsSuccess()
	{
		//IL_008d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		if (LMBDown && RMBDown)
		{
			if ((UnityEngine.Object)(object)((Component)Target).gameObject.transform.parent == (UnityEngine.Object)null)
			{
				_targetCachedRotation = ((Component)Target).gameObject.transform.rotation;
				((Component)Target).gameObject.transform.SetParent(LITSession.Instance.Player.CameraContainer.gameObject.transform);
			}
			((Component)Target).gameObject.transform.rotation = _targetCachedRotation;
			return true;
		}
		if ((!LMBDown || !RMBDown) && (UnityEngine.Object)(object)((Component)Target).gameObject.transform.parent != (UnityEngine.Object)null)
		{
			((Component)Target).gameObject.transform.SetParent((Transform)null);
		}
		return false;
	}

	private void MouseDragPositionProcess()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_007a: Unknown result type (might be due to invalid IL or missing references)
		//IL_007f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0084: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0136: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Unknown result type (might be due to invalid IL or missing references)
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0164: Unknown result type (might be due to invalid IL or missing references)
		//IL_0171: Unknown result type (might be due to invalid IL or missing references)
		//IL_0172: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = Vector3.zero;
		Vector3 val2 = ((UI.RepositionReference == MoveModeUI.ESpaceReference.Player) ? (_cameraRotation * Vector3.right) : Vector3.right);
		Vector3 val3 = ((UI.RepositionReference == MoveModeUI.ESpaceReference.Player) ? (_cameraRotation * Vector3.forward) : Vector3.forward);
		Vector3 up = Vector3.up;
		if (LMBDown)
		{
			val += new Vector3(PlayerMovementDelta.x, 0f, PlayerMovementDelta.z);
		}
		if (LMBDown && !UI.PosTab.LockX.isOn)
		{
			val += val2 * _mouseX * _mouseRepositionSpeedMultiplier;
		}
		if (LMBDown && !UI.PosTab.LockY.isOn)
		{
			val += up * _mouseY * _mouseRepositionSpeedMultiplier;
		}
		if (_scrollInput != 0f && !UI.PosTab.LockZ.isOn)
		{
			val += val3 * _scrollInput * _scrollRepositionSpeedMultiplier;
		}
		Space val4 = (UI.RepositionReference == MoveModeUI.ESpaceReference.Item) ? Space.Self : Space.World;
		_targetTransform.Translate(val, val4);
	}

	private void PhysicsProcess()
	{
		if (LMBDown && !TargetMoveable.PhysicsIsEnabled)
		{
			TargetMoveable.SetPhysicsEnabled(enabled: true, pausable: false);
		}
		if (!LMBDown && TargetMoveable.PhysicsIsEnabled)
		{
			TargetMoveable.DisablePhysics();
		}
	}
}
