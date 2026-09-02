using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using System.Reflection;

namespace _5ReadCustomJsonConfig;

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
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.readjsonconfig";
    public override string Name { get; init; } = "ReadJsonConfigExample";
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

// We want to load after PreSptModLoader is complete, so we set our type priority to that, plus 1.
[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class ReadJsonConfig(
        ISptLogger<ReadJsonConfig> logger,
        ModHelper modHelper) : IOnLoad // Implement the IOnLoad interface so that this mod can do something
{
    /// <summary>
    /// This is called when this class is loaded, the order in which its loaded is set according to the type priority
    /// on the [Injectable] attribute on this class. Each class can then be used as an entry point to do
    /// things at varying times according to type priority
    /// </summary>
    public Task OnLoad()
    {
        // This will get us the full path to the mod, e.g. C:\spt\user\mods\5ReadCustomJsonConfig-0.0.1
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        // We give the path to the mod folder and the file we want to get, giving us the config, supply the files 'type' between the diamond brackets
        var config = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "config.json");

        logger.Success($"Read property: 'ExampleProperty' from config with value: {config.ExampleProperty}");

        // Return a completed task
        return Task.CompletedTask;
    }
}

// This class should represent your config structure
public record ModConfig
{
    public required string ExampleProperty { get; set; }
}
