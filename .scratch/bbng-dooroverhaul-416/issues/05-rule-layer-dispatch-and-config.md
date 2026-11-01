# 05: 规则层、统一命中分派与配置面

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 把破门判定链演进为**纯函数规则层（Attack Policy）**：输入＝武器/弹药模板数据、材质、命中点相对锁锚、门类、门状态、配置快照；输出＝锁损伤量、破坏是否受理、拒绝原因。以**单一命中分派点**驱动双轨（收敛上游 BB postfix 与 DO 事件订阅两个独立钩子）。BB 与 DO 的全部既有旋钮迁入 7 节 F12 配置面（`1.通用` / `2.破门` / `3.破坏` / `4.交互` / `5.爆炸` / `6.排除与开关` / `7.调试`；键名英文、显示名中文；列表型配置以明确可编辑载体承载并文档化）；结构数据驱动、为更细粒度规则组预留扩展位。落地 S0 单元测试接缝（表驱动，覆盖规则层全部判定维度）。

**Blocked by:** 04（交互并入与菜单合并）

**Status:** ready-for-human

- [x] 规则层为纯函数模块，表驱动单测通过（武器类别/弹药白名单/材质规则/锚定命中/门类/模式开关/血量区间）——`src/Rules/`，40 用例全绿
- [x] 单一命中分派点生效（原双钩子收敛为一处观察喂两线）——`ApplyHit` postfix → `AttackPolicyAdapter` → `AttackPolicy`，同时产出锁损伤与破坏受理
- [x] 全旋钮 F12 配置面：BB+DO 全部设置可调，持久化/重载正常，默认值＝宽松路线（黑名单空、容器/后备箱可破、血量 100–200、白名单沿用上游默认）——7 节已齐；06 的新增高危组开关不在本单
- [x] 列表型配置载体文档化（增删武器/弹药/黑名单条目的方法）——README「列表型配置载体」
- [x] `dotnet build` + 单测全绿；游戏内抽查旋钮行为（白名单、血量、模式开关各至少一项）——2026-10-23 Overseer 实测通过；日志 17 补丁、无 DoorBreach 异常
- [x] 无配置迁移器（新身份全新配置面）；扩展位在结构注释中体现——`RuleConfig` / `AttackInput` / `AttackDecision` 注释

## Comments

- 2026-10-18（前置输入，来自工单 03 实测）：Pleb/SemiPleb 的锁血池锚定语义已按「放行武器/弹药/材质/黑名单，但保留锁区（门把手 0.25m）锚定」实现（见 README 偏差表第 13 行；`DamageUtility.IsValidDoorLockHit` 已修正坐标系）。规则层（本单）需将其正式化为纯函数输入（模式 + 锚定判定 → 锁损伤/破坏受理/拒绝原因），并评估是否暴露「全放行是否忽略锚定」旋钮（默认关闭）。
- 2026-10-20（工单 05 代码侧完成）：
  - **纯规则层**：新增 `src/Rules/AttackTypes.cs`（`AttackInput` / `AttackDecision` / `RuleConfig` / 枚举）与 `AttackPolicy.cs`（纯函数，无 Unity/EFT 依赖）。输入＝武器/弹药事实、材质、锁锚、门类/门状态、配置快照；输出＝锁损伤、破坏受理、拒绝原因。`RuleConfig` 预留按材质/门类/命中区扩展规则组的位置。
  - **运行时适配**：新增 `src/AttackPolicyAdapter.cs`，只做 `DamageInfo` / `DoorEntry` → 纯输入映射；`Settings.BuildRuleConfig(ModConfig)` 在插件启动 + 每个战局启动构造 `DoorBreachPlugin.RuleConfig`（列表转 `HashSet`）。
  - **单一命中分派**：`ApplyHit` postfix 的 `DispatchDestruction` 先评估破坏受理，再调用 `DoorDamageIntake.OnHit`；锁血池三个 handler 统一调用 `AttackPolicyAdapter.EvaluateLock` 并按 `LockDamage` 扣血。`DamageUtility` 收敛为事实提取（锚点/弹药分类/武器类别），不再做受理决策。
  - **行为保持**（上游特例已用测试锁定）：白名单内非霰弹武器跳过弹药判定；后备箱 500 仅霰弹第二分支触发；Pleb 门要求锁锚、容器/后备箱任意命中；Pleb 绕过黑名单与容器/后备箱开关；SemiPleb 金属门回落常规规则；近战不校验锁锚；破门弹对金属门受开关限制。
  - **测试**：`tests/SamMeow.DoorBreachOverhaul.Tests`（xUnit/net10.0，只编译 `src/Rules/`）；46 个表驱动用例全绿。主工程 `<Compile Remove="tests\**\*.cs" />`。
  - **验证**：`dotnet build -c Release -t:Rebuild` 0 warning / 0 error；标准检查 PASS=12 / FAIL=0 / WAIVED=2；单测 40/40。
  - **待 Overseer 游戏内抽查**：白名单武器、血量区间、Pleb/SemiPleb 模式各至少一项；确认上锁门破门与破坏双轨仍正常。
- 2026-10-20（@oracle 复审 PASS WITH CHANGES，Must/Should fix 已修）：
  - **Must Fix 1**：`RuleConfig.WhitelistedRounds` 真正参与判定（移除 `AmmoFacts.IsWhitelisted`，纯层按 `TemplateId` 查配置）；新增“清空白名单后霰弹 whitelisted 弹被拒”回归用例，消除 S0 假阳性。
  - **Must Fix 2**：`TryGet*Anchor` 失败时 fail-closed（`HasLockAnchor=true` / `IsLockAnchorHit=false`），不再当作无把手门放行。
  - **Must Fix 3**：`BuildAmmo` 改 `ItemTemplates.TryGetValue`；且仅锁血池评估（`entry == null`）构建，破坏派发不再因未知 `SourceId` 抛 `KeyNotFoundException`。
  - **Should Fix**：`DestructionEnabled` 在破坏评估时实时同步（与“3. 破坏实时读值”一致）；`DoorBreachComponent` 校验并归一化血量区间；`DamageUtility` 缺 `Lock`/`CarLock_Hand` 子物体时打 Debug 日志；README 修正“SemiPleb 仍受黑名单限制”。
  - **补测**：SemiPleb+黑名单 / SemiPleb+锚点失败 / SemiPleb+后备箱 / 破片弹 / 双轨独立性 / 白名单清空；单测 **46/46**；build 0 warning / 0 error；标准检查 PASS=12 / FAIL=0 / WAIVED=2。
  - 待 Overseer：部署新 DLL 后抽查白名单、血量区间、Pleb/SemiPleb 模式各一项，并确认破坏/交互无回归。
  - 部署（2026-10-20）：MO2 覆盖层 DLL 已更新为 `168,960 B` / SHA256 `A33D6E21325E7F4EA4BC7E7115DA5ECE6E9999AE70A21794CF022E8C49628CD1`；上一版（工单 04，`F6F74395…`，164,352 B）备份在 `.artifacts\doorbreach-ticket05-backup\DoorBreachOverhaul.pre-ticket05.dll`。
- 2026-10-23（Overseer 抽查反馈 / 日志分析）：
  - 现象：Overseer 用**新增到白名单**的喷子 `6761b213607f9a6f79017d23` + 新弹药 `5d6e6806a4b936088465b17e` 打锁，门仍会开。
  - 日志：本局快照 `Pleb=False, SemiPleb=False, 霰弹=12, 弹药=4, 耐久=200-400`；出现 15 条 `[Patches] 对 Door 施加命中伤害 49.37…` → **锁血池受理，非 Pleb 漏开**。
  - 弹药解析：`5d6e6806…` = `patron_12x70_buckshot_85`（12/70 8.5mm 鹿弹，8 弹丸、单颗 50 伤害）；49.37 与单颗伤害吻合，200–400 耐久约 2 发鹿弹即开 → **白名单生效，属预期行为**。
  - 已用 SPT `/client/items` 运行时物品库核实：`6761b213607f9a6f79017d23` = `[WTT-Armory]_(Serbu Super Shorty 12ga shotgun)`，`weapClass="shotgun"`、`ammoCaliber="Caliber12g"`。→ 走的是“霰弹枪 + 弹药白名单”第二分支，不是“非霰弹跳过弹药”分支。
  - 结论：新增霰弹与 12/70 8.5mm 鹿弹都在白名单，开门为**预期行为**，不是规则层漏判。若只想让 slug/破门弹开门，从 `Whitelisted Rounds` 移除 `5d6e6806a4b936088465b17e` 即可；负向测试请用不在白名单的 12/70 弹（同枪），锁血池应不受理，门板仍可能被打孔。
- 2026-10-23（最终验证，Overseer 实测通过）：
  - 启动日志：17 个 DoorBreach 补丁全部 `Enabled patch`；无 `Failed to create patch`；无 DoorBreach 异常。仅包内 `Fontaine-SPTRealismCommon-Lib` 的既有 `ArgumentOutOfRangeException`（与本 mod 无关）。
  - 配置快照：`Pleb=False / SemiPleb=False / 耐久 200-400 / 霰弹 12 / 弹药 4`（含新增 Serbu Super Shorty + 12/70 8.5mm 鹿弹）；白名单正/负向测试均通过。
  - 构建：`dotnet build -c Release` 0 warning / 0 error；S0 单测 **46/46**；标准检查 PASS=12 / FAIL=0 / WAIVED=2。
  - 部署：覆盖层 DLL `168,960 B` / SHA256 `A33D6E21325E7F4EA4BC7E7115DA5ECE6E9999AE70A21794CF022E8C49628CD1`，与 `bin/Release` / `release` 一致；工单 04 版备份保留。
  - 结论：工单 05 全部 checkbox 完成；剩余仅为工单 04 KeyCard 单项与工单 08 C4 运行时（独立项）。
