using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using BepInEx.Logging;

namespace ThatsLit;

internal static class Logger
{
	public static Dictionary<string, ManualLogSource> LogSourcesDictionary = new Dictionary<string, ManualLogSource>();

	public static object LastLogData { get; private set; }

	public static void LogInfo(object data)
	{
		Log((LogLevel)16, data);
	}

	public static void LogDebug(object data)
	{
		Log((LogLevel)32, data);
	}

	public static void LogWarning(object data)
	{
		Log((LogLevel)4, data);
	}

	public static void LogError(object data)
	{
		Log((LogLevel)2, data);
	}

	private static void Log(LogLevel level, object data)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Invalid comparison between Unknown and I4
		//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		string text = string.Empty;
		Type type = null;
		if ((int)level != 32)
		{
			int maxFrames = GetMaxFrames(level);
			int num = 0;
			StackTrace stackTrace = new StackTrace();
			for (int i = 0; i < stackTrace.FrameCount; i++)
			{
				MethodBase method = stackTrace.GetFrame(i).GetMethod();
				Type declaringType = method.DeclaringType;
				if (!(declaringType == typeof(Logger)))
				{
					type = type ?? declaringType;
					text = text + "." + method.Name;
					if (num >= maxFrames)
					{
						break;
					}
					num++;
				}
			}
			text = "[" + text + "]:";
		}
		LastLogData = data;
		string text2 = $"{text} [{data}]";
		SelectLogSource(type).Log(level, (object)text2);
	}

	private static int GetMaxFrames(LogLevel level)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected I4, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Invalid comparison between Unknown and I4
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		switch ((int)(level - 1))
		{
		default:
			if ((int)level != 16)
			{
				_ = 32;
				return 0;
			}
			return 1;
		case 3:
			return 2;
		case 1:
			return 3;
		case 0:
			return 4;
		case 2:
			return 0;
		}
	}

	private static ManualLogSource SelectLogSource(Type type = null)
	{
		string text = type?.Name ?? "SAIN";
		if (!LogSourcesDictionary.ContainsKey(text))
		{
			LogSourcesDictionary.Add(text, BepInEx.Logging.Logger.CreateLogSource(text));
		}
		return LogSourcesDictionary[text];
	}
}
