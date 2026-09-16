using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace WaiverMod;

public record WaiverModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.example.waiver-mod";

    public string Name { get; init; } = "Waiver Mod Fixture";

    public string Author { get; init; } = "C8";

    public Version Version { get; init; } = new("1.0.0");

    public Range SptVersion { get; init; } = new("~4.1.0");

    public string License { get; init; } = "MIT";
}
