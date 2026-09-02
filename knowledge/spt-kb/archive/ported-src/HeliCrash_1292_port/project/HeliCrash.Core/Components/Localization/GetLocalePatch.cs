using System;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using JetBrains.Annotations;
using SamSWAT.HeliCrash.ArysReloaded.Utils;
using SPT.Reflection.Patching;

namespace SamSWAT.HeliCrash.ArysReloaded;

[UsedImplicitly]
public class GetLocalePatch : ModulePatch
{
    private static ConfigurationService s_configService;
    private static Logger s_logger;
    private static LocalizationService s_localizationService;

    public GetLocalePatch(
        ConfigurationService configService,
        Logger logger,
        LocalizationService localizationService
    )
    {
        s_configService = configService;
        s_logger = logger;
        s_localizationService = localizationService;
    }

    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(
            typeof(EFT.DataPrepareOperation),
            nameof(EFT.DataPrepareOperation.ReloadBackendLocale)
        );
    }

    [PatchPostfix]
    private static async void PatchPostfix(string locale, Task __result)
    {
        try
        {
            s_localizationService.LoadLocale(locale);
            s_configService.InitializeBindings();

            await __result;
        }
        catch (Exception ex)
        {
            s_logger.LogError(
                $"Error patching {nameof(EFT.DataPrepareOperation.ReloadBackendLocale)}: {ex.Message}\n{ex.StackTrace})"
            );
        }
    }
}
