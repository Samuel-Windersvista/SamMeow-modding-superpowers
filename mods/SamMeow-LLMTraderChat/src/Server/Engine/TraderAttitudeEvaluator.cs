using System.Globalization;
using System.Text;
using SamMeow.LLMTraderChat.Server.Config;

namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 动态态度评测与渲染（工单 14）：由 <see cref="TraderRelationshipSnapshot"/> 计算
/// <see cref="AttitudeTier"/>，并渲染注入 system prompt 的「态度与关系背景」块。
///
/// 纯函数、不依赖 SPT 运行时；内置 5 档通用中文关系指令，可由 <see cref="TraderConfig.AttitudePrompts"/>
/// 按档覆写。渲染块始终以反泄露禁令收尾，超总量预算时截断正文但保留禁令。
/// </summary>
public static class TraderAttitudeEvaluator
{
    /// <summary>态度块总量预算（字）。</summary>
    public const int MaxAttitudeTotalChars = 600;

    /// <summary>单个任务名预算（字）。</summary>
    public const int MaxQuestNameChars = 60;

    /// <summary>最多渲染的进行中任务数。</summary>
    public const int MaxActiveQuestsRendered = 5;

    /// <summary>最多渲染的最近完成任务数。</summary>
    public const int MaxRecentCompletedQuestsRendered = 3;

    /// <summary>反泄露禁令（渲染块收尾，恒保留）。</summary>
    private const string AntiLeakDirective =
        "表达禁令：严禁在回复中说出好感度数值、忠诚等级数字（如“1级/LL2/皇冠”）、玩家等级数字或任务内部ID；"
        + "只用符合你人设的自然口吻体现上述亲疏态度与委托记忆。";

    /// <summary>评测态度档位：无快照 → Cold；未解锁或好感度为负 → Hostile；否则按忠诚等级分档（未知视为 1）。</summary>
    public static AttitudeTier EvaluateTier(TraderRelationshipSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return AttitudeTier.Cold;
        }

        if (snapshot.TraderUnlocked == false || (snapshot.Standing.HasValue && snapshot.Standing.Value < 0))
        {
            return AttitudeTier.Hostile;
        }

        var loyaltyLevel = snapshot.LoyaltyLevel ?? 1;
        return loyaltyLevel switch
        {
            <= 1 => AttitudeTier.Cold,
            2 => AttitudeTier.Familiar,
            3 => AttitudeTier.Trusted,
            _ => AttitudeTier.Ally,
        };
    }

    /// <summary>档位 → 配置键（hostile/cold/familiar/trusted/ally）。</summary>
    public static string GetTierConfigKey(AttitudeTier tier) => tier switch
    {
        AttitudeTier.Hostile => TraderAttitudeKeys.Hostile,
        AttitudeTier.Cold => TraderAttitudeKeys.Cold,
        AttitudeTier.Familiar => TraderAttitudeKeys.Familiar,
        AttitudeTier.Trusted => TraderAttitudeKeys.Trusted,
        AttitudeTier.Ally => TraderAttitudeKeys.Ally,
        _ => TraderAttitudeKeys.Cold,
    };

    /// <summary>内置默认档位文案（未被 <see cref="TraderConfig.AttitudePrompts"/> 覆写时使用）。</summary>
    private static string GetBuiltInPrompt(AttitudeTier tier) => tier switch
    {
        AttitudeTier.Hostile =>
            "【敌意/戒备】你对该玩家心存戒备甚至反感（对方尚未取得你的准入资格，或曾损害过你的利益）。"
            + "语气冷硬、警惕、不耐烦或带讥讽，拒绝透露任何私人信息与核心情报。",
        AttitudeTier.Familiar =>
            "【熟稔/认可】该玩家与你已有过几次顺畅合作，算是知根知底的熟客。"
            + "语气平和自然，愿意多聊几句行业见闻与近期风声。",
        AttitudeTier.Trusted =>
            "【信任/器重】该玩家是你的可靠搭档与老主顾，替你办成了不少棘手差事。"
            + "语气热络、关照，愿意分享有价值的内幕消息与个人看法。",
        AttitudeTier.Ally =>
            "【核心盟友/过命交情】该玩家是你最信赖的自己人，与你同生共死、交情极深。"
            + "语气推心置腹、毫无保留，把对方当成真正的朋友与王牌拍档。",
        _ =>
            "【冷淡/生疏】你与该玩家只是初识或普通买卖关系，尚无私交。"
            + "语气公事公办、保持距离、言简意赅，只谈眼前生意，不主动寒暄。",
    };

    /// <summary>诊断串：供服务端日志判读档位与关键信号（不含敏感原文）。</summary>
    public static string FormatDiagnosticSummary(AttitudeTier tier, TraderRelationshipSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return $"{tier}(snapshot=null)";
        }

        return $"{tier}(unlocked={snapshot.TraderUnlocked?.ToString() ?? "null"},"
            + $"ll={snapshot.LoyaltyLevel?.ToString() ?? "null"},"
            + $"standing={snapshot.Standing?.ToString("F2", CultureInfo.InvariantCulture) ?? "null"},"
            + $"done={snapshot.CompletedQuestCount},fail={snapshot.FailedQuestCount},"
            + $"active={snapshot.ActiveQuests?.Count ?? 0})";
    }

    /// <summary>
    /// 渲染注入 system prompt 的态度块：关系阶段（档位文案）+ 玩家背景（阵营/等级带）+ 委托记录
    /// + 收尾反泄露禁令。超总量预算时截断正文并记录告警，禁令恒保留。
    /// </summary>
    public static string RenderAttitudeBlock(
        TraderRelationshipSnapshot? snapshot, TraderConfig trader, List<string> warnings)
    {
        var tier = EvaluateTier(snapshot);

        var builder = new StringBuilder();
        builder.Append("关系阶段：").Append(ResolveTierPrompt(trader, tier));

        var playerLine = RenderPlayerLine(snapshot);
        if (playerLine is not null)
        {
            builder.Append('\n').Append(playerLine);
        }

        builder.Append('\n').Append(RenderQuestLine(snapshot));

        var body = builder.ToString();
        var full = body + "\n" + AntiLeakDirective;
        if (full.Length <= MaxAttitudeTotalChars)
        {
            return full;
        }

        warnings.Add($"[LLMTC][engine] 态度渲染截断：超出总量预算 {MaxAttitudeTotalChars} 字已截断");
        var allowed = MaxAttitudeTotalChars - AntiLeakDirective.Length - 1;
        if (allowed < 0)
        {
            allowed = 0;
        }

        var truncated = body.Length <= allowed ? body : body[..allowed];
        return truncated.Length == 0 ? AntiLeakDirective : truncated + "\n" + AntiLeakDirective;
    }

    /// <summary>取档位文案：优先 per-trader 覆写（trim 后非空），否则内置默认。</summary>
    private static string ResolveTierPrompt(TraderConfig trader, AttitudeTier tier)
    {
        var key = GetTierConfigKey(tier);
        if (trader.AttitudePrompts is not null
            && trader.AttitudePrompts.TryGetValue(key, out var custom)
            && !string.IsNullOrWhiteSpace(custom))
        {
            return custom.Trim();
        }

        return GetBuiltInPrompt(tier);
    }

    /// <summary>渲染玩家背景行（阵营 + 等级带）；无有效信号返回 null。不输出原始等级数字。</summary>
    private static string? RenderPlayerLine(TraderRelationshipSnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return null;
        }

        var side = string.IsNullOrWhiteSpace(snapshot.PlayerSide) ? null : MapSide(snapshot.PlayerSide);
        var band = snapshot.PlayerLevel is > 0 ? MapLevelBand(snapshot.PlayerLevel.Value) : null;

        if (side is null && band is null)
        {
            return null;
        }

        if (side is not null && band is not null)
        {
            return $"玩家背景：{side} 阵营佣兵（{band}）";
        }

        return side is not null ? $"玩家背景：{side} 阵营佣兵" : $"玩家背景：{band}";
    }

    private static string MapSide(string side) => side.ToLowerInvariant() switch
    {
        "usec" => "USEC",
        "bear" => "BEAR",
        _ => side,
    };

    private static string MapLevelBand(int level) => level switch
    {
        < 15 => "初入塔科夫的新手",
        <= 34 => "有一定经验的佣兵",
        _ => "身经百战的老手",
    };

    /// <summary>渲染委托记录行；无任何委托信号时给出「暂无记录」。</summary>
    private static string RenderQuestLine(TraderRelationshipSnapshot? snapshot)
    {
        if (snapshot is null
            || (snapshot.CompletedQuestCount == 0
                && snapshot.FailedQuestCount == 0
                && (snapshot.ActiveQuests is null || snapshot.ActiveQuests.Count == 0)
                && (snapshot.RecentlyCompletedQuestNames is null || snapshot.RecentlyCompletedQuestNames.Count == 0)))
        {
            return "委托记录：该玩家在你这里暂无任务委托记录。";
        }

        var builder = new StringBuilder();
        builder.Append($"委托记录：已完成 {snapshot.CompletedQuestCount} 项委托，失败 {snapshot.FailedQuestCount} 项。");

        if (snapshot.ActiveQuests is { Count: > 0 } active)
        {
            var items = active
                .Take(MaxActiveQuestsRendered)
                .Select(quest =>
                    $"《{Truncate(quest.Name ?? string.Empty, MaxQuestNameChars)}》"
                    + $"（{(quest.ReadyToFinish ? "待交付" : "进行中")}）");
            builder.Append("当前进行中：").Append(string.Join("、", items)).Append('。');
        }

        if (snapshot.RecentlyCompletedQuestNames is { Count: > 0 } recent)
        {
            var names = recent
                .Take(MaxRecentCompletedQuestsRendered)
                .Select(name => $"《{Truncate(name ?? string.Empty, MaxQuestNameChars)}》");
            builder.Append("近期刚完成：").Append(string.Join("、", names)).Append('。');
        }

        return builder.ToString();
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength];
}
