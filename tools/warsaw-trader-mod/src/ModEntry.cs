using System.Reflection;
using SPTarkov.Common.Models.Logging;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers.Server;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Spt.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Services.Locales;
using SPTarkov.Server.Core.Services.Modding.Custom;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using SPTarkov.Server.Core.Utils.Json;
using Path = System.IO.Path;

namespace SamMeow.WarsawTrader;

/// <summary>
/// 华约商人 mod：注册自定义商人（Voron）+ 上货 + 本地化 + 一个击杀任务。
/// 4.1 表注入模式：TradersTable/TemplateTable/RagfairConfig/TraderConfig 直接构造注入。
/// </summary>
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class WarsawTraderModEntry(
    ISptLogger<WarsawTraderModEntry> logger,
    ModHelper modHelper,
    ImageRouter imageRouter,
    TradersTable tradersTable,
    LocaleTable localeTable,
    RagfairConfig ragfairConfig,
    TraderConfig traderConfig,
    TimeUtil timeUtil,
    ICloner cloner,
    CustomQuestService customQuestService)
    : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        // 1. 加载商人 base 数据
        var traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/base.json");

        // 2. 注册头像路由（图片放 data/voron.jpg，目前用占位图）
        var traderImagePath = Path.Combine(pathToMod, "data/voron.jpg");
        if (File.Exists(traderImagePath))
        {
            imageRouter.AddRoute(traderBase.Avatar.Replace(".jpg", ""), traderImagePath);
        }

        // 3. 设置补货时间（1-2 小时）
        SetTraderUpdateTime(traderBase, timeUtil.GetHoursAsSeconds(1), timeUtil.GetHoursAsSeconds(2));

        // 4. 跳蚤市场可见
        ragfairConfig.Traders.TryAdd(traderBase.Id, true);

        // 5. 注册商人到数据库（空 assort 占位）
        AddTraderWithEmptyAssortToDb(traderBase);

        // 6. 本地化（所有语言）
        AddTraderToLocales(traderBase, "Voron", "Warsaw Pact surplus dealer. Kalashnikovs, ammunition, and honest prices.");

        // 7. 加载并写入 assort
        var assort = modHelper.GetJsonDataFromFile<TraderAssort>(pathToMod, "data/assort.json");
        OverwriteTraderAssort(traderBase.Id, assort);

        // 8. 创建击杀任务
        CreateKillQuest(traderBase.Id);

        logger.Success($"Warsaw Pact Trader loaded: {traderBase.Name} ({traderBase.Id})");
        return Task.CompletedTask;
    }

    private void SetTraderUpdateTime(TraderBase traderBase, int minSeconds, int maxSeconds)
    {
        traderConfig.UpdateTime.Add(new UpdateTime
        {
            TraderId = traderBase.Id,
            Seconds = new MinMax<int>(minSeconds, maxSeconds)
        });
    }

    private void AddTraderWithEmptyAssortToDb(TraderBase traderBase)
    {
        var emptyAssort = new TraderAssort
        {
            Items = [],
            BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
            LoyalLevelItems = new Dictionary<MongoId, int>()
        };

        var trader = new Trader
        {
            Assort = emptyAssort,
            Base = cloner.Clone(traderBase),
            QuestAssort = new Dictionary<string, Dictionary<MongoId, MongoId>>
            {
                { "Started", new() },
                { "Success", new() },
                { "Fail", new() }
            },
            Dialogue = new Dictionary<string, List<string>?>()
        };

        if (!tradersTable.TryAdd(traderBase.Id, trader))
        {
            logger.Warning($"Failed to add trader {traderBase.Id} to TradersTable");
        }
    }

    private void AddTraderToLocales(TraderBase traderBase, string firstName, string description)
    {
        var newTraderId = traderBase.Id.ToString();
        var fullName = traderBase.Name;
        var nickName = traderBase.Nickname;
        var location = traderBase.Location;

        foreach (var (_, localeKvP) in localeTable.Global)
        {
            localeKvP.AddTransformer(lazyloadedLocaleData =>
            {
                lazyloadedLocaleData[$"{newTraderId} FullName"] = fullName;
                lazyloadedLocaleData[$"{newTraderId} FirstName"] = firstName;
                lazyloadedLocaleData[$"{newTraderId} Nickname"] = nickName;
                lazyloadedLocaleData[$"{newTraderId} Location"] = location;
                lazyloadedLocaleData[$"{newTraderId} Description"] = description;
                return lazyloadedLocaleData;
            });
        }
    }

    private void OverwriteTraderAssort(MongoId traderId, TraderAssort newAssort)
    {
        var trader = tradersTable.GetTrader(traderId);
        if (trader is null)
        {
            logger.Warning($"Unable to update assorts for trader: {traderId}, not found");
            return;
        }
        trader.Assort = newAssort;
    }

    private void CreateKillQuest(MongoId traderId)
    {
        var questId = new MongoId("329594f1c922e3edff59c9d5");

        var quest = new Quest
        {
            Id = questId,
            QuestName = "First Contract",
            TraderId = traderId,
            Location = "any",
            Image = "/files/quest/icon/596b465486f77457ca186188.jpg",
            Type = QuestTypeEnum.Elimination,
            Restartable = false,
            Side = "Pmc",
            Name = $"{questId} name",
            Description = $"{questId} description",
            CanShowNotificationsInGame = true,
            AcceptPlayerMessage = $"{questId} acceptPlayerMessage",
            AcceptanceAndFinishingSource = "eft",
            ProgressSource = "eft",
            StartedMessageText = $"{questId} startedMessageText",
            SuccessMessageText = $"{questId} successMessageText",
            FailMessageText = "",
            DeclinePlayerMessage = "",
            CompletePlayerMessage = "",
            ChangeQuestMessageText = "",
            Note = $"{questId} note",
            Status = 0,
            SecretQuest = false,
            InstantComplete = false,
            IsKey = false,
            KeyQuest = false,
            GameModes = ["regular", "pve"],
            RankingModes = [],
            ArenaLocations = [],
            Conditions = new QuestConditionTypes
            {
                AvailableForFinish =
                [
                    new QuestCondition
                    {
                        Id = new MongoId("85b81de879a7e78cb75443fc"),
                        Index = 0,
                        ParentId = "",
                        DynamicLocale = false,
                        Type = "Elimination",
                        ConditionType = "CounterCreator",
                        Value = 10,
                        OneSessionOnly = false,
                        OnlyFoundInRaid = false,
                        IsNecessary = true,
                        IsResetOnConditionFailed = false,
                        DoNotResetIfCounterCompleted = false,
                        VisibilityConditions = [],
                        Counter = new QuestConditionCounter
                        {
                            Id = "31ac1672f5c8739883393d66",
                            Conditions =
                            [
                                new QuestConditionCounterCondition
                                {
                                    Id = new MongoId("4c051824891c5eb59df1c9c9"),
                                    DynamicLocale = false,
                                    ConditionType = "Kills",
                                    Target = new ListOrT<string>(null, "Savage"),
                                    Value = 10,
                                    CompareMethod = ">=",
                                    Weapon = [],
                                    WeaponCaliber = [],
                                    WeaponModsInclusive = [],
                                    WeaponModsExclusive = [],
                                    EnemyEquipmentInclusive = [],
                                    EnemyEquipmentExclusive = [],
                                    EnemyHealthEffects = [],
                                    BodyPart = [],
                                    SavageRole = [],
                                    Distance = new CounterConditionDistance { CompareMethod = ">=", Value = 0 },
                                    Daytime = new DaytimeCounter { From = 0, To = 0 },
                                    ResetOnSessionEnd = false
                                }
                            ]
                        }
                    }
                ],
                AvailableForStart = [],
                Fail = []
            },
            Rewards = new Dictionary<string, List<Reward>>
            {
                { "Started", [] },
                {
                    "Success",
                    [
                        new Reward
                        {
                            Id = new MongoId("acadf3a14417083636845c03"),
                            Type = RewardType.Experience,
                            Value = 2000,
                            IsHidden = false,
                            Unknown = false,
                            GameMode = ["regular", "pve"],
                            AvailableInGameEditions = []
                        },
                        new Reward
                        {
                            Id = new MongoId("74daea786045a4f4bbd27a35"),
                            Type = RewardType.TraderStanding,
                            Target = traderId.ToString(),
                            Value = 0.05,
                            IsHidden = false,
                            Unknown = false,
                            GameMode = ["regular", "pve"],
                            AvailableInGameEditions = []
                        },
                        new Reward
                        {
                            Id = new MongoId("d493cb82a46d287b2641879b"),
                            Type = RewardType.Item,
                            Target = "20dc4578af21cc92b094f65b",
                            Value = 25000,
                            FindInRaid = false,
                            IsEncoded = false,
                            IsHidden = false,
                            Unknown = false,
                            GameMode = ["regular", "pve"],
                            AvailableInGameEditions = [],
                            Items =
                            [
                                new Item
                                {
                                    Id = new MongoId("20dc4578af21cc92b094f65b"),
                                    Template = new MongoId("5449016a4bdc2d6f028b456f"),
                                    Upd = new Upd { StackObjectsCount = 25000 }
                                }
                            ]
                        }
                    ]
                },
                { "Fail", [] }
            }
        };

        var locales = new Dictionary<string, Dictionary<string, string>>
        {
            ["en"] = new()
            {
                [$"{questId} name"] = "First Contract",
                [$"{questId} description"] = "Greetings, comrade. I have surplus Warsaw Pact equipment to move, but I need to know you can handle yourself first. Eliminate 10 Scavs. Any weapon, any location. Prove your worth and we can do business.",
                [$"{questId} successMessageText"] = "Good work. You fight like a professional. The stock is open to you now.",
                [$"{questId} startedMessageText"] = "Eliminate 10 Scavs for Voron.",
                [$"{questId} acceptPlayerMessage"] = "Accepted: First Contract",
                [$"{questId} declinePlayerMessage"] = "Declined: First Contract",
                [$"{questId} completePlayerMessage"] = "Completed: First Contract",
                [$"{questId} note"] = "Kill quest for the Warsaw Pact trader.",
                ["85b81de879a7e78cb75443fc"] = "Eliminate Scavs on any location",
                ["4c051824891c5eb59df1c9c9"] = "Kill Scavs"
            },
            ["ch"] = new()
            {
                [$"{questId} name"] = "第一份合同",
                [$"{questId} description"] = "你好，同志。我有一批华约装备要出手，但得先确认你的实力。干掉 10 个 Scav，任何武器任何地点都行。证明你自己，我们再谈生意。",
                [$"{questId} successMessageText"] = "干得漂亮。你是个行家。我的库存对你开放了。",
                [$"{questId} startedMessageText"] = "为 Voron 干掉 10 个 Scav。",
                [$"{questId} acceptPlayerMessage"] = "已接受：第一份合同",
                [$"{questId} declinePlayerMessage"] = "已拒绝：第一份合同",
                [$"{questId} completePlayerMessage"] = "已完成：第一份合同",
                [$"{questId} note"] = "华约商人的击杀任务。",
                ["85b81de879a7e78cb75443fc"] = "在任何地点消灭 Scav",
                ["4c051824891c5eb59df1c9c9"] = "击杀 Scav"
            },
            ["ru"] = new()
            {
                [$"{questId} name"] = "Первый контракт",
                [$"{questId} description"] = "Здравствуй, товарищ. У меня есть излишки снаряжения Варшавского договора, но сначала мне нужно убедиться, что ты умеешь держать оружие. Устрани 10 диких. Любое оружие, любая локация. Докажи свою состоятельность, и мы сможем вести дела.",
                [$"{questId} successMessageText"] = "Хорошая работа. Ты настоящий профессионал. Склад открыт для тебя.",
                [$"{questId} startedMessageText"] = "Устрани 10 диких для Ворона.",
                [$"{questId} acceptPlayerMessage"] = "Принято: Первый контракт",
                [$"{questId} declinePlayerMessage"] = "Отклонено: Первый контракт",
                [$"{questId} completePlayerMessage"] = "Выполнено: Первый контракт",
                [$"{questId} note"] = "Задание на убийство для торговца Варшавского договора.",
                ["85b81de879a7e78cb75443fc"] = "Устрани диких на любой локации",
                ["4c051824891c5eb59df1c9c9"] = "Убить диких"
            }
        };

        var result = customQuestService.CreateQuest(new NewQuestDetails
        {
            NewQuest = quest,
            Locales = locales
        });

        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                logger.Error($"CreateQuest failed: {error}");
            }
        }
        else
        {
            logger.Success($"Quest 'First Contract' created ({questId})");
        }
    }
}
