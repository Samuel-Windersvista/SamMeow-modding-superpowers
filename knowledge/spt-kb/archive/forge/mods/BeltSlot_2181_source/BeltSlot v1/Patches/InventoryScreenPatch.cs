using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Numerics;
using System.Reflection;
using UnityEngine.EventSystems;

namespace BeltSlot.Patches
{
    internal class InventoryScreenPatch : ModulePatch // all patches must inherit ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            // one way methods can be patched is by targeting both their class name and the name of the method itself
            // the example in this patch is the Jump() method in the Player class
            return AccessTools.Method(typeof(InventoryScreen), nameof(InventoryScreen.method_0));
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
                Plugin.Instance.inventoryScreenLoaded = true;
            }
        }
    }
}