namespace SamMeow.LLMTraderChat.Server.Knowledge;

/// <summary>
/// 检索命中的知识片段：来源文件 + 标题 + 正文 + 得分。
/// <see cref="Source"/>（相对文档目录的文件名）与 <see cref="Title"/> 供 prompt 引用标注。
/// </summary>
public sealed record KnowledgeSnippet(string Source, string Title, string Text, int Score);
