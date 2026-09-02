using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;

namespace _8OnLoad;

/// <summary>
/// This is the replacement for the former package.json data. This is required for all mods.
///
/// This is where we define all the metadata associated with this mod.
/// You don't have to do anything with it, other than fill it out.
/// All properties must be overriden, properties you don't use may be left null.
/// It is read by the mod loader when this mod is loaded.
/// </summary>
public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.onload";
    public override string Name { get; init; } = "OnLoadExampleExample";
    public override string Author { get; init; } = "SPTarkov";
    public override List<string>? Contributors { get; init; }
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; }
    public override string License { get; init; } = "MIT";
}

// Check `OnLoadOrder` for list of possible choices
[Injectable(TypePriority = OnLoadOrder.PostDBModLoader)] // Can also give an int value for fine-grained control
public class OnLoadExample(
    ISptLogger<OnLoadExample> logger) : IOnLoad // Must implement the IOnLoad interface
{
    public Task OnLoad()
    {
        // Can do work here
        logger.Success($"Mod loaded after database!");

        return Task.CompletedTask;
    }
}
