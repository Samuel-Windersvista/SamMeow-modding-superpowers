using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;

namespace _14AfterDBLoadHook;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.afterdbhook";
    public override string Name { get; init; } = "AfterDBLoadHookExample";
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

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class AfterDBLoadHook(
    DatabaseServer databaseServer,
    ISptLogger<AfterDBLoadHook> logger) : IOnLoad
{
    private Dictionary<MongoId, TemplateItem>? _itemsDb;

    public Task OnLoad()
    {
        _itemsDb = databaseServer.GetTables().Templates.Items;

        // Database will be loaded, this is the fresh state of the DB so NOTHING from the SPT
        // logic has modified anything yet. This is the DB loaded straight from the JSON files
        logger.LogWithColor($"Database item size: {_itemsDb.Count}", LogTextColor.Red, LogBackgroundColor.Yellow);

        // lets do a quick modification and see how this looks later on
        // find the nvgs item by its Id
        // this also checks if the item exists before giving you the item
        // if it doesn't, this if check will fail
        if (_itemsDb.TryGetValue(ItemTpl.NIGHTVISION_L3HARRIS_GPNVG18_NIGHT_VISION_GOGGLES, out var nvgs))
        {
            // Lets log the state before the modification
            logger.LogWithColor($"NVGs default CanSellOnRagfair: {nvgs.Properties.CanSellOnRagfair}", LogTextColor.Red,
                LogBackgroundColor.Yellow);

            // Update one of its properties to be true
            nvgs.Properties.CanSellOnRagfair = true;
        }

        return Task.CompletedTask;
    }
}

[Injectable(TypePriority = OnLoadOrder.PostSptModLoader + 1)]
public class AfterSptLoadHook(
    DatabaseServer databaseServer,
    ISptLogger<AfterDBLoadHook> logger) : IOnLoad
{

    private Dictionary<MongoId, TemplateItem>? _itemsDb;

    public Task OnLoad()
    {
        _itemsDb = databaseServer.GetTables().Templates.Items;

        // The modification we made above would have been processed by now by SPT, so any values we changed had
        // already been passed through the initial lifecycles (OnLoad) of SPT.

        if (_itemsDb.TryGetValue(ItemTpl.NIGHTVISION_L3HARRIS_GPNVG18_NIGHT_VISION_GOGGLES, out var nvgs))
        {
            // Lets log the state after the modification
            logger.LogWithColor($"NVGs default CanSellOnRagfair: {nvgs.Properties.CanSellOnRagfair}",
                LogTextColor.Red, LogBackgroundColor.Yellow);
        }

        return Task.CompletedTask;
    }
}

