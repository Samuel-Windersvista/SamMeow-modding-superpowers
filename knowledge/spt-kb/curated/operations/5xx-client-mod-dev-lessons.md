# SPT 5.0 客户端 mod 开发实战经验（渲染捕获 / 射线扫描 / 配置体系 / 数据源事实 / 诊断方法论 / interop 调用 / 健康与 Bot 数据源 / Harmony detour 封送）

> 适用：[5.0] | 来源：TrueRealTimeMap 项目（SPT 5.0.0 BE / EFT 1.1.5.47242 / IL2CPP / BepInEx 6 / net6.0）实机开发与验证记录（2026-09-21）；**SamMeow.DebugToolkit 项目**实机开发与验证记录（2026-09-25；三源整合客户端 mod：DebugTooltip / BotDebug / DadGamerMode 功能移植）；**ITBS（Inescapable Tarkov's Bot System）**实机事故复盘与修复验证（2026-09-28）
> 关联：`curated/api-notes-5.0/`、`curated/modding-standard/05-client.md`、`curated/operations/3114-client-mod-build-gotchas.md`、`skills/porting-spt-mod-to-spt5/`
> 完整实现与证据：`E:\云文件\GitHub\SamMeow-TrueRealTime-Dynamic-Map`、`E:\云文件\GitHub\SamMeow-DebugToolkit`（spec / 工单 / 双轴审查与验证记录）

## 1. IL2CPP 运行时被剥离 API（实测清单）

| API | 实测结果 | 替代方案 |
|---|---|---|
| `Font.CreateDynamicFontFromOSFont` | `NotSupportedException`（剥离） | 游戏内字体（`SimSun-Chinese` / 其 TMP SDF 资产）；内置 `LegacyRuntime.ttf` 可用 |
| `NavMesh.Triangulate` | 剥离（`Method unstripping failed`） | `NavMesh.SamplePosition` / `NavMesh.Raycast` 实测可用；范围判定可改用射线密度直方图 |
| `AsyncGPUReadback.Request` | 剥离 | 同步 `ReadPixels` **分块**（按行拆块、分摊到多帧） |
| `RenderTexture.antialiasing` 属性 | interop 无该属性 | 构造参数或跳过 |

**已验证可用**：`Camera.Render` + `RenderTexture` + `ReadPixels` + `GetPixels32`；`RaycastCommand.ScheduleBatch` + `NativeArray`；`Shader.Find` + `SetReplacementShader`（Unlit/Color、Unlit/Texture、Sprites/Default、UI/Default、Standard 均可用）；UGUI（`RawImage`/`Text`）；`LocalizationManager`。

> 经验：**凡是"运行时才可能被剥离"的 API，都应在项目最初做 spike 实测**（类型存在 ≠ 方法可用；`Type.GetType` 能解析不代表调用不抛）。

## 2. 实机捕获底图（正交相机实拍）要点

- **相机**：`orthographic=true`、`size=世界边长/2`、中心上空（最高命中点+40m）、俯视 `Euler(90,0,0)`、`cullingMask=-1`、关遮挡剔除、`enabled=false` 手动 `Render()`。
- **行序**：`Texture2D.SetPixels32` 按 **bottom-up** 解释；回读缓冲与目标缓冲必须统一（否则底图上下颠倒——本项目实际发生过）。
- **回读**：同步 `ReadPixels` 1024² 约 75–120ms；**分块（128 行/块 → 8 块）**把单帧峰值压到 ~35ms。
- **光照**：夜间场景 `ambient=(0,0,0)` 时实拍几乎全黑，仅靠曲线提亮只会"黑变灰"（无细节）。**渲染期临时覆盖 `RenderSettings.ambientMode=Flat` + 中性灰（~0.42）+ `ambientIntensity=1`（渲染后立即还原，fail-open）** + 适度补光（夜间 ~2.5），暗部才真正有纹理。
- **色调映射 ≠ 线性增益**：启用色调映射（分位数拉伸 + Gamma + 软膝）时**不得**再叠加线性自动增益——本项目曾因 `gain=12` 叠加把图像推向饱和，表现为"两档平色 + 零纹理"的剪影化。
- **其他**：渲染期临时关雾（`RenderSettings.fog=false`，之后还原）；`allowHDR=false` + Forward 渲染降低后处理干扰；正俯视 + `clearFlags=SolidColor` 避免天空盒。

## 3. 射线扫描（高度图）要点

- `RaycastCommand.ScheduleBatch` 批量 + **每帧时间预算** + **单批硬上限**（如 512 条）：缺少硬上限时自适应会把批推到 2048 → 单帧 64ms 尖峰；加上限后 3.35ms。
- 批耗时分布（p50/p95/max）+ "调度 vs 等待"分离计时，是归因尖峰的标准工具。
- **范围判定**：`NavMesh.Triangulate` 被剥离时，用**粗扫射线密度直方图**（行/列门限裁剪 + 正方形化 + 外扩）自动确定可玩区——无需人工地图资产，支持自制图。
- **吞吐约束**：总时长下界 ≈ `射线数 × 单射线成本 ÷ (每帧预算 × 帧率)`；"单帧预算"与"总时长"不可兼得，验收需明确口径（如 单帧 ≤10ms / 总时长 ≤20s）。
- 扫描结果 + 实拍底图应做**持久缓存**（PNG + 元数据），二次进图直接加载（~14ms），把首扫成本一次性化。

## 4. BepInEx 6 配置与缓存坑

- **持久化配置不跟随默认值变更**：改了默认值，旧 `cfg` 仍是旧值 → 必须做**版本化迁移**（`[Meta] CfgVer` + 逐键"仅当值等于旧默认才改写"，保留用户自定义值）。
- **缓存指纹**：任何影响最终输出的参数（含后加的夜间/风格/判定参数）都必须纳入指纹，否则旧缓存不失效（本项目曾"秒开旧图"）；结构/语义变更时递增 `CacheVer`。
- **MO2 VFS**：插件目录写入会被重定向到实例 `overwrite\`；诊断产物（结果文件/PNG/dump）应写 `%TEMP%` 真实路径。
- 部署：MO2 覆盖层（`mods\<分类>-<名字>-<版本>`）；**游戏/服务端运行中 DLL 被锁**，部署前需关闭。

## 5. 验证闭环（本项目最高效的工程实践）

1. **游戏内测试脚手架**：配置门控（默认关、不影响游玩）+ 结构化结果**双通道**（`%TEMP%` 文件 + 日志 `[TRMTEST]` 前缀）+ 场景分派（如 `scan` / `capture` / `mapui`）。
2. **Runtime Bridge（`tarkov_*` MCP）**：读 raid 状态/玩家坐标/日志，与 mod 输出交叉断言。
3. **dump PNG 诊断**：捕获时同时落盘"原始回读图 + 成品图"，供离线逐像素复核（色调塌陷/黑区/覆盖/行序）。
4. **服务端联调**：`SPT.Server.exe` 本地 HTTPS（自签证书；请求头 `responsecompressed: 0` 可取明文 JSON）；客户端 HTTP + 证书跳过实测可用。
5. 每次改动的验证路径：构建 0 警告 + 单测 → 部署 → 进 raid（对应场景）→ 读结果/日志/dump → 观察者复核 → 归档证据。

## 6. 工程流程教训

- **同一时间只允许一个实现者改仓库**：并发写入会互相覆盖（本项目发生过双执行者事故，收敛成本高）。
- 性能敏感逻辑：**先固定值标定、再开自适应**；自适应必须有硬上限与观测数据。
- "离线推断"不等于"实测"：本项目多次出现离线模拟乐观、实机暴露新问题（如夜间全黑、gain 叠加）——所有画质/性能结论以实机 dump/日志为准。
- **多 agent 并行补充（2026-09-25）**：共享一个工作树时——① 按**文件域**切分写权（每模块一个写者），共享文件（csproj / 入口 / 公共 Settings）由编排者单写；② 他人半成品会让整仓编译红——**域外错误等 60s 重试、绝不改域外文件**；③ 「恢复会话」可能**丢失跟踪但真实运行**（幽灵态）——以其**文件活动**判定死活，未证实死亡前**禁止重派**（重派 = 双写撞车）；④ 构建失败先分辨「自己域内 vs 域外瞬态」，再决定修复或等待。

## 7. IL2CPP interop 内存与纹理铁律（2026-09-22 实战补充）

- **托管数组按"数组长度"marshal**：`Texture2D.SetPixels32(x, y, w, h, colors)` 每次调用都会把**整个 `colors` 数组**转成 native（分配 + 拷贝），与 w/h 参数无关。把整层缓冲（1024²×4B=4MB）传给每个脏矩形 → 12 矩形 × 20Hz ≈ **240MB/s native 分配**：表现为主线程卡顿 + GC 堆膨胀吃光内存（实测 `refreshMs` 69ms、用户 32GB 内存被耗尽）。**修复：按 rect 精确尺寸复用 scratch 数组（传给 API 的长度恒等于 w×h）**。
- **新建 `Texture2D` 内容未定义**：只写脏矩形就 `Apply()`（整张上传）会把未写区域的垃圾一并上传——表现为地图上大片"**战争迷雾**"，标记移动写过的区域被逐块"揭开"（用户原话："没走过的地方被战争迷雾覆盖，走过去就清除"）。**修复：创建时全量初始化为透明（一次性 `SetPixels32` + `Apply`）**。
- **UGUI 标签三坑**：
  - `Outline`/`Shadow` 是 `BaseMeshEffect`，**必须与 `Text` 同 GameObject** 才作用于文字网格；
  - 标签定位**不要依赖父子关系**（`rect.parent == area` 这类门控极易恒假 → 位置从不更新、全部堆在画布中心）；用 `area.TransformPoint(本地偏移)` 转世界坐标后设 `rect.position`，对任意父节点成立；
  - 标签显隐必须与面板可见性**同帧同步**（挂在低频刷新周期里会"关闭后残留 1-2 秒"）。
- 标签可读性：底图为实景照片时，**纯色文字在浅色区域不可读**——深色 1px 四向描边 + 半透明深色自适应背景（`preferredWidth/Height` + 内边距）是低成本解法。

## 8. SPT 5.0 客户端数据源事实（TrueRealTimeMap 实测，勿再踩）

| 目标 | 事实 | 正确做法 |
|---|---|---|
| 物品手册价 | 客户端 `ItemTemplate.CreditsPrice` **恒为 0**（该字段未填充）；`items.json` 无此字段 | 服务端读 `SPT_Data/database/templates/handbook.json` 下发；**查表键必须是模板 ID**（`LootItem.TemplateId`），不是实例 ID（`LootItem.ItemId`） |
| 地面物资集合 | `GameWorld.LootItems`（`DictionaryListHydra`）：无索引器、`Count` 为 invoke | `GetValuesEnumerator()` 遍历；物品读 `_item`（字段）失败回退 `Item`（invoke） |
| 容器"已搜刮" | `LootableContainer`/`WorldInteractiveObject`/`LootItem` **无** `Searched`/`Looted` 成员；`DoorState` 只反映门开合（关上即回 `Shut`） | 语义要"开过即已开"只能**每 raid 观察记忆**（收集 Open 观察到的容器 Id 集合） |
| 任务区域 | `QuestTemplate.Conditions`（`ConditionsDict`）**无公开可遍历成员**；zone 数据在服务端 `quests.json`（zoneIds 主要挂 `counter.conditions[]`，需递归） | 服务端解析 quests.json → `{questId: [zoneId]}` 下发；客户端用 `Profile.QuestsData` 关联 |
| 地图归属 | `QuestTemplate.LocationId` = 地图 **MongoID**；`GameWorld.LocationId` = 地图**名** | 服务端从 `locations/<map>/base.json` 的 `_Id` + 目录名生成映射表下发；比较要规范化（去空白/小写/去分隔符） |
| 场景任务 zone | zone id 在 `TriggerWithId._id` / `TriggerZone._triggerId`（可能为空，另有 `Input/OutputTriggerIds`） | 大小写不敏感匹配；进图早期场景未加载完 → 空表时缩短重建周期（~6s） |
| 撤离点 | `ExfiltrationPoint.Status`（invoke，`EExfiltrationStatus`）+ `Settings.Name`；**无** `EligiblePoints`（用 `EligibleEntryPoints`） | 状态映射表放 Core；集合入口 `ExfiltrationController.ExfiltrationPoints` 进图缓存、~30s 重建 |
| 转移点 | `TransitController.pointsById`（field）；名称 `parameters.name`（field）优先、`Description`（invoke）回退 | 静态信息走字段；进图缓存 |

**玩家/Bot 分类**：`WildSpawnType` 中 `pmcUSEC`/`pmcBEAR` 是 SPT 的 PMC 波次 bot（**不是**玩家控制）——按"敌 PMC"着色的地图 mod 必须显式识别它们，否则全部落入 Scav（灰色）。

## 9. BepInEx 6 配置体系（实时化 / 中文化 / 通用迁移）

- **实时化**：视觉/行为参数应在**读取点**每次读取（或做变化检测），不要 `BuildUi` 只读一次——否则 F12 改了要重进 raid 才生效（用户会当成 bug 反馈）。按刷新周期读取的参数（2s 层）改完 ≤ 一个周期生效即可；需重建对象才能生效的（纹理层尺寸/面板开关/池开关）应明确"需重启/重进"。
- **F12 中文化零依赖做法**：不硬引用 `ConfigurationManager.dll`（其他用户可能没装）。用 BCL `System.ComponentModel.DisplayNameAttribute` 设中文显示名 + `ConfigDescription.Tags` 传字符串 `"Advanced"`/`"ReadOnly"`（本机 fork 与官方版均识别：`Advanced` 折叠、`ReadOnly` 灰显）；`Order` 可由内部同名类补齐。**避免** `AcceptableValueList`（已知崩溃风险）。
- **通用 section 重命名迁移**（英文分组改中文而不丢值）：
  - BepInEx `ConfigFile` **按需加载**：构造后 `Keys`/`Values` 只反映本进程 `Bind` 过的键，不能用它枚举旧文件里的键；
  - 正确做法：**扫描 cfg 文本**得到 `(旧分组, 键)` 清单 → 用目标条目类型反射 `Bind` 旧键（此时才读得到文件值）→ 值拷贝到目标条目 → `Remove` 旧条目；目标键不存在则不动（值留在文件里不丢）；
  - 注意**描述粘滞**：同进程内先 `Bind` 的条目描述不会被后 `Bind` 覆盖——迁移顺序要在目标键 `Bind` 之外规划；
  - 迁移必须**幂等**（第二次 `migrated=False`），并在**真实 cfg 副本**上实跑验证（含用户自定义值保留）。高效做法：独立 harness 引用已构建的插件 DLL，**反射调用其迁移器**对真实 cfg 副本干跑，比对迁移前后全部键值与二次幂等（SamMeow.DebugToolkit 工单 09 实践：22/22 保全）——比进游戏手测便宜且可重复。
- 配置项分级：玩家向（默认可见）/ 高级（`Advanced` 折叠）/ 开发者（`Advanced` + 描述前缀"【仅开发者使用】"）；内部键（版本号等）标 `ReadOnly`。

## 10. 服务端协同取数模式（客户端拿不到就让服务端下发）

- 适用于：**客户端 interop 读不到/不可靠、但服务端文件里有**的数据（手册价、地图名→ID、任务 zone 表）。
- 模式：server mod 新增 `/truemap/<x>` 路由 → 读 `SPT_Data/...` 文件（**多候选路径**：cwd → BaseDirectory → 上两级）→ 懒加载 + 内存缓存 + **5s 失败冷却** + 一次性告警 → 紧凑 JSON 下发（如 `{id:price}`，5075 项 ≈160KB）；客户端握手后拉取 + 重试 2 次 + 会话级缓存，**服务端缺失时 fail-open 降级**（回退客户端字段 + 一次性告警）。
- 校验：服务端解析器写成 `public static` 便于本地离线实跑（对照样本值，如 `699f0b87…=27000`）；客户端侧单测覆盖序列化/解析/查询/空表。

## 11. 诊断方法论（两次"计数矛盾"的教训）

- **计数语义 × Reset 频率必须匹配**：诊断对象每轮 `Reset()` 时，只在"重建轮"赋值的计数会恒为 0，与"当轮真实值"并列就产生矛盾（本项目两次：`price0` 只在缓存未命中路径累加、`zones` 只在重建轮赋值）。规则：**每轮计数要么每轮重算、要么不 Reset**，并在字段名/格式串里写明统计范围（过滤前/后、命中/未命中）。
- **来源分解计数**：缓存命中会让"当轮来源计数"全为 0——必须输出 `priceSrc=[server/client/cache]` 这类分解，才能判断数据真实来源。
- **失败路径必须留证**：捕获被拒时也要落 raw dump + 环境快照（fog/ambient/sun/相机位置与裁剪/readback 路径/重试次数）——否则只能猜根因。
- 结构性排查顺序：① 集合是否存在（raw 计数）→ ② 字段是否可读（null/异常计数）→ ③ 值域是否合理（分布 + top-N 样本）→ ④ 过滤/判定是否按预期（前后计数对比）。**样本必须带原始键**（templateId/questId 等），便于离线对照数据文件。
- **多级过滤要打印中间集合规模**：如 `raw → 高度过滤 → 解析 → 价值过滤 → 显示`，每级计数都输出，否则"为什么是 0"无法定位。

## 12. IL2CPP interop 调用陷阱：可选 Nullable 参数（2026-09-25 实战补充）

- **现象**：托管侧调用 `SimpleTooltip.Show(text)` 必抛 `NullReferenceException`（每悬停刷屏 `[Tooltip] 显示 tooltip 失败`）；而游戏自身的 tooltip 显示完全正常。
- **根因（ilspycmd 反编译 interop 桩实锤）**：1.1.5 的 `SimpleTooltip.Show(string text, Il2CppSystem.Nullable<Vector2> offset = null, float delay = 0f, Il2CppSystem.Nullable<float> maxWidth = null, bool locked = false)` 托管桩对两个 Nullable 参数执行 `Il2CppObjectBaseToPtrNotNull((Il2CppObjectBase)offset)` + `il2cpp_object_unbox(...)`——**C# 默认值 `null` 在进入原生方法之前就被 NotNull 转换抛炸**（与 Pointer/激活状态无关）。游戏在原生侧调用可传 null；托管 interop 调用不行。
- **修复模式**：显式构造非 null 包装透传——`new Il2CppSystem.Nullable<Vector2>()` / `new Il2CppSystem.Nullable<float>()`（原生 `hasValue=false`，等价"未提供"→ 回退默认值）。
- **通用规则**：任何 il2cpp 方法的**可选引用/Nullable 参数**都可能带同类桩。凡"托管侧必须主动调用、且有默认 null 参数"的 API，先看 interop 桩有无 `NotNull`/`unbox`；发现即显式构造包装。**类型存在 + 能编译 ≠ 能调用**（与 §1 剥离坑同族：调用面必须验真）。
- **收敛纪律**：手动显示类调用应汇入单一 helper（本项目 4 处直调收敛至 1 处），一处修复全局生效。

## 13. F12 列表选择的安全解法：CustomDrawer 按钮（2026-09-25 已验证）

- 背景：`AcceptableValueList`/枚举渲染走 ComboBox → `GUI.DoButtonGrid` 被剥离必崩（§9 只写了"避免"）。少值列表**并非只能退回文本输入**：
- **已验证替代**：`ConfigurationManagerAttributes.CustomDrawer`（`Action<ConfigEntryBase>`）自绘**单按钮**，点击循环切换取值：
  - vendor 一份 `ConfigurationManagerAttributes` 类即可（CM 不引用插件程序集，**按类型名反射读 `Tags` 内该字段**，命名空间无关）；
  - 用 `GUILayout.Button`（与 ComboBox 的 DoButtonGrid 不同路径）——实机：F12 稳定、点击即时生效并 `Config.Save()` 落盘；
  - 绘制体必须 try/catch 且异常退化为 `GUILayout.Label`（CM 每帧重绘，异常穿透 = 窗口坏掉/刷屏）；
  - 值仍以原始字符串存储 → cfg 手工编辑、代码侧解析与非法值回退全部保留。
- 注意：多模块各自 vendor 同名类时 CM 取"第一份"有轻微歧义——同仓库应**统一一份**共享实现。

## 14. 客户端数据源与 API 事实（SamMeow.DebugToolkit 实测，2026-09-25）

**Bot 调试数据源（现成，无需自采）**
- `BotSpawner.GetBotDebugData(IPlayer, string profileId) : DebugBotStruct`；入口 `Singleton<IBotGame>.Instance.BotsController.BotSpawner`。
- `DebugBotStruct`：`PlayerOwner` → `IObserverToPlayerBridge`（`AIData`/`Nickname`/`iPlayer`/`CurrentStataName` 可用）；`BotData` → `DebugBotDataStructInner`（**class**，需空判）；`HeathsData` → `DebugHeathsDataStructInner`（**struct**，无空路径）；字段 `ProfileId` → **`FakeProfileID`**（空时用 `player.ProfileId` 兜底）。
- 敌我判定：`AIData.BotOwner.EnemiesController.EnemyInfos` 含本机 `ProfileId` = 红——**勿按 `side` 分类**（PMC 为 `side=Savage`，见 §8）。

**健康 / 无敌系统（补丁与字段级）**

| 目标 | 事实 |
|---|---|
| `ActiveHealthController.ApplyDamage` / `DestroyBodyPart` | **非虚**；Harmony prefix 可 `ref float damage` 改写 + `return false` 短路（全伤害入口，含坠落） |
| 伤害类型判定 | `EFT.Ballistics.DamageInfo.DamageType`（字段直读）；`EDamageType` 为 `[Flags]`，**`Fall = 2`**（"GodMode 豁免坠落"类需求用） |
| 部位状态字典 | `BaseHealthController<Effect>._bodyState : Dictionary<EBodyPart, BodyPartState>`（public 字段；含 `.Health` / `.IsDestroyed`） |
| 负面效果清理 | 优先原生 `ActiveHealthController.RemoveNegativeEffects(EBodyPart)`；勿碰混淆/受保护成员 |
| 血量读写 | 用非虚字段 `Value`（`ValueStruct`）读写；**virtual `Current` 有 interop AV 风险**；`GetBodyPartHealth` 返回**值拷贝**（写它不改真实血量——上游 DadGamer 的"3HP 钉血"因此实际无效） |
| 坠落安全高度 | 写字段 `_fallSafeHeight`（规避 virtual `FallSafeHeight`）；写策略：开启时按需写、关闭只还原一次，勿每帧覆写（不干扰他方 mod） |

**UI 钩子（tooltip 类 mod）**
- `QuestListItem` 在 1.1.5 更名 **`QuestListItemView`**，`Init` 扩为 `(Quest, Action<QuestListItemView>, QuestController)`。
- 手动显示 tooltip：`ItemUiContext.Instance.Tooltip.Show(...)`（注意 §12 的 Nullable 包装）；归属判定 `__instance.Pointer == tooltip.Pointer`。
- `HoverTrigger` 订阅需 `DelegateSupport.ConvertDelegate<Il2CppSystem.Action<PointerEventData>>` + 显式 `add_/remove_`；退订登记用 `UIElement.AddDisposable`（`UIContext` 在 1.1.5 不存在，`UI` 字段类型为 `UIParent`）。

**编译引用（CS0012 链）**
- 强类型访问 `EFT.Player`（→`IPlayer`/`IDissonancePlayer`）需引用 `DissonanceVoip.dll`；`CameraManager.SSAA` 需 `Unity.Postprocessing.Runtime.dll`；EFT UI 基链需 `Sirenix.Serialization`（+`.Config`/`.Utilities`/`.OdinInspector.Attributes`）；TMP 需 `Unity.TextMeshPro.dll`。缺引用可先用反射桥降级，但**优先补引用回强类型**（反射 = 运行期不确定性）。

## 15. 工具链与运维事实（MO2 CLI / MCP / ModulePatch 撤销）

- **MO2 CLI 启动（可绕开 MCP）**：`ModOrganizer.exe --profile <p> run <可执行名或路径>`——对**已运行**实例是**转发**（瞬间返回、主实例执行）；未运行则**冷启动**并执行。用 **bat 完整路径**（如 `SPT_5xx\sptvfsbridge.bat`）可绕开中文入口名的编码问题。两种场景均实测可用（VFS 生效、server 健康 200、launcher 拉起）。
- **mo2-mcp 离线路径 bug（避坑）**：offline 分支 `spawn(join(mo2Root, "ModOrganizer.exe"))` **假设便携布局**（exe 在实例根）。"程序目录 + 实例目录分离"布局下 → spawn ENOENT 且未处理 `'error'` 事件 → **整个 MCP 进程崩溃、工具集消失**。降级：文件系统铺放 + meta.ini 手改 + modlist 追加 + 上述 CLI 转发。建议向 mo2-mcp 提修（错误处理 + 布局探测）。
- **`ModulePatch` 无 `Disable()`**：撤销走 `Harmony.UnpatchID(patchType.Name)`（ModulePatch 的 Harmony id = 类型名，ilspycmd 可验）；`Harmony.UnpatchAll(string)` 在 HarmonyX 已过时/升级为错误。模块 `Disable()` 应记录补丁实例、逐一 `UnpatchID`。
- **usvfs 重定向实例化**（§4 补充）：插件新建的 cfg 落 `<MO2实例>\overwrite\BepInEx\config\`——验证"配置面已生成"要去 overwrite 下找，不是游戏目录。

## 16. Harmony detour 的「结构体封送」陷阱：放行前缀也会损坏原版状态（2026-09-28 实战事故复盘）

> 来源：ITBS（Inescapable Tarkov's Bot System）P0 实机事故——对 bot 决策层方法做 Harmony 前缀「接管演示」时，**未接管（前缀纯放行 `return true`）**状态下游戏侧决策数据被静默损坏。

**现象**：第三方调试面板（DebugToolkit BotOverlay 的 `EnterBy`）字段恒空；深挖发现游戏侧 `Agent._lastResult.Reason` **全量置空**、`GetActiveNodeName()` **退化为数字 ID**（hash）；而 bot 行为表面正常（移动/交战如常）——**静默的状态损坏，不是崩溃**。

**排查路径（可复用的方法论）**：
1. **「读取方只读」不能给补丁脱罪**：数据流是 game→tool 单向 ≠ 你的补丁不影响**写方**。需要可证伪实验，别停在静态论证。
2. **A/B + 原始字段探针 = 金标准**：同一构建、cfg 开关切换（装/不装该补丁），探针**直接读游戏内部字段**（`Agent.LastResult().Reason` / `GetActiveNodeReason()`，绕开中间工具的拷贝/展示层），对比**分布**而非单样本。
3. **状态性噪声会骗人**：本例某节点状态（`doorOpen`）的 reason 在**无补丁的对照局里也恒为 null**（约占 1/3 样本）。先建立对照组分布，再看「两字段是否**同时、系统性**翻转」（本例：ON 侧 100% null + 65% 数字 node vs OFF 侧全部有值 + 全具名 ⇒ 系统性损坏坐实）。
4. **补丁目标先验真**：前一版目标 `AICoreStrategy<Int32Enum>.Update` 是泛型定义（RVA=-1）、全二进制**无任何直接调用点**（被内联）——detour 注定不命中（且 MonoMod 对 0/桩指针**静默 no-op、无日志**）。用反汇编扫 call site 确认目标**真实被执行**后，问题才暴露在下一层。
5. **逐层收敛**：先证「前缀体只读无罪」（无 `intercept_error`、代码只读）→ 嫌疑收敛到 **detour/trampoline 机制本身**。

**根因机制**：HarmonyX `Il2CppDetourMethodPatcher` 在 native↔managed 间转换参数/返回值；对**尺寸 ∉ {1,2,4,8} 字节的结构体**（本例 `Il2CppSystem.Nullable<AICoreActionResult<T,W>>`，含 string 指针的大值类型）走 **return buffer + unbox 拷贝**路径，其正确性依赖「boxed 布局/尺寸与原生严格一致」等假定；泛型值类型共享实例化（枚举实参）再叠一层指针解析复杂度。任一假定不成立 → **字段错位**（Action/Reason/Data 移位）——恰好表现为「字符串字段空 / 变数字」。**放行路径（return true）同样受影响**：detour 已替换原生入口，返回值必经 trampoline 往返。
（同族静默坑回顾：§12 Nullable 参数 `NotNull` 桩；以及 HarmonyX 对 `__N` 注入参数**不做可赋值性校验**——签名错配也会静默 apply。）

**修复模式（已验证）**：**不给「含结构体参数/返回」的方法打 detour**。把 seam 迁到 **void / 无参 / 纯引用类型签名**的入口——本例迁至 `AICoreAgent<T>.Update()`（detour 仅转换 `this`，**零结构体往返 ⇒ 天然透明**）。接管语义若因此变化（抑制本帧 vs 回填上一决策），要诚实标注并同步文档。

**打补丁前自查清单**：
- [ ] 目标签名含结构体 / `Nullable` / 大值类型（含**返回**）？→ 换 seam 或先做封送验证，慎打 detour；
- [ ] 目标**真实被执行**？反汇编 call site（排除内联/定义桩；记录实际 detour 指针做探针）；
- [ ] 打过「**透明度 A/B**」吗？cfg 开关 + 原始字段探针**分布对比**，而非仅「没崩 / 没 AV」；
- [ ] 观测面 = 一次性有界探针（每对象/每状态 ≤N 条）+ 失败路径一次性日志；
- [ ] 开关语义：BepInEx cfg **仅启动读取**；F12 改内存+文件；**手改文件可能被退出写回覆盖**（改动要在游戏关闭时做，或用 F12 确认）；
- [ ] 证据纪律：`LogOutput.log` **每次启动覆写**——里程碑时快照存档（本例快照 `D:\Temp\opencode\itbs-logs\session-20260928-*.log`）。

**证据**：ITBS 仓 `.scratch/p0-foundation-proof/issues/02-poc-a-layer-injection.md`（Comments 全史：A/B 设计 → ON/OFF 分布 → 修复验收）；关键事件 `layer.detour_target` / `layer.seam_hit` / `diag.agent`。
