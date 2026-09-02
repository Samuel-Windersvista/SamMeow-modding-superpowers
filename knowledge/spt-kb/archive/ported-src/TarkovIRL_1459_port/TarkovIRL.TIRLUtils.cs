using BepInEx.Logging;
using UnityEngine;

namespace TarkovIRL;

public static class TIRLUtils
{
	public enum E_DEBUG_PRIORITY
	{
		LOG,
		SPAM_LOG,
		ALWAYS_LOG
	}

	public static ManualLogSource Logger;

	private static float _dt = 0f;

	private static float _spamTimer = 0f;

	private static float _spamTimeResolution = 2f;

	public static void LogError(string toPrint)
	{
		Logger.LogError(toPrint);
	}

	public static void Log(string toPrint, bool spam)
	{
		if (!PrimeMover.IsLogging.Value)
		{
			return;
		}
		if (spam)
		{
			Logger.LogInfo(toPrint);
			return;
		}
		_spamTimer += _dt;
		if (_spamTimer > PrimeMover.LoggingUpdateFrequency.Value)
		{
			_spamTimer = 0f;
			Logger.LogInfo(toPrint);
		}
	}

	public static void LogPriority(E_DEBUG_PRIORITY priority, string toPrint)
	{
		switch (priority)
		{
		case E_DEBUG_PRIORITY.SPAM_LOG:
			if (PrimeMover.IsLogging.Value && PrimeMover.DebugSpam.Value)
			{
				Logger.LogError(toPrint);
			}
			break;
		case E_DEBUG_PRIORITY.LOG:
			if (PrimeMover.IsLogging.Value && !PrimeMover.DebugSpam.Value)
			{
				Logger.LogError(toPrint);
			}
			break;
		case E_DEBUG_PRIORITY.ALWAYS_LOG:
			Logger.LogError(toPrint);
			break;
		}
	}

	public static void Update(float dt)
	{
		_dt = dt;
	}

	public static float FulfilledLerp(float value, float target, float step)
	{
		value += step;
		if (value > target)
		{
			value = target;
		}
		return value;
	}

	public static Quaternion GetQuatFromV3(Vector3 v)
	{
		Quaternion identity = Quaternion.identity;
		identity.x = v.x;
		identity.y = v.y;
		identity.z = v.z;
		return identity;
	}
}
