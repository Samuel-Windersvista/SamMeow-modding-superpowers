using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace InescapableTarkovsSoftcore;

/// <summary>
/// Mod metadata compiled into the assembly. SPT discovers exactly one
/// <see cref="IModMetadata"/> implementation per server mod; this is it.
/// </summary>
public sealed class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = ModIdentity.Guid;

    public string Name { get; init; } = ModIdentity.Name;

    public string Author { get; init; } = "SamMeow";

    public List<string>? Contributors { get; init; }

    public Version Version { get; init; } = new(ModIdentity.Version);

    public Range SptVersion { get; init; } = new("~4.1.6");

    public bool HasPrepatcher { get; init; } = false;

    public List<string>? Incompatibilities { get; init; }

    public Dictionary<string, Range>? ModDependencies { get; init; }

    public string? Url { get; init; }

    public string License { get; init; } = "MIT";
}
