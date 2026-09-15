using System;
using System.Collections.Generic;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// bot 分类表（纯逻辑，单点常量）。优先级：boss &gt; pmc &gt; scav &gt; other。
/// boss 判定 = role（忽略大小写）含 <c>boss</c> 或命中显式表（SPT 的 <c>sectantPriest</c>
/// 等不含 "boss" 字样的 boss 族）。
/// </summary>
internal static class BotClassifier
{
    internal const string Boss = "boss";
    internal const string Pmc = "pmc";
    internal const string Scav = "scav";
    internal const string Other = "other";

    /// <summary><c>EPlayerSide.Savage</c> 的稳定字符串形式。</summary>
    internal const string SavageSide = "Savage";

    /// <summary>
    /// 显式 boss 族（role 字面量，忽略大小写）。用于不含 "boss" 字样的 boss 角色：
    /// <c>WildSpawnType.sectantPriest</c>（邪教徒祭司）。
    /// </summary>
    private static readonly HashSet<string> ExplicitBossRoles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sectantpriest",
        };

    internal static bool IsBoss(string role)
    {
        if (string.IsNullOrEmpty(role))
        {
            return false;
        }

        if (role.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return ExplicitBossRoles.Contains(role);
    }

    /// <summary>
    /// 分类：role 优先于 side（SPT 的 pmcUSEC/pmcBEAR 阵营为 Savage，
    /// 仅凭 side 会把 PMC 误判为 scav）。
    /// </summary>
    internal static string Classify(string role, string side)
    {
        if (IsBoss(role))
        {
            return Boss;
        }

        if (!string.IsNullOrEmpty(role) && role.StartsWith("pmc", StringComparison.OrdinalIgnoreCase))
        {
            return Pmc;
        }

        if (string.Equals(side, SavageSide, StringComparison.Ordinal))
        {
            return Scav;
        }

        return Other;
    }
}
