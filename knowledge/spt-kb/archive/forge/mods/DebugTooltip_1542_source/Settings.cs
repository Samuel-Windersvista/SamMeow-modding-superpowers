using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DebugTooltip
{
    internal class Settings
    {
        // Categories
        private const string GeneralSection = "1. General";

        // General
        public static ConfigEntry<bool> ShowDebugInfo { get; set; }
        public static ConfigEntry<bool> ShowCopyPrompt { get; set; }
        public static ConfigEntry<KeyboardShortcut> ToggleShortcut { get; set; }

        public static void Init(ConfigFile config)
        {
            var configEntries = new List<ConfigEntryBase>();

            // General
            configEntries.Add(ShowDebugInfo = config.Bind(
                GeneralSection,
                "Show Debug Tooltip",
                true,
                new ConfigDescription(
                    "Whether to add debugging information to tooltips",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(ShowCopyPrompt = config.Bind(
                GeneralSection,
                "Show Copy Instructions",
                true,
                new ConfigDescription(
                    "Shows a reminder of how to copy the tooltip contents",
                    null,
                    new ConfigurationManagerAttributes { })));

            configEntries.Add(ToggleShortcut = config.Bind(
                GeneralSection,
                "Toggle Shortcut",
                new KeyboardShortcut(KeyCode.F9),
                new ConfigDescription(
                    "Keyboard shortcut to toggle debug tooltips",
                    null,
                    new ConfigurationManagerAttributes { })));

            RecalcOrder(configEntries);
        }

        private static void RecalcOrder(List<ConfigEntryBase> configEntries)
        {
            // Set the Order field for all settings, to avoid unnecessary changes when adding new settings
            int settingOrder = configEntries.Count;
            foreach (var entry in configEntries)
            {
                if (entry.Description.Tags[0] is ConfigurationManagerAttributes attributes)
                {
                    attributes.Order = settingOrder;
                }

                settingOrder--;
            }
        }
    }

    internal static class SettingExtensions
    {
        public static void Subscribe<T>(this ConfigEntry<T> configEntry, Action<T> onChange)
        {
            configEntry.SettingChanged += (_, _) => onChange(configEntry.Value);
        }


        public static void Bind<T>(this ConfigEntry<T> configEntry, Action<T> onChange)
        {
            configEntry.Subscribe(onChange);
            onChange(configEntry.Value);
        }
    }
}
