# 05: 维度 ⑨-⑩ 规则：PKG / VERIFY

**What to build:** 撰写 `09-packaging.md`（打包与发布：MO2 overlay 布局 `SPT_Runtime/user/mods/<Name>/` 与 `BepInEx/plugins/<Name>/`、meta.ini `comments`/`notes` 约定、paired 单 zip 双端、服务端 mod 目录唯一 `IModMetadata`、README/LICENSE 必备）与 `10-verification.md`（验证流程：build → 本地 server 冒烟 → 日志断言；引用既有工具链：MO2 安装/备注工具、server 日志解析、tarkov-runtime 快照断言）。格式要求同 ticket 02。

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] 两个维度文件完成，规则五要素齐全
- [ ] PKG 覆盖 paired 单 zip 与"唯一 IModMetadata"机制约束（引用 gap-investigation 证据）
- [ ] VERIFY 与现有工具链衔接（可被 agent 实际执行）
- [ ] index.json 登记本票文件
