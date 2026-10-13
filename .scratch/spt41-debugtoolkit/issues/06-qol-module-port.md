# 06: 便利组（QOL）移植（DadGamer QOL 全组）

**Spec:** `.scratch/spt41-debugtoolkit/spec.md`

**What to build:** 端到端编译级可用的 QOL 模块（游戏内验证随 08）：无限体力（身体体力+手部体力+氧气）、无限能量、无限水分、瞬产（制造即刻完成）、瞬建（藏身处升级即刻完成）、换弹/卸弹速度（开关 + 两倍率，关闭恢复默认）、对敌伤害倍率（1-20，默认 1；并入 05 的 `ApplyDamage` 补丁文件）、物品总重百分比（0-100，默认 100，战局前设置）；`6. 便利` 配置节（10 键）；瞬产/瞬建目标按 02 检出结果定位实现或降级；全部补丁体 fail-open。

**Blocked by:** 05（共用 `ApplyDamage` 补丁文件，必须串行）

**Status:** ready-for-agent

- [x] 10 键按 spec 表实现（默认值：Reload 0.85 / Unload 0.3 / 敌伤倍率 1 / 负重 100）
- [x] 瞬产/瞬建按 02 检出的 0.16.9 目标实现，或按降级预案记录并禁用单项
- [x] 换弹卸速关闭时恢复默认；负重为「减少物品总重百分比」语义
- [x] 与 05 共用的伤害补丁文件零回归（复跑 05 验收项）
- [x] 补丁体 fail-open；构建 0 error；标准检查通过

**上游源码（本仓归档）:** `knowledge/spt-kb/archive/forge/mods/DadGamerMode_686_source/`（Features 与 Patches 全组；v1.9.3）。

## Comments

- 2026-10-11（编排者）: 前置就绪——触点表 `docs/api-touchpoints/dadgamer.md` 已完成（本节所需：§1.4–1.10 映射 + §3 降级预案 + §4.2 瞬建片段）；`ApplyDamagePatch.cs`（05 产物）已留敌伤倍率插入点。本单文件域 `src/Modules/QoL/**` + `ApplyDamagePatch.cs` 一处定点编辑。施工（fix-4 续用）。
- 2026-10-11（编排者）: **完成**（fix-4 施工 + 主控台复核）。
  - 交付：`src/Modules/QoL/**` 7 文件（QoLSettings 10 键、QoLRaidComponent、4 支补丁、模块入口）+ `ApplyDamagePatch.cs` 定点编辑（敌伤倍率仅非本玩家分支；`Ready` 守卫移位、本玩家分支逐行不变）。
  - 复核（编排者复跑）：`dotnet build -c Release` 0 warning / 0 error；标准检查 PASS=13 / FAIL=0 / WAIVED=1；10 键键名/默认值/范围/中文显示名对表通过。
  - 偏差/决策 8 条随单记录（瞬建双路径覆盖：`InitConstructing` + `WaitUpgradeTime`（0.16.9 拆分）；瞬建不提前调后端 Complete；Task 短路置 `Task.CompletedTask`；瞬产用 EndTime 稳健点；负重 100 时零改写；换弹还原范围（Disable 不复位配置值——编排者认可：避免退出游戏抹用户设置）；敌伤倍率守卫独立；挂载形态；供 07 汇总）。
  - 瞬产/瞬建/能量水分行为待 S3（08）游戏内确认（观察点已在回报列明）。
  - 模块工单全部完成 → 进入统一双轴代码审查（Standards/Spec），修订回填各单。
- 2026-10-11（编排者）: **双轴审查闭环（Round 1 → 修订轮 → 复验）**。本单相关：`QoLModule` 迁移共享 `PatchSet`（Disable 无条件清理 + 换弹还原保留）；`QoLSettings` 迁移 `ConfigDescriptions` 工厂；瞬建边界语义（零时长/AutoUpgrade 停在 ReadyToInstall；`alreadyUnderConstructing` 直接完成）按审查结论**不改码**，入 S3 清单与 07 行为注记。复验：构建 0 warning/0 error、标准检查 PASS=13/FAIL=0/WAIVED=1。记录：`reviews/round1-*.md` 三件。
