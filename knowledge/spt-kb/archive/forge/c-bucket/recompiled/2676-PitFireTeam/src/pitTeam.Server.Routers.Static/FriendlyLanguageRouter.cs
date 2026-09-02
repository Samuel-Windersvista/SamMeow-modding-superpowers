using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Callbacks;
using pitTeam.Server.Models;

namespace pitTeam.Server.Routers.Static;

[Injectable(InjectionType.Transient, typePriority: 400000)]
public class FriendlyLanguageRouter : StaticRouter
{
	public FriendlyLanguageRouter(JsonUtil jsonUtil, FriendlyLanguageCallbacks callbacks)
		: base(jsonUtil, new RouteAction[]
		{
			new RouteAction<FriendlyLanguageRequest>("/singleplayer/pitfireteam/lang", async (string url, FriendlyLanguageRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.Get(url, info, sessionId)),
			new RouteAction<FriendlyLanguageRequest>("/singleplayer/pitlang", async (string url, FriendlyLanguageRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.Get(url, info, sessionId))
		})
	{
	}
}
