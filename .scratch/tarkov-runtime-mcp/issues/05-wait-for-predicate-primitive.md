# 05: tarkov_wait_for 谓词原语

**What to build:** 测试自动化的核心同步手段。实�?`tarkov_wait_for`：对任意工具结果求值谓词（�?`server_status.mods contains X`、`server_status.version matches Y`），轮询直到满足或超时；超时返回结构�?`WAIT_TIMEOUT`（含谓词、最后观察值、已耗时）。轮询间隔与超时上限可配置，�?sane default。谓词语法保持最小（contains / equals / matches / 数值比较），不做通用表达式语言�?

**Blocked by:** 01（穿甲弹：工具层与连接握手）

**Status:** ready-for-human

- [ ] 谓词满足时立即返回成功（含求值结果与耗时�?
- [ ] 超时返回结构�?`WAIT_TIMEOUT`（谓�?最后观察�?耗时�?
- [ ] 谓词�?`server_status` 结果可用；语法非法时返回结构化错误而非异常
- [ ] 测试�?fake connection 模拟"�?N 次不满足、第 N+1 次满�?序列，验证轮询行�?
- [ ] 谓词实现是通用的（不绑定特定工具），后�?snapshot 就位后天然适用
