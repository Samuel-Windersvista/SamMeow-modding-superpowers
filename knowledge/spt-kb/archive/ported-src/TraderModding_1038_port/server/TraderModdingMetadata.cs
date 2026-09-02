using SPTarkov.Server.Core.Models.Spt.Mod;

namespace TraderModding;

public record TraderModdingMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.choochoo.tradermodding";
    public string Name { get; init; } = "TraderModding";
    public string Author { get; init; } = "Choo²";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("2.1.2");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.X");


    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string? License { get; init; } = "MIT";
    public bool HasPrepatcher { get; init; }
}
