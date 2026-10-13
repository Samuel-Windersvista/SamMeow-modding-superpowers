using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EFT.Hideout;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace SamMeow.DebugToolkit.Modules.QoL.Patches
{
    /// <summary>
    /// 瞬间建造 / 升级的共用落地逻辑：跳过计时，直接把当前 Stage 置为「待安装」。
    /// </summary>
    /// <remarks>
    /// 与上游 DadGamerMode 1.9.3 <c>InstantCompleteConstruction</c> 的字段设置逐字一致
    /// （<c>Waiting=false</c> / <c>Status=ReadyToInstall*</c> / <c>ActionGoing=false</c> / <c>ActionReady=true</c>）。
    /// </remarks>
    internal static class InstantConstructionHelper
    {
        /// <summary>把区域当前 Stage 置为待安装（不调用后端 CompleteUpgradeZone，保持上游语义）。</summary>
        internal static void CompleteToReady(AreaData area)
        {
            if (area == null)
            {
                return;
            }

            Stage stage = area.CurrentStage;
            if (stage == null)
            {
                return;
            }

            stage.Waiting = false;
            area.Status = area.CurrentLevel > 0
                ? EAreaStatus.ReadyToInstallUpgrade
                : EAreaStatus.ReadyToInstallConstruct;
            stage.ActionGoing = false;
            stage.ActionReady = true;
        }
    }

    /// <summary>
    /// <c>EFT.Hideout.AreaData.InitConstructing(int, bool)</c> prefix：藏身处载入时把
    /// 「正在建造 / 升级」的区域即刻置为待安装。
    /// </summary>
    /// <remarks>
    /// 0.16.9 混淆名映射（见 <c>docs/api-touchpoints/dadgamer.md</c> §2.2）：旧
    /// <c>AreaData.method_0(int)</c> → <c>InitConstructing(int timestamp, bool alreadyUnderConstructing)</c>；
    /// 方法体字段设置与上游逐字一致。原方法返回 <c>Task</c>，短路时必须显式置
    /// <c>__result = Task.CompletedTask</c>（否则调用方 <c>.HandleExceptions()</c> 遇 null 任务 NRE）。
    /// <c>alreadyUnderConstructing</c> 分支行为待 S3 游戏内确认。补丁体 try/catch fail-open。
    /// </remarks>
    internal sealed class InstantConstructionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(typeof(AreaData), "InitConstructing", new[] { typeof(int), typeof(bool) });

        [PatchPrefix]
        private static bool Prefix(AreaData __instance, ref Task __result)
        {
            try
            {
                if (!QoLSettings.Ready || !QoLSettings.InstantConstruction.Value || __instance == null)
                {
                    return true;
                }

                InstantConstructionHelper.CompleteToReady(__instance);
                __result = Task.CompletedTask;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] 瞬建 InitConstructing Prefix 异常（fail-open）：{ex.Message}");
                return true;
            }
        }
    }

    /// <summary>
    /// <c>EFT.Hideout.AreaData.WaitUpgradeTime(float, CancellationToken)</c> prefix：界面点击
    /// 升级 / 建造后跳过等待计时，即刻置为待安装。
    /// </summary>
    /// <remarks>
    /// 与 <see cref="InstantConstructionPatch"/> 同一落地形态（上游 <c>method_0</c> 在 0.16.9 被拆为
    /// 「载入恢复」与「界面点击等待」两条路径；两者都需覆盖才能让界面操作即刻完成）。
    /// 原方法返回 <c>Task</c>，短路时置 <c>__result = Task.CompletedTask</c>。补丁体 try/catch fail-open。
    /// </remarks>
    internal sealed class InstantUpgradeWaitPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
            => AccessTools.Method(
                typeof(AreaData),
                "WaitUpgradeTime",
                new[] { typeof(float), typeof(CancellationToken) });

        [PatchPrefix]
        private static bool Prefix(AreaData __instance, ref Task __result)
        {
            try
            {
                if (!QoLSettings.Ready || !QoLSettings.InstantConstruction.Value || __instance == null)
                {
                    return true;
                }

                InstantConstructionHelper.CompleteToReady(__instance);
                __result = Task.CompletedTask;
                return false;
            }
            catch (Exception ex)
            {
                Plugin.Logger?.LogWarning($"[QoL] 瞬建 WaitUpgradeTime Prefix 异常（fail-open）：{ex.Message}");
                return true;
            }
        }
    }
}
