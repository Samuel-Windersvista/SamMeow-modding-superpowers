using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Callbacks;
using pitTeam.Server.Models;

namespace pitTeam.Server.Routers.Static;

[Injectable(InjectionType.Transient, typePriority: 400000)]
public class FriendlyPostRaidRouter : StaticRouter
{
	public FriendlyPostRaidRouter(JsonUtil jsonUtil, FriendlyPostRaidCallbacks callbacks)
		: base(jsonUtil, new RouteAction[]
		{
			new RouteAction<FriendlyPostRaidReturnItemsRequest>("/singleplayer/returnitems", async (string url, FriendlyPostRaidReturnItemsRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.ReturnItems(url, info, sessionId)),
			new RouteAction<FriendlyPostRaidTeamEscapedRequest>("/singleplayer/teamescaped", async (string url, FriendlyPostRaidTeamEscapedRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.TeamEscaped(url, info, sessionId)),
			new RouteAction<FriendlyRecruitPickupRequest>("/singleplayer/pitfireteam/recruitpickup", async (string url, FriendlyRecruitPickupRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.RecruitPickup(url, info, sessionId)),
			new RouteAction<FriendlyPostRaidKillMessageRequest>("/singleplayer/pitfireteam/postraid/kill-message", async (string url, FriendlyPostRaidKillMessageRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.RecordKillMessage(url, info, sessionId)),
			new RouteAction<FriendlyPostRaidProtectedItemsRequest>("/singleplayer/pitfireteam/postraid/protected-items", async (string url, FriendlyPostRaidProtectedItemsRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.RegisterProtectedItems(url, info, sessionId)),
			new RouteAction<EndLocalRaidRequestData>("/client/match/local/end", async (string url, EndLocalRaidRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.EndLocalRaid(url, info, sessionId, output)),
			new RouteAction<FriendlyTeammateDeathEscapeRequest>("/singleplayer/pitfireteam/teammate/raid-outcomes", async (string url, FriendlyTeammateDeathEscapeRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.RaidOutcomes(url, info, sessionId)),
			new RouteAction<FriendlyTeammateDeathEscapeRequest>("/singleplayer/pitfireteam/teammate/death-escape", async (string url, FriendlyTeammateDeathEscapeRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.DeathEscape(url, info, sessionId))
		})
	{
	}
}
