using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;

namespace _9OnUpdate;

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
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.onupdate";
    public override string Name { get; init; } = "OnUpdateExample";
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

// Check `OnUpdateOrder` for list of possible choices
[Injectable(TypePriority = OnUpdateOrder.InsuranceCallbacks)] // Can also give it an int value for more fine-grained control
public class OnUpdateExample(
    ISptLogger<OnUpdateExample> logger) : IOnUpdate // Must implement the IOnUpdate interface
{
    public Task<bool> OnUpdate(long timeSinceLastRun)
    {
        // Can do work here
        logger.Success($"Mod running update after insurance callbacks have run!");

        return Task.FromResult(true); // Return true for a success, false for failure
    }
}
