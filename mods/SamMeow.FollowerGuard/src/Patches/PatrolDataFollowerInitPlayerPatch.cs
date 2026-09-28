using HarmonyLib;

namespace SamMeow.SPT.FollowerGuard.Patches
{
    /// <summary>
    /// P3: vanilla PatrolDataFollower.InitPlayer(Player) Postfix。
    ///
    /// 玩家 boss 跟随路径下，InitPlayer 只构造 followerPlayerBase；若此前存在
    /// followerAIBase 残留（AI 路径遗留），GClass241 的 ManualUpdate 可能误入 AI 分支。
    /// Postfix 置空 followerAIBase，让 B 的 followerAIBase==null 守卫兜住。
    ///
    /// 注意：不要动 IsInited —— 它是玩家跟随路径
    /// （GClass241 → ManualUpdate → followerPlayerBase.Update）的入口条件，
    /// 置 false 会破坏 Goons/玩家 boss 的跟随。
    ///
    /// 与既有 B（followerPlayerBase 检查）、D（followerAIBase 短路）、C（幂等）无冲突。
    /// </summary>
    [HarmonyPatch(typeof(PatrolDataFollower), "InitPlayer")]
    internal static class PatrolDataFollowerInitPlayerPatch
    {
        private static void Postfix(PatrolDataFollower __instance)
        {
            if (__instance == null)
                return;
            try
            {
                __instance.followerAIBase = null;
            }
            catch
            {
                // 防御：置空失败不影响 InitPlayer 已完成的玩家跟随初始化。
            }
        }
    }
}
