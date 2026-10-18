# 03: 依赖证据 + `STD-DEP-001` 升 MUST

**What to build:** 纠正“`ModDependencies` 无语料先例”的旧结论，并把依赖声明规则升级为 MUST。新的语料证据是 53 个仓库 / 82 对非空依赖，其中 `com.wtt.commonlib` 49 次、`com.wtt.contentbackport` 11 次、`com.morebotsapi.tacticaltoaster` 4 次、`com.epicrangetime.aio` 3 次、`me.sol.sain` 3 次。

**Blocked by:** 02（证据基线刷新）

**Status:** ready-for-agent

- [ ] `EV-GAP-DEP` 删除“0 个真实非空依赖”，改为 53 仓库 / 82 对，并列出主要依赖与两种写法。
- [ ] `EV-NOCORPUS` 删除 `ModDependencies` 第 1 条；`STD-DEP-001` 不再引用 `EV-NOCORPUS #1`。
- [ ] `STD-DEP-001` 从 SHOULD 升为 MUST，Evidence 同时含机制与语料；规则文本保持 key=ModGuid / value=SemVer Range。
- [ ] 依赖章节补充“共享库依赖是主导场景，范围必须与目标运行时实测”。
- [ ] `rules.json` 的 Level 与 prose 逐字一致；`STD-DEP-002..005` 不变。
- [ ] `validate-mod-standard.ps1` exit 0。

**执行说明：** 本票只动依赖证据与 DEP-001；不新增依赖规则。依据 spec 的 D2。
