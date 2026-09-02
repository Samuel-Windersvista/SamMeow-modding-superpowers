using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace _10CustomRoute;

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
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.customroute";
    public override string Name { get; init; } = "CustomStaticRouterExample";
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

/// <summary>
///  This class registers a new static router in SPT, you can register as many routes as you want here
/// </summary>
[Injectable]
public class CustomStaticRouter(JsonUtil jsonUtil, CustomStaticRouterCallback customStaticRouterCallback) : StaticRouter(jsonUtil, [
            new RouteAction<ExampleStaticRequestData>(
                "/example/route/static",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) => await customStaticRouterCallback.HandleExampleStaticRoute(url, info, sessionId)
            ),
            // There are cases where you dont want to send data to the server, in that case you can ignore ExampleStaticRequestData and use EmptyRequestData
            new RouteAction<EmptyRequestData>(
                "/example/route/emptystatic",
                async (
                    url,
                    info,
                    sessionId,
                    output
                ) => await customStaticRouterCallback.HandleEmptyExampleStaticRoute(url, info, sessionId)
            )
        ])
{ }

/// <summary>
/// This class handles callbacks that are sent to your route, you can run code both synchronously here as well as asynchronously
/// </summary>
[Injectable]
public class CustomStaticRouterCallback(ISptLogger<CustomStaticRouterCallback> logger, HttpResponseUtil httpResponseUtil)
{
    public ValueTask<string> HandleEmptyExampleStaticRoute(string url, EmptyRequestData info, MongoId sessionId)
    {
        // Your mods code goes here
        logger.Info($"Callback on {url} route received!");
        return new ValueTask<string>(httpResponseUtil.NullResponse());
    }

    public ValueTask<string> HandleExampleStaticRoute(string url, ExampleStaticRequestData info, MongoId sessionId)
    {
        // Your mods code goes here
        logger.Info($"Callback on {url} route received!");
        return new ValueTask<string>(httpResponseUtil.NullResponse());
    }
}

/// <summary>
/// This record represents your incoming data model, any data you are sending to the server you will need to have in here.
/// </summary>
public record ExampleStaticRequestData : IRequestData
{
}