using System;
using System.Collections.Generic;
using LeaveItThere.Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace LeaveItThere.CustomUI;

internal static class UIColorHelper
{
	public static void RefreshColors()
	{
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Unknown result type (might be due to invalid IL or missing references)
		//IL_012c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_0158: Unknown result type (might be due to invalid IL or missing references)
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Unknown result type (might be due to invalid IL or missing references)
		MoveModeUI instance = MoveModeUI.Instance;
		Color buttonColor = GetButtonColor(instance.SelectedTab);
		foreach (MoveModeUI.MenuTab allMenuTab in instance.AllMenuTabs)
		{
			bool isSelected = allMenuTab == instance.SelectedTab;
			SetSelectableColor((Selectable)(object)allMenuTab.TabButton, GetTabButtonColor(isSelected, allMenuTab), MoveModeUI.ERecolorTarget.Base);
			SetSelectableColor((Selectable)(object)allMenuTab.TabButton, Settings.HighlightColor.Value, MoveModeUI.ERecolorTarget.Highlight);
			SetSelectableColor((Selectable)(object)allMenuTab.TabButton, Settings.ClickColor.Value, MoveModeUI.ERecolorTarget.Pressed);
		}
		SetSelectableColor((Selectable)(object)instance.SaveButton, buttonColor, MoveModeUI.ERecolorTarget.Base);
		SetSelectableColor((Selectable)(object)instance.SaveButton, Settings.HighlightColor.Value, MoveModeUI.ERecolorTarget.Highlight);
		SetSelectableColor((Selectable)(object)instance.SaveButton, Settings.ClickColor.Value, MoveModeUI.ERecolorTarget.Pressed);
		SetSelectableColor((Selectable)(object)instance.CancelButton, buttonColor, MoveModeUI.ERecolorTarget.Base);
		SetSelectableColor((Selectable)(object)instance.CancelButton, Settings.HighlightColor.Value, MoveModeUI.ERecolorTarget.Highlight);
		SetSelectableColor((Selectable)(object)instance.CancelButton, Settings.ClickColor.Value, MoveModeUI.ERecolorTarget.Pressed);
		SetSelectableColor((Selectable)(object)instance.DragWindowButton, buttonColor, MoveModeUI.ERecolorTarget.Base);
		SetSelectableColor((Selectable)(object)instance.DragWindowButton, Settings.HighlightColor.Value, MoveModeUI.ERecolorTarget.Highlight);
		SetSelectableColor((Selectable)(object)instance.DragWindowButton, Settings.ClickColor.Value, MoveModeUI.ERecolorTarget.Pressed);
		SetTabColor(instance.PosTab, Settings.PositionTabColor.Value, MoveModeUI.ERecolorTarget.Base);
		SetTabColor(instance.RotTab, Settings.RotationTabColor.Value, MoveModeUI.ERecolorTarget.Base);
		SetTabColor(instance.PhysTab, Settings.PhysicsTabColor.Value, MoveModeUI.ERecolorTarget.Base);
		SetTabColor(instance.PosTab, Settings.HighlightColor.Value, MoveModeUI.ERecolorTarget.Highlight);
		SetTabColor(instance.RotTab, Settings.HighlightColor.Value, MoveModeUI.ERecolorTarget.Highlight);
		SetTabColor(instance.PhysTab, Settings.HighlightColor.Value, MoveModeUI.ERecolorTarget.Highlight);
		SetTabColor(instance.PosTab, Settings.ClickColor.Value, MoveModeUI.ERecolorTarget.Pressed);
		SetTabColor(instance.RotTab, Settings.ClickColor.Value, MoveModeUI.ERecolorTarget.Pressed);
		SetTabColor(instance.PhysTab, Settings.ClickColor.Value, MoveModeUI.ERecolorTarget.Pressed);
		((Graphic)instance.Background).color = Settings.BackgroundColor.Value;
	}

	public static Color GetButtonColor(MoveModeUI.MenuTab selectedTab)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		if (selectedTab is MoveModeUI.PositionTab)
		{
			return Settings.PositionTabColor.Value;
		}
		if (selectedTab is MoveModeUI.RotationTab)
		{
			return Settings.RotationTabColor.Value;
		}
		if (selectedTab is MoveModeUI.PhysicsTab)
		{
			return Settings.PhysicsTabColor.Value;
		}
		return Color.gray;
	}

	public static Color GetTabButtonColor(bool isSelected, MoveModeUI.MenuTab tab)
	{
		//IL_0003: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		if (!isSelected)
		{
			return Color.gray;
		}
		if (tab is MoveModeUI.PositionTab)
		{
			return Settings.PositionTabColor.Value;
		}
		if (tab is MoveModeUI.RotationTab)
		{
			return Settings.RotationTabColor.Value;
		}
		if (tab is MoveModeUI.PhysicsTab)
		{
			return Settings.PhysicsTabColor.Value;
		}
		return Color.gray;
	}

	public static void SetSelectableColor(Selectable selectable, Color color, MoveModeUI.ERecolorTarget target)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		ColorBlock colors = selectable.colors;
		if (target == MoveModeUI.ERecolorTarget.Base)
		{
			colors.normalColor = color;
			colors.selectedColor = color;
		}
		if (target == MoveModeUI.ERecolorTarget.Highlight)
		{
			colors.highlightedColor = color;
		}
		if (target == MoveModeUI.ERecolorTarget.Pressed)
		{
			colors.pressedColor = color;
		}
		selectable.colors = colors;
	}

	public static void SetTabColor(MoveModeUI.MenuTab tab, Color color, MoveModeUI.ERecolorTarget target)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		MyExtensions.ExecuteForEach<Selectable>(tab.SelectablesOnTab, delegate(Selectable sel)
		{
			SetSelectableColor(sel, color, target);
		});
	}
}
