---
version: [4.1]
domain: server
topic: operations
source: curated
---
# SPT 4.1.5 (SP-Tushonka) 服务端源码审查报告

> 状态：8 路并行只读审查完成（989 文件 / 约 10.7 万行），关键 Critical 已人工交叉验证
> 审查对象：`E:\云文件\GitHub\SamMeow_SP-Tushonka_source_code`（commit `7d7add55`，tag `4.1.5`）
> 方法：codemap（架构）→ 按目录 7 路 oracle 审查（Services / Helpers / Generators+Callbacks / Controllers+Routers+Servers+Migration / Models / Extensions+Utils+DI+Loaders / Web+启动器+库）→ 交叉验证 → 本报告

---

## 1. 架构总览（Codemap 提炼）

**一句话**：.NET 10 单进程整体式架构，核心是**自研反射 DI 容器**（`[Injectable(TypePriority)]` 注解驱动扫描注册），`TypePriority` 同时决定依赖装配顺序与 `IOnLoad` 启动回调顺序。

**启动链**：
```
Program.Main
└─ StartServer
   ├─ ConfigLoader.Initialize        # 扫 SPT_Data/configs/*.{json,jsonc} 强类型反序列化（8 个自定义 JsonConverter）
   ├─ CreateEarlySptProvider         # 一次性 WebApplication 只为拿 ModLoader/ModValidator 依赖
   ├─ ModLoader.RunModLoader         # enum prepatch 内存补丁 + 隔离宿主(PrepatchLoadContext) + 遍历 user/mods/*.dll 读 IModMetadata
   └─ StartServerAfterModLoading
      ├─ DatabaseImporter.LoadDatabaseAsync   # XxHash3 校验 checks.dat + 递归灌库
      ├─ CreateNewHostBuilder                 # DatabaseTables 10 张表单例注册
      ├─ RegisterSptServicesAsync             # 扫描 Program/PatchManager/SPTWeb + mod 程序集注入 DI
      ├─ ConfigureKestrel                     # TLS1.2/1.3 自签名证书
      └─ app.RunAsync
```

**请求链**：
```
Kestrel → UseWebSockets → SptLoggerMiddleware → /health → HttpServer.HandleRequestAsync
  → SptHttpListener (ZLib 解压) → HttpRouter → Router(Static/Dynamic) → Callbacks → Controller
  → Service/Helper → 内存表/SaveServer → HttpResponseUtil 包装 → ISerializer 特判 → ZLib 压缩回写
```

**数据流**：
- 内存数据库：`ImporterUtil.LoadRecursiveAsync` 递归灌 JSON → `DatabaseTables`（Bots/Hideout/Locales/Locations/Match/Templates/Traders/Globals/Server/Settings 十张单例表）
- Profile：`SaveServer` 用 `ConcurrentDictionary<MongoId, SptProfile>` 内存主副本 + `SaveLoadRouter` 钩子 + `Migration` 迁移链 + MD5 写盘去重

**关键接口**：`[Injectable]`（DI 注解）、`IOnLoad`/`IOnUpdate`（启动/周期钩子，5s 一循环）、`Router`/`StaticRouter`/`DynamicRouter`（URL→RouteAction 绑定）、`ISerializer`（响应特判）、`IOnDIConstruct`（mod DI 钩子）。

完整架构图见 `curated/api-notes-4.1/architecture-map.md`。

---

## 2. Critical 发现（按主题归类）

### 2.1 移植语义失真（TS→C# 翻译错误）—— 最高危，静默改行为、极难察觉

这是本次审查最大的一类问题：逐行翻译 JS→C# 时丢失 `!`、丢失 `else`、抄错字符串、误用 API。每个都是单点小改、用户可感知的功能损坏。

| 位置 | 问题 | 修法 |
|---|---|---|
| `Extensions/TraderAssortExtensions.cs:47` | **黑名单缺 `!`**：`RemoveItemsFromAssort` 保留的是黑名单物品、删除全部正常商品。玩家档案含黑名单物品时 trader 库存只剩被禁商品 | `Contains` 前加 `!`（已人工验证） |
| `Generators/Loot/LootGenerator.cs:566-572` | 密封武器箱奖励池**三处谓词错误**（transplant 移植：缺 `!`、逻辑反转）：① `IsItemBlacklisted` 缺 `!` → 池只保留黑名单物品；② `!(AllowBossItems \|\| IsBossItem)` → AllowBossItems=true 时恒 false 池空；③ `QuestItem is null` → items.json 实测 0 个 null（false 4429 / true 148），恒 false 池空。**三个叠加导致武器箱奖励池永远近似空**。修法：`!IsItemBlacklisted && (AllowBossItems \|\| !IsBossItem) && QuestItem != true`（已复核，db 数据佐证） |
| `Services/Bot/BotEquipmentFilterService.cs:241-248` | 黑名单分支 `Value.Clear()` 清空的是保存的同一引用 → 凡配置装备黑名单的槽位全部装备被清空 | 仿 whitelist 分支改 `Value = [];`（替换引用） |
| `Services/Profile/ProfileFixerService.cs:185-209` | `ContainsKey` 条件反了（上游是 `!hasOwnProperty`）：**有效**外观被重置、**无效**的保留 | 加 `!` |
| `Services/Commerce/FenceService.cs:753-755` | 值元组 `(int current,int max)` 副本 `value.current += 1` 修改丢失 → 类型限量永远为 0 | 写回字典 |
| `Services/Server/SeasonalEventService.cs:724` | LINQ `Append` 惰性返回值被丢弃 → 敌意设置从未添加 | `Concat([settings]).ToList()` |
| `Services/Bot/BotLootCacheService.cs:267` | drink 白名单误读 `Items.Food.Whitelist`（应为 `.Drink`） | 改 `.Drink` |
| `Services/Commerce/MailSendService.cs:373` | 条件反转：`Count == 0 ? ... : null`，有 events 时丢弃 | 改 `Count is > 0 ? ... : null` |
| `Services/Bot/PmcChatResponseService.cs:148` | JS 正则字面量 `"/(%giftcode%)/gi"` 当 C# pattern → 占位符永不替换 | 改 `"%giftcode%"` + `RegexOptions.IgnoreCase` |
| `Generators/RepeatableQuests/RepeatableQuestRewardGenerator.cs:392` | 无条件 `break`（上游是 `else { break; }`）→ 任务奖励恒 1 件，`RewardNumItems` 失效 | break 移入 else 分支 |
| `Generators/Ragfair/RagfairOfferGenerator.cs:703` | `GetDouble(Max.Min, Max.Min)` 两参数都是 `.Min` → maxMultiplier 恒最小值 | 改 `.Max.Max` |
| `Generators/Bot/BotLootGenerator.cs:789` | 防死循环保护 `currentLimitCount > currentLimitCount * 10` 恒假 | 与 `globalLimitCount * 10` 比较 |
| `Generators/Bot/PMCLootGenerator.cs:123` | backpack 池误用 `"vest"` 价格覆盖 | 改 `"backpack"` |
| `Generators/Weather/WeatherGenerator.cs:132` | Unix **秒**当 DateTime **ticks**（`new DateTime(long)` 语义）→ 恒得公元 1 年、温度恒取 Night 权重 | `DateTimeOffset.FromUnixTimeSeconds(...).DateTime` |
| `Services/Commerce/GiftService.cs:134` | `x ?? x` 同值死表达式 | 改 `LocaleTextId ?? MessageText` |

### 2.2 NOTIFY 通知链路整体失效（三层叠加）

- `Helpers/Server/HttpServerHelper.cs:82-91`：`SendTextJson` **空实现**（`output` 参数被忽略，留 `// TODO`）——长轮询最终走这个空方法，客户端收到 `200 + 空 body`，HTTP 通知通道完全失效。
- `Routers/Serializers/NotifySerializer.cs:30-33`：`.ContinueWith` 链式把 `Task<Task<IEnumerable<string>>>` 当 text 传入。
- `Controllers/NotifierController.cs:28-56`：`Task.Factory.StartNew` + `Thread.Sleep(300)` 阻塞线程池 15s，取消 token 运行中无效。
- `Callbacks/NotifierCallbacks.cs:29-42`：`SendNotification` 是死代码（无调用方），但与 Serializer 是两份不一致的重复实现，正确的那份是死的。

**影响**：`/notifierServer/get/{sessionId}` 通知通道不可用（WS 通道正常）。这是游戏会话每次都命中的路径。

### 2.3 反序列化崩溃面（整库加载失败）

- `Models/Eft/Common/Tables/TemplateItem.cs:21,41,105,134` 等 **8 处** `string.Intern(value)` setter 未判空：JSON 含 `null` 时 `string.Intern(null)` 抛 `ArgumentNullException` → **整个 JSON 文档反序列化失败**（物品/任务数据库加载崩溃、mod 物品包无法启动）。同文件 `Type` setter 已判空，属遗漏（已人工验证）。

### 2.4 安全漏洞

- `SPTushonka.Server/Program.cs:249-256`：`UseForwardedHeaders` 设 `ForwardLimit = null` + 清空 `KnownProxies` → 无条件信任任意客户端 `X-Forwarded-For`。当 `httpConfig.Ip` 绑 LAN 地址时，外部客户端可伪造 `127.0.0.1` 绕过 `RequireCredentialsOnLocalhost=false` 免凭据入口，以默认管理员身份进 Web 面板。
- `Libraries/SPTushonka.Server.Web/Services/ConfigEditorService.cs:262-283,311-335`：配置预设 `Id`（用户可手工编辑 `user/config-presets/*.json`）未经校验进 `GetPresetFilePath` → `DeletePreset` 可删除任意 `*.json`、`SavePreset` 可覆盖任意 `*.json`（路径遍历）。

### 2.5 编译错误（经本地编译复核后撤销）

- ~~`Services/InRaid/AirdropService.cs:123` 编译错误~~ → **误报，已撤销**。本地 `dotnet build` = 0 错误、官方 CI build = success、4.1.5 已发布。真相：`lootResult.AddRange(itemAndChildren)` 命中 .NET 的 `CollectionExtensions.AddRange<T>(List<T>, ReadOnlySpan<T>)` 单元素 span 重载（IL 实证），语义等价于 `Add`，编译通过且运行时正确。此条不作为 bug 提交。

### 2.6 数据完整性 / 排序语义

- `Models/Common/MongoId.cs:292-296`：`CompareTo` 字节序反转，不保时间戳单调序——`SortedDictionary`/`OrderBy`/二分查找结果错乱。
- `Extensions/ContainerExtensions.cs:89-139`：`TryFillContainerMapWithItem` 失败时已写入格不回滚，容器地图留下半填充损坏状态。
- `Utils/ProbabilityObjectArray.cs:84-87`：`Drop` 强制 `(ProbabilityObjectArray<K,V>)this.Where(...)` 必然 `InvalidCastException`（public API，任何 mod 调用即炸）。

---

## 3. Major 发现汇总（按类别）

### 并发 / 无锁共享状态（TS 单线程→C# 多线程的真实隐患）

| 位置 | 问题 |
|---|---|
| `Services/Server/NotificationService.cs:10` | 普通 `Dictionary` 被长轮询线程与请求线程并发读写，可能损坏/抛异常 |
| `Services/Commerce/InsuranceService.cs:38` | `Insured` 普通字典多 session 并发写竞态 |
| `Services/InRaid/RaidWeatherService.cs:23` | `WeatherForecast` 普通 List，RemoveAll 与枚举并发抛集合修改异常 |
| `Servers/Ws/SptWebSocketConnectionHandler.cs:158-162,213-236` | send-gate 释放竞态，OnClose Dispose 后其它线程 `gate.Release()` 抛 ObjectDisposedException |
| `Helpers/Profile/HandbookHelper.cs:108-133` | Singleton + `??=` 惰性缓存并发写 Dictionary |
| `Helpers/Ragfair/RagfairOfferHelper.cs:309-320` | 批量 `Task.Factory.StartNew` + `WaitAll` + `OfferCounter++` 非原子 → InternalId 重复 |
| `Libraries/SPTushonka.DI/DependencyInjectionHandler.cs:224-251` | 泛型单例缓存 key 不含泛型参数 → 不同参数类型实例复用/错配 |

### 健壮性（坏数据/坏存档拖垮启动）

| 位置 | 问题 |
|---|---|
| `Services/Profile/ProfileMigrationService.cs:49-67` | 迁移循环在 try 外，任一迁移抛非 InvalidOperationException 异常 → **单个坏存档导致整个服务器启动失败** |
| `Servers/SaveServer.cs:222-244` | 坏 profile catch 后继续走回调，`GetProfile` 抛 "no profile found" 继续传播 |
| `Helpers/Items/ItemHelper.cs:763-785` | `SplitStack` 中 `maxStackSize ?? 0` 为 0 时 `remainingCount -= 0` 永不变 → **死循环**挂死请求线程 |
| `Helpers/Items/ItemHelper.cs:1256-1291` | `AddCartridgesToAmmoBox` 同模式死循环 |
| `Helpers/Profile/HideoutHelper.cs:1044-1077` | `Slots[i].Items?[0]` 空列表 `[0]` 抛异常 |
| `Helpers/Ragfair/RagfairOfferHelper.cs:354-364` | `SortOffers(...)[0]` 空列表越界 |
| `Helpers/Ragfair/RagfairOfferHelper.cs:460-469` | `is null` 应为 `is not null`，购买上限过滤整体失效 |
| `Helpers/Ragfair/RagfairOfferHelper.cs:55` | ~locale key "5bdabfb886f7743e152e867e 0" 含空格 → NRE~ **误报已复核**：locale 数据库（SPT_Data/database/locales/global/*.json）所有语言文件的 key 均带 `" 0"` 后缀（EFT 邮件模板约定），常量正确。真实问题仅为 `GetLocalisedOfferSoldMessage` 的 TryGetValue 失败只 log 不 return → 后续 `.Replace()` 可能 NRE（防御缺失，低触发概率，属忠实移植 TS 行为） |
| `Helpers/Ragfair/RagfairServerHelper.cs:30` | ~locale key 含空格，邮件空白~ **误报已复核**：`goodsReturnedTemplate` 常量正确（locale key 存在），仅作 template id 透传，无 bug |
| `Helpers/Dialogue/.../GiveSptCommand.cs:43` | `[Injectable]` 默认 Transient 却承载两步交互状态 → `spt give` 编号选择永远失败 |
| `Controllers/QuestController.cs:193` | `int.Parse("2.5")` 抛 FormatException → 500 |

### 性能（热路径）

| 位置 | 问题 |
|---|---|
| `Services/InRaid/LocationLifecycleService.cs:221` | 每次 raid 开始请求路径 `GC.Collect`（阻塞式全量压缩） |
| `Helpers/Commerce/TradeHelper.cs:42` | `static readonly Lock` 包裹深克隆+I/O，所有玩家购买全局串行 |
| `Helpers/Dialogue/.../DisplaySkillNamesHandler.cs:40` | `Thread.Sleep(500)` 同步阻塞 ~5.5s |
| `Services/Locales/AbstractLocalisationService.cs:111` | locale key miss 线性 `FirstOrDefault`（上游 O(1)） |
| `Services/Commerce/PaymentService.cs:494-499,433-442` | O(n²) 排序比较器 + 递归线性查找 |
| `Utils/Json/Converters/EftEnumConverter.cs:41-59` | 每次序列化枚举都反射扫字段 |

### 枚举序列化策略混乱

- `EftEnumConverter.Write` 反射判断永不命中 → 所有枚举一律输出数字，与客户端协议字符串期望不一致；读方向靠 `Enum.Parse(ignoreCase:true)` 兜底暂不炸，但读写不对称是定时炸弹。`EftListEnumConverter` 静态 `_options` 里 `JsonStringEnumConverter` 注册在前遮蔽了 Factory，导致 List 内枚举写字符串、独立枚举写数字，格式不一致。78 处无属性级 converter 的枚举属性受影响。

### 启动/Mod 加载

| 位置 | 问题 |
|---|---|
| `SPTushonka.Server/Modding/ModValidator.cs:127 vs 238-241` | `_skippedMods` 存 ModGuid、查询用 `{Author}-{Name}`，永不匹配 |
| `SPTushonka.Server/Modding/ModValidator.cs:154-178` | `IsModCompatibleWithSpt` 注释与实现矛盾：一个 mod 版本失配 → 全部 mod 不加载 |
| `SPTushonka.Server/Modding/ModValidator.cs:203` | `sptCoreAsmRefVersion[..^2]` 字符串截断脆弱 |
| `Libraries/SPTushonka.Reflection/Patching/PatchManager.cs:171-223` | AutoPatch `DisablePatches` 必然失败（新实例 `_harmony` 非同一引用） |
| `SPTushonka.Server/Middleware/SptLoggerMiddleware.cs:38-50` | catch 后不写响应不 rethrow → 异常吞掉返回 200 空 body |
| `Libraries/SPTushonka.Server.Core/Servers/HttpServer.cs:35-37` | `PHPSESSID` cookie 未校验直接构造 MongoId，非法值抛异常被吞 |
| `Libraries/SPTushonka.Common/Logger/LogFileRollManager.cs:44-47` | `Task.Factory.StartNew(async () => ...)` 反模式，worker 无引用、CTS 提前 dispose |

---

## 4. 最值得修的 10 件事（按性价比排序）

1. **TraderAssortExtensions.cs:47 加 `!`**（critical，一行修复恢复商店库存）
2. **LootGenerator.cs 三连谓词反转 + RepeatableQuest 无条件 break + RagfairOfferGenerator `.Min,.Min`**（critical，都是单行移植笔误，恢复奖励系统）
3. **修 8 处未判空的 `string.Intern` setter**（critical，一行一个判空，消灭整库反序列化崩溃面）
4. **重写 NOTIFY 链路**（critical，补 SendTextJson body + NotifySerializer 改 await + NotifierController 改 Task.Delay，一次修通通知通道）
5. **收紧 UseForwardedHeaders + 校验 preset Id**（critical 安全，堵住 LAN 认证绕过与路径遍历）
6. **ProfileMigrationService 迁移循环加 try/catch + SaveServer catch 后 return**（major，保证任何坏存档不阻断启动）
7. **ItemHelper 三处死循环加"上限为 0 提前返回"守卫**（major，消灭请求线程挂死）
8. **NotificationService/InsuranceService/RaidWeatherService 换并发集合**（major，多线程真实隐患）
9. **统一枚举序列化策略**（major，`Enum.GetUnderlyingType` 缓存 + 统一字符串/数字输出，核对 78 处协议字段）
10. **修 RagfairOfferHelper/RagfairServerHelper 的 locale key 空格 + TraderAssortExtensions 缺 `!`**（critical，卖出结算 NRE + 商店库存清空）

---

## 5. 质量总体评价

- **架构**：整体式 + 自研反射 DI，设计意图清晰，prepatch 双宿主隔离、Argon2id 凭据、XxHash3 校验是高于社区平均的工程实践。
- **最大风险**：**移植语义失真**（至少 15 处 `!`/`else`/字符串/API 翻译错误）——这类 bug 静默改行为，比 NRE 更危险；其次是 **TS 单线程→C# 多线程的并发假设失效**（多处无锁共享状态）；以及 **log-then-continue** 的无效防御反复出现（判 null 只记日志不 return）。
- **S.P.E.C.I.A.L. 综合**：Strength（性能）C — 阻塞 GC/全局锁/O(n²)/同步 Sleep；Perception（错误处理）C — 通知通道静默失败、SptLoggerMiddleware 吞异常返回空 200；Endurance（可靠性）D — 单坏档可拖垮启动；Charisma B；Intelligence B — 忠实移植但语义对照不严；Agility B；Luck C — 竞态窗口小但无兜底。
- **整体判断**：大规模 TS→C# 移植的典型中间态代码，移植完整度约 85%（35% 模型无引用、大量 TODO 残留），功能可运行但存在多处静默损坏点。

> [CAUTION] 本报告为只读审查，未修改任何源码文件。审查中有 1 处误报已人工复核撤销（AirdropService.cs:123 编译错误 → 实为 .NET CollectionExtensions span 重载，见 §2.5）。提交 PR 前其余结论仍建议逐条复核。

--- END OF TERMINAL ENTRY ---
