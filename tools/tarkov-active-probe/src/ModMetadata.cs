using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace TarkovActiveProbe;

/// <summary>
/// tarkov-runtime-MCP 活跃探针的 mod 元数据（ADR-0004）。
/// </summary>
public record TarkovActiveProbeMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.sammeow.tarkov-active-probe";
    public string Name { get; init; } = "TarkovActiveProbe";
    public string Author { get; init; } = "SamMeow";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("0.1.0");
    public Range SptVersion { get; init; } = new("~5.0.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}
