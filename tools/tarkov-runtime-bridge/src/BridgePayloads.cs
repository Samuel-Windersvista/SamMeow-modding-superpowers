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
            + ",\"capabilities\":{\"endpoints\":[\"/bridge/info\",\"/raid/player\",\"/raid/status\",\"/raid/bots\",\"/raid/events\"]"
            + ",\"sections\":[\"player\",\"raid\",\"bots\",\"events\"]}"
            + ",\"sampling\":{\"intervalMs\":" + sampleIntervalMs.ToString(ci) + "}"
            + ",\"network\":{\"host\":\"127.0.0.1\",\"port\":" + port.ToString(ci) + "}}";
    }

    /// <summary>/raid/player 在 raid 负载（含当前武器与已装备槽摘要）。</summary>
    internal static string BuildPlayer(RaidState state, long nowMs)
    {
        var player = state.Player;
        var position = player.Position;
        var rotation = player.Rotation;
        var health = player.Health;

        var builder = new StringBuilder(320);
        builder.Append("{\"inRaid\":true,\"position\":{\"x\":").Append(FormatFloat(position.X))
            .Append(",\"y\":").Append(FormatFloat(position.Y))
            .Append(",\"z\":").Append(FormatFloat(position.Z))
            .Append("},\"rotation\":{\"x\":").Append(FormatFloat(rotation.X))
            .Append(",\"y\":").Append(FormatFloat(rotation.Y))
            .Append("},\"pose\":").Append(FormatString(player.Pose))
            .Append(",\"health\":{\"alive\":").Append(FormatBool(health.Alive))
            .Append(",\"total\":").Append(FormatFloat(health.Total))
            .Append(",\"parts\":{\"Head\":").Append(FormatFloat(health.Head))
            .Append(",\"Chest\":").Append(FormatFloat(health.Chest))
            .Append(",\"Stomach\":").Append(FormatFloat(health.Stomach))
            .Append(",\"LeftArm\":").Append(FormatFloat(health.LeftArm))
            .Append(",\"RightArm\":").Append(FormatFloat(health.RightArm))
            .Append(",\"LeftLeg\":").Append(FormatFloat(health.LeftLeg))
            .Append(",\"RightLeg\":").Append(FormatFloat(health.RightLeg))
            .Append("}},\"weapon\":");
        AppendWeapon(builder, player.Weapon);
        builder.Append(",\"equipment\":");
        AppendEquipment(builder, player.Equipment);
        builder.Append(",\"sampleAgeMs\":").Append(AgeMs(state, nowMs)).Append('}');
        return builder.ToString();
    }

    /// <summary>
    /// /raid/events 负载：<c>{inRaid, seq, dropped, events:[...]}</c>。
    /// 缓冲跨 raid 保留，故 <paramref name="inRaid"/> 为假时仍可能返回历史事件。
    /// </summary>
    internal static string BuildEvents(RaidEventBuffer buffer, long since, int limit, bool inRaid)
    {
        var page = buffer.Snapshot(since, limit);
        var ci = CultureInfo.InvariantCulture;
        var builder = new StringBuilder(256);
        builder.Append("{\"inRaid\":").Append(FormatBool(inRaid))
            .Append(",\"seq\":").Append(page.Seq.ToString(ci))
            .Append(",\"dropped\":").Append(page.Dropped.ToString(ci))
            .Append(",\"events\":[");

        for (var i = 0; i < page.Events.Length; i++)
        {
            if (i > 0)
            {
                builder.Append(',');
            }

            AppendEvent(builder, page.Events[i]);
        }

        builder.Append("]}");
        return builder.ToString();
    }

    /// <summary>武器 → JSON；null 输出 JSON <c>null</c>。</summary>
    internal static void AppendWeapon(StringBuilder builder, WeaponSnapshot weapon)
    {
        if (weapon == null)
        {
            builder.Append("null");
            return;
        }

        var ci = CultureInfo.InvariantCulture;
        builder.Append("{\"tpl\":").Append(FormatString(weapon.TemplateId))
            .Append(",\"name\":").Append(FormatString(weapon.Name))
            .Append(",\"ammoInMag\":").Append(weapon.AmmoInMag.ToString(ci))
            .Append(",\"ammoInChamber\":").Append(weapon.AmmoInChamber.ToString(ci))
            .Append('}');
    }

    /// <summary>装备槽数组 → JSON 数组（空 / null 均为 <c>[]</c>）。</summary>
    internal static void AppendEquipment(StringBuilder builder, EquipmentEntry[] equipment)
    {
        builder.Append('[');
        if (equipment != null)
        {
            for (var i = 0; i < equipment.Length; i++)
            {
                if (i > 0)
                {
                    builder.Append(',');
                }

                var entry = equipment[i];
                builder.Append("{\"slot\":").Append(FormatString(entry.Slot))
                    .Append(",\"tpl\":").Append(FormatString(entry.TemplateId))
                    .Append(",\"name\":").Append(FormatString(entry.Name))
                    .Append('}');
            }
        }

        builder.Append(']');
    }

    /// <summary>UTC Ticks → ISO 8601（"o"，InvariantCulture）。</summary>
    internal static string FormatTimestamp(long utcTicks)
    {
        if (utcTicks <= 0)
        {
            return string.Empty;
        }

        return new DateTime(utcTicks, DateTimeKind.Utc).ToString("o", CultureInfo.InvariantCulture);
    }

    private static void AppendEvent(StringBuilder builder, RaidEvent raidEvent)
    {
        var ci = CultureInfo.InvariantCulture;
        builder.Append("{\"seq\":").Append(raidEvent.Seq.ToString(ci))
            .Append(",\"ts\":").Append(FormatString(FormatTimestamp(raidEvent.TimestampUtcTicks)))
            .Append(",\"type\":").Append(FormatString(RaidEvent.TypeName(raidEvent.Kind)))
            .Append(",\"raidId\":").Append(FormatString(raidEvent.RaidId))
            .Append(",\"payload\":");

        switch (raidEvent.Kind)
        {
            case RaidEventKind.Damage:
                AppendDamagePayload(builder, raidEvent.Damage);
                break;
            case RaidEventKind.Death:
                AppendDeathPayload(builder, raidEvent.Death);
                break;
            case RaidEventKind.Extraction:
                AppendExtractionPayload(builder, raidEvent.Extraction);
                break;
            default:
                // 未知 kind 不得伪装成 extraction：输出空 payload（type 记为 "unknown"）。
                builder.Append("{}");
                break;
        }

        builder.Append('}');
    }

    private static void AppendDamagePayload(StringBuilder builder, DamagePayload payload)
    {
        builder.Append("{\"victimProfileId\":").Append(FormatString(payload.VictimProfileId))
            .Append(",\"victimIsLocal\":").Append(FormatBool(payload.VictimIsLocal))
            .Append(",\"part\":").Append(FormatString(payload.Part))
            .Append(",\"amount\":").Append(FormatFloat(payload.Amount))
            .Append(",\"sourceType\":").Append(FormatString(payload.SourceType))
            .Append('}');
    }

    private static void AppendDeathPayload(StringBuilder builder, DeathPayload payload)
    {
        builder.Append("{\"victimProfileId\":").Append(FormatString(payload.VictimProfileId))
            .Append(",\"victimIsLocal\":").Append(FormatBool(payload.VictimIsLocal))
            .Append(",\"damageType\":").Append(FormatString(payload.DamageType))
            .Append(",\"killer\":");

        if (!payload.HasKiller)
        {
            builder.Append("null");
        }
        else
        {
            var killer = payload.Killer;
            builder.Append("{\"profileId\":").Append(FormatString(killer.ProfileId))
                .Append(",\"name\":").Append(FormatString(killer.Name))
                .Append(",\"side\":").Append(FormatString(killer.Side))
                .Append(",\"role\":").Append(FormatString(killer.Role))
                .Append(",\"isLocal\":").Append(FormatBool(killer.IsLocal))
                .Append('}');
        }

        builder.Append('}');
    }

    private static void AppendExtractionPayload(StringBuilder builder, ExtractionPayload payload)
    {
        builder.Append("{\"exitName\":").Append(FormatString(payload.ExitName))
            .Append(",\"status\":").Append(FormatString(payload.Status))
            .Append('}');
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
