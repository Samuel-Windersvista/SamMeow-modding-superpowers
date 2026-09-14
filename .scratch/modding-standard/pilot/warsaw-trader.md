# 试点合规报告：WarsawTrader

> 试点对象：`tools/warsaw-trader-mod/`（类型：server；目标 SPT 版本：4.1.x —— csproj `net10.0` + `SptVersion ~4.1.0`，试点记录 SPT 4.1.1 实机验证）
> 规则版本：Modding Standard 84 条（commit 635a1c38）
> 报告日期：2026-09-14

## 摘要

- 判定统计：PASS 31 / FAIL 4 / N-A 49（合计 84）
- 关键缺口 Top 3（按严重度）：
  1. **README 缺失**（`STD-STRUCT-005`、`STD-PKG-006`）：mod 目录无 README，仓库根 README 面向插件本体、未覆盖本 mod 的用途/安装/数据说明；影响可发现性与安装可复现性。
  2. **`.gitignore` 未覆盖 IDE/用户文件**（`STD-STRUCT-001`）：仓库根 `.gitignore:35-36` 仅在任意深度排除 `**/obj/`、`**/bin/`，未排除 `.vs/`、`.idea/`、`*.user`；mod 目录自身无 `.gitignore`。
  3. **`.editorconfig` 缺失**（`STD-STRUCT-007`，MAY）：仓库根与 mod 目录均无，跨 IDE 缩进/编码一致性无约束。
- 规则修订建议数：6
- 总体判断：代码本体（元数据/构建/服务端机制/日志/依赖/数据加载）完全合规；全部 4 条 FAIL 集中在**仓库卫生文件**，且根因是「规范默认 mod 为独立仓库、而本试点是 monorepo 内嵌套目录」的作用域歧义（见「规则修订建议」#1）。

### 判定口径说明（作用域）

本试点 mod 并非独立 git 仓库，而是插件 monorepo `SamMeow-modding-superpowers` 内的 `tools/warsaw-trader-mod/` 目录。报告采用如下口径：

- **仓库级文件**（`.gitignore`、`README`、`LICENSE`、`.editorconfig`）以 mod 所属仓库根（monorepo 根）为准；同时核对内容是否覆盖本 mod。
- **mod 组件**（源码、工程、数据、部署目录）以 mod 目录 `tools/warsaw-trader-mod/` 为准。
- **Applies 不匹配**（本 mod 面向 4.1.x，规则 `Applies: 5.0`）与**组件不存在**（无客户端、无 Router、无 bundle、无配置系统、无发布归档）判 N-A。

## 逐规则核对

| Rule ID | 判定 | 证据（文件:行）与说明 | 修复建议 |
|---------|------|----------------------|----------|
| STD-STRUCT-001 | FAIL | 仓库根 `.gitignore:35-36` 仅以 `**/obj/`、`**/bin/` 排除构建产物；未排除 `.vs/`、`.idea/`、`*.user` 等常见 IDE/用户文件。`tools/warsaw-trader-mod/` 目录内无 `.gitignore`（目录清单：仅 csproj、`src/`、`data/`、`bin/`、`obj/`）。 | 在仓库根 `.gitignore` 增补 `*.user`、`.vs/`、`.idea/`，或在 mod 目录新增 `.gitignore` 覆盖同样模式。 |
| STD-STRUCT-002 | PASS | 仓库根 `.gitignore:35-36` 在任意深度忽略 `**/obj/` 与 `**/bin/`；mod 目录磁盘上存在 `obj/`（含 NuGet 中间文件）与空 `bin/`，均被该规则忽略。本报告未运行 git，跟踪状态未直接核验（见修订建议 #2）。 | — |
| STD-STRUCT-003 | PASS | 源码置于 `src/`（`src/ModEntry.cs`、`src/ModMetadata.cs`），数据置于 `data/`，工程文件 `WarsawTraderMod.csproj` 在 mod 根；非「源码平铺根目录」。 | — |
| STD-STRUCT-004 | N-A | Applies 为 paired mod 布局；本 mod 为 server-only（`WarsawTraderMod.csproj:22-39` 无 BepInEx 引用），无客户端组件。 | — |
| STD-STRUCT-005 | FAIL | 仓库根存在 `README.md`（103 行），但内容为插件本体介绍，全文未提及本 mod；`tools/warsaw-trader-mod/` 内无 README。mod 用途/安装/数据说明仅存在于 KB（`knowledge/spt-kb/curated/migration/pilot-experience-warsaw-trader.md`、`recipes/01-add-custom-trader.md`），不在仓库内。 | 新增 `tools/warsaw-trader-mod/README.md`：用途（华约商人 Voron + 击杀任务）、安装位置（`user/mods/WarsawTrader/`）、`data/base.json` 与 `data/assort.json` 说明、无配置项说明。 |
| STD-STRUCT-006 | PASS | 仓库根 `LICENSE:1` 为 MIT License；`src/ModMetadata.cs:19` `License = "MIT"` 与之一致。 | — |
| STD-STRUCT-007 | FAIL | 仓库根与 mod 目录均无 `.editorconfig`（`node_modules` 内的第三方 `.editorconfig` 不计）。MAY 级，可选偏好未采用。 | MAY 级、非阻塞：可选新增 `.editorconfig`（charset/indent_style/indent_size/end_of_line）。 |
| STD-META-001 | PASS | `src/ModMetadata.cs:7` `WarsawTraderModMetadata : IModMetadata`；11 个属性齐备（`:9-19`，可选属性 Contributors/Incompatibilities/ModDependencies/Url 为可空自动属性，默认 null）；全 mod 目录仅此一处实现（grep `IModMetadata` 命中 1 处）。 | — |
| STD-META-002 | PASS | 元数据实现位于独立文件 `src/ModMetadata.cs`，文件名与规则约定一致。 | — |
| STD-META-003 | PASS | `src/ModMetadata.cs:9` `ModGuid = "com.sammeow.warsawtrader"`，反向域名、两段以上。 | — |
| STD-META-004 | PASS | `src/ModMetadata.cs:14` `SptVersion = new("~4.1.0")`，tilde 范围。 | — |
| STD-META-005 | PASS | `src/ModMetadata.cs:13` `Version = new("1.0.0")`；`WarsawTraderMod.csproj:11` `<Version>1.0.0</Version>`，均为三段式。 | — |
| STD-META-006 | N-A | 无客户端插件入口（server-only），不存在 `[BepInPlugin]`。 | — |
| STD-META-007 | N-A | 非 paired mod，无两端版本联动需求。 | — |
| STD-BUILD-001 | PASS | `WarsawTraderMod.csproj:4` `<TargetFramework>net10.0</TargetFramework>`，与 SPT 服务端一致。 | — |
| STD-BUILD-002 | N-A | 无客户端工程，不涉及 Mono 兼容目标框架。 | — |
| STD-BUILD-003 | N-A | 无 BepInEx / Harmony / `Assembly-CSharp` / UnityEngine 等客户端运行时引用（`WarsawTraderMod.csproj:22-39` 仅服务端与 SemanticVersioning）。 | — |
| STD-BUILD-004 | PASS | `WarsawTraderMod.csproj:23-38` 引用 `SPTarkov.Server.Core`、`SPTarkov.DI`、`SPTarkov.Common`，`HintPath` 指向 `$(SPTInstallPath)` 下的实际运行时 DLL（版本随运行时、不高于目标），符合 MUST 条款；未用 NuGet `PackageReference`（规则中「优先」属建议，见修订建议 #6）。 | 可选：改用 `PackageReference` 并锁定 `4.1.x` 版本号，以显式声明目标版本。 |
| STD-BUILD-005 | PASS | `WarsawTraderMod.csproj:19` `<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>`。 | — |
| STD-BUILD-006 | PASS | `WarsawTraderMod.csproj:17` `<SPTInstallPath Condition="'$(SPTInstallPath)' == ''">...</SPTInstallPath>`，可经 `-p:SPTInstallPath=...` 覆盖。 | — |
| STD-SRV-001 | PASS | `src/ModEntry.cs:27` `[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]` 标注入口类，由 DI 容器构造注入（`:28-39` 构造参数）；无手工 `new` 服务实例。 | — |
| STD-SRV-002 | PASS | `src/ModEntry.cs:27` 使用阶段常量偏移 `OnLoadOrder.PostLoad + 1`，非裸数字。 | — |
| STD-SRV-003 | PASS | `src/ModEntry.cs:41` `public Task OnLoadAsync(CancellationToken cancellationToken)`，async 签名正确；方法体为一次性同步 JSON 读取与表注入（`:43-72`），无接受 token 的异步调用，故 token 未传播；同步段规模有限（两份 JSON + 表写入）。 | 可选：在耗时同步段之间插入 `cancellationToken.ThrowIfCancellationRequested()` 以完备（见修订建议 #4）。 |
| STD-SRV-004 | N-A | 未实现 `IOnUpdate`，无周期性任务。 | — |
| STD-SRV-005 | N-A | 未注册任何 Router；通过注入 `TradersTable` / `LocaleTable` / `RagfairConfig` / `TraderConfig`（`:32-35`）直接注入表，不涉及游戏路由。 | — |
| STD-SRV-006 | N-A | 无 Router action，无 `CancellationToken` 签名场景。 | — |
| STD-SRV-007 | N-A | 无 Router，业务逻辑直接在入口方法内，不涉及 Router→Callbacks 分层。 | — |
| STD-SRV-008 | PASS | `src/ModEntry.cs:29` 构造函数注入 `ISptLogger<WarsawTraderModEntry> logger`，`:74`、`:111`、`:141`、`:342` 使用。 | — |
| STD-CLI-001 | N-A | server-only，无 `BaseUnityPlugin` 入口。 | — |
| STD-CLI-002 | N-A | 无客户端插件 GUID。 | — |
| STD-CLI-003 | N-A | 无 Harmony 补丁类。 | — |
| STD-CLI-004 | N-A | 无 Harmony 补丁目标。 | — |
| STD-CLI-005 | N-A | 无 `[BepInDependency]`。 | — |
| STD-CLI-006 | N-A | 无客户端日志。 | — |
| STD-CLI-007 | N-A | 无 `Awake` / `OnDestroy` 生命周期。 | — |
| STD-CFG-001 | PASS | `src/ModEntry.cs:43` 用 `modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly())` 取 mod 根，`:46`、`:68` 以相对名 `data/base.json`、`data/assort.json` 读取；无硬编码绝对路径、不依赖工作目录。本 mod 无玩家可改配置，`data/` 为随 mod 部署的内容数据。 | — |
| STD-CFG-002 | N-A | 无 `config/` 配置系统（`data/` 为只读内容数据，非配置；见修订建议 #3）。 | — |
| STD-CFG-003 | N-A | 无配置，未使用 `IOnDIConstruct` + `AddSingleton`。 | — |
| STD-CFG-004 | N-A | 无配置类（POCO/record）。 | — |
| STD-CFG-005 | N-A | 无 `config/defaultConfig.jsonc`。 | — |
| STD-CFG-006 | N-A | 无客户端，不涉及 `Config.Bind`。 | — |
| STD-LOG-001 | PASS | 日志经构造函数注入的 `ISptLogger<T>`（`src/ModEntry.cs:29`）输出；无 `Console.WriteLine` 或自建静态日志器。 | — |
| STD-LOG-002 | PASS | 按语义分级：`:74`、`:347` `Success`（成功流程），`:111`、`:141` `Warning`（可恢复：表写入失败、商人缺失），`:342` `Error`（任务创建失败）；无昂贵 Debug 消息拼装。 | — |
| STD-LOG-003 | N-A | 无客户端，不涉及 BepInEx `Logger`。 | — |
| STD-LOG-004 | PASS | 全文件无 `catch` 块，不存在静默吞异常；错误路径均有日志：`:109-112`（`TryAdd` 失败记 Warning）、`:139-143`（trader 为 null 记 Warning 并返回）、`:338-344`（`CreateQuest` 失败逐条记 Error）。 | — |
| STD-LOG-005 | N-A | 无异步 IO 与取消路径，不涉及 `OperationCanceledException` 处理（见修订建议 #4）。 | — |
| STD-DEP-001 | N-A | 条件不成立：本 mod 无硬依赖，`src/ModMetadata.cs:17` `ModDependencies` 为 null。 | — |
| STD-DEP-002 | PASS | `src/ModMetadata.cs:17` `public Dictionary<string, Range>? ModDependencies { get; init; }` 未初始化（null），未声明任何可选/软依赖。 | — |
| STD-DEP-003 | N-A | 无可选依赖，无需 `IOnLoad` 自判降级。 | — |
| STD-DEP-004 | N-A | 无客户端，不涉及 `[BepInDependency]` 硬依赖。 | — |
| STD-DEP-005 | N-A | 无客户端，不涉及 `SoftDependency`。 | — |
| STD-PKG-001 | N-A | 本 mod 为仓库内参考实现，未产出发布归档（无 zip、无发布目录），无归档结构可核验。 | — |
| STD-PKG-002 | N-A | 未建立 MO2 overlay，无 `meta.ini`。 | — |
| STD-PKG-003 | N-A | 非 paired mod，无两端归档。 | — |
| STD-PKG-004 | PASS | 服务端 mod 目录内仅一个 `IModMetadata` 实现（`src/ModMetadata.cs:7`；全目录 grep `IModMetadata` 仅 1 处命中）。 | — |
| STD-PKG-005 | N-A | 非 paired mod。 | — |
| STD-PKG-006 | FAIL | 仓库根提供 `README.md` 与 `LICENSE`，但 README 未覆盖本 mod（与 `STD-STRUCT-005` 同源缺口）；mod 目录内无 README/LICENSE。LICENSE 部分已由仓库根 MIT 满足。 | 补 `tools/warsaw-trader-mod/README.md`（见 `STD-STRUCT-005` 修复建议）。 |
| STD-PKG-007 | N-A | 单来源组装，无多来源合并产生的冲突运行时文件（如 `config.json`、`escrow.json`）。 | — |
| STD-VERIFY-001 | PASS | 有 Release 构建记录：`knowledge/spt-kb/curated/migration/pilot-experience-warsaw-trader.md:19-20`「编译 0 错误」。本报告未重跑构建（`WarsawTraderMod.csproj:17` 默认 `SPTInstallPath` 指向本机外的 SPT 安装）。 | — |
| STD-VERIFY-002 | PASS | 实机验证记录覆盖 Level B 并超出：`pilot-experience-warsaw-trader.md:16-23`（服务器加载 mod 成功、商人实机可见、可购买武器、可接任务）。 | — |
| STD-VERIFY-003 | PASS | 加载日志断言可用：`src/ModEntry.cs:74` 输出 `Warsaw Pact Trader loaded: ...`；试点记录「服务器加载 + OnLoad 成功（日志确认）」（`pilot-experience-warsaw-trader.md:20`）。 | — |
| STD-VERIFY-004 | N-A | 无客户端插件，无 `BepInEx/LogOutput.log` 断言对象。 | — |
| STD-VERIFY-005 | N-A | Applies: 5.0；本 mod 面向 4.1.x，不适用 tarkov-runtime MCP。 | — |
| STD-VERIFY-006 | N-A | 单 mod 直接部署，无多 mod 批次清单/加载顺序/冲突可预检；规则的批次场景不成立（适用边界见修订建议 #5）。 | — |
| STD-VERIFY-007 | N-A | 条件不成立：验证过程无失败信号，未触发向 `diagnosing-spt-problems` 的移交。 | — |
| STD-VERIFY-008 | N-A | 本 mod 只注入内存表，不写 profile/save，无存档边界风险。 | — |
| STD-VERIFY-009 | PASS | MAY 级、已采用：试点含局内验证（购买武器、接任务）——`pilot-experience-warsaw-trader.md:22-23`。 | — |
| STD-VER-002 | N-A | Applies: 5.0，本 mod 面向 4.1.x。 | — |
| STD-VER-003 | N-A | Applies: 5.0，本 mod 面向 4.1.x。 | — |
| STD-VER-004 | N-A | Applies: 5.0，本 mod 面向 4.1.x。 | — |
| STD-BND-001 | N-A | 无服务端 bundle；资源仅 `data/voron.jpg`（普通图片）与 `data/*.json`，不涉及 `bundles/` 与 `bundles.json`。 | — |
| STD-BND-002 | N-A | 无 `bundles.json`。 | — |
| STD-BND-003 | N-A | 无客户端私有 bundle。 | — |
| STD-BND-004 | N-A | 无 bundle，不涉及脚本绑定键。 | — |
| STD-BND-005 | N-A | 无 bundle，不涉及 Unity 版本头判定。 | — |
| STD-BND-006 | PASS | 数据由 mod 代码显式读取并注入内存表：`src/ModEntry.cs:46`、`:68` 读 `data/base.json`、`data/assort.json`，`:62`（`tradersTable.TryAdd`）、`:69`（assort 写入）、`:72`（任务创建）注入；不依赖服务器自动合并 `user/mods/` 内 JSON。 | — |
| STD-PERF-001 | N-A | 无客户端每帧射线/物理查询（server-only）。 | — |
| STD-PERF-002 | N-A | 无客户端 AI 子系统每帧遍历。 | — |
| STD-PERF-003 | N-A | 无寻路计算。 | — |
| STD-PERF-004 | N-A | 无客户端 Harmony 补丁。 | — |
| STD-PERF-005 | PASS | `IOnLoad` 路径（`src/ModEntry.cs:41-75`）无 `GC.Collect`、`Thread.Sleep`、全局锁包裹 I/O；仅一次性同步 JSON 读取与表写入。 | — |
| STD-PERF-006 | PASS | 无高频查找/排序/反射路径；`localeTable.Global` 仅在启动时遍历一次（`:122`）。 | — |
| STD-PERF-007 | PASS | 文件路径由 `modHelper` 根目录 + 固定相对名拼接（`:46`、`:49`、`:68`），无外部（配置/存档/网络）提供的文件名；`traderBase.Avatar`（`:52`）仅用作 image route key，非文件路径。 | — |
| STD-PERF-008 | PASS | 无外部输入校验面：随包 JSON 反序列化失败会直接抛出中止加载（fail-closed）；`TryAdd` 失败（`:109-112`）与任务创建失败（`:338-344`）走日志降级，属可选功能降级而非 log-then-continue 式输入校验。 | — |

## 规则修订建议（反向校准）

| # | 涉及规则 | 问题类型 | 建议修订 | 理由 |
|---|----------|----------|----------|------|
| 1 | STD-STRUCT-001 / -005 / -006 / -007、STD-PKG-006 | 表述不清 | 明确「mod 仓库根」在 monorepo / 嵌套 mod 场景的判定口径：或规定「仓库根」指 mod 所属仓库根、或为嵌套 mod 规定「mod 目录内至少提供 README 与 .gitignore」 | 本试点为仓库内工具目录而非独立仓库；README/LICENSE/.gitignore/.editorconfig 按字面无法唯一判定（同一文件按 monorepo 根判 PASS、按 mod 目录判 FAIL），易产生争议判定 |
| 2 | STD-STRUCT-002 | 表述不清 | 明确「仓库不得包含 bin/obj」判「物理存在」还是「git 跟踪」，并说明全局 `.gitignore`（`**/obj/`、`**/bin/`）覆盖时是否算满足 | 本 mod 磁盘上存在 `obj/` 与空 `bin/`，但被仓库根 `.gitignore` 全局忽略；在不运行 git 的审查流程中无法判定跟踪状态，规则按字面无法落地 |
| 3 | STD-CFG-001 / -002 | 表述不清 | 明确 CFG 维度是否覆盖「随 mod 部署的只读内容数据」（如 `data/*.json`），或界定「配置」与「内容数据」的边界 | 本 mod 无玩家配置，却以 ModHelper 相对路径读取 `data/`；按字面 CFG 多数组件缺失落入 N-A，规范未描述「无配置但有随包内容数据」这一常见形态 |
| 4 | STD-SRV-003、STD-LOG-005 | 证据不足 | 明确「无异步 IO 的纯同步 `IOnLoad`」下 `CancellationToken` 的判定：判 N-A，还是仍要求显式 `ThrowIfCancellationRequested()` | 两条规则均以异步 IO / 长同步循环为前提；同步一次性加载场景缺少合规判据，易过判或漏判 |
| 5 | STD-VERIFY-006 | 表述不清 | 明确适用范围：仅多 mod 批次，还是单 mod 冒烟也须执行 spt-mcp 预检 | 本试点为单 mod 直接部署，无批次清单/加载顺序/冲突可预检；规则未限定场景，判定边界不清 |
| 6 | STD-BUILD-004 | 表述不清 | 将「优先使用 NuGet `PackageReference`」与 MUST 条款拆开表述，明确 `HintPath` 本地引用（版本随运行时）是否算完全合规 | 本 mod 以 `HintPath` 指向 `$(SPTInstallPath)` DLL，满足「不高于运行时版本」但不显式「锁定版本号」；MUST 与「优先」混写导致判定歧义 |

## 已知豁免记录（如有）

无。

- 本 mod 源码与目录中未发现任何 `Waiver: STD-XXX-nnn` 标记。
- 附带观察：因 mod 目录无 README（`STD-STRUCT-005` FAIL），按规范「豁免记录位置：mod 仓库 README 或项目 dev-log」，本 mod 当前缺少合规的豁免记录载体；若后续需要豁免 `STD-STRUCT-007` 等项，须先补齐 README 或 dev-log。

---

> 本报告为 Modding Standard 的首批应用样例（试点 ×1/3，对象 WarsawTrader）。判定统计：PASS 31 / FAIL 4 / N-A 49（合计 84）。规则修订建议 6 条，供 ticket 11 校准闭环引用。
