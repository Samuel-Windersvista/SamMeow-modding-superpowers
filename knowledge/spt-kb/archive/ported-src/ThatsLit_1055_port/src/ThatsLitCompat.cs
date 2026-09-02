using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BepInEx;
using EFT.Communications;
using Newtonsoft.Json;

namespace ThatsLit;

public static class ThatsLitCompat
{
	[Serializable]
	public class Device
	{
		private DeviceTemplate templateInstance;

		public string id { get; set; }

		public string template { get; set; }

		public DeviceTemplate TemplateInstance
		{
			get
			{
				if (templateInstance == null)
				{
					DeviceTemplates.TryGetValue(template, out templateInstance);
				}
				return templateInstance;
			}
		}
	}

	[Serializable]
	public class DeviceTemplate
	{
		public string name { get; set; }

		public DeviceMode[] modes { get; set; }

		public DeviceMode SafeGetMode(int mode, bool fallbackLast = true)
		{
			if (modes == null || modes.Length == 0)
			{
				return default(DeviceMode);
			}
			if (modes.Length <= mode)
			{
				if (!fallbackLast)
				{
					return default(DeviceMode);
				}
				if (modes.Length == 0)
				{
					return default(DeviceMode);
				}
				mode = modes.Length - 1;
			}
			return modes[mode];
		}
	}

	[Serializable]
	public class Goggle
	{
		private GoggleTemplate templateInstance;

		public string id { get; set; }

		public string template { get; set; }

		public GoggleTemplate TemplateInstance
		{
			get
			{
				if (templateInstance == null)
				{
					GoggleTemplates.TryGetValue(template, out templateInstance);
				}
				return templateInstance;
			}
		}
	}

	[Serializable]
	public class GoggleTemplate
	{
		public string name { get; set; }

		public NightVision nightVision { get; set; }

		public Thermal thermal { get; set; }
	}

	[Serializable]
	public struct DeviceMode
	{
		public float light { get; set; }

		public float laser { get; set; }

		public float irLight { get; set; }

		public float irLaser { get; set; }

		public static DeviceMode MergeMax(DeviceMode a, DeviceMode b)
		{
			a.light = ((a.light > b.light) ? a.light : b.light);
			a.laser = ((a.laser > b.laser) ? a.laser : b.laser);
			a.irLight = ((a.irLight > b.irLight) ? a.irLight : b.irLight);
			a.irLaser = ((a.irLaser > b.irLaser) ? a.irLaser : b.irLaser);
			return a;
		}
	}

	[Serializable]
	public class NightVision
	{
		public int horizontalFOV { get; set; }

		public int verticalFOV { get; set; }

		public float nullification { get; set; }

		public float nullificationDarker { get; set; }

		public float nullificationExtremeDark { get; set; }
	}

	[Serializable]
	public class CompatFile : IComparable<CompatFile>
	{
		public int protocol { get; set; }

		public string FilePath { get; internal set; }

		public int priority { get; set; } = 999;

		public ScopeTemplate[] scopeTemplates { get; set; }

		public Scope[] scopes { get; set; }

		public GoggleTemplate[] goggleTemplates { get; set; }

		public Goggle[] goggles { get; set; }

		public DeviceTemplate[] deviceTemplates { get; set; }

		public Device[] devices { get; set; }

		public Device[] extraDevices { get; set; }

		public Dictionary<string, BushDef> bushDefs { get; set; }

		public int CompareTo(CompatFile other)
		{
			return priority - other.priority;
		}
	}

	[Serializable]
	public class Scope
	{
		private ScopeTemplate templateInstance;

		public string id { get; set; }

		public string template { get; set; }

		public ScopeTemplate TemplateInstance
		{
			get
			{
				if (templateInstance == null)
				{
					ScopeTemplates.TryGetValue(template, out templateInstance);
				}
				return templateInstance;
			}
		}
	}

	[Serializable]
	public class ScopeTemplate
	{
		public string name { get; set; }

		public NightVision nightVision { get; set; }

		public Thermal thermal { get; set; }
	}

	[Serializable]
	public class Thermal
	{
		public int effectiveDistance { get; set; }

		public int horizontalFOV { get; set; }

		public int verticalFOV { get; set; }
	}

	[Serializable]
	public class BushDef
	{
		public double af { get; set; }

		public double fdMin { get; set; }

		public double fdMax { get; set; }

		public double edMin { get; set; }

		public double edMax { get; set; }

		public bool edClamp { get; set; }

		public bool edYdScale { get; set; }

		public double psMin { get; set; }

		public double psMax { get; set; }

		public double psProne { get; set; }

		public double psStand { get; set; }

		public double psStandMin { get; set; }

		public double psStandMax { get; set; }

		public double psStandScale { get; set; }

		public bool psCrouch { get; set; }

		public bool psProneOnly { get; set; }

		public double psMid { get; set; }

		public double psMidScale { get; set; }

		public double yd { get; set; }

		public bool ydUse { get; set; }

		public string _ { get; set; }
	}

	public const int MIN_COMPATIBLE_PROTOCOL = 1;

	private static Task running;

	public static Dictionary<string, ScopeTemplate> ScopeTemplates { get; private set; }

	public static Dictionary<string, Scope> Scopes { get; private set; }

	public static Dictionary<string, GoggleTemplate> GoggleTemplates { get; private set; }

	public static Dictionary<string, Goggle> Goggles { get; private set; }

	public static Dictionary<string, DeviceTemplate> DeviceTemplates { get; private set; }

	public static Dictionary<string, Device> Devices { get; private set; }

	public static Dictionary<string, Device> ExtraDevices { get; private set; }

	public static Dictionary<string, BushDef> BushDefs { get; private set; }

	static ThatsLitCompat()
	{
		ScopeTemplates = new Dictionary<string, ScopeTemplate>();
		Scopes = new Dictionary<string, Scope>();
		GoggleTemplates = new Dictionary<string, GoggleTemplate>();
		Goggles = new Dictionary<string, Goggle>();
		DeviceTemplates = new Dictionary<string, DeviceTemplate>();
		Devices = new Dictionary<string, Device>();
		ExtraDevices = new Dictionary<string, Device>();
		BushDefs = new Dictionary<string, BushDef>();
	}

	public static Task LoadCompatFiles()
	{
		if (running != null && !running.IsCompleted)
		{
			return running;
		}
		ScopeTemplates.Clear();
		Scopes.Clear();
		GoggleTemplates.Clear();
		Goggles.Clear();
		DeviceTemplates.Clear();
		Devices.Clear();
		ExtraDevices.Clear();
		BushDefs.Clear();
		running = Task.Run(delegate
		{
			List<CompatFile> list = new List<CompatFile>();
			foreach (string item in Directory.EnumerateFiles(BepInEx.Paths.PluginPath, "**thatslitcompat.json", SearchOption.AllDirectories))
			{
				CompatFile compatFile = JsonConvert.DeserializeObject<CompatFile>(File.ReadAllText(item));
				if (compatFile == null)
				{
					string text = $"[That's Lit] Invalid thatslitcompat file: {compatFile}";
					NotificationManager.DisplayWarningNotification(text, (ENotificationDurationType)0);
					Logger.LogError(text);
				}
				else if (compatFile.protocol < 1)
				{
					string text2 = $"[That's Lit] Incompatible outdated thatslitcompat file: {compatFile}, minimum compatible protocol: {1}";
					NotificationManager.DisplayWarningNotification(text2, (ENotificationDurationType)0);
					Logger.LogError(text2);
				}
				else
				{
					compatFile.FilePath = item;
					list.Add(compatFile);
				}
			}
			list.Sort();
			foreach (CompatFile item2 in list)
			{
				if (ThatsLitPlugin.DebugCompat.Value)
				{
					Logger.LogWarning("[That's Lit Debug] Loading compat file: " + item2.FilePath);
				}
				if (item2.scopeTemplates != null)
				{
					ScopeTemplate[] scopeTemplates = item2.scopeTemplates;
					foreach (ScopeTemplate scopeTemplate in scopeTemplates)
					{
						ScopeTemplates[scopeTemplate.name] = scopeTemplate;
						if (ThatsLitPlugin.DebugCompat.Value)
						{
							Logger.LogWarning("[That's Lit Debug] Scope Template: " + scopeTemplate.name);
						}
					}
				}
				if (item2.goggleTemplates != null)
				{
					GoggleTemplate[] goggleTemplates = item2.goggleTemplates;
					foreach (GoggleTemplate goggleTemplate in goggleTemplates)
					{
						GoggleTemplates[goggleTemplate.name] = goggleTemplate;
						if (ThatsLitPlugin.DebugCompat.Value)
						{
							Logger.LogWarning("[That's Lit Debug] Goggle Template: " + goggleTemplate.name);
						}
					}
				}
				if (item2.deviceTemplates != null)
				{
					DeviceTemplate[] deviceTemplates = item2.deviceTemplates;
					foreach (DeviceTemplate deviceTemplate in deviceTemplates)
					{
						if (deviceTemplate.modes == null || deviceTemplate.modes.Length == 0)
						{
							Logger.LogError("[That's Lit] Device Template: " + deviceTemplate.name + " have 0 mode defined");
						}
						DeviceTemplates[deviceTemplate.name] = deviceTemplate;
						if (ThatsLitPlugin.DebugCompat.Value)
						{
							Logger.LogWarning("[That's Lit Debug] Device Template: " + deviceTemplate.name);
						}
					}
				}
				if (item2.scopes != null)
				{
					Scope[] scopes = item2.scopes;
					foreach (Scope scope in scopes)
					{
						Scopes[scope.id] = scope;
						if (ThatsLitPlugin.DebugCompat.Value)
						{
							Logger.LogWarning("[That's Lit Debug] Scope: " + scope.id);
						}
					}
				}
				if (item2.goggles != null)
				{
					Goggle[] goggles = item2.goggles;
					foreach (Goggle goggle in goggles)
					{
						Goggles[goggle.id] = goggle;
						if (ThatsLitPlugin.DebugCompat.Value)
						{
							Logger.LogWarning("[That's Lit Debug] Goggle: " + goggle.id);
						}
					}
				}
				if (item2.devices != null)
				{
					Device[] devices = item2.devices;
					foreach (Device device in devices)
					{
						if (device.TemplateInstance == null)
						{
							Logger.LogError("[That's Lit] Device: " + device.id + " template is invalid: " + device.template);
						}
						else if (device.TemplateInstance.modes == null || device.TemplateInstance.modes.Length == 0)
						{
							Logger.LogError("[That's Lit] Device: " + device.id + " have 0 mode defined");
						}
						else
						{
							Devices[device.id] = device;
							if (ThatsLitPlugin.DebugCompat.Value)
							{
								Logger.LogWarning("[That's Lit Debug] Device: " + device.id);
							}
						}
					}
				}
				if (item2.extraDevices != null)
				{
					Device[] devices = item2.extraDevices;
					foreach (Device device2 in devices)
					{
						if (device2.TemplateInstance == null)
						{
							Logger.LogError("[That's Lit] Device: " + device2.id + " template is invalid: " + device2.template);
						}
						else if (device2.TemplateInstance.modes == null || device2.TemplateInstance.modes.Length == 0)
						{
							Logger.LogError("[That's Lit] Device: " + device2.id + " have 0 mode defined");
						}
						else
						{
							ExtraDevices[device2.id] = device2;
							if (ThatsLitPlugin.DebugCompat.Value)
							{
								Logger.LogWarning("[That's Lit Debug] Extra Device: " + device2.id);
							}
						}
					}
				}
				if (item2.bushDefs != null)
				{
					foreach (KeyValuePair<string, BushDef> bushDef in item2.bushDefs)
					{
						BushDefs[bushDef.Key] = bushDef.Value;
						if (ThatsLitPlugin.DebugCompat.Value)
						{
							Logger.LogWarning("[That's Lit Debug] Bush Def: " + bushDef.Key);
						}
					}
				}
			}
		}).ContinueWith(delegate(Task t)
		{
			running = null;
			if (t.IsFaulted)
			{
				Logger.LogError(t.Exception.Flatten());
			}
		});
		return running;
	}
}
