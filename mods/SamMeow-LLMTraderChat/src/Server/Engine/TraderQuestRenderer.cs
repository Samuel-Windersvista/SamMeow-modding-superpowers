using System.Text;

namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 任务知识渲染（工单 16；修订 R1/R3/R4）：按 <see cref="DisclosureLevel"/> 把玩家在当前商人名下
/// 进行中的委托渲染为注入 system prompt 的 <c>{{quests}}</c> 段。纯函数、不依赖 SPT 运行时。
///
/// 门控：None → 不注入任何委托名或细节（仅保留反编造约束）；Hint → 仅委托名 + 地图/地区级提示
/// + 一句通用引导（不含房间号 / 钥匙名 / 目标原文）；Full → 委托名 + 目标明细 + 描述摘要
/// （≤ <see cref="MaxDescriptionChars"/> 字）。
/// 预算：单条委托 ≤ <see cref="MaxQuestItemChars"/> 字，整段 ≤ <see cref="MaxQuestTotalChars"/> 字
/// （含收尾禁令；独立于 <see cref="TraderAttitudeEvaluator"/> 的 600 字预算）；超预算丢弃尾部条目并告警。
/// 委托条数上限由提供器单一负责（<see cref="TraderAttitudeEvaluator.MaxActiveQuestsRendered"/>，与态度段共享）；
/// 本渲染器不设自身条数上限。
///
/// 修订 R1：反编造禁令在**所有档位**恒存（含 None / 零委托 / 截断路径）——取代早期「None/零委托 → 空段」行为，
/// 因实机缺陷场景（Cold 档 → None）下旧行为会使 prompt 完全缺失反编造约束。
/// </summary>
public static class TraderQuestRenderer
{
    /// <summary>任务知识整段总量预算（字），含收尾禁令。</summary>
    public const int MaxQuestTotalChars = 800;

    /// <summary>单条委托渲染预算（字）。</summary>
    public const int MaxQuestItemChars = 500;

    /// <summary>Full 档委托描述摘要预算（字）。</summary>
    public const int MaxDescriptionChars = 300;

    /// <summary>段标题（自描述，便于旧模板追加后仍可读）。</summary>
    private const string Caption = "当前进行中的委托（信息以你实际掌握的为准）：";

    /// <summary>
    /// Hint 档通用引导句（修订 R3）：不含任何地区 / 房间 / 钥匙 / 目标信息。
    ///
    /// 此处**有意偏离**字面规格「地图名 + 大致区域/目标类型」：从目标文本派生「目标类型」有泄露具体
    /// 细节的风险，故 Hint 维持「委托名 + 地区 + 通用引导」，不合成目标类型。
    /// </summary>
    private const string HintGuidance = "具体走位与要带的东西我不多嘴，你先看任务面板，拿不准再来问我。";

    /// <summary>反编造硬约束（段收尾，恒保留）。</summary>
    private const string AntiFabricationDirective =
        "任务知识约束：严禁编造；不知道就明说不知道；禁止用「不归我管」「我记不清」之类借口搪塞；"
        + "不得报出内部数值或任务内部 ID。";

    /// <summary>
    /// 渲染任务知识段。None 档与零委托 → 只输出反编造约束行（无委托名、无细节、无其他噪音）。
    /// 超预算时丢弃尾部条目并记录 <c>[LLMTC][engine]</c> 告警；反编造禁令恒存在。
    /// </summary>
    public static string RenderQuestBlock(
        IReadOnlyList<TraderQuestDetail>? quests, DisclosureLevel level, List<string> warnings)
    {
        // 修订 R1：本段恒以反编造禁令收尾——所有档位、零委托亦然。
        if (level == DisclosureLevel.None || quests is null || quests.Count == 0)
        {
            return AntiFabricationDirective;
        }

        var builder = new StringBuilder(Caption);
        var hintGuidance = level == DisclosureLevel.Hint ? HintGuidance : null;

        // 为收尾禁令（与 Hint 引导句）预留空间，保证整段 ≤ MaxQuestTotalChars。
        var reserve = AntiFabricationDirective.Length + 1;
        if (hintGuidance is not null)
        {
            reserve += hintGuidance.Length + 1;
        }

        var questsBudget = MaxQuestTotalChars - reserve;

        var perItemTruncated = 0;
        var dropped = 0;
        for (var index = 0; index < quests.Count; index++)
        {
            var block = RenderQuest(quests[index], level);
            if (block.Length > MaxQuestItemChars)
            {
                block = Truncate(block, MaxQuestItemChars);
                perItemTruncated++;
            }

            if (builder.Length + 1 + block.Length > questsBudget)
            {
                dropped = quests.Count - index; // 本条及后续（更低优先）丢弃
                break;
            }

            builder.Append('\n').Append(block);
        }

        if (hintGuidance is not null)
        {
            builder.Append('\n').Append(hintGuidance);
        }

        builder.Append('\n').Append(AntiFabricationDirective);

        if (perItemTruncated > 0)
        {
            warnings.Add($"[LLMTC][engine] 任务知识渲染截断：{perItemTruncated} 条超出单任务预算 {MaxQuestItemChars} 字已截断");
        }

        if (dropped > 0)
        {
            warnings.Add(
                $"[LLMTC][engine] 任务知识渲染截断：任务段超出 {MaxQuestTotalChars} 字预算，"
                + $"已丢弃 {dropped} 条较低优先级委托");
        }

        return builder.ToString();
    }

    /// <summary>渲染单条委托：Hint 仅名 + 地区；Full 追加目标明细与描述摘要。</summary>
    private static string RenderQuest(TraderQuestDetail quest, DisclosureLevel level)
    {
        var builder = new StringBuilder();
        builder.Append('《').Append(string.IsNullOrWhiteSpace(quest.Name) ? "未命名委托" : quest.Name.Trim()).Append('》');
        var location = string.IsNullOrWhiteSpace(quest.LocationName) ? null : quest.LocationName.Trim();

        if (level == DisclosureLevel.Hint)
        {
            // 修订 R3：地区不可解析时省略括号，不打印「未知」；Hint 绝不带出目标原文、房间号或钥匙名。
            if (location is not null)
            {
                builder.Append("（地区：").Append(location).Append('）');
            }

            return builder.ToString();
        }

        if (location is not null)
        {
            builder.Append("（地区：").Append(location).Append('）');
        }

        if (quest.Objectives is { Count: > 0 } objectives)
        {
            foreach (var objective in objectives)
            {
                if (string.IsNullOrWhiteSpace(objective))
                {
                    continue;
                }

                builder.Append("\n- ").Append(objective.Trim());
            }
        }

        var excerpt = string.IsNullOrWhiteSpace(quest.DescriptionExcerpt) ? null : quest.DescriptionExcerpt.Trim();
        if (excerpt is not null)
        {
            builder.Append("\n说明：").Append(Truncate(excerpt, MaxDescriptionChars));
        }

        return builder.ToString();
    }

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..maxLength];
}
