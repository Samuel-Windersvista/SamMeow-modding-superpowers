using EFT;
using HarmonyLib;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 撤离事件采集：postfix patch <c>LocalGame.Stop(string profileId, ExitStatus exitStatus, string exitName, float delay)</c>。
///
/// spike 结论：spec 候选的 <c>AbstractGame.Stop</c> **不存在**（AbstractGame 无 Stop 成员）；
/// 实际方法为 <c>EFT.LocalGame.Stop</c>（override），参数携带撤离点名与结局状态。
///
/// STD-CLI-003：显式 <c>[HarmonyPatch(typeof(...))]</c> 注解。
/// STD-CLI-007：Harmony 生命周期在 <see cref="Plugin"/>（new Harmony + PatchAll + Unload 撤销）。
/// 补丁体只读参数并转发到 <see cref="RaidEventCollector"/>，不修改游戏逻辑。
/// </summary>
[HarmonyPatch(typeof(LocalGame), nameof(LocalGame.Stop))]
internal static class LocalGameStopPatch
{
    [HarmonyPostfix]
    internal static void Postfix(string profileId, ExitStatus exitStatus, string exitName)
    {
        RaidEventCollector.NotifyLocalGameStop(profileId, exitStatus.ToString(), exitName);
    }
}
