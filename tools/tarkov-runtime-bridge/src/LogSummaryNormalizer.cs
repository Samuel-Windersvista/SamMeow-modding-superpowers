using System.Text.RegularExpressions;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 日志文本归一化（纯逻辑，可独立单测）：剥离易变 token 得到聚合键，
/// 使「同一错误的数千个实例」塌缩为一组。
///
/// 保守策略（宁可少合并，不可误合并——误合并会把不同错误藏进同一组）：
/// 1. 只替换**形状明确**的 token：GUID、恰好 24 位 hex（EFT tpl / MongoId 形态）、
///    <c>0x…</c> 十六进制字面量（文件 / IL 偏移）、十进制数字；
/// 2. 24-hex 的右边界是「其后不得再有 hex 字符」而非词边界——真实告警样本会把 24hex id
///    紧贴字面量 <c>s</c>（<c>…3c2bs undefined StackObjectsCount value…</c>），
///    词边界在该位置不成立会漏配、同一错误的不同实例无法合并；左边界仍要求非词字符，
///    故不会吞掉 25+ 位 hex 串或内嵌于更长标识符的片段；
/// 3. 不做大小写折叠（<c>Error</c> 与 <c>error</c> 保持不同键）；
/// 4. 不做词干化 / 模糊匹配，仅把连续空白折叠为单空格并去首尾空白
///    （日志前缀的空格差异属噪声，折叠不引入跨错误合并）。
///
/// 与 TS 侧 <c>tools/tarkov-runtime-mcp/src/logs/log-normalizer.ts</c> **同构**：
/// 两侧的 5 条规则、边界语义、占位符与替换顺序必须一致（该文件亦已注明互为移植）。
///
/// 占位符：<c>&lt;guid&gt;</c> / <c>&lt;id&gt;</c> / <c>&lt;hex&gt;</c> / <c>&lt;n&gt;</c>。
/// 替换顺序固定（GUID → 24-hex → 0x-hex → 数字），保证幂等。
/// </summary>
internal static class LogSummaryNormalizer
{
    private static readonly Regex GuidPattern = new Regex(
        @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 恰好 24 位连续 hex 段：左边界非词字符（等价 <c>\b</c> 左侧），
    /// 右边界其后不得再有 hex 字符（镜像 TS 侧 <c>(?&lt;!\w)[0-9a-fA-F]{24}(?![0-9a-fA-F])</c>）。
    /// </summary>
    private static readonly Regex HexIdPattern = new Regex(
        @"(?<!\w)[0-9a-fA-F]{24}(?![0-9a-fA-F])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex HexLiteralPattern = new Regex(
        @"\b0[xX][0-9a-fA-F]+\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex NumberPattern = new Regex(
        @"\b\d+(?:\.\d+)?\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex WhitespacePattern = new Regex(
        @"\s+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// 文本 → 聚合键。null / 空 / 纯空白 → <c>""</c>。
    /// 幂等：对已归一化文本再次调用结果不变。
    /// </summary>
    internal static string Normalize(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var normalized = GuidPattern.Replace(text, "<guid>");
        normalized = HexIdPattern.Replace(normalized, "<id>");
        normalized = HexLiteralPattern.Replace(normalized, "<hex>");
        normalized = NumberPattern.Replace(normalized, "<n>");
        normalized = WhitespacePattern.Replace(normalized, " ");
        return normalized.Trim();
    }
}
