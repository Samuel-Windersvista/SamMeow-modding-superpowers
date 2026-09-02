// LogToServerRequestCallbacks: SPT 4.0 -> 4.1.2 迁移
using System.Threading.Tasks;
using RaidOverhaulMain.Helpers;
using RaidOverhaulMain.Models;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Utils;

namespace RaidOverhaulMain.Callbacks;

[Injectable(InjectionType.Transient, int.MaxValue)]
public class LogToServerRequestCallbacks(HttpResponseUtil httpResponseUtil)
{
	public virtual ValueTask<string> LogToServer<T>(LogToServerRequestData request, ISptLogger<T> logger)
	{
		ROLogger.LogToServer(logger, request.Message ?? string.Empty, LogTextColor.Cyan);
		return new ValueTask<string>(httpResponseUtil.NullResponse());
	}
}
