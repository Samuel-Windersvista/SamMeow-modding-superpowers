---
version: [5.0]
domain: server
topic: save-profile
source: curated
---
# 存档（Profile）笔记 [5.0]

> **[UNSTABLE-PREVIEW]** SPT 5.0 处于开发初期（`5.0x-dev` 分支，尚无正式版）。本笔记为 2026-09-13 源码快照（HEAD `ff0bf3281`）提炼，上游 API 可能变更；使用前请以源码复核，勿据此做长期承诺。

> 状态：**源码实读（2026-09-13）** | 版本：[5.0]
> 源码：`Libraries/SPTushonka.Server.Core/Servers/SaveServer.cs`、`Callbacks/SaveCallbacks.cs`、`Models/Eft/Profile/SptProfile.cs`、`Migration/`、`Services/Profile/`、`Services/Modding/ProfileDataService.cs`

## 存档文件与内存态

- **存档路径**：`user/profiles/<sessionId>.json`（`SaveServer.cs:31` `profileFilepath = "user/profiles/"`，`:190` 拼 `{sessionID}.json`）。
- **内存态**：`ConcurrentDictionary<MongoId, SptProfile> profiles`（`SaveServer.cs:33`）；另有 `ConcurrentDictionary<MongoId, string> saveMd5`（`:34`）与 `ConcurrentDictionary<MongoId, SemaphoreSlim> saveLocks`（`:35`）。
- `SaveServer` 是 `[Injectable(InjectionType.Singleton)]`（`SaveServer.cs:19`），构造注入 `FileUtil`、`IEnumerable<SaveLoadRouter>`、`JsonUtil`、`HashUtil`、`ProfileMigrationService`、`BackupService`、`ISptLogger<SaveServer>`、`CoreConfig`（`:20-29`）。

## `SaveServer` 公开方法一览

| 方法 | 签名要点 | 位置 |
|------|----------|------|
| `LoadAsync` | 建目录 → 取 `*.json` → 仅 MongoId 文件名 → 逐个 `LoadProfileAsync` | `SaveServer.cs:40-67` |
| `SaveAsync` | 遍历全部内存 profile 逐个 `SaveProfileAsync` | `SaveServer.cs:72-85` |
| `GetProfile` | 内存取；空 sessionId / 无 profile 抛异常 | `SaveServer.cs:93-111` |
| `ProfileExists` | 是否在内存 | `SaveServer.cs:113-116` |
| `GetProfiles` | 返回 `Dictionary<MongoId, SptProfile>` | `SaveServer.cs:122-125` |
| `DeleteProfileById` | **只删内存，不删磁盘** | `SaveServer.cs:132-143` |
| `CreateProfile(Info)` | 内存建空 profile（空 `PmcData`/`ScavData`） | `SaveServer.cs:150-172` |
| `AddProfile(SptProfile)` | 整档入内存 | `SaveServer.cs:178-181` |
| `LoadProfileAsync` | 读盘 → 迁移校验 → 跑 `SaveLoadRouter` | `SaveServer.cs:188-245` |
| `SaveProfileAsync` | 加锁 → 序列化 → MD5 去重 → 写盘 | `SaveServer.cs:253-291` |
| `RemoveProfile` | 删内存 + 删磁盘文件 | `SaveServer.cs:298-311` |
| `IsProfileInvalidOrUnloadable` | 检查 `ProfileInfo.InvalidOrUnloadableProfile` | `SaveServer.cs:320-332` |

### `LoadProfileAsync`（`SaveServer.cs:188-245`）

1. 路径 `user/profiles/{sessionID}.json`（`:190`）；不存在则直接返回（`:191`）。
2. `jsonUtil.DeserializeFromFileAsync<JsonObject>`（`:198`）。
3. `JsonException` → 记 warning，先把原文件复制为 `{sessionID}-corrupt.json`（`:206-207`），再 `backupService.RestoreProfile(sessionID)`（`:209`）；成功则重读，失败抛异常（`:210-217`）。
4. `profiles[sessionID] = profileValidatorService.MigrateAndValidateProfile(profile)`（`:224`）；`InvalidOperationException` 记 critical（`:226-230`）。
5. 无效/不可载入则到此为止（`:235-238`）。
6. 依次执行 `saveLoadRouters` 的 `HandleLoad`（`:241-244`，注释列出 Health/Inraid/Insurance/Profile 四个内建 router）。

### `SaveProfileAsync`（`SaveServer.cs:253-291`）

1. 无效/不可载入 → 返回 0（`:256-259`）。
2. 按 sessionId 取/建 `SemaphoreSlim(1,1)` 并加锁，避免同文件并发写（`:263-264`）。
3. `jsonUtil.Serialize(profiles[sessionID], !coreConfig.Features.CompressProfile)`（`:272-274`）——**`Features.CompressProfile == false` 时不压缩**。
4. `hashUtil.GenerateHashForDataAsync(HashingAlgorithm.MD5, jsonProfile, token)`（`:275`）；与 `saveMd5` 相同则**跳过写盘**，不同才 `fileUtil.WriteFileAsync(filePath, jsonProfile, token)` 并更新 `saveMd5`（`:276-281`）。
5. `finally` 释放锁（`:285-288`）；返回耗时毫秒（`:290`）。

## 写盘时机 / 频率（自动保存）

- 由 `SaveCallbacks` 驱动：`[Injectable(TypePriority = OnLoadOrder.SaveCallbacks)]`（`Callbacks/SaveCallbacks.cs:11`），实现 `IOnLoad, IOnUpdate`（`:19-20`）。
- `OnLoadAsync`（`:22-31`）：
  1. `customItemService.ProfilesLoaded = true`（`:24`）
  2. `databaseIntegrityService.TakeItemSnapshot()`（`:25`）
  3. `await saveServer.LoadAsync(token)`（`:27`）
  4. `await backupService.StartBackupSystem(token)`（`:30`，必须在 Load 之后，避免备份坏档）
- `OnUpdateAsync`（`:33-44`）：
  ```csharp
  if (secondsSinceLastRun < coreConfig.ProfileSaveIntervalInSeconds) return false;
  await saveServer.SaveAsync(cancellationToken);
  return true;
  ```
  - 间隔取自 `CoreConfig.ProfileSaveIntervalInSeconds`（JSON `profileSaveIntervalSeconds`，`Models/Spt/Config/CoreConfig.cs:21-22`）。
  - `IOnUpdate` 循环本身每 **5 秒** tick 一次（`Services/Hosted/SPTStartupHostedService.cs:81`），`secondsSinceLastRun` 为该组件上次成功返回 `true` 以来的秒数（`SPTStartupHostedService.cs:93-101`）。因此实际保存周期 = 配置值（未到则返回 `false`，累积时间）。
  - 配置默认值来自 `SPT_Data/configs/core.json`（源码仓库不含该文件，故默认值需以运行时资产为准）。

## 存档备份（`BackupService`）

`[Injectable(InjectionType.Singleton)]`（`Services/Profile/BackupService.cs:11`）。配置来自 `BackupConfig`（`:17`）。

- 常量：`ProfileDir = "./user/profiles"`（`:21`）、`ActiveModsFilename = "activeMods.json"`（`:22`）。
- `StartBackupSystem`（`:67-105`）：`BackupConfig.BackupInterval.Enabled` 为假时只跑一次 `InitializeAsync`（`:74-80`）；为真时用 `Timer` 按 `BackupInterval.IntervalMinutes` 周期执行（`:82-98`）。
- `InitializeAsync`（`:112-211`）：`BackupCooldown` 节流（`:131-134`）；目标目录 `{Directory}/{yyyy-MM-dd_HH-mm-ss}`（`:137,237-250`）；复制 `user/profiles` 下文件（`:165-177`）；写 `activeMods.json`（活动 mod 的 `Author - Version`，`:180-185,374-384`）；`CleanBackups` 按 `MaxBackups` 删最旧（`:200-205,268-277`）。
- `RestoreProfile(profileId)`（`:391-409`）：找最近备份并覆盖回 `user/profiles`，供 `SaveServer.LoadProfileAsync` 的坏档恢复使用。

## `SptProfile` 顶层结构（`Models/Eft/Profile/SptProfile.cs:12-65`）

| C# 属性 | JSON | 类型 | 位置 |
|---------|------|------|------|
| `ProfileInfo` | `info` | `Info?` | `:14-15` |
| `CharacterData` | `characters` | `Characters?` | `:17-18` |
| `UserBuildData` | `userbuilds` | `UserBuilds?` | `:20-21` |
| `DialogueRecords` | `dialogues` | `Dictionary<MongoId, Dialogue>?` | `:23-24` |
| `SptData` | `spt` | `Spt?` | `:26-27` |
| `InraidData` | `inraid` | `Inraid?` | `:29-30` |
| `InsuranceList` | `insurance` | `List<Insurance>?` | `:32-33` |
| `BtrDeliveryList` | `btrDelivery` | `List<BtrDelivery>?` | `:35-36` |
| `TraderPurchases` | `traderPurchases` | `Dictionary<MongoId, Dictionary<MongoId, TraderPurchaseData>?>?` | `:41-42` |
| `FriendProfileIds` | `friends` | `HashSet<MongoId>?` | `:47-48` |
| `CustomisationUnlocks` | `customisationUnlocks` | `List<CustomisationStorage>?` | `:53-54` |
| `DialogueProgress` | `dialogueProgress` | `List<NodePathTraveled>?` | `:59-60` |
| `PurchasedShopOffers` | `purchasedShopOffers` | `HashSet<string>?` | `:63-64` |

关键子记录：

- `Info`（`:76-102`）：`ProfileId`(`id`)、`ScavengerId`(`scavId`)、`Aid`、`Username`、`IsWiped`(`wipe`)、`Edition`、`InvalidOrUnloadableProfile?`（`internal set`，`:99-101`）。
- `Characters`（`:104-111`）：`PmcData`(`pmc`)、`ScavData`(`scav`)，均 `PmcData`。
- `UserBuilds`（`:116-126`）：`WeaponBuilds`/`EquipmentBuilds`/`MagazineBuilds`。
- `Spt`（`:347-399`）：`Version`、`Mods`(`List<ModDetails>`)、`ReceivedGifts`、`BlacklistedItemTemplates`、`FreeRepeatableRefreshUsedCount`、`TutorialCompleted`、`Migrations`(`Dictionary<string, long>`，迁移时间戳)、`CultistRewards`、`PendingPrestige`、`ExtraRepeatableQuests`。

## Profile 迁移机制（`Migration/`）

### 接口与基类

- `IProfileMigration`（`Migration/IProfileMigration.cs:6-73`）：
  - `string MigrationName`（`:11`）
  - `IEnumerable<Type> PrerequisiteMigrations`（`:16`）
  - `bool CanMigrate(JsonObject, IEnumerable<IProfileMigration>)`（`:24`）+ 带 `ProfileMigrationContext` 的重载（`:33-36`）
  - `JsonObject? Migrate(JsonObject)`（`:44`）+ 带 context 重载（`:52-55`）
  - `bool PostMigrate(SptProfile)`（`:61`）+ 带 context 重载（`:69-72`）
- `AbstractProfileMigration`（`Migration/AbstractProfileMigration.cs:6-55`）：提供默认实现；`GetProfileVersion` 读 `profile["spt"]["version"]`（去空格分段后 `SemanticVersioning.Version.TryParse`，`:42-54`）。
- `ProfileMigrationContext`（`Migration/ProfileMigrationContext.cs:3-21`）：每 profile 一个的 `Dictionary<string, object?>`，`Set<T>/Get<T>` 在同一次迁移的多步间共享数据。
- 迁移类示例：`ThreeElevenToFourZero`（`Migration/Migrations/4.0/ThreeElevenToFourZero.cs:11-90`），`[Injectable]`、`FromVersion="~3.11"`、`ToVersion="4.0"`、`MigrationName="311x-SPTSharp"`、`PrerequisiteMigrations=[typeof(ThreeTenToThreeEleven)]`。

### 排序：拓扑（依赖优先）

`ProfileMigratorExtensions.Sort`（`Extensions/ProfileMigratorExtensions.cs:13-25`）：DFS 遍历 `PrerequisiteMigrations`，保证每个迁移排在其前置之后（`:50-65`）。检测到环抛 `InvalidOperationException("Cycle detected...")`（`:44`）；前置缺失抛 `InvalidOperationException("... depends on missing prerequisite migration ...")`（`:54-56`）。

### 执行：`ProfileMigrationService`

`[Injectable(InjectionType.Singleton)]`（`Services/Profile/ProfileMigrationService.cs:14`），构造注入 `IEnumerable<IProfileMigration>`、`ProfileValidatorHelper`、`TimeUtil`、`ISptLogger`（`:15-20`），并在构造时排序：`_sortedMigrations = profileMigrations.Sort()`（`:22`）。

`MigrateAndValidateProfile(JsonObject profile)`（`:30-134`）：
1. 取 `profileId = profile["info"]["id"]`（`:32`）。
2. **wipe/重置短路**：若 `characters.pmc.Info` 或 `characters.scav.Info` 缺失，或 `info.wipe == true`，直接反序列化为 `SptProfile` 返回，不跑迁移（`:35-43`）。
3. 顺序遍历 `_sortedMigrations`：`CanMigrate` 为真则 `Migrate`，成功则记入 `ranMigrations`（`:49-67`）。
4. 反序列化为 `SptProfile`，并 `profileValidatorHelper.CheckForOrphanedModdedData(profileId, sptReadyProfile)`（`:71-78`）。
5. 失败处理：能反序列化则标记 `InvalidOrUnloadableProfile = true`；完全反序列化失败则造一个最小 mock profile 并标记无效（`:79-108`）。
6. 对每个已运行迁移调 `PostMigrate`；成功则把 `MigrationName → 时间戳` 写入 `SptData.Migrations`（`:111-131`）。

`ProfileValidatorHelper.CheckForOrphanedModdedData`（`Helpers/Profile/ProfileValidatorHelper.cs:33-41`）依次清理：无效物品、无效 userbuilds、无效 dialog、无效服装、无效重复任务、无效 trader purchases；均由 `CoreConfig.Fixes.RemoveModItemsFromProfile` 决定「删除」还是「抛异常」（例 `:67-78`）。

### 已存在的迁移（18 个）

- 3.11：`ThreeTenToThreeEleven`、`ThreeTenMinorFixes`、`HideoutSeed`
- 4.0：`ThreeElevenToFourZero`、`TheVoices`、`RemoveVitaltyFromProfile`、`RemovePassword`、`RemoveGInterfaceFromVictims`、`BuyRestrictionMaxStringToInt`
- 4.1：`FixChatBotAids`、`AddMissingPrestigesToProfile`
- Fixes（7）：`InvalidTerraGroupEmployeeQuestFix`、`InvalidStackObjectsCountFix`、`InvalidSkillProgressFix`、`InvalidRepeatableQuestFix`、`InvalidPocketFix`、`InvalidPmcVoiceFix`、`InvalidPmcHeadFix`

（迁移实现位于 `Migration/Migrations/{3.11,4.0,4.1,Fixes}/`。）

## `ProfileDataService`：mod 自有持久化

`[Injectable(InjectionType.Singleton)]`（`Services/Modding/ProfileDataService.cs:8`），构造注入 `FileUtil`、`JsonUtil`（`:9`）。

- **落盘路径**：`user/profileData/{profileId}/{modKey}.json`（常量 `ProfileDataFilepath = "user/profileData/"`，`:11`；拼接见 `:21,84,117`）。
- **内存缓存**：`ConcurrentDictionary<string, object> _profileDataCache`（`:12`），键格式 `{profileId}:{modKey}`（`GetCacheKey`，`:148-151`）。
- 方法：
  - `ProfileDataExists(profileId, modKey)`（`:19-22`）
  - `GetAllProfileDataAsync(MongoId, token)` → `Dictionary<string, object>`（`:29-74`；逐个 json 读，命中缓存则用缓存）
  - `GetProfileDataAsync<T>(MongoId, modKey, token)`（`:76-100`；缓存未命中则读盘并回填）
  - `SaveProfileDataAsync<T>(MongoId, modKey, data, token)`（`:102-118`；序列化后先更新缓存再写盘）
  - `ClearProfileData(MongoId)`（`:124-143`；删该 profile 目录下所有文件并清缓存）
- 与 `SaveServer` 的区别：`ProfileDataService` 存 mod 独立数据（与整档分离，不随 `SaveServer` 的 15s/配置周期保存，由 mod 自己决定何时调用）；`SaveServer` 管完整 `SptProfile`。

## 4.1 → 5.0 的存档/输出模型差异

- `ProfileChange`（`Models/Eft/ItemEvent/ItemEventRouterBase.cs:35-114`）新增/存在字段：`ChangedHideoutStashes`（`:74-75`）、`CompletableItems`（`:88-89`）、`ReadQuestData`（`:91-92`）、`NewQuestNotes`（`:94-95`）、`VariableValues`（`:97-98`）、`SeasonalRewards`（`:100-101`）、`BattlePassProgress`（`:106-107`）、`BattlePassUniversalDocumentBalance`（`:103-104`）；`TraderData` 增 `DialogueAvailable`（`:202-203`）。直接构造/序列化 `ProfileChange` 的 mod 需补字段。
- `SaveServer` 方法名与职责与 4.1 一致；5.0 差异集中在新增系统的输出字段（赛季/通行证/商店/教程）。
- 存档迁移链在 5.0 保留 3.11/4.0/4.1 迁移（见上），说明 5.0 仍可加载旧档并逐步升级。

## 已核实位置

- `Servers/SaveServer.cs:19-35,40-85,93-143,150-181,188-245,253-291,298-332`
- `Callbacks/SaveCallbacks.cs:11-44`
- `Models/Spt/Config/CoreConfig.cs:21-22`
- `Services/Hosted/SPTStartupHostedService.cs:81,93-101`
- `Services/Profile/BackupService.cs:11-22,67-105,112-211,237-250,268-277,374-384,391-409`
- `Models/Eft/Profile/SptProfile.cs:12-65,76-111,116-126,347-399`
- `Migration/IProfileMigration.cs:6-73`、`AbstractProfileMigration.cs:6-55`、`ProfileMigrationContext.cs:3-21`
- `Services/Profile/ProfileMigrationService.cs:14-134`
- `Extensions/ProfileMigratorExtensions.cs:13-66`
- `Helpers/Profile/ProfileValidatorHelper.cs:16-41,67-78`
- `Services/Modding/ProfileDataService.cs:8-151`
- `Models/Eft/ItemEvent/ItemEventRouterBase.cs:35-114,185-204`
