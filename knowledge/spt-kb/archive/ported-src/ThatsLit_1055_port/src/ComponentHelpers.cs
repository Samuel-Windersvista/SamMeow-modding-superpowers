using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ThatsLit.Helpers;

internal class ComponentHelpers
{
	private static readonly List<string> NoDisposeMethods = new List<string>();

	private static readonly Dictionary<Type, MethodInfo> DisposeMethods = new Dictionary<Type, MethodInfo>();

	public static T AddOrDestroyComponent<T, K>(T component, K condition) where T : Component where K : Component
	{
		if ((Object)(object)component == (Object)null && (Object)(object)condition != (Object)null)
		{
			component = GetOrAddComponent<T, K>(condition);
		}
		else if ((Object)(object)component != (Object)null && (Object)(object)condition == (Object)null)
		{
			DestroyComponent(component);
		}
		return component;
	}

	public static void DestroyComponent<T>(T component) where T : Component
	{
		if ((Object)(object)component == (Object)null)
		{
			LogDebug("Component is already null, no need to dispose");
			return;
		}
		try
		{
			if (TryGetDisposeMethod<T>(out var methodInfo))
			{
				methodInfo.Invoke(component, null);
			}
		}
		catch (Exception ex)
		{
			LogError(ex, "Dispose");
		}
		if (!((Object)(object)component != (Object)null))
		{
			return;
		}
		try
		{
			Object.Destroy((Object)(object)component);
		}
		catch (Exception ex2)
		{
			LogError(ex2, "Destroy");
		}
	}

	private static bool TryGetDisposeMethod<T>(out MethodInfo methodInfo)
	{
		methodInfo = null;
		Type typeFromHandle = typeof(T);
		if (!NoDisposeMethods.Contains(typeFromHandle.Name) && !DisposeMethods.ContainsKey(typeFromHandle))
		{
			methodInfo = typeFromHandle.GetMethod("Dispose");
			if (methodInfo != null)
			{
				LogDebug("Caching Dispose Method " + typeFromHandle.Name);
				DisposeMethods.Add(typeFromHandle, methodInfo);
			}
			else
			{
				NoDisposeMethods.Add(typeFromHandle.Name);
			}
		}
		else if (DisposeMethods.ContainsKey(typeFromHandle))
		{
			methodInfo = DisposeMethods[typeFromHandle];
		}
		return methodInfo != null;
	}

	private static void LogError(Exception ex, string message)
	{
		Logger.LogError(message + " Error");
		Logger.LogError(ex);
	}

	private static void LogDebug(string message)
	{
		Logger.LogDebug(message);
	}

	public static T GetOrAddComponent<T, K>(K original) where T : Component where K : Component
	{
		return ((Component)original).GetComponent<T>() ?? ((Component)original).gameObject.AddComponent<T>();
	}
}
