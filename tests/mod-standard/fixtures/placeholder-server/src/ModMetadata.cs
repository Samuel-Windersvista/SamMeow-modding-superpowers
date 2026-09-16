using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace PlaceholderServer;

public record PlaceholderServerMetadata : IModMetadata
{
    // 脚手架占位符未替换：META-003 应报 SKIP
    public string ModGuid { get; init; } = "{{MOD_GUID}}";

    public string Name { get; init; } = "Placeholder Server Fixture";

    public string Author { get; init; } = "C8";

    public Version Version { get; init; } = new("{{MOD_VERSION}}");

    public Range SptVersion { get; init; } = new("~4.1.0");

    public string License { get; init; } = "MIT";
}
