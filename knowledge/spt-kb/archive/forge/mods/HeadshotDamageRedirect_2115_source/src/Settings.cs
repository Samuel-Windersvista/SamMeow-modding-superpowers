using BepInEx.Configuration;
using System.Collections.Generic;

namespace HeadshotDamageRedirect
{
    internal class Settings
    {
        public static ConfigEntry<bool> ModEnabled;
        public static ConfigEntry<bool> ChestEnabled;
        public static ConfigEntry<bool> DisplayMessage;
        public static ConfigEntry<bool> DebugEnabled;
        public static ConfigEntry<float> RedirectPercentage;
        public static ConfigEntry<float> MaxHeadDamageNumber;
        public static ConfigEntry<float> MinHeadDamageToRedirect;
        public static ConfigEntry<float> ChanceToRedirect;
        public static ConfigEntry<float> GlobalDamageReductionPercentage;
        public static ConfigEntry<float> HeadshotMultiplier;
        public static ConfigEntry<int> BodyPartsCountToRedirectTo;

        public static Dictionary<EBodyPart, ConfigEntry<bool>> RedirectParts = new Dictionary<EBodyPart, ConfigEntry<bool>>();

        public static void Init(ConfigFile Config)
        {
            const string GeneralSectionTitle = "General";
            const string OptionalSectionTitle = "Optional";
            const string SelectPartSection = "Redirection Body Part Targets";

            int optionCount = 0;

            string name = "Enable Headshot Damage Redirection";
            string description = "Turns headshot damage redirection On or Off";
            bool defaultBool = true;

            ModEnabled = Config.Bind(
                GeneralSectionTitle, name, defaultBool,
                new ConfigDescription(description, null,
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

            name = "Enable Chest Damage Redirection";
            description = "Turns Chest damage redirection On or Off";
            defaultBool = true;

            ChestEnabled = Config.Bind(
                GeneralSectionTitle, name, defaultBool,
                new ConfigDescription(description, null,
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));


            name = "Display Notification on Damage Received";
            description = "Display an in game notification when damage to your head is detected and modified.";
            defaultBool = false;

            DisplayMessage = Config.Bind(
                GeneralSectionTitle, name, defaultBool,
                new ConfigDescription(description, null,
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

            name = "Damage Redirection Percentage";
            description = "The amount of damage in percentage, to redirect to another body part when the player receives damage. " +
                    "So if this is set to 60, and you receive 50 damage to your head, you will instead receive 20 damage to their head, " +
                    "and 40 will be redirected to a random body part selected. ";
            float defaultFloat = 60f;

            RedirectPercentage = Config.Bind(
                GeneralSectionTitle, name, defaultFloat,
                new ConfigDescription(description,
                new AcceptableValueRange<float>(0f, 100f),
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

            name = "Debug Enabled";
            description = "Log all damage before and after redirection";
            defaultBool = false;

            DebugEnabled = Config.Bind(
                GeneralSectionTitle, name, defaultBool,
                new ConfigDescription(description, null,
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

            // Optional Settings
            name = "Percentage Chance to Redirect";
            description =
                "100 means this is disabled. " +
                "If below 100, this will roll a chance to actually redirect damage. " +
                "if the roll fails, this mod will affect nothing, and full damage will go to your head.";
            defaultFloat = 100f;

            ChanceToRedirect = Config.Bind(
                OptionalSectionTitle, name, defaultFloat,
                new ConfigDescription(description,
                new AcceptableValueRange<float>(0f, 100f),
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

            name = "Redirected Damage Multiplier";
            description =
                "1 means this is disabled. " +
                "If above 1, damage to the head/chest will be multiplied before being sent to another body part, making it more punishing.";
            defaultFloat = 1f;

            HeadshotMultiplier = Config.Bind(
                OptionalSectionTitle, name, defaultFloat,
                new ConfigDescription(description,
                new AcceptableValueRange<float>(0f, 3f),
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

            name = "Clamp Max Damage";
            description =
                "0 means this is disabled. " +
                "Max damage that can be applied to head/chest after redirection.";
            defaultFloat = 0f;

            MaxHeadDamageNumber = Config.Bind(
                OptionalSectionTitle, name, defaultFloat,
                new ConfigDescription(description,
                new AcceptableValueRange<float>(0f, 100f),
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

            name = "Minimum Damage To Redirect";
            description =
                "0 means this is disabled. " +
                "If set above 0, this will be the minimum damage to your head for damage redirection to occur " +
                "So for example, if the player receives 20 damage to the head, and this is set to 30, no damage will be redirected at all, and the mod will do nothing. " +
                "This happens BEFORE redirection!";
            defaultFloat = 17f;

            MinHeadDamageToRedirect = Config.Bind(
                OptionalSectionTitle, name, defaultFloat,
                new ConfigDescription(description,
                new AcceptableValueRange<float>(0f, 100f),
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

            // Body Part Selection
            List<EBodyPart> baseParts = ApplyDamageInfoPatch.AllBodyParts;

            name = "Parts to Redirect To";
            description =
                "How many parts to spread the damage received to.";

            BodyPartsCountToRedirectTo = Config.Bind(
                SelectPartSection, name, 1,
                new ConfigDescription(description,
                new AcceptableValueRange<int>(1, baseParts.Count),
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

            for (int i = 0; i < baseParts.Count; i++)
            {
                EBodyPart part = baseParts[i];
                name = part.ToString();
                description =
                    $"Headshot damage will be able to be redirected to [{part}]. " +
                    $"Which part is selected is random, but will only select from the parts enabled here. " +
                    $"For example, If you wish to have HDR only redirect to the chest, disable all other parts except the chest.";

                ConfigEntry<bool> config = Config.Bind(
                SelectPartSection, name, true,
                new ConfigDescription(description, null,
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

                RedirectParts.Add(part, config);
            }

            name = "% All Damage Received";
            description = "Scale all incoming damage.";
            defaultFloat = 100f;

            GlobalDamageReductionPercentage = Config.Bind(
                "Dad Gamer!", name, defaultFloat,
                new ConfigDescription(description,
                new AcceptableValueRange<float>(0f, 100f),
                new ConfigurationManagerAttributes { Order = optionCount-- }
                ));

        }

        public static int CheckPartsEnabledCount()
        {
            int result = 0;
            foreach (var part in RedirectParts.Values)
                if (part.Value == true)
                    result++;

            return result;
        }
    }
}