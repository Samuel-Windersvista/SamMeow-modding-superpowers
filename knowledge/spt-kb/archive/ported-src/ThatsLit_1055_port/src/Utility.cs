using System;
using System.Collections.Generic;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InventoryLogic;
using EFT.UI;
using UnityEngine;

namespace ThatsLit;

public static class Utility
{
	private static string lastLogged;

	public static BotImpactType GetBotImpactType(WildSpawnType type)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_00be: Expected I4, but got Unknown
		//IL_0180: Unknown result type (might be due to invalid IL or missing references)
		//IL_0186: Invalid comparison between Unknown and I4
		return (int)type switch
		{
			32 => BotImpactType.BOSS, 
			36 => BotImpactType.BOSS, 
			3 => BotImpactType.BOSS, 
			11 => BotImpactType.BOSS, 
			6 => BotImpactType.BOSS, 
			26 => BotImpactType.BOSS, 
			7 => BotImpactType.BOSS, 
			17 => BotImpactType.BOSS, 
			22 => BotImpactType.BOSS, 
			29 => BotImpactType.BOSS, 
			40 => BotImpactType.BOSS, 
			38 => BotImpactType.BOSS, 
			43 => BotImpactType.BOSS, 
			34 => BotImpactType.FOLLOWER, 
			35 => BotImpactType.FOLLOWER, 
			14 => BotImpactType.FOLLOWER, 
			12 => BotImpactType.FOLLOWER, 
			13 => BotImpactType.FOLLOWER, 
			15 => BotImpactType.FOLLOWER, 
			27 => BotImpactType.FOLLOWER, 
			28 => BotImpactType.FOLLOWER, 
			33 => BotImpactType.FOLLOWER, 
			41 => BotImpactType.FOLLOWER, 
			42 => BotImpactType.FOLLOWER, 
			5 => BotImpactType.FOLLOWER, 
			8 => BotImpactType.FOLLOWER, 
			16 => BotImpactType.FOLLOWER, 
			23 => BotImpactType.FOLLOWER, 
			30 => BotImpactType.FOLLOWER, 
			44 => BotImpactType.FOLLOWER, 
			45 => BotImpactType.FOLLOWER, 
			21 => BotImpactType.BOSS, 
			20 => BotImpactType.FOLLOWER, 
			39 => BotImpactType.BOSS, 
			25 => BotImpactType.BOSS, 
			9 => BotImpactType.FOLLOWER, 
			24 => BotImpactType.FOLLOWER, 
			37 => BotImpactType.FOLLOWER, 
			0 => BotImpactType.FOLLOWER, 
			_ => ((int)type != 199) ? BotImpactType.DEFAULT : BotImpactType.BOSS, 
		};
	}

	public static bool IsExcludedSpawnType(WildSpawnType type)
	{
		return false;
	}

	internal static float GetInGameDayTime()
	{
		if (Singleton<GameWorld>.Instance?.GameDateTime == null)
		{
			return 19f;
		}
		DateTime dateTime = Singleton<GameWorld>.Instance.GameDateTime.Calculate();
		float num = (float)dateTime.Minute / 59f;
		return (float)dateTime.Hour + num;
	}

	internal static float GetNightProgress()
	{
		float num = GetInGameDayTime();
		if (num < 12f)
		{
			num += 4f;
		}
		return Mathf.InverseLerp(0f, 10f, num);
	}

	internal static float GetDayProgress()
	{
		float inGameDayTime = GetInGameDayTime();
		return Mathf.InverseLerp(6f, 20f, inGameDayTime);
	}

	internal static void CalculateDetailScore(string name, int num, out float prone, out float crouch)
	{
		prone = 0f;
		crouch = 0f;
		if (name != null)
		{
			if (num == 0)
			{
				prone = 0f;
				crouch = 0f;
			}
			else if (name.EndsWith("e2eb60"))
			{
				prone = 0.05f * Mathf.Pow(Mathf.Clamp01((float)num / 20f), 2f) * (float)Mathf.Min(num, 50);
				crouch = 0.003f * (float)Mathf.Min(num, 10);
			}
			else if (name.EndsWith("df6e82") || name.EndsWith("7c58e7") || name.EndsWith("994963"))
			{
				prone = 0.05f * Mathf.Pow(Mathf.Clamp01((float)num / 20f), 2f) * (float)Mathf.Min(num, 50);
				crouch = 0.0035f * (float)Mathf.Min(num, 5);
			}
			else if (name.EndsWith("27bbce"))
			{
				prone = 0.008f * (float)Mathf.Min(num, 25);
				crouch = 0f;
			}
			else if (name.EndsWith("fa097b") || name.EndsWith("2adee9"))
			{
				prone = 0.06f * (float)num;
				crouch = 0.009f * (float)Mathf.Min(num, 15);
			}
			else if (name.EndsWith("eb7931"))
			{
				prone = 0.06f * (float)num;
				crouch = 0.01f * (float)num;
			}
			else if (name.EndsWith("adb33a"))
			{
				prone = 0.02f * (float)num;
				crouch = 0.02f * (float)num;
			}
			else if (name.EndsWith("f83e15"))
			{
				prone = 0.04f * (float)num;
				crouch = 0.018f * (float)num;
			}
			else if (name.EndsWith("ead4fa"))
			{
				prone = 0.06f * (float)num;
				crouch = 0.008f * (float)num;
			}
			else if (name.EndsWith("40d9d4"))
			{
				prone = 0.007f * (float)num;
				crouch = 0.009f * (float)num;
			}
			else if (name.EndsWith("4ad690"))
			{
				prone = 0.015f * (float)num;
				crouch = 0.015f * (float)num;
			}
			else if (name.EndsWith("bf0a23"))
			{
				prone = 0.007f * (float)num;
				crouch = 0.007f * (float)num;
			}
			else if (name.EndsWith("b6cf18"))
			{
				prone = 0.01f * (float)num;
				crouch = 0.01f * (float)num;
			}
			else if (name.EndsWith("a84c21"))
			{
				prone = 0.007f * (float)num;
				crouch = 0.006f * (float)num;
			}
			else if (name.EndsWith("d17f80"))
			{
				prone = 0.008f * (float)Mathf.Min(num, 25);
				crouch = 0f;
			}
			else if (!name.EndsWith("d17180") && !name.EndsWith("e84f39") && !name.EndsWith("e9cd39") && ThatsLitPlugin.DebugInfo.Value && Time.frameCount % 47 == 0 && name != lastLogged)
			{
				string text = $"That's Lit: Missing terrain detail: {name}";
				NotificationManager.DisplayWarningNotification(text, (ENotificationDurationType)0);
				Logger.LogWarning(text);
				lastLogged = name;
			}
		}
	}

	private static ThatsLitCompat.DeviceMode CheckDevicesOnItem(Item item)
	{
		//IL_0035: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
		ThatsLitCompat.DeviceMode deviceMode = default(ThatsLitCompat.DeviceMode);
		if (item == null)
		{
			return deviceMode;
		}
		foreach (Item allItem in item.GetAllItems())
		{
			MongoID? val = ((allItem != null) ? new MongoID?(allItem.TemplateId) : ((MongoID?)null));
			if (string.IsNullOrWhiteSpace(val.HasValue ? (string)(val.GetValueOrDefault()) : null))
			{
				continue;
			}
			if (ThatsLitCompat.ExtraDevices.TryGetValue((string)(allItem.TemplateId), out var value) && value?.TemplateInstance != null)
			{
				EFT.InventoryLogic.SightMod val2 = (EFT.InventoryLogic.SightMod)(object)((allItem is EFT.InventoryLogic.SightMod) ? allItem : null);
				if (val2 != null)
				{
					ThatsLitCompat.DeviceMode a = deviceMode;
					ThatsLitCompat.DeviceTemplate templateInstance = value.TemplateInstance;
					SightComponent sight = val2.Sight;
					deviceMode = ThatsLitCompat.DeviceMode.MergeMax(a, templateInstance.SafeGetMode((sight != null) ? sight.SelectedScopeMode : 0));
				}
				else
				{
					deviceMode = ThatsLitCompat.DeviceMode.MergeMax(deviceMode, value.TemplateInstance.SafeGetMode(0));
				}
			}
			else
			{
				LightComponent itemComponent = allItem.GetItemComponent<LightComponent>();
				if (itemComponent != null && itemComponent.IsActive)
				{
					ThatsLitCompat.DeviceMode deviceMode2 = GetDeviceMode((string)(allItem.TemplateId), itemComponent.SelectedMode);
					deviceMode = ThatsLitCompat.DeviceMode.MergeMax(deviceMode, deviceMode2);
				}
			}
		}
		return deviceMode;
	}

	internal static (ThatsLitCompat.DeviceMode mode, ThatsLitCompat.DeviceMode modeSub) DetermineShiningEquipments(Player player)
	{
		object obj;
		if (player == null)
		{
			obj = null;
		}
		else
		{
			Slot activeSlot = player.ActiveSlot;
			obj = ((activeSlot != null) ? activeSlot.ContainedItem : null);
		}
		ThatsLitCompat.DeviceMode deviceMode = CheckDevicesOnItem((Item)((obj is Weapon) ? obj : null));
		ThatsLitCompat.DeviceMode deviceMode2 = default(ThatsLitCompat.DeviceMode);
		InventoryEquipment val = player.Inventory?.Equipment;
		if (val == null)
		{
			return (mode: deviceMode, modeSub: deviceMode2);
		}
		ThatsLitCompat.DeviceMode a = deviceMode;
		Slot slot = val.GetSlot((EquipmentSlot)11);
		deviceMode = ThatsLitCompat.DeviceMode.MergeMax(a, CheckDevicesOnItem((slot != null) ? slot.ContainedItem : null));
		if (((player != null) ? player.ActiveSlot : null) != val.GetSlot((EquipmentSlot)0))
		{
			ThatsLitCompat.DeviceMode a2 = deviceMode2;
			Slot slot2 = val.GetSlot((EquipmentSlot)0);
			deviceMode2 = ThatsLitCompat.DeviceMode.MergeMax(a2, CheckDevicesOnItem((slot2 != null) ? slot2.ContainedItem : null));
		}
		if (((player != null) ? player.ActiveSlot : null) != val.GetSlot((EquipmentSlot)1))
		{
			ThatsLitCompat.DeviceMode a3 = deviceMode2;
			Slot slot3 = val.GetSlot((EquipmentSlot)1);
			deviceMode2 = ThatsLitCompat.DeviceMode.MergeMax(a3, CheckDevicesOnItem((slot3 != null) ? slot3.ContainedItem : null));
		}
		if (((player != null) ? player.ActiveSlot : null) != val.GetSlot((EquipmentSlot)2))
		{
			ThatsLitCompat.DeviceMode a4 = deviceMode2;
			Slot slot4 = val.GetSlot((EquipmentSlot)2);
			deviceMode2 = ThatsLitCompat.DeviceMode.MergeMax(a4, CheckDevicesOnItem((slot4 != null) ? slot4.ContainedItem : null));
		}
		return (mode: deviceMode, modeSub: deviceMode2);
	}

	private static ThatsLitCompat.DeviceMode GetDeviceMode(string itemTemplateId, int selectedMode)
	{
		ThatsLitCompat.Devices.TryGetValue(itemTemplateId, out var value);
		if (value == null)
		{
			return default(ThatsLitCompat.DeviceMode);
		}
		if (value.TemplateInstance?.modes == null || value.TemplateInstance.modes.Length <= selectedMode)
		{
			if (ThatsLitPlayer.IsDebugSampleFrame)
			{
				ItemFactory instance = Singleton<ItemFactory>.Instance;
				object arg;
				if (instance == null)
				{
					arg = null;
				}
				else
				{
					Item presetItem = instance.GetPresetItem(itemTemplateId);
					arg = ((presetItem != null) ? presetItem.Name : null);
				}
				string text = $"[That's Lit] Unknown device or mode: {itemTemplateId} {arg} mode {selectedMode}";
				NotificationManager.DisplayWarningNotification(text, (ENotificationDurationType)0);
				Logger.LogWarning(text);
				EFT.UI.ConsoleScreen.Log(text);
			}
			return default(ThatsLitCompat.DeviceMode);
		}
		return value.TemplateInstance.modes[selectedMode];
	}

	public static void GUILayoutDrawAsymetricMeter(int level, bool alternative = false, GUIStyle style = null)
	{
		if (style == null)
		{
			style = GUI.skin.label;
		}
		if (alternative)
		{
			if (level < -10)
			{
				GUILayout.Label("  ##########|----------", style, Array.Empty<GUILayoutOption>());
				return;
			}
			if (level > 10)
			{
				GUILayout.Label("  ----------|##########", style, Array.Empty<GUILayoutOption>());
				return;
			}
			switch (level)
			{
			case -11:
				GUILayout.Label("  ##########|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -10:
				GUILayout.Label("  ##########|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -9:
				GUILayout.Label("  -#########|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -8:
				GUILayout.Label("  --########|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -7:
				GUILayout.Label("  ---#######|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -6:
				GUILayout.Label("  ----######|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -5:
				GUILayout.Label("  -----#####|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -4:
				GUILayout.Label("  ------####|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -3:
				GUILayout.Label("  -------###|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -2:
				GUILayout.Label("  --------##|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case -1:
				GUILayout.Label("  ---------#|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case 0:
				GUILayout.Label("  ----------|----------", style, Array.Empty<GUILayoutOption>());
				break;
			case 1:
				GUILayout.Label("  ----------|#---------", style, Array.Empty<GUILayoutOption>());
				break;
			case 2:
				GUILayout.Label("  ----------|##--------", style, Array.Empty<GUILayoutOption>());
				break;
			case 3:
				GUILayout.Label("  ----------|###-------", style, Array.Empty<GUILayoutOption>());
				break;
			case 4:
				GUILayout.Label("  ----------|####------", style, Array.Empty<GUILayoutOption>());
				break;
			case 5:
				GUILayout.Label("  ----------|#####-----", style, Array.Empty<GUILayoutOption>());
				break;
			case 6:
				GUILayout.Label("  ----------|######----", style, Array.Empty<GUILayoutOption>());
				break;
			case 7:
				GUILayout.Label("  ----------|#######---", style, Array.Empty<GUILayoutOption>());
				break;
			case 8:
				GUILayout.Label("  ----------|########--", style, Array.Empty<GUILayoutOption>());
				break;
			case 9:
				GUILayout.Label("  ----------|#########-", style, Array.Empty<GUILayoutOption>());
				break;
			case 10:
				GUILayout.Label("  ----------|##########", style, Array.Empty<GUILayoutOption>());
				break;
			case 11:
				GUILayout.Label("  ----------|##########", style, Array.Empty<GUILayoutOption>());
				break;
			}
		}
		else if (level < -10)
		{
			GUILayout.Label("  ▰▰▰▰▰▰▰▰▰▰ ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
		}
		else if (level > 10)
		{
			GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▰▰", style, Array.Empty<GUILayoutOption>());
		}
		else
		{
			switch (level)
			{
			case -11:
				GUILayout.Label("  ▰▰▰▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -10:
				GUILayout.Label("  ▰▰▰▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -9:
				GUILayout.Label("  ▱▰▰▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -8:
				GUILayout.Label("  ▱▱▰▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -7:
				GUILayout.Label("  ▱▱▱▰▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -6:
				GUILayout.Label("  ▱▱▱▱▰▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -5:
				GUILayout.Label("  ▱▱▱▱▱▰▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -4:
				GUILayout.Label("  ▱▱▱▱▱▱▰▰▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -3:
				GUILayout.Label("  ▱▱▱▱▱▱▱▰▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -2:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▰▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case -1:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▰  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 0:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 1:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 2:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 3:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 4:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 5:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 6:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▱▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 7:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▱▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 8:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▱▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 9:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▰▱", style, Array.Empty<GUILayoutOption>());
				break;
			case 10:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▰▰", style, Array.Empty<GUILayoutOption>());
				break;
			case 11:
				GUILayout.Label("  ▱▱▱▱▱▱▱▱▱▱  ▰▰▰▰▰▰▰▰▰▰", style, Array.Empty<GUILayoutOption>());
				break;
			}
		}
	}

	internal static void RightAlignedGUILabel(string str)
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		GUILayout.HorizontalScope val = new GUILayout.HorizontalScope(Array.Empty<GUILayoutOption>());
		try
		{
			GUILayout.FlexibleSpace();
			GUILayout.Label(str, Array.Empty<GUILayoutOption>());
		}
		finally
		{
			((IDisposable)val)?.Dispose();
		}
	}

	internal static void GUILayoutFoliageMeter(int level, bool alternative = false, GUIStyle style = null)
	{
		if (style == null)
		{
			style = GUI.skin.label;
		}
		if (alternative)
		{
			if (level <= 0)
			{
				GUILayout.Label("  FOLIAGE  ----------|", style, Array.Empty<GUILayoutOption>());
				return;
			}
			if (level >= 10)
			{
				GUILayout.Label("  FOLIAGE  ##########|", style, Array.Empty<GUILayoutOption>());
				return;
			}
			switch (level)
			{
			case 1:
				GUILayout.Label("  FOLIAGE  #---------|", style, Array.Empty<GUILayoutOption>());
				break;
			case 2:
				GUILayout.Label("  FOLIAGE  ##--------|", style, Array.Empty<GUILayoutOption>());
				break;
			case 3:
				GUILayout.Label("  FOLIAGE  ###-------|", style, Array.Empty<GUILayoutOption>());
				break;
			case 4:
				GUILayout.Label("  FOLIAGE  ####------|", style, Array.Empty<GUILayoutOption>());
				break;
			case 5:
				GUILayout.Label("  FOLIAGE  #####-----|", style, Array.Empty<GUILayoutOption>());
				break;
			case 6:
				GUILayout.Label("  FOLIAGE  ######----|", style, Array.Empty<GUILayoutOption>());
				break;
			case 7:
				GUILayout.Label("  FOLIAGE  #######---|", style, Array.Empty<GUILayoutOption>());
				break;
			case 8:
				GUILayout.Label("  FOLIAGE  ########--|", style, Array.Empty<GUILayoutOption>());
				break;
			case 9:
				GUILayout.Label("  FOLIAGE  #########-|", style, Array.Empty<GUILayoutOption>());
				break;
			}
		}
		if (level <= 0)
		{
			GUILayout.Label("  FOLIAGE  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			return;
		}
		if (level >= 10)
		{
			GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▰▰▰▰", style, Array.Empty<GUILayoutOption>());
			return;
		}
		switch (level)
		{
		case 1:
			GUILayout.Label("  FOLIAGE  ▰▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 2:
			GUILayout.Label("  FOLIAGE  ▰▰▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 3:
			GUILayout.Label("  FOLIAGE  ▰▰▰▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 4:
			GUILayout.Label("  FOLIAGE  ▰▰▰▰▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 5:
			GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 6:
			GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 7:
			GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▰▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 8:
			GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▰▰▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 9:
			GUILayout.Label("  FOLIAGE  ▰▰▰▰▰▰▰▰▰▱", style, Array.Empty<GUILayoutOption>());
			break;
		}
	}

	internal static void GUILayoutTerrainMeter(int level, bool alternative = false, GUIStyle style = null)
	{
		if (style == null)
		{
			style = GUI.skin.label;
		}
		if (alternative)
		{
			if (level <= 0)
			{
				GUILayout.Label("  TERRAIN  ----------|", style, Array.Empty<GUILayoutOption>());
				return;
			}
			if (level >= 10)
			{
				GUILayout.Label("  TERRAIN  ##########|", style, Array.Empty<GUILayoutOption>());
				return;
			}
			switch (level)
			{
			case 1:
				GUILayout.Label("  TERRAIN  #---------|", style, Array.Empty<GUILayoutOption>());
				break;
			case 2:
				GUILayout.Label("  TERRAIN  ##--------|", style, Array.Empty<GUILayoutOption>());
				break;
			case 3:
				GUILayout.Label("  TERRAIN  ###-------|", style, Array.Empty<GUILayoutOption>());
				break;
			case 4:
				GUILayout.Label("  TERRAIN  ####------|", style, Array.Empty<GUILayoutOption>());
				break;
			case 5:
				GUILayout.Label("  TERRAIN  #####-----|", style, Array.Empty<GUILayoutOption>());
				break;
			case 6:
				GUILayout.Label("  TERRAIN  ######----|", style, Array.Empty<GUILayoutOption>());
				break;
			case 7:
				GUILayout.Label("  TERRAIN  #######---|", style, Array.Empty<GUILayoutOption>());
				break;
			case 8:
				GUILayout.Label("  TERRAIN  ########--|", style, Array.Empty<GUILayoutOption>());
				break;
			case 9:
				GUILayout.Label("  TERRAIN  #########-|", style, Array.Empty<GUILayoutOption>());
				break;
			}
		}
		if (level <= 0)
		{
			GUILayout.Label("  TERRAIN  ▱▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			return;
		}
		if (level >= 10)
		{
			GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▰▰▰▰", style, Array.Empty<GUILayoutOption>());
			return;
		}
		switch (level)
		{
		case 1:
			GUILayout.Label("  TERRAIN  ▰▱▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 2:
			GUILayout.Label("  TERRAIN  ▰▰▱▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 3:
			GUILayout.Label("  TERRAIN  ▰▰▰▱▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 4:
			GUILayout.Label("  TERRAIN  ▰▰▰▰▱▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 5:
			GUILayout.Label("  TERRAIN  ▰▰▰▰▰▱▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 6:
			GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▱▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 7:
			GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▰▱▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 8:
			GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▰▰▱▱", style, Array.Empty<GUILayoutOption>());
			break;
		case 9:
			GUILayout.Label("  TERRAIN  ▰▰▰▰▰▰▰▰▰▱", style, Array.Empty<GUILayoutOption>());
			break;
		}
	}

	public static string DetermineDir(Vector3 dir)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		//IL_001f: Unknown result type (might be due to invalid IL or missing references)
		Vector2 val = new Vector2(dir.x, dir.z);
		Vector2 normalized = val.normalized;
		float num = Vector2.SignedAngle(Vector2.up, normalized);
		if (num >= -22.5f && num <= 22.5f)
		{
			return "N";
		}
		if (num >= 22.5f && num <= 67.5f)
		{
			return "NE";
		}
		if (num >= 67.5f && num <= 112.5f)
		{
			return "E";
		}
		if (num >= 112.5f && num <= 157.5f)
		{
			return "SE";
		}
		if ((num >= 157.5f && num <= 180f) || (num >= -180f && num <= -157.5f))
		{
			return "S";
		}
		if (num >= -157.5f && num <= -112.5f)
		{
			return "SW";
		}
		if (num >= -112.5f && num <= -67.5f)
		{
			return "W";
		}
		if (num >= -67.5f && num <= -22.5f)
		{
			return "NW";
		}
		return "?";
	}

	internal static T ExpensiveCopyComponent<T>(T original, GameObject destination, BindingFlags bindingFlags) where T : Component
	{
		Type type = ((object)original).GetType();
		Component val = destination.AddComponent(type);
		FieldInfo[] fields = type.GetFields(bindingFlags);
		foreach (FieldInfo fieldInfo in fields)
		{
			fieldInfo.SetValue(val, fieldInfo.GetValue(original));
		}
		return (T)(object)((val is T) ? val : null);
	}

	internal static float GetPoseFactor(float currentPoseLevel, float maxPoseLevel, bool isInPronePose)
	{
		float num = currentPoseLevel / maxPoseLevel * 0.6f + 0.4f;
		if (isInPronePose)
		{
			num -= 0.4f;
		}
		num += 0.05f;
		return Mathf.Clamp01(num);
	}

	internal static float GetPoseWeightedRegularTerrainScore(float pPoseFactor, TerrainDetailScore terrainScore)
	{
		return terrainScore.regular / (1f + 0.35f * Mathf.InverseLerp(0.45f, 1f, pPoseFactor));
	}

	internal static float GetObservedTerrainDetailScoreProne(Player player, Vector3 observerEyePos, Vector3 observerLookDir, float base3x3TerrainScore)
	{
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Unknown result type (might be due to invalid IL or missing references)
		//IL_0075: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
		Vector3 surfaceNormal = player.MovementContext.SurfaceNormal;
		Dictionary<BodyPartType, EnemyPart> mainParts = player.MainParts;
		Vector3 val = (mainParts[(BodyPartType)4].Position + mainParts[(BodyPartType)5].Position) / 2f;
		Vector3 val2 = mainParts[(BodyPartType)0].Position - val;
		Vector3 val3 = observerEyePos - val;
		Vector3 val4 = Vector3.ProjectOnPlane(val2, -val3);
		float num = val4.magnitude / val2.magnitude;
		float num2 = Vector3.Angle(val2, val3);
		num = ((!(num2 > 90f)) ? (num * Mathf.InverseLerp(0f, 90f, num2)) : (num * Mathf.InverseLerp(180f, 90f, num2)));
		float num3 = Vector3.Angle(surfaceNormal, val3);
		num *= Mathf.InverseLerp(0f, 90f, num3);
		return base3x3TerrainScore * num;
	}

	internal static bool IsPMCSpawnType(WildSpawnType? spawnType)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Invalid comparison between Unknown and I4
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Invalid comparison between Unknown and I4
		if (spawnType.HasValue)
		{
			if ((int)spawnType.GetValueOrDefault() != 51)
			{
				return (int)spawnType.GetValueOrDefault() == 52;
			}
			return true;
		}
		return false;
	}
}
