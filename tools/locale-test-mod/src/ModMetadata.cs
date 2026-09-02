using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace LocaleTest;

public record LocaleTestMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.sammeow.localetest";
    public string Name { get; init; } = "LocaleTest";
    public string Author { get; init; } = "SamMeow";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("0.1.0");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}
