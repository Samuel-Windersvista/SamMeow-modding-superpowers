using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace BeltSlot.Patches
{
    internal class InventoryScreenPatch : ModulePatch // all patches must inherit ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // 4.1.2: InventoryScreen.method_0 is gone; the setup work now runs in Awake.
            return AccessTools.Method(typeof(InventoryScreen), nameof(InventoryScreen.Awake));
        }


        [PatchPrefix]
        static void Prefix(InventoryScreen __instance)
        {
           
            //Plugin.Instance.generatedGridsView = __instance.transform.parent.gameObject.GetComponentInChildren<GeneratedGridsView>().gameObject;
            //return true;
        }

        [PatchPostfix]
        static void Postfix(InventoryScreen __instance)
        {
            if(Plugin.Instance != null)
            {
                Plugin.Instance.inventoryScreen = __instance;
                Plugin.Instance.inventoryScreenLoaded = true;
            }
        }
    }
}