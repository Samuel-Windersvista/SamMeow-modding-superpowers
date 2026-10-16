# ADR-0001：游戏内聊天载体复用原生 Messenger，不自建面板

- 状态：已接受（2026-10-14）
- 决策人：Overseer（用户）

## 背景

本 mod 要在游戏内承载「玩家与商人文字对话」。SPT 原生 Messenger 已内置聊天通道（SPT Friend、Commando）：玩家输入经 `SendMessageRequest{dialogId,type,text,replyTo}` 送达服务端，回复经 `MailSendService` 落回同一消息线程；官方示例 `20CustomChatBot` 证明该管线是公开扩展点。候选载体有三条：复用原生 Messenger；VisitAPI 式在商人屏运行时注入对话 UI；WebOverlay（WebView2 HTML 叠加）自建面板。生态先例 Miyako Carry Service 与 Trash Talk 均选择复用原生 Messenger（研究依据 `docs/research/llm-trader-chat-feasibility.md` §4.1/§4.2）。

## 决策

复用原生 Messenger 作为唯一聊天载体。客户端只做一小步接线——对 `UpdatableChatDialogue.CanSendMessage` 一处 Postfix 解锁目标对话的输入闸门，不新建任何 UI；消息发送沿用原生链路（路由 `/client/mail/msg/send`）；服务端接话后用 `MailSendService` 回投到同一线程。

## 备选方案

1. VisitAPI 式商人屏注入 —— 否决：需自造对话 UI 与 `.dlg` 脚本、补丁面大，且与原生体验割裂。
2. WebOverlay 自建面板 —— 否决：工作量大，输入/光标/独占全屏有已知坑（WebOverlay 文档自述），生态先例均未采用。

## 后果

- 正面：UI 面近乎为零，复用已核验的客户端触点与服务端消息持久化；与 Miyako/Trash Talk 既有实践对齐，风险最低。
- 负面：受原生 Messenger 的可见性模型约束——任意第三方 mod 商人是否自动获得可点击聊天入口需在 M1 骨架实测裁定；若「并入商人在现有消息线程」不可行，则回退「独立联系人」形态（同为原生载体）。
