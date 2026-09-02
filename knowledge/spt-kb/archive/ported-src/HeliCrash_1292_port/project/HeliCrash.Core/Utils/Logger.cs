using BepInEx.Logging;
using EFT.UI;
using JetBrains.Annotations;

namespace SamSWAT.HeliCrash.ArysReloaded.Utils;

[UsedImplicitly]
public class Logger(ManualLogSource logSource, ConfigurationService configService)
{
    public void LogInfo(string message)
    {
        logSource.LogInfo(message);

        if (configService.LoggingEnabled.Value)
        {
            ConsoleScreen.Log(message);
        }
    }

    public void LogWarning(string message)
    {
        logSource.LogWarning(message);

        if (configService.LoggingEnabled.Value)
        {
            ConsoleScreen.LogWarning(message);
        }
    }

    public void LogError(string message)
    {
        logSource.LogError(message);

        if (configService.LoggingEnabled.Value)
        {
            ConsoleScreen.LogError(message);
        }
    }
}
