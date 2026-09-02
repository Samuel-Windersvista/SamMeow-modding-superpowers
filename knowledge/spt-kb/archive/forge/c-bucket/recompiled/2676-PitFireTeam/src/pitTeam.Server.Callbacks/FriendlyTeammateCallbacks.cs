using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Models;
using pitTeam.Server.Services;

namespace pitTeam.Server.Callbacks;

[Injectable(InjectionType.Transient)]
public class FriendlyTeammateCallbacks(HttpResponseUtil httpResponse, FriendlyTeammateService teammateService, FriendlyServerSettingsService settingsService)
{
	public ValueTask<string> Create(string url, FriendlyTeammateCreateRequest request, MongoId sessionId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return new ValueTask<string>(httpResponse.GetBody<SearchFriendResponse>(teammateService.CreateTeammate(sessionId, request), (BackendErrorCodes)0, (string)null, true));
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> List(string url, EmptyRequestData _, MongoId sessionId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return new ValueTask<string>(httpResponse.GetBody<List<object>>(teammateService.ListTeammates(sessionId), (BackendErrorCodes)0, (string)null, true));
	}

	public ValueTask<string> ListAutoJoin(string url, EmptyRequestData _, MongoId sessionId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return new ValueTask<string>(httpResponse.GetBody<List<string>>(teammateService.GetAutoJoinTeammateAccountIds(sessionId), (BackendErrorCodes)0, (string)null, true));
	}

	public ValueTask<string> SetServerSettings(string url, FriendlyServerSettingsRequest request, MongoId sessionId)
	{
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005a: Unknown result type (might be due to invalid IL or missing references)
		string text = settingsService.LoadSettings().LoadoutManagementMode ?? "Restricted";
		settingsService.SaveAndApply(request);
		string text2 = request?.LoadoutManagementMode ?? "Restricted";
		if (!string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
		{
			teammateService.LogLoadoutManagementModeChange(sessionId, text, text2);
			teammateService.SelectDefaultLoadoutForAllTeammates(sessionId, text, text2);
		}
		return new ValueTask<string>(httpResponse.NullResponse());
	}

	public ValueTask<string> GetLostOnDeathSettings(string url, EmptyRequestData _, MongoId sessionId)
	{
		return new ValueTask<string>(httpResponse.GetBody<FriendlyLostOnDeathSettingsResponse>(settingsService.GetLostOnDeathSettings(), (BackendErrorCodes)0, (string)null, true));
	}

	public ValueTask<string> GetStartupRecoveryNotice(string url, EmptyRequestData _, MongoId sessionId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		return new ValueTask<string>(httpResponse.GetBody<FriendlyTeammateStartupRecoveryNotice>(teammateService.GetStartupRecoveryNotice(sessionId), (BackendErrorCodes)0, (string)null, true));
	}

	public ValueTask<string> AcknowledgeStartupRecoveryNotice(string url, EmptyRequestData _, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		teammateService.AcknowledgeStartupRecoveryNotice(sessionId);
		return new ValueTask<string>(httpResponse.NullResponse());
	}

	public ValueTask<string> GetProfile(string url, GetOtherProfileRequest request, MongoId sessionId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return new ValueTask<string>(httpResponse.GetBody<GetOtherProfileResponse>(teammateService.GetTeammateProfile(sessionId, request), (BackendErrorCodes)0, (string)null, true));
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> GetProfileOptions(string url, FriendlyTeammateProfileOptionsRequest request, MongoId sessionId)
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return new ValueTask<string>(httpResponse.GetBody<FriendlyTeammateProfileOptionsResponse>(teammateService.GetProfileOptions(sessionId, request), (BackendErrorCodes)0, (string)null, true));
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> SetSuit(string url, FriendlyTeammateSuitRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			teammateService.SetTeammateSuit(sessionId, request);
			return new ValueTask<string>(httpResponse.NullResponse());
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> Rename(string url, FriendlyTeammateRenameRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			teammateService.RenameTeammate(sessionId, request);
			return new ValueTask<string>(httpResponse.NullResponse());
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> SetLoadout(string url, FriendlyTeammateLoadoutRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			teammateService.SetTeammateLoadout(sessionId, request);
			return new ValueTask<string>(httpResponse.NullResponse());
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> SaveDefaultEquipment(string url, FriendlyTeammateDefaultEquipmentRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			FriendlyTeammateDefaultEquipmentResponse friendlyTeammateDefaultEquipmentResponse = teammateService.SaveTeammateDefaultEquipment(sessionId, request);
			return new ValueTask<string>(httpResponse.GetBody<FriendlyTeammateDefaultEquipmentResponse>(friendlyTeammateDefaultEquipmentResponse, (BackendErrorCodes)0, (string)null, true));
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> BuyKit(string url, FriendlyTeammateBuyKitRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			FriendlyTeammateBuyKitResponse friendlyTeammateBuyKitResponse = teammateService.BuyTeammateKit(sessionId, request);
			return new ValueTask<string>(httpResponse.GetBody<FriendlyTeammateBuyKitResponse>(friendlyTeammateBuyKitResponse, (BackendErrorCodes)0, (string)null, true));
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> RepairDefaultEquipment(string url, FriendlyTeammateRepairEquipmentRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			FriendlyTeammateRepairEquipmentResponse friendlyTeammateRepairEquipmentResponse = teammateService.RepairTeammateDefaultEquipment(sessionId, request);
			return new ValueTask<string>(httpResponse.GetBody<FriendlyTeammateRepairEquipmentResponse>(friendlyTeammateRepairEquipmentResponse, (BackendErrorCodes)0, (string)null, true));
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> SetAggression(string url, FriendlyTeammateAggressionRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			teammateService.SetTeammateAggression(sessionId, request);
			return new ValueTask<string>(httpResponse.NullResponse());
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> SetTactic(string url, FriendlyTeammateTacticRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			teammateService.SetTeammateTactic(sessionId, request);
			return new ValueTask<string>(httpResponse.NullResponse());
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> SetAutoJoin(string url, FriendlyTeammateAutoJoinRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			teammateService.SetTeammateAutoJoin(sessionId, request);
			return new ValueTask<string>(httpResponse.NullResponse());
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}

	public ValueTask<string> Delete(string url, FriendlyTeammateDeleteRequest request, MongoId sessionId)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return new ValueTask<string>(httpResponse.GetBody<FriendlyTeammateDeleteResponse>(new FriendlyTeammateDeleteResponse
			{
				Deleted = teammateService.DeleteTeammate(sessionId, request)
			}, (BackendErrorCodes)0, (string)null, true));
		}
		catch (FriendlyTeammateException ex)
		{
			return new ValueTask<string>(httpResponse.GetBody<object>((object)null, (BackendErrorCodes)500, ex.Message, true));
		}
	}
}
