# SamMeow-DoorBreachOverhaul：以 BB-NG master 为基座合并 Door Overhaul 能力、自持独立演进

SPT 4.1.6 需要"Backdoor Bandit 破门 + Sage's Door Overhaul 破坏与交互"合一的单 mod，且上锁门不豁免破坏。上游现状：BB-NG（MIT）master 提交 `451c6cb` 已含 4.1 API 迁移但未发版；Door Overhaul（BSD-3）为 4.1.6 现役 v1.0.0。裁决：以 BB-NG master 为合并基座，并入 Door Overhaul 的破坏与交互能力（移除其锁门豁免），工程落 `mods/SamMeow-DoorBreachOverhaul`；身份新起：AssemblyName `DoorBreachOverhaul`、BepInPlugin GUID `com.sammeow.doorbreach`；自持维护、独立演进（上游仅作参考与对账来源）。

## Considered Options

- 等上游发 4.1 release 再以其为基座合并：时间不可控；且独立演进目标下上游只作参考。
- 双 mod 共存（BB-NG 移植 + DO 小改并存）：工期最短，但交互菜单与命中链交叠不可控，与"单 mod"诉求不符。

## Consequences

- 上游演进需人工对账，无自动同步；
- 双许可（MIT × BSD-3）署名与 NOTICE 义务，README 注明双上游与贡献者；
- C4 的 4.1 服务端组件为独立工单，与客户端主线解耦。
