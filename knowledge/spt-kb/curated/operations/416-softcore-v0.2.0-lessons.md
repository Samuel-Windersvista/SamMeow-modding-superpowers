---
version: [4.1]
domain: both
topic: operations
title: "SPT 4.1.6 服务端 mod 经验：配置外置化 · PostLoad+1 阶段实证 · 数据全量对齐方法论 — Inescapable Tarkov's Softcore v0.2.0"
keywords: ["外置数据表", "外部优先", "缺失重建", "损坏回落", "PostLoad+1", "阶段窗口", "内容注入", "A/B/C 对齐", "handbook 节点", "Grids[0]", "多网格背包", "狗牌", "克隆源", "JSONC", "条目级容错", "告警队列", "冷启动竞态"]
summary: "Inescapable Tarkov's Softcore v0.2.0（4.1.6 线）三段经验：其一配置外置化——12 张数据表随包分发 config/ 子树，外部优先/缺失重建/损坏回落不覆写/JSONC/条目级容错，静态加载器以 assembly.Location 解析目录、告警经静态队列由编排器注入 logger 输出；其二阶段窗口实证——内容 mod 注入物品须 PostLoad+n 才可见，迁窗后覆盖面显著扩大（backpacks 34→56 且告警清零）；其三数据全量对齐方法论——A 删/B 后移/C 补档，引用核对三源（production/quests/handbook），多网格背包 Grids[0] 上限、狗牌禁堆叠、父类真值与克隆源优先级；附多 lane 协作与独立复验经验。"
---
# SPT 4.1.6 服务端 mod 经验：配置外置化 / PostLoad+1 阶段实证 / 数据全量对齐（软核 v0.2.0）

> 适用：[4.1]（4.1.6 实证）｜来源：Inescapable Tarkov's Softcore v0.2.0（2026-10-16/17）——mod 仓库 `SamMeow-modding-superpowers/mods/InescapableTarkovsSoftcore416`，证据 `.scratch/inescapable-softcore-416/`（spec + 工单 08–17 + align-report）与 `docs/port-notes.md` §7。
> 状态：双轴审查（Spec/Standards）+ 235 单测 + 部署日志验收（告警 0 / 错误 0 / 跳过 0）。
> 关联：`curated/modding-standard/`（STD-LOG-001、06-config）、`curated/api-notes-4.1/mod-loading.md`、`curated/operations/5xx-server-mod-dev-lessons.md`（日志接缝 / overwrite 树）、`curated/migration/pilot-experience-softcore-416-backport.md`（v0.1.0 移植面）。

## 1. 运维事实（本轮再实证）

- **MO2 启动的服务器 → 日志写实例 `overwrite/`**：日志等写权重定向到 `＜MO2 实例＞\overwrite\SPT_Runtime\user\logs\spt\spt<yyyyMMdd(UTC)>.log`（会话跨 CST 零点时文件名用前一 UTC 日）。SPT 安装目录内的同名日志可能是旁路直启的陈旧残留——**排查先认实例 overwrite**（5.0 线同规则，见 `5xx-server-mod-dev-lessons.md`「日志运维」；本轮 4.1.6 再次实证其通用性）。
- **overlay 更新落位链**：关闭 SPT.Server/Launcher（DLL 文件锁）→ profile 备份（标签形如 `Default_pre-<mod>-v<版本>`）→ 拷贝构建产物 → **逐文件 SHA256 与产物核对**（本次 14/14 一致）→ 用户重启 → 日志验收。回退 = 撤销 overlay + 恢复 profile 备份。

## 2. 阶段窗口：`PostLoad + 1`（内容注入可见性）

- **事实**：包内内容 mod（WTT-ContentBackport/Armory/Artem、Epic AIO、TGC-NG、Russian-SF-Pack 等）在 **Preload+n** 注入 `db/CustomItems`；`Preload+1` 处理的服务端 mod 看不到这些物品（v0.1.0 日志：backpacks 9 条 / trueItems 23 条「未找到」告警，含 B 类 8 背包 + Vinyl）。
- 迁至 `PostLoad+1` 后同数据全部命中，且**覆盖面连带扩大**（同一 mod、同表，仅阶段变化）：

| 模块 | Preload+1（v0.1.0） | PostLoad+1（v0.2.0） |
|---|---|---|
| backpacks | 34 变更 + 9 告警 | **56 变更 + 0 告警** |
| trueItems | 250 + 23 告警 | **328 + 0** |
| samuelTweaks（域计数） | 126 | **247**（护甲 67→162、弹匣 10→21；臂章/近战父类标签纠正后 37/27） |
| softcore | 9900 | **16905** |

- **推论**：凡需覆盖「内容 mod 注入物品」的服务端数据编辑，窗口取 `PostLoad + n`（+1 即晚于全部 mod 装载回调）。排序 = TypePriority 升序 + ModGuid 字母序（同相位按 GUID）。

## 3. 配置外置化模式（12 表 config/ 子树）

- **语义（与 config.json 同构）**：外部文件优先 → 缺失 → 从内嵌模板重建（一行告警）→ 语法/结构非法 → 告警 + 回落内嵌，**绝不覆写用户文件**；JSONC（注释 + 尾随逗号）；**条目级容错**（数组逐元素、字符串键字典逐键；坏条目警告跳过，不整体失败）。
- **实现要点**：唯一共享缝（`ExternalJsonFile`）；静态加载器无 DI 上下文 → 模块目录解析用 `assembly.Location` 兜底（生产环境即 DLL 所在 mod 目录，等价 ModHelper 分支）；加载告警进**静态队列**，由编排器（持有注入 `ISptLogger<T>`）在运行末尾统一 Warning 输出——满足 STD-LOG-001 的静态折中。
- **缓存分层**：静态入口带进程级缓存；另备「显式目录重载」供测试隔离——文件语义测试不触静态缓存。
- **全局静态态 = 测试竞态源**：告警队列跨测试共享；xUnit 并行下冷启动「缺失重建」并发入队会打乱队列断言（热启动侥幸全绿、冷启动必挂的教科书案例）→ 测试程序集级 `[assembly: CollectionBehavior(DisableTestParallelization = true)]` + 队列用例「先清场再断言」。
- **分发**：build.ps1 幂等拷贝 `data/` → overlay `config/`，内置 12 项存在断言；产物 = DLL + config.json + `config/` 全树。

## 4. 数据全量对齐方法论（A/B/C）

- **A 删**：全包（基础库 ∪ 内容 mod CustomItems）不存在的引用（如旧包自定义 stim 占位 22 条）；**B 后移生效**：由内容注入、迁窗即可达者不删；**C 补档**：按既有风格，值来源优先级 **克隆源 > 旧包原版直系 > 族内中位**；无同族先例或候选超量（>60/表）→ 停手上报待裁，不硬塞。
- **引用核对三源**：配方 `hideout/production.json`（`_id`）、任务 `templates/quests.json`（`_id`；注意 `Select-Object -First N` 截断可致假阴性）、手册 `templates/handbook.json` 节点（`fleaListingsWhitelistHandBook` 的匹配对象是 `handbookItem.ParentId`，非物品 id）；配方运行时自建 id（如 ScavCaseChanger.ToRecipes）属「内部 id」非死引用。
- **多网格背包**：模块只改 `Grids[0]`；旧包原版对多网格包标注 `WARNING MAKING THIS ANY BIGGER WILL CAUSE GRID OVERLAPS`——小值是界面重叠**物理上限**，不得按风格域上浮。
- **狗牌类（dogtag 含变体）为唯一性击杀数据物品**：原表零先例、堆叠无语义收益 → 不纳入堆叠补档。
- **父类真值**：`5b3f15d486f77432d0509248` = ArmBand、`5447e1d04bdc2dff2f8b4567` = Melee（来源 mod 曾标反；DB 子项 `armband_*` / `weapon_*` 双重证实；单开关语义依赖此真值）。
- **验收 = 服务器日志唯一判据**：同类告警（未找到/未命中/跳过）= 0；v0.2.0 实测合计 17850 变更 / 警告 0 / 错误 0 / 跳过 0；外置表 0 重建、0 回落消息。

## 5. 多 lane 并行协作与独立复验（过程经验）

- **并行三分隔离**：文件所有权互斥清单（每 lane 显式「仅可改」列表）；构建产物隔离（`dotnet test --artifacts-path .artifacts/its-<lane>`）；全局静态态测试串行化。
- **独立复验捕获自验证漏网**（两例）：① 冷启动测试竞态——lane 自测两轮全绿仍漏，编排者冷启动复跑立现；② 预研 grep 截断误判——`63966fcc…` 实为 `quests.json` 现存任务「Snatch」，因截断显示被误判为死引用，被复核层纠正。
- **审查闭环**：Spec（规格覆盖）/ Standards（标准 + 气味）双轴独立执行 → 收口修复 → 复验收口；复核保留项（解析器 null 漏网、JsonNode 大小写不对称）在复核层给出精确修法后由裁决层落地。
- **并发真相判定**：平台任务追踪信号可能与实际状态不符（「已停止」误报但写入仍在进行）；以 **文件 mtime/hash 采样** 判定写者存活；写入互斥保护（检测到并发写者即拒写并上报）避免 last-writer-wins 撕裂。
