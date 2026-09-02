# SPT 4.1 移植源码归档（archive/ported-src）

> 创建：2026-08-07 | 状态：随 M2 移植进度持续更新
> 用途：3.11 -> 4.1 移植**完成后**的源码持久化。只读原始源码在 `../forge/mods/<Name>_<id>_source/`，本目录存**移植修改后**的源码。

## 目录结构

```
archive/ported-src/
├── MANIFEST.md             本文件
└── <Name>_<forgeId>_port/  每个移植 mod 一份（移植后完整源码 + 改过的 csproj；命名与 forge/mods 一致）
```

## 移植清单

| forgeId | mod | 移植日期 | 源码位置 | 部署 overlay | 移植笔记 |
|---------|-----|---------|---------|-------------|---------|
| 898 | KmyTarkovApi（API 库，6 项目） | 2026-08-07 | `KmyTarkovApi_898_port/` | `[1]前置框架-KmyTarkovApi` | `curated/migration/pilot-experience-kmytarkovapi.md` |
| 456 | GamePanelHUD（10 项目 HUD） | 2026-08-07 | `GamePanelHUD_456_port/` | `[11]更多界面信息-GamePanelHUD` | 同上（附 GamePanelHUD 补充） |
| 649 | Virtual's Custom Quest Loader | 2026-08-07 | `Virtuals-Custom-Quest-Loader_649_port/` | `[1]前置框架-自定义任务加载器-Virtual's Custom Quest Loader` | 同上（附 VCQL 补充） |
| 933 | UseLooseLoot | 2026-08-07 | `Use-Loose-Loot_933_port/` | `[8]直接使用未捡物品-UseLooseLoot` | — |
| 934 | SearchOpenContainers | 2026-08-07 | `SearchOpenContainers_934_port/` | `[8]直接搜索已打开容器-SearchOpenContainers` | — |
| 977 | DisableScavMode | 2026-08-07 | `DisableScavMode_977_port/` | `[8]禁用Scav模式-DisableScavMode` | — |
| 1247 | BossNotifier | 2026-08-07 | `BossNotifier_1247_port/` | `[8]boss刷新通知-BossNotifier` | — |
| 1260 | ReachExtender | 2026-08-07 | `Reach-Extender_1260_port/` | `[8]可拾取穿墙物品-ReachExtender` | — |
| 1343 | Skipper | 2026-08-07 | `Skipper_1343_port/` | `[11]任务跳过功能-Skipper` | — |
| 824 | TaskListFixes | 2026-08-08 | `TaskListFixes_824_port/` | `[12]任务列表修复-TaskListFixes` | 零源码改动，仅构建链 |
| 1089 | TraderScrolling | 2026-08-08 | `Kaeno-TraderScrolling_1089_port/` | `[12]商人列表滚动-TraderScrolling` | 零源码改动，仅构建链 |
| 1341 | QuickMoveToContainer | 2026-08-08 | `QuickMoveToContainer_1341_port/` | `[10]物品容器间快速移动-QuickMoveToContainer` | 零源码改动；QuickFindAppropriatePlace 4.1 签名多参，Harmony 按名匹配 |
| 2058 | SPTCorpseCleaner | 2026-08-08 | `SPTCorpseCleaner_2058_port/` | `[12]尸体可删除-SPTCorpseCleaner` | GetActionsClass → InteractionContextHelper 重构 |
| 2115 | HeadshotDamageRedirect | 2026-08-08 | `HeadshotDamageRedirect_2115_port/` | `[6]AI锁头重定向-Headshot Damage Redirection` | DamageInfoStruct→EFT.Ballistics.DamageInfo 等 4 处 |
| 2344 | DynamicItemWeights | 2026-08-08 | `DynamicItemWeights_2344_port/` | `[9]动态物品重量-DynamicItemWeights` | FuelItemClass→Fuel、TemplateId→StringTemplateId 等 |
| 2386 | UseItemsFromAnywhere | 2026-08-08 | `UseItemsAnywhere_2386_port/` | `[8]身上任何物品均可绑定快捷键-UseItemsFromAnywhere` | 零源码改动；门禁 40743 匹配 4.1.2 |
| 2521 | AmandsSense | 2026-08-08 | `AmandsSense_2521_port/` | `[11]物品感应-AmandsSense` | TypeTable→JsonTypes 等 3 处 |
| 1760 | Audio Accessibility Indicators | 2026-08-08 | `Audio-Accessibility-Indicators_1760_port/` | `[11]声音可视化-accessibilityindicators` | PhraseSpeakerClass→BaseSpeaker；门禁 40087→40743 |
| 2162 | Skill Multiplier | 2026-08-08 | `Skill-Multiplier_2162_port/` | `[8]技能速度调节器-SkillMultiplier` | SkillClass→EFT.Skill 等 7 处 |
| 865 | Gilded Key Storage | 2026-08-08 | `Gilded-Key-Storage_865_port/` | `[2]新物品-镀金钥匙容器-GildedKeyStorage客户端文件` | ItemManipulator.Discard 等 4 处；服务端 TS 未移植（见 ticket #9 Known gaps） |
| 2264 | SPT Battle Ambience | 2026-08-08 | `SPTBattleAmbience_2264_port/` | `[8]战场环境临场感增强-SPTBattleAmbience` | TOD_Sky→GameDateTime；Utils→AmbienceUtils |
| 1038 | Trader Modding | 2026-08-08 | `TraderModding_1038_port/`（含 `server/`） | `[7]更好的枪匠界面-Tradermodding客户端文件`+`服务器文件` | 4.x 源码零改动（客户端+服务端 net10+SPTarkov） |
| 954 | Borkel's RNVG | 2026-08-08 | `Borkel-RNVG_954_port/`（含 `server/`） | `[7]平衡的夜视装备视效-BRNVG客户端文件`+`服务器文件` | 4.x 源码零改动（客户端+服务端 net10+SPTarkov） |
| 1923 | LockableDoors | 2026-08-08 | `LockableDoors_1923_port/` | `[8]门可以锁-LockableDoors` | GetActionsClass→InteractionContextHelper；控制台命令剥离；Server 4.0.5 待定 |
| 697 | AlwaysLevelEndurance | 2026-08-08 | `AlwaysLevelEndurance_697_port/` | `[8]超重也能练耐力-Always-Level-Endurance` | GStruct242→MovementParams |
| 858 | BackdoorBandit | 2026-08-08 | `BackdoorBandit_858_port/` | `[8]武器破门-BackdoorBandit客户端文件` | GStruct389→ShotId；GetActionsClass.smethod_9/10→InteractionContextHelper；门禁 29197→40743 |
| 686 | DadGamerMode | 2026-08-08 | `DadGamerMode_686_port/` | `[11]我就是爹作弊工具-DadGamerMode` | InstantProduction 重写；EquipmentClass.method_10→GetTotalWeight；门禁 30626→40743 |
| 701 | FOV-Fix | 2026-08-08 | `FOV-Fix_701_port/` | `[7]瞄准视野大修-FOV-Fix` | FovValuePatch 删除；CameraClass→CameraManager；Realism 兼容层裁剪 |
| 1298 | HandsAreNotBusy | 2026-08-08 | `HandsAreNotBusy_1298_port/` | `[12]防卡手-HandsAreNotBusy` | method_16/17→事件处理器；SpawnController 工厂重建 |
| 1349 | LootRadius | 2026-08-08 | `LootRadius_1349_port/` | `[8]增加拾取距离-LootRadius` | StashGridClass→Grid；GClass3784-88 错误类；ItemContextAbstractClass→ItemContext |
| 1989 | MeaningfulWeaponMasteries | 2026-08-08 | `MeaningfulWeaponMasteries_1989_port/` | `[8]更有意义的武器精通-MeaningfulWeaponMasteries` | GClass2017→WeaponBuffsInfo；Mastering.Level→helper |
| 2248 | SeparateHostility | 2026-08-08 | `SeparateHostility_2248_port/` | `[6]分离敌意-SeparateHostility` | GClass555→BotZoneGroups；AllAlivePlayersList 移除；BotsController.Bots→BotSpawner._bots |
| 972 | MunitionsExpert | 2026-08-08 | `MunitionsExpert_972_port/` | `[9]子弹专家-MunitionsExpert` | Aki.*→SPT.*；**补 BepInEx 壳**（3.11 AKI Main() 4.1 不自动调用） |
| 2148 | StashSearch | 2026-08-08 | `StashSearch_2148_port/` | `[10]仓库搜索框-StashSearch` | StashGridClass→Grid；LootItemClass→CompoundItem；bundle 资源缺口 |
| 2038 | hideoutcat | 2026-08-08 | `hideoutcat_2038_port/` | `[2]新物品-藏身处猫咪-hideoutcat` | GetAvailableHideoutActions→InteractionContextHelper；AssetBundleLoader stub；bundle 资源缺口 |
| 2181 | BeltSlot | 2026-08-08 | `BeltSlot_2181_port/` | `[9]腰带槽-BeltSlot` | 8 个 ItemClass 去后缀；PackNStrap 分支剥离 |
| 1676 | InteractableExfilsAPI | 2026-08-08 | `InteractableExfilsAPI_1676_port/` | `[5]全地图间移动套装-IEAPI+PTT - AI优化版` | GetActionsClass→InteractionContextHelper；公共 API 保持兼容 |
| 2006 | AnimatorUpdater | 2026-08-08 | `AnimatorUpdater_2006_port/` | `[7]武器动画更新器-AnimatorUpdater` | GClass1808-1821→Reload/AddMod 操作；运行时字段名已修 |
| 2213 | VisualAssist | 2026-08-08 | `VisualAssist_2213_port/` | `[11]好莱坞投掷武器视觉辅助-VisualAssist` | method_9→Throw；Class1275/76→High/LowThrowOperation |
| 2238 | TaskAutomation | 2026-08-08 | `TaskAutomation_2238_port/` | `[9]任务交接自动化-TaskAutomation` | QuestClass→Quest；RawQuestClass→QuestTemplate 等 10+ 处 |
| 856 | InventoryOrganizingFeatures | 2026-08-08 | `InventoryOrganizingFeatures_856_port/` | `[10]物品自动分类功能-INVENTORY ORGANIZING FEATURES` | GClass2861→ItemFilterExtension；ISession→IClientSession |
| 1292 | HeliCrash | 2026-08-08 | `HeliCrash_1292_port/` | `[8]坠落的补给直升机-HeliCrash.ArysReloaded` | netstandard2.1 模式；UnityToolkit 依赖需运行时插件（stub 编译） |
| 1657 | MergeConsumables | 2026-08-08 | `MergeConsumables_1657_port/` | `[7]可以合并消耗品-MergeConsumables` | 4.x 源码零改动（客户端+服务端） |
| 861 | MoreCheckmarks | 2026-08-08 | `MoreCheckmarks_861_port/` | `[9]物品打勾标记提供更多信息-MoreCheckmarks` | Profile.Inventory→InventoryInfo；LootItem 不再继承 Item；带 MoreCheckmarksAssets（无扩展名 bundle） |
| 1278 | PackNStrap | 2026-08-08 | `PackNStrap_1278_port/` | `《生活在诺文斯克》腰包小容器-WTT-PackNStrap沉浸感增强设置` | RegisterCustomItemTypesPatch 删除（WTT-ClientCommonLib 2.0.0 替代）；服务端 SPTarkov 4.0.3 |
| 1950 | WeaponCustomizer | 2026-08-08 | `WeaponCustomizer_1950_port/` | `[7]武器配件位置自定义-WeaponCustomizer` | 服务端 4.0→4.1.2 手动升级（IModMetadata/OnLoadAsync/ServiceLocator 移除） |
| 1342 | UIFixes | 2026-08-08 | `UIFixes_1342_port/` | `[10]UI大修-UIFixes` | B 桶最大（150 类型）：90+ global using 映射 + ~200 处修复 + 9 类 patch 删除 + Fika stub |
| 1342 | UI Fixes（UI大修） | 2026-08-08 | `UIFixes_1342_port/` | — | 90+ 全局别名映射（GlobalUsings.cs）；Fika.Core stub（1.3.x API 形状）；服务端 net9.0 直编译成功；9 个 patch 因 4.1.2 重构删除/禁用（详见 UIFixes 移植报告） |
| 1459 | TarkovIRL W.H.M（武器操控大修） | 2026-08-10 | `TarkovIRL_1459_port/` | `[5]武器操控大修-TarkovIRL_W.H.M(关现实主义这个也一起关） - 已AI优化` | DLL 反编译（ilspycmd -r Refs-410）；**RealismMod 依赖整体裁剪**为本地 shim（目标环境无 Realism 4.1）：StanceController/PlayerState/WeaponStats/PluginConfig/GameWorldController 全中性化；GInterface355→IHealthController、IEffect→IHealthEffect、Class1599→PlayerInputTranslator、PlayerPhysicalClass.method_21→Physical.BaseStaminaRestorationFunc、ProcessUpperbodyRotation 补 isAi 参数、Player.Physical 字段化访问；Properties.Settings 删除 |
| 1401 | White Box Fix（白色方块bug修复） | 2026-08-10 | `WhiteBoxFix_1401_port/` | `[12]白色方块bug修复-WhiteBoxFix` | DLL 反编译；`ItemFilter.CheckItem` 签名 `string[]`→`MongoID[]`（MongoID 为 struct，null 判断改 `string.IsNullOrEmpty(s.ToString())`）；`Paths.ExecutablePath`→`BepInEx.Paths.ExecutablePath`（全局顶层 `Paths` 类遮蔽，KB 5.1 陷阱）；门禁 35392→40743；BepInPlugin 3.11.0→4.1.2 |

## 移植工作流（M2 标准）

1. 只读源：`../forge/mods/<Name>_<id>_source/`（Forge 3.11 原始源码）
2. 工作副本：`D:\Temp\opencode\m2-port\{Mod}-src\`（临时，编译用）
3. 编译产物：部署到 MO2 overlay（见 `docs/wayfinder/tickets/009-migrate-3114-to-spt41.md` 的 MO2 overlay 状态跟踪表）
4. **完成后**：工作副本源码复制到本目录 `<Name>_<forgeId>_port/`（排除 bin/obj/Refs，仅源码 + csproj）

## 注意

- 本目录含移植后的源码（可能引用了 4.1 API，不能直接当 3.11 源码用）
- 编译引用：SPT_410 的 Assembly-CSharp.dll + BepInEx（见 `D:\Temp\opencode\m2-port\Refs-410\`，临时）
- 与 `curated/migration/pilot-experience-*.md` 知识文档配合使用
