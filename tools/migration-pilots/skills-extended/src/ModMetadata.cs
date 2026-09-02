using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace SkillsExtended;

/// <summary>SkillsExtended 迁移版元数据（3.11 -> 4.1）。</summary>
public record SkillsExtendedMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.dirtbikercj.skillsextended";
    public string Name { get; init; } = "Skills Extended";
    public string Author { get; init; } = "dirtbikercj";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("1.6.0");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "CC BY-NC-ND 4.0";
}
