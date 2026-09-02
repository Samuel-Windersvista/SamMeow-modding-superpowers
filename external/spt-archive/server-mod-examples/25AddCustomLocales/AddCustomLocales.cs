using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;

namespace _25AddCustomLocales;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.customlocales";
    public override string Name { get; init; } = "AddCustomLocalesExample";
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

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 1)]
public class AddCustomLocales(
    ISptLogger<AddCustomLocales> logger,
    DatabaseService databaseService,
    LocaleService localeService,
    ServerLocalisationService serverLocalisationService)
    : IOnLoad
{
    // Constructor - Inject a 'ISptLogger' with your mods Class inside the diamond brackets
    // Save the logger we're injecting into a private variable that is scoped to this class (only this class has access to it)
    // save the locale service into a private variable that is scoped to this class (only this class has access to it)

    public Task OnLoad()
    {
        // Add a custom locale to the en game locales
       if (databaseService.GetLocales().Global.TryGetValue("en", out var lazyloadedValue))
        {
            // We have to add a transformer here, because locales are lazy loaded due to them taking up huge space in memory
            // The transformer will make sure that each time the locales are requested, the ones changed or added below are included
            lazyloadedValue.AddTransformer(lazyloadedLocaleData =>
            {
                lazyloadedLocaleData["Attention! This is a Beta version of Escape from Tarkov for testing purposes."] = "Testing change of beta version warning";
                lazyloadedLocaleData.Add("TestingLocales", "Testing Locales");

                return lazyloadedLocaleData;
            });

            logger.Success("Added a custom locale to the database");
        }

        var _locales = localeService.GetLocaleDb("en");
        // Log this so we can see it in the console
        logger.Info(_locales["TestingLocales"]);

        // Log by the locale key and output the language the player has set
        // If the locale isn't found, it tries english
        // If english isn't found, it shows the key
        logger.Info(serverLocalisationService.GetText("TestingLocales"));

        logger.Info(_locales["Attention! This is a Beta version of Escape from Tarkov for testing purposes."]);
        return Task.CompletedTask;
    }
}
