# 03: 快照 sections 补全——traders / quests / hideout / inventory

**What to build:** 快照覆盖面补全，支撑商人/任务/藏身�?物品�?mod 的生效断言。新增四�?section：`traders`（好�?等级/assort 计数）、`quests`（可�?进行/完成计数）、`hideout`（区域等级）、`inventory`（物品计数摘要，非全量枚举）。sections 可选参数支持任意组合，一次调用原子返回�?

**Blocked by:** 02（快照组装器�?schema�?

**Status:** ready-for-human

- [ ] 四个�?section 各自返回符合 schema 的计数型摘要
- [ ] `sections` 参数支持多选组合，单调用原子返回全部所�?section
- [ ] 每个 section �?fixture 测试覆盖正常与空数据（如新档无藏身处升级）两种情�?
- [ ] 快照确定性不被新 section 破坏
