using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Models;
using pitTeam.Server.Services;

namespace pitTeam.Server.Callbacks;

[Injectable(InjectionType.Transient)]
public class FriendlyPostRaidCallbacks(HttpResponseUtil httpResponse, FriendlyPostRaidService postRaidService, FriendlyRecruitService recruitService, FriendlyTeammateService teammateService)
{
	public ValueTask<string> ReturnItems(string url, FriendlyPostRaidReturnItemsRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		postRaidService.HandleReturnItems(sessionId, request);
		return new ValueTask<string>(httpResponse.NullResponse());
	}

	public ValueTask<string> TeamEscaped(string url, FriendlyPostRaidTeamEscapedRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		postRaidService.HandleTeamEscaped(sessionId, request);
		return new ValueTask<string>(httpResponse.NullResponse());
	}

	public ValueTask<string> RecruitPickup(string url, FriendlyRecruitPickupRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		recruitService.QueueRecruitPickups(sessionId, request.Candidates);
		return new ValueTask<string>(httpResponse.NullResponse());
	}

	public ValueTask<string> RecordKillMessage(string url, FriendlyPostRaidKillMessageRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		postRaidService.RecordKillMessage(sessionId, request);
		return new ValueTask<string>(httpResponse.NullResponse());
	}

	public ValueTask<string> RegisterProtectedItems(string url, FriendlyPostRaidProtectedItemsRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		postRaidService.RegisterProtectedRaidItems(sessionId, request);
		return new ValueTask<string>(httpResponse.NullResponse());
	}

	public ValueTask<string> EndLocalRaid(string url, EndLocalRaidRequestData request, MongoId sessionId, string? output)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		postRaidService.RemoveProtectedTeammateItemsFromExtractedProfile(sessionId, request);
		postRaidService.HandleEndLocalRaidKillMessages(sessionId, request);
		return new ValueTask<string>(output ?? httpResponse.NullResponse());
	}

	public ValueTask<string> RaidOutcomes(string url, FriendlyTeammateDeathEscapeRequest request, MongoId sessionId)
	{
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		if ((object)request == null)
		{
			request = new FriendlyTeammateDeathEscapeRequest();
		}
		if (request.ResolveOnly)
		{
			return new ValueTask<string>(httpResponse.GetBody<FriendlyTeammateRaidOutcomeResponse>(teammateService.ResolveRaidOutcomes(request.Entries), (BackendErrorCodes)0, (string)null, true));
		}
		List<FriendlyTeammateDeathEscapeEntry> list = request.Entries ?? new List<FriendlyTeammateDeathEscapeEntry>();
		if (list.Any((FriendlyTeammateDeathEscapeEntry entry) => entry?.RollEscape ?? false))
		{
			list = teammateService.ResolveRaidOutcomes(list).Entries;
		}
		FriendlyTeammateDeathEscapeSummary summary = teammateService.PersistDeathEscapeOutcomes(sessionId, list);
		if (request.Notify)
		{
			postRaidService.HandleDeathEscapeSummary(sessionId, summary);
		}
		return new ValueTask<string>(httpResponse.NullResponse());
	}

	public ValueTask<string> DeathEscape(string url, FriendlyTeammateDeathEscapeRequest request, MongoId sessionId)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		return RaidOutcomes(url, request, sessionId);
	}
}
