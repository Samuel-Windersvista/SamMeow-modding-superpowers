# knowledge —— 商人知识文档

本目录存放聊天引擎检索用的知识文档，随 mod 部署在
`SPT_Runtime/user/mods/SamMeow.LLMTraderChat/knowledge/`。

约定：

- 按商人分子目录：`knowledge/traders/<traderId>/`，与 per-trader 配置的 `docsDir` 对应。
- 支持 `.md` / `.txt`；检索实现按标题/段落分块，top-k 可配（`knowledgeTopK`）。
- 每商人建议 3–6 个主题文件（背景 / 立场 / 业务范围 / 常见问答），依据官方 lore，来源标注于文末。
- v1 试点商人：Therapist 与 Peacekeeper（`knowledge/traders/54cb57776803fa99248b456e/`、
  `knowledge/traders/5935c25fb3acc3127c3d8cd9/`）；工单 08 接入的现成 mod 商人 Painter
  （`knowledge/traders/668aaff35fd574b6dcc4a686/`）——内容均为 agent 起草草案，待 Overseer 审改。

详见 `.scratch/llm-trader-chat/spec.md` 的 Implementation Decisions #6 / #12。
