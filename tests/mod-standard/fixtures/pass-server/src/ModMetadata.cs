using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace PassServer;

// STD-META-001 / STD-META-002：每个 mod 目录恰好一个 IModMetadata 实现，放在 ModMetadata.cs
public record PassServerMetadata : IModMetadata
{
    // STD-META-003：反向域名式全局唯一 ModGuid
    public string ModGuid { get; init; } = "com.example.pass-server";

    public string Name { get; init; } = "Pass Server Fixture";

    public string Author { get; init; } = "C8";

    // STD-META-005：三段式 semver
    public Version Version { get; init; } = new("1.0.0");

    // STD-META-004：tilde 区间
    public Range SptVersion { get; init; } = new("~4.1.0");

    public string License { get; init; } = "MIT";
}
