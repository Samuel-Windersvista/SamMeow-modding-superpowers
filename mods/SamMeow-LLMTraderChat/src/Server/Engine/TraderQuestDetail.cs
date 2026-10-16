namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 单条委托详情（工单 16；纯模型，不依赖 SPT 运行时类型）。
/// 供 <see cref="TraderQuestRenderer"/> 按披露档位渲染；文本预算（描述摘要等）由渲染层单一负责。
/// </summary>
/// <param name="Name">委托名称（已解析为中文）。</param>
/// <param name="Objectives">目标/清单文本（取自 AvailableForFinish 条件的裸条件 id locale 键）。</param>
/// <param name="DescriptionExcerpt">委托描述原文（渲染层按 <see cref="TraderQuestRenderer.MaxDescriptionChars"/> 截断为摘要）；无则 null。</param>
/// <param name="LocationName">地图/地区名（Hint 档仅暴露此级线索）；无则 null。</param>
public sealed record TraderQuestDetail(
    string Name,
    IReadOnlyList<string> Objectives,
    string? DescriptionExcerpt,
    string? LocationName);
