# 08: mod 商人接入验证（零代码扩展）

**What to build:** 以一个 mod 商人（新建最小测试商人或使用现有）验证"配置驱动、零代码接入"成立：为其提供 per-trader 配置（traderId、人格、文档目录、开关）后，实机在 Messenger 可与其聊天（≥2 轮）。梳理并记录接入所需的最小配置契约（字段清单与目录约定），写入 README 或 GLOSSARY。

**Blocked by:** 07: 服务端接线 + 试点内容

**Status:** ready-for-agent

- [x] 一个 mod 商人在 Messenger 可聊（≥2 轮）
- [x] 全程仅配置文件变更，无代码改动
- [x] 最小接入契约（字段清单 + 目录约定）写入 README 或 GLOSSARY
- [x] 该商人的原版交易/任务行为无变化

## Comments

- 2026-10-14：Overseer 指定本工单接入验证对象为「油漆匠 Painter」（overlay `[4]新商人-油漆匠-Painter`）。已向实施会话（fix-3）传达该约束。
- 2026-10-14（代码侧收口 + 评审修订闭环）：oracle 双轴评审「修订后收口」（无 BLOCKER）——修订轮已落地：README LLM 前置段按 DeepSeek 默认重写（含占位 key→401→兜底说明）、ModConfig 默认分叉注释、ADR-0002 状态注记、同步守卫集合断言、升级 nuance、路径措辞拆分、GLOSSARY/README/config README 订正、Painter 补至 4 篇草稿。测试 105 全绿（主控台复验）。部署侧前置已由主控台执行：① 运行时名单 JSON 加 Painter id；② overlay 刷新保留 DS key（注入核验通过）。待办：Overseer 实机验证（≥2 轮 + 原版交易/任务无变化观察；与工单 12 复验合并）。
- 2026-10-14（实机验证通过，收口）：日志实证（15:19 会话，本地 23:19，3/3 商人）——`[opening] 商人=Painter 已发送开场消息`；Painter ≥2 轮真对话（"我就是路过的……"→Reply 82 字；"你都有啥？"→Reply 116 字；耗时约 2.4–2.8s）；整句 `重置` → `[reset] 选择性清空：移除 9 条无附件消息` + 确认文案；客户端名单含 Painter（`输入解锁=True；可聊天商人=…,668aaff35fd574b6dcc4a686`）。原版交易/任务行为按 Overseer 实测确认（"没问题"）。**零代码接入验证成立**（服务端 config + 客户端名单两处文本改动，无重编译）。
