namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>system prompt 模板装载结果。</summary>
/// <param name="Template">模板文本（可能为内置默认模板）。</param>
/// <param name="UsedFallback">是否回退到内置默认模板。</param>
/// <param name="Warning">回退原因（非 null 时接线应响亮记录）；正常读取为 null。</param>
public sealed record PromptTemplateResult(string Template, bool UsedFallback, string? Warning);

/// <summary>
/// system prompt 模板来源抽象。实现负责按路径读取模板；缺文件/读失败时**响亮兜底**
/// （返回内置默认模板 + 告警），绝不返回静默空模板。
/// </summary>
public interface IPromptTemplateProvider
{
    /// <summary>读取 system prompt 模板；<paramref name="relativePath"/> 相对 mod 根目录。</summary>
    PromptTemplateResult GetSystemPrompt(string relativePath);
}
