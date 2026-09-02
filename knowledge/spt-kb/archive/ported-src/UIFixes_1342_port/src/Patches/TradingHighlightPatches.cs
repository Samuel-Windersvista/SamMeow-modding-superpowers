// 4.1.2: TradingHighlightPatches disabled - TradingGridView was reworked and manages
// Assortment.RequisiteChanged internally; the 3.x hook points (method_15/method_19) no longer exist.
using System.Reflection;
using EFT.UI.DragAndDrop;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class TradingHighlightPatches
{
    public static void Enable()
    {
        // Disabled for 4.1.2 (see header comment)
    }
}
