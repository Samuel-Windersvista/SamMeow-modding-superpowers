using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace DrakiaXYZ.VersionChecker;

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
public class VersionChecker : Attribute
{
	internal sealed class ConfigurationManagerAttributes
	{
		public delegate void CustomHotkeyDrawerFunc(ConfigEntryBase setting, ref bool isCurrentlyAcceptingInput);

		public bool? ShowRangeAsPercent;

		public Action<ConfigEntryBase> CustomDrawer;

		public CustomHotkeyDrawerFunc CustomHotkeyDrawer;

		public bool? Browsable;

		public string Category;

		public object DefaultValue;

		public bool? HideDefaultButton;

		public bool? HideSettingName;

		public string Description;

		public string DispName;

		public int? Order;

		public bool? ReadOnly;

		public bool? IsAdvanced;

		public Func<object, string> ObjToStr;

		public Func<string, object> StrToObj;
	}

	private int version;

	public static int BuildVersion => (Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(VersionChecker), inherit: false)?.Cast<VersionChecker>()?.FirstOrDefault()?.version).GetValueOrDefault();

	public VersionChecker()
		: this(0)
	{
	}

	public VersionChecker(int version)
	{
		this.version = version;
	}

	public static bool CheckEftVersion(ManualLogSource Logger, PluginInfo Info, ConfigFile Config = null)
	{
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dc: Expected O, but got Unknown
		int filePrivatePart = FileVersionInfo.GetVersionInfo(BepInEx.Paths.ExecutablePath).FilePrivatePart;
		int buildVersion = BuildVersion;
		if (filePrivatePart != buildVersion)
		{
			string text = $"ERROR: This version of {Info.Metadata.Name} v{Info.Metadata.Version} was built for Tarkov {buildVersion}, but you are running {filePrivatePart}. Please download the correct plugin version.";
			Logger.LogError((object)text);
			if (Config != null)
			{
				Config.Bind<string>("", "TarkovVersion", "", new ConfigDescription(text, (AcceptableValueBase)null, new object[1]
				{
					new ConfigurationManagerAttributes
					{
						CustomDrawer = ErrorLabelDrawer,
						ReadOnly = true,
						HideDefaultButton = true,
						HideSettingName = true,
						Category = null
					}
				}));
			}
			return false;
		}
		return true;
	}

	private static void ErrorLabelDrawer(ConfigEntryBase entry)
	{
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Expected O, but got Unknown
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		GUIStyle val = new GUIStyle(GUI.skin.label);
		val.wordWrap = true;
		val.stretchWidth = true;
		GUIStyle val2 = new GUIStyle(GUI.skin.label);
		val2.stretchWidth = true;
		val2.alignment = (TextAnchor)4;
		val2.normal.textColor = Color.red;
		val2.fontStyle = (FontStyle)1;
		GUILayout.BeginVertical(Array.Empty<GUILayoutOption>());
		GUILayout.Label(entry.Description.Description, val, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
		GUILayout.Label("Plugin has been disabled!", val2, (GUILayoutOption[])(object)new GUILayoutOption[1] { GUILayout.ExpandWidth(true) });
		GUILayout.EndVertical();
	}
}
