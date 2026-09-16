# 发布说明

## v0.2.0（未发布）

SamMeow SPT modding 工具包的纯 SPT 版本——面向 SPT 4.1 mod 开发与整合包自动化的 OpenCode 插件。

### 纯 SPT 化清理

- 移除并行的整个 BGS 半边：skills、知识库、MCP 服务器、tools、scripts 以及非 OpenCode 的 harness 入口。仓库现为纯 SPT、纯 OpenCode。
- 插件与包更名为 `spt-modding-superpowers`。OpenCode 插件入口现在只注入 SPT bootstrap。
- MCP 声明面恰好为 `mo2` + `spt`。
- 共享环境变量去掉 `BGS_` 前缀重命名。
- 移除已提交的物化插件树；分发产物由 `scripts/build-portable-plugin.ps1` 按需构建。
- 构建产物与 vendored 归档不再入库；加固 `.gitignore` 防止回流。
- 围绕纯 SPT 不变量重写 bootstrap 验证套件。
- 采用 Matt agent-skills 配置：`docs/agents/` 配置文件、入库的 `AGENTS.md`、本地 markdown 工单跟踪器、五个默认 triage 标签。

### Modding Standard + 仓库重校准（2026-09-14）

- **Modding Standard**（`knowledge/spt-kb/curated/modding-standard/`）：84 条可审计规则，13 维度（28 MUST / 51 SHOULD / 5 MAY），每条双源留证（机制 + 语料）。证据索引含 21 个 EV 锚点（392 个受调查源码目录的语料统计）、试点校准记录（R1-R8 修订，含 5.0 IL2CPP 客户端形态与 monorepo 适用性）、4.1.5 ↔ 5.0 版本矩阵。已登记机读索引 `index.json`（16 条）。
- **模板**：server/client 按规范升级（配置注册链、LICENSE、规则注释）；新增 `templates/paired-mod/`（Client/Server/Shared，根级 `Directory.Build.props` 版本联动 + `pack.ps1`）。`writing-spt-mod` skill 重接三套模板，附 60 条规则 ID 引用表与豁免流程。
- **二期检查器**：`scripts/check-mod-standard.ps1`（约 25 条机检规则，monorepo 感知，模板占位符 SKIP，`Waiver: STD-XXX-NNN: <reason>` 豁免约定），已接入 bootstrap 套件为 `verify-standard-compliance.ps1`。四套模板目标全绿。
- **布局重校准**：移除 BGS 时代残留——过期物化的 `plugins/bgs-modding-superpowers` 树、空的 `hooks/` / `.claude-plugin/` / `.codex-plugin/` / `.agents/` 目录、以及被跟踪的 BGS `.mcp.json`（指向旧 BGS 插件树）。`external/spt-archive`（支撑 KB 的 gitignored 本地语料）移出布局 absent 清单。bootstrap 套件 8/8 全绿。

### 姿态调整：抢救前提退役（2026-09-14）

- SPT 5.0 已正式发布（官方组织 2026-08-11 归档全部仓库后，SP-Tushonka 社区 fork 成为事实延续）。创立前提——「SPT 可能停止运作，立即归档一切」——经 ADR-0006 全仓退役。
- 新姿态：现役生态工具链。归档层（wiki vendor 副本、Forge 归档、源码登记册）已建成，转为长期参考 + 离线兜底。
- 版本策略重述：4.1.5 稳定开发基线（Modding Standard Dev-Baseline，理由改为生态成熟而非版本终局性）；5.0 为已发布新主线（version-matrix 管辖双轨）；3.11.x 仅历史对照。「final locked version」表述已从 README、VERSIONS.md、skills、docs 清除。
- 本地资产清单扩展为六项：20 仓归档 clone、4.1.5 源码 fork、3.11.4 源码、5.x 源码、SPT 特化版 MO2 源码（`SamMeow-Tarkov-specific-Mod-Organizer`）、特化版 MO2 构建产物（`E:\build\spt-mo2\prefix\install\bin`）。
- ADR-0002（tarkov-runtime-mcp 只适配 5.x）维持不变——5.0 发布后该范围决策反而更加成立。
- 后续：按正式 release tag 复核 5.0 相关的 KB 内容（api-notes-5.0 的 UNSTABLE 标记、5xx-source-verification 状态、version-matrix 快照基线）。

### 架构（wayfinder，2026-08-02）

全部架构决策经 wayfinder 地图锁定（`docs/wayfinder/MAP.md`）。7 张票关闭，1 张延期。

### 新增

- **SPT 知识库**（`knowledge/spt-kb/`）：wiki vendor 副本、4.1 mod 开发指南、API 笔记、任务配方、Forge 归档（全站目录 + 热门详情 + 成品 zip + 源码 clone）。机读索引 `index.json`（可按 version/domain/topic 过滤；条目数随 KB 增长，以文件为准，不再在文档中固化）。
- **SPT 核心 skills**（5 个）：`using-spt-modding-superpowers`（bootstrap + 路由）、`writing-spt-mod`（从模板开发 mod）、`curating-spt-modpack`（6 阶段策展管线）、`spt-conflict-audit`（20 类冲突分类学）、`building-spt-modpack`（MO2 profile 构建）。
- **SPT 4.1 MO2 游戏插件**（`game_spt41.py`）：通用 MO2 的过渡兼容插件，处理 SPT_Runtime/ 路径布局。
- **冲突分类学**（`docs/wayfinder/findings/001-spt-conflict-taxonomy.md`）：跨服务端/客户端/跨层的 20 类冲突，按严重度（B/S/O/C）与检测方式（M/I/R）评级。
- **MO2 VFS 集成分析**（`docs/wayfinder/findings/002-mo2-vfs-spt-integration.md`）：3 个集成方案（A/B/C）、控制面差异、进程传播风险评估。

### 变更

- **README**：更新以反映纯 SPT 工具包、wayfinder 决策与当前进展。
- **KB INDEX.md**：更新 `external/spt-archive/` 内部路径，新增 agent 索引指针。

### 延期

- usvfs 进程传播验证（#8）—— ~~等待 SPT 特化版 MO2~~ **已解决（2026-08-05，ticket #8 CLOSED，见 wayfinder/MAP.md）**。
- ~~`spt-mcp-automation` skill~~——**已完成（2026-08-04，见下方「已完成」）**；此行系时序记录残留，保留以反映演进。
- `using-spt-translator` skill——**未实现**（架构审查 C6 记录：该技能从未落地，README 技能表中的幽灵行已移除；翻译能力待 SPT 方案成熟后另立）。
- IL 反编译管线——~~架构预留~~ **基础版已完成（3114 IL 冲突报告管线，tools/spt-mcp/il-helper）**。

### 已完成（2026-08-04）

- 8 个 SPT 支持 skills：`setting-up-spt-modding-environment`、`maintaining-spt-modding-environment`、`evaluating-spt-mods`、`interpreting-spt-mod-instructions`、`diagnosing-spt-problems`、`testing-spt-modpack`、`writing-spt-modpack-devlog`、`writing-spt-modpack-changelog`。全部面向 SPT 4.1，以本地 Forge 归档为数据源，接通 MO2 集成与冲突分类学。
- Mod 模板：`templates/server-mod/`（net10.0，IModMetadata + DI + 表注入模式）与 `templates/client-mod/`（netstandard2.1，BepInEx + Harmony + 配置模式）。全部 API 签名已对照 SPT 4.1 源码核实。两套模板对 stub 程序集编译零错误零警告。`{{PLACEHOLDER}}` 占位符格式供 agent 查找替换脚手架。
- `spt-mcp-automation` skill：spt MCP 服务器操作的枢纽 skill，含路由信条、反模式、子 agent 配方。
- `spt` MCP 服务器设计 spec（`docs/internal/mcp-specs/spt-mcp-design.md`）：5 类共 10 个工具（mod 清点、冲突分析、Forge 归档、知识库、MO2 桥接）。无守护进程——纯文件系统分析。读取 DLL 属性的 .NET helper CLI（延期）。
- `spt` MCP 服务器实现（`tools/spt-mcp/`）：7 个工具投运（list-mods、read-mod-metadata、scan-mod-files、analyze-conflicts、predict-load-order、forge-search、kb-query）；后续新增 `spt_health`，**现为 8 个**（见「运行时布局契约（C1）」）。冲突引擎：GUID 重复/版本失配/文件覆盖/配置碰撞检测 + 加载顺序预测。46/46 单元测试通过。TypeScript strict 模式零类型错误。经 stdio MCP 协议冒烟测试。
- 使用指南（`docs/使用指南.md`）：Overseer 快速上手——环境搭建、mod 编写、整合包构建、知识库检索、mod 搜索、排障、进度跟踪、系统边界、文件导航。
- 插件注册：`spt` MCP 服务器经 OpenCode 插件 `config.mcp` 钩子注册（源码 `tools/spt-mcp/`）。MCP 握手验证通过（spt-mcp v0.1.0）。
- 模板真实环境验证：server-mod 模板对真实 SPT 4.1 安装编译（0 错误 0 警告）。修复 csproj 缺失的 `SemanticVersioning.dll` 引用（真实编译发现的 bug，stub 验证未捕获）。客户端模板 DLL 引用核实齐备。
- MCP 真实环境验证：冲突分析检出真实版本失配（AlgorithmicLevelProgression 声明 SPT 3.11，目标 4.1 → Breaking）。forge-search 与 kb-query 经 live 数据验证（17 个 SAIN mod、12 份配方）。修复插件树运行时的 KB 根路径解析（当时做法为插件注册处注入 KB-root 环境变量；该做法已被 C1 取代——插件不再注入任何路径 env，改由共享运行时布局解析器统一解析并校验，见「运行时布局契约（C1）」）。已知限制：无 package.json 的服务端 mod（如 BarlogM-Unda）被跳过——需未来 .NET helper CLI 读取 DLL 属性。
- 可行性研究：3.11 → 4.1 mod 迁移（`docs/feasibility-311-to-41-migration.md`）。基于 Forge 归档真实 mod 源码分析。关键结论：3.11 → 4.1 是完全重写（TypeScript → C#、tsyringe → SPT DI、API 面完全不同），而非迁移。成功率：4.0→4.1 = 85-95%，3.11 简单 mod = 70-85%，3.11 复杂 mod = 20-40%。最大缺口：无 3.11→4.1 API 映射文档。
- 探索：资源包升级路径 + Blender MCP 集成（`docs/exploration-bundle-and-3d-pipeline.md`）。分析 Life_in_Norvinsk 整合包（177 mod、2622 个资源包、17.9 GB）。关键结论：SPT 3.11 与 4.1 共享同一 Unity 版本（2022.3.43f1）——75-85% 的资源包可直接拷贝，仅 3-8% 需重建。资源包迁移不是瓶颈；DLL 重编译才是。Blender MCP 对静态物品建模可行（B+），武器绑定/动画不可行（D）。详细结论见 `docs/wayfinder/findings/003-bundle-upgrade-analysis.md` 与 `004-blender-mcp-analysis.md`。
- 资源包差异审计（`docs/bundle-difference-audit.md`）：直接文件级验证（非文档推断）。事实基础：SPT 3.11 与 4.1 游戏均用 Unity 2022.3.43f1，但整合包 2622 个资源包中 85.4% 以 Unity 2019.4 构建（3.11 前向兼容）。0/2622 个资源包引用混淆类名——MonoBehaviour 绑定（PreviewPivot、EFT.Visual.LoddedSkin、HotObject）跨版本稳定。真实差异：shader 补丁级漂移（需逐 mod 游戏内验证，症状 = 模型发紫）与 IsBundleMod 元数据移除。迁移需要逐 mod 运行时验证，不能盲拷。
- 自动迁移可行性报告（`docs/feasibility-auto-migration-pipeline.md`）：3.11.4 → 4.1 mod 迁移管线（含资源包）自动化的完整评估。基于 Life_in_Norvinsk 真实扫描（91 个 TS 服务端 mod、132 个 DLL、2622 个资源包）。Council 评审（B+）。核心数字：服务端 TS→C# 67% 可自动（假设 locale 阻塞可解）、客户端 DLL 有源码 50-70% / 无源码 20-30%、资源包 80-90%、全链路零人工约 35-45%。识别 5 个阻塞项；locale 写入阻塞从「硬阻塞」降级为「大概率可解，需 10 分钟实验」。3 个 P0 门禁：4.1 dbdump、3.11 dbdump、GClass 编号对齐。支撑研究见 `docs/wayfinder/findings/005-auto-ts-to-csharp-conversion.md` 与 `006-auto-client-bundle-migration.md`。
- **Locale 阻塞已解决（v1.2，实证验证）**：发现 SPT 4.1 官方 locale 修改机制——`LazyLoad<T>.AddTransformer()`（PostDbLoadService.RenamePreraidLocales 在用）。构建并向 4.1 服务器部署 LocaleTest mod：transformer 注册 + 回读验证成功（"LocaleTest transformer WORKS on SPT 4.1"）。ETT 类 mod（locale 文本改写）现已可自动化。服务端自动化率从 67% 上调至 ~75%。
- **4.1 dbdump mod 已建成并验证**（`tools/dbdump-mod/`）：行为验证的基础设施。将 6 张关键表转储为 JSON 以供数据库状态 diff（templateItems 18.9MB、templateQuests 5.6MB、traders 4.3MB、globalsConfig 301KB、templateHandbook 515KB、locales 53MB 含懒加载 transformer 应用后状态）。已对真实 SPT 4.1 服务器部署并验证。
- **3.11 dbdump mod 已建成并验证**（`tools/dbdump-mod-311/`）：TypeScript/JS 行为基线工具。从 3.11 服务器转储相同 6 张表（items 17.3MB、quests 5.0MB、traders 2.4MB、globalsConfig 286KB、handbook 493KB、locales 47.9MB）。支持三路 diff（3.11 纯净 vs 3.11+mod vs 4.1+mod）验证迁移行为等价性。
- **P0 门禁完成（v1.3）**：4.1 dbdump 通过、3.11 dbdump 通过、locale 阻塞已解（实证）、GClass 编号部分验证（映射覆盖 3.11 全部 3920 个 GClass 名的 84.1%；缺失 624 个散布各区间；4.0 程序集不可得无法二进制比对；务实路径 = 映射覆盖 + 编译错误驱动迭代）。报告更新至 v1.3。
- **ETT 端到端试点通过**：迁移管线首次完整跑通（TS→C# locale mod）。6 个阶段全部验证：数据 carry-over（config/QuestInfo/GunsmithLocaleEN）、用 AddTransformer locale 模式的 TS→C# 转换、编译门（0 错误；编译器捕获 ListOrT/Path/Trader.Id/Item.Tpl API 失配）、加载门（mod 注册无错误）、行为门（dbdump diff 确认 5 处 locale 文本修改全部生效：Leads to 9775x、Collector 4403x、Lightkeeper 1734x、Durability 425x、Requires key 1156x）。关键经验记录于 `knowledge/spt-kb/curated/migration/pilot-experience-ett.md`（JsonUtil 大小写敏感、嵌套 requiredKeys 形态、编译错误驱动迭代）。迁移试点项目位于 `tools/migration-pilots/ett/`。
- **SecureMapbookMod 资源包试点通过**：管线第二次完整跑通（TS→C# + 物品资源包）。验证：资源包文件（4.94MB，Unity 2022.3.43f1）零改动直接加载、bundles.json 零改动兼容、IsBundleMod 字段移除、CustomItemService.CreateItemFromClone 迁移。修复一个真实 3.11 bug：原槽位 ID 生成用 (char)(98+i)，从第 6 个槽起产生非十六进制字符（4.1 中 MongoId 崩溃）；迁移版使用合法十六进制。行为经 dbdump 验证（mapbook 物品 6621a2e3a8d8b1a9f0e3b4c5 带槽位过滤器创建）。经验见 `knowledge/spt-kb/curated/migration/pilot-experience-secure-mapbook.md`。试点项目位于 `tools/migration-pilots/secure-mapbook/`。
- **SkillsExtended 客户端迁移：混淆解析 + 90% API 适配完成，深度逻辑重写待续（~8 点）**：经 AsmResolver 成员签名匹配解析全部 8 个混淆类名；68 个类名 + 35 个 EBuffId + 5 个 Item 替换；重写 GetBarterPricePatch（Assortment/ItemPrice）、MovementContextSetSpeedLimitPatch（SetPhysicalCondition），映射 Notification/Prone/Stats/Camera。编译错误从 40+ 降至 ~8 个独立深度重写点（Item 子类→组件系统：HealthEffectsComponent/KeycardComponent/Silencer；LockPicking 交互类型；SPT Utils API）。完整映射见 `knowledge/spt-kb/curated/migration/client-obfuscation-mapping-skills-extended.md`，接续点见 `tools/migration-pilots/skills-extended/PROGRESS.md`。
- KB 迁移知识新增 3 篇：`knowledge/spt-kb/curated/migration/api-mapping-311-to-41.md`（服务端 API 1:1 映射 + 542 文件用量统计 + 5 个阻塞项，locale 阻塞已标 SOLVED 附 AddTransformer 模式）、`client-mod-311-to-41.md`（客户端迁移指南）、`bundle-311-to-41.md`（资源包迁移指南）。index.json 增至 74 条。

### 运行时布局契约（C1，2026-09-16）

- **共享解析器**：新增 `shared/runtime-layout.mjs`（零依赖纯 ESM，插件与 spt-mcp 双端相对导入），KB 根 / forge 归档 / metadata+IL helper 的路径解析归一到唯一计算点。`env` 未设走默认派生 + 存在性校验；**显式设置却无效即报错，不静默回退**。
- **响亮失败**：KB 索引不可用时 `spt_kb_query` 返回 `kb_unavailable`；forge 归档数据缺失（目录在但 API 快照不在）时 `spt_forge_search` 返回 `kb_unavailable`；新增只读工具 **`spt_health`** 报告完整布局（mode / pluginRoot / 逐资源 path+source+ok+reason）；MCP 启动时把不可用资源写 stderr。IL helper 不可用时 `spt_analyze_conflicts` 在结果 `warnings` 中显式标注降级。
- **插件不再注入路径 env**：`config.mcp.spt.environment` 改为 `{}`（父进程 env 透传，用户自定义仍生效）；插件仅在会话启动时做动态导入的健康检查。
- **便携包携带知识层**：`scripts/build-portable-plugin.ps1` 随包分发 `shared/` 与 `knowledge/spt-kb/` 的 index + curated + wiki（约 5MB）；`archive/` 永不进包（forge 工具在便携版显式降级）；helper 产物存在则随包、缺失则打包警告；复制后断言 `shared/runtime-layout.mjs` 与 `knowledge/spt-kb/index.json` 存在。
- **文档数字锚点机检**：新增 `scripts/verify-doc-stats.ps1`（skills 数量 / index.json 条目数 / tools 子工程数：正则解析文档声称值 → 文件系统实测 → 断言相等；锚点缺失则跳过），接入 `tests/bootstrap/verify-all.ps1`。

### 会话接线契约（C4，2026-09-16）

- **tarkov-runtime-MCP 挂载**：插件 `config.mcp` 现声明三台本地服务器（mo2 / spt / tarkov）；tarkov 经此进入 OpenCode 会话工具面（此前唯一注册处是 OpenCode 不消费的陈旧 `.mcp.json`）。
- **幽灵物化契约退役**：插件从不物化任何文件（`hooks/` / `.claude-plugin/` / `.codex-plugin/` / `.agents/` / `.mcp.json` / `plugins/bgs-modding-superpowers/` 为多宿主时代残留，已备份至 `D:\Temp\opencode\bgs-leftover-backup-20260916.zip` 并清理）；`verify-layout.ps1` 恢复 absent 断言；`.gitignore` 幽灵规则删除。
- **契约文档**：`docs/internal/specs/session-wiring-contract.md`（单一权威：插件行为面 / config.mcp 清单 / 不物化声明 / 退役记录）。
- **便携包自包含修复**：修复 `Copy-McpRuntimeDependencies` 的 npm stdout 解码缺陷（CJK 路径 + 旧代码页 → node_modules 静默不入包）；便携树现携带三台 MCP 运行时依赖（7.24MB → 40.57MB），仓库外端到端冒烟三台均通过。
