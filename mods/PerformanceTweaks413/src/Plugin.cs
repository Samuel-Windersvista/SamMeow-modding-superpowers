using System;
using System.IO;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using SamMeow.SPT.PerformanceTweaks413.Patches;

namespace SamMeow.SPT.PerformanceTweaks413
{
    /// <summary>
    /// SPT 4.1.x (EFT 0.16.9.5) 客户端性能优化 — 共 16 补丁（P1-P16）。
    /// 设计原则（沿用 3.11 版纪律，见 docs/eft-0.16.9.5-spt412-性能复查报告.md）：
    ///   1. 全部 Prefix/Postfix，禁用 Transpiler（每补丁独立开关，fail-open）
    ///   2. 目标类型/字段名一律按 4.1.2 反编译树核实，不照抄 3.11 混淆名
    ///   3. P2/P5/P15 默认关闭（调 AI 行为或病灶不成立，谨慎开启）
    ///   4. 不改公开 API、不改数据结构、不序列化状态
    /// </summary>
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    public class PerformanceTweaks413Plugin : BaseUnityPlugin
    {
        public const string ModGuid = "com.sammeow.spt413.performancetweaks";
        public const string ModName = "SamMeow PerformanceTweaks413";
        public const string ModVersion = "0.3.0";

        internal static ManualLogSource Log;
        internal static bool SainDetected;

        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            PerfTweaksConfig.Init(Config);
            SainDetected = DetectSain();
            if (SainDetected)
            {
                // 4.x 生态兼容性审计结论：SAIN 4.x 不 patch CheckLookEnemy / UpdateLook / CalcPath /
                // AddEnemy 语义（其 AddEnemy patch 仅为 null/距离守卫，与本 mod P5 条件正交），
                // 与本 mod 全部补丁零真冲突，无需禁用，共存互补。
                Log.LogInfo("检测到 SAIN — 4.x 审计结论：零真冲突，全部补丁保持启用（互补关系）");
            }

            _harmony = new Harmony(ModGuid);
            int applied = 0;
            // P1 用低优先级 prefix：QuestingBots 4.x 在同方法（CheckLookEnemy）有睡眠 bot 的
            // SetVisible(false) prefix（源码审计确认），我方低优先级保证 QB 的 prefix 先执行，
            // 避免我方截断导致 QB 的睡眠不可见语义被跳过。
            applied += TryPatch(EnemyInfoLookPrunePatch.TargetMethod(), typeof(EnemyInfoLookPrunePatch), "P1 感知距离修剪", Priority.Low);
            applied += TryPatch(LookSensorGroupTuningPatch.TargetMethod(), typeof(LookSensorGroupTuningPatch), "P2 感知调度放宽");
            applied += TryPatch(PausedBotLookSkipPatch.TargetMethod(), typeof(PausedBotLookSkipPatch), "P3 睡眠bot跳过感知");
            applied += TryPatch(CalcPathCooldownPatch.TargetMethod(), typeof(CalcPathCooldownPatch), "P4 寻路冷却");
            applied += TryPatch(AddEnemyDistanceGatePatch.TargetMethod(), typeof(AddEnemyDistanceGatePatch), "P5 初始感知距离门限");
            applied += TryPatch(CameraSetDedupPatch.TargetMethod(), typeof(CameraSetDedupPatch), "P6 相机重复设置去重");
            applied += TryPatch(BotFrameBudgetPatch.TargetMethod(), typeof(BotFrameBudgetPatch), "P7 远距bot分帧降频");
            applied += TryPatch(FlickerThrottlePatch.TargetMethod(), typeof(FlickerThrottlePatch), "P8 灯光闪烁分帧");

            // P9 一个补丁 = 4 个目标方法（2 维护 + 2 查询），任一成功即计该补丁生效
            int p9 = 0;
            p9 += TryPatch(PlayerIndexPatch.TargetRegister(), typeof(PlayerIndexPatch), "RegisterPostfix", null, "P9 玩家查找索引化(注册)");
            p9 += TryPatch(PlayerIndexPatch.TargetUnregister(), typeof(PlayerIndexPatch), "UnregisterPostfix", null, "P9 玩家查找索引化(注销)");
            p9 += TryPatch(PlayerIndexPatch.TargetTryGetAlive(), typeof(PlayerIndexPatch), "TryGetAlivePrefix", null, "P9 玩家查找索引化(存活查询)");
            p9 += TryPatch(PlayerIndexPatch.TargetTryGetObserved(), typeof(PlayerIndexPatch), "TryGetObservedPrefix", null, "P9 玩家查找索引化(观察查询)");
            applied += p9 > 0 ? 1 : 0;

            applied += TryPatch(EnvironmentUpdateThrottlePatch.TargetMethod(), typeof(EnvironmentUpdateThrottlePatch), "P10 环境管理器降频");
            applied += TryPatch(TripwirePrewarmPatch.TargetMethod(), typeof(TripwirePrewarmPatch), "P11 绊线规划器预热");
            applied += TryPatch(BulletCullingSnapshotPatch.TargetMethod(), typeof(BulletCullingSnapshotPatch), "P12 弹道可见性查询降频");

            // 第二批+第三批：P13 烟雾射线距离门限 / P14 本地AI的IK隔帧 / P15 carving同帧去重 /
            // P16 AI扎堆斥力分帧。P15 病灶复查为不成立（各源已有节流），默认关闭的防御性补丁。
            applied += TryPatch(SmokeRayDistanceGatePatch.TargetMethod(), typeof(SmokeRayDistanceGatePatch), "P13 烟雾射线距离门限");
            applied += TryPatch(LocalBotFbbikFrameSkipPatch.TargetMethod(), typeof(LocalBotFbbikFrameSkipPatch), "P14 本地AI的IK隔帧");
            applied += TryPatch(CarvingDedupPatch.TargetMethod(), typeof(CarvingDedupPatch), "P15 carving同帧去重");
            applied += TryPatch(LocalAvoidanceFrameSkipPatch.TargetMethod(), typeof(LocalAvoidanceFrameSkipPatch), "P16 AI扎堆斥力分帧");

            Log.LogInfo($"{ModName} v{ModVersion} 已加载，{applied}/16 个补丁生效");
        }

        private int TryPatch(MethodBase target, Type patchClass, string label, int? prefixPriority = null)
        {
            return TryPatch(target, patchClass, "Prefix", "Postfix", label, prefixPriority);
        }

        /// <summary>
        /// 支持显式指定 prefix/postfix 方法名（多目标补丁如 P9 各方法不同名）。
        /// </summary>
        private int TryPatch(MethodBase target, Type patchClass, string prefixName, string postfixName, string label, int? prefixPriority = null)
        {
            if (target == null)
            {
                Log.LogError($"{label}: 找不到目标方法，补丁未应用");
                return 0;
            }
            try
            {
                MethodInfo prefix = prefixName != null ? patchClass.GetMethod(prefixName) : null;
                MethodInfo postfix = postfixName != null ? patchClass.GetMethod(postfixName) : null;
                _harmony.Patch(target,
                    prefix: prefix != null
                        ? (prefixPriority.HasValue ? new HarmonyMethod(prefix, prefixPriority.Value) : new HarmonyMethod(prefix))
                        : null,
                    postfix: postfix != null ? new HarmonyMethod(postfix) : null);
                Log.LogInfo($"{label}: 已应用 -> {target.DeclaringType?.Name}.{target.Name}");
                return 1;
            }
            catch (Exception ex)
            {
                Log.LogError($"{label}: 应用失败 — {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// 文件级 SAIN 检测：扫描 BepInEx/plugins 下文件名含 sain/solarint 的 DLL。
        /// 文件检测与插件加载顺序无关，比 Chainloader 轮询可靠。
        /// </summary>
        private static bool DetectSain()
        {
            try
            {
                foreach (string dll in Directory.GetFiles(BepInEx.Paths.PluginPath, "*.dll", SearchOption.AllDirectories))
                {
                    string name = Path.GetFileName(dll).ToLowerInvariant();
                    if (name.Contains("sain") || name.Contains("solarint"))
                    {
                        return true;
                    }
                }
            }
            catch (Exception)
            {
                // 扫描失败按未检出处理，不影响补丁应用
            }
            return false;
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
        }
    }
}
