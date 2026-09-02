using System.Collections.Generic;
using SPTarkov.Common.Models.Logging;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Utils.Json;

namespace tarkovhdrework;

public class NewLocales(ISptLogger<TarkovHDRework> logger, LocaleTable localeTable, LocaleService localeService, ServerLocalisationService serverLocalisationService)
{
	public void EditLocales()
	{
		if (localeTable.Global.TryGetValue("en", out LazyLoad<GlobalLocaleDictionary>? value))
		{
			value.AddTransformer(lazyloadedLocaleData =>
			{
				if (lazyloadedLocaleData is null)
				{
					return null;
				}
				lazyloadedLocaleData["Attention! This is a Beta version of Escape from Tarkov for testing purposes."] = "It's Porkin Time!";
				lazyloadedLocaleData["57347ca924597744596b4e71 Name"] = "RTX4090";
				lazyloadedLocaleData.Add("TestingLocales", "Testing Locales");
				return lazyloadedLocaleData;
			});
			logger.Success("Added a custom locale to the database");
		}
		Dictionary<string, string> localeDb = localeService.GetLocaleDb("en");
		logger.Info(localeDb["TestingLocales"]);
		logger.Info(serverLocalisationService.GetText("TestingLocales", (object?)null));
		logger.Info(localeDb["Attention! This is a Beta version of Escape from Tarkov for testing purposes."]);
	}
}
