# 12: 聊天输入中文标点修复（M2 实测发现）

**What to build:** 修复 M2 实测发现：在已解锁的商人消息对话中输入**中文全角标点**（，。！？、：；「」等）会被输入框过滤、无法输入（中文汉字可正常输入）。定位输入过滤点（客户端聊天输入组件；嫌疑：字符校验白名单只放行字母/数字/空白，如 `\p{L}\p{N}\s`——汉字属 `\p{L}` 可通过、全角标点属 `\p{P}` 被拦），以最小 Harmony 补丁放行全角标点。作用范围限定为"本 mod 已解锁的商人对话"（复用 `ClientSettings.ShouldUnlock` 判定），不改变其他界面原版行为。修复后由 Overseer 在下一轮测试中实机复验。

**Blocked by:** 无（可直接开工）

**Status:** ready-for-agent

- [x] 定位过滤点（给出类/方法/行为证据）
- [x] 全角标点可在解锁的商人对话中输入并发送（代码侧证据；实机复验留待下一测试轮）
- [x] 未解锁对话/其他输入界面行为不变（范围限定说明）
- [x] 构建 0 警告 0 错误、测试全绿、打包刷新

## Comments

- 2026-10-14（代码侧收口）：实现完成 + oracle 双轴评审「可收口（代码侧）」（无 BLOCKER/SHOULD；独立反编译复核挂钩点/调用链/引用链全部成立；测试 103 全绿；zip 已刷新，哈希与构建产物一致）。
- 证据链归档（NIT-4）：过滤点＝`EFT.UI.ValidationInputField.ValidationError`——白名单 `^[<pairs>]+$` 不含全角标点；CJK 只能经 `\w`（`AnyWordChars`）通过，而汉字实测可输入 ⇒ 白名单/剥离器处于活跃态 ⇒ 全角标点必被 `_replacer` 剥离（三段论闭环；反编译参考 `.artifacts/llm-trader-chat-ilspy/t-EFT_UI_Chat_ChatScreen.utf8.cs` 等）。挂钩签名独立复核：`ChatScreen.Show`(ChatScreen.cs:226) / `ChatScreen.OnSelectedDialogChanged`(:284) / `_chatInputField`（`[SerializeField] public`，:145）；`ValidNickname` 跳过语义经调用方链复核与原版"合法路径"同效。
- TMP 层残留预案（NIT-1；仅当下一轮实机发现标点仍被剥时启用）：先读 `_chatInputField.characterValidation`/`onValidateInput` 取证；备选修法＝在 Show/选中切换时对该实例置 `characterValidation = None`、`onValidateInput = null`（实例级、受解锁名单门控，约 5 行）。
- 可选项登记（非阻塞）：NIT-2 输入框文本跨对话保留的窄外溢（切到未解锁对话时以 `ValidateCurrentInput()` 复验硬化，可选）；NIT-3 `PatchAll()` 全有或全无（按类隔离可选）；NIT-5 `ChatPunctuationPatch.cs:1` 疑未用 using + `Directory.Build.props:101` 注释措辞（改"基类链含"）。以上随下次代码触点顺手处理。
- 待办：Overseer 实机复验（下一轮测试：对启用商人输入含全角标点句子 → 可输入并发送；未解锁对话保持剥离）。
- 2026-10-14（实机复验通过，收口）：Overseer 实测——全角标点随消息完整进入服务端：`[recv] text="我就是路过的……"`、`[recv] text="你都有啥？"`（均为启用的商人对话，本地时间 23:21）；对照项（未解锁对话仍被剥离）未单独实测，按补丁作用域设计 + oracle 代码侧结论维持（如需严格留证可下次顺手补测）。
