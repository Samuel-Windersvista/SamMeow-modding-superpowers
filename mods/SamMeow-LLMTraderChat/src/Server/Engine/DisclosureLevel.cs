namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 披露档位——商人对委托细节的交代程度（工单 16）：不披露（None）/ 只给方向性线索（Hint）/ 交底（Full）。
/// 由态度档位映射决定，是「低好感不说、高好感才交底」的机制化。
/// </summary>
public enum DisclosureLevel
{
    /// <summary>不披露：本段不注入任何委托详情。</summary>
    None = 0,

    /// <summary>只给方向性线索：仅委托名 + 地图/地区级提示，不给房间号 / 钥匙名 / 目标原文。</summary>
    Hint = 1,

    /// <summary>交底：委托名 + 目标明细 + 描述摘要（含玩家在任务面板本就能看到的房间 / 钥匙等信息）。</summary>
    Full = 2,
}

/// <summary>披露门控：态度档位 → 披露档位的唯一映射来源（工单 16）。</summary>
public static class DisclosurePolicy
{
    /// <summary>态度档位 → 披露档位：Hostile/Cold → None；Familiar → Hint；Trusted/Ally → Full。</summary>
    public static DisclosureLevel FromAttitudeTier(AttitudeTier tier) => tier switch
    {
        AttitudeTier.Hostile => DisclosureLevel.None,
        AttitudeTier.Cold => DisclosureLevel.None,
        AttitudeTier.Familiar => DisclosureLevel.Hint,
        AttitudeTier.Trusted => DisclosureLevel.Full,
        AttitudeTier.Ally => DisclosureLevel.Full,
        _ => DisclosureLevel.None,
    };
}
