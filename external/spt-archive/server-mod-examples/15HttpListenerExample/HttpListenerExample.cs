using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Servers.Http;

namespace _15HttpListenerExample;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.sp-tarkov.examples.httplistener";
    public override string Name { get; init; } = "HttpListenerExample";
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

[Injectable(TypePriority = 0)]
public class HttpListenerExample : IHttpListener
{
    public bool CanHandle(MongoId sessionId, HttpContext context)
    {
        return context.Request.Method == "GET" && context.Request.Path.Value!.Contains("/type-custom-url");
    }

    public async Task Handle(MongoId sessionId, HttpContext context)
    {
        context.Response.StatusCode = 200;
        await context.Response.Body.WriteAsync("[1] This is the first example of a mod hooking into the HttpServer"u8.ToArray());
        await context.Response.StartAsync();
        await context.Response.CompleteAsync();
    }
}
