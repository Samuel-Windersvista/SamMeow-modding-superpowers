using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using EFT.Quests;
using EFT.UI;
using HarmonyLib;
using SPT.Common.Utils;
using SPT.SinglePlayer.Utils.InRaid;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using TaskAutomation.Helpers;
using TMPro;
using UnityEngine;
using static EFT.Profile;

#nullable enable

namespace TaskAutomation.MonoBehaviours
{
    internal class UpdateMonoBehaviour : MonoBehaviour
    {
        private const string GPCOINTEMPLATEID = "5d235b4d86f7742e017bc88a";
        private readonly List<MongoID> declinedHandoverItemConditions = new List<MongoID>();
        private AbstractQuestControllerClass? abstractQuestController;
        private CancellationToken? cancellationToken;
        private CancellationTokenSource? cancellationTokenSource;
        private Type? conditionChecker;
        private Type? dailyQuestType;
        private bool didInvestigate = false;
        private MethodInfo? itemsProviderMethod;
        private MongoID lastConditionHandoverItemId = MongoID.Generate();
        private DateTime? lastRun = null;
        private FieldInfo? openFieldInfo;
        private ProfileEndpointFactoryAbstractClass? profileEndpointFactory;

        private Coroutine? runningCoroutine;
        private GClass3834? windowContext;

        public void SetAbstractQuestController(AbstractQuestControllerClass abstractQuestController)
        {
            this.abstractQuestController = abstractQuestController;
            if (Globals.Debug)
                LogHelper.LogInfo($"SetAbstractQuestController");
            this.startCoroutine();
        }

        public void SetReflection(Type conditionChecker, MethodInfo itemsProviderMethod, Type dailyQuistType, ProfileEndpointFactoryAbstractClass profileEndpointFactory)
        {
            this.conditionChecker = conditionChecker;
            this.itemsProviderMethod = itemsProviderMethod;
            this.dailyQuestType = dailyQuistType;
            this.profileEndpointFactory = profileEndpointFactory;
            this.openFieldInfo = typeof(HandoverQuestItemsWindow).GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.NonPublic).FirstOrDefault(fi => fi.Name == "Open");
            LogHelper.LogInfo($"{this.openFieldInfo}");
            if (Globals.Debug)
                LogHelper.LogInfo($"SetReflection");
        }

        public void UnsetAbstractQuestController()
        {
            this.abstractQuestController = null;
            this.stopCoroutine();
        }

        public void Update()
        {
            bool shouldInvestigate = Globals.Debug && Globals.InvestigateKeys.IsPressed();
            if (this.didInvestigate != shouldInvestigate)
            {
                this.didInvestigate = shouldInvestigate;
                if (shouldInvestigate)
                    this.investigate();
            }
            if (this.abstractQuestController == null)
                return;
            if (hasRaidLoaded())
            {
                this.cancellationTokenSource?.Cancel();
                this.UnsetAbstractQuestController();
                return;
            }
            if (Globals.ResetDeclinedHandoverItemConditionsKeys.IsPressed() == false)
                return;
            if (hasRaidLoaded())
                return;
            if (this.lastRun == null)
                return;
            if (Globals.Debug)
                this.investigate();

            this.lastRun = null;
            this.cancellationTokenSource?.Cancel();
            this.stopCoroutine();

            this.lastConditionHandoverItemId = MongoID.Generate();
            this.declinedHandoverItemConditions.Clear();
            LogHelper.LogInfoWithNotification($"Declined HandoverItem reset.");
            this.startCoroutine();
        }

        private static bool hasRaidLoaded()
        {
            return RaidTimeUtil.HasRaidLoaded()
                && Singleton<AbstractGame>.Instance?.GameType != EGameType.Hideout;
        }

        private static bool isQuestThatFailsByQuest(QuestClass quest, string target)
        {
            if (quest.RawQuestClass.Conditions.ContainsKey(EQuestStatus.Fail) == false)
                return false;
            var failconditions = quest.RawQuestClass.Conditions[EQuestStatus.Fail].IEnumerable_0;
            bool canFail = failconditions.Any();
            if (canFail == false)
                return false;
            foreach (Condition? condition in failconditions)
            {
                if (condition is ConditionQuest conditionQuest
                    && conditionQuest.target == target)
                    return true;
            }
            return false;
        }

        private void completeCondition(AbstractQuestControllerClass abstractQuestController, QuestClass quest, Condition condition)
        {
            MongoID id = condition.id;
            if (quest.IsConditionDone(condition))
                return;
            quest.ProgressCheckers[condition].SetCurrentValueGetter(_ => condition.value);
            FieldInfo conditionControllerFieldInfo = abstractQuestController.GetType().GetFields().FirstOrDefault(fi => fi.FieldType == this.conditionChecker);
            if (conditionControllerFieldInfo == null)
                return;
            var conditionController = conditionControllerFieldInfo.GetValue(abstractQuestController);
            MethodInfo setConditionCurrentValueMethodInfo = AccessTools.DeclaredMethod(conditionController.GetType().BaseType, "SetConditionCurrentValue");
            if (setConditionCurrentValueMethodInfo == null)
                return;
            setConditionCurrentValueMethodInfo.Invoke(conditionController, new object[] { quest, EQuestStatus.AvailableForFinish, condition, condition.value, true });
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification($"Skipped: {condition.FormattedDescription} for {quest.RawQuestClass.Name}.");
        }

        private IEnumerator coroutine()
        {
            this.cancellationTokenSource = new CancellationTokenSource();
            this.cancellationToken = cancellationTokenSource.Token;
            while (true)
            {
                if (Globals.Debug)
                    LogHelper.LogInfo($"Started new run.");
                yield return new WaitForSeconds(Globals.WaitForSeconds);
                this.lastRun = DateTime.Now;
                if (this.cancellationToken?.IsCancellationRequested == true)
                    yield break;
                if (this.abstractQuestController == null)
                    yield break;
                if (Globals.Debug)
                    LogHelper.LogInfo($"abstractQuestController not null.");
                if (Globals.Debug)
                    LogHelper.LogInfo($"Not in a raid.");
                var allQuests = this.abstractQuestController.Quests.Where(this.isNotFinished);
                if (Globals.Debug)
                {
                    LogHelper.LogInfo($"Handle started quests {allQuests.Count()}/{this.abstractQuestController.Quests.Count}.");
                    foreach (EQuestStatus questStatus in Enum.GetValues(typeof(EQuestStatus)))
                    {
                        LogHelper.LogInfo($" - {questStatus.ToString()} {allQuests.Count(quest => quest.QuestStatus == questStatus)}");
                        //foreach (var quest in allQuests.Where(quest => quest.QuestStatus == questStatus))
                        //LogHelper.LogInfo($"  * {quest.RawQuestClass.Name}");
                    }
                }
                IEnumerable<QuestClass> startedQuests = allQuests.Where(this.isStarted);
                foreach (QuestClass startedQuest in startedQuests)
                {
                    try
                    {
                        if (this.cancellationToken?.IsCancellationRequested == true)
                            yield break;
                        if (this.shouldHandleQuest(startedQuest, allQuests))
                            this.handleQuest(this.abstractQuestController, startedQuest);
                        else if (Globals.Debug)
                            LogHelper.LogInfo($"blocked quest {startedQuest.RawQuestClass.Name}.");
                    }
                    catch (Exception exception)
                    {
                        LogHelper.LogExceptionToConsole(exception);
                    }
                    yield return new WaitForSeconds(Globals.WaitForSeconds);
                }
                yield return new WaitForSeconds(Globals.WaitForSeconds);
                //FinishQuests
                if (Globals.AutoCompleteQuests)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo($"Complete quests");
                    List<string> questsReadyToFinish = this.getIdsReadyToComplete();
                    foreach (string id in questsReadyToFinish)
                    {
                        if (this.cancellationToken?.IsCancellationRequested == true)
                            yield break;
                        try
                        {
                            QuestClass? questToComplete = this.getQuestById(id);
                            if (questToComplete == null)
                                continue;
                            if (Globals.Debug)
                                LogHelper.LogInfo($"AvailableForFinish {questToComplete.RawQuestClass.Name}");
                            this.abstractQuestController.FinishQuest(questToComplete, true);
                            LogHelper.LogInfoWithNotification($"Completed: {questToComplete.RawQuestClass.Name}");
                        }
                        catch (Exception exception)
                        {
                            LogHelper.LogExceptionToConsole(exception);
                        }
                        yield return new WaitForSeconds(Globals.WaitForSeconds);
                    }
                }
                //StartQuests
                if (Globals.AutoAcceptQuests)
                {
                    if (Globals.Debug)
                        LogHelper.LogInfo($"Start quests");
                    List<string> questsReadyToStart = this.getIdsReadyToStart();
                    foreach (string id in questsReadyToStart)
                    {
                        if (this.cancellationToken?.IsCancellationRequested == true)
                            yield break;
                        try
                        {
                            QuestClass? questToStart = this.getQuestById(id);
                            if (questToStart == null)
                                continue;
                            if (Globals.AutoAcceptScavQuests == false
                                && this.abstractQuestController.IsQuestForCurrentProfile(questToStart) == false)
                                continue;
                            if (Globals.Debug)
                                LogHelper.LogInfo($"AvailableForStart {questToStart.RawQuestClass.Name}, json={Json.Serialize<RawQuestClass>(questToStart.RawQuestClass)}");
                            this.abstractQuestController.AcceptQuest(questToStart, true);
                            LogHelper.LogInfoWithNotification($"Accepted: {questToStart.RawQuestClass.Name}");
                        }
                        catch (Exception exception)
                        {
                            LogHelper.LogExceptionToConsole(exception);
                        }
                        yield return new WaitForSeconds(Globals.WaitForSeconds);
                    }
                }
                if (this.cancellationToken?.IsCancellationRequested == true)
                    yield break;
                //FailedQuests
                if (Globals.Debug)
                    LogHelper.LogInfo($"Check for failed quest.");
                allQuests = this.abstractQuestController.Quests;
                QuestClass failedQuest = allQuests.FirstOrDefault(this.isMarkedAsFailed);
                if (failedQuest != null)
                {
                    try
                    {
                        if (this.abstractQuestController.IsQuestForCurrentProfile(failedQuest) == false)
                            continue;
                        if (Globals.Debug)
                            LogHelper.LogInfo($"FailConditional {failedQuest.RawQuestClass.Name}");
                        this.abstractQuestController.FailConditional(failedQuest);
                    }
                    catch (Exception exception)
                    {
                        LogHelper.LogExceptionToConsole(exception);
                    }
                    yield return new WaitForSeconds(Globals.WaitForSeconds);
                }
            }
        }

        private List<string> getIdsReadyToComplete()
        {
            if (this.abstractQuestController == null)
                return [];
            QuestBookClass quests = this.abstractQuestController.Quests;
            IEnumerable<QuestClass> questsReadyToFinish = quests.Where(quest => this.isReadyToFinish(quest, quests));
            return questsReadyToFinish.Select(quest => quest.Id).ToList();
        }

        private List<string> getIdsReadyToRestart()
        {
            if (this.abstractQuestController == null)
                return [];
            QuestBookClass quests = this.abstractQuestController.Quests;
            IEnumerable<QuestClass> questsReadyToFinish = quests.Where(this.isMarkedAsFailRestartable);
            return questsReadyToFinish.Select(quest => quest.Id).ToList();
        }

        private List<string> getIdsReadyToStart()
        {
            if (this.abstractQuestController == null)
                return [];
            QuestBookClass quests = this.abstractQuestController.Quests;
            IEnumerable<QuestClass> questsReadyToStart = quests.Where(this.isReadyToStart);
            return questsReadyToStart.Select(quest => quest.Id).ToList();
        }

        private int getItemCount(string templateId)
        {
            if (this.abstractQuestController == null)
                return 0;
            int count = 0;
            IEnumerable<Item> items = this.abstractQuestController.Profile.Inventory.GetAllItemByTemplate(templateId);
            foreach (Item item in items)
                count += item.StackObjectsCount;
            return count;
        }

        private Item[] getItemsAllowedToHandover(double take, Item[] result)
        {
            return result.Where(item => this.isAllowToHandover(item, take)).Take((int)take).ToArray();
        }

        private QuestClass? getQuestById(string id)
        {
            if (this.abstractQuestController == null)
                return null;
            QuestBookClass quests = this.abstractQuestController.Quests;
            return quests.FirstOrDefault(quest => quest.Id == id);
        }

        /// <summary>
        /// Handle quest for automation
        /// </summary>
        /// <returns>True if the sequence should run again.</returns>
        private bool handleQuest(AbstractQuestControllerClass abstractQuestController, QuestClass quest)
        {
            if (quest.QuestStatus != EQuestStatus.Started)
                return false;
            if (this.isHandoverQuestItemsWindowOpen())
                return false;
            if (this.lastConditionHandoverItemIsDeclined())
                this.declinedHandoverItemConditions.Add(this.lastConditionHandoverItemId);
            foreach (Condition condition in quest.NecessaryConditions)
            {
                if (this.cancellationToken?.IsCancellationRequested == true)
                    return false;
                if (Globals.Debug)
                    LogHelper.LogInfo($" - {condition.GetType()} {condition.FormattedDescription} IsNecessary:{condition.IsNecessary} Done:{quest.IsConditionDone(condition)}");
                if (quest.IsConditionDone(condition))
                    continue;

                if (condition is ConditionHandoverItem conditionHandoverItem)
                {
                    bool isRecentlyDeclined = this.declinedHandoverItemConditions.Contains(conditionHandoverItem.id);
                    if (isRecentlyDeclined)
                    {
                        if (Globals.Debug)
                            LogHelper.LogInfo($"ConditionHandoverItem: {conditionHandoverItem.id} recently declined.");
                        continue;
                    }
                    if (Globals.SkipFindAndObtain && conditionHandoverItem.onlyFoundInRaid == false)
                    {
                        this.completeCondition(abstractQuestController, quest, condition);
                    }
                    else if (Globals.SkipFindInRaid && conditionHandoverItem.onlyFoundInRaid)
                    {
                        this.completeCondition(abstractQuestController, quest, condition);
                    }
                    else if ((Globals.AutoHandoverFindInRaid && conditionHandoverItem.onlyFoundInRaid == true)
                          || (Globals.AutoHandoverObtain && conditionHandoverItem.onlyFoundInRaid == false))
                    {
                        ConditionProgressChecker conditionProgressChecker = quest.ProgressCheckers[condition];
                        double handoverValue = 0;
                        double currentValue = conditionProgressChecker.CurrentValue;
                        double expectedValue = condition.value;
                        if (currentValue < expectedValue)
                            handoverValue = expectedValue - currentValue;
                        Item[]? result = this.itemsProviderMethod?.Invoke(null, new object[] { abstractQuestController.Profile.Inventory, condition }) as Item[];
                        if (result == null || result.Length == 0)
                            continue;
                        if (this.isBlockedCurrency(result.FirstOrDefault(), (int)handoverValue))
                            continue;
                        handoverValue = result.Length;
                        if (Globals.Debug)
                            LogHelper.LogInfo($"{quest.RawQuestClass.Name} HandoverItem(s): currentValue={currentValue}, expectedValue={expectedValue}, handoverValue={result.Length} done={quest.IsConditionDone(condition)} test={conditionProgressChecker.Test()}");
                        if (handoverValue == 0)
                            continue;
                        if (this.shouldShowHandoverQuestItemsWindow(quest, conditionHandoverItem, result) == false)
                        {
                            result = this.getItemsAllowedToHandover(take: handoverValue, result);
                            return this.handoverItems(abstractQuestController, handoverValue, result, quest, conditionHandoverItem);
                        }
                        result = this.getItemsAllowedToHandover(take: 100, result);
                        this.showHandoverQuestItemsWindow(abstractQuestController, quest, conditionHandoverItem, result, currentValue, handoverValue);
                        return true;
                    }
                }
                else if (condition is ConditionWeaponAssembly conditionWeaponAssembly)
                {
                    if (Globals.SkipWeaponAssembly)
                        this.completeCondition(abstractQuestController, quest, condition);
                    else if (Globals.BlockTurnInWeapons == false)
                    {
                        IEnumerable<Item> playerItems = abstractQuestController.Profile.Inventory.GetPlayerItems(EPlayerItems.NonQuestItemsExceptHideoutStashes);
                        Item[] weapons = Inventory.GetWeaponAssembly(playerItems, conditionWeaponAssembly).ToArray();
                        if (weapons == null || weapons.Length == 0)
                            continue;
                        abstractQuestController.HandoverItem(quest, conditionWeaponAssembly, weapons, runNetworkTransaction: true);
                        LogHelper.LogInfoWithNotification($"HandoverItem(s): {quest.RawQuestClass.Name}");
                    }
                }
                else if (condition is ConditionFindItem conditionFindItem)
                {
                    if (Globals.SkipFindAndObtain && conditionFindItem.onlyFoundInRaid == false)
                        this.completeCondition(abstractQuestController, quest, condition);
                    if (Globals.SkipFindInRaid && conditionFindItem.onlyFoundInRaid)
                        this.completeCondition(abstractQuestController, quest, condition);
                }
                else if (condition is ConditionCounterCreator conditionCounterCreator)
                {
                    if (Globals.SkipElimination && conditionCounterCreator.type == RawQuestClass.EQuestType.Elimination)
                        this.completeCondition(abstractQuestController, quest, condition);
                    else if (Globals.SkipVisitPlace && conditionCounterCreator.type == RawQuestClass.EQuestType.Exploration)
                        this.completeCondition(abstractQuestController, quest, condition);
                    else if (Globals.SkipVisitPlace && conditionCounterCreator.type == RawQuestClass.EQuestType.Discover)
                        this.completeCondition(abstractQuestController, quest, condition);
                    else if (Globals.SkipSkill && conditionCounterCreator.type == RawQuestClass.EQuestType.Experience)
                        this.completeCondition(abstractQuestController, quest, condition);
                    else if (Globals.SkipSurviveAndExtract && conditionCounterCreator.type == RawQuestClass.EQuestType.Completion)
                        this.completeCondition(abstractQuestController, quest, condition);
                    else if (Globals.Debug)
                        LogHelper.LogInfo($"ConditionCounterCreator: {conditionCounterCreator.type} not handled.");
                }
                else if (condition is ConditionVisitPlace conditionVisitPlace)
                {
                    if (Globals.SkipVisitPlace)
                        this.completeCondition(abstractQuestController, quest, condition);
                }
                else if (condition is ConditionLeaveItemAtLocation conditionLeaveItemAtLocation)
                {
                    if (Globals.SkipLeaveItemAtLocation)
                        this.completeCondition(abstractQuestController, quest, condition);
                }
                else if (condition is ConditionTraderLoyalty conditionTraderLoyalty)
                {
                    if (Globals.SkipTraderLoyalty)
                        this.completeCondition(abstractQuestController, quest, condition);
                }
                else if (condition is ConditionPlaceBeacon conditionPlaceBeacon)
                {
                    if (Globals.SkipPlaceBeacon)
                        this.completeCondition(abstractQuestController, quest, condition);
                }
                else if (condition is ConditionSkill conditionSkill)
                {
                    if (Globals.SkipSkill)
                        this.completeCondition(abstractQuestController, quest, condition);
                }
                else if (condition is ConditionSellItemToTrader conditionSellItemToTrader)
                {
                    if (Globals.SkipSellItemToTrader)
                        this.completeCondition(abstractQuestController, quest, condition);
                }
                else if (Globals.Debug)
                    LogHelper.LogInfo($"ConditionType: {condition.GetType()} not handled.");
            }
            return false;
        }

        private bool handoverItems(AbstractQuestControllerClass abstractQuestController, double handoverValue, Item[] items, QuestClass quest, ConditionHandoverItem conditionHandoverItem)
        {
            if (items.Length == 0)
                return false;
            abstractQuestController.HandoverItem(quest, conditionHandoverItem, items, runNetworkTransaction: true);
            LogHelper.LogInfoWithNotification($"HandoverItem(s): {quest.RawQuestClass.Name}");
            return true;
        }

        private void investigate()
        {
            TimeSpan? dtm = DateTime.Now - this.lastRun;
            LogHelper.LogInfoWithNotification($"TA: Last run: {dtm?.Seconds ?? -1} seconds ago.");
            if (hasRaidLoaded())
            {
                EGameType? gameType = Singleton<AbstractGame>.Instance?.GameType;
                LogHelper.LogErrorWithNotification($"TA: Appears to have raid loaded of type: {gameType}");
            }
            if (this.abstractQuestController == null)
                LogHelper.LogErrorWithNotification("TA: No abstractQuestController");
            if (this.cancellationToken?.IsCancellationRequested == true)
                LogHelper.LogErrorWithNotification("TA: CancellationRequested");

            LogHelper.LogInfoWithNotification($"TA: QuestController: {this.abstractQuestController?.Quests.Count ?? 0} quests.");
        }

        private bool isAllowToHandover(Item item, double handoverValue)
        {
            return this.isInEquipmentSlot(item) == false
                && this.isBlockedWeapon(item) == false
                && this.isPartOfWeaponOrArmor(item) == false
                && this.isBlockedCurrency(item, (int)handoverValue) == false
                && this.isFilledCompoundItem(item) == false
                && this.isFilledWithPlates(item) == false;
        }

        private bool isBlockedCurrency(Item item, int handoverValue)
        {
            if (item is not MoneyItemClass moneyItemClass)
                return false;
            else if (Globals.BlockTurnInCurrency)
                return true;
            int itemCount = this.getItemCount(item.TemplateId);
            if (item.TemplateId == GPCOINTEMPLATEID)
                handoverValue = (int)(handoverValue * Globals.ThresholdGPCoinHandover);
            handoverValue = (int)(handoverValue * Globals.ThresholdCurrencyHandover);
            if (Globals.Debug)
                LogHelper.LogInfo($"count: {itemCount}, expected: {handoverValue}");
            return itemCount < handoverValue;
        }

        private bool isBlockedWeapon(Item item)
        {
            if (Globals.BlockTurnInWeapons == false)
                return false;
            else if (item is Weapon)
                return true;
            return false;
        }

        private bool isEquipmentSlot(ItemAddress itemAddress)
        {
            if (itemAddress == null)
                return false;
            if (Globals.Debug)
                LogHelper.LogInfo($"ItemAddress {itemAddress.Container.ID} {itemAddress.GetType()}");
            string containerId = itemAddress.Container.ID.ToLower();
            if (containerId == null)
                return false;
            else if (containerId == "firstprimaryweapon"
                 || containerId == "secondprimaryweapon"
                 || containerId == "headwear"
                 || containerId == "earpiece"
                 || containerId == "facecover"
                 || containerId == "eyewear"
                 || containerId == "tacticalvest"
                 || containerId == "armorvest"
                 || containerId == "backpack"
                 || containerId == "pocket1"
                 || containerId == "pocket2"
                 || containerId == "pocket3"
                 || containerId == "pocket4"
                 || containerId == "pocket5"
                 || containerId == "pocket6"
                 || containerId == "armband"
                 || containerId == "holster"
                 || containerId == "scabbard"
                 || containerId == "securedcontainer")
                return true;
            return false;
        }

        private bool isFilledCompoundItem(CompoundItem compoundItem)
        {
            return compoundItem.Containers.Any(c => c.Items.Any());
        }

        private bool isFilledCompoundItem(Item item)
        {
            if (Globals.Debug)
                LogHelper.LogInfo($"itemtype: {item.GetType()}");
            if (item.IsContainer
             && item is CompoundItem container
             && this.isFilledCompoundItem(container))
                return true;
            return false;
        }

        private bool isFilledWithPlates(Item item)
        {
            if (item.Components.Any(this.isFilledWithPlates))
                return true;
            return false;
        }

        private bool isFilledWithPlates(IItemComponent itemComponent)
        {
            if (itemComponent is not ArmorHolderComponent armorHolderComponent)
                return false;
            return armorHolderComponent.MoveAbleArmorSlots.Any(slot => slot.ContainedItem is ArmorPlateItemClass armorPlateItemClass && armorPlateItemClass.Armor.ArmorClass > Globals.BlockTurnInArmorPlateLevelHigherThan);
        }

        private bool isHandoverQuestItemsWindowOpen()
        {
            if (this.openFieldInfo == null)
                return false;
            HandoverQuestItemsWindow handoverItemsWindow = ItemUiContext.Instance.HandoverQuestItemsWindow;
            if (handoverItemsWindow == null)
                return false;
            return (bool)this.openFieldInfo.GetValue(handoverItemsWindow);
        }

        private bool isInEquipmentSlot(Item item)
        {
            if (this.isEquipmentSlot(item.CurrentAddress))
                return true;
            return false;
        }

        private bool isMarkedAsFailed(QuestClass quest)
        {
            return quest.QuestStatus == EQuestStatus.MarkedAsFailed;
        }

        private bool isMarkedAsFailRestartable(QuestClass quest)
        {
            return Globals.AutoRestartFailedQuests
                && quest.QuestStatus == EQuestStatus.FailRestartable;
        }

        private bool isNotFinished(QuestClass quest)
        {
            return quest.QuestStatus != EQuestStatus.Success;
        }

        private bool isPartOfWeaponOrArmor(Item item)
        {
            if (Globals.Debug)
                LogHelper.LogInfo($"WeaponOrArmor: check {item.LocalizedName()} ");
            if (item.CurrentAddress?.Container is Slot slot
                && (slot.ParentItem is Weapon || slot.ParentItem is ArmoredEquipmentItemClass))
            {
                if (Globals.Debug)
                    LogHelper.LogInfo($"WeaponOrArmor: {item.Id} SlotParentItemType: {slot.ParentItem.GetType()}");
                return true;
            }
            return false;
        }

        private bool isQuestThatFailsOtherTasks(QuestClass quest, IEnumerable<QuestClass> quests)
        {
            string questId = quest.RawQuestClass.Id;
            return quests?.Any(quest => isQuestThatFailsByQuest(quest, questId)) == true;
        }

        private bool isReadyToFinish(QuestClass quest, QuestBookClass allQuests)
        {
            string traderId = quest.RawQuestClass.TraderId;
            return this.isUnlockedTrader(traderId)
                && quest.QuestStatus == EQuestStatus.AvailableForFinish
                && (Globals.AutoHandleQuestsThatFailOther
                || this.isQuestThatFailsOtherTasks(quest, allQuests) == false);
        }

        private bool isReadyToStart(QuestClass quest)
        {
            return (quest.QuestStatus == EQuestStatus.AvailableForStart
                || this.isMarkedAsFailRestartable(quest))
                && this.shouldAcceptQuestThatCanFail(quest)
                && this.shouldAcceptDailyQuists(quest)
                && this.isUnlockedTrader(quest.RawQuestClass.TraderId);
        }

        private bool isStarted(QuestClass quest)
        {
            return quest.QuestStatus == EQuestStatus.Started;
        }

        private bool isUnlockedTrader(string traderId)
        {
            if (this.abstractQuestController == null)
                return false;
            if (this.abstractQuestController.Profile.TryGetTraderInfo(traderId, out var traderInfo) == false)
                return false;
            bool shouldBlockLightKeeper = Globals.AcceptLightKeeperOutOfRaid == false && traderId == TraderInfo.LIGHT_KEEPER_TRADER_ID;
            if (shouldBlockLightKeeper)
                return false;
            bool shouldBlockBTR = Globals.AcceptBTROutOfRaid == false && traderId == TraderInfo.BTR_TRADER_ID;
            if (shouldBlockBTR)
                return false;
            return traderInfo.Unlocked;
        }

        private bool lastConditionHandoverItemIsDeclined()
        {
            return this.windowContext != null && this.windowContext.Gparam_0 == false;
        }

        private bool shouldAcceptDailyQuists(QuestClass quest)
        {
            if (Globals.AutoAcceptDailyQuests)
                return true;
            return quest.RawQuestClass.GetType() != this.dailyQuestType;
        }

        private bool shouldAcceptQuestThatCanFail(QuestClass quest)
        {
            if (quest.RawQuestClass.Conditions.ContainsKey(EQuestStatus.Fail) == false)
                return true;
            var failconditions = quest.RawQuestClass.Conditions[EQuestStatus.Fail];
            bool canFail = failconditions.IEnumerable_0.Any(condition => condition is not ConditionQuest);
            if (canFail == false)
                return true;
            else if (Globals.AutoAcceptQuestsThatCanFail)
                return true;
            return false;
        }

        private bool shouldHandleQuest(QuestClass quest, IEnumerable<QuestClass> allQuests)
        {
            return Globals.AutoHandleQuestsThatFailOther
                || this.isQuestThatFailsOtherTasks(quest, allQuests) == false;
        }

        private bool shouldShowHandoverQuestItemsWindow(QuestClass quest, ConditionHandoverItem conditionHandoverItem, Item[] items)
        {
            if (Globals.UseHandoverQuestItemsWindow)
                return true;
            if (conditionHandoverItem.onlyFoundInRaid == false)
                return false;
            string? checkTemplateId = null;
            foreach (Item item in items)
            {
                string templateId = item.TemplateId;
                if (checkTemplateId == null)
                    checkTemplateId = templateId;
                else if (checkTemplateId != templateId)
                    return true;
            }
            return false;
        }

        private void showHandoverQuestItemsWindow(AbstractQuestControllerClass abstractQuestController, QuestClass quest, ConditionHandoverItem conditionHandoverItem, Item[] result, double currentValue, double handoverValue)
        {
            string traderId = quest.RawQuestClass.TraderId;
            TraderClass? trader = this.profileEndpointFactory?.GetTrader(traderId);
            if (trader == null)
                return;
            TraderControllerClass? traderController = trader.TraderController;
            if (traderController == null)
                return;
            HandoverQuestItemsWindow handoverItemsWindow = ItemUiContext.Instance.HandoverQuestItemsWindow;
            this.lastConditionHandoverItemId = conditionHandoverItem.id;
            this.windowContext = handoverItemsWindow.Show(conditionHandoverItem, currentValue, result, abstractQuestController.Profile, traderController, (items) =>
            {
                this.handoverItems(abstractQuestController, handoverValue, items, quest, conditionHandoverItem);
            }, canShowCloseButton: true);
            string text = ((TMP_Text)handoverItemsWindow.Caption).text;
            text = text.Replace("to trader", $"to {trader.LocalizedName}");
            text += $" for quests: {quest.RawQuestClass.Name}";
            ((TMP_Text)handoverItemsWindow.Caption).text = text;
        }

        private void startCoroutine()
        {
            if (this.runningCoroutine == null)
            {
                this.runningCoroutine = this.StartCoroutine(this.coroutine());
                if (Globals.Debug)
                    LogHelper.LogInfoWithNotification("Started coroutine");
            }
        }

        private void stopCoroutine()
        {
            if (this.runningCoroutine != null)
                this.StopCoroutine(this.runningCoroutine);
            this.runningCoroutine = null;
            if (Globals.Debug)
                LogHelper.LogInfoWithNotification("Stopped coroutine");
        }
    }
}