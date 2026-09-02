using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;

namespace hideoutcat.bepinex
{
    /// <summary>
    /// 3.11: Singleton&lt;HideoutClass&gt;.Instance.AreaDatas
    /// 4.1: HideoutClass was removed during the hideout refactor. AreaDatas now lives on
    /// EFT.Hideout.HideoutRepresentation, which the game hands to HideoutController.InitHideoutAreas(...).
    /// This patch captures that reference so the plugin can query AreaDatas like before.
    /// </summary>
    internal class PatchHideoutInit : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(HideoutController), nameof(HideoutController.InitHideoutAreas));
        }

        [PatchPostfix]
        private static void Postfix(HideoutRepresentation hideout)
        {
            if (hideout != null)
                CatDependencyProviders.Hideout = hideout;
        }
    }
}
