using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace FailServer;

public record FailServerMetadata : IModMetadata
{
    // GUID 形状正确，保证 META-003 仍 PASS（FAIL 集合必须逐条可预期）
    public string ModGuid { get; init; } = "com.example.fail-server";

    public string Name { get; init; } = "Fail Server Fixture";

    public string Author { get; init; } = "C8";

    public Version Version { get; init; } = new("1.0");

    public Range SptVersion { get; init; } = new("~4.1.0");

    public string License { get; init; } = "MIT";
}
