# 侦察发现：LLM 全自动 TS->C# 转换可行性评估（SPT 3.11.4 -> 4.1）

> 状态：侦察完成（2026-08-05）| 目的：wayfinder 后续票的输入素材
> 素材（实读）：Expanded-Task-Text_2153_source（ETT 3 个 TS 文件）、A-核心服务端/server/project/src（3.11 服务端源码，DatabaseService/StaticRouterModService 等）、SamMeow_SPT410_source_code（4.1 源码，LocaleTable/DI/OnLoadOrder/HttpRouter/Controllers）、curated/api-notes-4.1/（7 篇）、curated/recipes/（11 篇）、curated/modding-guide/、wiki/SPT_41/Server_40_to_41.md（迁移文档）、docs/feasibility-311-to-41-migration.md、templates/server-mod/、Life_in_Norvinsk_v0.3.2/mods/（194 目录全量扫描）
> 环境事实：4.1 服务器已装于 `E:\Game\EFT_Offline\SPT_410\SPT_Runtime\`（SPT.Server.exe + SPTarkov.*.dll + user/mods 空）；3.11 安装于 `E:\Game\EFT_Offline\SPT_3114\`

---

## 0. 一句话结论

**自动转换可行，但不是"TS 翻译成 C#"——是"把 3.11 代码当作行为规格，用 4.1 配方模板生成 C#"**。真正可自动化的是**结构层（DI 骨架、表注入、入口、日志、配置）**，真正不可自动化的是**语义层（locale 直写、运行时枚举扩展、类扩展 override、assort 数据形状）**。全自动率的现实上限：简单 mod 约 70-85%，全包（91 个 TS mod）约 50-60%，且"编译通过"只证明 API 用对，不证明行为等价——行为验证需要一套独立于转换器的可观测性设施（见 §5）。

---

## 1. 自动化能力评估：什么能转、什么不能转

### 1.1 实测：3.11 mod 的真实结构（ETT，Expanded-Task-Text_2153_source）

3 个 TS 文件暴露了全部 3.11 常见模式：

| 3.11 模式 | 4.1 等价物 | 可自动化程度 |
|---|---|---|
| `module.exports = { mod: new DExpandedTaskText() }` | `IModMetadata` record + `[Injectable]` class | **全自动**（模板生成） |
| `implements IPostDBLoadMod, IPreSptLoadMod` | `IOnLoad.OnLoadAsync(CancellationToken)` + `TypePriority` | **全自动**（机械映射，见迁移文档第 2 节） |
| `postDBLoad(container)` / `preSptLoad(container)` | `OnLoadAsync(ct)`，阶段用 `OnLoadOrder.PostLoad + 1` | **全自动**（阶段常量映射） |
| `container.resolve<DatabaseServer>("DatabaseServer").getTables()` | 构造函数注入 `TemplateTable` / `TradersTable` 等 | **全自动**（表注入映射表已存在，见 §1.3） |
| `InstanceManager` 模式（集中 resolve 一堆服务） | 构造函数多参数注入，无需 InstanceManager | **全自动**（整个类可删除，机械） |
| `database.templates.quests` / `database.traders` / `database.locales.global` | `templateTable.Quests` / `tradersTable` / `localeService`（**注意**：4.1 `LocaleTable.Global` 懒加载，直改不保存） | **结构自动 + 语义需人工**（见 §3 阻断器 1） |
| `configServer.getConfig<T>(ConfigTypes.X)` | 注入具体配置类型 `RagfairConfig` 等 | **全自动**（config 类型映射表） |
| `vfs.read(file)` / `JSON.parse` | `modHelper.GetJsonDataFromModFile<T>`（4.1 ModHelper 支持读自己 mod 文件夹） | **全自动**（模板化 JSON 加载） |
| `logger.log(msg, LogTextColor.GREEN)` | `logger.Success(...)` / `LogWithColor(msg, Color.Green)` | **全自动**（日志 API 映射） |
| `StaticRouterModService.registerStaticRouter(...)` | `AbstractRouter` 子类 + `RouteAction<TRequest>`（body 强类型） | **全自动**（迁移文档第 5 节有完整对照） |
| `CustomItemService` / `imageRouter.addRoute(...)` | 4.1 `CustomItemService` / `ImageRouter`（命名空间变了） | **全自动**（命名空间映射表） |
| `Traders[traderId] = traderId`（运行时扩展枚举） | **无等价物**（4.1 客户端枚举靠 prepatcher，服务端不扩展） | **阻断**（见 §3 阻断器 2） |
| `database.locales.global[lang][key] = text`（写本地化） | 4.1 需走 `LocaleService`，但 LocaleService 源码**只有 getter 无 setter** | **阻断**（见 §3 阻断器 1，ETT 整个 mod 的核心就是这个） |
| JSON 数据文件（assort/base/quests/db/） | **原样搬运**——4.1 mod 文件夹直接放 JSON，`ModHelper` 读 | **100% 自动**（数据不参与翻译！） |
| `Object.keys(x).forEach(...)` / 递归遍历 | LINQ / 递归（C# 直接改写） | **自动**（纯逻辑翻译） |
| `Record<string, ITrader>` 索引访问 | `TradersTable : Dictionary<MongoId, Trader>` | **自动 + 类型注意**（MongoId 隐式转换） |
| `ITrader.assort.items[i]._tpl` 深层链 | 4.1 模型字段同名（`_tpl` 等保留） | **自动**（EFT 模型字段 3.11/4.1 一致） |
| `performance.now()` 计时 | `Stopwatch` / `DateTime.UtcNow` | **自动** |

### 1.2 关键实证：assort/base JSON 是可搬运资产（最重要的发现）

AES 商人 mod（3 TS + 9 个 JSON + 3 张图）的实际构成：
- `anastasiaAssort.json` 121KB、`svetlanaAssort.json` 237KB —— **纯数据**
- `AES.ts` 只有 89 行，核心逻辑 = 注册 3 个商人、设更新时间、写 locales、把 base+assort 塞进 `tables.traders`

也就是说**商人 mod 的 90% 内容是数据文件，翻译器只需要翻译那 89 行代码**。4.1 的 `AddCustomTraderHelper` 模式（配方 01）正好吃同一批 JSON。这对全包自动化率是重大利好：商人/物品/任务 mod 的"翻译面"极小。

### 1.3 表注入映射表：已经存在（3.11 -> 4.1 直接可用）

3.11 `DatabaseService` 的 getter 表面与 4.0 一致，迁移文档的 4.0->4.1 映射表**可直接当作 3.11->4.1 用**：

| 3.11 `DatabaseService.getX()` | 4.1 注入 |
|---|---|
| `getTables().templates` / `getTemplates()` | `TemplateTable` |
| `getTables().traders` / `getTraders()` | `TradersTable` |
| `getTables().locales` / `getLocales()` | `LocaleTable`（**直改危险**） |
| `getTables().globals` / `getGlobals()` | `GlobalTable` |
| `getTables().bots` / `getBots()` | `BotTable` |
| `getTables().hideout` / `getHideout()` | `HideoutTable` |
| `getTables().locations` / `getLocation(id)` | `LocationTable.GetLocation(id)` |
| `getTables().templates.items` / `getItems()` | `templateTable.Items` |
| `getTables().templates.quests` / `getQuests()` | `templateTable.Quests` |

### 1.4 按 mod 复杂度分级自动化率（可行性报告数字 + 本次实测修正）

| 档位 | 特征 | 可行性报告原判 | 本次实测修正 |
|---|---|---|---|
| 简单（1-5 文件） | 改数值/加商人/加物品 | 70-85% | **70-85% 维持**。ETT 类（写 locale）跌破 50%；商人/物品类（数据驱动）可达 90%+ |
| 中等（5-20 文件） | 多表交互 + 路由 | 50-70% | 50-60%。（路由自动、多表自动；但 config 自定义 POCO + IOnDIConstruct 注册是新增工作量） |
| 复杂（20+ 文件） | 类扩展 override 服务端核心类 | 20-40% | **10-20%**。progressivebotsystem（44 文件）用 `extends BotGenerator` 等 7 个核心类——4.1 这些类都是 `[Injectable]` 单例，子类化后容器注册与 virtual override 语义完全重做（见 §3 阻断器 4） |

---

## 2. 全自动转换流水线设计

```
┌ Stage 0: 预处理（输入：mod 目录 + package.json）
│   产物: 文件清单（TS/JSON/图片）、类型判定（server/client/both）
│   判定: JSON 数据文件 100% 原样搬运，不进入翻译
│
├ Stage 1: 解析 3.11 源码（静态分析）
│   输入: *.ts（排除 node_modules / types\）
│   产物: API 使用清单（用到哪些容器 token、哪些表、哪些路由、哪些配置类型）
│   技术: LLM 读文件 + AST grep；逐文件打标签（structure/translatable/blocker）
│   闸门: 全部 API 调用都落入映射表 或 明确标记为 blocker（禁止静默跳过）
│
├ Stage 2: 映射（查映射表）
│   输入: API 使用清单
│   产物: 4.1 API 等价调用清单 + blocker 报告（每类阻断给出处理建议）
│   映射表来源: 迁移文档 §Tables、§Namespace moves + api-notes + recipes（已齐）
│   缺失: 3.11 特有 API -> 4.1 的对照（目前只有 4.0->4.1，需人工补 ~20 行）
│
├ Stage 3: 生成 C#（模板 + 翻译后的逻辑）
│   输入: 映射清单 + templates/server-mod/（csproj + ModEntry + ModMetadata）
│   产物: .cs 文件 + 搬运的 JSON/图片 + .csproj
│   规则: [Injectable(TypePriority = OnLoadOrder.X + n)]、IModMetadata、
│         config 用 IOnDIConstruct 注册 POCO、异步 + CancellationToken 贯穿
│
├ Stage 4: 编译验证闸门
│   执行: dotnet build -p:SPTInstallPath=E:\Game\EFT_Offline\SPT_410\SPT_Runtime
│   通过: 0 错误 0 警告（编译器抓 API 名/命名空间/签名错误 = 最大的免费正确性检查）
│   失败: 错误回灌 LLM 修复（编译错误是确定性信号，可自动迭代 3-5 轮）
│
├ Stage 5: 加载验证闸门
│   执行: 复制到 user\mods\<guid>\ -> 启动 SPT.Server.exe -> 日志扫描
│   通过: 无异常、mod 加载成功、无 DI 解析失败
│   注意: 4.1 启动失败模式=注入类型无法解析/配置类型未映射（mod-loading.md 已确认）
│
├ Stage 6: 运行时行为等价验证（最难，见 §5）
│   最小方案: 启动前后数据库状态 diff（§5.2）
│   增强方案: 无客户端 API 驱动（§5.3）
└ Stage 7: 人工抽检（仅复杂 mod / 行为不可观测的 mod）
```

### 2.1 各阶段自动化程度

| 阶段 | 自动化 | 说明 |
|---|---|---|
| 0 预处理 | 95% | 脚本扫描即可 |
| 1 解析 | 80% | LLM 能做，但"识别 blocker"需要知道 4.1 语义（locale 懒加载等）——知识库必须喂给解析 prompt |
| 2 映射 | 70% | 映射表已齐的部分全自动；3.11 特有 API 缺口需人工补表 |
| 3 生成 | 85% | 模板骨架 100% 自动；业务逻辑翻译 LLM 可做，复杂度越高越容易漂移 |
| 4 编译 | 100% | dotnet build 纯机械 |
| 5 加载 | 90% | 进程控制 + 日志扫描机械；启动失败诊断需 LLM 读日志 |
| 6 行为验证 | 50-70% | 见 §5 |

---

## 3. 自动化阻断器（不可自动 / 需人工决策的清单）

### 阻断器 1（最高优先）：`LocaleTable.Global` 直写——ETT 类 mod 的核心功能没有 4.1 等价物

- 3.11：`database.locales.global[localeId][key] = text` 直接改内存表，保存并生效。
- 4.1：`LocaleTable.cs` 源码注释明确 **"DO NOT USE THIS PROPERTY DIRECTLY, USE LOCALESERVICE INSTEAD. THIS IS LAZY LOADED AND YOUR CHANGES WILL NOT BE SAVED"**。
- 而 `LocaleService.cs`（211 行实读）**只有 getter**（GetLocaleDb/TryGetLocaleDb/GetDesiredGameLocale...），**没有 setter / 没有保存本地化的入口**。
- 影响：ETT（Expanded Task Text）整个 mod 就是改写 quest 描述文本——它的全部功能落在 4.1 无法直接表达的区域。**这不是"翻译"问题，是"4.1 平台是否支持该行为"的问题**。
- 可能出路（需原型验证）：改 `SPT_Data/Server/database/locales/global/<lang>.json` 文件本身（mod 启动时直接写文件再重载？4.1 是否懒加载缓存无法刷新？）——这属于规避，不是等价实现。

### 阻断器 2：`Traders[traderId] = traderId`（运行时扩展枚举）

- AES 等商人 mod 在 `preSptLoad` 里写 `Traders[anastasiaBase._id] = anastasiaBase._id`，把商人 ID 动态注入枚举，供后续代码/客户端引用。
- 4.1 服务端无此机制（迁移文档明确 enum 扩展走 prepatcher，且是客户端/服务端枚举声明式定义）。服务端 mod 里凡是依赖该枚举值的逻辑都要重写为字符串/MongoId 常量。

### 阻断器 3：assort JSON 数据形状的隐性依赖

- 3.11 的 `assort.json` / `base.json` 结构按 3.11 数据库形状组织，4.1 的 `AddCustomTraderHelper`（配方 01 提到）期望的形状可能略有差异（字段增删）。
- 数据文件本身自动搬运，但**形状校验**需要对照 4.1 本地 `SPT_Data/Server/database/traders/` 实况，属半人工。
- 好消息：`anastasiaBase.json` 仅 2.8KB（metadata 结构），assort 大文件主要是 items/barter_scheme/loyal_level_items 三件套，结构 3.11/4.1 大概率兼容（EFT 数据格式稳定）。

### 阻断器 4：类扩展 override 服务端核心类（复杂 mod 的死亡区）

- progressivebotsystem（44 TS）：`class APBSBotGenerator extends BotGenerator` 并覆盖 7 个核心生成器。
- 3.11：运行时 tsyringe 解析后直接替换实例方法（猴子补丁式 override 是 JS 常态）。
- 4.1：`BotGenerator` 是 `[Injectable]` 单例；子类要生效必须改注册（TypePriority + 让容器解析你的子类），且被覆盖方法必须是 virtual——**架构语义完全不同，等于重新设计**。
- SAIN 类（745 TS 文件）同样落此档。

### 阻断器 5：TypeScript 类型系统 -> C# 类型系统

| TS | C# | 难度 |
|---|---|---|
| `Record<string, T>` | `Dictionary<string, T>` | 自动 |
| `Record<string, T>` 的任意字符串索引 + `?.` 可选链 | `Dictionary.TryGetValue` + nullable 检查 | 自动（但 3.11 空安全靠运行时，C# 靠静态——**编译期会抓住 null 路径**，反而是优势） |
| union `string \| number`（IQuestCondition.target: `string[] \| string`） | 无直接等价，需包装类或 `object` + 分支 | **半自动**（需 LLM 设计类型） |
| `any` / 未类型化 JSON | 强类型模型 | **半自动**（需从 4.1 模型库找对应 record） |
| 可选字段 `field?: T` | `T?` nullable | 自动 |
| 函数式回调（forEach/map/filter） | LINQ / foreach | 自动 |
| 泛型 JSON 读取 `loadJsonFile<T>` | `modHelper.GetJsonDataFromModFile<T>` | 自动 |

**净判断**：类型翻译不难，因为目标模型（IQuest/ITrader/Item）3.11 和 4.1 都来自同一套 EFT 数据模型，字段名基本一致。难的是**语义差异**（blocker 1/2）和**架构差异**（blocker 4），不是语法差异。

---

## 4. 177-mod 包规模评估（实测 Life_in_Norvinsk_v0.3.2/mods/）

### 4.1 全量扫描（194 个目录，脚本统计）

| 类别 | 数量 | 占比 | 说明 |
|---|---|---|---|
| 含 TS 源码（服务端 mod，转换对象） | **91** | 47% | 本次评估主体 |
| JS-only（已编译、无 TS 源码） | 4 | 2% | 无源码=需要反编译 JS 再翻译（额外一档难度） |
| DLL-only（客户端 BepInEx mod） | 71 | 37% | 不在 TS->C# 服务端转换范围内（客户端是另一条流水线） |
| 空目录/separator/配置文件夹 | 28 | 14% | 无代码 |
| **合计** | 194 | | 任务书称 177，实测 194（含 separator 与配置类） |

### 4.2 TS 规模分层（91 个含 TS 源码的 mod）

| 档位 | 数量 | 占比 | 典型 |
|---|---|---|---|
| 简单（1-5 TS 文件） | **61** | 67% | 商人 mod、数值 mod、小功能 mod |
| 中等（6-20 TS 文件） | 24 | 26% | SkillsExtended(12)、CornerStore(11)、Artem(11) |
| 复杂（21+ TS 文件） | 6 | 7% | progressivebotsystem(44)、Softcore(29)、IEAPI+PTT(27)、Realism(26)、RaidOverhaul(24×2) |
| 合计 | 91 | 100% | 全部 TS 文件数 542 |

### 4.3 API 使用面实测（542 个 TS 文件统计，正则扫描）

| 3.11 API | 涉及文件数 | 映射 |
|---|---|---|
| `database`（任何表访问） | 298 | 表注入（全自动） |
| `postDBLoad` | 169 | `OnLoadOrder.PostLoad + 1`（全自动） |
| `traders` | 154 | `TradersTable`（全自动） |
| `preSptLoad` | 152 | `OnLoadOrder.Preload`（全自动） |
| `locales` | 150 | **LocaleTable（blocker 1 高危区）** |
| `assort` | 146 | 数据搬运（全自动） |
| `globals` | 144 | `GlobalTable`（全自动） |
| `container.resolve` | 126 | 构造注入（全自动） |
| `configServer` | 118 | 类型注入（全自动） |
| `hideout` | 104 | `HideoutTable` |
| `bots` | 92 | `BotTable` |
| `jsonUtil` | 84 | 4.1 JsonUtil（命名空间变化） |
| `imageRouter` | 59 | `ImageRouter` |
| `CustomItemService` | 59 | 4.1 `CustomItemService` |
| `profileHelper` | 55 | 4.1 ProfileHelper（命名空间变化） |
| `StaticRouterModService` | 52 | `AbstractRouter`（全自动） |
| `addTrader` | 36 | 配方 01（全自动 + 数据） |
| `templates.quests` | 35 | `templateTable.Quests` |
| `ragfairPriceService` | 32 | 4.1 RagfairPriceService（命名空间变化） |
| `DynamicRouterModService` | 24 | `DynamicRouter` 子类 |
| `vfs` | 6 | `ModHelper` / System.IO |

**结论**：除 `locales`（150 文件，blocker 1）外，绝大多数 API 调用面有 1:1 或机械映射。但注意——**locale 直写是 3.11 最常见的习惯**（150/542 文件触碰），所以 blocker 1 不是角落问题，而是大规模问题：接近半数文件涉及不能直接翻译的 locale 操作。

### 4.4 全流水线自动化率（包级估算）

| 档位 | 数量 | 单 mod 全自动概率 | 包级贡献 |
|---|---|---|---|
| 简单（61） | 61 | 70-85%（locale 型 50%，数据型 90%） | ~46 mod |
| 中等（24） | 24 | 50-60% | ~13 mod |
| 复杂（6） | 6 | 10-20% | ~1 mod |
| JS-only（4） | 4 | 30%（需先反编译） | ~1 mod |
| **合计（91）** | | **全自动约 61/91 = 67%**；含半自动（编译+加载通过、行为未验证）约 **75-80%** | |

**实际含义**：约 55-60 个 mod 可全自动转换并达到"编译+加载+行为抽查通过"；约 20 个需要 1-2 轮人工修复；约 6-10 个（复杂档 + locale 核心档）基本是人工重写，自动化只提供骨架。

---

## 5. 运行时行为等价验证（推荐策略）

### 5.1 现实：编译通过 != 行为等价（成立）

- 编译验证只能证明 API 调用正确（签名、命名空间、类型）。
- 加载验证只能证明 DI 构造成功、无启动异常。
- **不证明**：改的数据真的改了、改对了、改全了、时序对了、与其他 mod 不冲突。

### 5.2 最小可信验证：数据库状态 diff（推荐，成本最低）

SPT 服务端是内存数据库 mod。可行方案：

1. 启动干净的 4.1 服务器（无 mod）→ 导出基线：`templateTable.Quests` / `tradersTable` / `globals` / `locales`（懒加载需先触发）的关键表 JSON dump（写一个只读 dump mod 或利用服务器启动日志/内存 dump）。
2. 加载目标 mod → 再 dump 同一批表。
3. **diff 两张 dump**：新增/修改/删除的条目应恰好等于 mod 的预期行为（可对照 3.11 服务器同样 dump 一遍做三方对照：3.11 基线 vs 3.11+mod vs 4.1+mod）。

- 优点：不启动游戏客户端、纯服务器进程可控、diff 是确定性证据。
- 覆盖：改数值/加物品/加商人/改任务的 mod 全覆盖（占 91 个中的绝大多数）。
- 不覆盖：路由响应逻辑（`StaticRouter`/`DynamicRouter` 的行为）、运行时动态逻辑。

### 5.3 增强：无客户端 API 驱动（对路由/动态逻辑 mod）

- 4.1 服务器是 ASP.NET Core HTTP 服务器（HttpRouter + 大量 Controllers：`TraderStaticRouter`、`ProfileStaticRouter`、`LauncherV2StaticRouter`、`QuestStaticRouter` 等，已实读目录确认）。
- 可行：`Invoke-WebRequest http://127.0.0.1:<port>/client/trading/...` 驱动游戏协议端点，检查响应 JSON 是否含预期内容（例：加了商人后拉 trader 列表端点确认新 ID 出现）。
- 限制：需要 launcher/profile 握手（`LauncherV2` 路由存在），且端点语义需逐一逆向；成本高于 diff 方案，**只对 diff 覆盖不到的 mod 用**。

### 5.4 推荐的最小验证组合（按档位）

| 档位 | 最小验证 | 是否足够信任 |
|---|---|---|
| 简单-数据型（商人/物品/数值） | Stage 4 编译 + Stage 5 加载 + §5.2 diff | **足够**（diff 证明数据等价） |
| 简单-locale 型（ETT 等） | Stage 4 + 5 + 人工确认文本实际出现在游戏内 | 需要人工（blocker 1 本身待解） |
| 中等（含路由） | Stage 4 + 5 + diff + §5.3 抽查关键路由 | 基本足够 |
| 复杂（类扩展） | Stage 4 + 5 + 人工功能测试 | 人工兜底 |

### 5.5 关键前提：一个"只读数据库 dump"工具必须先行

diff 方案依赖一个能在 4.1 里 dump 任意表的只读 mod（或服务器自带能力）。**在写转换流水线之前，应先用现有模板做一个 `dbdump` mod 验证 dump 可行、懒加载 locale 可触发、输出可对比**——这是整个行为验证的基石，也是验证 blocker 1 能否绕过的实验台。

---

## 6. 给后续计划的建议（按优先级）

1. **补 3.11->4.1 API 映射表**（~20-30 行）：把 §1.3 的表 + 命名空间迁移 + vfs/configServer/路由对照落成知识库记录。这是最大的低成本杠杆。
2. **先做 locale 写入门实验**：确认 4.1 是否真的无法通过服务端代码保存 locale 修改（读 LocaleService 完整实现 + 看 4.1 是否有 locale 写入 API 在别处）。结果决定 ETT 类 mod 的可行性结论。
3. **做 `dbdump` 只读 dump mod**：行为验证的基础设施，先行验证 §5.2 可行。
4. **用 ETT 做端到端试点**（可行性报告已建议）：3 文件、逻辑简单，正好暴露 blocker 1 的真伪。
5. **商人 mod 试点第二个**（如 AES：3 TS + 9 JSON）：验证数据搬运 + AddCustomTraderHelper 路径，预期高成功率。
6. **包级批次策略**：先 61 个简单档（其中先数据型后 locale 型），再 24 个中等，最后评估 6 个复杂档是否值得做（大概率不值得——除非有现成 4.1 版本，注意 SamMeow-* 仓库已有大量 4.1 重制版，应优先复用）。

---

## 7. 关键事实核对表（证据坐标）

| 事实 | 证据 |
|---|---|
| 4.1 `LocaleTable.Global` 懒加载、直改不保存 | `SamMeow_SPT410_source_code/Libraries/SPTarkov.Server.Core/Models/Spt/Tables/LocaleTable.cs`（注释原文） |
| `LocaleService` 只有 getter | 同源码 `Services/Locales/LocaleService.cs`（211 行实读） |
| `DatabaseServer/DatabaseService/ConfigServer` 已移除 | `wiki/SPT_41/Server_40_to_41.md` §"Tables and configs are injectable" |
| 4.0->4.1 表注入映射表可复用 | 同上（含全部 10 表 + TemplateTable 捷径 + 按 ID 查询） |
| `[Injectable]` / `OnLoadOrder` 常量 | `Libraries/SPTarkov.DI/Annotations/Injectable.cs`、`Libraries/SPTarkov.Server.Core/DI/OnLoadOrder.cs`（0..1000000） |
| 4.1 服务器可 API 驱动 | `Routers/Static/TraderStaticRouter.cs` 等 23 个静态路由 + `Controllers/*` 51 个控制器（目录实读） |
| 4.1 服务器已安装 | `E:\Game\EFT_Offline\SPT_410\SPT_Runtime\`（SPT.Server.exe + SPTarkov.*.dll + user/mods 空） |
| 包内 API 使用分布 | 542 TS 文件正则扫描（§4.3 表） |
| 包内规模分层 | 194 目录脚本统计（§4.1/4.2） |
| ETT mod 3 文件结构 | `knowledge/spt-kb/archive/forge/mods/Expanded-Task-Text_2153_source/src/`（mod.ts 331 行实读） |
| AES 商人 mod 构成 | `Life_in_Norvinsk_v0.3.2/mods/[4]新商人-AES/user/mods/AES/`（AES.ts 89 行 + 9 JSON 400KB+） |
| 类扩展模式（复杂 mod） | `mods/[6]Acid的AI装备管理系统.../src/ClassExtensions/APBSBotGenerator.ts`（extends BotGenerator） |
