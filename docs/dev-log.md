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
