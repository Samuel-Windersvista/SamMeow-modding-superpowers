using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using HarmonyLib;
using UnityEngine;

namespace SamMeow.SPT.PerformanceTweaks.Patches
{
    /// <summary>
    /// P7: BotsClass.UpdateByUnity 远距 bot 分帧降频。
    /// 原版:每帧遍历全部 bot 调 UpdateManual,单 bot 异常记录 hashSet_1,尾部 AddFromList()
    /// (BotsClass.cs:273-290)。大图大量 bot 时每帧全量更新是主线程主要 AI 成本。
    /// 补丁:Prefix 替代实现——距主玩家 &lt;= 近距离阈值(默认 100m)的 bot 每帧更新;
    /// 超出阈值的 bot 按 Time.frameCount % N == bot.Id % N 分帧(N 默认 2,可配 1-8;N=1 不降频)。
    /// 原方法的异常语义(hashSet_1 去重记录)与尾调用 AddFromList() 完整保留;
    /// 主玩家位置取 Singleton&lt;GameWorld&gt;.Instance.MainPlayer(GameWorld.cs:570),取不到时
    /// 放行原版完整遍历(fail-open)。整体脚手架包 try/catch,出错放行原版。
    /// 证据:external/decompile-cache/eft-0.16-spt3114/BotsClass.cs:273-290, :37, :43
    /// </summary>
    internal static class BotFrameBudgetPatch
    {
        internal static MethodBase TargetMethod()
        {
            return AccessTools.Method(typeof(BotsClass), "UpdateByUnity");
        }

        // ReSharper disable once InconsistentNaming
        public static bool Prefix(BotsClass __instance)
        {
            if (!PerfTweaksConfig.BotFrameBudgetEnabled.Value)
            {
                return true;
            }
            try
            {
                int n = PerfTweaksConfig.BotFrameBudgetDivisor.Value;
                if (n <= 1 || __instance.hashSet_0 == null || __instance.hashSet_0.Count == 0)
                {
                    // N=1 等价不降频;空集合直接放行原版(原版同样立即返回)
                    return true;
                }
                GameWorld gameWorld = Singleton<GameWorld>.Instance;
                Player mainPlayer = gameWorld?.MainPlayer;
                if (mainPlayer == null)
                {
                    // 取不到主玩家位置,fail-open 完整遍历
                    return true;
                }
                Vector3 mainPos = mainPlayer.Position;
                float near = PerfTweaksConfig.BotFrameBudgetNearMeters.Value;
                float nearSq = near * near;
                int frame = Time.frameCount;
                HashSet<int> failed = __instance.hashSet_1;
                foreach (BotOwner item in __instance.hashSet_0)
                {
                    if ((item.Position - mainPos).sqrMagnitude > nearSq
                        && frame % n != item.Id % n)
                    {
                        continue; // 远距且非本 bot 帧,跳过本次更新
                    }
                    try
                    {
                        item.UpdateManual();
                    }
                    catch (Exception)
                    {
                        if (!failed.Contains(item.Id))
                        {
                            failed.Add(item.Id);
                        }
                    }
                }
                __instance.AddFromList(); // 尾调用必须每次执行(Add 挂起的 bot)
                return false;
            }
            catch
            {
                // 脚手架异常,fail-open 放行原版
                return true;
            }
        }
    }
}
