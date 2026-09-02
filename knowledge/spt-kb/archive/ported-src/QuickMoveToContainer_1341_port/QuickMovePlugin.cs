using BepInEx;
using Bsg.GameSettings;
using DrakiaXYZ.QuickMoveToContainer.Helpers;
using DrakiaXYZ.QuickMoveToContainer.VersionChecker;
using EFT.InventoryLogic;
using EFT.Settings.Game;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DrakiaXYZ.QuickMoveToContainer
{
    [BepInPlugin("xyz.drakia.quickmovetocontainer", "DrakiaXYZ-QuickMoveToContainer", "1.5.0")]
    [BepInDependency("com.SPT.core", "4.1.0")]
    public class QuickMovePlugin : BaseUnityPlugin
    {
        private void Awake()
        {
            if (!TarkovVersion.CheckEftVersion(Logger, Info, Config))
            {
                throw new Exception($"Invalid EFT Version");
            }

            Settings.Init(Config);

            new QuickFindPatch().Enable();
            new DisablePriorityWindowPatch().Enable();
        }
    }

    public class QuickFindPatch : ModulePatch
    {
        private static FieldInfo _windowListField;
        private static FieldInfo _windowItemField;
        private static FieldInfo _windowDataWindowField;

        protected override MethodBase GetTargetMethod()
        {
            _windowListField = AccessTools.Field(typeof(ItemUiContext), "_windows");
            _windowItemField = AccessTools.Field(typeof(GridWindow), "_item");
            _windowDataWindowField = AccessTools.Field(typeof(ItemUiContext.WindowData), "Window");

            return typeof(ItemManipulator).GetMethod(nameof(ItemManipulator.QuickFindAppropriatePlace));
        }

        [PatchPrefix]
        public static void PatchPrefix(Item item, ref IEnumerable<CompoundItem> targets, ItemManipulator.EMoveItemOrder order)
        {
            // If `order` doesn't have `MoveToAnotherSide` set, don't do anything
            if (!order.HasFlag(ItemManipulator.EMoveItemOrder.MoveToAnotherSide))
            {
                return;
            }

            // Find the currently active containers
            var itemContainer = item.Parent.Container;
            List<CompoundItem> targetContainers = FindTargetContainers(itemContainer);
            if (targetContainers.Count == 0)
            {
                return;
            }

            var newTargets = new List<CompoundItem>();
            newTargets.AddRange(targetContainers);
            newTargets.AddRange(targets);

            targets = newTargets;
        }

        private static List<CompoundItem> FindTargetContainers(IContainer itemContainer)
        {
            var gridWindowList = new List<CompoundItem>();

            IList openWindowList = (IList)_windowListField.GetValue(ItemUiContext.Instance);
            for (int i = openWindowList.Count - 1; i >= 0; i--)
            {
                var window = _windowDataWindowField.GetValue(openWindowList[i]);
                if (window.GetType() == typeof(GridWindow))
                {
                    GridWindow gridWindow = (GridWindow)window;
                    CompoundItem windowLootItem = _windowItemField.GetValue(gridWindow) as CompoundItem;

                    // Skip if the gridWindow contains the container the item is coming from
                    if (Enumerable.Contains(windowLootItem.Containers, itemContainer))
                    {
                        continue;
                    }

                    gridWindowList.Add(windowLootItem);

                    // If we're only checking the topmost container, exit here
                    if (!Settings.AllOpenContainers.Value)
                    {
                        break;
                    }
                }
            }

            return gridWindowList;
        }
    }

    public class DisablePriorityWindowPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameSettingsGroup).GetConstructors().First();
        }

        [PatchPostfix]
        public static void PatchPostfix(ref GameSetting<GameSettingsGroup.EPriorityWindowMode> ___PriorityWindowMode)
        {
            ___PriorityWindowMode = new CustomDisabledSetting<GameSettingsGroup.EPriorityWindowMode>("Settings/Game/PriorityWindowMode", GameSettingsGroup.EPriorityWindowMode.Disabled, null);
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
