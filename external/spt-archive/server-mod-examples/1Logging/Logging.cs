using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;

namespace _1Logging;

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
    /// <summary>
    /// Any string can be used for a modId, but it should ideally be unique and not easily duplicated
    /// a 'bad' ID would be: "mymod", "mod1", "questmod"
    /// It is recommended (but not mandatory) to use the reverse domain name notation,
    /// see: https://docs.oracle.com/javase/tutorial/java/package/namingpkgs.html
    /// </summary>
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.logging";

    /// <summary>
    /// The name of your mod
    /// </summary>
    public override string Name { get; init; } = "LoggingExample";

    /// <summary>
    /// Who created the mod (you!)
    /// </summary>
    public override string Author { get; init; } = "SPTarkov";

    /// <summary>
    /// A list of people who helped you create the mod
    /// </summary>
    public override List<string>? Contributors { get; init; }

    /// <summary>
    ///  The version of the mod, follows SEMVER rules (https://semver.org/)
    /// </summary>
    public override SemanticVersioning.Version Version { get; init; } = new("1.0.0");

    /// <summary>
    /// What version of SPT is your mod made for, follows SEMVER rules (https://semver.org/)
    /// </summary>
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");

    /// <summary>
    /// ModIds that you know cause problems with your mod
    /// </summary>
    public override List<string>? Incompatibilities { get; init; }

    /// <summary>
    /// ModIds your mod REQUIRES to function
    /// </summary>
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }

    /// <summary>
    /// Where to find your mod online
    /// </summary>
    public override string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";

    /// <summary>
    /// Does your mod load bundles? (e.g. new weapon/armor mods)
    /// </summary>
    public override bool? IsBundleMod { get; init; } = false;

    /// <summary>
    /// What Licence does your mod use
    /// </summary>
    public override string License { get; init; } = "MIT";
}

// We want to load after PreSptModLoader is complete, so we set our type priority to that, plus 1.
[Injectable(TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class Logging(
    ISptLogger<Logging> logger) // We inject a logger for use inside our class, it must have the class inside the diamond <> brackets
    : IOnLoad // Implement the IOnLoad interface so that this mod can do something on server load
{
    public Task OnLoad()
    {
        // We can access the logger and call its methods to log to the server window and the server log file
        logger.Success("This is a success message");
        logger.Warning("This is a warning message");
        logger.Error("This is an error message");
        logger.Info("This is an info message");
        logger.Critical("This is a critical message");

        // Logging with colors requires you to 'pass' the text color and background color
        logger.LogWithColor("This is a message with custom colors", LogTextColor.Red, LogBackgroundColor.Black);
        logger.Debug("This is a debug message that gets written to the log file, not the console");
        
        // Inform the server our mod has finished doing work
        return Task.CompletedTask;
    }
}
