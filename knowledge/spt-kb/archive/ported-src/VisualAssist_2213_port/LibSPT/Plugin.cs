using System.Diagnostics.CodeAnalysis;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace VisualAssist;

[BepInPlugin("com.janky.visualassist", "Janky-VisualAssist", VisualAssistVersion)]
[SuppressMessage("ReSharper", "HeapView.ObjectAllocation.Evident")]
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
public class Plugin : BaseUnityPlugin
{
    public const string VisualAssistVersion = "1.1.0";

    public static ManualLogSource Log;

    public static ConfigEntry<bool> GrenadeArcEnabled;
    public static ConfigEntry<float> GrenadeArcDistance;
    public static ConfigEntry<Color> GrenadeArcStartColor;
    public static ConfigEntry<Color> GrenadeArcEndColor;
    public static ConfigEntry<Color> GrenadeArcKnobColor;
    public static ConfigEntry<float> GrenadeArcKnobSize;
    public static ConfigEntry<float> GrenadeArcStartGirth;
    public static ConfigEntry<float> GrenadeArcEndGirth;
    public static ConfigEntry<float> GrenadeArcResolution;

    private static ConfigEntry<bool> _loggingEnabled;

    private void Awake()
    {
        Log = Logger;

        SetupConfig();

        // Grenade
        new GrenadeThrowPrefixPatch().Enable();
        new GrenadeAssistGameWorldStartedPostfixPatch().Enable();
        new GrenadeAssistPlayerDisposePrefixPatch().Enable();
        new GrenadeAssistPlayerOnDeadPrefixPatch().Enable();

        if (_loggingEnabled.Value)
        {
            Log.LogInfo("Logging enabled");
        }
        else
        {
            Log.LogInfo("Logging disabled");
            BepInEx.Logging.Logger.Sources.Remove(Log);
        }
    }

    private void SetupConfig()
    {
        const string headerAssist = "1. Assist";
        const string headerDebug = "2. Debug";

        GrenadeArcEnabled = Config.Bind(headerAssist, "Enable Grenade Assist", true, new ConfigDescription(
            "Toggles a visual grenade assist - mostly for helping with 3rd person aiming.",
            tags: new ConfigurationManagerAttributes { Order = 9 }
        ));
        GrenadeArcDistance = Config.Bind(headerAssist, "Max Grenade Assist Distance", 25f, new ConfigDescription(
            "How far to draw the grenade assist arc.",
            new AcceptableValueRange<float>(1f, 50f),
            tags: new ConfigurationManagerAttributes { Order = 8 }
        ));
        GrenadeArcStartColor = Config.Bind(headerAssist, "Grenade Arc Start Color", new Color(0, 1, 0, 0f), new ConfigDescription(
            "Color of the start of the grenade arc. Make sure to configure the alpha value correctly so that you get a nice fade.",
            tags: new ConfigurationManagerAttributes { Order = 7 }
        ));
        GrenadeArcEndColor = Config.Bind(headerAssist, "Grenade Arc End Color", new Color(1, 0, 0, 0.75f), new ConfigDescription(
            "Color of the end of the grenade arc. Make sure to configure the alpha value correctly so that you get a nice fade.",
            tags: new ConfigurationManagerAttributes { Order = 6 }
        ));
        GrenadeArcKnobColor = Config.Bind(headerAssist, "Grenade Arc Knob Color", new Color(1, 0, 0, 0.75f), new ConfigDescription(
            "The uh, color of the knob. At the end of the big curved thing. Knob.",
            tags: new ConfigurationManagerAttributes { Order = 5 }
        ));
        GrenadeArcKnobSize = Config.Bind(headerAssist, "Grenade Arc Knob Size", 0.2f, new ConfigDescription(
            "Giggity.",
            new AcceptableValueRange<float>(0.01f, 1f),
            tags: new ConfigurationManagerAttributes { Order = 4 }
        ));
        GrenadeArcStartGirth = Config.Bind(headerAssist, "Grenade Arc Start Girth", 0.1f, new ConfigDescription(
            "Giggity.",
            new AcceptableValueRange<float>(0.01f, 1f),
            tags: new ConfigurationManagerAttributes { Order = 3 }
        ));
        GrenadeArcEndGirth = Config.Bind(headerAssist, "Grenade Arc End Girth", 0.05f, new ConfigDescription(
            "Giggity.",
            new AcceptableValueRange<float>(0.01f, 1f),
            tags: new ConfigurationManagerAttributes { Order = 2 }
        ));
        GrenadeArcResolution = Config.Bind(headerAssist, "Grenade Assist Resolution", 0.5f, new ConfigDescription(
            "Step size when calculating the grenade arc. Too small values take exponentially more time to compute and too large values become inaccurate.",
            new AcceptableValueRange<float>(0.1f, 2f),
            tags: new ConfigurationManagerAttributes { Order = 1, IsAdvanced = true }
        ));

        _loggingEnabled = Config.Bind(headerDebug, "Enable Debug Logging", false, new ConfigDescription(
            "Duh. Requires restarting the game to take effect.",
            tags: new ConfigurationManagerAttributes { Order = 1 }
        ));
    }
}