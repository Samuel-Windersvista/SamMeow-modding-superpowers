using System;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using ImmersiveDaylightCycle.Common;
using Newtonsoft.Json;
using SPT.Common.Http;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Jehree.ImmersiveDaylightCycle.Helpers;

internal class ModUtils
{
	public static string TimeRequestURL = "/jehree/idc/request_time";

	public static string HostRaidStartedURL = "/jehree/idc/host_raid_started";

	public static string ClientLeftRaidURL = "/jehree/idc/client_exited";

	public static string ConsoleCommandURL = "/jehree/idc/console_command";

	public static bool IsDayTime(DateTime dateTime)
	{
		if (dateTime.Hour > 5 && dateTime.Hour < 21)
		{
			return true;
		}
		return false;
	}

	public static void DisableTimeUI(TextMeshProUGUI phaseToDisable, Toggle timeToggle)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			timeToggle.isOn = false;
			((Behaviour)timeToggle).enabled = false;
			((Component)((TMP_Text)phaseToDisable).transform).gameObject.SetActive(false);
			GameObject gameObject = ((Component)((Component)timeToggle).transform.Find("Background")).gameObject;
			((Graphic)gameObject.GetComponent<Image>()).color = Color.clear;
			((Graphic)((Component)gameObject.transform.Find("Checkmark")).gameObject.GetComponent<Image>()).color = Color.clear;
		}
		catch (Exception)
		{
		}
	}

	public static void EnableTimeUI(TextMeshProUGUI phaseToEnable, Toggle timeToggle, string enabledText, bool chooseThisTime = true)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			((Behaviour)timeToggle).enabled = true;
			if (chooseThisTime)
			{
				timeToggle.isOn = true;
			}
			((Component)((TMP_Text)phaseToEnable).transform).gameObject.SetActive(true);
			GameObject gameObject = ((Component)((Component)timeToggle).transform.Find("Background")).gameObject;
			((Graphic)gameObject.GetComponent<Image>()).color = Color.white;
			((Graphic)((Component)gameObject.transform.Find("Checkmark")).gameObject.GetComponent<Image>()).color = Color.white;
			((TMP_Text)phaseToEnable).text = enabledText;
		}
		catch (Exception)
		{
		}
	}

	public static DateTime GetCurrentTime()
	{
		IDCTime iDCTime = ServerRoute<IDCTime>(TimeRequestURL);
		return new DateTime(2024, 6, 8, iDCTime.Hour, iDCTime.Minute, iDCTime.Second);
	}

	public static void SetRaidTime()
	{
		if (!Singleton<GameWorld>.Instantiated)
		{
			throw new Exception("ModUtils.SetRaidTime was called when the GameWorld instance was not yet instantiated!");
		}
		IDCTime iDCTime = ServerRoute<IDCTime>(TimeRequestURL);
		DateTime dateTime = new DateTime(2024, 6, 8, iDCTime.Hour, iDCTime.Minute, iDCTime.Second);
		Singleton<GameWorld>.Instance.GameDateTime.Reset(DateTime.Now, dateTime, (float)iDCTime.CycleRate);
	}

	public static T ServerRoute<T>(string url, object data = null)
	{
		string text = JsonConvert.SerializeObject(data);
		string text2 = RequestHandler.PostJson(url, text);
		return JsonConvert.DeserializeObject<T>(text2);
	}

	public static string ServerRoute(string url, object data = null)
	{
		string text;
		if (data is string)
		{
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			dictionary.Add("data", (string)data);
			text = JsonConvert.SerializeObject((object)dictionary);
		}
		else
		{
			text = JsonConvert.SerializeObject(data);
		}
		return RequestHandler.PutJson(url, text);
	}
}
