# Dev Log

## 2026-09-13 — tarkov-runtime-MCP 首版建成并通过真实 server 冒烟

- 完成 `.scratch/tarkov-runtime-mcp/` 全部 7 张工单（01 穿甲弹 / 02 快照+profile / 03 sections 补全 / 04 日志解析 / 05 wait_for / 06 注册+冒烟 / 07 会话获取+信封修复）。
- 工具面：`tarkov_server_status` / `tarkov_instances` / `tarkov_snapshot`（profile/traders/quests/hideout/inventory）/ `tarkov_wait_for`；`raid.*` 占位待 Phase 2 BepInEx Client Bridge。
- 测试：142/142 绿；live 冒烟两轮（第一轮暴露会话缺失与信封解包两缺陷 → ticket 07 修复；第二轮全部通过，真实数据人工核对一致）。
- 关键经验：5.0 响应统一 `{err,errmsg,data}` 信封；`PHPSESSID` = profileId，经免会话的 `/launcher/v2/profiles` 获取；`/launcher/v2/mods` 是 mod 清单的最优路由源。
- 待办（follow-up）：hideout 等级改读 profile `Hideout.Areas`；traders standing/assort 改读 `TradersInfo`；MCP 进程需 export `TARKOV_RUNTIME_MCP_USERNAME`（可选 PASSWORD）才能用 session 受限工具。

## 2026-09-13（晚）— ticket 08：活跃 profile 跟随 + MO2 VFS 全链路验证

- 会话获取升级为规则链：探针跟随 -> username 精确 -> 单 profile 零配置自动 -> 多 profile 结构化歧义（AMBIGUOUS_PROFILE）。
- 新增可选 server mod `tools/tarkov-active-probe`（ADR-0004）：只读路由 `/spt/runtime/active-profiles` 暴露 ProfileActivityService 活跃窗口。
- 里程碑：**首次经 MO2 VFS 启动 SPT 5.0 server 并成功加载自定义 server mod**（假设 A-1 验证通过）；MCP 零配置（无 username）直连并返回真实数据。

## 2026-09-13（深夜）— MO2 安装能力完善：命名约定 + comments/notes 备注

- `mo2_install` / `mo2_create_mod` 新增 `comments`（必填：MO2 列表短摘要）+ `notes`（可选：安装记录）参数，写入 meta.ini（QSettings 兼容转义）；工具描述固化 `<category>-<mod-name>-<version>` 命名约定。
- `mo2_set_mod_notes` 修复转义缺陷：引号 / 反斜杠 / 换行按 QSettings 格式编码，多行备注不再破坏 meta.ini。
- 探针覆盖层改名 `TarkovActiveProbe` → `工具-MCP活跃探针-0.1.0`，并补 comments + notes（本实例修复）。
- 测试：mo2-mcp 510 passed / 0 failed（新增 `qsQuote` 单测 + `mo2_install` / `mo2_create_mod` 注释写入测试）。
- `tarkov_snapshot` 输出新增 `session`（source / username / profileId）可观测性——测试自动化可断言"这次读的是哪个 profile、经哪条路径"。
- **live 验证通过（玩家实际游玩中）**：探针路由返回活跃 profileId；零配置快照 `session.source=active-probe`，正确跟随 Samuel——"玩家实时用哪个 profile，MCP 就读哪个"全链路证实。tarkov-runtime-mcp 150/150 绿。
- 注意：本机 mo2 MCP 连接在 ticket 08 的 run_tool 调用后断开（实例修复经脚本完成）；`snapshot.session` 字段在下次会话重启 MCP 后生效。

## 2026-09-14 — KB 更新：Tushonka wiki 同步 + sp-mod.com 源码采集（近一月 4.1.5）

- **wiki 通道**：克隆 `SP-Tushonka/wiki`（@392e5005，2026-09-11，经代理）→ vendor 至 `knowledge/spt-kb/wiki-tushonka/`（78 文件，2.06MB）；新增 UPSTREAM.md；`sources/repositories.md` 登记源；index.json 新增 64 条（总 205）。
- **源码通道**：sp-mod.com API v0（`filter[spt_version]=4.1.5` & `filter[updated_between]=2026-08-14,2026-09-14`）→ 271 mod / 297 唯一仓库；批量浅克隆（`git clone --depth 1`，经代理 7890）至 `archive/forge/mods/`（gitignored，不污染仓库）。
  - 最终 **297/297 全就位**（含 17 条网页 URL 规范化补克隆）；392 个 `_source` 目录共 ~14.7GB；`MANIFEST-sp-mod-2026-09.md` 记录 provenance（mod 名 / id / URL / commit）。
  - 教训：API 的 `source_code_links` 混有网页 URL（`/tree/<branch>`、`/releases/tag/<v>`），批量克隆前必须规范化（tree/tag → `--branch`）。
- **旧 wiki 保留**：`wiki/`（sp-tarkov 官方快照）不整体替换——全文 120 处旧路径引用（skills/curated/docs）；两棵树并存，新知识优先查 `wiki-tushonka/`。
- **技能表述已更新**（Overseer 批准）：`using-spt-modding-superpowers` / `maintaining-spt-modding-environment` / `setting-up-spt-modding-environment` / `evaluating-spt-mods` / `interpreting-spt-mod-instructions` 五处 "Forge is offline" 全部改为 "Forge API v0 在线（sp-mod.com/api/v0，公共只读，~300 req/min，守 ToS）"，本地归档定位为稳定快照。
- **工具固化**：`scripts/spt-kb/`（fetch → clone → finalize MANIFEST 三步，含 URL 规范化与 dry-run）——供未来重复采集。

## 2026-09-14（晚）：Modding Standard 全流程闭环 + BGS 残留清理 + 二期检查器

- **Modding Standard 12 票全部完成**：grilling → spec → tickets → implement → 双轴评审 → 试点校准 → S3 自检，提交 8 笔（d71f27f6 … 2e69d920）
  - 规则集 84 条 / 13 维度（28 MUST），双源证据（机制 + 语料），evidence-index 21 锚点 + EV-CALIBRATION 校准记录
  - version-matrix.md：4.1.5 ↔ 5.0 双轨差异矩阵
  - 模板：server/client 升级（config 链 + LICENSE + 规则注释）+ 新增 paired-mod（Client/Server/Shared + 根级 Directory.Build.props 版本联动 + pack.ps1）；S1 构建通过
  - writing-spt-mod 技能改造：三模板绑定 + 60 Rule ID + 豁免流程
  - 试点校准：3 份合规报告，R1–R8 修订（5.0 客户端形态 IL2CPP、monorepo 作用域、CFG 边界等）
- **BGS 残留清理**：删除 plugins/bgs-modding-superpowers 物化树、空 hooks/ .claude-plugin/ .codex-plugin/ .agents/、被跟踪且已失效的 .mcp.json（引用 BGS 插件树）；verify-layout.ps1 重新校准（external/spt-archive 为 gitignored 本地语料，移出 absent 清单）
- **二期检查器**：scripts/check-mod-standard.ps1（约 25 条机检规则子集；monorepo 感知——根级 Directory.Build.props 版本/安装路径、共享 README/LICENSE；模板占位符 SKIP；豁免约定 Waiver: STD-XXX-NNN: reason）；接入 bootstrap 为 verify-standard-compliance.ps1，四套模板目标全 PASS，bootstrap 8/8 全绿
- **文档同步**：README（版本策略改双轨 + Modding Standard 专节 + 15 skills + 知识库表增行）、RELEASE-NOTES（v0.2.0-spt 新增小节）、spt-kb/INDEX.md（增行 + 日期更新）

## 2026-09-14（深夜续）：姿态大调整——抢救前提退役（ADR-0006）+ RELEASE-NOTES 中文化

- **背景**：SPT 5.0 已由 SP-Tushonka 社区 fork 正式发布；「SPT 可能停止运作」的创立前提作废（ Overseer 指示全仓方向调整）
- **ADR-0006**（`docs/adr/0006-posture-active-toolchain.md`）：抢救使命关闭（归档层转长期参考资产 + 离线兜底）；版本策略重述（4.1.5 稳定开发基线 + 5.0 已发布新主线双轨；3.11 历史对照）；ADR-0002 维持不变；Forge 策略动机改写（离线可复现 + 速率/ToS，非幸存副本）
- **全仓清扫**：@explorer 普查约 25 处 → 双 fixer lane 落地 25 项 + 主会话 20+ 项——README（使命/版本策略/资产表六项/特化 MO2 已建成）、VERSIONS.md、CONTEXT.md、kb README、5 个 skills、docs 6 份、KB curated 4 份；rescue 设计文档标 [HISTORICAL]
- **本地资产清单扩展为六项**：+3.11.4 源码、5.x 源码、特化 MO2 源码（`SamMeow-Tarkov-specific-Mod-Organizer`）与构建产物（`E:\build\spt-mo2\prefix\install\bin`）
- **RELEASE-NOTES.md 全文中文化**（Overseer 指示；标识符/路径/版本号保持原文）
- **教训（harness 运行时态）**：删除 plugins/ hooks/ .mcp.json 等根级 harness 文件后，OpenCode 插件在会话期重新物化——布局不变量从「磁盘不存在」改为「git 不跟踪」（新增 Assert-PathNotTracked）；.mcp.json 移出索引 + .gitignore 收编 6 项运行时路径；verify-mcp-surface 仅对受跟踪的 .mcp.json 强制内容
- **验证**：bootstrap 8/8（活跃会话下运行，运行时文件在盘但通过）；遗留：5.0 正式 tag 的 KB 技术复核列入后续工单；基线何时迁 5.x 属 Modding Standard 修订决策

## 2026-09-14（SPT5 更新）— BEM-20260914 同步 + 安装升级 + Phase 1 冒烟复验

- **源码同步**：`server-csharp` 新 tag `5.0.0-BEM-20260914`（`ec15a4083`，距 0910 共 21 commits：战斗通行证 / 任务系统与数据重生成 / 套装修复 / bot 数据全量重生成 / DI 重构；**未触及**版本端点、HTTP 监听、加密与 shuffle、Mod 加载器）；`modules`（客户端）0910 与 0914 为**同一 commit**（`b5513e6`）；EFT 兼容版本 `1.1.5.0.47242` **未变**。
- **安装升级**（Overseer 操作）：`SPT_5xx` 替换为 `SPT-BLEEDINGEDGEMODS-5.0.0-47242-ec15a40-20260914`；`SPT_Runtime\user`（1 profile）保留。
- **台账更新**：`tarkov-runtime-mcp` anchor 默认值 `5.0.0-BEM-20260910` → `0914`（config.ts / types.ts / version.ts 注释 + config.test.ts；150/150 绿 + typecheck 通过）；`.scratch/tarkov-runtime-inraid/spec.md` 增环境基线注记；KB `5xx-source-verification.md` tag 列表 + 再同步记录；CONTEXT.md（Bridge / In-Raid State）去「首版不实现」。
- **Phase 1 冒烟复验（live，直启 server）**：版本门禁通过（自报 `SPT 5.0.0 (BEM) ec15a4` × anchor `5.0.0-BEM-20260914`）；`tarkov_instances` 1 实例；`tarkov_snapshot` 全 5 section 真实数据（profile L3 / exp 5437 / 技能 37；商人 18；任务 62 条 30-26-5；藏身处 28 区；库存 485 件 / 259 模板；session=auto-single Samuel 零配置）；`tarkov_wait_for` 满足 + 超时两路径正常；`raid_status` 占位错误码正常。冒烟后 server 已停止。
- **已知项（非回归）**：商人 assort 计数、藏身处等级为既有 follow-up（读路径待改 `TradersInfo` / `Hideout.Areas`）。

## 2026-09-14（T03–T07）— 局内桥全字段 + 错误模型 + wait_for + 录制：三局 live 验收闭环

- **交付**：桥插件扩展（`/bridge/info` + 玩家全字段 + raid 元数据 + bot 域；`SPTInstallPath` 标准名 + LICENSE）；MCP 侧协议门禁 + 错误码拆分（`BRIDGE_UNREACHABLE`/`BRIDGE_VERSION_MISMATCH`/`NOT_IN_RAID`）+ `raid_status`/`raid_player`/`raid_bots` 真实化 + wait_for 快速失败 + 录制/回放；测试 **214/214**（25 文件）
- **live 验收（三局真实 raid，Overseer 配合）**：
  - 采样间隔配置实证（1000→250ms，`/bridge/info` + age 观测 ≤250ms）
  - 姿态 Stand↔Duck（`wait_for` `pose equals Duck` 11.2s/23 轮询满足）；移动比对 Δ≈64 单位；**击杀 1 scav → bot 计数 13→12 精确反映**；**受伤 → Chest 85→78.93 / total 440→433.93**
  - `raidId` 三轮 live 驱动修正：空 profileId → `MainPlayer.ProfileId`；`StartDateTime` 不可靠（同局翻转 `0001-11-23…`↔`no-start`）→ **桥自持会话起点墙钟**（两次读取完全一致，`<profileId>@<UTC ISO>`）
  - bot 分类修正：role 优先（`pmcUSEC`/`pmcBEAR` 的 side 为 `Savage`，side 优先会误计）
- **回归资产**：真实 raid 录制（profileId 匿名化）入 `tests/fixtures/live-raid/` + 回放测试 4 条
- **Modding Standard 机检**（`-TargetSptVersion 5.0.0`）：**PASS=11 FAIL=0 WAIVED=3**（豁免记录在桥目录）；补齐 LICENSE、`SPTInstallPath` 标准名（兼容 `GameDir`）
- **live 踩坑记录**：usvfs 把**新建**配置文件重定向到 MO2 `overwrite/BepInEx/config/`；覆盖层 DLL 在游戏运行中被锁定（部署需先退出游戏）
- 遗留（第二波）：事件流（击杀/受伤/撤离）、装备/武器状态、`getInfo` 缓存策略复核、boss 判定扩展（`sectantPriest` 等无 "boss" 字样）

## 2026-09-15（凌晨）— Phase 2 双轴评审 + 修复闭环

- **双轴评审**（@oracle ×2：Standards + Spec）对象 = Phase 2 未提交改动（13 tracked + 27 new）：
  - **Spec 轴**：1 实质缺口——桥侧零单测（spec 测试决策要求）→ 已补 xunit 测试工程 **85 用例**（纯逻辑抽取 `BridgeRouter`/`BridgePayloads`/`RaidIdBuilder`，主工程 0 error）；采样间隔范围收紧 **250–5000ms**（spec 0.25–5s）；`raid_status` 非 raid 路径补桥自报（US23）；README 过时行修正
  - **Standards 轴**：MUST（mod 级 `.gitignore`）已补；SHOULD（关闭路径空 catch 静默）已修（Debug 留痕 + 注释，STD-LOG-004）；smell 清理（`PLACEHOLDER_TOOLS` 移除、raid 工具编排上收 `fetchRaidSample`、`MainPlayer` 单次取值）；CLI-007 标准漂移（5.0 用 `Unload()` 而非 `Dispose()`）记录为**标准/检查器校准 follow-up**
  - 附带：MCP 测试 flake 根治（`listen(0)` 偶发命中 fetch forbidden port（实测 5061）→ 命中换端口重试）
- **验证**：桥 `0 error` + **85/85**；MCP **214/214**（连跑 3 次）；Modding Standard 机检 **PASS=11 FAIL=0 WAIVED=3**；新 DLL（31,232 bytes）已部署覆盖层（游戏关闭时）
- **Phase 2 状态**：T01–T08 全部核销（含三局 live 验收）；全部改动**未提交**（按 Overseer 规则）

## 2026-09-14（T02 首刀）— 局内桥 live 打通：MO2 客户端投送 / IL2CPP 读数 / HttpListener 三假设全成立

- **交付**：`tools/tarkov-runtime-bridge/`（BepInEx 6 IL2CPP 桥插件 0.1.0，net6.0，GUID `com.sammeow.tarkov-runtime-bridge`）+ `tarkov-runtime-mcp` MCP 侧 `BridgeConnection` 接缝与 `raid_player` 真实化（168/168 测试绿，基线 150 + 18）
- **MO2 交付链路**：覆盖层 `工具-tarkov-runtime-client-bridge-0.1.0`（实例 `Inescapable Tarkov` / Default）→ `BepInEx/plugins/TarkovRuntimeBridge.dll`；BepInEx 日志确认加载（`Loading [Tarkov Runtime Bridge 0.1.0]` + `listening on 127.0.0.1:49777`）
- **live 验收（真实游戏）**：菜单 `{"inRaid":false}` → raid `{"inRaid":true,"position":{...}}` → 移动比对 x 64.30→99.06 / z 157.47→172.60（Δ≈38 单位）；MCP `raid_player` 全链路 ok；采样循环 age ≤1s
- **三假设实测结论**：① usvfs 客户端投送成立（A-1 扩展）；② IL2CPP 成员读数成立（`MainPlayer.Position`）；③ HttpListener 在游戏进程内可用（127.0.0.1 免 URL ACL；另经受限令牌预测试）
- **架构落点**：ADR-0007（插件内嵌 HTTP + MCP 拉取）首刀验证；MCP 侧唯一新接缝 `BridgeConnection`
- 遗留：T03（采样/错误模型加固）、T08（Modding Standard 机检与文档收尾）

## 2026-09-15（晚）— tarkov-runtime 第二波：事件流 + 装备 + CLI-007 校准（live 验收进行中）

- **交付（未提交）**：桥事件子系统（`RaidEventBuffer` 环形缓冲 / `KillAttribution` 归属映射 / `RaidEventCollector` / `LocalGameStopPatch`）+ `/raid/events` + `/raid/player` weapon/equipment + `BotClassifier`；MCP `raid_events` 工具 + `raid_player` 扩展 + `getInfo` 去缓存（每次调用拉取）；CLI-007 校准（`05-client.md` / `check-mod-standard.ps1` / `version-matrix.md` / 模板注释 / 桥豁免移除）
- **测试**：桥 `dotnet test` **141/141**；MCP `npm test` **236/236** + typecheck + build（本次修复未触 MCP 侧）；机检 `-TargetSptVersion 5.0.0` **PASS=13 / FAIL=0 / WAIVED=1**（仅剩 CLI-006）
- **live 验收（第一局 Interchange，阵亡）**：
  - [OK] 撤离/停局事件：`LocalGame.Stop` postfix patch 实证生效（HarmonyX 对 IL2CPP 的 detour 成立）——阵亡落 `{type:"extraction", exitName:"", status:"Killed"}`，raidId 正确（`<profileId>@<会话起点>`）
  - [FAIL] damage/death 事件 0 条——根因：Il2CppInterop 的 `DelegateSupport.ConvertDelegate` 拒绝 `Action<EBodyPart, float, DamageInfo>`（`DamageInfo` 非 blittable struct，封送被拒），异常被订阅 try 吞掉并**连带跳过**同块的 `DiedEvent` 订阅（日志重复 100+ 行实锤）
  - [修复] 受伤改 Harmony patch `ActiveHealthController.ApplyDamage(EBodyPart, float, DamageInfo)`（prefix 记录归属 / postfix 输出事件；生态先例 Deminvincibility / Miyako-Carry-Service 等 6+ mod；方法 non-virtual、无子类覆盖）；`DiedEvent` 订阅独立 try/catch；Harmony 补丁改逐类独立应用（一类失败不影响另一类）；复跑构建 0 error + 141/141 + 机检 PASS=13；DLL（52,224 bytes）已部署覆盖层（旧版备份 `D:\Temp\opencode\TarkovRuntimeBridge-pre-fix-20260915.dll`）
  - [偏差] `equipment`/`weapon` 的 `name` 字段 live 实测返回本地化键形态（`<tpl> Name`）而非本地化值；`tpl` 为权威标识（与 SPT 本地化数据交叉一致）；本地化名解析列 backlog
  - [OK] 修复后复验（第二局 Sandbox，2026-09-15）：damage/death 事件 1900+ 条（seq 单调、raidId 一致）；本地玩家击杀 bot → `killer.isLocal=true`（seq 348 / 942，2 次）；本地受伤 `victimIsLocal=true`（seq 1796/1797）；增量语义实证（`since=676` → 仅回 677..686；缓冲淘汰后 `since=0` → `dropped=252`、从最旧 253 返回）；撤离 `{exitName:"Sniper_exit", status:"Survived"}`（seq 1936，赛后读取）——工单 05 全部验收项通过
- **其他 live 读数**：装备 12 槽读取正常；bots 22（pmc 6 / scav 16 / boss 0），无分类回归；`getInfo` 每调用拉取生效
- **收尾（2026-09-15）**：双轴评审（Standards 9 项 / Spec 10 项）+ 修复两 lane 完成；三笔提交 `f2d069b1`（桥）/ `3a6ab667`（MCP）/ `c4380039`（文档+标准台账）；评审修复版 DLL（52,736 bytes）已部署覆盖层（游戏退出时）；工单 01–06 全部核销。

## 2026-09-16（凌晨）— Accurate Circular Radar（Tyrian-Radar）4.1.3 → 5.0 移植：部署 + 六轮实机修复闭环

- **交付**：`mods/SPT5-AccurateCircularRadar/`（v1.3.4-spt5.1，移植作者 SamMeow / 原作者 Leonana69）；MO2 覆盖层 `界面-AccurateCircularRadar-1.3.4-spt5.1`（实例 `Inescapable Tarkov`）。**实机验证通过**：HUD / scav 绿 + boss 红 / 尸体 / 战利品（`tracked=58`，maxPrice 139k）/ PMC 黄橙 / F12 中文 / 无闪退。
- **流程**：@fixer 全量移植（21 文件 + 25 内嵌资源，0 error）→ 部署 → 实机多轮复验（6 轮修复闭环）；期间用桥 HTTP 端点读局内状态（raid/player/bots/events）+ BepInEx `ErrorLog.log` / Windows 事件日志做进程级崩溃取证。
- **实机修复（7 项，逐条见 mod README「实战修复记录」）**：① bundle 生命周期静态持有 ② 失败路径 `Destroy(this)`（上游 `Destroy(gameObject)` 会摧毁 GameWorld 对象 → 局内连锁崩坏）③ 补丁体异常护栏（KeyNotFound 闪退）④ `TrackableTransform` AV（virtual 属性 interop → 非虚替代；**AV 不可 try/catch**）⑤ F12 ComboBox 规避（被剥离方法刷屏 6.5k 条）⑥ prices.json 本地价格表直读 + `StringTemplateId` 取法（战利品命中的关键）⑦ SPT PMC `side=Savage` 颜色修正。
- **知识沉淀**：新技能 `porting-spt-mod-to-spt5`（六阶段 + API 映射表 + IL2CPP 模式 + 致命坑位清单）；KB 归档 `archive/ported-src/RadarStandalone_1100_spt5_port/`（+ 修复上游残缺克隆 `RadarStandalone_1100_source` 并补建 ported-src MANIFEST）。
- **新 spec**：`.scratch/tarkov-runtime-logwatch/spec.md`（错误/告警即时捕获：桥内 `ILogListener` 环形缓冲 + MCP 侧 5–10s 日志 tail + `ErrorLog.log` fatal 通道；待排期）。
- **未提交**（按 Overseer 规则）。

## 2026-09-16 — tarkov-runtime logwatch（错误/告警即时捕获）

- **交付（未提交）**：
  - **桥侧**（`tools/tarkov-runtime-bridge/`，工单 01/02；本会话未改动）：BepInEx `ILogListener` 捕获（回调只写内存、零 I/O、有界）+ 日志环形缓冲（默认 1000 条，`dropped` 计淘汰）+ 归一化去重聚合（500 组上限、`overflowDropped`）+ 端点 `GET /logs/recent`（游标增量）与 `GET /logs/summary`（聚合视图）；配置 `[LogWatch] Enabled`（默认 true）/ `MinLevel`（默认 `Warning`）/ `RingSize`（默认 1000）。
  - **MCP 侧**（`tools/tarkov-runtime-mcp/`，工单 03/04）：工具 `logs_recent`（`since` 独占 / `level` / `limit` 透传，输出 `{seq, dropped, entries}`）与 `logs_summary`（三通道合并视图 `{groups, overflowDropped, bridge, server, fatal}`）；服务器日志**字节游标** tail（`spt` / `kestrel` / `requests`，半行不推进、文件变短重置、新文件自动纳入、目录缺失静默降级，`source=server:<文件名>`）；fatal 通道监视 `BepInEx/ErrorLog.log`（`source=fatal`，不过滤级别）；惰性刷新（默认 5000ms，钳制 5000–10000）。
  - 新增 `LogWatchSource` 接缝（MCP 侧唯一新增抽象；桥侧接缝仍为 `BridgeConnection`，未新增 MCP-桥接缝）。
- **测试**：桥 `dotnet test` **295/295**（评审修复后复跑，0 error）；MCP `npx vitest run` **395/395**（36 文件，较上波 279 增 116）+ `npm run typecheck` + `npm run build` 通过。
- **评审修复（同日）**：① `logs_summary` 增 `server` / `fatal` 通道可用性元数据（`{available:true}` 或 `{available:false, reason}`，reason ∈ `logs_root_missing` / `no_log_dirs` / `path_unresolved` / `file_missing`），不可用通道的组恒为空——**静默降级不再不可见**；② `logs_recent` 的 `level` 改严格值域校验（只认 BepInEx 六级别名 `fatal`/`error`/`warning`/`message`/`info`/`debug`，大小写不敏感，归一为小写透传），未知取值（如 `Information`）返回 `INVALID_INPUT` 并列出合法值，消除「桥侧不识别 → 静默不过滤」footgun；③ 归一化两侧同构（24hex 右边界放宽，桥侧随本轮同步；改一侧须同步另一侧）；④ 桥侧：`/logs/recent` 级别过滤先于 limit 截窗（消除 starvation）、死成员清理、移除监听器失败改 `Warning`（STD-LOG-002）、`[LogWatch]` 配置键去冗余；⑤ 双 README 同步。
- **关键决策**：
  - 采集阈值默认 **Warning 及以上**（Info/Debug 不入缓冲）；查询侧 `level` 为叠加过滤，受采集阈值约束。
  - **fatal 通道在桥死亡场景仍可用（本通道存在的根本理由）**：进程级崩溃会杀死桥进程，故 `logs_summary` 遇桥故障时**不返回错误信封**，而是 ok 信封 + `bridge:{available:false, reason}`（`unreachable` / `version_mismatch` / `endpoint_missing`），服务器组与 fatal 组照常返回；`logs_recent` 保持严格门禁不变。
  - 归一化 **24hex 右边界放宽**（真实样本驱动）：SPT 打印 `Fixed item: <24hex>s undefined StackObjectsCount value, now set to 1`，id 紧贴字面量 `s`，`\b[0-9a-fA-F]{24}\b` 在该处不成立 → 同错误不同实例无法合并；两侧统一为「恰好 24 位连续 hex 段」（左边界非词字符、右边界其后不得再有 hex 字符），25/32 位 hex 串与内嵌片段仍不匹配（保守性保留）。
  - 服务器日志行内时间戳无时区标记 → 按本机本地时间解释后转 UTC ISO，使三通道 `ts` 同域可比（排序 / `since` 过滤一致）。
  - `since` 策略：桥侧原样透传（桥自行过滤）+ MCP 组同语义本地过滤（`lastTs > since` 独占）。
- **状态（2026-09-16 实机）**：live 验收**全部通过**——`/bridge/info` 新端点上线；告警可见性实测 age=1–4s（≤10s 要求）；字体刷屏组 count=5610 与日志文件逐字吻合；`since` 增量 `5625..5649`→`5650..5674` 无缝 0 重复；level 过滤、三通道合并、桥故障降级均实机验证；fatal 通道 fixture 文件级验证（真实 ErrorLog 空，无崩溃样本）。雷达复测见下段；全部改动**未提交**（按 Overseer 规则）。
- **参考**：`.scratch/tarkov-runtime-logwatch/`（spec + 工单 01–06）。

## 2026-09-16 — Accurate Circular Radar 复测（价格源隔离 + 商人价熔断）

- **复测通过**（Sandbox 局，MO2 覆盖层 `界面-AccurateCircularRadar-1.3.4-spt5.1`，v1.3.4-spt5.1）：战利品
  `Loot scan: owners=1067, tracked=69, maxPrice=139000, threshold=30000`（修复前 `tracked=0`）；`Local flea price table loaded: 4719 entries.`；商人价首次失败即熔断（Warning，本地价格表继续供价）；ragfair 回调 5.0 已知拒绝形态（1 条 Warning）。
- **F12 阈值即时性**：阈值 30000 → 55634 / 54718 / … / 52887 连续改动，`tracked` 69 → 17，每次改动触发 Rebuild。
- **稳定性**：无闪退；F12 无异常刷屏（唯一 ConfigurationManager 匹配为 Il2CppInterop Info 注册行）。
- **归档刷新**：`knowledge/spt-kb/archive/ported-src/RadarStandalone_1100_spt5_port/` 镜像 src/bundle/bin/Release（DLL 244,736 bytes / `76D8C6E3…`，与 MO2 覆盖层部署副本一致）；PROVENANCE 补复验行；mod README「实战修复记录」补第 11 项 + §6 复测证据。未提交。

## 2026-09-16 — 架构审查（improve-codebase-architecture）：10 候选 + C1 运行时布局契约 + C6 状态权威

- **架构审查**：4 路只读勘探（MCP 工具链 / 游戏侧 mod 与模板 / 技能与打包管线 / KB 与文档追踪）+ 主线活体核验 → 10 项深化候选 + P0-P3 路线图，HTML 报告 `D:\Temp\architecture-review-20260916-1337.html`（10 卡 / 6 图）。头号发现：**知识层静默死亡**——插件把 `SPT_KB_ROOT` 指到仓库上两级不存在路径 + spt-mcp 无条件信任 env 且吞错 → KB/Forge 工具静默空集（本会话实测 kb_query 0 条；服务器进程正常）。
- **C1 运行时布局契约（已交付，未提交）**：`shared/runtime-layout.mjs` 唯一解析点（插件与 spt-mcp 双端导入；env 显式无效即报错、不回退；逐资源 path/source/ok/reason）；spt-mcp 新增 `spt_health`（8 工具面）+ `kb_unavailable` 结构化降级（含 forge 文件级校验）+ IL 降级 warnings；插件不再注入路径 env（仅启动健康检查，动态导入防炸）；便携包携带知识层（index+curated+wiki ≈5MB，archive 永不进包）。**验收三项全过**：kb_query 0→50 条、伪造 env → 响亮报错、spt_health 完整报告。双轴审查：Standards 无硬违规（8 判断项 → 修复 4 / 记 backlog 4）；Spec 1 实质缺口（il-reader 静默空返回）已修 + 复核。测试：72 绿 + 5 条件跳过。
- **C6 状态权威（已交付，未提交）**：新建 `docs/README.md` 总索引（26 项状态标签）+ 权威分工表（dev-log=状态时间线 / .scratch=在办工作 / wayfinder=已决决策 / RELEASE-NOTES=发布摘要）；wayfinder MAP 与全局路线报告加 HISTORICAL 横幅；8 个 .scratch slug 进展状态行；新增 `scripts/verify-doc-stats.ps1` 数字锚点机检（skills 15 / index.json 221 实测断言；锚点缺失则跳过）接入 bootstrap（**9/9 全绿**）；README / RELEASE-NOTES / 使用指南 / VERSIONS 漂移修正（幽灵技能行移除、106→定性化、VERSIONS 环境段实测更新：4.1 线 `4.1.5-RELEASE+7d7add5`（2026-09-05 构建）/ 5.0 线 `5.0.0-BLEEDINGEDGEMODS+ec15a40`（2026-09-14 构建，BE 通道非正式 tag））。
- **已知遗留**：C1+C6 全部变更未提交（按 Overseer 规则）；`build-portable-plugin.ps1` 的 `Copy-McpRuntimeDependencies` 路径分隔符缺陷（便携树 node_modules 为空 → 物化 MCP 无法直启，建议单独立项）；本机 Forge API 快照缺失（5 条 forge-reader 断言条件跳过，按 `scripts/spt-kb` 刷新后自动恢复）；两个 .NET helper 未构建（`spt_health` 给出构建命令提示）。

## 2026-09-16 — C4 会话接线契约：tarkov 挂载 + 幽灵物化契约退役 + 便携包自包含

- **tarkov-runtime-MCP 挂载**：插件 `config.mcp` 2 台 → **3 台**（mo2 / spt / tarkov；timeout 360000 覆盖 `wait_for` 300s 上限；env 透传）。此前唯一注册处是 OpenCode 不消费的陈旧 `.mcp.json` → 会话内实际未挂载。
- **幽灵物化契约退役**：证实插件（135 行）零物化、OMO 源码无物化、残留 mtime 冻结 09-14 12:03（多次重启未再生）；备份（`D:\Temp\opencode\bgs-leftover-backup-20260916.zip`，27.4MB）后清理 6 路径（`plugins/bgs-modding-superpowers` 94.8MB、陈旧 `.mcp.json`、4 个空目录）；`verify-layout.ps1` 恢复 absent 断言（含红-绿证明）；`.gitignore` 幽灵规则删除；契约文档 `docs/internal/specs/session-wiring-contract.md`（单一权威）。
- **便携包自包含修复（既有缺陷，根因实锤）**：`Copy-McpRuntimeDependencies` 的 npm stdout 解码缺陷——Windows PowerShell 5.1 以 OEM 代码页（CP936）解码 UTF-8 输出，CJK 仓库路径下前缀匹配恒 0 → node_modules 静默不入包（三台全缺）。修复后便携树 7.24MB → **40.57MB**（三台运行时依赖 33.33MB），仓库外端到端冒烟三台 MCP `serverInfo` 均返回、无 `ERR_MODULE_NOT_FOUND`。
- **验证**：bootstrap **9/9**（含更新后的 `verify-mcp-surface` 3 台断言 / `verify-layout` absent / `verify-mcp-entrypoints`）；全部变更未提交。
- 待办（重启后复核）：OpenCode 重启后确认会话内 tarkov 工具面可见（配置在重启时加载）。

## 2026-09-16 — C3 mo2-mcp schema 归一化抽离（P0 批次收官）

- **纯搬移重构**：`normalizeMcpInputSchema` + `HoistedDiscriminants` + `extractDiscriminants` + `_flattenUnionBranches` + 全部文档注释从 `index.ts:180-456` 搬移至新建 `src/schema-normalizer.ts`（289 行逐行 byte-for-byte 一致 + 34 行模块头注释）；`index.ts` 456 → **167 行**（只留接线 + `schemaFor` 胶水），新增一行 import；测试 import 改指新模块，index 无 re-export。
- **验证**：`tsc --noEmit` exit 0；mo2-mcp 全量测试 **510 passed / 19 skipped**（含 normalize 专项 11/11）；build 通过；bootstrap **9/9**；`git diff --stat` 仅 2 文件（2 insertions / 291 deletions）。
- **P0 批次收官**：C1（运行时布局契约）+ C6（状态权威）+ C4（会话接线契约）+ C3（schema 归一化）全部交付；全部变更未提交。
- 备注：mo2-mcp 的 package.json 缺 `typecheck` script（spt-mcp 有）——小一致性项，可并入后续批次。

## 2026-09-16 — C2 MCP 共享内核（tools/mcp-kit）：三台收敛 + SDK 统一 + 便携接线（P1 首项）

- **共享内核抽取（已交付，未提交）**：新建 `tools/mcp-kit`（schema 管道 / envelope / result / stdio 引导 + `runMain`；24→25 测试）；spt/tarkov/mo2 三台迁移为薄适配器——接线为**相对 dist 导入**（`../../mcp-kit/dist/index.js`，零 npm 链接；沿用 `shared/runtime-layout.mjs` 先例），便携包以第四包接入现有 `Copy-McpPackage` 机制（含独立 node_modules vendor）。
- **grilling 四决策**：D1 相对 dist 导入；D2 SDK 收敛（mo2 `^0.6.0`→`^1.0.0`，实装 1.30.0，**零源码适配**——0.6↔1.0 所用 API 面逐字节相同经源码比对证实）；D3 canonical schema 管道（jsonSchema7 → 去 `$schema` → normalize）；D4 spt+tarkov 统一 canonical envelope（`{message, hint?, details?}`），mo2 错误方言本轮保留（另议）。
- **wire 审计（golden pre/post/portable 三态）**：spt/tarkov 逐字节零 diff；mo2 仅 2 处转换（`mo2_install.target_priority` 的 `enum:["top"|"bottom"]`→`const`，openApi3→jsonSchema7 编码差异，已声明）；无未声明输出变化；spt 唯一字段级 delta = err 信封 `summary`→`message`。
- **验证**：kit 25 / spt 73（+5 条件跳过）/ tarkov 395 / mo2 499（normalize 11 项搬 kit）全绿；bootstrap **9/9**；便携重建 + 三台 stdio 冒烟（portable2 vs portable 三台 SHA256 全等）。
- **双轴评审 + 修复轮**：oracle ×2 无 BLOCKER（Spec 轴判「可提交」）；修复轮落地 7 项——S1 引导钩子时序断言（fixture 标志锁 + mo2 smoke await ready）、S2 便携悬空脚本（剥离列表补 pretest/lint；顺带修复 `Strip-PortableMcpPackageJson` 的 StrictMode 剥空缺陷——mo2 scripts 剥至空即抛错）、N1 三台死依赖 `zod-to-json-schema` 清理、NIT-2 spt 级 err 字段断言、kit 导出面快照测试等。
- **文档**：README / CONTRIBUTING / INSTALL 修正（三台计数、kit 先构建顺序、dist 非跟踪事实、技能数 14→15）；`session-wiring-contract.md` 补共享内核先构建。
- **记录项（不修）**：N5（kit 冻结维持 ToolResult workaround）；N7（dist 陈旧性护栏，已知限制）；N8（`using-spt-translator` 幽灵路由——C6 尾账）；SDK 1.x 传递依赖瘦身（后续批次）。
- 全部变更未提交；**重启后复核**：三台 MCP 加载 + mo2 工具 schema 接受。

## 2026-09-16 — C7 KB 索引接口归一 + 管线闭环（P1 第二项）

- **契约模块（已交付，未提交）**：`tools/spt-mcp/src/kb/`（`contract.ts`：类型 + `KB_SCHEMA_VERSION=2` + `validateIndex` 严格 + `parseIndexForQuery` 结构校验 + version 防御归一 + `collectStats`；`query.ts` 纯函数查询；`index.ts` 导出面）；`kb-query.ts` 改薄；`types.ts` 死 facade 移除。
- **现役 bug 修复（活体实测）**：`spt_kb_query({version:"4.1"})` 原为 `internal_error: entry.version.map is not a function`（2 条 string 型 version 条目让整条过滤路径崩溃）→ 修复后 **169 命中**（= 71×4.1 + 98×通用，语义正确）。
- **数据迁移 v1→v2**：2 条 version string→array、3 条 source 补齐（2×curated + 1×archive）、schema_version→2；文件 diff **+13/−6**，条目集合与文件键序未动；备份于 `.scratch/c7-kb-index/goldens/index-pre-migration.json`（与 HEAD blob LF 归一后逐字节相等）。
- **管线闭环**：`scripts/spt-kb/validate-index.mjs`（薄包装调 dist，exit 0/1/2）+ `sync-index.mjs`（upsert 生成器 + drift 报告：已有条目绝不改写、孤儿仅报告不删、`--write` 幂等、非法数据 exit 1、坏 JSON/entries 非数组 exit 2）；bootstrap 第 10 项 `verify-kb-index`（**9→10 全绿**）。
- **验证**：spt **98**（+26：23 契约 + 3 查询）/ validate exit 0（221 条 stats 与基线逐项一致）/ sync 幂等（0 新增 · 4 archive 孤儿 · 0 非法）/ 对抗性 **24/24** / bootstrap **10/10** / 便携 validate exit 0；kb_query golden pre/post = version 修复 + 已声明数据迁移（值级 5 处）+ 输出键序归一（canonical 重建，31/32 form-B 条目重排）。
- **双轴评审 + 修复轮**：oracle ×2 无 BLOCKER；修复轮落地 11 项（S1 `--write` 退出码统一 / F1 非法条目报告可定位 / 防御加固 / 统计派生修正 / CLI 规范 / shebang+parseArgs 对齐 / 死代码清理等）。
- **记录项（不修）**：sync `--write` 入库测试化（后续工单）；dry-run 在非法数据时 exit 1（语义统一，已披露）；便携 helper 预构建告警（既有）。
- 全部变更未提交；**重启后复核**：`spt_kb_query({version:...})` live 返回（当前会话 spt 进程为旧 dist）。

## 2026-09-16 — C8 Modding Standard 规则机读化（P1 第三项）

- **机读注册表（已交付，未提交）**：`knowledge/spt-kb/curated/modding-standard/rules.json`——全量 **84 条**（13 domain）元数据 + 可检子集 **29 条**检查规格（18 handler = 11 声明式 + 7 命名；规则常量全量入册，handler 内零硬编码）；顺序=旧检查器输出顺序（golden 保真）。
- **检查器 registry 驱动重构**：`scripts/check-mod-standard.ps1`（300→456 行）读注册表；CLI/输出/退出码/waiver/占位符/monorepo 语义逐字保持——**5/6 目标 golden 字节级一致**（模板×4 + bridge 5.0.0），radar 仅 3 行已声明 delta。
- **META-005 假阳性修复（活体案例）**：`mods/SPT5-AccurateCircularRadar` 的 `<Version>1.3.4-spt5.1</Version>` 原 `[FAIL]`（检查器正则比散文严格）→ 放宽为「三段核心 + 可选 semver 预发布/构建后缀」+ prose 一行措辞同步；radar 现 12/0/0 WAIVED=2 exit 0；夹具双向锁定（prerelease PASS / 四段式 FAIL）。
- **validator（新）**：`scripts/validate-mod-standard.ps1`——registry↔prose 双向校验（ID 集合/标题/Level/Applies/域/形状/发射登记）+ MUST 双源标记 28/28 + 重复 ID/块级解析/发射双护栏/handler 必需键映射；S3 不变量常驻化。
- **夹具回归（新）**：`tests/mod-standard/`（7 夹具 + runner，含负向对照自证）；`verify-standard-compliance.ps1` 三段接线（validator + 4 模板 + 夹具）；bootstrap 保持 **10/10**。
- **评审 + 修复轮**：oracle 双轴无 BLOCKER；修复轮 10 项落地，新检查 **7/7 负向对照命中**（真会报错）。
- **登记台账（4 条，维持登记）**：① `STD-VER-001` 检查器专用发射 ID（prose 无对应；白名单 + README + dev-log 三处显式登记，validator 防新增）；② `STD-STRUCT-002` prose=禁 bin/obj vs 检查器=禁 TS/JS；③ `STD-VER-002` prose=复用 4.1 骨架 vs 检查器=版本同源；④ `LOG-001` 部分覆盖。均落 `rules.json` note 字段，待后续 prose 侧决策。
- **环境事件**：BGS 幽灵物化在重启窗口（22:24-22:32）复活（6 路径：`.mcp.json` + `plugins/` + `hooks`/`.claude-plugin`/`.codex-plugin`/`.agents` 空目录；在场期间树 24→78 文件增长）→ 按用户裁决清理（备份 `D:\Temp\opencode\bgs-reappearance-20260916`，79 文件）+ `verify-layout` 恢复绿；**若下次重启再现需深挖来源**（OMO / Claude 插件物化 / 云同步）。
- **校准**：双重包裹修复实际为 27× `return (New-Result ...)` + 2× `return $results`（29 = 可检规则数，原报告口径误差记录在案）。
- **记录项（不修）**：N4 夹具负分支扩展（后续）；N10 scratch 捕获器引号启发式；便携树不自带夹具（设计边界）。
- **提交口径**：`tests/bootstrap/verify-all.ps1` 混含 C7（verify-kb-index 第 10 项）与 C8（UTF-8 行）——C7/C8 建议同批提交，或剔除该行后修正 C8 验证记录。
- 全部变更未提交。
