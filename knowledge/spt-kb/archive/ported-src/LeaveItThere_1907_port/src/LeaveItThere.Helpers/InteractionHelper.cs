using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Comfort.Common;
using EFT;
using EFT.Communications;
using EFT.InputSystem;
using EFT.UI;
using HarmonyLib;
using LeaveItThere.Components;
using UnityEngine;

namespace LeaveItThere.Helpers;

public static class InteractionHelper
{
	private static IEnumerable<ECommand> _allCommands = Enum.GetValues(typeof(ECommand)).Cast<ECommand>();

	public static MethodInfo GetInteractiveActionsMethodInfo<TIneractive>()
	{
		return AccessTools.FirstMethod(typeof(InteractionContextHelper), (Func<MethodInfo, bool>)((MethodInfo method) => method.GetParameters()[0].Name == "owner" && method.GetParameters()[1].ParameterType == typeof(TIneractive)));
	}

	public static void RefreshPrompt()
	{
		LITSession instance = LITSession.Instance;
		instance.GamePlayerOwner.ClearInteractionState();
		try
		{
			instance.GamePlayerOwner.InteractionsChangedHandler();
		}
		catch (Exception)
		{
		}
	}

	public static void NotificationLong(string message)
	{
		NotificationManager.DisplayMessageNotification(message, (ENotificationDurationType)1, (ENotificationIconType)0, (Color?)null);
	}

	public static void NotificationLongWarning(string message)
	{
		NotificationManager.DisplayWarningNotification(message, (ENotificationDurationType)1);
	}

	public static void ErrorPlayerFeedback(string message)
	{
		NotificationLongWarning(message);
		Singleton<GUISounds>.Instance.PlayUISound((EUISoundType)6);
	}

	public static void SetCameraRotationLocked(bool enabled)
	{
		//IL_0049: Unknown result type (might be due to invalid IL or missing references)
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0070: Unknown result type (might be due to invalid IL or missing references)
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
		Player player = LITSession.Instance.Player;
		Vector2 val = new Vector2(-360f, 360f);
		Vector2 val2 = new Vector2(-90f, 90f);
		Vector2 val3 = new Vector2(-16f, 25f);
		if (enabled)
		{
			Vector2 val4 = new Vector2(player.MovementContext.Rotation.x, player.MovementContext.Rotation.x);
			Vector2 val5 = new Vector2(player.MovementContext.Rotation.y, player.MovementContext.Rotation.y);
			player.MovementContext.SetRotationLimit(val4, val5);
		}
		else
		{
			Vector2 val6 = ((!player.MovementContext.IsInPronePose) ? val2 : val3);
			player.MovementContext.SetRotationLimit(val, val6);
		}
	}

	public static void SetMostInputsIgnored(bool ignored, IEnumerable<ECommand> except = null)
	{
		if (except == null)
		{
			except = Array.Empty<ECommand>();
		}
		if (ignored)
		{
			GamePlayerOwner.AddIgnoreInputCommands(_allCommands.Except(except));
		}
		else
		{
			GamePlayerOwner.RemoveIgnoreInputCommands(_allCommands.Except(except));
		}
	}
}
