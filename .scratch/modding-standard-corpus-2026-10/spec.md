# SPT Modding Standard — 2026-10 语料刷新与增补 Spec

> 进展：DRAFT — 2026-10-18。基于 631 个源码仓库 / 34,050 个文件的语料扫描。
> 前序：`.scratch/modding-standard/spec.md`（2026-09，86 条规则 / 13 维度）。
> 证据产物：`.artifacts/forge-refresh-2026-10-18/`（corpus-pattern-scan / metadata-scan / metadata-scan2）。

Status: ready-for-agent

## Problem Statement

现有 Modding Standard 建立在 2026-09 的 392 个源码目录上。Forge 归档现已增长到 **631 个 `_source` 目录、34,050 个文件**。重新扫描后暴露出四类问题：

1. **事实错误**：`EV-GAP-DEP` / `EV-NOCORPUS #1` 声称 392 个目录中“0 个真实 `ModDependencies` 声明”，`STD-DEP-001` 因此只有机制证据、级别停在 SHOULD。新语料中实际有 **53 个仓库 / 82 对非空依赖**，其中 `com.wtt.commonlib` 出现 49 次。
2. **规范空白**：Fika 多人兼容、本地化、补丁生命周期、配置迁移、异步并发、诊断钩子、TS 服务端遗产、monorepo 多 mod 布局，都是语料中的高频做法，但 13 个维度没有对应规则。
3. **规则与语料主导形态冲突**：`STD-CLI-003` 要求“由入口统一 `Harmony.PatchAll()` 应用”，但语料中 **277 个仓库使用 `ModulePatch` / `PatchSet`**，只有 **91 个文件** 使用 `PatchAll()`。现有表述会让合规模块被判 FAIL，或诱导实现者采用脆弱的全程序集扫描。
4. **证据计数过期**：`SptVersion`、`ModGuid`、语料类型/结构等 `EV-CORPUS-*` 数字仍是 2026-09 口径；新增的 67 个 10 月仓库未进入统计。

后果：agent 会按错误证据写依赖声明；客户端补丁会按与生态相反的形态实现；Fika/本地化/迁移/异步的重复踩坑无人记录；检查器可能误伤合法 mod。

## Solution

分四阶段刷新规范，保持现有“分级 + 双源证据”模型不变：

- **P0 事实修正（无新规则）**：更新语料计数与 `ModDependencies` 证据，升级 `STD-DEP-001`；刷新 `SptVersion` / `ModGuid` 计数。
- **P1 现有维度增补**：修正 `STD-CLI-003`；新增客户端补丁隔离规则；新增服务端补丁生命周期规则；新增配置迁移规则；新增异步/并发规则；补充 monorepo 与 TS 遗产说明。
- **P2 新维度**：新增 `MP`（Fika / 多人兼容）与 `LOC`（本地化）两个维度，按 README 扩展流程补 ADR。
- **P3 延后**：存档/Profile 迁移、诊断钩子、TS 运行时兼容、细粒度检查器处理器，等语料或试点。

所有新增 MUST 必须同时具备机制证据与 ≥10 仓库的语料证据；样本不足 10 的候选一律 SHOULD 或 MAY，并标注证据缺口。新维度不修改已有 86 条规则的编号；新规则使用域内下一个未用编号。

## User Stories

1. As a mod 编写 agent, I want `STD-DEP-001` 的证据反映真实的 53 仓库 / 82 对依赖，so that 我不会再被告知“`ModDependencies` 是无语料先例”。
2. As a mod 编写 agent, I want 知道 `com.wtt.commonlib` 是生态中最常见的共享库依赖，so that 我按真实惯例声明并测试版本范围。
3. As a reviewer, I want `STD-DEP-001` 升级为 MUST，so that 依赖声明的 key/range 写法有强制标准。
4. As a maintainer, I want `EV-CORPUS-SOURCE` 从 392 更新到 631，so that 后续规则的样本计数有正确基线。
5. As a mod 编写 agent, I want `SptVersion` 波浪号规则的证据从“模板推断”升级为 297/326 的真实语料，so that 我知道 tilde 是生态主导写法。
6. As a mod 编写 agent, I want 知道 15 个精确版本和 13 个范围写法的例外，so that 我不会把合理的补丁级 pin 误判为违规。
7. As a mod 编写 agent, I want `ModGuid` 反向域名规则有 266/277 的语料支撑，so that GUID 命名有可引用的证据。
8. As a client mod 作者, I want 标准接受 `ModulePatch.Enable()` 作为默认应用方式，so that 我不必为了合规改用脆弱的 `PatchAll()`。
9. As a client mod 作者, I want 每个 patch 目标缺失时只记 Error 并跳过该 patch，so that 一个 EFT 重命名不会让整个 mod 的补丁全部失效。
10. As a client mod 作者, I want 撤销补丁前先恢复被修改的游戏状态，so that `UnpatchSelf` 不会留下“被压制的 bot”或幽灵缓存。
11. As a client mod 作者, I want raid 结束时清理缓存的 raid 对象，so that 反复进图不会积累 Unity 伪 null 引用。
12. As a server mod 作者, I want 服务端补丁通过 `IRuntimePatch` + DI 收集 + `IOnLoad` 统一启用，so that 服务端补丁有可预测的生命周期。
13. As a Fika mod 作者, I want Fika interop 隔离在独立程序集、主程序集不出现 Fika 类型，so that Fika 未安装时不会 `TypeLoadException`。
14. As a Fika mod 作者, I want 用 SoftDependency + Chainloader GUID + 版本范围检测 Fika，so that 版本不匹配时能降级到 local-only。
15. As a Fika mod 作者, I want 包注册只在 `FikaNetworkManagerCreatedEvent` 后进行、销毁时退订，so that 不会重复注册或跨 raid 泄漏。
16. As a Fika mod 作者, I want host/client 角色分叉和中继模式有规则可循，so that 多人状态同步不会在 client 上重复执行。
17. As a localizer, I want `locales/<SPT 语言码>.json` 的布局与语言码规则，so that 我不会用 ISO 码（zh/ja）导致 locale 不加载。
18. As a localizer, I want `en.json` 主文件 + 当前语言 → en → 键名的回退链，so that 缺翻译时 UI 不显示裸键。
19. As a localizer, I want 客户端语言切换钩子的规则，so that 玩家在游戏内切换语言后 mod 文本会刷新。
20. As a server mod 作者, I want 通过 `AddTransformer` 合并 locale 表而不是重写，so that 不破坏 vanilla 和其他 mod 的条目。
21. As a config-heavy mod 作者, I want 隐藏 `Internal` 版本字段与显式重置/迁移规则，so that 配置结构变化不会静默保留旧默认值。
22. As a mod 作者, I want 异步/并发规则（禁 client tick `Thread.Sleep`、`async void` 围栏、共享状态守卫），so that 我不会写出难诊断的卡顿或死锁。
23. As a maintainer, I want monorepo 多 mod 的布局说明，so that 一个仓库里的多个 `IModMetadata` 不会被误判为违规。
24. As a porter, I want `package.json` TS 服务端 mod 的 3.11 遗产识别与迁移映射，so that 我知道 4.1+ 应迁移到 C# `IModMetadata`。
25. As a maintainer, I want 新维度 `MP` / `LOC` 先有 ADR 再进规则集，so that 维度扩展有决策记录。
26. As an agent, I want `validate-mod-standard.ps1` 与 fixtures 同步支持 15 个维度，so that 规则集自检不会因新维度失败。
27. As a reviewer, I want 现有 mod 的过渡与豁免说明，so that 规则升级不会追溯性地把已发布 mod 判为不合规。
28. As a maintainer, I want 明确的 ticket 拆分与依赖，so that P0/P1/P2 可以分批实施而不互相阻塞。

## Implementation Decisions

### D1. 证据基线与刷新口径

- 语料基线改为 **631 个 `_source` 目录 / 34,050 个文件**；统计日期 2026-10-18。
- `EV-CORPUS-SOURCE` 更新为 631；`EV-CORPUS-TYPE` / `LANG` / `STRUCT` / `CSPROJ` / `META` / `CFG` / `ANOMALY` / `TOP5` 按原方法重算，保留原表结构。
- 可复用的种子计数：551 个仓库含 `.csproj`；40 个仓库含 `package.json`；302 个仓库引用 `SPTarkov.Server`；409 个仓库引用 BepInEx；277 个仓库含 `ModulePatch` / `PatchSet`；91 个文件使用 `PatchAll()`。
- `EV-REVIEW` 的“语料集更新”触发条件保持；本次刷新记录进 evidence-index 的校准/复核段。

### D2. P0 依赖证据修正（最高优先级）

- `EV-GAP-DEP` 删除“0 个真实非空依赖”的结论，改为：
  - **53 个仓库 / 82 对非空 `ModDependencies`**；
  - 依赖分布：`com.wtt.commonlib` 49、`com.wtt.contentbackport` 11、`com.morebotsapi.tacticaltoaster` 4、`com.epicrangetime.aio` 3、`me.sol.sain` 3；
  - 典型写法：`{ "com.wtt.commonlib", new Range("~3.0.6") }`、`["com.wtt.commonlib"] = new Range("~3.0.0")`。
- `EV-NOCORPUS` 删除第 1 条（`ModDependencies` 真实声明）；`STD-DEP-001` 的 Evidence 不再引用 `EV-NOCORPUS #1`。
- `STD-DEP-001` 从 **SHOULD 升为 MUST**（机制 + 53 仓库双源达标），规则文本保持“key = 依赖 ModGuid，value = SemVer Range”；补一句“共享库依赖是生态主导场景，范围必须与目标运行时实测”。
- 不新增依赖规则；`STD-DEP-002..005` 保持不变。`EV-GAP-DEP` 的“服务端无软依赖”结论仍然成立。

### D3. P1 客户端补丁规则与 `STD-CLI-003` 修正

- **修正 `STD-CLI-003`**：保留 `[HarmonyPatch(typeof(<目标>), "<方法名>")]` 的 MUST 要求；把“由入口统一 `Harmony.PatchAll()` 应用”改为：
  - 默认使用 `ModulePatch` / `PatchSet` 显式 `Enable()`；
  - `PatchAll()` 仅在“所有目标都保证存在、且接受全有或全无”时允许；
  - 不得用 `PatchAll()` 作为未逐个验证目标的捷径。
  - 证据：277 个仓库使用 `ModulePatch` / `PatchSet`；91 个文件使用 `PatchAll()`。
- 新增 `STD-CLI-008`（SHOULD）：每个 patch 目标解析失败时记录 Error 并跳过该 patch，不得让单个缺失目标中断全部补丁。证据：`Adaptive-Bot-Culling` 的 `ApplyHarmonyPatch` null 守卫、ABPS 的逐 patch `Enable()`。
- 新增 `STD-CLI-009`（SHOULD）：撤销补丁前先恢复被修改的游戏状态（bot 表现/移动/缓存），再 `UnpatchSelf`。证据：`Adaptive-Bot-Culling` 的 `OnDestroy` 顺序。
- 新增 `STD-CLI-010`（SHOULD）：补丁缓存的 raid 对象在 raid 结束/对象销毁时复位或修剪。证据：Airburst `ClearRaidState`、Blackout `_registered=false`、Advanced-Modding-Lights 的 `PruneDestroyedObjects`。
- 新增 `STD-CLI-011`（MAY）：隐式接口/非常规目标用 `AccessTools.DeclaredMethod` 或 `GetInterfaceMap` 精确解析，不按名字猜。证据：`Adaptive-Bot-Culling`、`Advanced-Modding-Lights`。
- **同步检查器**：`STD-CLI-003` 的现有 handler 只匹配 `[HarmonyPatch(typeof(`，文本修正不影响它；但 `STD-CLI-007` 的 `cli-007-harmony-lifecycle` 只把 `Harmony` / `PatchAll` 识别为 Harmony 使用，必须补 `ModulePatch` / `.Enable()` 识别，否则使用主导形态的 mod 会被误判为 N/A。

### D4. P1 服务端补丁生命周期

- 新增 `STD-SRV-009`（SHOULD）：服务端 Harmony 补丁用 `IRuntimePatch` / `AbstractPatch` 表达，通过 DI `IEnumerable<IRuntimePatch>` 收集，在 `IOnLoad` 中统一 `Enable()`；不要在服务端用 `PatchAll()` 扫程序集。
- 证据：28 个仓库同构（ABPS `Server/PatchManager.cs`、Strange-Curios、EternalCycle、Ironman、WTT-CommonLib 等）。
- 这是 `04-server.md` 当前完全缺失的形态；新增小节，不改变已有 SRV-001..008。

### D5. P1 配置模式迁移

- 新增 `STD-CFG-007`（SHOULD）：有破坏性配置变更的 mod 在隐藏的 `Internal` 段保存 schema 版本（`Browsable = false`）；证据 11 个仓库 `ConfigVersion`。
- 新增 `STD-CFG-008`（MUST）：版本不匹配必须显式处理——迁移（保留用户值）或重置（删除/清空 + 日志）；不得静默按新 schema 重新解释旧配置。证据：11 个仓库 + `Config.Bind` 的覆盖语义。
- 新增 `STD-CFG-009`（SHOULD）：schema 版本独立于发布版本，优先整数递增；不要把 `ConfigVersion` 绑到插件版本字符串。证据：BTN 的 `ConfigVersion = 134` vs CloudSix 的版本字符串。
- 新增 `STD-CFG-010`（SHOULD）：服务端持久化 JSON 记录包含 `SchemaVersion` + `CurrentSchemaVersion` 常量 + `<=0` 回填；证据：Career-Log 的 `RaidRecordMigration`。
- 明确“整体重置”是合法策略，但必须记录日志并让用户可预期；不把“重置”本身判为违规。

### D6. P1 异步/并发规则

- 新增 `STD-PERF-009`（SHOULD）：禁止在 client spawn / tick / patch 路径用 `Thread.Sleep` 轮询；改用时间戳或宿主 `Update` 驱动重试。证据：27 个文件 / 20 个仓库含 `Thread.Sleep`，典型在 bot 生成路径。
- 新增 `STD-PERF-010`（SHOULD）：可变共享状态（配置重载/保存、共享缓存）用 `Interlocked` CAS 或 `SemaphoreSlim` 守卫；Harmony 补丁内锁粒度最小化。证据：60 个文件 `SemaphoreSlim`、89 个仓库 `lock`、ABPS 的 CAS 门禁。
- 新增 `STD-LOG-006`（SHOULD）：`async void` 必须自带 try/catch 围栏，或改为 `async Task` / 同步事件转发。证据：129 个文件含 `async void`。
- 新增 `STD-LOG-007`（SHOULD）：verbose / 性能诊断日志必须由配置项或常量门控，默认关闭。证据：194 个仓库含诊断相关代码，C11-True-North 的 Diagnostics 配置段。
- 新增 `STD-LOG-008`（MAY）：热路径/每包失败日志去重，避免刷屏；证据：Airburst 的 `_sendFailureLogged`。
- **口径警告**：扫描到的 573 条 `.Result` / `.Wait()` / `Thread.Sleep` 不能直接当“死锁数”；大量是 `await Task.WhenAll` 后的良性读取。危险样本不足 10，因此不写“禁用 `.Result`”的 MUST，只写“不得在未完成 Task 上阻塞”的 MAY/SHOULD 说明。

### D7. P2 新维度 `MP`（Fika / 多人兼容）

- 先写 ADR（建议 `docs/adr/0013-modding-standard-multiplayer-dimension.md`），定义 domain slug `MP`、范围、与 `CLI` / `SRV` 的边界。
- `STD-MP-001`（MUST）：Fika interop 放在独立程序集，主程序集不得出现 Fika 类型；跨程序集调用经反射/桥接，并对桥接方法使用 `NoInlining`，确保 Fika 缺失时主程序集仍可加载。证据：69 个文件引用 `com.fika.core`、48 个 Fika 相关目录、约 19 个独立 interop 程序集；Airburst / Blackout 双实现。
- `STD-MP-002`（SHOULD）：用 `[BepInDependency(..., SoftDependency)]` + `Chainloader.PluginInfos` GUID 查找 + 版本范围门禁；缺失/超范围时降级 local-only 并记录 Warning。证据：Airburst `AirburstNetwork.cs`、ISB-Aishi。
- `STD-MP-003`（SHOULD）：包注册只在 `FikaNetworkManagerCreatedEvent` 后进行；`FikaNetworkManagerDestroyedEvent` / Shutdown 时退订并复位；注册幂等；manager 尚未创建时允许退避重试。证据：36 个文件使用 `FikaEventDispatcher`，约 8 个完整生命周期样本。
- `STD-MP-004`（SHOULD）：host/client 逻辑用 `FikaBackendUtils.IsServer` 分叉；client→host 中继采用 apply + relay。证据：Blackout、Airburst、CloudSix、BulletTime。
- `STD-MP-005`（MAY）：Fika 兼容性决策（支持/不支持/版本范围）写入 README 或 ADR。证据：Keep-Starting-Gear 的 ADR-001。
- 5.0 / IL2CPP 的 Fika 形态无语料，`Applies` 先标 4.1；5.0 只在机制确认后扩展。

### D8. P2 新维度 `LOC`（本地化）

- 先写 ADR（建议 `docs/adr/0014-modding-standard-localization-dimension.md`），定义 domain slug `LOC`。
- `STD-LOC-001`（SHOULD）：locale 文件放 `locales/<langId>.json`，`langId` 使用 SPT `Locales.Global` 的键（`ch` / `jp` / `kr` / `ge` / `po` / `cz` / `tu` 等），不是 ISO 代码。证据：228 仓库 / 353 文件，63 个 `ch.json`；Better-Keys-NG 遍历 `Locales.Global.Keys`。
- `STD-LOC-002`（SHOULD）：`en.json` 是主文件；非 en 加载器必须把缺失键从 en 合并，并按“当前语言 → en → 键名”回退。证据：Advanced-Stash-Sorting 的 `GetRaw`、All-Quests-Checkmarks 的 `Locales.cs`。
- `STD-LOC-003`（SHOULD）：客户端在 `LocaleManagerClass.UpdateApplicationLanguage` 后缀重新注入 locale，或用 `AddLocaleUpdateListener`；不要缓存启动时的语言。证据：All-Quests-Checkmarks 的 patch、Advanced-Stash-Sorting 的 listener（样本 n=2，标 SHOULD）。
- `STD-LOC-004`（SHOULD）：mod 自有键用稳定前缀（`<modprefix>_`）；服务器表条目沿用 `<mongoId> <suffix>` 游戏约定。证据：`aqc_*`、MongoId 键控 locale 表。
- `STD-LOC-005`（SHOULD）：服务器 locale 修改通过惰性 `AddTransformer` / 合并到 `locales.global`，不重写整表。证据：Better-Keys-NG、WTT CommonLib 的 `CustomLocaleService`（样本 n=2）。

### D9. P1 结构 / 元数据补充

- 新增 `STD-STRUCT-008`（SHOULD）：一个仓库可以包含多个可部署 mod 目录；每个部署目录恰好一个 `IModMetadata`；monorepo 必须在 README 标注目录与部署边界。证据：11 个仓库含多个 metadata 目录（Gunwright 8、RCTA 12、Junko 8、Hj's 4.1/4.0 并行）。
- 新增 `STD-META-008`（SHOULD）：`package.json` + `sptVersion` 的 TS/JS 服务端 mod 是 3.11 遗产；4.1/5.0 新服务端 mod 使用 C# `IModMetadata`。证据：41 个 package.json mod，全部为 3.10/3.11 或工具；ABPS 的 3.11 TS 与 4.1 C# 双版本对照。
- 新增 `STD-VER-005`（MAY）：移植 3.11 TS mod 时，把 `sptVersion` → `SptVersion`、`loadBefore/loadAfter` → `TypePriority`、`incompatibilities` → `Incompatibilities`，并核对运行时版本门禁。证据：ABPS `package.json` / `src/mod.ts`。

### D10. P3 延后项

- **存档/Profile 迁移**：`AbstractProfileMigration` 只有 2 个语料样本；先作为 KB 操作笔记（`curated/operations/`）或 SRV 的“深入”链接，不写入规则。等 n≥10 再评估 `STD-SRV-010`。
- **诊断钩子**：`[ConsoleCommand]` 8+、API self-check 2、Stopwatch 2；先写 MAY 级 `VERIFY-010..012` 草案，或等更多样本。不进入 P1。
- **检查器处理器**：P1 只复用现有 handler；新增 handler 仅限稳定、低误报的规则（如 `STD-PERF-009` 的 `Thread.Sleep` 路径检查）。`async void`、Fika 程序集隔离、locale 布局等 C# 结构检查先 `check: null`，避免脆弱的正则/语法解析。

### D11. 注册表、校验器与文档同步

- `rules.json`：新增 `MP` / `LOC` domains；新增规则使用域内下一个未用编号；不重编号已有 86 条。
- `README.md`：13 维度 → 15 维度；规则计数、索引、扩展流程同步；新维度 ADR 链接。
- `validate-mod-standard.ps1`：维度数断言 13 → 15；domain slug 合法性；MUST 双源标记；新增规则的 prose/registry 逐字一致。
- `tests/mod-standard/run-fixtures.ps1`：为新增可检规则和新维度补夹具；现有夹具的维度数/规则数期望同步更新。
- `knowledge/spt-kb/index.json`：用 `sync-index.mjs --write` 登记新维度文档，`validate-index.mjs` 必须 exit 0。
- `version-matrix.md`：注明 MP / LOC 证据均为 4.1 Mono/BepInEx 5；5.0 无语料，Applies 先标 4.1。

### D12. 过渡与豁免

- `STD-CLI-003` 的修正是向后兼容的：`PatchAll()` 在“所有目标已验证”的条件下仍允许；现有 mod 不因使用 `PatchAll()` 被自动判 FAIL。
- `STD-DEP-001` 升级为 MUST 是条件规则：没有依赖的 mod 保持空字典/`null` 即合规；只有声明了依赖的 mod 才要求 key/range 正确。
- 新增 MUST（`STD-MP-001`、`STD-CFG-008`）只对新增/修改的 mod 强制；已有 mod 可用 `Waiver: STD-...` 记录理由，按现有豁免流程。
- 不追溯重跑已有合规报告；仅在 mod 下次修改时按新规则复核。

## Testing Decisions

- **S1（最高接缝，复用）**：`scripts/validate-mod-standard.ps1` 的 registry ↔ prose 双向校验。验证 ID 集合、title/Level/Applies/domain 逐字一致、维度数 15、MUST 双源标记。这是规则集的“外部行为”接缝，优先于任何新的解析器。
- **S2（复用）**：`tests/mod-standard/run-fixtures.ps1`。为新增可检规则补夹具：`STD-CLI-003` 的 `ModulePatch.Enable` 与受控 `PatchAll`、`STD-PERF-009` 的 client `Thread.Sleep`、`STD-CFG-008` 的显式重置/迁移、`STD-DEP-001` 的非空 key/range、`STD-META-004` 的 tilde 范围。断言 PASS/FAIL/N-A、exit code、发射 ID，不测 handler 内部实现。
- **S3（复用）**：`scripts/spt-kb/sync-index.mjs` + `validate-index.mjs`。新增 `MP` / `LOC` 维度文档必须进入 `index.json`，schema 2 校验 exit 0。
- **S4（复用）**：`tests/bootstrap/verify-standard-compliance.ps1`。确认 bootstrap 仍全绿，维度数/规则数变化没有破坏既有门禁。
- 不新开测试接缝：规则内容正确性由 S1/S2 和一次试点合规报告覆盖；检查器行为由 S2 锁定；KB 登记由 S3 覆盖。

## Out of Scope

- 本 spec 不直接修改任何规则 prose、`rules.json`、校验器或夹具；实施由后续 tickets 承载。
- 不修改 `templates/server-mod` / `templates/client-mod`；模板同步是后续 ticket。
- 不修改 `writing-spt-mod` 技能去引用新规则 ID；技能同步是后续 ticket。
- 不写 5.0 / IL2CPP 的 Fika、本地化、TS 规则；无语料。
- 不写存档/Profile 迁移规则；样本 n=2，先留在操作笔记。
- 不承诺 4.1+ 仍支持 TS 服务端 mod；语料中 0 个 4.1+ TS mod。
- 不为每条新规则实现检查器 handler；只对稳定、低误报的规则增加可检规格。
- 不追溯修改已有 mod 的合规报告或豁免记录。

## Further Notes

### 证据产物

- `.artifacts/forge-refresh-2026-10-18/corpus-pattern-scan.json` — 631 仓库 × 34,050 文件的模式存在性统计。
- `.artifacts/forge-refresh-2026-10-18/metadata-scan.json` — 依赖/迁移/诊断/Fika/补丁/阻塞调用的行级样例。
- `.artifacts/forge-refresh-2026-10-18/metadata-scan2.json` — 精确的 ModGuid / SptVersion / ModDependencies / monorepo / package.json 统计。

### 关键计数与口径

| 指标 | 计数 |
|------|------|
| 源码仓库 / 文件 | 631 / 34,050 |
| 含 `.csproj` / `package.json` | 551 / 40 |
| 引用 `SPTarkov.Server` / BepInEx | 302 / 409 |
| `ModulePatch` / `PatchSet` | 277 仓库 |
| `PatchAll()` | 91 文件 / 108 命中 |
| 非空 `ModDependencies` | 53 仓库 / 82 对 |
| `SptVersion` 声明 / tilde | 326 / 297（91%） |
| `ModGuid` 声明 / 唯一 / 反向域名 | 322 / 277 / 266（96%） |
| locale 相关仓库 / 文件 | 228 / 353 |
| `ConfigVersion` / 迁移相关 | 11 / 50 仓库 |
| profile/save 相关 | 90 仓库 |
| 诊断相关 | 194 仓库 / 2,171 命中 |
| Fika GUID 引用 / Dispatcher | 69 文件 / 36 文件 |
| `Thread.Sleep` / `async void` | 27 / 129 文件 |
| `SemaphoreSlim` | 60 文件 |
| TS `package.json` 服务端 mod | 41 仓库（全部 3.10/3.11 或工具） |
| monorepo 多 metadata 目录 | 11 仓库 |

口径警告：`.Result` / `.Wait()` 的 573 条原始命中包含大量 `await Task.WhenAll` 后的良性读取，不能当死锁计数；Fika / 本地化证据均为 4.1 Mono/BepInEx 5；5.0 语料极薄。

### 建议 ticket 拆分（供 `/to-tickets`）

| # | Ticket | 依赖 |
|---|--------|------|
| 01 | P0 证据刷新：EV-CORPUS-*、EV-GAP-DEP、EV-NOCORPUS、SptVersion/ModGuid 计数 | — |
| 02 | `STD-DEP-001` SHOULD→MUST + 规则证据更新 | 01 |
| 03 | 修正 `STD-CLI-003` + 新增 CLI-008..011 | 01 |
| 04 | 新增 `STD-SRV-009` 服务端补丁生命周期 | 01 |
| 05 | 新增 `STD-CFG-007..010` 配置迁移 | 01 |
| 06 | 新增 `STD-PERF-009/010` + `STD-LOG-006/007/008` | 01 |
| 07 | 新增 `STD-STRUCT-008`、`STD-META-008`、`STD-VER-005` | 01 |
| 08 | ADR-0013 + `MP` 维度与 MP-001..005 | 03/04（生命周期规则评审后） |
| 09 | ADR-0014 + `LOC` 维度与 LOC-001..005 | 01 |
| 10 | P3 诊断钩子 `VERIFY-010..012`（可延后） | 01 |
| 11 | 校验器 / fixtures / README / index / version-matrix 同步 | 02–10 |
| 12 | `templates/*` 与 `writing-spt-mod` 技能同步（后续） | 02–10 |

### 风险与缓解

- **规则膨胀**：新增候选多，容易把标准写成百科全书。缓解：P0/P1/P2 分批；n<10 一律 SHOULD/MAY；P3 延后。
- **误伤现有 mod**：`CLI-003` 修正与 `DEP-001` 升级可能改变检查结果。缓解：条件式规则 + 过渡说明 + 豁免流程；不追溯重跑。
- **校验器维度数**：`validate-mod-standard.ps1` 硬编码 13 维度；加 MP/LOC 必须同步改断言与 fixtures，否则 bootstrap 红。
- **证据漂移**：语料持续增长；以 2026-10-18 为锚，`EV-REVIEW` 触发复核。
- **5.0 不对称**：Fika/LOC 只有 4.1 语料；Applies 先标 4.1，避免把 4.1 结论伪装成跨版本。

### 开放决策（需 Overseer 确认）

1. `MP` / `LOC` 是否作为独立维度，还是折入 `CLI` / `SRV` / `STRUCT`？建议独立维度 + ADR，因为两者都正交且语料规模大。
2. `STD-CLI-003` 是否保留 `PatchAll()` 作为受控选项，还是直接弃用？建议保留受控选项，避免追溯性破坏。
3. `STD-DEP-001` 是否立即升 MUST？建议是：53 仓库 + 机制双源达标。
4. P3 诊断钩子是否进入本轮？建议延后，先完成 P0/P1 与两个新维度。
