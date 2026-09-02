using EFT.UI;
using EFT.UI.DragAndDrop;
using EFT.UI.WeaponModding;
using HarmonyLib;
using System.Reflection;

namespace TraderModding
{
    internal static class FieldInfos
    {
        public static FieldInfo EditBuildScreen_profile = AccessTools.Field(typeof(EditBuildScreen), "_profile");
        public static FieldInfo ModdingScreenSlotView_slot = AccessTools.Field(typeof(ModdingScreenSlotView), "_slot");
    }
}