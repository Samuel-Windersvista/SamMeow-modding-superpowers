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
