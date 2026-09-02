using BeltSlot.Helpers;
using EFT.InventoryLogic;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System.Reflection;
using UnityEngine;
using GameSettingsClass = GClass1053;
using Bsg.GameSettings;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace BeltSlot.Patches
{
    // Create the submenu options (inventory screen)
    public class ManualPriorityWindowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameSettingsClass).GetConstructors().First();
        }

        [PatchPostfix]
        public static void PatchPostfix(ref GameSetting<GameSettingsClass.EPriorityWindowMode> ___PriorityWindowMode)
        {
            //if(!Settings.AutoWindowPriority.Value)
            //{
            //    return;
            //}
            ___PriorityWindowMode = new CustomDisabledSetting<GameSettingsClass.EPriorityWindowMode>("Settings/Game/PriorityWindowMode", GameSettingsClass.EPriorityWindowMode.Manual, null);
        }

        private class CustomDisabledSetting<T> : StateGameSetting<T>
        {
            public CustomDisabledSetting(string key, T defaultValue, Func<T, T> preProcessor) : base(key, defaultValue, preProcessor) { }

#pragma warning disable CS1998
            // Setting is disabled, so don't allow setting its value
            public override async Task SetValue(T value) { }
#pragma warning restore CS199
        }
    }
}