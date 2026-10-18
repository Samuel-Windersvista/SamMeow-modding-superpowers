# 04: 交互并入与菜单合并

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 把 DO 的门交互全套并入：Peek（点按快速开缝 / 按住逐步开到全开）、Open Further、Open Quietly（幅度/缓速/音量可配）、体推门与静音开门音效、踢门持枪（就绪/朝向两开关）、交互键捕获与部分开合动画相关补丁；交互菜单从多补丁收敛为**单点注入并去重**——Peek / Open Further / Open Quietly 与 Plant Explosive 同菜单共存、不重复、不冲突。游戏内可演示：门交互菜单动作齐全且无重复，逐项可用（含锁定状态表现）。

**Blocked by:** 03（破坏并入与上锁门豁免移除）

**Status:** ready-for-agent

- [ ] 交互动作全套可用：Peek（点按/按住语义）、Open Further、Open Quietly（静开参数）、体推门、踢门持枪
- [ ] 菜单单点注入 + 去重守卫：动作不重复；Plant Explosive 与其他动作共存
- [ ] 锁定门状态的菜单表现符合预期（无非法动作）
- [ ] 与门状态随机化（包内覆盖层）共存初检：菜单不崩、行为合理
- [ ] 日志无异常；进出战局清理正常
- [ ] DO 交互侧参数（静开幅度/缓速/音量、推门强度、踢枪开关）暂按上游默认运行（旋钮化在 05）

## Comments
