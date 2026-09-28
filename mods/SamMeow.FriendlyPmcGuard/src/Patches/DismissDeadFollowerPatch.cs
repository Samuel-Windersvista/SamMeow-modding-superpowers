using System;
using System.Reflection;
using EFT;
using HarmonyLib;

namespace SamMeow.SPT.FriendlyPmcGuard.Patches
{
    /// <summary>
    /// P2: friendlyPMC.Components.BotFollowerPlayer.Dismiss(bool) Prefix。
    ///
    /// 原方法（BotFollowerPlayer.cs:582-604）先 Dispose Receiver/Brain，随后
    /// `if (_bot.IsDead || (int)_bot.BotState != 2) return;` 提前返回 —— 该路径下
    /// `_bot.BotFollower.BossToFollow` 仍悬垂、`AIBossPlayer.Followers` 仍含该 bot。
    ///
    /// Prefix 仅在"原方法会提前 return"的路径补齐 vanilla 等价清理：
    ///   - BossToFollow = null（等价 vanilla Dismiss :606-610 的悬垂清理）
    ///   - AIBossPlayer.Followers.Remove(_bot)（等价 vanilla AIBossPlayer.RemoveFollower）
    /// 正常路径（存活且 Active）原样放行，不做任何改动。
    ///
    /// 全部字段反射读取（_bot/_player 为 protected），全 try/catch。
    /// </summary>
    internal static class DismissDeadFollowerPatch
    {
        internal static FieldInfo BotField;    // BotFollowerPlayer._bot  (BotOwner)
        internal static FieldInfo PlayerField; // BotFollowerPlayer._player (pitAIBossPlayer : AIBossPlayer)

        private static bool Prefix(object __instance)
        {
            try
            {
                if (__instance == null)
                    return true;

                BotOwner bot = BotField?.GetValue(__instance) as BotOwner;
                if (bot == null)
                    return true; // 原方法自身会 return

                // 正常路径（存活且 Active）：原方法会完整清理，无需介入
                if (!bot.IsDead && bot.BotState == EBotState.Active)
                    return true;

                // ---- 原方法将在此提前 return 的路径：补齐等价清理 ----
                BotFollower botFollower = bot.BotFollower;
                if (botFollower != null && botFollower.BossToFollow != null)
                {
                    botFollower.BossToFollow = null;
                }

                object player = PlayerField?.GetValue(__instance);
                if (player is AIBossPlayer bossPlayer)
                {
                    bossPlayer.Followers.Remove(bot);
                }

                FriendlyPmcGuardPlugin.Log.LogWarning(
                    $"[FriendlyPmcGuard] dismissed dead follower cleanup: {bot.name}");
            }
            catch (Exception ex)
            {
                FriendlyPmcGuardPlugin.Log.LogWarning(
                    $"[FriendlyPmcGuard] Dismiss prefix failed: {ex.Message}");
            }

            return true; // 放行原方法（它会安全地提前 return）
        }
    }
}
