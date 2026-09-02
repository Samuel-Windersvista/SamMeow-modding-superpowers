using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.HandBook;
using EFT.UI.Ragfair;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace UIFixes;

public static class FleaSlotSearchPatches
{
    public static void Enable()
    {
        new MyOffersPatch().Enable();
    }

    // 4.1.2: HandbookWorkaroundPatch + LinkedSlotSearchPatch removed - the RagFair searches API (method_24) and the
    // custom session endpoint (Class1594/LegacyParamsStruct) were reworked; slot search needs a fresh 4.1.2 implementation.

    public class MyOffersPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(RagFairClass), nameof(RagFairClass.FilterMyOffers));
        }

        [PatchPrefix]
        public static bool Prefix(RagFairClass __instance)
        {
            return __instance.FilterRule.ViewListType == EViewListType.MyOffers;
        }
    }
}
