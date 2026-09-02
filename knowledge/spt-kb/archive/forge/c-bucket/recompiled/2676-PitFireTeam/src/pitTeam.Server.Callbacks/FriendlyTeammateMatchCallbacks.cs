using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Match;
using SPTarkov.Server.Core.Models.Eft.Ws;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Dialog;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;
using pitTeam.Server.Models;
using pitTeam.Server.Services;

namespace pitTeam.Server.Callbacks;

[Injectable(InjectionType.Transient)]
public class FriendlyTeammateMatchCallbacks(FriendlyTeammateService teammateService, FriendlyPostRaidService postRaidService, HttpResponseUtil httpResponseUtil, NotificationSendHelper notificationSendHelper, MailSendService mailSendService)
{
	public ValueTask<string> SendGroupInvite(string url, MatchGroupInviteSendRequest request, MongoId sessionId, string? previousOutput)
	{
		if (!teammateService.TryGetRaidGroupCharacter(sessionId, request.To, out GroupCharacter teammate, out string rejectionReason))
		{
			if (teammate != null && !string.IsNullOrWhiteSpace(rejectionReason))
			{
				Task.Run(async delegate
				{
					await Task.Delay(1000);
					WsNotificationEvent val2 = new WsNotificationEvent
					{
						EventType = NotificationEventType.groupMatchInviteDecline,
						EventIdentifier = new MongoId()
					};
					val2.ExtensionData["Aid"] = teammate.Aid;
					CharacterInfo info = teammate.Info;
					val2.ExtensionData["Nickname"] = ((info != null) ? info.Nickname : null) ?? teammate.Aid?.ToString();
					await notificationSendHelper.SendMessageAsync(sessionId, val2);
					mailSendService.SendSystemMessageToPlayer(sessionId, rejectionReason, null, 172800L, null);
				});
				return new ValueTask<string>(previousOutput ?? httpResponseUtil.GetBody("pitfireteam-teammate-invite", BackendErrorCodes.None, null, true));
			}
			return new ValueTask<string>(previousOutput ?? httpResponseUtil.NullResponse());
		}
		Task.Run(async delegate
		{
			await Task.Delay(1000);
			GroupCharacter val = teammate;
			WsNotificationEvent notification = new WsNotificationEvent
			{
				EventType = NotificationEventType.groupMatchInviteAccept,
				EventIdentifier = new MongoId()
			};
			notification.ExtensionData["Id"] = val.Id;
			notification.ExtensionData["Aid"] = val.Aid;
			notification.ExtensionData["Info"] = val.Info;
			notification.ExtensionData["VisualRepresentation"] = val.VisualRepresentation;
			notification.ExtensionData["IsLeader"] = false;
			notification.ExtensionData["IsReady"] = true;
			notification.ExtensionData["Region"] = val.Region;
			notification.ExtensionData["LookingGroup"] = false;
			await notificationSendHelper.SendMessageAsync(sessionId, notification);
		});
		return new ValueTask<string>(previousOutput ?? httpResponseUtil.GetBody("pitfireteam-teammate-invite", BackendErrorCodes.None, null, true));
	}

	public ValueTask<string> GetFollowerDetails(string url, EmptyRequestData _, MongoId sessionId)
	{
		return new ValueTask<string>(httpResponseUtil.GetBody(teammateService.ListFollowerDetails(sessionId)));
	}

	public ValueTask<string> PersistFollowerProgress(string url, FriendlyTeammateFollowerProgressBatchRequest request, MongoId sessionId)
	{
		teammateService.PersistFollowerProgress(sessionId, request.Entries);
		return new ValueTask<string>(httpResponseUtil.EmptyResponse());
	}

	public ValueTask<string> GenerateFollowerProfile(string url, FriendlyTeammateFollowerGenerateRequest request, MongoId sessionId)
	{
		BotBase? profile = null;
		if (!teammateService.TryGetSpawnProfile(sessionId, request.MemberId, request.Custom?.Health, out profile))
		{
			return new ValueTask<string>(httpResponseUtil.GetBody<object[]>(Array.Empty<object>(), BackendErrorCodes.None, null, true));
		}
		string spawnContext = profile?.Id.ToString() ?? request.MemberId ?? "unknown";
		HashSet<string> protectedIds = teammateService.GetProtectedSpawnItemIdsForExtraction(profile);
		postRaidService.RegisterProtectedRaidItemIds(sessionId, protectedIds, "server-generated teammate spawn equipment " + spawnContext);
		return new ValueTask<string>(httpResponseUtil.GetBody<BotBase[]>(new BotBase[] { profile }, BackendErrorCodes.None, null, true));
	}
}
