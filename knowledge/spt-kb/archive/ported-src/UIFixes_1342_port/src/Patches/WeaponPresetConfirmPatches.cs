using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.InventoryLogic;
using Diz.Binding;
using EFT.UI;
using EFT.UI.Builds;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class WeaponPresetConfirmPatches
{
    public static bool MoveForward;
    public static bool InstantSavePreset = false;

    public static void Enable()
    {
        // Two patches are required for the edit preset screen - one to grab the value of moveForward from CloseScreenInterruption(), and one to use it.
        // This is because BSG didn't think to pass the argument in to.method_35
        // 4.1.2: DetectWeaponPresetCloseTypePatch removed (CloseScreenInterruption(moveForward) no longer exists)
        new ConfirmDiscardWeaponPresetChangesPatch().Enable();

        // The save button should just save, not prompt to rename
        new SavePresetPatch().Enable();
        new InstantSavePresetPatch().Enable();

        // Also BSG just immediately sets the dirty flag on load, because they don't understand what anything is or how it should work
        new NoUnsavedChangesPatch().Enable();
    }

    // 4.1.2: the 3.x CloseScreenInterruption(moveForward) navigation callback was removed.
    // The confirmation is now driven directly from TryCloseScreen() based on the setting only.
    public class ConfirmDiscardWeaponPresetChangesPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.TryCloseScreen));
        }

        [PatchPrefix]
        public static bool Prefix(ref Task<bool> __result)
        {
            if (Settings.ShowPresetConfirmations.Value == WeaponPresetConfirmationOption.Never)
            {
                __result = Task.FromResult(true);
                return false;
            }

            return true;
        }
    }

    public class SavePresetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(EditBuildScreen), nameof(EditBuildScreen.SaveAsBuild));
        }

        [PatchPrefix]
        public static void Prefix(bool asNewBuild)
        {
            InstantSavePreset = !asNewBuild;
        }

        [PatchPostfix]
        public static void Postfix()
        {
            InstantSavePreset = false;
        }
    }

    public class InstantSavePresetPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(ItemUiContext), nameof(ItemUiContext.ShowEditBuildNameWindow));
        }

        [PatchPrefix]
        public static bool Prefix(string savedName, ref EditBuildNameWindowContext __result)
        {
            if (string.IsNullOrEmpty(savedName) || !InstantSavePreset || !Settings.OneClickPresetSave.Value)
            {
                return true;
            }

            __result = new EditBuildNameWindowContext();
            __result.AcceptCompletionSource.SetResult(savedName); // Don't use Accept(), it's stupid
            Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ButtonClick);
            return false;
        }
    }

    public class NoUnsavedChangesPatch : ModulePatch
    {
        private static FieldInfo DirtyFlagField;

        protected override MethodBase GetTargetMethod()
        {
            DirtyFlagField = AccessTools.GetDeclaredFields(typeof(EditBuildScreen)).First(
                f => f.FieldType.IsGenericType &&
                f.FieldType.GetGenericTypeDefinition() == typeof(Diz.Binding.BindableState<>) &&
                f.Name.EndsWith("_0"));

            return AccessTools.DeclaredMethod(
                typeof(EditBuildScreen),
                nameof(EditBuildScreen.Show),
                [typeof(Item), typeof(Item), typeof(InventoryController), typeof(ISession)]);
        }

        [PatchPostfix]
        public static void Postfix(EditBuildScreen __instance)
        {
            var bindable = DirtyFlagField.GetValue(__instance) as BindableState<bool>;
            bindable.Value = false;
        }
    }
}