# 03: 配置装载层（全局 + per-trader）

**What to build:** 可实现、可测试的配置层：全局配置（LLM 字段、延迟范围、历史注入条数、兜底话术、语言模板路径）与 per-trader 配置（traderId、enabled、persona、文档目录、开场消息、reset 关键词）。行为：合理的默认值；非法 JSON 或缺失必填字段时仅禁用对应商人并输出可诊断的响亮日志（指明文件与字段），不影响其他商人；随包分发示例配置（全局 + 一个商人样例）。

**Blocked by:** 01: 项目脚手架

**Status:** ready-for-agent

- [x] 单测覆盖：默认值、缺字段、非法 JSON、启用/禁用开关各路径
- [x] 非法配置仅影响对应商人；错误信息可诊断（文件与字段）
- [x] 示例配置随包分发（全局 + 商人样例）
- [x] 单测运行不依赖网络与游戏安装

## Comments

- 2026-10-14：工单收口。交付：`src/Server/Config/*`（ModConfig / ConfigLoadResult / ConfigLoader 纯函数 Parse / ConfigRegistration `IOnDIConstruct`）+ `config/{config.jsonc,defaultConfig.jsonc,README.md}`；ModEntry 启动逐条告警 + 配置摘要。测试：ConfigLoaderTests 13 条（含修订轮新增：非法 endpoint 视同未配置、非字符串 traderId 单告警）。oracle 双轴评审：修订后收口（无 BLOCKER）；修订轮已落地（endpoint 绝对 URI 校验、traderId 单告警、config/README 补 MO2 overwrite 说明）。check-mod-standard：STD-CFG-002/003/004/005 PASS（CFG-006 属客户端工单；BUILD-001/006、META-003 系检查器缺陷，发布门禁前修检查器或 waiver）。未提交。
