using System;
using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SPT.Reflection.Patching;

namespace WhiteBoxFix;

public static class ItemViewPatches
{
	public class DraggedItemViewMethodCheckItem : ModulePatch
	{
		protected override MethodBase GetTargetMethod()
		{
			return AccessTools.Method(typeof(ItemFilter), "CheckItem", (Type[])null, (Type[])null);
		}

		[PatchPrefix]
		private static void PatchPrefix(Item item, ref MongoID[] acceptableNodes)
		{
			if (Array.Exists(acceptableNodes, (MongoID s) => string.IsNullOrEmpty(s.ToString())))
			{
				ModulePatch.Logger.LogError((object)"--------------------------------");
				ModulePatch.Logger.LogError((object)"WhiteBoxFix: Found null in array");
				ModulePatch.Logger.LogError((object)"--------------------------------");
				ModulePatch.Logger.LogError((object)"Start clean up");
				acceptableNodes = acceptableNodes.Where((MongoID s) => !string.IsNullOrEmpty(s.ToString())).ToArray();
				ModulePatch.Logger.LogError((object)"End clean up");
				if (Array.Exists(acceptableNodes, (MongoID s) => string.IsNullOrEmpty(s.ToString())))
				{
					ModulePatch.Logger.LogError((object)"--------------------------------");
					ModulePatch.Logger.LogError((object)"WhiteBoxFix: Found null in array after clean up");
					ModulePatch.Logger.LogError((object)"--------------------------------");
				}
				else
				{
					ModulePatch.Logger.LogError((object)"--------------------------------");
					ModulePatch.Logger.LogError((object)"WhiteBoxFix: No null found in array after clean up");
					ModulePatch.Logger.LogError((object)"--------------------------------");
				}
			}
		}
	}
}
