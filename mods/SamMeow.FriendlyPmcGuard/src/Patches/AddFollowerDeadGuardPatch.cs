using EFT;
using HarmonyLib;

namespace SamMeow.SPT.FriendlyPmcGuard.Patches
{
    /// <summary>
    /// P4: friendlyPMC.Modules.BossPlayers.AddFollower(...) Prefix。
    ///
    /// 拒绝把已死 / 非 Active 的 bot 招募为跟班（原方法会 new BotFollowerPlayer 并 Init，
    /// 对死尸没有意义且会制造后续悬垂状态）。
    /// `return false` 跳过原方法，默认返回 null；两处调用方已核实对 null 返回安全。
    /// </summary>
    internal static class AddFollowerDeadGuardPatch
    {
        private static bool Prefix(BotOwner bot)
        {
            if (bot == null || bot.IsDead || bot.BotState != EBotState.Active)
            {
                FriendlyPmcGuardPlugin.Log.LogWarning(
                    $"[FriendlyPmcGuard] skip recruit dead bot: {(bot == null ? "<null>" : bot.name)}");
                return false;
            }
            return true;
        }
    }
}
