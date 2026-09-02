// ROLogger: SPT 4.0 -> 4.1.2 迁移
// 4.1.2 ISptLogger<T> 位于 SPTarkov.Common.Models.Logging；
// LogWithColor 颜色参数由旧 LogTextColor 枚举改为 Spectre.Console.Color；
// IsLogEnabled 参数由旧 LogLevel 改为 Microsoft.Extensions.Logging.LogLevel。
// 为保持全部调用点不变，本 mod 自定义 LogTextColor 枚举（沿用 EFT 控制台 ANSI 色码），
// 并在 Log/LogToServer 中映射为 Spectre 颜色。
using System;
using Microsoft.Extensions.Logging;
using RaidOverhaulMain.Models;
using SPTarkov.Common.Models.Logging;
using Spectre.Console;

namespace RaidOverhaulMain.Helpers;

public enum LogTextColor
{
	Black = 30,
	Red = 31,
	Green = 32,
	Yellow = 33,
	Blue = 34,
	Magenta = 35,
	Cyan = 36,
	White = 37,
	Gray = 38,
	Default = 39
}

public static class ROLogger
{
	private const string LogPrefix = "[Raid Overhaul] ";

	public static void Log<T>(ISptLogger<T> logger, string message, LogTextColor textColor = LogTextColor.White)
	{
		logger.LogWithColor(LogPrefix + message, ToColor(textColor), (Color?)null, (Exception?)null);
	}

	public static void LogDebug<T>(ISptLogger<T> logger, string message)
	{
		if (logger.IsLogEnabled(LogLevel.Debug))
		{
			logger.Debug(LogPrefix + message, (Exception?)null);
		}
	}

	public static void LogInfo<T>(ISptLogger<T> logger, string message)
	{
		if (logger.IsLogEnabled(LogLevel.Information))
		{
			logger.Info(LogPrefix + message, (Exception?)null);
		}
	}

	public static void LogWarning<T>(ISptLogger<T> logger, string message)
	{
		if (logger.IsLogEnabled(LogLevel.Warning))
		{
			logger.Warning(LogPrefix + message, (Exception?)null);
		}
	}

	public static void LogError<T>(ISptLogger<T> logger, string message)
	{
		if (logger.IsLogEnabled(LogLevel.Error))
		{
			logger.Error(LogPrefix + message, (Exception?)null);
		}
	}

	public static void LogToServer<T>(ISptLogger<T> logger, string message, LogTextColor textColor = LogTextColor.White)
	{
		logger.LogWithColor(LogPrefix + message, ToColor(textColor), (Color?)null, (Exception?)null);
	}

	private static Color ToColor(LogTextColor textColor)
	{
		return textColor switch
		{
			LogTextColor.Black => Color.Black,
			LogTextColor.Red => Color.Red,
			LogTextColor.Green => Color.Green,
			LogTextColor.Yellow => Color.Yellow,
			LogTextColor.Blue => Color.Blue,
			LogTextColor.Magenta => Color.Magenta,
			LogTextColor.Cyan => Color.Cyan,
			LogTextColor.Gray => Color.Grey,
			_ => Color.White
		};
	}
}
