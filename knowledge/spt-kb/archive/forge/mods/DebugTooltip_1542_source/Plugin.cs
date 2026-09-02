using BepInEx;

namespace DebugTooltip
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public void Awake()
        {
            R.Init();
            Settings.Init(Config);

            new ShortcutPatch().Enable();

            TooltipPatches.Enable();

            ItemPatches.Enable();
            TraderPatches.Enable();
            QuestPatches.Enable();
        }
    }
}
