using BepInEx.Configuration;
using UnityEngine;

#nullable enable

internal static class Globals
{
    public static bool AcceptBTROutOfRaid = false;
    public static bool AcceptLightKeeperOutOfRaid = false;
    public static bool AutoAcceptDailyQuests = true;
    public static bool AutoAcceptQuests = true;
    public static bool AutoAcceptQuestsThatCanFail = false;
    public static bool AutoAcceptScavQuests = false;
    public static bool AutoCompleteQuests = true;
    public static bool AutoHandleQuestsThatFailOther = false;
    public static bool AutoHandoverFindInRaid = true;
    public static bool AutoHandoverObtain = true;
    public static bool AutoRestartFailedQuests = true;
    public static int BlockTurnInArmorPlateLevelHigherThan = 3;
    public static bool BlockTurnInCurrency = false;
    public static bool BlockTurnInWeapons = false;
    public static bool Debug = false;
    public static KeyboardShortcut InvestigateKeys = new KeyboardShortcut(KeyCode.I, KeyCode.LeftControl);
    public static KeyboardShortcut ResetDeclinedHandoverItemConditionsKeys = new KeyboardShortcut(KeyCode.R, KeyCode.LeftControl);
    public static bool SkipElimination = false;
    public static bool SkipFindAndObtain = false;
    public static bool SkipFindInRaid = false;
    public static bool SkipLeaveItemAtLocation = false;
    public static bool SkipLocate = false;
    public static bool SkipPlaceBeacon = false;
    public static bool SkipSellItemToTrader = false;
    public static bool SkipSkill = false;
    public static bool SkipSurviveAndExtract = false;
    public static bool SkipTraderLoyalty = false;
    public static bool SkipVisitPlace = false;
    public static bool SkipWeaponAssembly = false;
    public static double ThresholdCurrencyHandover = 1.5;
    public static double ThresholdGPCoinHandover = 0.0;
    public static bool UseHandoverQuestItemsWindow = false;
    public static float WaitForSeconds = 0.3f;
}