using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Callbacks;

namespace pitTeam.Server.Routers.Static;

[Injectable(InjectionType.Transient, typePriority: 400000)]
public class FriendlyTeammateSocialRouter : StaticRouter
{
	public FriendlyTeammateSocialRouter(JsonUtil jsonUtil, FriendlyTeammateSocialCallbacks callbacks)
		: base(jsonUtil, new RouteAction[]
		{
			new RouteAction<EmptyRequestData>("/client/friend/list", async (string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.MergeFriendList(url, info, sessionId, output)),
			new RouteAction<EmptyRequestData>("/client/friend/request/list/inbox", async (string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.MergeFriendRequestInbox(url, info, sessionId, output)),
			new RouteAction<GetOtherProfileRequest>("/client/profile/view", async (string url, GetOtherProfileRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.MergeProfileView(url, info, sessionId, output)),
			new RouteAction<AcceptFriendRequestData>("/client/friend/request/accept", async (string url, AcceptFriendRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.AcceptFriendRequest(url, info, sessionId, output)),
			new RouteAction<EmptyRequestData>("/client/friend/request/accept-all", async (string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.AcceptAllFriendRequests(url, info, sessionId, output)),
			new RouteAction<DeclineFriendRequestData>("/client/friend/request/decline", async (string url, DeclineFriendRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.DeclineFriendRequest(url, info, sessionId, output)),
			new RouteAction<DeleteFriendRequest>("/client/friend/delete", async (string url, DeleteFriendRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.DeleteFriend(url, info, sessionId, output))
		})
	{
	}
}
