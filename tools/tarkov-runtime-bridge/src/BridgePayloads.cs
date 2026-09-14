using System;
using System.Globalization;
using System.Text;

namespace SamMeow.TarkovRuntimeBridge;

/// <summary>
/// 桥响应负载构造（纯逻辑，可独立单测）：字段顺序稳定、数值 InvariantCulture、
/// NaN/Infinity 退化为 <c>0</c>、字符串按 JSON 规则转义。
/// </summary>
internal static class BridgePayloads
{
    internal const string NotFoundBody = "{\"error\":\"not_found\"}";
    internal const string MethodNotAllowedBody = "{\"error\":\"method_not_allowed\"}";
    internal const string InternalErrorBody = "{\"error\":\"internal_error\"}";
    internal const string NotInRaidBody = "{\"inRaid\":false}";

    /// <summary>
    /// /bridge/info：插件版本 / 协议版本 / 能力清单 / 采样配置 / 网络配置。
    /// 参数化（不直接读 <see cref="Plugin"/>）以便独立单测。
    /// </summary>
    internal static string BuildInfo(string pluginVersion, int protocolVersion, int sampleIntervalMs, int port)
    {
        var ci = CultureInfo.InvariantCulture;
        return "{\"pluginVersion\":" + FormatString(pluginVersion)
            + ",\"protocolVersion\":" + protocolVersion.ToString(ci)
            + ",\"capabilities\":{\"endpoints\":[\"/bridge/info\",\"/raid/player\",\"/raid/status\",\"/raid/bots\"]"
            + ",\"sections\":[\"player\",\"raid\",\"bots\"]}"
            + ",\"sampling\":{\"intervalMs\":" + sampleIntervalMs.ToString(ci) + "}"
            + ",\"network\":{\"host\":\"127.0.0.1\",\"port\":" + port.ToString(ci) + "}}";
    }

    /// <summary>/raid/player 在 raid 负载。</summary>
    internal static string BuildPlayer(RaidState state, long nowMs)
    {
        var player = state.Player;
        var position = player.Position;
        var rotation = player.Rotation;
        var health = player.Health;

        return "{\"inRaid\":true,\"position\":{\"x\":" + FormatFloat(position.X)
            + ",\"y\":" + FormatFloat(position.Y)
            + ",\"z\":" + FormatFloat(position.Z)
            + "},\"rotation\":{\"x\":" + FormatFloat(rotation.X)
            + ",\"y\":" + FormatFloat(rotation.Y)
            + "},\"pose\":" + FormatString(player.Pose)
            + ",\"health\":{\"alive\":" + FormatBool(health.Alive)
            + ",\"total\":" + FormatFloat(health.Total)
            + ",\"parts\":{\"Head\":" + FormatFloat(health.Head)
            + ",\"Chest\":" + FormatFloat(health.Chest)
            + ",\"Stomach\":" + FormatFloat(health.Stomach)
            + ",\"LeftArm\":" + FormatFloat(health.LeftArm)
            + ",\"RightArm\":" + FormatFloat(health.RightArm)
            + ",\"LeftLeg\":" + FormatFloat(health.LeftLeg)
            + ",\"RightLeg\":" + FormatFloat(health.RightLeg)
            + "}},\"sampleAgeMs\":" + AgeMs(state, nowMs) + "}";
    }

    /// <summary>/raid/status 在 raid 负载。</summary>
    internal static string BuildStatus(RaidState state, long nowMs)
    {
        var raid = state.Raid;
        return "{\"inRaid\":true,\"map\":" + FormatString(raid.Map)
            + ",\"status\":" + FormatString(raid.Status)
            + ",\"remainingSeconds\":" + FormatFloat(raid.RemainingSeconds)
            + ",\"raidId\":" + FormatString(raid.RaidId)
            + ",\"sampleAgeMs\":" + AgeMs(state, nowMs) + "}";
    }

    /// <summary>/raid/bots 在 raid 负载；<paramref name="detail"/> 为真时附 bot 明细与 truncated。</summary>
    internal static string BuildBots(RaidState state, bool detail, long nowMs)
    {
        var bots = state.Bots;
        var builder = new StringBuilder(256);
        builder.Append("{\"inRaid\":true,\"total\":").Append(bots.Total)
            .Append(",\"alive\":").Append(bots.Alive)
            .Append(",\"byCategory\":{\"pmc\":").Append(bots.Pmc)
            .Append(",\"scav\":").Append(bots.Scav)
            .Append(",\"boss\":").Append(bots.Boss)
            .Append(",\"other\":").Append(bots.Other)
            .Append("},\"spawner\":{\"aliveAndLoading\":").Append(bots.SpawnerAliveAndLoading)
            .Append(",\"delayed\":").Append(bots.SpawnerDelayed)
            .Append(",\"allWithDelayed\":").Append(bots.SpawnerAllWithDelayed)
            .Append('}');

        if (detail)
        {
            var details = state.BotDetails;
            builder.Append(",\"bots\":[");
            for (var i = 0; i < details.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var entry = details[i];
                builder.Append("{\"x\":").Append(FormatFloat(entry.Position.X))
                    .Append(",\"y\":").Append(FormatFloat(entry.Position.Y))
                    .Append(",\"z\":").Append(FormatFloat(entry.Position.Z))
                    .Append(",\"role\":").Append(FormatString(entry.Role))
                    .Append(",\"side\":").Append(FormatString(entry.Side))
                    .Append(",\"alive\":").Append(FormatBool(entry.Alive))
                    .Append('}');
            }

            builder.Append("],\"truncated\":").Append(FormatBool(bots.Total > details.Length));
        }

        builder.Append(",\"sampleAgeMs\":").Append(AgeMs(state, nowMs)).Append('}');
        return builder.ToString();
    }

    /// <summary>快照年龄（毫秒）；时钟回拨导致的负值钳制为 0。</summary>
    internal static long AgeMs(RaidState state, long nowMs)
    {
        var ageMs = nowMs - state.SampledAtMs;
        return ageMs < 0 ? 0 : ageMs;
    }

    internal static string FormatBool(bool value)
    {
        return value ? "true" : "false";
    }

    /// <summary>
    /// float → JSON 数字字面量。NaN / ±Infinity 无 JSON 字面量，退化为 <c>0</c>；
    /// 其余用 "R" 往返格式 + InvariantCulture（小数点恒为 "."）。
    /// </summary>
    internal static string FormatFloat(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return "0";
        }

        return value.ToString("R", CultureInfo.InvariantCulture);
    }

    /// <summary>string → JSON 字符串字面量（含引号）；null / 空串为 <c>""</c>；控制字符转义。</summary>
    internal static string FormatString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        var builder = new StringBuilder(value.Length + 2);
        builder.Append('"');
        foreach (var character in value)
        {
            switch (character)
            {
                case '"':
                    builder.Append("\\\"");
                    break;
                case '\\':
                    builder.Append("\\\\");
                    break;
                case '\n':
                    builder.Append("\\n");
                    break;
                case '\r':
                    builder.Append("\\r");
                    break;
                case '\t':
                    builder.Append("\\t");
                    break;
                default:
                    if (character < 0x20)
                    {
                        builder.Append("\\u")
                            .Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        builder.Append('"');
        return builder.ToString();
    }
}
