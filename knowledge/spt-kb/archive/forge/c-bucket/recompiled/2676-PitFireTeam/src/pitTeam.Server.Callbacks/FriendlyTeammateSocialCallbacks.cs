using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Dialog;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Models;
using pitTeam.Server.Services;

namespace pitTeam.Server.Callbacks;

[Injectable(InjectionType.Transient)]
public class FriendlyTeammateSocialCallbacks(FriendlyTeammateService teammateService, FriendlyRecruitService recruitService, HttpResponseUtil httpResponseUtil, JsonUtil jsonUtil)
{
	public ValueTask<string> MergeFriendList(string url, EmptyRequestData _, MongoId sessionId, string? previousOutput)
	{
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		FriendlyTeammateBodyResponse<GetFriendListDataResponse> friendlyTeammateBodyResponse = DeserializeBody<GetFriendListDataResponse>(previousOutput);
		GetFriendListDataResponse val = (GetFriendListDataResponse)(((object)friendlyTeammateBodyResponse?.Data) ?? ((object)new GetFriendListDataResponse
		{
			Friends = new List<UserDialogInfo>(),
			Ignore = new List<string>(),
			InIgnoreList = new List<string>()
		}));
		GetFriendListDataResponse val2 = val;
		if (val2.Friends == null)
		{
			List<UserDialogInfo> list = (val2.Friends = new List<UserDialogInfo>());
		}
		foreach (UserDialogInfo teammate in teammateService.ListTeammateDialogs(sessionId))
		{
			if (!val.Friends.Any((UserDialogInfo existing) => existing.Id == teammate.Id || (existing.Aid != 0 && teammate.Aid != 0 && existing.Aid == teammate.Aid)))
			{
				val.Friends.Add(teammate);
			}
		}
		return new ValueTask<string>(httpResponseUtil.GetBody<GetFriendListDataResponse>(val, (friendlyTeammateBodyResponse?.Err).GetValueOrDefault(), friendlyTeammateBodyResponse?.ErrMsg, true));
	}

	public ValueTask<string> MergeFriendRequestInbox(string url, EmptyRequestData _, MongoId sessionId, string? previousOutput)
	{
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_009f: Unknown result type (might be due to invalid IL or missing references)
		FriendlyTeammateBodyResponse<List<FriendlySocialFriendRequestEntry>> friendlyTeammateBodyResponse = DeserializeBody<List<FriendlySocialFriendRequestEntry>>(previousOutput);
		List<FriendlySocialFriendRequestEntry> list = friendlyTeammateBodyResponse?.Data ?? new List<FriendlySocialFriendRequestEntry>();
		foreach (FriendlySocialFriendRequestEntry request in recruitService.ListRecruitFriendRequests(sessionId))
		{
			if (!list.Any((FriendlySocialFriendRequestEntry existing) => string.Equals(existing.From, request.From, StringComparison.Ordinal)))
			{
				list.Add(request);
			}
		}
		return new ValueTask<string>(httpResponseUtil.GetBody<List<FriendlySocialFriendRequestEntry>>(list, (friendlyTeammateBodyResponse?.Err).GetValueOrDefault(), friendlyTeammateBodyResponse?.ErrMsg, true));
	}

	public ValueTask<string> MergeProfileView(string url, GetOtherProfileRequest request, MongoId sessionId, string? previousOutput)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		FriendlyTeammateBodyResponse<GetOtherProfileResponse> friendlyTeammateBodyResponse = DeserializeBody<GetOtherProfileResponse>(previousOutput);
		if (teammateService.TryGetTeammateProfile(sessionId, request.AccountId, out GetOtherProfileResponse profile) && profile != (GetOtherProfileResponse)null)
		{
			return new ValueTask<string>(httpResponseUtil.GetBody<GetOtherProfileResponse>(profile, (BackendErrorCodes)0, (string)null, true));
		}
		if (recruitService.TryGetRecruitProfile(sessionId, request.AccountId, out GetOtherProfileResponse profile2) && profile2 != (GetOtherProfileResponse)null)
		{
			return new ValueTask<string>(httpResponseUtil.GetBody<GetOtherProfileResponse>(profile2, (BackendErrorCodes)0, (string)null, true));
		}
		if (int.TryParse(request.AccountId, out var result) && (object)friendlyTeammateBodyResponse != null)
		{
			GetOtherProfileResponse? data = friendlyTeammateBodyResponse.Data;
			if (((data != null) ? data.Aid : ((int?)null)) == result)
			{
				return new ValueTask<string>(previousOutput ?? httpResponseUtil.NullResponse());
			}
		}
		return new ValueTask<string>(previousOutput ?? httpResponseUtil.NullResponse());
	}

	public ValueTask<string> DeleteFriend(string url, DeleteFriendRequest request, MongoId sessionId, string? previousOutput)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		teammateService.DeleteTeammateByProfileId(sessionId, request.FriendId);
		return new ValueTask<string>(previousOutput ?? httpResponseUtil.NullResponse());
	}

	public ValueTask<string> AcceptFriendRequest(string url, AcceptFriendRequestData request, MongoId sessionId, string? previousOutput)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		if (recruitService.AcceptRecruitRequest(sessionId, ((BaseFriendRequest)request).ProfileId))
		{
			return new ValueTask<string>(httpResponseUtil.NullResponse());
		}
		return new ValueTask<string>(previousOutput ?? httpResponseUtil.NullResponse());
	}

	public ValueTask<string> AcceptAllFriendRequests(string url, EmptyRequestData request, MongoId sessionId, string? previousOutput)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		recruitService.AcceptAllRecruitRequests(sessionId);
		return new ValueTask<string>(previousOutput ?? httpResponseUtil.NullResponse());
	}

	public ValueTask<string> DeclineFriendRequest(string url, DeclineFriendRequestData request, MongoId sessionId, string? previousOutput)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		if (recruitService.DeclineRecruitRequest(sessionId, ((BaseFriendRequest)request).ProfileId))
		{
			return new ValueTask<string>(httpResponseUtil.NullResponse());
		}
		return new ValueTask<string>(previousOutput ?? httpResponseUtil.NullResponse());
	}

	private FriendlyTeammateBodyResponse<T>? DeserializeBody<T>(string? previousOutput)
	{
		if (string.IsNullOrWhiteSpace(previousOutput))
		{
			return null;
		}
		return jsonUtil.Deserialize<FriendlyTeammateBodyResponse<T>>(previousOutput);
	}
}
