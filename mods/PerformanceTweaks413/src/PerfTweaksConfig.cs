using BepInEx.Configuration;

namespace SamMeow.SPT.PerformanceTweaks413
{
    /// <summary>
    /// BepInEx 配置（全中文，人话版）。每个补丁独立开关，F12 ConfigurationManager 热调。
    /// 配置文件：BepInEx/config/com.sammeow.spt413.performancetweaks.cfg
    /// 说明风格：游戏里发生了什么 → 你会感到什么 → 副作用/调节建议。
    /// P2/P5 默认关闭：两者直接改 AI 感知/仇恨行为，须由 Overseer 明确开启。
    /// </summary>
    internal static class PerfTweaksConfig
    {
        // ===== P1 感知距离修剪 =====
        internal static ConfigEntry<bool> LookPruneEnabled;
        internal static ConfigEntry<float> LookPruneDistanceMultiplier;

        // ===== P2 感知调度放宽（默认关闭） =====
        internal static ConfigEntry<bool> LookGroupTuningEnabled;
        internal static ConfigEntry<float> LookGroupPeriod;
        internal static ConfigEntry<float> LookGroupMaxPeriod;

        // ===== P3 睡眠bot跳过感知 =====
        internal static ConfigEntry<bool> PausedBotLookSkipEnabled;

        // ===== P4 寻路冷却 =====
        internal static ConfigEntry<bool> PathCooldownEnabled;
        internal static ConfigEntry<float> PathCooldownSeconds;
        internal static ConfigEntry<float> PathRetargetThresholdMeters;

        // ===== P5 初始感知距离门限（默认关闭） =====
        internal static ConfigEntry<bool> AggroDistanceGateEnabled;
        internal static ConfigEntry<float> AggroDistanceMeters;

        // ===== P6 相机重复设置去重 =====
        internal static ConfigEntry<bool> CameraSetDedupEnabled;

        // ===== P7 远距bot分帧降频 =====
        internal static ConfigEntry<bool> BotFrameBudgetEnabled;
        internal static ConfigEntry<float> BotFrameBudgetNearMeters;
        internal static ConfigEntry<int> BotFrameBudgetDivisor;

        // ===== P8 灯光闪烁分帧 =====
        internal static ConfigEntry<bool> FlickerThrottleEnabled;
        internal static ConfigEntry<int> FlickerThrottleDivisor;

        // ===== P9 玩家查找索引化 =====
        internal static ConfigEntry<bool> PlayerIndexEnabled;

        // ===== P10 环境管理器降频 =====
        internal static ConfigEntry<bool> EnvironmentThrottleEnabled;
        internal static ConfigEntry<int> EnvironmentThrottleDivisor;

        // ===== P11 绊线规划器预热 =====
        internal static ConfigEntry<bool> TripwirePrewarmEnabled;

        // ===== P12 弹道可见性查询降频 =====
        internal static ConfigEntry<bool> BulletCullingSnapshotEnabled;
        internal static ConfigEntry<int> BulletCullingSnapshotInterval;

        // ===== P13 烟雾射线距离门限 =====
        internal static ConfigEntry<bool> SmokeRayDistanceGateEnabled;
        internal static ConfigEntry<float> SmokeRayDistanceMeters;

        // ===== P14 本地AI的IK隔帧 =====
        internal static ConfigEntry<bool> FbbikFrameSkipEnabled;

        // ===== P15 carving同帧去重（默认关闭：病灶不成立，防御性） =====
        internal static ConfigEntry<bool> CarvingDedupEnabled;

        // ===== P16 AI扎堆斥力分帧 =====
        internal static ConfigEntry<bool> AvoidanceFrameSkipEnabled;
        internal static ConfigEntry<int> AvoidanceCrowdThreshold;

        internal static void Init(ConfigFile config)
        {
            // ========== P1 感知距离修剪 ==========
            LookPruneEnabled = config.Bind("P1 感知距离修剪", "启用", true,
                "AI 不再检查视距外且看不见的敌人。人多图和交火时卡顿减少。无副作用，建议常开。");
            LookPruneDistanceMultiplier = config.Bind("P1 感知距离修剪", "视距倍率", 1.5f,
                new ConfigDescription(
                    "超过 AI 视距 N 倍的敌人跳过检查。默认 1.5。调大更保守（收益小），调小更激进（收益大）。装了 That's Lit 建议调到 2.0 以上，避免砍掉它的夜视/光照远距补偿。",
                    new AcceptableValueRange<float>(1.0f, 5.0f)));

            // ========== P2 感知调度放宽 ==========
            LookGroupTuningEnabled = config.Bind("P2 感知调度放宽", "启用", false,
                "【默认关闭，调 AI 行为，谨慎开启】AI 的'东张西望'节奏整体放缓。bot 多时帧数更稳，AI 发现你慢零点几秒（贴脸对枪不受影响）。");
            LookGroupPeriod = config.Bind("P2 感知调度放宽", "感知任务周期(秒)", 0.2f,
                new ConfigDescription(
                    "原版 0.1 秒，默认放宽到 0.2。调大更省 CPU 但 AI 反应变钝，超过 0.5 秒会明显感觉 AI'发呆'。",
                    new AcceptableValueRange<float>(0.1f, 1.0f)));
            LookGroupMaxPeriod = config.Bind("P2 感知调度放宽", "感知任务周期上限(秒)", 1.0f,
                new ConfigDescription(
                    "bot 特别多时感知节奏自动放宽的上限，原版 0.6 秒，默认 1.0。调大更省 CPU；近距离 AI 不受影响。",
                    new AcceptableValueRange<float>(0.6f, 3.0f)));

            // ========== P3 睡眠bot跳过感知 ==========
            PausedBotLookSkipEnabled = config.Bind("P3 睡眠bot跳过感知", "启用", true,
                "修复原版漏洞：睡觉的 AI（130 米外休眠）不再偷偷做视线检查。战局前期帧数提升。无副作用，建议常开。");

            // ========== P4 寻路冷却 ==========
            PathCooldownEnabled = config.Bind("P4 寻路冷却", "启用", true,
                "AI 短时间内重复算路时沿用上次的路线，不再每次硬算。AI 集体移动时的'突然卡一下'减少。");
            PathCooldownSeconds = config.Bind("P4 寻路冷却", "冷却时间(秒)", 0.5f,
                new ConfigDescription(
                    "默认 0.5 秒内不重复算路。调大更省 CPU 但 AI 追人会'木'；超过 2 秒可能出现原地转圈。",
                    new AcceptableValueRange<float>(0.1f, 3.0f)));
            PathRetargetThresholdMeters = config.Bind("P4 寻路冷却", "重算阈值(米)", 2.0f,
                new ConfigDescription(
                    "目标挪出这个距离就无视冷却立刻重新算路。默认 2 米。不用动它；调太大 AI 会朝旧位置跑。",
                    new AcceptableValueRange<float>(0.5f, 20.0f)));

            // ========== P5 初始感知距离门限 ==========
            AggroDistanceGateEnabled = config.Bind("P5 初始感知距离门限", "启用", false,
                "【默认关闭，调 AI 行为，谨慎开启】战局开局时，远处的 AI 不再把你登记进'仇人名单'。开枪、被打、队友报点不受影响——你暴露了自己 AI 照样知道。与 SAIN 4.x 的 AddEnemy 守卫 prefix 条件正交可共存，但为避免影响 SAIN 的敌人登记链路，默认关闭。");
            AggroDistanceMeters = config.Bind("P5 初始感知距离门限", "初始感知距离(米)", 120f,
                new ConfigDescription(
                    "开局登记的最大距离。默认 120 米（AI 休眠唤醒距离是 110 米，别调得比 110 低，否则被唤醒的 AI 可能第一时间不知道你在哪）。",
                    new AcceptableValueRange<float>(110f, 500f)));

            // ========== P6 相机重复设置去重 ==========
            CameraSetDedupEnabled = config.Bind("P6 相机重复设置去重", "启用", true,
                "原版每帧都把主相机拆掉重装一遍，现在相机没变就跳过。白捡的帧时间。无副作用，建议常开。");

            // ========== P7 远距bot分帧降频 ==========
            BotFrameBudgetEnabled = config.Bind("P7 远距bot分帧降频", "启用", true,
                "远处的 AI 改成隔帧思考，近处的全速思考。大图帧数提升最明显的来源。注意：这是所有补丁里行为影响最大的一个——远处 AI 反应会慢约 1/30 秒，打远处狙击 AI 可能察觉它'愣一下'。觉得怪就单独关它。");
            BotFrameBudgetNearMeters = config.Bind("P7 远距bot分帧降频", "近距离阈值(米)", 100f,
                new ConfigDescription(
                    "这个距离内的 AI 永远全速思考。默认 100 米覆盖绝大多数交火距离。调小收益更大，但中远距离 AI 会变迟钝。",
                    new AcceptableValueRange<float>(10f, 300f)));
            BotFrameBudgetDivisor = config.Bind("P7 远距bot分帧降频", "降频帧间隔N", 2,
                new ConfigDescription(
                    "远处 AI 每 N 帧思考一次。默认 2（一半频率）。建议不超过 4，否则远处 AI 行为会一卡一卡的。",
                    new AcceptableValueRange<int>(1, 8)));

            // ========== P8 灯光闪烁分帧 ==========
            FlickerThrottleEnabled = config.Bind("P8 灯光闪烁分帧", "启用", true,
                "地图上闪烁的灯（霓虹、坏灯管）不再每帧重算亮度。灯光多的室内图微幅提升。视觉无感。");
            FlickerThrottleDivisor = config.Bind("P8 灯光闪烁分帧", "降频帧间隔N", 2,
                new ConfigDescription(
                    "灯每 N 帧算一次亮度。默认 2。建议不超过 4，否则警报灯这类快速闪烁会有顿挫感。",
                    new AcceptableValueRange<int>(1, 8)));

            // ========== P9 玩家查找索引化 ==========
            PlayerIndexEnabled = config.Bind("P9 玩家查找索引化", "启用", true,
                "原版按 ID 查玩家要把全图列表从头翻到尾（弹道命中判定就在高频用它），现在建了索引一步到位。多人互射、弹片横飞时卡顿减少。无副作用，建议常开。");

            // ========== P10 环境管理器降频 ==========
            EnvironmentThrottleEnabled = config.Bind("P10 环境管理器降频", "启用", true,
                "阴影距离和曝光适应从每帧改成隔帧计算。微小但免费的帧时间改善。进出室内时的曝光过渡依然无感。");
            EnvironmentThrottleDivisor = config.Bind("P10 环境管理器降频", "降频帧间隔N", 2,
                new ConfigDescription(
                    "每 N 帧算一次环境参数。默认 2。建议不超过 3，否则快速进出室内时曝光适应会有点跟不上。",
                    new AcceptableValueRange<int>(1, 4)));

            // ========== P11 绊线规划器预热 ==========
            TripwirePrewarmEnabled = config.Bind("P11 绊线规划器预热", "启用", true,
                "战局开始时提前加载绊线工具，消除第一次放绊线时的卡顿。不玩绊线也无害。");

            // ========== P12 弹道可见性查询降频 ==========
            BulletCullingSnapshotEnabled = config.Bind("P12 弹道可见性查询降频", "启用", true,
                "'要不要渲染这颗在飞的子弹'这个判断不再每帧重算，缓存一帧再用。大规模枪战掉帧减轻。");
            BulletCullingSnapshotInterval = config.Bind("P12 弹道可见性查询降频", "缓存帧数N", 2,
                new ConfigDescription(
                    "判断结果缓存 N 帧。默认 2。建议不超过 3，否则快速甩视角时曳光弹显示可能慢一两帧。",
                    new AcceptableValueRange<int>(1, 5)));

            // ========== P13 烟雾射线距离门限 ==========
            SmokeRayDistanceGateEnabled = config.Bind("P13 烟雾射线距离门限", "启用", true,
                "4.1 新版 AI 视线检查都要额外'穿没穿烟雾'（一条条体素翻过去）。把距离门限放到烟雾检查前面——目标太远直接不看，烟雾检查也就不跑了。有烟雾弹的战局（扔烟撤离/掩护）帧数不降反升。副作用：AI 对 150 米外的烟雾完全无视（本来就看不见那么远的烟，不影响近战）。");
            SmokeRayDistanceMeters = config.Bind("P13 烟雾射线距离门限", "门限距离(米)", 150f,
                new ConfigDescription(
                    "超过这个距离的视线检查直接跳过烟雾体素遍历。默认 150 米（远大于 AI 正常视线距离，几乎只在罕见场景触发）。调小更激进；别低于 80 米，否则中远距离交火时烟雾掩护会失效。",
                    new AcceptableValueRange<float>(50f, 500f)));

            // ========== P14 本地AI的IK隔帧 ==========
            FbbikFrameSkipEnabled = config.Bind("P14 本地AI的IK隔帧", "启用", true,
                "AI 的手脚贴合（端枪姿势、踩地）每帧都完整解算，联机路径早就隔 3 帧算了，单机路径反而没有。现在对齐：40 米内全速，40-80 米隔 2 帧，80 米外隔 3 帧。bot 密集近战时帧数更稳。副作用：远处 AI 手脚贴合精度略降，肉眼难察。");

            // ========== P15 carving同帧去重 ==========
            CarvingDedupEnabled = config.Bind("P15 carving同帧去重", "启用", false,
                "【默认关闭】病灶复查结论：'同一个导航障碍物在同一帧被重复赋值'在 4.1.2 代码里实际不存在（门有 1 秒节流、路障有 300 秒节流、火车是事件驱动），真病灶是'多个障碍物同帧并发'——消除它需要延迟合批（门开了导航却不更新 = bot 卡门），被议会红线禁止。本开关保留为防御性：同帧去重不改时序，只消掉同一帧内对同一障碍物的同值冗余赋值。普通玩家不用开。");

            // ========== P16 AI扎堆斥力分帧 ==========
            AvoidanceFrameSkipEnabled = config.Bind("P16 AI扎堆斥力分帧", "启用", true,
                "AI 挤在一起时（撤离点、门口、僵尸群）每帧都互相推挤，一多就是 O(n²) 开销。同区域挤满 N 个 AI 时改成隔帧推挤，稀疏时原样执行。扎堆场景帧尖峰减轻。副作用：只隔 1 帧，AI 不会因此重叠穿模，但隔帧时推挤响应慢一帧。");
            AvoidanceCrowdThreshold = config.Bind("P16 AI扎堆斥力分帧", "拥挤阈值N", 4,
                new ConfigDescription(
                    "同区域 AI 数达到这个数才启动隔帧。默认 4。调小更省 CPU（但 3 个 AI 面对面时推挤会稍钝），调大更保守。",
                    new AcceptableValueRange<int>(3, 10)));
        }
    }
}
