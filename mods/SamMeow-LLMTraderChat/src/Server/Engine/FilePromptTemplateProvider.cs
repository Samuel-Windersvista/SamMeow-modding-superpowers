namespace SamMeow.LLMTraderChat.Server.Engine;

/// <summary>
/// 从 mod 根目录按相对路径读取 system prompt 模板文件的实现。
/// 缺文件 / 空文件 / 读失败一律回退内置默认模板并给出告警（不静默、不抛异常）。
/// </summary>
public sealed class FilePromptTemplateProvider : IPromptTemplateProvider
{
    private readonly string _modRootDirectory;

    /// <summary>以 mod 根目录构造（配置文件中的模板路径相对此目录）。</summary>
    public FilePromptTemplateProvider(string modRootDirectory)
    {
        _modRootDirectory = modRootDirectory ?? throw new ArgumentNullException(nameof(modRootDirectory));
    }

    /// <inheritdoc />
    public PromptTemplateResult GetSystemPrompt(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return new PromptTemplateResult(
                SystemPromptDefaults.Template, true, "[LLMTC][prompt] systemPromptPath 未配置；已使用内置默认模板");
        }

        var path = Path.Combine(_modRootDirectory, relativePath);
        try
        {
            if (!File.Exists(path))
            {
                return new PromptTemplateResult(
                    SystemPromptDefaults.Template, true, $"[LLMTC][prompt] 未找到 system prompt 模板：{relativePath}；已使用内置默认模板");
            }

            var text = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(text))
            {
                return new PromptTemplateResult(
                    SystemPromptDefaults.Template, true, $"[LLMTC][prompt] system prompt 模板为空：{relativePath}；已使用内置默认模板");
            }

            return new PromptTemplateResult(text, false, null);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return new PromptTemplateResult(
                SystemPromptDefaults.Template, true, $"[LLMTC][prompt] 读取模板失败：{relativePath}（{ex.Message}）；已使用内置默认模板");
        }
    }
}
