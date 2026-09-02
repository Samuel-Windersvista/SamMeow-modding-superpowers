// TransferRequestCallbacks: SPT 4.0 -> 4.1.2 迁移
// MailSendService 命名空间: Services -> Services.Commerce
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RaidOverhaulMain.Models;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Extensions;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Eft.Profile;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services.Commerce;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;

namespace RaidOverhaulMain.Callbacks;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class TransferRequestCallbacks(HttpResponseUtil httpResponseUtil, MailSendService mailSendService, ICloner cloner)
{
	public virtual ValueTask<string> ReceiveAndSendItems(TransferRequestData request, MongoId sessionId)
	{
		if (request.Items == null || request.Items.Count == 0)
		{
			return new ValueTask<string>(httpResponseUtil.NullResponse());
		}
		List<Item>? list = cloner.Clone<List<Item>>(request.Items);
		List<Item>? list2 = (list != null) ? ItemExtensions.ReplaceIDs(list).ToList() : null;
		mailSendService.SendDirectNpcMessageToPlayer(sessionId, request.TraderId, (MessageType)2, request.Message ?? "Your items have been delivered. Don't forget to leave a tip!", list2, 172800L, (SystemData?)null, (MessageContentRagfair?)null);
		return new ValueTask<string>(httpResponseUtil.NullResponse());
	}
}
