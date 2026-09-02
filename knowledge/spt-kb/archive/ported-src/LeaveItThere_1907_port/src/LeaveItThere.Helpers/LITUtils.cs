using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using Comfort.Common;
using EFT;
using LeaveItThere.Components;
using Newtonsoft.Json;
using SPT.Common.Http;
using UnityEngine;

namespace LeaveItThere.Helpers;

public class LITUtils
{
	public static string AssemblyPath { get; private set; } = Assembly.GetExecutingAssembly().Location;

	public static string AssemblyFolderPath { get; private set; } = Path.GetDirectoryName(AssemblyPath);

	public static Vector3 PlayerFront
	{
		get
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			Player player = LITSession.Instance.Player;
			return player.Transform.position + player.Transform.forward + player.Transform.up / 2f;
		}
	}

	public static string GetCardinalDirection(Vector3 from, Vector3 to)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = to - from;
		val.y = 0f;
		val.Normalize();
		float num = Mathf.Atan2(val.z, val.x) * 57.29578f;
		if (num < 0f)
		{
			num += 360f;
		}
		string locationId = Singleton<GameWorld>.Instance.LocationId;
		if (locationId == "factory4_day" || locationId == "factory4_night")
		{
			if ((double)num >= 337.5 || (double)num < 22.5)
			{
				return "South";
			}
			if ((double)num >= 22.5 && (double)num < 67.5)
			{
				return "South East";
			}
			if ((double)num >= 67.5 && (double)num < 112.5)
			{
				return "East";
			}
			if ((double)num >= 112.5 && (double)num < 157.5)
			{
				return "North East";
			}
			if ((double)num >= 157.5 && (double)num < 202.5)
			{
				return "North";
			}
			if ((double)num >= 202.5 && (double)num < 247.5)
			{
				return "North West";
			}
			if ((double)num >= 247.5 && (double)num < 292.5)
			{
				return "West";
			}
			if ((double)num >= 292.5 && (double)num < 337.5)
			{
				return "South West";
			}
		}
		else
		{
			if ((double)num >= 337.5 || (double)num < 22.5)
			{
				return "East";
			}
			if ((double)num >= 22.5 && (double)num < 67.5)
			{
				return "North East";
			}
			if ((double)num >= 67.5 && (double)num < 112.5)
			{
				return "North";
			}
			if ((double)num >= 112.5 && (double)num < 157.5)
			{
				return "North West";
			}
			if ((double)num >= 157.5 && (double)num < 202.5)
			{
				return "West";
			}
			if ((double)num >= 202.5 && (double)num < 247.5)
			{
				return "South West";
			}
			if ((double)num >= 247.5 && (double)num < 292.5)
			{
				return "South";
			}
			if ((double)num >= 292.5 && (double)num < 337.5)
			{
				return "South East";
			}
		}
		return "this shouldn't ever be reached";
	}

	public static void ExecuteAfterSeconds(float seconds, Action<object> callback, object arg = null)
	{
		StaticManager.BeginCoroutine(ExecuteAfterSecondsRoutine(seconds, callback, arg));
	}

	public static IEnumerator ExecuteAfterSecondsRoutine(float seconds, Action<object> callback, object arg)
	{
		yield return (object)new WaitForSeconds(seconds);
		callback(arg);
	}

	public static void ExecuteNextFrame(Action<object> callback, object arg = null)
	{
		StaticManager.BeginCoroutine(ExecuteNextFrameRoutine(callback, arg));
	}

	public static IEnumerator ExecuteNextFrameRoutine(Action<object> callback, object arg)
	{
		yield return null;
		callback(arg);
	}

	public static Quaternion ScaleQuaternion(Quaternion rotation, float scale)
	{
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = default(Vector3);
		float num = default(float);
		rotation.ToAngleAxis(out num, out val);
		num *= scale;
		return Quaternion.AngleAxis(num, val);
	}

	public static void ForAllDescendants(GameObject parent, Action<GameObject> action)
	{
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Expected O, but got Unknown
		foreach (Transform item in parent.transform)
		{
			Transform val = item;
			action(((Component)val).gameObject);
			ForAllDescendants(((Component)val).gameObject, action);
		}
	}

	public static List<GameObject> GetAllDescendants(GameObject parent)
	{
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Expected O, but got Unknown
		List<GameObject> list = new List<GameObject>();
		foreach (Transform item in parent.transform)
		{
			Transform val = item;
			list.Add(((Component)val).gameObject);
			list.AddRange(GetAllDescendants(((Component)val).gameObject));
		}
		return list;
	}

	public static T ServerRoute<T>(string url, T data = default(T))
	{
		string text = JsonConvert.SerializeObject((object)data);
		return JsonConvert.DeserializeObject<T>(RequestHandler.PostJson(url, text));
	}

	public static void ServerRouteAsync<T>(string url, T data)
	{
		ThreadPool.QueueUserWorkItem(delegate
		{
			try
			{
				string text = JsonConvert.SerializeObject((object)data);
				RequestHandler.PostJson(url, text);
			}
			catch (Exception ex)
			{
				Plugin.LogSource.LogError((object)("ServerRouteAsync failed: " + ex.Message));
			}
		});
	}
}
