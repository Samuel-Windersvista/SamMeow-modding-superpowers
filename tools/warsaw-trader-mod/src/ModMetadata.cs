using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace SamMeow.WarsawTrader;

public record WarsawTraderModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.sammeow.warsawtrader";
    public string Name { get; init; } = "Warsaw Pact Trader";
    public string Author { get; init; } = "SamMeow";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("1.0.0");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}
