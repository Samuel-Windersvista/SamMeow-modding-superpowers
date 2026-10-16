# 15: 臂章/近战父类互换修复

**What to build:** 纠正 `ArmbandParentId` / `MeleeWeaponParentId` 常量对调（真值：`5b3f15d486f77432d0509248` = ArmBand 类；`5447e1d04bdc2dff2f8b4567` = Melee 类——来源 mod 原始代码同样互换，默认全开时行为等价，但单开关语义与日志标签错位）。同步 README / `config.json` 注释与测试标签；默认游玩行为不变（两族仍全解锁）。

**Blocked by:** None（可立即开始）

**Status:** ready-for-agent

- [ ] 常量对调纠正（代码 + 注释）；模块日志的「臂章/近战」标签与计数一一对应
- [ ] 相关测试更新（标签/语义断言）；全部测试绿
- [ ] 参考：`spec-v0.2.0.md` D5；DB 实证（5b3f15d4… 子项全为 armband_*，5447e1d0… 子项全为 weapon_*）

## Comments

- 待实现会话登记修改点与测试结果。

### 实施登记（2026-10-16）

**常量对调前后标签-计数映射**（日志标签含义纠正）：

| 开关 | 对调前实际处理族 | 对调后实际处理族（修正） | 对应日志标签 |
|---|---|---|---|
| `lootableItems.armband` | 5447e1d0… = Melee 族 | 5b3f15d4… = ArmBand 族 | 「臂章可掠夺」 |
| `lootableItems.meleeWeapons` | 5b3f15d4… = ArmBand 族 | 5447e1d0… = Melee 族 | 「近战可掠夺」 |

- 默认全开时两族均被处理，变更总数不变（行为等价）；纠正后单开关语义与日志标签-计数一一对应。

**改动文件**：
- `src/InescapableTarkovsSoftcore/Features/SamuelTweaksModule.cs`：对调两常量值 + XML 注释标注「来源 mod 标反，已按 DB 真值纠正」。`ArmbandParentId = 5b3f15d486f77432d0509248`、`MeleeWeaponParentId = 5447e1d04bdc2dff2f8b4567`。规则逻辑（FixArmorConflict / MakeLootable / ResizeMagazines）零改动。
- `tests/.../SamuelTweaksModuleTests.cs`：新增 `ParentConstants_MatchDatabaseTruth`（两个字面值断言防回归）。既有符号化用法（`ParentItem(…, SamuelTweaksModule.ArmbandParentId)`）无需改写，语义随常量自动纠正；`MakeLootable_RespectsPerTypeSwitches`（armband=true/melee=false 只处理臂章族）断言方向核对无误。
- `README.md` 配置样例父类两行注释、`config/default-config.json` 对应两行注释同步为真值。

**测试证据**（仓库根）：
```
dotnet test "mods\InescapableTarkovsSoftcore416\InescapableTarkovsSoftcore.sln" --artifacts-path .artifacts/its-15 -c Debug --nologo -v q
已通过! - 失败: 0，通过: 216，已跳过: 0，总计: 216
```
（基线 215 + 新增 1 项字面值断言 = 216。首轮全量运行曾出现 1 项 `ModLoadHookTests.Loader_IsInjectable_AtPostLoadPlusOne` 失败，经隔离重跑通过，判定为并行 lane 14 编辑器竞态的瞬时状态，与本工单无关。）

**遗留提示（超出本工单所有权，未改）**：`src/InescapableTarkovsSoftcore/Config/ModuleConfigs.cs` 第 32、36 行注释仍引用错误的父类归属（`armband` 注 5447e1d0…、`meleeWeapons` 注 5b3f15d4…），与本工单纠正后的真值相反。该文件不在本工单所有权清单内，建议由编排器另开小工单或扩权修正。

**编排者补充（2026-10-16）**：上述遗留已由编排者在收口时直接修正（`ModuleConfigs.cs` 第 32、36 行注释同步真值：armband → 5b3f15d4…、meleeWeapons → 5447e1d0…）。属低风险两行注释改动，未触碰任何并行 lane 所有权；随本工单闭环。
