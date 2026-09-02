using System.Reflection;
using EFT;
using SPT.Reflection.Patching;

namespace _DisableScavMode_egboggied.Patches;

public class InsuranceScreenPatch : ModulePatch {
    protected override MethodBase GetTargetMethod() {
        return typeof(MainMenuControllerClass).GetMethod("method_81", BindingFlags.Public | BindingFlags.Instance);
    }

    [PatchPrefix]
    public static bool Prefix(RaidSettings ___RaidSettings_0) {
        if (!Plugin.InsuranceScreen.Value) ___RaidSettings_0.RaidMode = ERaidMode.Local;

        return true;
    }
}