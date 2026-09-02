using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Callbacks;
using pitTeam.Server.Models;

namespace pitTeam.Server.Routers.Static;

[Injectable(InjectionType.Transient, typePriority: 400000)]
public class FriendlyTeammateMatchRouter : StaticRouter
{
	public FriendlyTeammateMatchRouter(JsonUtil jsonUtil, FriendlyTeammateMatchCallbacks callbacks)
		: base(jsonUtil, new RouteAction[]
		{
			new RouteAction<MatchGroupInviteSendRequest>("/client/match/group/invite/send", async (string url, MatchGroupInviteSendRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.SendGroupInvite(url, info, sessionId, output)),
			new RouteAction<EmptyRequestData>("/client/game/bot/followerdetails", async (string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.GetFollowerDetails(url, info, sessionId)),
			new RouteAction<FriendlyTeammateFollowerProgressBatchRequest>("/client/game/bot/followerprogress", async (string url, FriendlyTeammateFollowerProgressBatchRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.PersistFollowerProgress(url, info, sessionId)),
			new RouteAction<FriendlyTeammateFollowerGenerateRequest>("/client/game/bot/followergenerate", async (string url, FriendlyTeammateFollowerGenerateRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.GenerateFollowerProfile(url, info, sessionId))
		})
	{
	}
}
