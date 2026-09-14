# 试点合规报告：tarkov-active-probe

> 试点对象：`tools/tarkov-active-probe/`（类型：server；目标 SPT 版本：5.0）
> 规则版本：Modding Standard 84 条（commit 635a1c38）
> 报告日期：2026-09-14

## 摘要

- 判定统计：PASS 23 / FAIL 6 / N-A 55（合计 84）
- 关键缺口 Top 3（按严重度）
  1. 路由 TypePriority 未按 `Routers + 1` 偏移（STD-SRV-005，MUST）。
  2. 路由内联业务逻辑、未拆独立 Callbacks（STD-SRV-007，SHOULD）。
  3. 安装路径属性名偏离标准（`SPT5Runtime` 而非 `SPTInstallPath`，STD-BUILD-006，SHOULD）。
- 规则修订建议数：8
- 判定口径说明：本 mod 为 5.0 服务端形态，无客户端组件、无配置、无日志、无 bundle、无发布归档；规则集中不存在 `Applies: 4.1.5` 的条目，全部为 `both` 或 `5.0`，故无因版本失配而判 N-A 的规则。MAY 级规则未采用时按「可选实践未采用，不构成违规」判 N-A（与同批 `spt5-no-stamina-drain` 报告口径一致）。目标版本由 `src/ModMetadata.cs:17` 的 `SptVersion = new("~5.0.0")` 与 `TarkovActiveProbe.csproj:19,24-39` 的 `SPT5Runtime` 引用共同判定。

## 逐规则核对

| Rule ID | 判定 | 证据（文件:行）与说明 | 修复建议 |
|---------|------|----------------------|----------|
| STD-STRUCT-001 | FAIL | 仓库根 `.gitignore` 已全局忽略构建产物 `**/bin/`、`**/obj/`（`.gitignore:35-36`）与部分用户文件 `*.log`/`*.tmp`/`*.bak`/`Thumbs.db`/`Desktop.ini`（`.gitignore:8-12`），但缺 `.vs/`、`.idea/`、`*.user` 等 IDE 文件；`tools/tarkov-active-probe/` 无自有 `.gitignore`。 | 在根 `.gitignore` 补 `.vs/`、`.idea/`、`*.user`（或于 mod 子目录增补）。 |
| STD-STRUCT-002 | PASS | 根 `.gitignore:35-36` 以 `**/bin/`、`**/obj/` 任意深度忽略构建产物；磁盘存在 `bin/Release/`、`obj/` 但被忽略。按任务边界未运行 git，判定基于忽略规则（未核验索引）。 | 无。 |
| STD-STRUCT-003 | PASS | 源码置于 `src/`（`src/ActiveProbeRouter.cs`、`src/ModMetadata.cs`），工程文件留根（`TarkovActiveProbe.csproj`）。 | 无。 |
| STD-STRUCT-004 | N-A | 非 paired：mod 仅含服务端组件（`TarkovActiveProbe.csproj:24-39` 仅引用 `SPTarkov.*`，无 `BepInEx`），无 `Client/`+`Server/` 分层需求。 | 无。 |
| STD-STRUCT-005 | FAIL | mod 目录无 README；仓库根 `README.md` 为插件级说明，全文未提及本 mod 的用途/安装位置（`README.md` 无 `tarkov-active-probe`）。 | 增 `tools/tarkov-active-probe/README.md`（用途、部署到 `user/mods/`、只读路由说明）；口径见修订建议 #4。 |
| STD-STRUCT-006 | PASS | 仓库根 `LICENSE` 存在（MIT，`README.md:103` 声明），覆盖仓库内全部内容。 | 无。 |
| STD-STRUCT-007 | N-A | 仓库根无 `.editorconfig`；MAY 级为可选实践，未采用不构成违规。 | 可选：补 `.editorconfig` 统一风格。 |
| STD-META-001 | PASS | `src/ModMetadata.cs:10` `record TarkovActiveProbeMetadata : IModMetadata`，程序集内唯一实现，覆盖全部属性（`ModMetadata.cs:12-22`）。 | 无。 |
| STD-META-002 | PASS | 元数据实现位于独立文件 `src/ModMetadata.cs`。 | 无。 |
| STD-META-003 | PASS | `ModGuid = "com.sammeow.tarkov-active-probe"`（`ModMetadata.cs:12`），反向域名、多段、全局唯一。 | 无。 |
| STD-META-004 | PASS | `SptVersion = new("~5.0.0")`（`ModMetadata.cs:17`），tilde 范围。 | 无。 |
| STD-META-005 | PASS | `Version = new("0.1.0")`（`ModMetadata.cs:16`）与 csproj `<Version>0.1.0</Version>`（`TarkovActiveProbe.csproj:11`），均三段式 semver。 | 无。 |
| STD-META-006 | N-A | 无客户端插件，无 `[BepInPlugin]` 声明。 | 无。 |
| STD-META-007 | N-A | 非 paired mod，无两端版本联动。 | 无。 |
| STD-BUILD-001 | PASS | `<TargetFramework>net10.0</TargetFramework>`（`TarkovActiveProbe.csproj:4`）。 | 无。 |
| STD-BUILD-002 | N-A | 无客户端工程，无 Mono 兼容框架要求。 | 无。 |
| STD-BUILD-003 | N-A | 无客户端运行时程序集（BepInEx/Harmony/Assembly-CSharp）引用。 | 无。 |
| STD-BUILD-004 | PASS | 引用 `SPTarkov.Server.Core`、`SPTarkov.DI`、`SPTarkov.Common`（`TarkovActiveProbe.csproj:24-35`），`HintPath` 指向 5.0 运行时 DLL（`:19`），版本与运行时一致。用本地 `<Reference>` 而非 NuGet `PackageReference`，属规则允许的次选（语料 31/114）。 | 无。 |
| STD-BUILD-005 | PASS | `<AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>`（`TarkovActiveProbe.csproj:20`）。 | 无。 |
| STD-BUILD-006 | FAIL | 以 `SPT5Runtime` 属性定位安装目录并支持 `dotnet build -p:SPT5Runtime=...` 覆盖（`TarkovActiveProbe.csproj:17,19`），语义等价但属性名非规则规定的 `SPTInstallPath`。 | 改名 `SPTInstallPath`；或按修订建议 #1 允许版本化属性名。 |
| STD-SRV-001 | PASS | `[Injectable(TypePriority = OnLoadOrder.Routers)]`（`ActiveProbeRouter.cs:20`）；依赖 `JsonUtil`/`HttpResponseUtil`/`ProfileActivityService` 经构造函数注入（`:21-24`）。 | 无。 |
| STD-SRV-002 | PASS | TypePriority 基于 `OnLoadOrder.Routers` 阶段常量，无裸数字（`ActiveProbeRouter.cs:20`）。 | 无。 |
| STD-SRV-003 | N-A | 未实现 `IOnLoad`，无生命周期入口。 | 无。 |
| STD-SRV-004 | N-A | 未实现 `IOnUpdate`，无周期性任务。 | 无。 |
| STD-SRV-005 | FAIL | 用 `StaticRouter` 注册精确路径（`ActiveProbeRouter.cs:25-29`，路由 `/spt/runtime/active-profiles`），但 TypePriority 为 `OnLoadOrder.Routers`（`:20`），缺规则要求的 `+1` 偏移（机制依据 `api-notes-4.1/http-routing.md:24`「新路由：`Routers + 1` 起」）。 | 改为 `[Injectable(TypePriority = OnLoadOrder.Routers + 1)]`；分级依据见修订建议 #2。 |
| STD-SRV-006 | PASS | action lambda 为五参 `(_, _, _, _, _)`（`ActiveProbeRouter.cs:30`），含末位 `CancellationToken` 形参；handler 为同步读取，无后续可传播调用。 | 无（可选：具名形参）；`_` 是否算「显式声明」见修订建议 #3。 |
| STD-SRV-007 | FAIL | 业务逻辑内联在 Router 的 action lambda：直接调 `profileActivityService.GetActiveProfileIdsWithinMinutes(30)` 并组装响应（`ActiveProbeRouter.cs:30-36`），无独立 Callbacks 类。 | 抽 `ActiveProbeCallbacks`（`[Injectable]`）承载查询与响应组装，Router 只声明路由并转发。 |
| STD-SRV-008 | N-A | mod 无任何日志调用（`ActiveProbeRouter.cs` 未注入 `ISptLogger<T>`），不存在日志机制选择点。 | 无（零日志合规性见修订建议 #7）。 |
| STD-CLI-001 | N-A | 无客户端组件（无 `BaseUnityPlugin`/`BasePlugin` 入口）。 | 无。 |
| STD-CLI-002 | N-A | 无客户端插件 GUID。 | 无。 |
| STD-CLI-003 | N-A | 无 Harmony patch。 | 无。 |
| STD-CLI-004 | N-A | 无 Harmony patch 目标。 | 无。 |
| STD-CLI-005 | N-A | 无 BepInEx 插件依赖。 | 无。 |
| STD-CLI-006 | N-A | 无客户端日志。 | 无。 |
| STD-CLI-007 | N-A | 无 Harmony 生命周期。 | 无。 |
| STD-CFG-001 | N-A | mod 无配置文件，无 `ModHelper.GetAbsolutePathToModFolder` 路径拼接。 | 无。 |
| STD-CFG-002 | N-A | 无 `config/config.jsonc`。 | 无。 |
| STD-CFG-003 | N-A | 未实现 `IOnDIConstruct`/`AddSingleton`。 | 无。 |
| STD-CFG-004 | N-A | 无配置类（POCO/record）。 | 无。 |
| STD-CFG-005 | N-A | 无 `defaultConfig.jsonc`。 | 无。 |
| STD-CFG-006 | N-A | 无客户端配置。 | 无。 |
| STD-LOG-001 | N-A | 无日志调用，不存在绕过 `ISptLogger<T>` 的情形。 | 无（零日志合规性见修订建议 #7）。 |
| STD-LOG-002 | N-A | 无日志级别选择点。 | 无。 |
| STD-LOG-003 | N-A | 无客户端组件。 | 无。 |
| STD-LOG-004 | N-A | 无 `catch` 块，规则前提不成立。 | 无。 |
| STD-LOG-005 | N-A | 无异步 IO / `CancellationToken` 传播点。 | 无。 |
| STD-DEP-001 | N-A | 无服务端硬依赖需声明。 | 无。 |
| STD-DEP-002 | PASS | `ModDependencies` 无初始化器，保持 `null`（`ModMetadata.cs:20`），未写入可选依赖。 | 无。 |
| STD-DEP-003 | N-A | 无可选依赖集成场景。 | 无。 |
| STD-DEP-004 | N-A | 无客户端插件依赖。 | 无。 |
| STD-DEP-005 | N-A | 无客户端软依赖。 | 无。 |
| STD-PKG-001 | N-A | 试点为源码仓库，仓库内无发布归档（无 zip/dist），无法核对归档顶层布局；判定对象为发布产物，见修订建议 #8。 | 无。 |
| STD-PKG-002 | N-A | MO2 overlay 与 `meta.ini` 位于外部 MO2 实例（部署记录见 `.scratch/tarkov-runtime-mcp/issues/08-active-profile-following.md:18`），不在仓库范围，无法核验。 | 无。 |
| STD-PKG-003 | N-A | 非 paired，无两端单归档需求。 | 无。 |
| STD-PKG-004 | PASS | 服务端目录仅一个程序集、仅一个 `IModMetadata` 实现（`src/ModMetadata.cs:10`）。 | 无。 |
| STD-PKG-005 | N-A | 非 paired。 | 无。 |
| STD-PKG-006 | FAIL | 仓库根有 `README.md` 与 `LICENSE`，但 README 为插件级说明，未含本 mod 的安装步骤/兼容性说明（同 STD-STRUCT-005）。 | 增 mod README；重复口径见修订建议 #4。 |
| STD-PKG-007 | N-A | 单一来源，无多来源合并/旧目录迁移。 | 无。 |
| STD-VERIFY-001 | PASS | Release 产物存在：`bin/Release/TarkovActiveProbe.dll`（及 `.pdb`、`.deps.json`）。按任务边界未重跑构建。 | 无。 |
| STD-VERIFY-002 | PASS | 试点记录：经 MO2/VFS 启动 SPT5 server，探针 mod 成功加载，`/spt/runtime/active-profiles` 返回预期形状（`.scratch/tarkov-runtime-mcp/issues/08-active-profile-following.md:18`）。 | 无。 |
| STD-VERIFY-003 | PASS | SPT 自身为每个已加载 mod 输出加载行 `Mod: {name} version: {version} by: {author} loaded`（`tools/tarkov-runtime-mcp/src/logs/mod-list.ts:12-15`），本 mod 可被日志断言；issue 08 记录其加载成功。 | 无。 |
| STD-VERIFY-004 | N-A | 无客户端插件。 | 无。 |
| STD-VERIFY-005 | PASS | 本 mod 即 tarkov-runtime MCP 的结构化探针：路由由 MCP 握手探测、可用即跟随活跃 profile（`docs/adr/0004-optional-active-probe-mod.md:3`；issue 08:18）。注：活跃 profile 跟随的 live 断言未完成（issue 08:19 记客户端缺失）。 | 无。 |
| STD-VERIFY-006 | N-A | 未执行 spt-mcp 离线预检流程；另见修订建议 #6（`spt_list_mods(server)` 仅扫 `package.json`，C# 服务端 mod 不可见）。 | 无。 |
| STD-VERIFY-007 | N-A | 未出现失败信号，无移交流程适用。 | 无。 |
| STD-VERIFY-008 | N-A | mod 只读、不写 profile/存档，不涉及 profile 边界验证。 | 无。 |
| STD-VERIFY-009 | N-A | MAY 可选深验；mod 影响不在局内（仅暴露只读路由）。 | 无。 |
| STD-VER-002 | PASS | 复用 4.1 骨架：`IModMetadata`（`ModMetadata.cs:10`）、`[Injectable]`（`ActiveProbeRouter.cs:20`）、`StaticRouter`（`:25`），无无依据重写。 | 无。 |
| STD-VER-003 | N-A | MAY 可选；未引用 `SeasonTable`/`ShopTable` 等 5.0 新扩展点（不依赖即合规）。 | 无。 |
| STD-VER-004 | PASS | 面向 5.0：csproj 直接引用 5.0 运行时 DLL（`TarkovActiveProbe.csproj:19,24-39`）；服务端仅用 5.0 API（`SPTarkov.Server.Core.Services.Profile.ProfileActivityService`，`ActiveProbeRouter.cs:4,24`）；无客户端类名绑定、无 EFT 模型表引用。 | 无。 |
| STD-BND-001 | N-A | 无 `bundles/` 与 `bundles.json`。 | 无。 |
| STD-BND-002 | N-A | 无 `bundles.json` manifest。 | 无。 |
| STD-BND-003 | N-A | 无客户端私有 bundle。 | 无。 |
| STD-BND-004 | N-A | 无 bundle 脚本绑定。 | 无。 |
| STD-BND-005 | N-A | 无 bundle。 | 无。 |
| STD-BND-006 | N-A | 无数据库覆盖数据（无 `db/`）。 | 无。 |
| STD-PERF-001 | N-A | 无客户端 tick 射线/物理查询 patch。 | 无。 |
| STD-PERF-002 | N-A | 无客户端 AI 子系统每帧遍历。 | 无。 |
| STD-PERF-003 | N-A | 无寻路/路径计算。 | 无。 |
| STD-PERF-004 | N-A | 无客户端 Harmony patch。 | 无。 |
| STD-PERF-005 | PASS | 路由 handler 仅调用服务读取列表并序列化（`ActiveProbeRouter.cs:30-36`），未引入 `GC.Collect`、`Thread.Sleep` 或全局锁包裹 I/O。 | 无。 |
| STD-PERF-006 | N-A | MAY 可选；未引入 O(n²) 查找/排序或逐次反射（查询委托 server 内部服务，`ActiveProbeRouter.cs:32`）。 | 无。 |
| STD-PERF-007 | N-A | 路由请求体为 `EmptyRequestData`，无文件名/Id 等外部路径输入。 | 无。 |
| STD-PERF-008 | N-A | 无外部输入校验点（请求体为空类型）。 | 无。 |

## 规则修订建议（反向校准）

| # | 涉及规则 | 问题类型 | 建议修订 | 理由 |
|---|----------|----------|----------|------|
| 1 | STD-BUILD-006 | 分级不当 | 将属性名约定明确为「推荐 `SPTInstallPath`」，允许语义等价的可覆盖属性名（如版本化的 `SPT5Runtime`）；把「不得无条件硬编码绝对路径」保留为强制语义。 | 试点用 `SPT5Runtime`（`TarkovActiveProbe.csproj:17,19`）同时服务 5.0 运行时，意图（可覆盖、默认值可改）与规则一致，仅名称不同；规则未说明多 SPT 版本共存时的命名。 |
| 2 | STD-SRV-005 | 分级不当 | 把 `Routers + 1` 偏移细节移入 `STD-SRV-002`（或将其自身降为 SHOULD），MUST 只保留「游戏流量走 Router、精确匹配用 `StaticRouter`、TypePriority 基于 `OnLoadOrder` 常量」。 | 该规则 Evidence 的语料计数仅覆盖 `StaticRouter`/`DynamicRouter` 出现次数，未覆盖 `TypePriority` 偏移写法；`+1` 是机制推断（`api-notes-4.1/http-routing.md:24`），单源证据不足以支撑 MUST。 |
| 3 | STD-SRV-006 | 表述不清 | 明确 `(_, _, _, _, _)` 这类匿名丢弃参数是否视为「声明了 `CancellationToken`」；若要求具名，措辞与示例需写明。 | 试点 action 形参含末位 token 但以 `_` 丢弃（`ActiveProbeRouter.cs:30`），「即使不用也要声明」的措辞无法判定通过与否。 |
| 4 | STD-STRUCT-005 / STD-PKG-006 | 无法遵守 | 明确 monorepo（多 mod 共仓、工具子目录）场景：README 应置于 mod 子目录还是仓库根；两规则对 README 的要求重复，建议合并或明确分工（STRUCT=仓库卫生，PKG=发布随附）。 | 试点 mod 无自有 README，仓库根 README 为插件级说明；同一缺口被两条规则重复计分。 |
| 5 | STD-STRUCT-001 / -002 / -006 | 表述不清 | 定义「仓库根」在单 mod 仓库与 monorepo 下的判定口径：monorepo 下是「仓库根有对应文件即可」还是「每个 mod 子目录自带」。 | 试点为 monorepo 子目录，`.gitignore`/`LICENSE` 仅存在于仓库根，规则未给出判定口径。 |
| 6 | STD-VERIFY-006 | 与机制矛盾 | 补充说明：`spt_list_mods(type=server)` 只扫描含 `package.json` 的（JS/TS）mod；C# 服务端 mod（DLL-only）需改用日志解析 / `spt_scan_mod_files`，或扩展 MCP 覆盖 DLL-only mod。 | 主流 C# 服务端 mod 无 `package.json`，按当前规则执行会漏掉本类 mod，离线预检对试点不可执行。 |
| 7 | STD-LOG-001 | 表述不清 | 明确「完全无日志的 mod」是否合规：若不要求，注明；若要求至少一条加载/就绪日志，另立 SHOULD 规则。 | 试点零日志，规则只约束「日志必须走 `ISptLogger`」，无法判定零日志情形（本报告判 N-A）。 |
| 8 | STD-PKG-001 / STD-PKG-002 | 表述不清 | 注明规则适用对象为发布归档 / MO2 overlay（安装产物），源码仓库内无产物时应判 N-A 而非 FAIL。 | 试点为源码仓库，overlay/meta.ini 在外部 MO2 实例，无法在仓库内判定，报告只能 N-A。 |

## 已知豁免记录（如有）

无。本 mod 的 6 项 FAIL 均为应修复项，未走 `Waiver: STD-XXX-nnn` 豁免流程；其中 STD-SRV-005（MUST）需优先修复，其余 5 项为 SHOULD 级设计/命名/文档问题，均有直接修复方案。
