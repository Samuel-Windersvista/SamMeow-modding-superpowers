using Comfort.Common;
using EFT;
using EFT.Quests;
using EFT.Trading;
using EFT.UI;
using HarmonyLib;
using SPT.Reflection.Patching;
using SPT.SinglePlayer.Utils.InRaid;
using System;
using System.Linq;
using System.Reflection;
using TaskAutomation.Helpers;
using TaskAutomation.MonoBehaviours;

namespace TaskAutomation.Patches.Screens
{
    internal class InventoryScreen_Show : ModulePatch
    {
        private static Type conditionChecker;
        private static Type dailyTaskType;
        private static Type itemsProvider;
        private static MethodInfo itemsProviderMethod;

        protected override MethodBase GetTargetMethod()
        {
            conditionChecker = AccessTools.GetTypesFromAssembly(typeof(AbstractGame).Assembly)
                    .SingleOrDefault(t => t.GetEvent("OnConditionQuestTimeExpired", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) != null);
            conditionChecker = conditionChecker.MakeGenericType(typeof(Quest));

            itemsProvider = AccessTools.GetTypesFromAssembly(typeof(AbstractGame).Assembly)
                    .SingleOrDefault(t => t.GetMethod("GetItemsForCondition", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance) != null);
            itemsProvider = itemsProvider.MakeGenericType(typeof(Quest));
            LogHelper.LogInfo($"{itemsProvider}");
            itemsProviderMethod = itemsProvider.GetMethod("GetItemsForCondition", BindingFlags.Public | BindingFlags.Static);
            LogHelper.LogInfo($"{itemsProviderMethod}");
            dailyTaskType = AccessTools.GetTypesFromAssembly(typeof(QuestTemplate).Assembly).SingleOrDefault(this.isDailyTaskType);
            return AccessTools.FirstMethod(typeof(InventoryScreen), this.IsTargetMethod);
        }

        [PatchPostfix]
        private static void PatchPostfix(InventoryScreen __instance, object questController)
        {
            if (RaidTimeUtil.HasRaidLoaded()
                || questController is not QuestController abstractQuestController)
                return;
            if (Globals.Debug)
                LogHelper.LogInfo($"Found abstractQuestController.");
            var sessionField = AccessTools.Field(typeof(InventoryScreen), "_backEndSession");
            ITradingSession profileEndpointFactory = sessionField.GetValue(__instance) as ITradingSession;
            if (Globals.Debug)
                LogHelper.LogInfo($"Found profileEndpointFactory {profileEndpointFactory.GetType()}.");
            Singleton<UpdateMonoBehaviour>.Instance.SetReflection(conditionChecker, itemsProviderMethod, dailyTaskType, profileEndpointFactory);
            Singleton<UpdateMonoBehaviour>.Instance.SetAbstractQuestController(abstractQuestController);
        }

        private bool isDailyTaskType(Type type)
        {
            Type rawQuestType = typeof(QuestTemplate);
            return type != rawQuestType
                && type.BaseType == rawQuestType
                && type.GetProperty("ExpirationTime") != null;
        }

        private bool IsTargetMethod(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            return method.Name == nameof(InventoryScreen.Show)
                && parameters.Length != 1;
        }
    }
}