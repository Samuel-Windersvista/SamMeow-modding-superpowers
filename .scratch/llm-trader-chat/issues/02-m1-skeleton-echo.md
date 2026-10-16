# 02: M1 骨架 —— 输入解锁 + 服务端回显（端到端）

**What to build:** 打通端到端最小链路并裁定三个未知。客户端：解锁一个试点商人（先 Therapist）消息线程的输入（Harmony 补丁输入闸门）。服务端：以最小接线接住玩家发往该商人的消息，回显固定文本并以商人身份落回同一线程。三个未知需要实机回答并记录：① 输入解锁是否对商人消息线程生效；② 玩家消息的路由归属（注册 chatbot 形态 vs 在对话收口处拦截）；③ 回复落回同一线程的可行形态。结论记录在本工单评论区与 dev-log，供工单 07 采用。

**Blocked by:** 01: 项目脚手架

**Status:** ready-for-agent

- [x] 实机：在商人消息线程输入文字并发送成功（不被闸门拦截）
- [x] 服务端收到消息（结构化日志可见），回显以商人消息落回同一线程
- [x] 三个未知的答案、所选接入路线与证据（日志片段/签名）记录在案
- [x] 未启用的其他商人行为无任何变化
- [x] 服务器正常启停、无崩溃；模块加载日志可辨识

## Comments

- 2026-10-14（agent 侧交付，待实机）：客户端＝`CanSendMessage` Postfix（仅目标商人对话题翻真，目标收敛为 Shared 常量 `M1TargetTraderId`）；服务端＝路线 A（`[Injectable] IDialogueChatBot`，Id＝Therapist `54cb57776803fa99248b456e`；回显经 `MailSendService.SendDirectNpcMessageToPlayer`）+ 缺失对话一次性补发（`GenerateDialogueList` 前缀，编译期开关）。源码层定性：② 路由归属（`EnabledBots` 门禁不影响发信路由，且有意不开启以免商人被塞进好友列表）；③ 同线程落回（dialog id == trader id）；① 输入解锁待实机。主控台复验：构建 0W0E、测试 5/5、zip 刷新（12 条目基线，20,065 bytes）。部署方式：zip 解压至 `SPT_41x` 根（MO2 实例路径失效时的备选，已实查）。待 Overseer 实机验证后收口。
- 2026-10-14（部署完成，自动）：已部署至《永远的诺文斯克》MO2 实例（数据目录 `E:\Game\EFT_Offline\Norvinsk_Forever`，game_path=SPT_41x）——overlay `[9]全局功能大修-商人LLM对话-llm-trader-chat-test-build`（priority 81，[9] 组内，enabled=true；内容 = `BepInEx/plugins/{Client,Shared}.dll` + `SPT_Runtime/user/mods/{Server,Shared}.dll` + `config/` + `knowledge/`；meta.ini 已记 comments/notes）。profile 备份 `pre-llmtc-m1`（`.mo2-mcp\profile-backups\Default_pre-llmtc-m1`，5 文件）；安装快照 `adaaeddd`、位置快照 `0048ebe7`。待 Overseer 按验证表实机回报后收口。
- 2026-10-14（实机验证通过，收口）：Overseer 实机在 Therapist 对话发送「喂」，全链路日志实证（本地时间 20:51:27，`overwrite\SPT_Runtime\user\logs\spt\spt20261014.log`）：`/client/mail/msg/send` → `[LLMTC][recv] 路由=IDialogueChatBot:Therapist text="喂"` → `[LLMTC][reply] 已回显 14 字`；Overseer 目视确认同一对话出现 `[M1-echo] 收到：喂`（无需额外刷新补丁）。三个未知结论：① 输入解锁生效；② 路线 A（bot 注册）实机成立，`EnabledBots` 门禁确无影响（有意不开启）；③ 同线程回显成立。旁证：Prapor（未启用）无输入框、原版行为（Overseer 确认）；两端日志无本 mod 报错；`[LLMTC][config]` 行正常（DI 钩子生效）；seed 补发未触发（该档案已存在商人对话题）。结论供工单 07 采用：客户端＝仅 `CanSendMessage` 解锁一处补丁；服务端＝沿用路线 A。
