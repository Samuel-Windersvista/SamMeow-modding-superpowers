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

## 2026-09-17 — C5 发版身份契约归一（P1 末项）

- **版本单一源（已交付，未提交）**：`scripts/version/sync-version.mjs`（check / `--write` / `--set`；kind = json / npm-lock / toml / xml / code；exit 0/1/2；BOM/行尾字节保真）+ `.version-bump.json` 扩展为注册表（source + 18 目标）；插件组件 0.1.0（含 -dev0）→ 0.2.0 一次性传播；`bump-version.sh` 退役（jq 依赖消失）；CONTRIBUTING 版本节改写。
- **bootstrap 第 11 项（新）**：`verify-version-identity.ps1`（① sync check；② 身份禁入扫描：操作性表面 5 处 × 三模式 `git grep -in -E`）；正/负对照实测（植入违规被捕获 / 干净绿）。
- **身份归一**：`start-mo2.ps1` ×3、`install-mo2-control-plane.ps1` ×2、MO2 资源审计器改名（`Mo2AssetsInspectorPlugin` / `NAME = "Mo2AssetsInspector"` / "MO2 资源审计器"）、`xedit-client.launch.ps1` 注释指针；根 `package.json` 元数据 → origin（repository/homepage/bugs）；LICENSE ×2 追加 fork 行（上游行保留）。
- **文档规范化（D5 口径，含历史档案）**：INSTALL `<owner>` ×3、RELEASE-NOTES 标题 `v0.2.0`、全局路线报告 :13/:51/:52/:105（现状陈述归一；日期口径 09-14 首清 / 09-16 复清）、05-31 计划 4 行命令示例指针、05-13/04-23 旧路径；事件记录/署名保留——逐条分类表落盘 `.scratch/c5-release-identity/identity-classification.md`（62 命中：4 已归一 / 54 保留 / 8 超范围）。
- **双轴评审 + 修复轮**：oracle ×2 无 BLOCKER；修复轮 6 项落地（S1a/S1b/S3/S4/S5/N4p）；S3 根因 = UTF-8 无 BOM 在 PS 5.1 下吞换行（守卫失效）→ BOM 修复；S5 = 便携树按树内实际目标裁剪注册表（18→10）后自校验 exit 0；编码卫生对齐 3 个 C5 触及文件 BOM。
- **验证**：bootstrap **10→11 全绿**；sync 18/18 = 0.2.0（幂等 + `--set` 往返 + 退出码路径实测）；工具套件 kit 25 / spt 98 / tarkov 395 / mo2 499 全绿；便携重建（14440 文件）+ 树内 sync + 树内 validate ×2 全 exit 0；py_compile OK（pytest 环境缺失，声明跳过）。
- **事故记录**：`fix-2` 会话在修复轮两度调度后进入 `error (unconfirmed) / no live status entry`（会话级故障，工作区零落盘）→ 改道全新会话 `fix-3` 幂等推进完成。
- **记录项（不修）**：N1 死 catch / N2 `audit.exclude` 消费者 / N3 semver 前缀匹配 / N4 快照文补注 / N5 扫描盲区（设计内）/ N6 `mo2-install.ts` 示例串 / F4 计数硬化 / F5 MO2 控制面版本口径（S2 豁免：MO2 插件版本体系独立，冻结 0.1.0）；`templates/paired-mod/scripts/pack.ps1` 与 `verify-mcp-entrypoints.ps1` 编码观察项；tools 测试旧沙箱夹具路径（超范围）。
- 规格：`.scratch/c5-release-identity/spec.md`（CLOSED）。
- **提交口径**：C5 单批提交（建议 `feat(release-identity): single-source versioning + identity gate + docs normalization (C5)`）。
- 全部变更未提交。

## 2026-09-17 — C10 Bridge 跨语言契约工件化（P2 首项）

- **契约工件（已交付，未提交）**：`shared/bridge-contract/contract.json`（protocolVersion + 日志归一化规则表 + 聚合常量 + 空白字符类）；C# 经 EmbeddedResource 编译期嵌入（`src/BridgeContract.cs`），TS 运行时读取（`src/bridge/contract.ts`，仓库/便携树同相对路径）；两端三处消费点全部契约派生（无残留字面量）。
- **共享 golden 夹具**：`fixtures/`（归一化 55 例含对抗向量 / 聚合 12 例 / payload 10 份 + errors）；两端测试读同一批文件；pre/post 捕获 5/5 + CS↔TS 2/2 逐字节 MATCH（捕获语料冻结）。
- **ADR-0009**：单一源范围 / 双轴版本语义（组件 0.2.0 ≠ wire 1；仅不兼容变更 +1）/ 夹具机制 / 非目标（payload codegen、协议 v2、传输改动）。
- **bootstrap 第 12 项**：`verify-bridge-contract.ps1`（工件与夹具可解析 + 消费点断言：EmbeddedResource LogicalName / TS 契约模块 / `Plugin.cs` 派生 / 便携清单）。
- **双轴评审 + 修复轮**：oracle ×2 判 1 BLOCKER（`\s` ECMAScript 反向对齐——ECMAScript 下 .NET `\s` 仅 ASCII；对抗向量分叉 5/18→8/18）+ 6 SUGGESTION + 3 NIT；修复轮 F1-F6 全落地（空白字符类升为契约数据 + trim 收敛 + 对齐向量 + 校验对齐 + #12 加固）。
- **验证**：bootstrap **11→12 全绿**；C# **316**/0（基线 295）；TS **414**/0（基线 395）；post 捕获零漂移；便携重建 + 树内对拍（nbsp→space / nel preserved / bom trimmed）；Node `\s` 全码点枚举 25 个 SET-EQUAL。
- **已声明 delta**：C# 空白/词类向 JS 对齐（U+FEFF/U+0085 边缘 trim、é/ß0x/阿拉伯数字边界）；U+0130 残余记录（ADR-0009）。
- **环境事件**：BGS 幽灵物化**二次复发**（2026-09-17 01:06:07，`.mcp.json` + `plugins/`；与未注册 `ticket-07` 工作树逐字节同源）→ 备份（`D:\Temp\opencode\bgs-reappearance-20260917`）+ 清理 + verify-layout 恢复绿；来源深挖调查进行中（配置侧物化者 / `plugins/` 幽灵来源）。
- 规格：`.scratch/c10-bridge-contract/spec.md`（CLOSED）。
- **提交口径**：C10 单批提交（建议 `feat(bridge-contract): single-source cross-language contract + shared golden fixtures (C10)`）。
- 全部变更未提交。

## 2026-09-17 — 环境事件：BGS 幽灵物化根因定位（WPS 云同步回写）

- **根因（决定性证据）**：`E:\云文件\` 为 WPS Cloud 活跃同步文件夹（注册表 `HKCU:\Software\Kingsoft\wpscloud\usercenter\qingsyncfolderconfig\wpsoffice\plugins\qing\205625147\syncfolderinfodrive\E650A46D2396424CBFD12D844D49F213`：`originPath=E:\云文件`、`status=1`）；仓库位于同步范围内。本地清理 BGS 残余后，云端旧副本（`.mcp.json` + `plugins/bgs-modding-superpowers/`）被同步引擎回写本地 → 幽灵复发（09-16 22:24 / 09-17 01:06 / 深挖期间共三次）。**非** OpenCode / OMO / Claude 插件物化（已逐一排除）。
- **同源链**：复发 `.mcp.json` SHA256 `034AE7B4…` 与 `.slim/worktrees/ticket-07/.mcp.json` 及旧备份 zip 逐字节一致。
- **处置**：本轮再次清理（verify-layout 恢复绿）；**根治在云端侧**——(a) 仓库移出 WPS 同步范围（推荐；兼防 .git 被云同步扰动的数据完整性风险）或 (b) WPS Cloud 网页端删除 BGS 残余云端副本后再本地清理。**未根治前预期周期性复发**。
- **附带发现**：`C:\Users\Winde\.config\opencode` 亦在 WPS 同步范围（旧配置同样可能被云端恢复）。
- **历史澄清**：`47144a12`（Sep 14）曾将现象归因"OpenCode 插件每次会话启动物化"（不变量放宽为"不得入库"）；C4（Sep 16）恢复"必须缺席"不变量——根因现更正为外部云同步服务行为。
- **后续（同日）**：用户已自行处置 WPS 云同步问题；编排者复核——无幽灵、verify-layout 绿。观察期：若复发（WPS 注册表中 `E:\云文件` 仍为同步条目）则按上述选项再处置并报告。

## 2026-09-17 — C9 游戏侧路径可移植化 + pilots 退出机制（P3 安全子集）

- **路径可移植化（已交付，未提交）**：`mods/Directory.Build.props` + `tools/migration-pilots/Directory.Build.props` 集中 `SptRoot`（`-p:`/env 可覆盖 + 缺失响亮失败）；11 个 csproj 改造 + 修复轮延伸（3 tools csproj 内联 + `tarkov-active-probe` csproj + `tarkov-runtime-bridge` props）；`SPT_410`（不存在）→ 实际 `SPT_41x`；413 CHANGELOG/STATUS/PROGRESS 部署陈述纠偏（"已部署"→"待部署验证"；实测 `SPT_41x\BepInEx\plugins` 为空）。
- **pilots 退出机制**：`tools/migration-pilots/{ett,secure-mapbook,skills-extended}/STATUS.md`（状态/owner/验证状态/关闭条件/归档去向 `examples/`）；skills-extended 服务端 csproj glob 修复（client-src 排除；**534 错→0 错**，1 既有 CS9113 警告）。
- **release DLL 可复现验证**：`scripts/verify-mod-release-dlls.ps1`（重建 + hash 对照 + 防污染断言）；实测 **DRIFT×2 = 元数据级**（311 `47E50BE3…` / 413 `F02BB894…`；签名集 diff=0、同尺寸、~0.6% 字节差、确定性重建）→ **裁决不替换**（签名级工具化 = 后续项）。
- **双轴评审 + 修复轮**：oracle ×2 无 BLOCKER（F1-F6 / R1-R6）；修复轮全落地（体积转录、glob 修复、措辞、同类路径延伸）；CRLF 事故（12 文件误伤）经双轴复核闭环（XML 15/15、首字符完好、4 文件 LF 纯告警）。
- **A5 分类依据（R2）**：`SPT_410` 29 命中全部为 Markdown——历史文档（wayfinder/分析报告/计划）与 KB curated 运行参考（game-data-ref×4 / migration×3 / sources×1）；`.scratch/c6-state-authority/spec.md:30` 已含"路径更正（不再写 SPT_410）"工单。
- **验证**：bootstrap **12/12**；全部受影响工程构建 exit 0；覆盖链实测（`-p:SptRoot`/env）；release DLL 与 HEAD 逐字节一致（防污染成立）。
- **记录项（不修）**：默认根定义点 6 处（漂移风险；后续可评估合并）；签名级 DLL 比较工具化；共享核心抽取（D5 延后，触发 = 实测期结束 + 需求）。
- **提交口径**：C9 单批提交（建议 `chore(game-assets): portable SptRoot paths + pilot exit markers + release DLL verification (C9)`）；**注**：`docs/dev-log.md` 混含 WPS 环境条目（C9 前预存）与 C9 条目——可拆可合。
- 全部变更未提交。

## 2026-09-27 — SPT 5.0 mod 模板三件套（spt5-*）落地 + 联动修订

- **前置研究（已交付，未提交）**：`docs/research/spt-5.0-mod-template-design.md`——内部素材侦察 + 外部一手资料 + 本地运行时实测裁定；含命名空间裁定（服务端命名空间 `SPTarkov.*`、包名/目录名 `SPTushonka.*`；客户端 `SPTushonka.*`）、发布状态修正（BEM 滚动快照通道）与 KB 勘误清单；`docs/README.md` 已登记索引。
- **交付（已落地，未提交）**：
  - `templates/spt5-client-mod/`（7 文件）：`net6.0` / BepInEx 6 IL2CPP / `BasePlugin` + `Load`/`Unload` / `Log` / `BepInEx/core+interop` 引用 / `CheckGameReferences` 构建护栏 / 逐类补丁隔离。
  - `templates/spt5-server-mod/`（12 文件）：`net10.0` / `SPTarkov.*` / `IModMetadata` `SptVersion ~5.0.0` / 无 `package.json` / `IOnDIConstruct` 配置注册 / `StaticRouter` 示例路由（`/spt/…` 独立前缀）/ `CheckSpt5RuntimeReferences` 护栏。
  - `templates/spt5-paired-mod/`（21 文件）：单 sln 双端 + `Directory.Build.props` 版本联动（`ModVersion.Value` 编译期常量）+ `SharedConstants.ExampleRoute` 真接线 + `pack.ps1` 单 zip。
- **联动修订**：`tests/bootstrap/verify-templates.ps1`（+3 模板清单）；`skills/writing-spt-mod/SKILL.md` 5.0 版本线分支（frontmatter description / Step 2 检索过滤 / Step 3 模板分流 / Step 4 日志口径 / Step 5 构建属性全面条件化）；KB 勘误（`version-matrix.md:48/:57/:79`、`06-config.md:106`、`07-logging.md:40`，并补 STD-LOG-003 5.0 分支与模板落位说明）；4.1 三模板 README 交叉指引。
- **验证（P3 终验 + 修复后复验，全部主会话直跑）**：`verify-templates.ps1` exit 0；paired sln 构建 0 警告/0 错误 + `pack.ps1` 实跑产 `SmokePaired-1.2.3.zip`（stage 与 zip 条目核对通过）；client/server 全新填充构建 0/0、无运行时 DLL 泄漏、占位符审计干净；`check-mod-standard -TargetSptVersion 5.0.0`：client PASS=12/FAIL=2、server PASS=22/FAIL=1——3 个 FAIL 全部裁定为 **checker 参数未覆盖 5.0 命名的已知假阳性**（STD-BUILD-006 硬编码 `SPTInstallPath`；STD-CLI-006 仅匹配 `Logger.Log*`；R8 在案、桥项目同因持豁免；模板符合 prose）。
- **双轴评审 + 修复轮（oracle ×2）**：Standards 5 硬项 + 4 判断项、Spec 4 缺口 + 2 超范围 + 3 偏差；修复轮全落地（server README persona 残留清除 / skill 分支深化 / LOG-003 5.0 分支 / client+paired README 补 `StringTemplateId` 坑 / router 与 SharedConstants 注释纠偏 / version-matrix 落位说明）。
- **已裁定披露项**：`pack.ps1` 采用 UTF-8 BOM（本机仅 PS 5.1，BOM-less 中文注释解析失败；4.1 母版为无 BOM）；server 附加护栏与 paired 额外可覆盖属性（超 Q6 两处新增，实测有益）；5.0 客户端 `Nullable=disable`（4.1 为 enable；按蓝图与桥先例保留）；standalone 未采用蓝图示意性 `SptRoot` 派生链（决策⑥「沿用既有约定」）。
- **记录项（建议后续独立处理，未修）**：checker 参数修订（BUILD-006/CLI-006 5.0 命名 + `-TargetSptVersion 5.0` 输入归一；需同步 golden，R8 家族）；paired 双份源码漂移守卫（verify 脚本目前仅查存在性）；STD-CFG-001 静态注册取路径 / STD-CLI-003 逐类 `PatchAll(Type)` 的规则侧注记。
- **提交口径**：单批提交（建议 `feat(templates): SPT 5.0 mod templates (client/server/paired) + linkage revisions`）；**全部变更未提交**。

## 2026-09-28 — KB 追加：Harmony detour 结构体封送事故复盘（ITBS 来源）

- **背景**：ITBS（Inescapable Tarkov's Bot System）P0 实机事故——bot 决策层 Harmony detour 在**未接管（纯放行 `return true`）**状态下静默损坏游戏侧决策数据（`Agent._lastResult.Reason` 全量置空 / node 名退化为数字 ID），第三方调试面板 `EnterBy` 恒空（bot 行为表面正常）。A/B（cfg 开关）+ 原始字段探针定位到 detour 对 `Il2CppSystem.Nullable<…>` 结构体返回的封送损坏；修复 = seam 迁至 void/无参入口（`AICoreAgent<T>.Update()`，零结构体往返），ON≈OFF 分布验收通过。
- **交付（未提交）**：`knowledge/spt-kb/curated/operations/5xx-client-mod-dev-lessons.md` 追加 **§16「Harmony detour 的『结构体封送』陷阱：放行前缀也会损坏原版状态」**（现象 / 排查方法论 5 条 / 根因机制 / 修复模式 / 打补丁前自查清单 6 条）；文件标题与来源行同步（+ITBS 2026-09-28）。
- **KB 索引体检**：`node scripts/spt-kb/sync-index.mjs`（dry-run）**exit 0**——新增 0 / 非法 0 / 孤儿 4（archive 预期项，不随包分发）；同文件内追加无需 `--write`。
- **提交口径**：KB 条目 + dev-log 单批；**全部变更未提交**。

## 2026-10-05 — 报告：SPT5 mod 开发问题与新旧对比（人话版）

- **交付**：`docs/SPT5-mod开发问题与新旧对比报告.md`——面向普通读者的阶段性总结：①实际遇到的问题与难点（interop 代理静默失败 / Harmony 挂钩三重陷阱 / 进程级崩溃与取证 / MO2 部署弯绕 / F12 配置碎玻璃 / 数据服务端兜底 / 服务端无大变化 / 逆向工具链）；②与 SPT4 等旧版本的差别（客户端 Mono→IL2CPP 换代；服务端断层在 3.11→4.x 而非 4.1→5.0）；③常见误解澄清 + 沉淀的硬规矩 + 术语速查表 + 素材来源清单。
- **素材**：两路只读侦察汇总（dev-log 全量 + SPT5 实战 mod 记录：桥 / 雷达 / NoStaminaDrain + 移植技能 + KB 5xx lessons）。
- **索引**：`docs/README.md` 报告表登记（参考）。
- 未提交（按 Overseer 规则）。

## 2026-10-11 — Forge 扫描刷新（元数据 + 源码库）与 4.1.x mod 生态报告

- **元数据刷新**：新增 `scripts/spt-kb/fetch-metadata.mjs`——`api/mods-catalog.json` 全量重刷（1830 → **1969**）；新增 `api/compat-4.1x.json`（4.1.x 兼容 **600**，服务端 SemVer 求值；补丁分布：4.1.5=502 / 4.1.6=547 / 仅 4.1.6=61 / 仅 4.1.5=16）。抓取中定位并修复一个**假阳性 bug**：节流检测用 `/slow down/i` 扫全文，撞上 mod 简介（"Low Budget Bullet Time" teaser 含 "slow down time"）→ 改为先 `JSON.parse` 再判节流（教训入脚本注释与 README）；限速默认 2s。
- **源码库**：窗口 2026-09-14 → 10-11（`filter[spt_version]=~4.1.0`）→ 272 mod / 296 源码条目；新增克隆 **172** 个仓库；`mods/MANIFEST-sp-mod-2026-10.md`（total=296 ok=291 missing=5——缺失 5 条为非仓库形态链接：Google Drive / GitHub 组织页 / 网站 / release zip / 失效 ref）；抓取页（6 页）移存 `.incoming-2026-10/`（`.incoming` 留给下次刷新）。
- **工具修复**：`clone-sources.mjs` URL 规范化扩展（GitHub 裸 `/releases`、`/upload`；Codeberg `/src/branch|tag/<ref>`）——捞回 5 个 mod / 8 条源码（Choccy Milk 的 93R / WA2000 / China Lake 迁移仓库 + ShrapnelFix + Moar Supplies）。
- **报告**：`docs/forge-4.1x-mod-survey-2026-10.md`（生态速览 + 颠覆潜力清单：Fika 联机 / 内容大回移 / 开放世界实验 / 硬核 Ironman / ORBIT 2.0 等）；分析数据与脚本在 `.scratch/forge-4.1x-survey/`；`docs/README.md` 已登记。
- 全部变更未提交（按 Overseer 规则）。

## 2026-10-13 — KB 回填：最近实战经验五条（softcore416 反向移植 / DebugToolkit 整合移植 / 触点核验方法 / MO2 版本误判 / 移植评估方法）

- **背景**：将 2026-10-11 ~ 13 一批 SPT 实战经验沉淀进 spt-kb（此前除 USVFS 长路径条目外均未入库）。编排：4 路只读侦察消化一手材料 → 5 路撰写（主控 1 + @fixer 4，目标文件互不重叠）。
- **新增条目（5 条，合计 547 行）**：
  - `curated/migration/pilot-experience-softcore-416-backport.md`（101 行）——SPT 5.0→4.1.6 反向移植：骨架同构判定 → 15 处适配清单（可空性 9 / 取整 1 / 工程与元数据 3 / 清单外与部署期 2）；null 跳过且不计数；部署保护三保险 / 隔离区 / 日志标定；与 3.11→4.1 跨架构重写的本质区别。
  - `curated/migration/pilot-experience-spt41-debugtoolkit.md`（133 行）——4.1.6 三源整合移植项目复盘：8 工单依赖链 / 触点闸门 / fail-open / F12 配置面 / ADR-0010 身份版本线 / ADR-0011 BigBrain 硬依赖 / 双轴评审闭环（PatchSet 教训：重复代码与健壮性缺陷同根因）。
  - `curated/operations/client-api-touchpoint-verification.md`（110 行）——4.1.x 客户端触点核验标准流程：反编译 + 反射双通路 / 四态判据 / 版本锚定 / 混淆名语义指纹映射 / 跨线污染陷阱对照表。
  - `curated/operations/mo2-spt-version-misdetection.md`（120 行）——MO2 fork 版本误判（4.1.x→4.0.x）：looksValid 谓词等价 + 双排序偏置机制链 / 方案 A 互斥谓词修复 / 独立复验四项 / 复发条件与预防。
  - `curated/migration/porting-candidate-assessment.md`（83 行）——移植候选批量评估方法论：源码真实性核验 / 上游就绪复核 / 有效 LOC 口径 / 难度与结论分类；附仍有效结论（IOF / BRNVG / Softcore-NG）。
- **索引**：`node scripts/spt-kb/sync-index.mjs --write` 新增 5 条，total 227→232，exit 0；写后复跑幂等。交叉引用 14 条路径全量核验存在。
- **说明**：条目事实取自各工作区 spec / 工单 / 评审 / 核实表 / 研究报告（证据锚点随文保留）；未改任何代码、未提交 git；工作区既有未提交改动（多库查询系统 WIP）未触碰。

## 2026-10-14 — 整合包知识全量接入知识库：一包一库（5 新库）+ Norvinsk_Forever 增量回写（10-06 → 10-14）

- **背景**：Overseer 指示「其他 MO2 整合包的知识也不在知识库」→ 全量接入。范围 =《生活在诺文斯克》3.11.4 /《独享的塔科夫》3.11.4 /《无法逃离塔科夫》SPT5 /《诺文斯克潜行者》3.7.x（归档）+《永远的诺文斯克》本体回写。编排：4 路只读侦察（实例与知识载体全量盘点 + 增量账重建）+ 6 路撰写（4 档案卡 + NF 回写双线 + 潜行者档案卡）+ 主控收口。
- **一包一库（kb-registry 2 → 7 库）**：新增 `norvinsk-forever` / `life-in-norvinsk` / `exclusive-tarkov` / `inescapable-tarkov` / `norvinsk-stalker`——root 指向各包目录（`E:/Game/EFT_Offline/…`；envKey 逐库独立、可覆盖），scan 白名单 `docs`，sections 逐文件映射 topic（pack-profile / dev-log / ledger / pending / assessment / patch）。索引逐库构建（`scripts/kb/build-index.mjs --kb <id> --write`，刻意不走默认全库，避免动 spt-kb / norvinsk-notes 既有管线）：norvinsk-forever 5 条 / life-in-norvinsk 2 / exclusive-tarkov 1 / inescapable-tarkov 1 / norvinsk-stalker 1；非法 0。
- **包档案卡（5 份 `docs/README.md`，随包落位）**：独享 80 行 / 生活 111 行 / 无法逃离 94 行 / 潜行者 44 行（归档版）/ Norvinsk_Forever 81 行——首次为各包建档（此前「整合包说明文档」均为空目录，名不副实）。
- **Norvinsk_Forever 增量回写**：dev-log 新增 5 条条目（10-11 B8 → 10-11 DebugToolkit → 10-12 Softcore416+F1/F4/F6+扩充批 → 10-13 更新批+F2 复验 → 10-14 追加安装 2 项），既有条目一字未动；对照表 116 → 245 行（§四–§十一：增量补装 / B8 / 扩充批【重建推定】/ 就地更新 / 禁用清单 12 / 修复登记 / 10-14 追加 / 待确认 7）；pending 同步（6 项被自建/替代覆盖：Softcore 连带 4 项 + DadGamerMode）。
- **数据修复**：norvinsk-notes 两篇无法逃离笔记 version `[3.11]` → `[4.0]`（按正文「基于 SPT4.0.x/SPT4」定标；手改条目，builder 不改写）。
- **验证**：5 库 dry-run/--write exit 0、非法 0；条目 title/topic/version 抽取抽查通过；NF 文档计数交叉核对（modlist 120 = 93 启用 mod + 12 禁用 mod + 15 分隔符；mods 实体 105）。
- **边界（重要）**：运行时注册表为**模块级缓存**（`tools/spt-mcp/src/kb/registry.ts`）——**重启 MCP / 新会话后 7 库可见**；本会话实测仍报 2 库（预期行为，磁盘侧已全部就位）。
- **施工漂移**：实例持续在建（执行快照后 +2：DoorOverhaul / BackpackGunSlot，已补记）；后续新增按 dev-log 惯例滚动记录。
- 全部变更未提交（按 Overseer 规则）。

## 2026-10-14（维护批次）— 4.1.6 目标线文档校准 + 本机工件清理

- **背景**：后续工作以 SPT 4.1.6 整合包搭建为主线（Overseer 指示）→ 全库文档校准 + 过时工件清理。
- **登记更新（4.1.5 → 4.1.6）**：VERSIONS.md（发布线补 4.1.4/4.1.5/4.1.6 行 + 基线改 4.1.6 + 环境表复核实：4.1 线 `4.1.6-RELEASE+731d7a2` / 5.0 线 `8b1e800.20261010`）；根 README（基线行 + 资产行改指 `SamMeow_SP-Tushonka_source_code` + index 计数 221→232 修正现行机检失配）；KB `INDEX.md` / `sources/repositories.md` / `README.md`（4.1.6 源码 tag `731d7a2a` 登记）；RELEASE-NOTES v0.2.0 节；skills 目标版本声明（5 个文件 6 处：using-spt / setting-up / maintaining / evaluating×2 / interpreting）；modding-standard `version-matrix.md` 补 4.1.6 注记（线标签语义：4.1.5 列 = 4.1 线含 4.1.6）；ADR-0006 加 2026-10-14 注记（原文未改写）。
- **对锚证据**：SPT_41x DLL ProductVersion `4.1.6-RELEASE+731d7a2.20260918` == fork tag `4.1.6` commit `731d7a2a`（逐字一致）；4.1.6 = 较 4.1.5 +21 commits / 44 files。
- **工件清理**：移除 5 个已并入 main 的 `omos/ticket-0{2,3,4,5,7}` 工作树 + 分支（零未提交；`.slim/worktrees*` 为 gitignored 本地工件）；清空 `.slim/worktrees.json` lanes；删除仓库根 MO2 崩溃转储 `ModOrganizer-2.5.2-20260924T164004.dmp`（未跟踪、全库零引用）。
- **遗留（未动）**：`dist/portable-plugin`（2026-09-17 便携产物，62.9MB，可由 `scripts/build-portable-plugin.ps1` 重建）与既有未提交改动（KB 回写 WIP 等）。
- 未提交（按 Overseer 规则）。

## 2026-10-14 — 调研交付：LLM 商人聊天 mod 可行性（Messenger + IDialogueChatBot + RAG）

- **背景**：Overseer 设想"在游戏信息面板里与商人文字聊天"（LLM API + 知识库/文档库生成回复，可扩展到 mod 商人），此前判断"没有可行方案"→ 立项调研。
- **交付**：`docs/research/llm-trader-chat-feasibility.md`——三路后台只读侦察汇总（本地 KB/模板/官方示例；本地 Forge 归档 570 mod 源码快照；外部 Forge live API/GitHub/发行说明）+ 主控台磁盘抽查（TrashTalk `UpdatableChatDialogue.CanSendMessage` 补丁、MiyakoChatBot `IDialogueChatBot` 实现、server-mod-examples 目录，逐项核验）。
- **核心结论**：可行且已有整例——Miyako Carry Service（#2709，自定义商人 + LLM 多 provider 对话）与 Trash Talk（#2816，Messenger 回复 + 客户端输入解锁）已在 4.1.x 实现并发布；空白 = "泛化到任意第三方 mod 商人 + RAG 知识库"（无先例，机制齐备）。服务端官方扩展点 `IDialogueChatBot`/`MailSendService`；客户端解锁点 `UpdatableChatDialogue.CanSendMessage`。
- **索引**：`docs/README.md` research 表（7 → 8 篇）。
- 未提交（按 Overseer 规则）。

## 2026-10-14 — LLM 商人聊天：设计冻结 + spec + 工单发布（grill → to-spec → to-tickets）

- **设计闭合**：grill-with-docs 三轮（11 + 9 + 2 项决策全部闭合，均按推荐）——载体=复用原生 Messenger（入口并入商人现有消息线程，骨架不通过则回退独立联系人）；试点=Peacekeeper + Therapist（mod 商人验证排 M2）；v1 纯配置驱动扩展；LLM=OpenAI 兼容单层（Ollama/DeepSeek 示例）；知识库=本地文档 + 轻量关键词检索（留检索后端适配口）；会话=被动回复 + 一次性开场 + reset 指令，历史存 profile（注入最近 20 条）；默认中文；延迟 2–6s 可配；不承诺 Fika、按共存设计；非目标=语音/交易联动/自建面板/5.0。
- **双端触点核验**（fixer，ilspycmd + 反射双通路）：客户端 8 组、服务端 6 组全部命中（2 处修正：字段 `_receiveAllButton` 单下划线、`CG_TransferAll` 为方法）；关键发现：`[Injectable] IDialogueChatBot` 自动收编 + `ChatbotFeatures.EnabledBots` 门禁；产物 `.artifacts/llm-trader-chat-ilspy/`；报告 §8 已补记。
- **交付**：`.scratch/llm-trader-chat/spec.md`（ready-for-agent：26 条用户故事 / 17 项实现决策 / 4 层测试接缝 / 6 条验收清单）+ 11 张工单（issues/01–11；frontier：01 立即可施工，03/04/05 可并行）。
- **下一步**：工单 01（项目脚手架）起按 frontier 施工；需 Overseer 实机操作的环节（M1 骨架验收等）将提前提示。
- 未提交（按 Overseer 规则）。

## 2026-10-14 — LLM 商人聊天：工单 01–05 实现 + MO2 部署 + 双轴评审闭环（M1 实机待验）

- **工单 01（脚手架）**：`mods/SamMeow-LLMTraderChat/` 四工程（Client netstandard2.1 / Server net10.0 / Shared netstandard2.0 / Tests xunit net10.0）+ ADR×3 + pack.ps1；oracle 评审修订后收口（ADR 补齐 + NIT×4）。
- **工单 02（M1 骨架）**：客户端 `CanSendMessage` Postfix 解锁（目标＝Therapist，收敛为 Shared 常量）；服务端路线 A（`[Injectable] IDialogueChatBot`，回显经 `MailSendService`；`EnabledBots` 门禁经源码定性为不影响发信路由且有意不开启）+ 缺失对话一次性补发；三项未知（输入解锁/路由归属/同线程落回）待 Overseer 实机回报。
- **工单 03–05（配置/LLM/检索）**：ConfigLoader 纯函数 + `IOnDIConstruct` 首启复制（示例配置随包）；OpenAI 兼容 LlmClient（失败面＝结果对象，含 endpoint/读体异常映射）；KeywordKnowledgeRetriever（CJK 双字 + 上限）。oracle 双轴评审修订后收口（异常逃逸 + README 漂移两条 SHOULD 已修）；测试 37 全绿（5 + 27 + 修订 5）。
- **MO2 自动部署**：修正 MO2 MCP 绑定（旧路径失效 → `Norvinsk_Forever`，其实例数据目录；game_path＝`SPT_41x`，VFS 托管）；overlay `[9]全局功能大修-商人LLM对话-llm-trader-chat-test-build` 安装并启用（priority 81，[9] 组内）；profile 备份 `pre-llmtc-m1`；快照 adaaeddd / 0048ebe7。注意：本地部署的构建与源码头存在一轮修订差（下次测试轮前更新）。
- **遗留**：M1 实机验证（Overseer 操作中）；check-mod-standard 2 类 FAIL（CFG-006 → 客户端工单；BUILD-001/006 + META-003 → 检查器缺陷，发布门禁前修或 waiver）；数值钳制与 prompt 模板 → 工单 06 验收项已登记。
- 未提交（按 Overseer 规则）。

## 2026-10-14（补）— M1 实机验证通过（LLM 商人聊天）

- **结果**：Therapist 对话打字「喂」→ `/client/mail/msg/send` → `[LLMTC][recv]`（路由=IDialogueChatBot）→ `[LLMTC][reply]`（14 字）→ 同一对话目视收到 `[M1-echo] 收到：喂`（无需额外刷新补丁）。三个未知落定：① 输入解锁生效；② 路线 A 实机成立（`EnabledBots` 门禁确无影响）；③ 同线程回显成立。
- **旁证**：未启用商人（Prapor）无输入框、原版行为；两端日志无本 mod 报错；`[LLMTC][config]` 行正常（DI 钩子生效）；seed 未触发（档案已有商人对话题）。
- **工单 02 收口**；下一 frontier＝工单 06（引擎），随后 07（接线 + M2）。
- 未提交（按 Overseer 规则）。

## 2026-10-14（续）— 工单 06（引擎核心）收口 + 工单 07 派发

- 交付：`src/Server/Engine/*`（六态 outcome + 只读历史/状态/模板接口 + 内置模板兜底）+ `IKnowledgeRetrieverFactory` + `knowledgeTopK` + `config/prompts/system-zh.txt`；测试 73 全绿（5+27+5+26+10）。
- oracle 双轴评审修订后收口：E1 prompt 尺寸预算（历史 500/条·4000 总量；知识 800/条·3000 总量，截断告警）+ 单趟替换 + 异常/兜底面 + 测试补强全部完成。
- frontier 推进：工单 07（服务端接线 + 试点内容）已派发；M2 实机需 LLM 可用（Ollama 或云端端点——待 Overseer 确认环境）。
- 未提交（按 Overseer 规则）。

## 2026-10-14（续 2）— 工单 07 评审闭环 + 待 M2 实机

- 工单 07 agent 侧完成：引擎接线（历史/状态/开场/延迟/日志）+ 两试点商人 persona 与知识文档草稿 + 客户端名单配置化（STD-CFG-006 转 PASS）；测试 97 全绿（主控台复验）。
- oracle 双轴评审修订后收口：reset 改"选择性清空"（保留未领取附件 + 角标重算）；开场原子 claim（TryClaimOpeningAsync）；客户端边界补强（空数组语义/大小写/异常面）。
- 待办：Overseer 审改人设与文档 + LLM 环境确认 + 关闭游戏/服务器/MO2 以重部署；随后 M2 实机（清单已备）。
- 未提交（按 Overseer 规则）。

## 2026-10-14（续 3）— M2 验收通过；工单 12/08 完成与评审

- **M2 实机验收通过**（工单 07/09 收口）：两商人真实 LLM 对话（DeepSeek `deepseek-flash`，耗时 2.0–3.0s）；检索命中、reset 选择性清空、开场一次性、延迟、兜底路径全部实证。发现：中文全角标点被原版输入白名单剥离。
- **工单 12**（标点修复）：过滤点＝`EFT.UI.ValidationInputField` 白名单（`\w` 含 CJK 解释"汉字过、标点拦"）；补丁按"解锁名单"限定作用域；oracle 评审可收口（代码侧），实机复验待下轮。测试 103。
- **工单 08**（mod 商人接入）：Overseer 指定「油漆匠 Painter」（id `668aaff35fd574b6dcc4a686`）；配置侧接入 + 最小接入契约（三处配置、零代码）入文档；oracle 评审修订后收口（README LLM 段重写 + 文档同步 + 升级 nuance）。测试 104。
- **部署前须知**（oracle SHOULD-2 两颗雷）：① 运行时 `BepInEx\config\SamMeow.LLMTraderChat.traders.json` 需加 Painter id（或删除重建）；② overlay 刷新必须保留已填 DeepSeek key（当前 overlay config 含真实 key）。
- 未提交（按 Overseer 规则）。

## 2026-10-14（续 4）— 工单 12/08 实机验证通过并收口

- **工单 12**：全角标点实机进入服务端（`text="我就是路过的……"`、`text="你都有啥？"`，本地 23:21）；对照项未单独实测（按作用域结论维持）。
- **工单 08**：3/3 商人启用；Painter 开场 + ≥2 轮真对话 + 重置全通过；客户端名单含 Painter；零代码接入验证成立；原版交易/任务按 Overseer 确认无变化。
- **部署过程**：文件级覆盖（保 key，注入核验 `sk-75***c1e1`）+ 运行时名单加 id；overlay 与 downloads 副本同步。
- frontier：工单 10（检索接线/文档打磨/M3）派发；随后 11（共存 + 终验 + 发布准备）。
- 未提交（按 Overseer 规则）。

## 2026-10-15 — LLM 商人聊天：工单 10/11 收口 + 终审 + 最终部署（12/12 工单闭环）

- **工单 10（M3 检索）**：`[kg] 命中数/来源` 日志、3×2 文档细节验收问答（Overseer 实测「提问都没问题」）、FAQ 文档侧打磨（未调参）、apiKey 占位符启动警告；oracle 评审可收口。
- **工单 11（终验 + 发布准备）**：`docs/acceptance.md`（六条验收汇总 + 共存分析[未同装] + NIT-2/3 决定与理由）、`docs/standards-waiver.md`（检查器 3 条误报豁免 + severity + 门禁注记）、发布资料收尾（README/隐私/AI 声明/release）；终审「修订后收口」→ 修订轮（仅文档状态同步）。
- **最终部署**：NF overlay = 最终构建（保 key `sk-75***c1e1`；Server.dll 78,848B @ 00:32:52；3 商人；downloads 副本同步）；运行时客户端名单含 3 商人；`[kg] 来源` 自本构建生效。
- **交付状态**：12/12 工单闭环；测试 108/108；check-mod-standard 27/3（3 条已豁免记录）；zip 76,921 bytes / 28 entries 可部署。
- **遗留（流程外）**：`git commit`、Forge 上传、release notes——均未执行（按 Overseer 规则）。
- 未提交（按 Overseer 规则）。

## 2026-10-15（续）— KB 回填：LLM 商人聊天经验 2 条（配方 + 复盘）

- **新增条目**（`node scripts/spt-kb/sync-index.mjs --write`，total 232 → 234；非法 0；孤儿 4 为 archive 预期项不处理）：
  - `curated/recipes/13-trader-chat-pipeline.md` — 玩家↔商人文字聊天管线配方（客户端 `CanSendMessage` 解锁 / `IDialogueChatBot` 路线 A / `MailSendService` 回信 / 开场 / reset 语义 / `ValidationInputField` 标点白名单坑 / 共存分析）。
  - `curated/operations/llm-mod-integration-lessons.md` — LLM 接入 SPT mod 工程复盘（失败面契约 / apiKey ASCII 坑 / prompt 预算 / 轻量 RAG 适配口 / MO2 保 key 部署循环 / 人设蒸馏方法）。
- **索引与登记**：`knowledge/spt-kb/INDEX.md`（日期 + 「任务配方与实战复盘」节）；`curated/recipes/README.md`（11 → 13 条，补登 12/13）。
- 未提交（按 Overseer 规则）。

## 2026-10-15（续 2）— 全商人接入批次（工单 13）：12 原版 + 7 mod 共 19 位

- **范围**：接手（handoff `docs/handoff-llm-trader-chat-2026-10-15.md`）。侦察确认整合包商人人集＝12 位原版（SPT 4.1.6 trader DB）+ 7 位 mod（Painter 既有 / Artem / Scorpion / Croupier / Hideout Harry / Badger / Courier）；排除项（BARF 箱子、Ref 任务补丁、Tradermodding UI、改原版 assort 的若干 mod、停用 mod 等）记录于工单 13。
- **实现**：`config.jsonc` / `defaultConfig.jsonc` 各 19 条（persona + 开场语 + docsDir + resetKeywords，两份 traderId 集合由单测守护）；`SharedConstants` 新增 16 位 id 常量 + `DefaultEnabledTraderIds` → 19；`ConfigLoaderTests` 计数 3→19 + 新增「客户端默认名单覆盖」守卫测试。
- **文档**：64 篇新知识文档（16 位 × 4）由 4 条 fixer lane 并行产出，风格对齐治疗师样例；结构核查 19 目录 × 4 文件 + 抽查通过。
- **门禁**：`dotnet test` **109/109**；`pack.ps1` 产出 `release\SamMeow.LLMTraderChat-0.1.0.zip`（137,701 bytes）。
- **部署**：文件级覆盖 overlay（robocopy，保留 meta.ini）→ 注入旧 key 并核验（`sk-75***c1e1`，placeholder=False）→ config traders=19；运行时客户端名单 JSON → 19 位；`downloads\llmtc-m1.zip` 同步；overlay README/GLOSSARY 随新包刷新。
- **待办**：Overseer 实机验收（服务器 `[LLMTC][route]` ×19 + 抽测新商人对话），通过后工单 13 收口并补记 acceptance。
- 未提交（按 Overseer 规则）。

## 2026-10-15（续 3）— 全商人接入实机验收通过；工单 13 收口

- **验收证据**（`spt20261014.log` 17:15 起）：`[LLMTC][config] 启用商人=19/19`；`[LLMTC][route]` ×19 全名单；16 位新商人开场全部发出；新商人真对话（Mechanic / Croupier / Fence / BTR Driver，含 `[kg]` 命中 `04-faq.md` / `01-background.md`）；窗口内无 `[LLMTC]` 告警；客户端 `可聊天商人=` 19 位。Overseer 确认「测试没问题」。
- **收口**：工单 13 勾选完成；`docs/acceptance.md` §四 补记通过证据。全商人接入批次（12 原版 + 7 mod = 19 位）交付完成。
- **下一工作（已交接）**：商人按玩家**任务进度与好感度（standing）动态改变态度**——v2 特性，交接文档已备（见 `docs/handoff-llm-trader-chat-attitude-2026-10-15.md`）。
- 未提交（按 Overseer 规则）。

## 2026-10-15（续 4）— v2 动态态度（工单 14）：grilling → 实现 → 双轴审查 → 部署

- **决策（grilling Q1–Q7）**：状态信号＝该商人 standing/loyalty/unlocked + 名下任务进度（活跃/最近完成/计数）+ 玩家阵营与等级带；5 档 `Hostile/Cold/Familiar/Trusted/Ally`（`Unlocked==false` 或 `Standing<0` → Hostile；否则按 `LoyaltyLevel` 1/2/3/4+）；内置通用档位语义 + per-trader `attitudePrompts` 可选覆写；注入粒度＝计数 + 活跃任务名≤5 + 最近完成≤3（单名≤60 字，整段≤600 字）；反泄露禁令；prompt 新增 `{{attitude}}` 且旧模板自动追加兼容；服务端日志 `[LLMTC][attitude]`。
- **实现**：新增 `TraderAttitudeEvaluator`（评测/渲染/诊断）、`TraderRelationshipSnapshot`、`ITraderRelationshipProvider`、`SptTraderRelationshipProvider`（4.1：`ProfileHelper`/`TemplateTable`/`LocaleService`）；改动 Config（`AttitudePrompts` + `TraderAttitudeKeys`）、`TraderChatEngine`（注入关系提供器 + 渲染 + 兼容 + 告警）、`TraderChatBot`、`SystemPromptDefaults`/`system-zh.txt`；严格垂直切片 TDD。
- **验证（orchestrator 独立复验）**：`dotnet build -c Release` 0 警告 0 错误；`dotnet test -c Release` **144/144**（原 109，+35）。
- **审查**：`@oracle` 双轴（Standards / Spec）。修订 R1–R7（裸 catch → 记日志；`AttitudeTier` 注释术语；档位键单一归属 `TraderAttitudeKeys`；`config/README.md` 补齐；测试魔数 → 常量；默认模板补禁令条款；模板缺 `{{persona}}`/`{{attitude}}` 告警）。驳回 Spec 轴 `MongoId.IsValidMongoId` 误报（4.1.6 源码实为 24 位十六进制校验）。记录于 `issues/14` §审查记录。
- **部署（最小保 key 集）**：仅覆盖变化文件——`Server.dll`（102,400 B，二进制含 `EvaluateTier`/`RenderAttitudeBlock`）、`config/prompts/system-zh.txt`（含 `{{attitude}}`）、`config/README.md`、根 `README.md`/`GLOSSARY.md`；**`config.jsonc` 未动**（真实 apiKey 在位，placeholder=False）；`downloads\llmtc-m1.zip` 同步（149,679 B）；zip `release\SamMeow.LLMTraderChat-0.1.0.zip`（149,679 B）；覆盖前备份在 `.artifacts/overlay-backup-llmtc-v2-20261015/`。部署时游戏/服务器未运行（MO2 开着）。
- **待办**：实机验收（低好感 vs 高好感语气差异可判读 + `[LLMTC][attitude]` 日志 + 不泄露内部数值），由 Overseer 进游戏回报。
- 未提交（按 Overseer 规则）。

## 2026-10-16（续 5）— 实机故障：商人回复兜底话术（「联系不上」）根因定位与修复

- **症状**：与 Therapist 对话两条消息均收到兜底文案「（商人暂时联系不上，请稍后再试。）」（玩家截图 + 日志 `[LLMTC][fallback]`）。
- **排障链路**：`[LLMTC][recv]` → `[LLMTC][route]` → `LLM 返回空内容` → `结果=Fallback`；同时 `[LLMTC][attitude] 档位=Cold(unlocked=True,ll=1,standing=0.24,done=2,active=2)` 正常 → **v2 接线无恙，问题在 LLM 响应侧**。
- **直连 API 对照实验**（key 取自部署副本，仅内存使用、未打印）：最小提示词正常（`finish=stop`、content 154–168 字、响应含 `reasoning_content`）；近似真实 prompt + `max_tokens=512` → 复现 `finish=length`、`contentLen=0`、`reasoningLen=2038`；同 prompt + `max_tokens=2048` → `finish=stop`、`contentLen=155`。
- **根因**：`deepseek-flash` 为**推理型模型**，推理链同样计入 `max_tokens` 预算；512 被推理吃满时 API 返回空 `content`，mod 表现为兜底话术。v2 态度块使 prompt 由 314 → 449 token，显著提高触发概率（10/15 的 v1 成功属概率性通过，日志 0 次空响应）。
- **修复（配置层，无需改码）**：`maxTokens` 512 → 4096（仓库 `config.jsonc` / `defaultConfig.jsonc` 加注原因；部署副本以「仓库文本 + 原位注入 key」重写，核验 `keyUnchanged=True`、mask `sk-75***c1e1`、placeholder=False）；重新打包 `release\SamMeow.LLMTraderChat-0.1.0.zip`（150,107 B）并同步 `downloads\llmtc-m1.zip`。
- **后续工单**：`issues/15-llm-empty-response-diagnostics.md`——客户端应解析 `finish_reason`，把 `length` 截断与「模型空回复」区分开，日志直指「调大 maxTokens」。
- **待办**：Overseer 重启 SPT 服务器后复测对话。
- **复测通过（同日 13:54–13:58，重启后）**：6/6 轮真实回复（107 / 93 / 150 / 108 / 101 字），`[LLMTC][attitude]` 逐条输出（Therapist 档位=Cold, unlocked=True, ll=1, standing=0.24, done=2, active=2），`[kg]` 命中 `02-stance.md` / `04-faq.md`；0 次空响应。引擎耗时 2.9–12.6 s（推理型模型特性，叠加 2–6 s 回复延迟后玩家可见等待约 10–18 s，如需提速可评估换非推理模型）。
- 未提交（按 Overseer 规则）。

## 2026-10-16（续 6）— v3 任务知识（工单 16/17）：实机缺陷取证 → grilling → 实现 → 双轴审查 → 部署

- **触发与取证**：玩家问 Therapist「水瓶座行动」，模型**自信编造**——首答地点为「储备站水处理区」（实为**海关宿舍楼**），被纠正后未承认不知道，反以「我知道但不细说」的姿态补圆（「编号我不背」「带盥洗室那间」）。取证：Therapist 四篇知识文档对 钥匙/水瓶/海关/宿舍/任务 命中**全 0**、检索 `[kg] 命中数=0`；对照复现（真实模板+空知识）确认编造行为。
- **判定修正**：**不是好感度扣留，也不是无害的「不知道」，而是自信编造**——比拒答更危险（会误导玩家跑错地图/买错钥匙）。
- **决策（grilling Q1–Q7，Overseer 全部采纳）**：仅注入该商人名下进行中委托详情；态度档位→披露三档 None/Hint/Full；独立占位符 `{{quests}}` 与独立预算（单任务 ≤500 字、整段 ≤800 字）；静态层改为**自动生成**的任务手册（非人工起草）；Full 档允许报房间号/钥匙名；加入反编造硬约束。
- **实现**：工单 16（`DisclosureLevel`/`TraderQuestDetail`/`TraderQuestRenderer` + 引擎/提供器/模板接线，测试 144→165）；工单 17（`scripts/gen-quest-manuals.mjs` + 15 份 `05-quests.md`，742 条任务、幂等、跳过 4 位零任务商人）。
- **关键数据事实**：任务目标文本＝**裸 24-hex 条件 id 作为独立 locale 键**（链路 `Quest.Conditions.AvailableForFinish[].Id`，非 `_props.id`）；实机 profile 位于 MO2 profile 的 `saves\`（非游戏目录 `SPT_Runtime\user\profiles`）。
- **双轴审查（@oracle）**：Standards 1 条术语漂移（已统一为「任务知识」）；Spec 4 项，其中 **1 项击穿验收靶心**——`None` 档/零委托时反编造禁令整段消失，叠加模板缺「禁止借口搪塞」子句，实机 Cold 档 prompt 内**无任何反编造指令** → 修订 R1–R6（禁令恒存 + 模板补全 + Hint 收敛 + 预算诊断校正）后 165/165。
- **部署**：最小保 key 集（DIFF 5 + NEW 15）覆盖 overlay，**20/20 哈希校验通过**；`config.jsonc` 未动（key 在，`maxTokens=4096`）；`Server.dll` 109,056 B；zip 350,801 B；`downloads\llmtc-m1.zip` 已同步；备份在 `.artifacts/overlay-backup-llmtc-v3-20261016/`。
- **待办**：实机验收（Cold 档问任务细节应拒答且不编造；未接任务提问应命中 `05-quests.md`；日志出现 `[LLMTC][quests]`）。
- 未提交（按 Overseer 规则）。

## 2026-10-16（续 7）— 流程缺陷修复：MO2 `meta.ini` 描述字段冻结（P1–P4）

- **事故**：`[9]全局功能大修-商人LLM对话…` overlay 的 `meta.ini` `comments/notes` 冻结在 M1（"M1 测试构建 / 仅解锁 Therapist 回显"），而 mod 已经历 6+ 次能力跃迁（19 位商人 / LLM 回复 / 动态态度 / 任务知识）——元数据与实际能力完全脱节。
- **根因（四层）**：① 部署流程 `robocopy（保 meta.ini）` 保护 MO2 记账字段（正确），但**没有配对的"刷新描述字段"步骤**；② `meta.ini` 只存在于游戏实例、**不在仓库** → 脱离版本控制 / 打包 / 审查三条防线；③ `STD-PKG-002` 旧文把 `notes` 定义为「安装记录（来源/版本/安装日期）」——按字面 **M1 文案当时是合规的**，规则未要求随状态更新；④ mo2-mcp 的 create/install 只在创建时写一次，`set_mod_notes` 需人工调用，**无任何机制绑定代码状态与元数据**。
- **修复（P1–P4）**：
  - **P1/P2 工具与真源**：新增 `tools/mo2-meta-sync/sync.mjs`（push / pull / self-test；QSettings 转义；只改 `comments`/`notes`/`version`/`installationFile`，其余字节逐字不动；保留 CRLF/BOM；原子写）+ `README.md`；本 mod 真源落地为 `mods/SamMeow-LLMTraderChat/deploy/meta.json`（经 `--pull` 自 overlay 引导）。
  - **P3 流程**：meta-sync 写入 `building-spt-modpack`（Step 2 必需步骤 + Step 4 验收第 7 项 + Anti-patterns 一条）、`porting-spt-mod-to-spt5` Phase 5、`writing-spt-mod` Step 5（**事故实际发生地**）。
  - **P4 规则**：`09-packaging.md` 的 **STD-PKG-002 就地重写**为现行语义（`comments`=一行摘要；`notes`=当前能力/前置/隐私/回滚；描述字段须反映当前能力；N-A 条款限定）；新增 **STD-PKG-008**（真源在仓库 + 部署同步 + 触发条件 + 反模式「保 meta.ini 不得作为唯一部署手段」）；证据升级——`STD-PKG-002` 的 `EV-NOCORPUS` 摘除，新增语料证据 `EV-CORPUS-MO2META`；`rules.json` 84→85、`evidence-index.md`、`README.md` 计数同步。
- **验证（orchestrator 亲测，非采信自述）**：`sync.mjs --self-test` → `SELF-TEST PASS`；`--pull` 自真实 overlay 引导 4 键 → `--dry-run` 回推报 `no changes (4 key(s) already in sync)`（幂等，overlay 哈希未变）；`EV-CORPUS-MO2META` 登记于 `evidence-index.md:308` 且被 `09-packaging.md:48/156/173` 引用；`rules.json` 中 `STD-PKG-002` 已不含 `EV-NOCORPUS`；3 个技能文件引用 `mo2-meta-sync`。
- 未提交（按 Overseer 规则）。

## 2026-10-17（续 8）— 新增规范 STD-PKG-009：每个 mod 维护仓库内 `CHANGELOG.md`

- **需求（Overseer）**：每个 mod 的**首个可用版本后**新建 changelog，记录版本与版本变化，供后续每次修改与版本迭代留痕。
- **规范**：`09-packaging.md` 新增 **STD-PKG-009**（MUST / both）——产物 `<mod>/CHANGELOG.md`（随代码版本控制）；首个可用版本产出时创建并写入该版本；此后每次版本迭代或用户可见变更**必须**追加；格式 `## <version> - <YYYY-MM-DD>`（最新在上；分组 新增/变更/修复/移除/已知限制）；**版本三处一致**（`Directory.Build.props <Version>` ＝ `deploy/meta.json version` ＝ CHANGELOG 最新节）；与整合包级 `<project>/docs/release-changelog.md` 是**两个层级、互不替代**；N-A 口径同 STD-PKG-002。
- **证据决策**：语料**存在**（Forge 归档 61 个 `_source` 目录含 `CHANGELOG.md`，全盘 109 份，样本 ≥10）→ 登记新语料锚点 `EV-CORPUS-CHANGELOG`，**未**走 `EV-NOCORPUS`；机制侧如实标注为「版本真源一致性锚点」（`STD-META-005` / `STD-PKG-008`），不冒充加载机制。
- **技能接线**：`writing-spt-mod`（主落点：新增「版本与 Changelog」段 + Level B 验收第 5 条 + paired 单仓库单份）、`writing-spt-modpack-changelog`（新增 `Scope: modpack level, not mod level` 层次对照 + When-NOT-to-Use 一行）、`porting-spt-mod-to-spt5`（Phase 5 第 7 条 + 完成判据）；`building-spt-modpack` 经判定**无需改动**（该技能面向第三方 Forge 包，CHANGELOG 属仓库产物）。
- **本 mod 落地**：新建 `mods/SamMeow-LLMTraderChat/CHANGELOG.md`（`0.1.0` 单节 + M1→v3 里程碑分组 + 4 条已知限制）；`pack.ps1` 随包清单加入 `CHANGELOG.md`；README 结构图更新。
- **验证（orchestrator 亲测）**：`scripts/validate-mod-standard.ps1` **PASS**（13 章 / **86 条**；MUST=29 SHOULD=52 MAY=5；**MUST 双源标记 29/29**；registry ↔ prose 双向一致）；`STD-PKG-009` 登记于 `rules.json` + `09-packaging.md`，`EV-CORPUS-CHANGELOG` 登记于 `evidence-index.md`；**版本三处一致 = True**（0.1.0 / 0.1.0 / 0.1.0）。
- **部署**：增量 **6/6 哈希校验通过**（新增 `CHANGELOG.md`、更新 `README.md`、重建 DLL），zip 353,051 B 并同步 `downloads\llmtc-m1.zip`；`config.jsonc` 的 key 完好；覆盖前备份在 `.artifacts/overlay-backup-llmtc-changelog-20261017/`。
- 未提交（按 Overseer 规则）。
