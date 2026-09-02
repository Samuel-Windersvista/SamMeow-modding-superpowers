using System.Threading.Tasks;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using pitTeam.Server.Models;
using pitTeam.Server.Services;

namespace pitTeam.Server.Callbacks;

[Injectable(InjectionType.Transient)]
public class FriendlyLanguageCallbacks(FriendlyLanguageService languageService)
{
	public ValueTask<string> Get(string url, FriendlyLanguageRequest request, MongoId sessionId)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		return new ValueTask<string>(languageService.GetLanguageJson(sessionId, request.Locale, request.EnglishJson));
	}
}
