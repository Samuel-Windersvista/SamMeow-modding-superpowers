using System;
using System.Linq;
using System.Reflection;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;

namespace Helpers.CursorHelper;

public static class CursorHelper
{
	public class CursorPatch : ModulePatch
	{
		private static FieldInfo _cursorResultField;

		protected override MethodBase GetTargetMethod()
		{
			_cursorResultField = typeof(InputManager).GetFields(BindingFlags.Instance | BindingFlags.NonPublic).First((FieldInfo f) => f.FieldType == typeof(ECursorResult));
			return AccessTools.Method(typeof(InputManager), "Update", (Type[])null, (Type[])null);
		}

		[PatchPrefix]
		private static bool PatchPrefix(InputManager __instance)
		{
			if (_blockAllInput)
			{
				return false;
			}
			if (CursorForceUnlocked)
			{
				_cursorResultField.SetValue(__instance, (object)(EFT.InputSystem.ECursorResult)2);
			}
			return true;
		}
	}

	private static bool _blockAllInput;

	private static readonly Type _cursorType;

	private static readonly MethodInfo _setCursorMethod;

	public static bool CursorForceUnlocked { get; private set; }

	static CursorHelper()
	{
		_cursorType = PatchConstants.EftTypes.Single((Type x) => x.GetMethod("SetCursor") != null);
		_setCursorMethod = _cursorType.GetMethod("SetCursor");
	}

	public static void SetCursor(ECursorType type)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		_setCursorMethod.Invoke(null, new object[1] { type });
	}

	public static void ToggleCursorForceUnlocked(bool blockAllInput = false)
	{
		SetCursorForceUnlocked(!CursorForceUnlocked, blockAllInput);
	}

	public static void SetCursorForceUnlocked(bool unlocked, bool blockAllInput = false)
	{
		if (unlocked)
		{
			ForceUnlockCursor(blockAllInput);
		}
		else
		{
			ReturnCursorControlToEFT();
		}
	}

	public static void ForceUnlockCursor(bool blockAllInput = false)
	{
		_blockAllInput = blockAllInput;
		CursorForceUnlocked = true;
		SetCursor(EFT.UI.ECursorType.Idle);
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
	}

	public static void ReturnCursorControlToEFT()
	{
		_blockAllInput = false;
		CursorForceUnlocked = false;
		SetCursor(EFT.UI.ECursorType.Invisible);
		Cursor.lockState = CursorLockMode.Locked;
		Cursor.visible = false;
	}
}
