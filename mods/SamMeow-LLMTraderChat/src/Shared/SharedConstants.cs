using System.Collections.Generic;

namespace SamMeow.LLMTraderChat.Shared;

/// <summary>
/// 两端共享的纯数据 / 常量，避免各写一份导致漂移。
/// 本工程不得引用 SPT 或 BepInEx 类型，只放能被 netstandard2.0（Shared/客户端）与 net10.0（服务端）同时消费的内容。
/// </summary>
public static class SharedConstants
{
    /// <summary>全局唯一 ID（客户端 [BepInPlugin] 与服务端 IModMetadata.ModGuid 共用同一值）。</summary>
    public const string ModGuid = "sammeow.llmtraderchat";

    /// <summary>人类可读的 mod 名。</summary>
    public const string ModName = "LLM Trader Chat";

    /// <summary>客户端与服务端握手用的协议版本；两端数据结构变更时递增。</summary>
    public const int ProtocolVersion = 1;

    // ===================== v1 试点 + 工单 08（保留原常量） =====================

    /// <summary>v1 试点商人 Therapist（MongoId 取自 4.1.6 trader base.json）。</summary>
    public const string TherapistTraderId = "54cb57776803fa99248b456e";

    /// <summary>Therapist 显示名。</summary>
    public const string TherapistTraderName = "Therapist";

    /// <summary>v1 试点商人 Peacekeeper（MongoId 取自 4.1.6 trader base.json）。</summary>
    public const string PeacekeeperTraderId = "5935c25fb3acc3127c3d8cd9";

    /// <summary>Peacekeeper 显示名。</summary>
    public const string PeacekeeperTraderName = "Peacekeeper";

    /// <summary>
    /// 工单 08 接入的现成 mod 商人 Painter（「油漆匠」；MongoId 取自《永远的诺文斯克》包内
    /// Painter-4.0 的 <c>db/base.json</c>）。用于验证「配置驱动、零代码接入」。
    /// </summary>
    public const string PainterTraderId = "668aaff35fd574b6dcc4a686";

    /// <summary>Painter 显示名。</summary>
    public const string PainterTraderName = "Painter";

    // ===================== 全商人接入批次：原版商人（12） =====================
    // MongoId 取自 4.1.6 SPT_Data/database/traders/<id>/base.json（经 en/ch 本地化核对）。

    /// <summary>普拉波（Prapor）。</summary>
    public const string PraporTraderId = "54cb50c76803fa8b248b4571";

    /// <summary>老栅栏（Fence）。</summary>
    public const string FenceTraderId = "579dc571d53a0658a154fbec";

    /// <summary>滑雪客（Skier）。</summary>
    public const string SkierTraderId = "58330581ace78e27b8b10cee";

    /// <summary>机械师（Mechanic）。</summary>
    public const string MechanicTraderId = "5a7c2eca46aef81a7ca2145d";

    /// <summary>拉格曼（Ragman）。</summary>
    public const string RagmanTraderId = "5ac3b934156ae10c4430e83c";

    /// <summary>猎人（Jaeger）。</summary>
    public const string JaegerTraderId = "5c0647fdd443bc2504c2d371";

    /// <summary>守灯人（Lightkeeper；SPT base.json nickname 为 caretaker）。</summary>
    public const string LightkeeperTraderId = "638f541a29ffd1183d187f57";

    /// <summary>БТР 司机（BTR Driver）。</summary>
    public const string BtrDriverTraderId = "656f0f98d80a697f855d34b1";

    /// <summary>竞技场裁判（Ref；SPT base.json nickname 为 Arena）。</summary>
    public const string RefTraderId = "6617beeaa9cfa777ca915b7c";

    /// <summary>说书人（Storyteller；SPT 特有商人）。</summary>
    public const string StorytellerTraderId = "6864e812f9fe664cb8b8e152";

    // ===================== 全商人接入批次：mod 商人（7，含既有 Painter） =====================
    // MongoId 取自《永远的诺文斯克》各 mod 的 db/data 目录（见 .scratch/llm-trader-chat/issues/13）。

    /// <summary>阿特姆（Artem；WTT-Artem）。</summary>
    public const string ArtemTraderId = "66bf757f27d0b097db0acea5";

    /// <summary>毒蝎（Scorpion；acidphantasm-scorpion）。</summary>
    public const string ScorpionTraderId = "6688d464bc40c867f60e7d7e";

    /// <summary>荷官（Croupier）。</summary>
    public const string CroupierTraderId = "1337bb0dd843363fcd1be869";

    /// <summary>藏身处物资商人哈里（Hideout Harry；acidphantasm-harryhideout）。</summary>
    public const string HideoutHarryTraderId = "67419e9d0d4541ce671543bb";

    /// <summary>獾（Badger；Epic's All in One 的 TraderBadger）。</summary>
    public const string BadgerTraderId = "bd3a8b28356d9c6509966546";

    /// <summary>快递员（Courier；PIT Fireteam 补给线投递身份）。</summary>
    public const string CourierTraderId = "67d3a28a3d6f4f7dbd09ed13";

    /// <summary>
    /// 客户端默认解锁的商人 id 列表（全商人接入批次：原版 12 + mod 7，共 19 位）。
    /// 客户端可就地覆盖（见客户端配置文件），无需重编译。
    /// </summary>
    public static IReadOnlyList<string> DefaultEnabledTraderIds { get; } =
        new[]
        {
            // 原版（12）
            PraporTraderId,
            TherapistTraderId,
            FenceTraderId,
            SkierTraderId,
            PeacekeeperTraderId,
            MechanicTraderId,
            RagmanTraderId,
            JaegerTraderId,
            LightkeeperTraderId,
            BtrDriverTraderId,
            RefTraderId,
            StorytellerTraderId,
            // mod（7）
            PainterTraderId,
            ArtemTraderId,
            ScorpionTraderId,
            CroupierTraderId,
            HideoutHarryTraderId,
            BadgerTraderId,
            CourierTraderId,
        };
}
