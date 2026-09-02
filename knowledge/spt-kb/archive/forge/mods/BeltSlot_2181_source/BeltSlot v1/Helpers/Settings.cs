using BepInEx.Configuration;
using UnityEngine;
using static GClass1943;

namespace BeltSlot.Helpers
{
    internal enum BeltSlotLocationOption
    {
        AbovePockets,
        BelowPockets
    }
    internal enum ModifierKeyOptions
    {
        Alt
    }

    internal class Settings
    {
        private const string BeltLocationSettings = "A. Belt Location";
        private const string AutoWindowPrioritySettings = "B. Auto Window Priority fix";
        private const string EnableBeltSettings = "C. Belt auto open toggle";
        private const string ModifierKeySettings = "D. Modifier key";

        public static ConfigEntry<BeltSlotLocationOption> BeltSlotLocation { get; set; }
        public static ConfigEntry<bool> AutoWindowPriority { get; set; }
        public static ConfigEntry<KeyboardShortcut> PriorityToggleKey;
        public static ConfigEntry<bool> EnableBeltToggle { get; set; }
        public static ConfigEntry<KeyboardShortcut> BeltToggleKey;
        public static ConfigEntry<bool> ModifierKeyToggle { get; set; }
        public static ConfigEntry<ModifierKeyOptions> ModifierKey;


        public static void Init(ConfigFile Config)
        {
            BeltSlotLocation = Config.Bind(
                BeltLocationSettings,
                "Adjust belt slot location",
                BeltSlotLocationOption.AbovePockets,
                "Adjust the belt slot location."
            );


            AutoWindowPriority = Config.Bind(
                AutoWindowPrioritySettings,
                "1. Toggle usage of custom window priority",
                false,
                "Must set BSG window priority system to manual."
            );

            PriorityToggleKey = Config.Bind(
                AutoWindowPrioritySettings,
                "2. Toggle Window priority",
                new KeyboardShortcut(KeyCode.M),
                "Key to toggle window priority on or off dynamically."
            );

            EnableBeltToggle = Config.Bind(
                EnableBeltSettings,
                "1. Toggle usage of belt toggle key",
                false,
                "Enables the ability to close an equipped belt, NOTE, can cause issues when looting in raid."
            );

            BeltToggleKey = Config.Bind(
                EnableBeltSettings,
                "2. Toggle Belt open by default key",
                new KeyboardShortcut(KeyCode.B),
                "Key to toggle belts open by default or not, can cause issues if you toggle the belt while looting a body."
            );

            ModifierKeyToggle = Config.Bind(
                ModifierKeySettings,
                "1. Toggle usage of a modifier key",
                false,
                "Key to modify activation of custom hotkeys."
            );

            ModifierKey = Config.Bind(
                ModifierKeySettings,
                "2. Modifier key",
                ModifierKeyOptions.Alt,
                "Hold this key to use with toggles."
            );
        }
    }
}
