using System.Text.RegularExpressions;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 日志文本归一化（纯逻辑，可独立单测）：剥离易变 token 得到聚合键，
/// 使「同一错误的数千个实例」塌缩为一组。
///
/// C10 起规则集**不再硬编码**：从 <see cref="BridgeContract"/> 读取
/// <c>shared/bridge-contract/contract.json</c> 的 <c>logNormalization</c>，
/// 顺序应用替换 → 按 <c>whitespacePattern</c> 折叠空白 → 按 <c>trim</c> 去首尾（同一空白集合）。
/// 契约是两端（C# / TS）唯一规则源，改规则须改契约（见 ADR-0009）。
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
/// 规则、边界语义、占位符与替换顺序全部来自同一契约（该文件亦已注明互为移植）。
///
/// 占位符：<c>&lt;guid&gt;</c> / <c>&lt;id&gt;</c> / <c>&lt;hex&gt;</c> / <c>&lt;n&gt;</c>。
/// 替换顺序固定（GUID → 24-hex → 0x-hex → 数字），保证幂等。
/// </summary>
internal static class LogSummaryNormalizer
{
    /// <summary>
    /// 连续空白折叠模式：直接编译契约的 <c>whitespacePattern</c>（显式字符类，**不含 <c>\s</c>**）。
    /// 显式类使 .NET 与 JS 的空白集合逐码点一致——ECMAScript 模式下 .NET 的 <c>\s</c>
    /// 仅匹配 ASCII 空白，与 JS 的 25 码点集合不同，故不能依赖 <c>\s</c>。
    /// </summary>
    private static readonly Regex WhitespacePattern = new Regex(
        BridgeContract.WhitespacePattern,
        RegexOptions.Compiled | RegexOptions.ECMAScript);

    /// <summary>
    /// 首尾空白去除模式：同一契约空白集合，<c>^(?:ws)|(?:ws)$</c> → 空串。
    /// 不使用 <c>string.Trim()</c>——其 <c>char.IsWhiteSpace</c> 集合与契约集合不同
    /// （U+0085 / U+FEFF 两处会漂移，见 ADR-0009）。
    /// </summary>
    private static readonly Regex TrimPattern = new Regex(
        "^(?:" + BridgeContract.WhitespacePattern + ")|(?:" + BridgeContract.WhitespacePattern + ")$",
        RegexOptions.Compiled | RegexOptions.ECMAScript);

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

        var normalized = text;
        foreach (var rule in BridgeContract.NormalizationRules)
        {
            normalized = rule.Pattern.Replace(normalized, rule.Replacement);
        }

        normalized = WhitespacePattern.Replace(normalized, " ");

        return BridgeContract.Trim ? TrimPattern.Replace(normalized, string.Empty) : normalized;
    }
}
