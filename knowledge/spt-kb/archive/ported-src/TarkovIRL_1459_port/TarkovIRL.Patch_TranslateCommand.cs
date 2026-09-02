using System.Reflection;
using EFT;
using EFT.InputSystem;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace TarkovIRL;

internal class Patch_TranslateCommand : ModulePatch
{
	private static FieldInfo _playerField;

	protected override MethodBase GetTargetMethod()
	{
		_playerField = AccessTools.Field(typeof(EFT.PlayerInputTranslator), "_player");
		return typeof(EFT.PlayerInputTranslator).GetMethod("TranslateCommand", BindingFlags.Instance | BindingFlags.Public);
	}

	[PatchPostfix]
	private static void PatchPostfix(EFT.PlayerInputTranslator __instance, ECommand command)
	{
		Player player = (Player)_playerField.GetValue(__instance);
		if (player != null && player.IsYourPlayer)
		{
			if (command == ECommand.ReloadWeapon)
			{
				AugmentedReloadController.ToggleAugmentedMode();
			}
			if (command == ECommand.SelectFirstPrimaryWeapon || command == ECommand.SelectSecondPrimaryWeapon || command == ECommand.QuickSelectSecondaryWeapon || command == ECommand.SelectSecondaryWeapon)
			{
				WeaponSelectionController.Process(command, player);
			}
			if (command == ECommand.ToggleBreathing)
			{
				PlayerMotionController.IsHoldingBreath = true;
			}
			if (command == ECommand.EndBreathing)
			{
				PlayerMotionController.IsHoldingBreath = false;
			}
			if (command == ECommand.FoldStock)
			{
				WeaponController.ToggleFolded();
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
			if (flag && command == ECommand.SelectFastSlot9)
			{
				WeaponSelectionController.Process(command, player);
			}
		}
	}
}
