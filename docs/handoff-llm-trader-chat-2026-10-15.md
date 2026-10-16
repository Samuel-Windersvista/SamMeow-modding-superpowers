# 交接文档：LLM 商人聊天（LLM Trader Chat）— 2026-10-15

> 状态：**项目完成（12/12 工单闭环，已实机验收，已部署至整合包）**。新会话接手目标：正式发布动作（commit / Forge）与/或 v2 迭代。
> 本文只放"接手动线"与操作要点；细节一律引用既有工件，不复制内容。

## 1. 一页索引（建议阅读顺序）

| 工件 | 路径 | 说明 |
|---|---|---|
| Mod 源码 | `mods/SamMeow-LLMTraderChat/` | `README.md`＝入口（安装/配置/隐私披露/AI 声明/最小接入契约/验收状态）；`docs/acceptance.md`（六条验收汇总 + 共存分析 + NIT 决策）；`docs/standards-waiver.md`（检查器 3 条误报的豁免记录） |
| Spec 与工单 | `.scratch/llm-trader-chat/` | `spec.md` + `issues/01–12`（全部收口；每张单的评论含评审/修订/实机证据） |
| 研究报告 | `docs/research/llm-trader-chat-feasibility.md` | 可行性、先例、机制证据；§8＝4.1.6 双端触点核验补记 |
| 时间线 | `docs/dev-log.md` | 「设计冻结 + spec + 工单」「实现 01–05 + MO2 部署」「续 3」「续 4」「2026-10-15」条目 |
| 触点核验产物 | `.artifacts/llm-trader-chat-ilspy/` | 4.1.6 双端 ilspy dumps（ChatScreen / DialogueController / MailSendService 等） |

## 2. 环境状态（实机）

- **MO2 实例**：数据目录 `E:\Game\EFT_Offline\Norvinsk_Forever`（game_path＝`SPT_41x`，VFS 托管）；MO2 程序目录 `E:\Game\EFT_Offline\SPT-Organizer-v2.5.2-Novinsk`。
  **注意**：MO2 MCP broker 未连接（live-only 操作如 `mo2_reinstall` 不可用）——部署走"文件级覆盖"路径（见 §4-1）。
- **部署 overlay**：`[9]全局功能大修-商人LLM对话-llm-trader-chat-test-build`（enabled、priority 81、[9] 组内）；已含最终构建（Server.dll 78,848 bytes @ 2026-10-15 00:32）。
- **安全网**：profile 备份 `pre-llmtc-m1`（`Norvinsk_Forever\.mo2-mcp\profile-backups\`）；安装/位置快照 `adaaeddd` / `0048ebe7`（另多轮部署快照）。
- **运行时客户端名单**：`Norvinsk_Forever\overwrite\BepInEx\config\SamMeow.LLMTraderChat.traders.json`（3 商人：Therapist / Peacekeeper / Painter）。
- **LLM**：DeepSeek `deepseek-flash`（`https://api.deepseek.com`，无 `/v1`）；**key 只存在于部署副本的 `config.jsonc`（不在仓库）**，其值形如 `sk-75***c1e1`（本文档不记真实值）。
- **仓库**：**全部未提交**（按 Overseer 规则）；`git status` 含 `mods/SamMeow-LLMTraderChat/`（untracked）与一批既有未提交改动。

## 3. 完成度与证据（引用）

- 12/12 工单收口（`issues/01–12`）；测试 **108/108**；最终 zip `release/SamMeow.LLMTraderChat-0.1.0.zip`（28 条目，结构可部署）。
- 验收：spec 六条全部通过（M1 链路 / M2 真对话 / M3 文档问答 / 标点修复 / 零代码接入）——证据在工单 02/07/09/10/12 评论 + dev-log 续 4。
- 门禁：`check-mod-standard` 27 PASS / 3 FAIL＝**检查器缺陷误报**（详见 `docs/standards-waiver.md`，修复去向已列）。
- 共存：按「未同装」结案 + 代码侧冲突分析（`docs/acceptance.md` §二；将来同装按 3 条观察点核对）。

## 4. 关键操作手册（坑位）

1. **重新部署（保 key 流程）**：关闭游戏 + SPT 服务器（DLL 锁；MO2 可留开着）→ `scripts/pack.ps1` 产 zip → 文件级覆盖（robocopy 到 overlay 目录）→ **保 key**：部署前从旧 `config.jsonc` 提取 `apiKey`，覆盖后注入新 config → 核验 `placeholder_present=False` → 同步 `Norvinsk_Forever\downloads\llmtc-m1.zip`。（本轮执行记录见工单 07/11 评论。）
2. **PowerShell 坑**：路径含 `[9]` 等方括号时必须 `-LiteralPath`（否则被当通配符）；中文输出乱码时，把结果写入 UTF-8 文件再用 read 工具查看（本会话的做法）。
3. **日志**：服务器 `Norvinsk_Forever\overwrite\SPT_Runtime\user\logs\spt\sptYYYYMMDD.log`；客户端 `E:\Game\EFT_Offline\SPT_41x\BepInEx\LogOutput.log`；统一 `[LLMTC]` 前缀（`recv / route / kg / engine / reply / fallback / notready / reset / opening`），`[kg]` 含"命中数 + 来源"。
4. **构建/测试/打包**：`dotnet test mods/SamMeow-LLMTraderChat/SamMeow.LLMTraderChat.sln -c Release`；打包 `mods/SamMeow-LLMTraderChat/scripts/pack.ps1`（含 UTF-8 BOM——PS 5.1 必需，勿重存为无 BOM）。
5. **新增商人（零代码）**：服务端 `config.jsonc` 的 `traders[]` + `knowledge/traders/<id>/` 文档 + 客户端名单 JSON；重启生效。契约原文在 mod `README.md` 与 `config/README.md`。
6. **行为语义速查**：回复延迟 2–6s 可配；`重置`（整句，trim+大小写不敏感）＝选择性清空（保留带附件消息）；开场消息一次性（at-most-once）；无命中降级模板不硬答；未启用商人零影响。

## 5. 下一步候选（按优先级）

1. **正式发布**：`git commit`（建议单批 feat 提交或按批次；提交口径可参考各工单评论）；Forge 上传（README 已备 AI 生成声明 / 隐私披露 / 安装配置指引）；release notes（可选）。
2. **检查器修复（仓库级，非 mod 范围）**：`STD-BUILD-001`（paired 布局取首个 TFM）、`STD-BUILD-006`（命名族 `SptRoot/SptClientDir/SptRuntimeDir`）、`STD-META-003`（常量间接引用）——修好后同步移除 `docs/standards-waiver.md` 对应条目。
3. **v2 迭代候选**：嵌入检索后端（适配口 `IKnowledgeRetriever` 已预留）、更多商人物料、消息 UI 刷新补丁（观察点）、打字指示、Fika 评估（未承诺）。
4. **同装共存实测**（若将来安装 Trash Talk / Miyako）：按 `docs/acceptance.md` §二观察点核对。

## 6. Suggested skills（新会话按需调用 Skill 工具）

- 继续开发 / 修 bug：`implement`；新需求先走 `grill-with-docs`。
- 发布前复审提交批次：`code-review`。
- 发版笔记：`writing-spt-modpack-changelog`；过程记录：`writing-spt-modpack-devlog`。
- SPT 领域问答/冲突分析：`spt-mcp-automation`、`spt-conflict-audit`（按需）；`using-spt-modding-superpowers` 由插件自动注入。
- 再次交接：`handoff`。

## 7. 安全与红线

- 不 commit（除非 Overseer 明确指令）；**不把 DeepSeek key 写入仓库或任何文档**。
- 游戏安装目录（`SPT_41x`）不做直写物理修改——一律 MO2 overlay（或既定"文件级覆盖 + 备份"流程）。
- 部署前确认 `pre-llmtc-m1` 备份与快照可用于回滚；部署后核验 key 未丢（mask 比对）。
