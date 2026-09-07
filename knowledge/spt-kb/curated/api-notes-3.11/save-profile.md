---
version: [3.11]
domain: server
topic: save-profile
source: curated
---
# 存档（Profile）笔记 [3.11]

> 状态：**已核实（源码实读 2026-09-07）** | 版本：[3.11]
> 源码：`server/project/src/servers/SaveServer.ts`、`callbacks/SaveCallbacks.ts`、`models/eft/profile/ISptProfile.ts`、`services/CreateProfileService.ts`、`helpers/ProfileHelper.ts`

## 存档文件与结构

- 保存路径：`user/profiles/<info.id>.json`（`SaveServer.ts:17` profileFilepath=`"user/profiles/"`）
- `info.id` = accountId（也是 sessionId / 磁盘文件名 `<id>.json` 的键）

`ISptProfile` 顶层（`models/eft/profile/ISptProfile.ts:12-32`）：

| 字段 | 说明 |
|---|---|
| `info` | id(accountId), scavId, aid, username, password, wipe, edition（:39-48） |
| `characters` | `{ pmc: IPmcData, scav: IPmcData }`（:50-53） |
| `suites?` | @deprecated 3.11 移除，服装改存 customisationUnlocks |
| `userbuilds` | `{ weaponBuilds, equipmentBuilds, magazineBuilds }`（:56-60） |
| `dialogues` | Record<string, IDialogue> |
| `spt` | 版本/已装 mods/礼物/迁移时间戳/威望/邪教奖励（:168-186） |
| `vitality` | health + effects |
| `inraid` | IInraid |
| `insurance` | IInsurance[] |
| `traderPurchases?` | Record<string, Record<string, ITraderPurchaseData>> |
| `friends` | string[] |
| `customisationUnlocks` | ICustomisationStorage[]（服装/藏身处墙/地板） |

## 保存流程

`saveProfile(id)`（`SaveServer.ts:191-236`）：
1. 按 sessionId 取/建带超时的 Mutex（5s），加锁（:197-202）
2. **先执行所有 `onBeforeSaveCallbacks`**（`addBeforeSaveCallback` 注册的，:207-218）——回调可修改/返回 profile，异常则回滚到 `previous`
3. `jsonUtil.serialize(profile, !configServer.getConfig(CORE).features.compressProfile)`（:220-224）——**是否压缩取决于 `core.json.features.compressProfile`**（本项目为 false）
4. 算 SHA1，与 `saveSHA1[id]` 对比；**内容无变化则跳过写盘**；有变化才 `fileSystem.write(filePath, jsonProfile)`（:225-231）
5. finally release 锁（:233-235）

`loadProfile(id)`（:171）：读 `${profileFilepath}${id}.json` 入内存；随后执行所有 `SaveLoadRouter.handleLoad()`（:180-182）。

## 自动保存触发（`callbacks/SaveCallbacks.ts`，39 行）

- `onLoad()`（:22）：`backupService.init()` + `saveServer.load()`
- `onUpdate(secondsSinceLastRun)`（:31-38）：**每 15 秒**（`secondsSinceLastRun > coreConfig.profileSaveIntervalSeconds`，值为 15）触发 `saveServer.save()`

间隔值可改：`assets/configs/core.json` 的 `profileSaveIntervalSeconds`（编译版 `SPT_Data/Server/configs/core.json`）。

## SaveServer 公开方法一览（`servers/SaveServer.ts`）

| 方法 | 签名 | 说明 |
|---|---|---|
| `load()` | `async load(): Promise<void>` :54 | ensureDir → 遍历 `*.json` → loadProfile(文件名) |
| `save()` | `async save(): Promise<void>` :74 | 遍历内存 profile，逐个 saveProfile(sessionId) |
| `loadProfile(id)` | `async` :171 | 读盘+SaveLoadRouter.handleLoad |
| `saveProfile(id)` | `async` :191 | 见上 |
| `getProfile(id)` | :98 | 从内存取，找不到抛异常 |
| `getProfiles()` | :124 | `Object.fromEntries(profiles)` |
| `createProfile(info)` | :146 | 建内存空 profile `{info, characters:{pmc:{},scav:{}}}` |
| `addProfile(details)` | :161 | 整档入内存 |
| `deleteProfileById(id)` | :133 | **只删内存，不删磁盘文件** |
| `removeProfile(id)` | :243 | 删内存 + 删磁盘文件 |
| `profileExists(id)` | :116 | |
| `addBeforeSaveCallback(id, cb)` | :38 | 保存前回调 |
| `removeBeforeSaveCallback(id)` | :46 | |

## profile 创建/修复（不在 SaveServer 职责内）

- `services/CreateProfileService.ts`（454 行）：`createProfile(sessionID, info)` :56 —— 从 `databaseService.getProfiles()[edition][side].character` 克隆 `IProfileTemplate` 作为 PMC 模板，填 `_id/aid/savage/sessionId/Nickname`，生成 `stats/quests/seed`，构建完整 `ISptProfile` 并 `saveServer.addProfile()`（:142）
- `helpers/ProfileHelper.ts`（692 行）：`getFullProfile/getPmcProfile/getScavProfile/getProfiles`、`getDefaultSptDataObject()` :194、`getDefaultCounters()` :295、`sanitizeProfileForClient` :106
- `services/ProfileFixerService.ts` 修坏档、`services/ProfileActivityService.ts` 活跃时间戳

## mod 持久化数据方案（**无 ModSaveService / onProfileLoad / onProfileSave**）

grep 全仓库无 `ModSaveService.ts`、无 `onProfileLoad`/`onProfileSave` 字符串。3.11 实际可行的 3 个方案：

| 方案 | 做法 | 适用 |
|---|---|---|
| A. 改内存 profile | `SaveServer.getProfile(id)` 拿可变引用直接改字段 → 随 15s 自动保存写盘 | 往 profile 附加/修改数据 |
| B. 保存前回调 | `SaveServer.addBeforeSaveCallback(id, cb)`，`cb(profile) => Promise<ISptProfile>` 返回写盘前最终 profile | 写盘前注入/清洗（最接近旧 onProfileSave） |
| C. 自管 JSON | mod 自己管理 `user/mods/<name>/**` 下 JSON（FileSystem/JsonUtil 或 node fs），postSptLoad 读、`OnUpdateModService.registerOnUpdate` 周期写 | 独立于 profile 的数据 |

## 其它事实

- **SSO 已移除**：全仓库无 `SSO`/`sso` 引用。账号体系为纯本地：profile 以 `info.id`（accountId）为键存于 `user/profiles/<id>.json`，Launcher 走 `/launcher/*` REST（LauncherController）。
- profile 写盘频率：15s（可配）；SHA1 去重节省 IO；压缩可选（compressProfile）。
- `SaveLoadRouter`（tag `SaveLoadRouter`）：Health/Inraid/Insurance/Profile 四个内建 router，`handleLoad(profile)` 在 loadProfile 时逐条执行。

## 已核实位置

- `servers/SaveServer.ts:17,38,54,74,171,191,220,227` 核心路径与方法
- `callbacks/SaveCallbacks.ts:31-38` 15s 保存
- `models/eft/profile/ISptProfile.ts:12-32` 结构
- `services/CreateProfileService.ts:56,142` / `helpers/ProfileHelper.ts:194,295`
