using BepInEx;
using BepInEx.Configuration;
using System;
using TaskAutomation.Helpers;
using TaskAutomation.Patches.Application;
using TaskAutomation.Patches.Screens;
using UnityEngine;

namespace TaskAutomation
{
    [BepInPlugin("com.KnotScripts.TaskAutomation", "TaskAutomation", VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public const string VERSION = "1.3.1";

        private ConfigEntry<bool> acceptBTROutOfRaid;
        private ConfigEntry<bool> acceptLightKeeperOutOfRaid;
        private ConfigEntry<bool> autoAcceptDailyQuests;
        private ConfigEntry<bool> autoAcceptQuests;
        private ConfigEntry<bool> autoAcceptQuestsThatCanFail;
        private ConfigEntry<bool> autoAcceptScavQuests;
        private ConfigEntry<bool> autoCompleteQuests;
        private ConfigEntry<bool> autoHandleQuestsThatFailOther;
        private ConfigEntry<bool> autoHandoverFindInRaid;
        private ConfigEntry<bool> autoHandoverObtain;
        private ConfigEntry<bool> autoRestartFailedQuests;
        private ConfigEntry<int> blockTurnInArmorPlateLevelHigherThan;
        private ConfigEntry<bool> blockTurnInCurrency;
        private ConfigEntry<bool> blockTurnInWeapons;
        private ConfigEntry<bool> debug;
        private ConfigEntry<KeyboardShortcut> resetDeclinedHandoverItemConditionsKeys;
        private ConfigEntry<bool> skipElimination;
        private ConfigEntry<bool> skipFindAndObtain;
        private ConfigEntry<bool> skipFindInRaid;
        private ConfigEntry<bool> skipLeaveItemAtLocation;
        private ConfigEntry<bool> skipPlaceBeacon;
        private ConfigEntry<bool> skipSellItemToTrader;
        private ConfigEntry<bool> skipSkill;
        private ConfigEntry<bool> skipSurviveAndExtract;
        private ConfigEntry<bool> skipTraderLoyalty;
        private ConfigEntry<bool> skipVisitPlace;
        private ConfigEntry<bool> skipWeaponAssembly;
        private ConfigEntry<double> thresholdCurrencyHandover;
        private ConfigEntry<double> thresholdGPCoinHandover;
        private ConfigEntry<bool> useHandoverQuestItemsWindow;
        private ConfigEntry<float> waitForSeconds;

        private void Awake()
        {
            try
            {
                LogHelper.Logger = this.Logger;

                this.enablePatches();
                this.setConfigurables();
            }
            catch (Exception exception)
            {
                LogHelper.LogException(exception);
            }
        }

        private void enablePatches()
        {
            new TarkovApplication_Init().Enable();
            new InventoryScreen_Show().Enable();
        }

        private float getWaitForSecondsValue()
        {
            if (this.waitForSeconds.Value < 0)
                return 0f;
            if (this.waitForSeconds.Value > 0.5f)
                return 0.5f;
            return this.waitForSeconds.Value;
        }

        private void global_SettingChanged(object sender, EventArgs e)
        {
            this.setGlobalSettings();
        }

        private void setConfigurables()
        {
            this.debug = this.Config.Bind("General", "Debug", false, "Debug");
            this.debug.SettingChanged += this.global_SettingChanged;

            this.waitForSeconds = this.Config.Bind("General", "DelayTime", 0.3f, "(expert) delay time between automation actions.");
            this.waitForSeconds.SettingChanged += this.global_SettingChanged;

            this.autoAcceptDailyQuests = this.Config.Bind("Automation", "AutoAcceptDailyQuests", true, "Automatically accept daily quests.");
            this.autoAcceptDailyQuests.SettingChanged += this.global_SettingChanged;

            this.autoAcceptQuests = this.Config.Bind("Automation", "AutoAcceptQuests", true, "Automatically accept quests.");
            this.autoAcceptQuests.SettingChanged += this.global_SettingChanged;

            this.acceptBTROutOfRaid = this.Config.Bind("Automation", "AcceptBTROutOfRaid", false, "Automatically accept BTR quests out of raid.");
            this.acceptBTROutOfRaid.SettingChanged += this.global_SettingChanged;

            this.acceptLightKeeperOutOfRaid = this.Config.Bind("Automation", "AcceptLightKeeperOutOfRaid", false, "Automatically accept Light Keeper quests out of raid.");
            this.acceptLightKeeperOutOfRaid.SettingChanged += this.global_SettingChanged;

            this.autoCompleteQuests = this.Config.Bind("Automation", "AutoCompleteQuests", true, "Automatically complete quests.");
            this.autoCompleteQuests.SettingChanged += this.global_SettingChanged;

            this.autoHandleQuestsThatFailOther = this.Config.Bind("Automation", "AutoHandleQuestsThatFailOther", false, "Automatically handle quests that fail other quests.");
            this.autoHandleQuestsThatFailOther.SettingChanged += this.global_SettingChanged;

            this.autoHandoverFindInRaid = this.Config.Bind("Automation", "AutoFindInRaid", true, "Automatically handover find in raid items.");
            this.autoHandoverFindInRaid.SettingChanged += this.global_SettingChanged;

            this.autoHandoverObtain = this.Config.Bind("Automation", "AutoFindAndObtain", true, "Automatically handover find and obtain items.");
            this.autoHandoverObtain.SettingChanged += this.global_SettingChanged;

            this.autoRestartFailedQuests = this.Config.Bind("Automation", "AutoRestartFailedQuests", true, "Automatically restart failed quests.");
            this.autoRestartFailedQuests.SettingChanged += this.global_SettingChanged;

            this.autoAcceptQuestsThatCanFail = this.Config.Bind("Automation", "AutoAcceptQuestsThatCanFail", false, "Automatically accept quests that are that can fail.");
            this.autoAcceptQuestsThatCanFail.SettingChanged += this.global_SettingChanged;

            this.autoAcceptScavQuests = this.Config.Bind("Automation", "AutoAcceptScavQuests", false, "Automatically accept scav quests.");
            this.autoAcceptScavQuests.SettingChanged += this.global_SettingChanged;

            this.blockTurnInArmorPlateLevelHigherThan = this.Config.Bind("Handover Items", "BlockTurnInArmorPlateLevelHigherThan", 3, new ConfigDescription("Block the automatic handover of items that have plates higher then level.", new AcceptableValueRange<int>(0, 6)));
            this.blockTurnInArmorPlateLevelHigherThan.SettingChanged += this.global_SettingChanged;

            this.blockTurnInCurrency = this.Config.Bind("Handover Items", "BlockTurnInCurrency", false, "Block the automatic handover of currency (Rubles, Dollars, Euros and GP Coins).");
            this.blockTurnInCurrency.SettingChanged += this.global_SettingChanged;

            this.blockTurnInWeapons = this.Config.Bind("Handover Items", "BlockTurnInWeapons", false, "Block the automatic handover of weapons.");
            this.blockTurnInWeapons.SettingChanged += this.global_SettingChanged;

            this.thresholdCurrencyHandover = this.Config.Bind("Handover Items", "ThresholdCurrencyHandover", 1.5, "Threshold for the number of times you want to have the currency amount before handing it in.");
            this.thresholdCurrencyHandover.SettingChanged += this.global_SettingChanged;

            this.thresholdGPCoinHandover = this.Config.Bind("Handover Items", "ThresholdGPCoinHandover", 0.0, "Threshold for the number of times you want to have the GP Coin amount before handing it in.");
            this.thresholdGPCoinHandover.SettingChanged += this.global_SettingChanged;

            this.useHandoverQuestItemsWindow = this.Config.Bind("Handover Items", "UseHandoverQuestItemsWindow", false, "Always show the HandoverQuestItemsWindow to handover items.");
            this.useHandoverQuestItemsWindow.SettingChanged += this.global_SettingChanged;

            this.resetDeclinedHandoverItemConditionsKeys = this.Config.Bind("Handover Items", "ResetDeclinedHandoverItemsKeys", new KeyboardShortcut(KeyCode.R, KeyCode.LeftControl), "Keys to press to reset the (in memory) declined handover items and restart the couritine. So the HandoverQuestItemsWindow will pop up again for these quests.");
            this.resetDeclinedHandoverItemConditionsKeys.SettingChanged += this.global_SettingChanged;

            this.skipFindInRaid = this.Config.Bind("Skipper", "SkipFindInRaid", false, "Skip finding items in raid quest conditions.");
            this.skipFindInRaid.SettingChanged += this.global_SettingChanged;

            this.skipFindAndObtain = this.Config.Bind("Skipper", "SkipFindAndObtain", false, "Skip find and obtain quest conditions.");
            this.skipFindAndObtain.SettingChanged += this.global_SettingChanged;

            this.skipLeaveItemAtLocation = this.Config.Bind("Skipper", "SkipLeaveItemAtLocation", false, "Skip leave item at location quest conditions.");
            this.skipLeaveItemAtLocation.SettingChanged += this.global_SettingChanged;

            this.skipVisitPlace = this.Config.Bind("Skipper", "SkipVisitPlace", false, "Skip leave item at location quest conditions.");
            this.skipVisitPlace.SettingChanged += this.global_SettingChanged;

            this.skipPlaceBeacon = this.Config.Bind("Skipper", "SkipPlaceBeacon", false, "Skip place beacon quest conditions.");
            this.skipPlaceBeacon.SettingChanged += this.global_SettingChanged;

            this.skipWeaponAssembly = this.Config.Bind("Skipper", "SkipWeaponAssembly", false, "Skip weapon assembly quest conditions.");
            this.skipFindAndObtain.SettingChanged += this.global_SettingChanged;

            this.skipTraderLoyalty = this.Config.Bind("Skipper", "SkipTraderLoyalty", false, "Skip trader loyalty quest conditions.");
            this.skipTraderLoyalty.SettingChanged += this.global_SettingChanged;

            this.skipSkill = this.Config.Bind("Skipper", "SkipSkill", false, "Skip skill level quest conditions.");
            this.skipSkill.SettingChanged += this.global_SettingChanged;

            this.skipSellItemToTrader = this.Config.Bind("Skipper", "SkipSellItemToTrader", false, "Skip sell items to traders quest conditions.");
            this.skipSellItemToTrader.SettingChanged += this.global_SettingChanged;

            this.skipSurviveAndExtract = this.Config.Bind("Skipper", "SkipSurviveAndExtract", false, "Skip survive and extract quest conditions.");
            this.skipSurviveAndExtract.SettingChanged += this.global_SettingChanged;

            this.skipElimination = this.Config.Bind("Skipper", "SkipElimination", false, "Skip elimination quest conditions.");
            this.skipElimination.SettingChanged += this.global_SettingChanged;
            this.skipElimination.SettingChanged += this.skipElimination_SettingChanged;

            this.setGlobalSettings();
        }

        private void setGlobalSettings()
        {
            Globals.Debug = this.debug.Value;
            Globals.AcceptBTROutOfRaid = this.acceptBTROutOfRaid.Value;
            Globals.AcceptLightKeeperOutOfRaid = this.acceptLightKeeperOutOfRaid.Value;
            Globals.AutoAcceptDailyQuests = this.autoAcceptDailyQuests.Value;
            Globals.AutoAcceptQuests = this.autoAcceptQuests.Value;
            Globals.AutoAcceptQuestsThatCanFail = this.autoAcceptQuestsThatCanFail.Value;
            Globals.AutoAcceptScavQuests = this.autoAcceptScavQuests.Value;
            Globals.AutoCompleteQuests = this.autoCompleteQuests.Value;
            Globals.AutoHandleQuestsThatFailOther = this.autoHandleQuestsThatFailOther.Value;
            Globals.AutoHandoverFindInRaid = this.autoHandoverFindInRaid.Value;
            Globals.AutoHandoverObtain = this.autoHandoverObtain.Value;
            Globals.AutoRestartFailedQuests = this.autoRestartFailedQuests.Value;
            Globals.BlockTurnInArmorPlateLevelHigherThan = this.blockTurnInArmorPlateLevelHigherThan.Value;
            Globals.BlockTurnInCurrency = this.blockTurnInCurrency.Value;
            Globals.BlockTurnInWeapons = this.blockTurnInWeapons.Value;
            Globals.ThresholdCurrencyHandover = this.thresholdCurrencyHandover.Value;
            Globals.ThresholdGPCoinHandover = this.thresholdGPCoinHandover.Value;
            Globals.SkipElimination = this.skipElimination.Value;
            Globals.SkipFindInRaid = this.skipFindInRaid.Value;
            Globals.SkipFindAndObtain = this.skipFindAndObtain.Value;
            Globals.SkipLeaveItemAtLocation = this.skipLeaveItemAtLocation.Value;
            Globals.SkipPlaceBeacon = this.skipPlaceBeacon.Value;
            Globals.SkipSellItemToTrader = this.skipSellItemToTrader.Value;
            Globals.SkipSkill = this.skipSkill.Value;
            Globals.SkipSurviveAndExtract = this.skipSurviveAndExtract.Value;
            Globals.SkipTraderLoyalty = this.skipTraderLoyalty.Value;
            Globals.SkipVisitPlace = this.skipVisitPlace.Value;
            Globals.SkipWeaponAssembly = this.skipWeaponAssembly.Value;
            Globals.UseHandoverQuestItemsWindow = this.useHandoverQuestItemsWindow.Value;
            Globals.ResetDeclinedHandoverItemConditionsKeys = this.resetDeclinedHandoverItemConditionsKeys.Value;
            Globals.WaitForSeconds = this.getWaitForSecondsValue();
        }

        private void skipElimination_SettingChanged(object sender, EventArgs e)
        {
            if (this.skipElimination.Value)
                LogHelper.LogInfoWithNotification("Are you kidding? What's the fun in that?");
            else
                LogHelper.LogInfoWithNotification("That's better!");
        }
    }
}