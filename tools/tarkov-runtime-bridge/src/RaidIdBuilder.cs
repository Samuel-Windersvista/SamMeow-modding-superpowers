using System;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// raidId 组装（纯逻辑，可独立单测）：
/// <c>&lt;profileId&gt;@&lt;桥自持会话起点 UTC ISO 8601&gt;</c>。
/// profileId 回退链：<c>MainPlayer.ProfileId</c> → <c>GameWorld.CurrentProfileId</c> → <c>unknown-profile</c>；
/// 会话起点未记录（ticks &lt;= 0）时时间戳段记 <c>no-start</c>。
/// </summary>
internal static class RaidIdBuilder
{
    /// <summary>桥尚未记录 raid 会话起点时，raidId 时间戳段的固定标记。</summary>
    internal const string NoStartMarker = "no-start";

    /// <summary>profileId 全部来源缺失时的保底标记（保证 raidId 的 profileId 段非空）。</summary>
    internal const string UnknownProfileMarker = "unknown-profile";

    /// <summary>
    /// profileId 回退链：首选 MainPlayer.ProfileId，其次 CurrentProfileId 字符串形式，
    /// 均缺失时空串 / null 统一退化为 <see cref="UnknownProfileMarker"/>。
    /// </summary>
    internal static string ResolveProfileId(string mainPlayerProfileId, string currentProfileId)
    {
        if (!string.IsNullOrEmpty(mainPlayerProfileId))
        {
            return mainPlayerProfileId;
        }

        if (!string.IsNullOrEmpty(currentProfileId))
        {
            return currentProfileId;
        }

        return UnknownProfileMarker;
    }

    /// <summary>会话起点格式化：&lt;= 0 记 <see cref="NoStartMarker"/>，否则 UTC ISO 8601（"o"）。</summary>
    internal static string FormatSessionStart(long sessionStartUtcTicks)
    {
        return sessionStartUtcTicks > 0
            ? new DateTime(sessionStartUtcTicks, DateTimeKind.Utc).ToString("o")
            : NoStartMarker;
    }

    /// <summary>组装 raidId：<c>&lt;profileId&gt;@&lt;会话起点&gt;</c>。</summary>
    internal static string Build(string profileId, long sessionStartUtcTicks)
    {
        return profileId + "@" + FormatSessionStart(sessionStartUtcTicks);
    }
}
