// DISABLED for SPT 4.1.2: target method (ItemManipulator.smethod_24) and error type
// (UnsearchedContainerError) were removed/reworked in 4.1.2 deobfuscation.
// The "AddToUnsearchedContainers" feature needs a fresh 4.1.2 implementation
// targeting the current unsearched-container check path in EFT.InventoryLogic.ItemManipulator.
/*
using System.Reflection;
using Diz.LanguageExtensions;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public class ModifyUnsearchedContainerPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(InteractionsHandlerClass), nameof(InteractionsHandlerClass.smethod_24));
    }

    [PatchPostfix]
    public static void Postfix(ref Error error, ref bool __result)
    {
        if (!Settings.AddToUnsearchedContainers.Value)
        {
            return;
        }

        if (!__result && error is UnsearchedContainerError)
        {
            error = null;
            __result = true;
        }
    }
}
*/
