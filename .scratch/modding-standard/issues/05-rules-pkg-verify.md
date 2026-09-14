# 05: 维度 ⑨-⑩ 规则：PKG / VERIFY

**What to build:** 撰写 `09-packaging.md`（打包与发布：MO2 overlay 布局 `SPT_Runtime/user/mods/<Name>/` 与 `BepInEx/plugins/<Name>/`、meta.ini `comments`/`notes` 约定、paired 单 zip 双端、服务端 mod 目录唯一 `IModMetadata`、README/LICENSE 必备）与 `10-verification.md`（验证流程：build → 本地 server 冒烟 → 日志断言；引用既有工具链：MO2 安装/备注工具、server 日志解析、tarkov-runtime 快照断言）。格式要求同 ticket 02。

**Blocked by:** 01

**Status:** done

- [x] 两个维度文件完成，规则五要素齐全
- [x] PKG 覆盖 paired 单 zip 与"唯一 IModMetadata"机制约束（引用 gap-investigation 证据）
- [x] VERIFY 与现有工具链衔接（可被 agent 实际执行）
- [x] index.json 登记本票文件

## Comments

**2026-09-14 完成（agent）**

- 交付：`09-packaging.md`（STD-PKG-001..007）、`10-verification.md`（STD-VERIFY-001..009），共 16 条规则。
- 双轴评审后修复：PKG-001/003 去外推（12 例抽样口径）；PKG-004/005/006 交叉引用；12 条无语料规则补 `EV-NOCORPUS`；16 条「深入」链接化（23 个链接 Test-Path 全过）。
- 机械核验：五要素齐全、ID 唯一、链接与锚点全部可解析。
