using System.Collections.Generic;
using System.Reflection;
using SPT.Reflection.Patching;
using Koenigz.PerfectCulling;
using Koenigz.PerfectCulling.EFT;
using UnityEngine;

namespace DoorBreach.Patches
{
    /// <summary>
    /// PerfectCulling 空引用防护：前缀接管 Toggle，跳过会抛空引用的原方法循环。
    /// </summary>
    internal class PerfectCullingNullRefPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod() => typeof(PerfectCullingBakeGroup).GetMethod(nameof(PerfectCullingBakeGroup.Toggle));

        // 4.1.6 字段改名映射（4.0 混淆名 -> 4.1.6 真名）：
        //   ___Int_0               -> ___runtimeGroupDataSize
        //   ___RuntimeGroupContent_0 -> ____runtimeGroupData
        [PatchPrefix]
        private static bool Prefix(PerfectCullingBakeGroup __instance,
                                    ref int ___runtimeGroupDataSize,
                                    PerfectCullingBakeGroup.RuntimeGroupContent[] ____runtimeGroupData,
                                    bool rendererEnabled)
        {
            if (__instance.runtimeProxies != null)
            {
                Renderer[] array = __instance.runtimeProxies;
                for (int i = 0; i < array.Length; i++)
                {
                    array[i].enabled = !rendererEnabled;
                }
            }

            if (__instance.cullingLightObjects != null)
            {
                foreach (CullingObject cullingObject in __instance.cullingLightObjects)
                {
                    if (cullingObject != null)
                    {
                        cullingObject.SetAutocullVisibility(rendererEnabled);
                    }
                }
            }
            if (__instance.analyticSources != null)
            {
                foreach (AnalyticSource analyticSource in __instance.analyticSources)
                {
                    if (analyticSource != null)
                    {
                        analyticSource.IsAutocullVisible = rendererEnabled;
                    }
                }
            }
            if (__instance.screenDistanceSwitcher != null)
            {
                __instance.screenDistanceSwitcher.IsBakedAutocullVisible = rendererEnabled;
            }

            if (___runtimeGroupDataSize > ____runtimeGroupData.Length)
                ___runtimeGroupDataSize = ____runtimeGroupData.Length;

            for (int j = 0; j < ___runtimeGroupDataSize; j++)
            {
                if (____runtimeGroupData[j].Renderer != null)
                    ____runtimeGroupData[j].Renderer.enabled = rendererEnabled;
            }

            return false;
        }
    }
}
