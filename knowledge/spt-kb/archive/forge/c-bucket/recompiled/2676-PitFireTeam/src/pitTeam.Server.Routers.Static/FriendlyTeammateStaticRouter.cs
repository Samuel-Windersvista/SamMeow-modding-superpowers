using System.Threading;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Callbacks;
using pitTeam.Server.Models;

namespace pitTeam.Server.Routers.Static;

[Injectable(InjectionType.Transient, typePriority: 400000)]
public class FriendlyTeammateStaticRouter : StaticRouter
{
	public FriendlyTeammateStaticRouter(JsonUtil jsonUtil, FriendlyTeammateCallbacks callbacks)
		: base(jsonUtil, new RouteAction[]
		{
			new RouteAction<FriendlyTeammateCreateRequest>("/singleplayer/pitfireteam/teammate/create", async (string url, FriendlyTeammateCreateRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.Create(url, info, sessionId)),
			new RouteAction<EmptyRequestData>("/singleplayer/pitfireteam/teammates", async (string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.List(url, info, sessionId)),
			new RouteAction<EmptyRequestData>("/singleplayer/autoteam", async (string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.ListAutoJoin(url, info, sessionId)),
			new RouteAction<FriendlyServerSettingsRequest>("/singleplayer/pitfireteam/settings", async (string url, FriendlyServerSettingsRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.SetServerSettings(url, info, sessionId)),
			new RouteAction<EmptyRequestData>("/singleplayer/pitfireteam/lostondeath", async (string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.GetLostOnDeathSettings(url, info, sessionId)),
			new RouteAction<EmptyRequestData>("/singleplayer/pitfireteam/recovery-notice", async (string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.GetStartupRecoveryNotice(url, info, sessionId)),
			new RouteAction<EmptyRequestData>("/singleplayer/pitfireteam/recovery-notice/ack", async (string url, EmptyRequestData info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.AcknowledgeStartupRecoveryNotice(url, info, sessionId)),
			new RouteAction<GetOtherProfileRequest>("/singleplayer/pitfireteam/teammate/profile", async (string url, GetOtherProfileRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.GetProfile(url, info, sessionId)),
			new RouteAction<FriendlyTeammateProfileOptionsRequest>("/singleplayer/pitfireteam/teammate/profile/options", async (string url, FriendlyTeammateProfileOptionsRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.GetProfileOptions(url, info, sessionId)),
			new RouteAction<FriendlyTeammateSuitRequest>("/singleplayer/pitfireteam/teammate/profile/suit", async (string url, FriendlyTeammateSuitRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.SetSuit(url, info, sessionId)),
			new RouteAction<FriendlyTeammateRenameRequest>("/singleplayer/pitfireteam/teammate/profile/rename", async (string url, FriendlyTeammateRenameRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.Rename(url, info, sessionId)),
			new RouteAction<FriendlyTeammateLoadoutRequest>("/singleplayer/pitfireteam/teammate/profile/loadout", async (string url, FriendlyTeammateLoadoutRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.SetLoadout(url, info, sessionId)),
			new RouteAction<FriendlyTeammateDefaultEquipmentRequest>("/singleplayer/pitfireteam/teammate/profile/default-equipment", async (string url, FriendlyTeammateDefaultEquipmentRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.SaveDefaultEquipment(url, info, sessionId)),
			new RouteAction<FriendlyTeammateBuyKitRequest>("/singleplayer/pitfireteam/teammate/profile/buy-kit", async (string url, FriendlyTeammateBuyKitRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.BuyKit(url, info, sessionId)),
			new RouteAction<FriendlyTeammateRepairEquipmentRequest>("/singleplayer/pitfireteam/teammate/profile/repair-equipment", async (string url, FriendlyTeammateRepairEquipmentRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.RepairDefaultEquipment(url, info, sessionId)),
			new RouteAction<FriendlyTeammateAggressionRequest>("/singleplayer/pitfireteam/teammate/profile/aggression", async (string url, FriendlyTeammateAggressionRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.SetAggression(url, info, sessionId)),
			new RouteAction<FriendlyTeammateTacticRequest>("/singleplayer/pitfireteam/teammate/profile/tactic", async (string url, FriendlyTeammateTacticRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.SetTactic(url, info, sessionId)),
			new RouteAction<FriendlyTeammateAutoJoinRequest>("/singleplayer/pitfireteam/teammate/autojoin", async (string url, FriendlyTeammateAutoJoinRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.SetAutoJoin(url, info, sessionId)),
			new RouteAction<FriendlyTeammateDeleteRequest>("/singleplayer/pitfireteam/teammate/delete", async (string url, FriendlyTeammateDeleteRequest info, MongoId sessionId, string? output, CancellationToken cancellationToken) => await callbacks.Delete(url, info, sessionId))
		})
	{
	}
}
