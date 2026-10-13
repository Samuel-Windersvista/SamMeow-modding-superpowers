using System;
using System.Reflection;
using EFT;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.QoL.Patches
{
    /// <summary>
    /// <c>EFT.Hideout.ItemsProducerBase.Update(float)</c> prefix：瞬间生产。
    /// </summary>
    /// <remarks>
    /// 0.16.9 混淆名映射（见 <c>docs/api-touchpoints/dadgamer.md</c> §2.1）：旧 <c>GClass1931.Update</c>
    /// → <c>ItemsProducerBase.Update(float)</c>；进度字段 <c>_producingProcess.EndTime</c> 置为当前后放行原方法，
    /// 原 <c>Produce()</c> 由 EndTime 重算 <c>Progress</c> 立即达到 1.0，随后的 <c>ProcessDoneItems()</c>
    /// 完成产出（等价上游 <c>CompleteProduction</c> 链）。
    /// 排除比特币 / 燃料配方（上游 scheme id，避免破坏比特币农场产出节奏）。
    /// 补丁体 try/catch fail-open；未启用时零副作用。
    /// </remarks>
    internal sealed class InstantProductionPatch : ModulePatch
    {
        /// <summary>上游排除的比特币 / 燃料 scheme id。</summary>
        private const string BitcoinSchemeId = "5d5589c1f934db045e6c5492";
        private const string FuelSchemeId = "5d5c205bd582a50d042a3c0e";

        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(ItemsProducerBase), "Update", new[] { typeof(float) });

        [PatchPrefix]
        private static void Prefix(ItemsProducerBase __instance)
        {
            try
            {
                if (!QoLSettings.Ready || !QoLSettings.InstantProduction.Value || __instance == null)
                {
                    return;
                }

                var producingItems = __instance.ProducingItems;
                if (producingItems == null)
                {
                    return;
                }

                double now = DateTimeExtensions.UtcNowUnix;

                foreach (var pair in producingItems)
                {
                    string schemeId = pair.Key;
                    if (schemeId == BitcoinSchemeId || schemeId == FuelSchemeId)
                    {
                        continue;
                    }

                    ProducingItemController item = pair.Value;
                    if (item == null)
                    {
                        continue;
                    }

                    var process = item._producingProcess;
                    if (process != null)
                    {
                        process.EndTime = now;
                    }
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] 瞬产 Prefix 异常（fail-open）：{ex.Message}");
            }
        }
    }
}
