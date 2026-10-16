# 07: 服务端接线 + 试点内容（M2）

**What to build:** 用真引擎替换 M1 的回显接线；落地 Peacekeeper 与 Therapist 的 persona 与知识文档（agent 起草、Overseer 审改后入库）；实现回复延迟（默认 2–6 秒可配）、首次开场消息、reset 指令与结构化日志（收到 / 路由判定 / 检索命中数 / LLM 耗时 / 兜底触发）。达成：两试点商人可与 LLM 真对话（M2）。LLM 未配置或不可用时给出"未就绪"类提示而非静默。

**Blocked by:** 02: M1 骨架；06: 引擎核心

**Status:** ready-for-agent

- [x] 实机：两试点商人可与 LLM 真对话（多轮）
- [x] 延迟符合配置；reset 生效；开场仅一次；未启用商人无变化
- [x] 结构化日志含关键事件（收到 / 路由 / 检索命中数 / LLM 耗时 / 兜底）
- [x] persona 与知识文档已由 Overseer 审改（记录在案）
- [x] LLM 不可用时回"未就绪"提示并记日志，不静默、不刷屏

## Comments

- 2026-10-14（agent 侧完成 + 评审修订闭环）：交付：接线（`Spt/SptChatHistoryProvider`、`Spt/FileTraderChatStateStore`、`Chatbots/TraderChatBot`、`Patches/TraderOpeningPatch`、ConfigRegistration 生产依赖 + 每启用商人一 bot；删除 M1 回显与 seed）+ 试点内容（两商人 persona/开场/reset 关键词 + `knowledge/traders/{54cb…,5935…}/` 各 4 篇草稿）+ 客户端名单配置化（`ClientTraderList`/`ClientSettings` + `Config.Bind`，STD-CFG-006 转 PASS）。测试 97 全绿（主控台复验；85 + 修订 12）。oracle 双轴评审：修订后收口——SHOULD（reset 误删未领取附件）以"选择性清空"落地（保留带附件消息 + 角标重算 + 文案/README 更新）；NIT：开场原子 claim（`TryClaimOpeningAsync` + 并发测试）、空数组语义、大小写不敏感、异常面、日志措辞、杂物均完成；NIT-1 会话串行化与 NIT-3 日志节流留待 M2 实机观察（09）。待办：① Overseer 实机（LLM 真对话 + persona/文档审改）；② 部署刷新（当前 overlay 为旧构建；需关游戏/服务器/MO2 后重装）。
- 2026-10-14（审改 + DS + 部署）：① Overseer 审改通过——人设按"蒸馏知识库主线素材"重写（特征清单见报告），知识文档范围确认 OK（persona 审改项完成）。② LLM 默认切至云端 DeepSeek（2026-10 文档核实：base_url `https://api.deepseek.com`（无 /v1）、模型 `deepseek-flash`；两份示例配置同步更新，key 待 Overseer 填入部署副本）。③ 部署已刷新（文件级覆盖＝等价 reinstall；MCP reinstall 需 live broker，当前不可用）：新 DLL（22:14:48）+ `config/prompts/` + `knowledge/traders/` 已就位，modlist 条目/位置/启用不变。待办仅剩：填入 DeepSeek key → 实机 M2。
- 2026-10-14（首轮实机诊断记录）：两位商人回兜底，日志定位＝`apiKey` 占位符含中文字符（`Request headers must contain only ASCII characters`，被错误处理正确兜住）。链路其余环节实机验证通过：开场消息×2、消息路由（[recv]/[route]）、延迟、兜底发送。后续小项（下一批次）：① `ConfigLoader` 对 apiKey 非 ASCII/占位符在启动时输出中文警告；② `LlmClient` 将该类异常映射为更清晰的失败详情；③ 流程注意——reinstall/文件级覆盖会重置 `config.jsonc`（含用户 key），重装前需备份 deployed config 并部署后回填。
- 2026-10-14（M2 实机通过，收口）：Overseer 完成全部验收（「都测了，没问题」）。日志实证（`spt20261014.log`，14:37 重启后）：Therapist/Peacekeeper 真实 LLM 回复（`[engine] 结果=Reply`，耗时 2.0–3.0s；`[reply]` 91–147 字）；内容类问题检索命中（`[kg] 命中数=1`）；reset 生效（`[reset] 选择性清空：移除 6 条无附件消息，保留 0 条带附件` + 29 字确认文案）；开场仅一次（三次重启无重复）；延迟 2–6s 正常。此前两次失败（占位符 key / 401 无效 key）均被兜底正确接住（兜底路径实机验证）。新发现：中文全角标点无法输入 → 已开新工单 12。收口。
