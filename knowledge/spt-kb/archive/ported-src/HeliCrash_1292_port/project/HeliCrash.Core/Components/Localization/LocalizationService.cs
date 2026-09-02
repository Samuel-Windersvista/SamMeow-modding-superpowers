using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class LocalizationService
{
    private Dictionary<string, string> _localeMappings;

    public void LoadLocale(string locale)
    {
        if (_localeMappings != null)
        {
            return;
        }

        string path = Path.Combine(FileUtil.Directory, "Locales.jsonc");

        var allLocaleMappings = FileUtil.LoadJson<Dictionary<string, Dictionary<string, string>>>(
            path
        );

        if (!allLocaleMappings.TryGetValue(locale, out Dictionary<string, string> localeMappings))
        {
            _localeMappings = allLocaleMappings["en"];
            return;
        }

        _localeMappings = localeMappings;
    }

    public string Localize(string key)
    {
        if (_localeMappings == null)
        {
            throw new InvalidOperationException(
                "HeliCrash localization mappings not yet loaded! Load it first with LocalizationService.LoadMappings()"
            );
        }

        return _localeMappings[key];
    }
}
