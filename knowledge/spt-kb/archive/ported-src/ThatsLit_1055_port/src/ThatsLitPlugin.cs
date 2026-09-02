using System;
using System.Collections;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using DrakiaXYZ.VersionChecker;
using EFT.Communications;
using EFT.UI;
using SPT.Reflection.Patching;
using ThatsLit.Patches.Vision;
using UnityEngine;

namespace ThatsLit;

[BepInPlugin("bastudio.thatslit", "That's Lit", "1.3100.3")]
[BepInDependency("com.SPT.core", "4.1.0")]
[BepInProcess("EscapeFromTarkov.exe")]
[BepInDependency("me.sol.sain", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("bastudio.updatenotifier", BepInDependency.DependencyFlags.SoftDependency)]
public class ThatsLitPlugin : BaseUnityPlugin
{
	internal static ManagedStopWatch swUpdate;

	internal static ManagedStopWatch swFUpdate;

	internal static ManagedStopWatch swGUI;

	internal static ManagedStopWatch swFoliage;

	internal static ManagedStopWatch swTerrain;

	internal static ManagedStopWatch swScoreCalc;

	internal static ManagedStopWatch swSeenCoef;

	internal static ManagedStopWatch swEncountering;

	internal static ManagedStopWatch swExtraVisDis;

	internal static ManagedStopWatch swNoBushOverride;

	internal static ManagedStopWatch swBlindFireScatter;

	internal static bool SAINLoaded { get; private set; }

	public static ConfigEntry<bool> ScoreInfo { get; private set; }

	public static ConfigEntry<bool> WeatherInfo { get; private set; }

	public static ConfigEntry<bool> EquipmentInfo { get; private set; }

	public static ConfigEntry<bool> TerrainInfo { get; private set; }

	public static ConfigEntry<bool> FoliageInfo { get; private set; }

	public static ConfigEntry<bool> DebugInfo { get; private set; }

	public static ConfigEntry<int> InfoOffset { get; private set; }

	public static ConfigEntry<int> InfoFontSizeOverride { get; private set; }

	public static ConfigEntry<bool> HideMapTip { get; private set; }

	public static ConfigEntry<bool> DebugTexture { get; private set; }

	public static ConfigEntry<bool> DebugTerrain { get; private set; }

	public static ConfigEntry<bool> DebugCompat { get; private set; }

	public static ConfigEntry<bool> DebugProxy { get; private set; }

	public static ConfigEntry<bool> EnabledMod { get; private set; }

	public static ConfigEntry<bool> EnabledLighting { get; private set; }

	public static ConfigEntry<bool> EnabledCameraThrottling { get; private set; }

	public static ConfigEntry<bool> EnabledEncountering { get; private set; }

	public static ConfigEntry<bool> EnabledFoliage { get; private set; }

	public static ConfigEntry<bool> EnabledGrasses { get; private set; }

	public static ConfigEntry<bool> EnabledBushRatting { get; private set; }

	public static ConfigEntry<bool> EnableMovementImpact { get; private set; }

	public static ConfigEntry<bool> EnableEquipmentCheck { get; private set; }

	public static ConfigEntry<bool> EnableSimFreeLook { get; private set; }

	public static ConfigEntry<bool> EnableBodyPartsRecognition { get; private set; }

	public static ConfigEntry<bool> EnableNearestBotSteering { get; private set; }

	public static ConfigEntry<bool> EnableExtraFlashLightReaction { get; private set; }

	public static ConfigEntry<bool> AlternativeReactionFluctuation { get; private set; }

	public static ConfigEntry<float> ScoreOffset { get; private set; }

	public static ConfigEntry<float> DarknessImpactScaleOffset { get; private set; }

	public static ConfigEntry<float> BrightnessImpactScaleOffset { get; private set; }

	public static float DarknessImpactScale => DarknessImpactScaleOffset.Value * 2f;

	public static float BrightnessImpactScale => BrightnessImpactScaleOffset.Value * 2f;

	public static ConfigEntry<float> ExtraDarknessImpactScale { get; private set; }

	public static ConfigEntry<float> ExtraBrightnessImpactScale { get; private set; }

	public static ConfigEntry<float> ExtraVisionDistanceScale { get; private set; }

	public static ConfigEntry<float> FinalOffset { get; private set; }

	public static ConfigEntry<float> FinalImpactScaleDelaying { get; private set; }

	public static ConfigEntry<float> FinalImpactScaleFastening { get; private set; }

	public static ConfigEntry<float> FoliageImpactScale { get; private set; }

	public static ConfigEntry<bool> IncludeBosses { get; private set; }

	public static ConfigEntry<bool> EnableLighthouse { get; private set; }

	public static ConfigEntry<bool> EnableFactoryNight { get; private set; }

	public static ConfigEntry<bool> EnableReserve { get; private set; }

	public static ConfigEntry<bool> EnableCustoms { get; private set; }

	public static ConfigEntry<bool> EnableShoreline { get; private set; }

	public static ConfigEntry<bool> EnableInterchange { get; private set; }

	public static ConfigEntry<bool> EnableStreets { get; private set; }

	public static ConfigEntry<bool> EnableGroundZero { get; private set; }

	public static ConfigEntry<bool> EnableWoods { get; private set; }

	public static ConfigEntry<bool> EnableHideout { get; private set; }

	public static ConfigEntry<bool> ShadowlessGroundZero { get; private set; }

	public static ConfigEntry<bool> ShadowlessStreets { get; private set; }

	public static ConfigEntry<bool> EnableBenchmark { get; private set; }

	public static ConfigEntry<int> ResLevel { get; private set; }

	public static ConfigEntry<int> FoliageSamples { get; private set; }

	public static ConfigEntry<bool> VolumetricLightRenderer { get; private set; }

	public static ConfigEntry<bool> InterruptSAINNoBush { get; private set; }

	public static ConfigEntry<bool> PMCOnlyMode { get; private set; }

	public static ConfigEntry<bool> ForceBlindFireScatter { get; private set; }

	public static ConfigEntry<bool> BotLookDirectionTweaks { get; private set; }

	static ThatsLitPlugin()
	{
		swUpdate = new ManagedStopWatch("Update");
		swFUpdate = new ManagedStopWatch("FUpdate");
		swGUI = new ManagedStopWatch("GUI");
		swFoliage = new ManagedStopWatch("Foliage");
		swTerrain = new ManagedStopWatch("Terrain");
		swScoreCalc = new ManagedStopWatch("ScoreCalc");
		swSeenCoef = new ManagedStopWatch("SeenCoef");
		swEncountering = new ManagedStopWatch("Encountering");
		swExtraVisDis = new ManagedStopWatch("ExtraVisDis");
		swNoBushOverride = new ManagedStopWatch("NoBushOverride");
		swBlindFireScatter = new ManagedStopWatch("BlindFireScatter");
	}

	private void Awake()
	{
		if (!VersionChecker.CheckEftVersion(Logger, Info, Config))
		{
			throw new Exception("Invalid EFT Version");
		}
		BindConfigs();
		ThatsLitCompat.LoadCompatFiles();
		if (Chainloader.PluginInfos.ContainsKey("me.sol.sain"))
		{
			SAINLoaded = true;
		}
		Patches();
		TryCheckUpdate();
	}

	private void Start()
	{
		((MonoBehaviour)this).StartCoroutine(FikaHelper());
		IEnumerator FikaHelper()
		{
			WaitForSeconds wait = new WaitForSeconds(1f);
			while (true)
			{
				if (MonoBehaviourSingleton<PreloaderUI>.Instantiated)
				{
					CommonUI instance = MonoBehaviourSingleton<CommonUI>.Instance;
					int num;
					if (instance == null)
					{
						num = 1;
					}
					else
					{
						MenuScreen menuScreen = instance.MenuScreen;
						num = ((((menuScreen != null) ? new bool?(((Behaviour)menuScreen).isActiveAndEnabled) : ((bool?)null)) != true) ? 1 : 0);
					}
					if (num == 0)
					{
						break;
					}
				}
				yield return wait;
			}
			if (Chainloader.PluginInfos.ContainsKey("com.fika.core") && EnabledLighting.Value && !Chainloader.PluginInfos.ContainsKey("bastudio.thatslit.sync"))
			{
				string text = "[That's Lit] Fika detected, but That's Lit Sync is not installed. Without the extension, you lose extra fps per player. Get Sync from Fika Discord - #mod-releases - That's Lit. Direct link is provided in the console (`).";
				ConsoleScreen.Log("That's Lit Sync: https://discord.com/channels/1202292159366037545/1245739513481924739");
				NotificationManager.DisplayWarningNotification(text, (ENotificationDurationType)2);
				Logger.LogError((object)text);
			}
		}
	}

	public void TryCheckUpdate()
	{
		string text = "https://raw.githubusercontent.com/No3371/SPT_ThatsLit/main/ThatsLit.Core/.update_notifier";
		if (!Chainloader.PluginInfos.TryGetValue("bastudio.updatenotifier", out var value))
		{
			Logger.LogInfo((object)"Update Notifier not found.");
			return;
		}
		BaseUnityPlugin instance = value.Instance;
		((object)instance).GetType().GetMethod("CheckForUpdate", new Type[2]
		{
			typeof(BaseUnityPlugin),
			typeof(string)
		}).Invoke(instance, new object[2] { this, text });
	}

	private void BindConfigs()
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		//IL_006b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Expected O, but got Unknown
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Expected O, but got Unknown
		//IL_0141: Unknown result type (might be due to invalid IL or missing references)
		//IL_014b: Expected O, but got Unknown
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Expected O, but got Unknown
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e9: Expected O, but got Unknown
		//IL_022e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0238: Expected O, but got Unknown
		//IL_0389: Unknown result type (might be due to invalid IL or missing references)
		//IL_0393: Expected O, but got Unknown
		//IL_03cc: Unknown result type (might be due to invalid IL or missing references)
		//IL_03d6: Expected O, but got Unknown
		//IL_0409: Unknown result type (might be due to invalid IL or missing references)
		//IL_0413: Expected O, but got Unknown
		//IL_0446: Unknown result type (might be due to invalid IL or missing references)
		//IL_0450: Expected O, but got Unknown
		//IL_0495: Unknown result type (might be due to invalid IL or missing references)
		//IL_049f: Expected O, but got Unknown
		//IL_0576: Unknown result type (might be due to invalid IL or missing references)
		//IL_0580: Expected O, but got Unknown
		//IL_05c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_05cf: Expected O, but got Unknown
		//IL_062e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0638: Expected O, but got Unknown
		//IL_0655: Unknown result type (might be due to invalid IL or missing references)
		//IL_065f: Expected O, but got Unknown
		//IL_067c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0686: Expected O, but got Unknown
		//IL_06a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ad: Expected O, but got Unknown
		//IL_06ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d4: Expected O, but got Unknown
		//IL_06f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_06fb: Expected O, but got Unknown
		//IL_071e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0728: Expected O, but got Unknown
		//IL_074c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0756: Expected O, but got Unknown
		//IL_077f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0789: Expected O, but got Unknown
		//IL_07ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_07b6: Expected O, but got Unknown
		//IL_080a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0814: Expected O, but got Unknown
		//IL_087e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0888: Expected O, but got Unknown
		//IL_08ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_08c4: Expected O, but got Unknown
		//IL_08f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0900: Expected O, but got Unknown
		string text = "0. Readme";
		Config.Bind<bool>(text, "Performance (Readme)", true, new ConfigDescription("The mod takes away at least several fps. Actual overhead varies from machine to machine, some lose 5, some lose 20. You can try giving up the brightness module if the performance is not acceptable.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				ReadOnly = true
			}
		}));
		Config.Bind<bool>(text, "Balance (Readme)", true, new ConfigDescription("The mod aims to make AIs reasonable without making it easy, but it requires some proper setup. Besides, SAIN or other mods can change bots, and everyone has different configurations, so you may have different experience than mine with default That's Lit configs. Check \"Recommended Mods\" on the mod page for more info.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				ReadOnly = true
			}
		}));
		Config.Bind<bool>(text, "Mechanics (Readme)", true, new ConfigDescription("The mod tries to make everything as intuitive as possible so you can enjoy human-like AIs by just applying common sense. However, EFT's AIs are never designed to be human-like, the mod basically \"imagine up\" some new systems out of data here and there in the game, there are things can't be done, or can't be very accurate. It's best to read the mod description page if you want to make the most out of That's Lit.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				ReadOnly = true
			}
		}));
		text = "1. Main";
		EnabledMod = Config.Bind<bool>(text, "Enable", true, "Enable the mod. Some features can't be re-enabled in raids.");
		text = "2. Darkness / Brightness";
		EnabledLighting = Config.Bind<bool>(text, "Enable", true, new ConfigDescription("Enable the module. With this turned off, AIs are not affected by your brightness.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 101
			}
		}));
		EnabledCameraThrottling = Config.Bind<bool>(text, "Throttle Camera", false, new ConfigDescription("When the ambience is bright, make the camera only run every some frames to reduce average fps impact.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 100
			}
		}));
		DarknessImpactScaleOffset = Config.Bind<float>(text, "Darkness Impact Offset", 0.5f, new ConfigDescription("Scale how AI noticing players slower due to darkness. If That's Lit change bot reaction from 1s to 3s, setting to 100% will makes it 5s (double from 50%). Be careful when increasing this as it could easily breaks the combat balance.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 1f), new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 95
			}
		}));
		BrightnessImpactScaleOffset = Config.Bind<float>(text, "Brightness Impact Offset", 0.5f, new ConfigDescription("Scale how AI noticing players faster due to brightness. If That's Lit change bot reaction from 3s to 2s, setting to 100% will makes it 1s (double from 50%; will not make it lower than 50% of original). Be careful when increasing this as it could easily breaks the combat balance.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 1f), new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 94
			}
		}));
		ExtraVisionDistanceScale = Config.Bind<float>(text, "Extra Vision Distance Scale", 1f, new ConfigDescription("Scale how AI noticing players from further under some circumstances. This is designed to compensate low night vision distance from SAIN, you may want to set this to 0 if you don't run SAIN.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 1f), new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 93
			}
		}));
		EnableFactoryNight = Config.Bind<bool>(text, "Factory (Night)", true, "Enable darkness/brightness on the map.");
		EnableLighthouse = Config.Bind<bool>(text, "Lighthouse", true, "Enable darkness/brightness on the map.");
		EnableShoreline = Config.Bind<bool>(text, "Shoreline", true, "Enable darkness/brightness on the map.");
		EnableReserve = Config.Bind<bool>(text, "Reserve", true, "Enable darkness/brightness on the map.");
		EnableWoods = Config.Bind<bool>(text, "Woods", true, "Enable darkness/brightness on the map.");
		EnableInterchange = Config.Bind<bool>(text, "Interchange", true, "Enable darkness/brightness on the map.");
		EnableCustoms = Config.Bind<bool>(text, "Customs", true, "Enable darkness/brightness on the map.");
		EnableStreets = Config.Bind<bool>(text, "Streets", true, "Enable darkness/brightness on the map.");
		EnableGroundZero = Config.Bind<bool>(text, "Ground Zero", true, "Enable darkness/brightness on the map.");
		VolumetricLightRenderer = Config.Bind<bool>(text, "Observe Volumetric Lights", true, "Let Brightness Module reacts to volumetric lights. Disable this if it cause issues.");
		text = "3. Encountering Patch";
		EnabledEncountering = Config.Bind<bool>(text, "Enable", true, new ConfigDescription("Enable the module. Encountering Patch nerf bots reaction at the moment they see a player, especially when they are sprinting.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 100
			}
		}));
		text = "4. Grasses & Foliage";
		EnabledGrasses = Config.Bind<bool>(text, "Enable Grasses", true, new ConfigDescription("Enable the module. This enable grasses to block bot vision.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 100
			}
		}));
		EnabledFoliage = Config.Bind<bool>(text, "Enable Foliage", true, new ConfigDescription("Enable the module. This enable foliage to distract distant bots.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 100
			}
		}));
		EnabledBushRatting = Config.Bind<bool>(text, "Enable Bush Ratting", true, new ConfigDescription("Enable the module. This enable foliage to distract distant bots.", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 100
			}
		}));
		FoliageImpactScale = Config.Bind<float>(text, "Foliage Impact Scale", 1f, new ConfigDescription("Scale the strength of extra chance to be overlooked by faraway bots from sneaking around foliages.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 1f), new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 99
			}
		}));
		text = "5. Tweaks";
		EnableMovementImpact = Config.Bind<bool>(text, "Movement Impact", true, "Should sprinting bots spot player slower & Should moving (esp. sprinting) player get spotted slightly faster. This option is provided because SAIN is including similiar (player side only) feature (though their effectiveness is unknown yet.");
		EnableSimFreeLook = Config.Bind<bool>(text, "Bot Simulated Free Look", true, "Should bots randomly focus on certain angles in front, just like how human player eyeballing around. This allows bots vision reaction slower or faster depends on how close you are relative to the direction they are focusing on.");
		EnableBodyPartsRecognition = Config.Bind<bool>(text, "Body Parts Recognition", true, "Should bots fail to recogninze the player when the player expose fewer body parts. The fewer parts seen the greater the chance bots fail to react.");
		EnableNearestBotSteering = Config.Bind<bool>(text, "Nearest Bot Steering", true, "Should nearest bot get told to look side way or to the player when they are very close. This is meant to make bots less vulnerable around corners or moving straight forward.");
		EnableExtraFlashLightReaction = Config.Bind<bool>(text, "Extra Flashlight Reaction", true, "Should bots react faster when they can see the player's light and it's shining direct against them, or randomly react faster when they can see the player's light shining towards somewhere else.");
		FinalImpactScaleDelaying = Config.Bind<float>(text, "Final Impact Scale (Slower)", 1f, new ConfigDescription("Scale how much slower bots react because of the mod. 0% = use the original value. *Carefully* adjust this to balance your game to your liking.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 1f), new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 98
			}
		}));
		FinalImpactScaleFastening = Config.Bind<float>(text, "Final Impact Scale (Faster)", 1f, new ConfigDescription("Scale how much faster bots react because of the mod. 0% = use the original value. *Carefully* adjust this to balance your game to your liking.", (AcceptableValueBase)(object)new AcceptableValueRange<float>(0f, 1f), new object[1]
		{
			new ConfigurationManagerAttributes
			{
				Order = 97
			}
		}));
		FinalOffset = Config.Bind<float>(text, "Final Offset", 0f, "(Not recommanded because it's easy to mess up the balance, try Final Impact Scale first) Modify the final 'time to be seen' seconds. Positive means AIs react slower and vice versa. Applied after Final Impact Scale.");
		AlternativeReactionFluctuation = Config.Bind<bool>(text, "Alternative Reaction Fluctuation", true, "If Brightness module is disabled, introduce a slight fluctuation to bot reaction time, so rarely you may get lucky or unlucky, may be not noticeable.");
		text = "6. Info";
		ScoreInfo = Config.Bind<bool>(text, "Lighting Info", true, new ConfigDescription("Display lighting meter.", (AcceptableValueBase)null, Array.Empty<object>()));
		WeatherInfo = Config.Bind<bool>(text, "Weather Info", true, new ConfigDescription("Clear/Cloudy indicator.", (AcceptableValueBase)null, Array.Empty<object>()));
		EquipmentInfo = Config.Bind<bool>(text, "Equipment Info", true, new ConfigDescription("Enabled lights/lasers indicator.", (AcceptableValueBase)null, Array.Empty<object>()));
		FoliageInfo = Config.Bind<bool>(text, "Foliage Info", true, new ConfigDescription("A rough rating of surrounding foliage.", (AcceptableValueBase)null, Array.Empty<object>()));
		TerrainInfo = Config.Bind<bool>(text, "Terrain Info", true, new ConfigDescription("A hint about surrounding grasses. Only grasses in direction to the bot doing vision check is applied and there are some more dynamic factors, so this only gives you the rough idea about how dense the surrounding grasses are.", (AcceptableValueBase)null, Array.Empty<object>()));
		HideMapTip = Config.Bind<bool>(text, "Hide Map Tip", false, new ConfigDescription("Hide the reminder about disabled Brightness module.", (AcceptableValueBase)null, Array.Empty<object>()));
		InfoOffset = Config.Bind<int>(text, "InfoOffset", 0, new ConfigDescription("Vertical offset to the top.", (AcceptableValueBase)(object)new AcceptableValueRange<int>(0, 7), Array.Empty<object>()));
		InfoFontSizeOverride = Config.Bind<int>(text, "Info Font Size Override", 0, new ConfigDescription("Change font size", (AcceptableValueBase)(object)new AcceptableValueRange<int>(0, 32), Array.Empty<object>()));
		text = "7. Performance";
		ResLevel = Config.Bind<int>(text, "Resolution Level", 2, new ConfigDescription("Resolution of the observed image by the observer camera, higher level means somewhat higher accuracy. Has an impact on CPU time. Level1 -> 32x32, Level2 -> 64x64... This config is used on raid start.", (AcceptableValueBase)(object)new AcceptableValueRange<int>(1, 4), Array.Empty<object>()));
		FoliageSamples = Config.Bind<int>(text, "Foliage Samples", 1, new ConfigDescription("How many foliage to check if it's inbetween you and bots, increasing this allows nearby foliage affects multiple bots from different directions. Could slightly an impact on CPU time. This config is used on raid start.", (AcceptableValueBase)(object)new AcceptableValueRange<int>(1, 5), Array.Empty<object>()));
		text = "8. Debug";
		DebugInfo = Config.Bind<bool>(text, "Debug Info (Expensive)", false, "A lot of gibberish.");
		DebugTexture = Config.Bind<bool>(text, "Debug Texture", false, new ConfigDescription("", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				IsAdvanced = true
			}
		}));
		EnableHideout = Config.Bind<bool>(text, "Hideout", false, "Enable darkness/brightness on the map.");
		EnableBenchmark = Config.Bind<bool>(text, "Benchmark", false, "");
		DebugTerrain = Config.Bind<bool>(text, "Debug Terrain", false, new ConfigDescription("", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				IsAdvanced = true
			}
		}));
		DebugCompat = Config.Bind<bool>(text, "Debug Compat", false, new ConfigDescription("", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				IsAdvanced = true
			}
		}));
		DebugProxy = Config.Bind<bool>(text, "Debug Proxy", false, new ConfigDescription("", (AcceptableValueBase)null, new object[1]
		{
			new ConfigurationManagerAttributes
			{
				IsAdvanced = true
			}
		}));
		text = "9. Balance";
		IncludeBosses = Config.Bind<bool>(text, "Include Bosses", false, "Should all features from this mod work for boss. Makes bosses EASY.");
		PMCOnlyMode = Config.Bind<bool>(text, "PMC Only Mode", false, "Requested. So the mod only affect PMCs.");
		EnableEquipmentCheck = Config.Bind<bool>(text, "Equipment Check", true, "Whether the mod checks your equipments. Disabling this stops lights/lasers detection and makes stealth EASY.");
		InterruptSAINNoBush = Config.Bind<bool>(text, "Interrupt SAIN No Bush", false, "DO NOT COMPLAIN ABOUT NO BUSH ESP TO Solarint IF YOU HAVE THIS ON. New SAIN No Bush is designed to be very aggressive, it can block bot vision even if you are just 2m away and the bot is looking straight at you. This add a chance to turn off SAIN's No Bush ESP at close range so things makes sense.");
		ForceBlindFireScatter = Config.Bind<bool>(text, "Force Blind Fire Scatter", true, "Force a random scatter on bot blind fireing, scaled by distance.");
		BotLookDirectionTweaks = Config.Bind<bool>(text, "Bot Look Direction Tweaks", true, "Try to tell the nearest bot to look towards the player when it makes sense.");
	}

	private void Patches()
	{
		((ModulePatch)new SeenCoefPatch()).Enable();
		((ModulePatch)new EncounteringPatch()).Enable();
		((ModulePatch)new ExtraVisibleDistancePatch()).Enable();
		((ModulePatch)new InitiateShotMonitor()).Enable();
		((ModulePatch)new ClientInitiateShotMonitor()).Enable();
		((ModulePatch)new BlindFirePatch()).Enable();
		if (SAINLoaded)
		{
			try
			{
				((ModulePatch)new SAINNoBushOverride()).Enable();
			}
			catch (Exception ex)
			{
				Logger.LogWarning((object)("[That's Lit] Failed to enable SAINNoBushOverride: " + ex.Message + ". SAIN NoBushESP interruption will be disabled. This is not fatal -- other features remain functional."));
			}
		}
	}

	private void Update()
	{
		GameWorldHandler.Update();
	}
}
