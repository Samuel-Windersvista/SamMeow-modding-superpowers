# 08: 外置文件加载与分发（配置外置化基础设施）

**What to build:** 把全部 11 张数据表以**外部可编辑文件**形式随包分发（overlay 内 `config/` 子树），并实现统一加载语义：外部优先 → 缺失时从内嵌模板重建（一行告警）→ 解析失败告警并回落内嵌；支持 JSONC（注释/尾逗号）；条目级容错（单条坏不整体失败）。玩家编辑外部文件重启即生效；删文件自动重建；写坏不崩服。契约细节见 `spec-v0.2.0.md` D1/D2/D6/D8。

**Blocked by:** None（可立即开始）

**Status:** ready-for-agent

- [ ] 新增共享「外部文件解析」助手（模目录定位沿用既有 `ResolveModDirectory` 模式）；三套数据加载器（TrueItems 六表 / FeatureTables（armbands、backpacks）/ Softcore 四表）统一改走
- [ ] 语义：外部优先；缺失 → 从内嵌重建（含告警）；解析失败 → 告警 + 回落内嵌；条目级容错
- [ ] 分发文件集合（11 个）：`config/trueitems/{barter,clothing,keycards,medicals,partsnmods,provisions}.json`、`config/armbands.json`、`config/backpacks.json`、`config/softcore/{scavcase,fleamarket,crafting-rebalance,crafting-recipes}.json`；模板内容 = 内嵌同源，文件头加中文 JSONC 注释
- [ ] `build.ps1` 幂等拷贝（data 源 → overlay `config/` 子树；产物 = DLL + config.json + config/ 全树）
- [ ] 单测：外部覆盖生效 / 缺失重建 / 损坏回落 / JSONC 解析 / 条目级容错；现有 215 项保持绿
- [ ] 参考：spec `D1/D2/D6/D8`、`align-report-2026-10-16.txt`

## Comments

### 实施登记（2026-10-16，v0.2.0 / 工单 08）

**改动文件清单**
- 新增 `src/InescapableTarkovsSoftcore/Features/ExternalJsonFile.cs`（唯一共享缝）：`ReadTableText`（外部优先 / 缺失重建 / 语法失败回落，不覆写用户文件）、`ReadTable<T>`（模型结构失败回落内嵌）、`ParseObject<T>`、`ParseArray<T>`（数组条目级容错）、`ResolveModDirectory(Assembly)`（静态兜底）、静态 pending 告警队列（`AddPendingWarnings` / `DrainPendingWarnings`）。
- 删除 `Features/EmbeddedJsonResource.cs`：职责并入助手（内嵌读取 + 序列化选项，含 `StringToMongoIdConverter`），无剩引用。
- `Features/FeatureTables.cs`：`Armbands`/`Backpacks` 静态入口改走助手；新增 `internal ReadArmbands/ReadBackpacks(modDirectory, warnings)`。
- `Features/TrueItems/TrueItemsResourceLoader.cs`：`Load` 静态 Lazy 缓存保留；新增 `internal LoadTables(modDirectory, warnings)`。
- `Features/Softcore/{ScavCase,FleaMarket,Crafting}ResourceLoader.cs`：`Load`/`LoadRebalance`/`LoadRecipes` 静态 Lazy 缓存保留；新增 `internal LoadFrom` / `LoadRebalanceFrom` / `LoadRecipesFrom`。
- `Features/ModuleOrchestrator.cs`：合计行后 `DrainPendingWarnings()` 并以 `ISptLogger<ModuleOrchestrator>`（Warning）逐条输出；空队列 no-op；不并入 `OrchestrationReport` 计数。
- `InescapableTarkovsSoftcore.csproj`：新增 `<InternalsVisibleTo Include="InescapableTarkovsSoftcore.Tests" />`。
- `scripts/build.ps1`：新增 data → overlay `config/` 幂等拷贝 + 12 项存在断言 + 产物清单输出；头注释更新（保持 UTF-8 BOM）。
- `data/` 12 个表文件：仅加统一中文 JSONC 文件头（2 行），数据内容不变。
- 测试：新增 `tests/.../ExternalJsonFileTests.cs`（12 用例）；`ModuleOrchestratorTests.cs` 增 1 用例（外置告警排空输出）。

**测试证据**
- `dotnet test "…\InescapableTarkovsSoftcore.sln" --artifacts-path .artifacts/its-08 -c Debug`：**228 通过 / 0 失败**（基线 215 + 新增 13）。
- `dotnet build -c Release`（src）：**0 告警 0 错误**。
- `scripts/build.ps1` 跑通；overlay 产物 **14 项** = DLL + `config.json` + 12 数据表（`config/trueitems/*` ×6、`config/armbands.json`、`config/backpacks.json`、`config/softcore/*` ×4）。

**设计选择**
- 缓存：保留在各加载器入口层（`FeatureTables` 静态属性初始化 / 各 `ResourceLoader` 静态 `Lazy`）；显式目录重载不缓存，供测试隔离。
- 可见性：`ExternalJsonFile` 与加载器 internal 重载 + `InternalsVisibleTo`（不扩大公共 API 面）。
- `EmbeddedJsonResource<T>`：删除并并入助手，避免死代码。
- 告警排空点：`ModuleOrchestrator.Run` 主路径末尾（模块循环 + 合计行之后）。
- 解析失败回落分层：JSON 语法非法在 `ReadTableText`（返回内嵌文本），模型结构非法在 `ReadTable<T>`（重解析内嵌）；两者均告警且不覆写用户文件。

**登记偏差**
- 徽标行「11 张」为计数笔误；按显式清单实现 **12 个**外置文件。
- 静态加载器无法注入 `ModHelper`，模块目录解析采用 `ConfigLoader.ResolveModDirectory` 兜底分支（`assembly.Location` 目录；空则 `AppContext.BaseDirectory`）。静态上下文无 DI；生产环境 DLL 所在目录即 mod 目录，两者等价。
- TrueItems 旧解析器 `PropertyNameCaseInsensitive=false` 统一为助手的 `true`（模型均带 `[JsonPropertyName]`，行为不变；外部文件大小写更宽容）。
- 测试副产品：静态加载器在测试输出目录（gitignore 的 `bin/`、`.artifacts/`）重建 `config/` 树，属可接受副产物。

**未决 / 遗留**
- 端到端「零缺失」需部署期服务器日志验收（工单 17）。
- C 类数据对齐（工单 09-13）不在本工单范围。

### 复验回归修复（2026-10-16）

**根因**：`ExternalJsonFile` 的 pending 告警队列是进程级静态共享状态；xUnit v2 默认不同 collection 并行执行。冷启动时任一 loader 首次「缺失重建」会并发调用 `AddPendingWarnings`，与 `PendingWarnings_AddAndDrain_AreQueueSemantics` 的「清空 → 入队 → 排空」精确相等断言交错，导致 `Assert.Equal([a,b,c], drained)` 失败（1 failed / 227 passed）。此前两次全绿系热启动侥幸（测试输出目录内 config 树已存在，重建告警消失，掩盖竞态）。

**修复机制**：测试工程新增程序集级序列化 `[assembly: CollectionBehavior(DisableTestParallelization = true)]`（`tests/.../TestAssemblyBehavior.cs`），并保留队列相关用例「先 Drain 清场 → 触发 → 断言自身内容」的顺序无关模式（`PendingWarnings_AddAndDrain_AreQueueSemantics`、`ModuleOrchestratorTests.Run_DrainsPendingExternalFileWarnings_AtWarningLevel` 均先清场）。测试仅百毫秒级，串行成本可忽略，并为后续工单根除同类全局态竞态。**未改任何产品代码（src/）语义**。

**验证证据（命令 + 数字）**
- 冷启动（全新路径 `.artifacts/its-08-fix1a`，运行前确认不存在）：
  `dotnet test "…\InescapableTarkovsSoftcore.sln" --artifacts-path .artifacts/its-08-fix1a -c Debug --nologo -v q`
  → 通过 228 / 失败 0。
- 热启动 #1（同路径）：通过 228 / 失败 0。
- 热启动 #2（同路径）：通过 228 / 失败 0。
- 矩阵稳定，无偶发。

**改动文件**：新增 `tests/InescapableTarkovsSoftcore.Tests/TestAssemblyBehavior.cs`；本 Comments 记录。（`[Collection("ExternalJsonFile")]` 保留，无害。）

### 审查收口（2026-10-16）

code-review 双轴（Spec / Standards）收口，仅动 src/ 加载器 + `ExternalJsonFile` + 测试 + 本文件。

**A. catch 缩窄（Standards S4）**
`ExternalJsonFile.cs` 两处 `catch (Exception)` 改为过滤式 `catch (Exception ex) when (ex is JsonException or FormatException)`：
- `ReadTable<T>` 的结构回落分支（保留 D2「解析失败回落内嵌」；坏 hex 的 MongoId 转换抛 `FormatException` 属数据非法）；
- `ParseArray<T>` 的元素级跳过分支。
每处加中文注释说明「过滤即容错边界」：仅吞数据非法（解析/格式），NullReference 等真 bug 继续 fast-fail。
（`ReadTableText` 的写盘 `catch (Exception)` 属 I/O 容错，不在本次范围。）

**B. 条目级容错扩展到 TrueItems 与 Softcore（Spec P2）**
新增 `ExternalJsonFile.ParseObjectTolerant<T>(json, label, warnings)`：对象其余字段正常绑定；对模型上每个 `List<E>` 字段逐元素、字符串键 `Dictionary<string,V>` 字段逐键试绑定，单条/单键非法 → 告警并跳过，其余照常；嵌套结构以一级元素为单位。仅此一个共享缝（私 helper `PruneArrayElements` / `PruneDictionaryEntries` / `Bind`）。
- TrueItems 六表：`List` / `ParentList` 逐元素容错（原来整表 `ParseObject`）。
- Softcore 四表：`ScavCaseTables`（Recipes 等 List、RewardItemValueRangeRub 字典）、`FleaMarketTables`（各 List、RequestWhitelist 字典）、`CraftingRebalanceTable`（RecipeAdjustments）、`CraftingRecipesTable`（AdditionalRecipes）全部改走 `ParseObjectTolerant`。
- 原非容错 `ParseObject<T>` 已无调用者，随重构移除（无死代码）。

**C. 加载器引导样板收敛（Standards S5）**
新增 `ExternalJsonFile.LoadProduction<T>(Assembly, Func<string, List<string>, T>)`：定位 mod 目录 → 执行加载 → 告警入 pending 队列。
7 处重复样板（FeatureTables ×2、TrueItems ×1、ScavCase/FleaMarket/Crafting ×1/×1/×2）全部收敛为该入口；各加载器**静态入口签名不变**（`Load()` / `LoadRebalance()` / `LoadRecipes()` / `Armbands` / `Backpacks`），internal 目录重载（测试用）不受影响。

**新增测试（3 项）**
- `TrueItemsLoader_EntryLevelTolerance_SkipsBadListAndParentListEntries`（List + ParentList 各一条坏，其余照常）；
- `FleaMarketLoader_DictionaryTolerance_SkipsBadKey`（requestWhitelist 单键值非法 → 跳过该键）；
- `ScavCaseLoader_EntryLevelTolerance_SkipsBadRecipe`（recipes 单条坏 → 跳过，其余照常）。

**验证证据（命令 + 数字）**
- 冷启动（全新路径 `.artifacts/its-08-fix2a`，运行前确认不存在）：`dotnet test "…\InescapableTarkovsSoftcore.sln" --artifacts-path .artifacts/its-08-fix2a -c Debug --nologo -v q` → 通过 233 / 失败 0（基线 230 + 新增 3）。
- 热启动复跑（同路径）：通过 233 / 失败 0。
- `dotnet build -c Release`（src）：0 告警 0 错误。

**不采纳项（评审备注，保持现状）**
- 与 `ConfigLoader` 的重复逻辑（目录定位 / JSONC 选项）：不动，依 spec D2「config 加载器保持现状」。
- `[Collection("ExternalJsonFile")]` 与单行程序集序列化文件（`TestAssemblyBehavior.cs`）：保留（评审备注项，无害且守护全局态用例）。
- `assembly.Location` 兜底目录解析偏差：保留（评审已认可；静态上下文无 DI，生产环境等价）。

### 复核保留项处置（2026-10-16，复验收口）

ora-2 复座复核确认 S4/S5/P2 全部闭环（并给出四类剪枝语义正确性核验：倒序删数组、快照删字典、过滤边界均无误），另留两项精修（语义正确性，非风格）。两项已由编排者直接修正并独立复验：

1. **`Bind` null 漏网**：原 `Bind` 未对 JSON 字面 `null` 抛 `JsonException`。现对 `node is null` 与绑定结果为空一律抛 `JsonException("条目为空")`，由剪枝层按条目级跳过 + 告警（与 `ParseArray` 的 null 语义一致）。`ToJsonString` 往返保留（加载期一次性开销可忽略；评审所述性能项不影响正确性）。
2. **节点定位大小写不对称**：原 `JsonNode.Parse(..., nodeOptions: null, ...)` 使 `TryGetPropertyValue` 区分大小写，与 `SerializerOptions`（不区分）不一致。现新增 `NodeOptions { PropertyNameCaseInsensitive = true }` 并统一用于 `ParseObjectTolerant` / `ParseArray`；外部文件字段名大小写变体（如 `list`/`stackmult`）可正确定位且照常走条目级剪枝。

**新增测试（2 项，判别性）**
- `TrueItemsLoader_NullListElement_IsSkipped_WithWarning`：List 含 JSON null 条目 → 跳过 + 告警，合法条目照常。
- `TrueItemsLoader_CaseVariantFieldNames_AreMatched_AndBadEntriesStillPruned`：小写字段名定位 + 坏条目仍走条目级剪枝（无「结构非法」整表回落）。

**验证证据**
- 冷启动（全新路径 `.artifacts/its-orch-w5b`，编排者独立执行）：`dotnet test "…\InescapableTarkovsSoftcore.sln" --artifacts-path .artifacts/its-orch-w5b -c Debug --nologo -v q` → 通过 **235** / 失败 0（233 + 新增 2）。

**闭环结论**：S4 / S5 / P2 + 两项保留项全部处置完毕；工单 08 相关审查记录至此封口。
