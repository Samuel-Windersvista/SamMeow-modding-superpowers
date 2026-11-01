# 06: 排除开关体系

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 把 DO 的门适用性排除重构为**配置化策略**并接入规则层：安全类（钥匙卡门/滑门/开关门/成对双门/非可操作）由**总开关**一键解除；**高危组**（触发任务门/撤离门）为独立子开关、默认保持排除、带警示文案；物理不可行项（无弹道碰撞体/无几何/异常尺寸）不可开关化，保留排除并在调试面板显示原因。单测覆盖排除策略判定（关闭态回归 + 解除态行为）。

**Blocked by:** 05（规则层、统一命中分派与配置面）

**Status:** ready-for-human

- [ ] 总开关解除安全类排除：钥匙卡门/滑门/开关门/双门在开启后可被破坏与破门（含交互语义检查）
- [ ] 高危组子开关独立、默认关、警示文案可见；开启后触发任务门/撤离门参与破坏
- [ ] 物理不可行项保留排除 + 调试面板显示原因（逐类可读）
- [x] 单测扩展：排除策略用例通过（含默认关闭语义）
- [ ] 游戏内逐开关验证（含关闭态回归；关注任务脚本与撤离交互未被误伤）
- [ ] 边界/偏差记录（被解除门类的异常表现）

## Comments

- 2026-10-24（工单 06 代码侧完成，待部署与实机验证）：
  - **路线 A（纯函数共享）**：新增 `src/Rules/ExclusionPolicy.cs`，输入 `DoorClassKind` + `RuleConfig`，输出 `Allowed / Category / Reason`。`DoorDestructionRegistry` 与 `AttackPolicy.EvaluateDestruction` 共用该函数，注册表不再单独维护一套字符串排除逻辑。
  - **门类扩展**：`DoorClassKind` 增 `NonOperatable` / `Double` / `Trigger` / `Extraction`；新增 `ExclusionCategory`（`None / Fika / Safety / HighRisk / Physical`）。`Excluded` 由注册表在物理不可行时回写，规则层同源拒绝。
  - **注册表判定顺序**：Fika 环境硬排除 → 物理不可行（无弹道碰撞体 / 材质无档案 / 无门扇几何 / 异常尺寸）→ `ExclusionPolicy` 门类判定。物理项永远不可开关化；Fika 不并入安全类。
  - **双门**：候选集改为含安全类的可成对门（排除物理 / Fika / 高危组）；只把 `Normal` 改写为 `Double` 并重跑排除判定，保留 Keycard/Sliding/Trigger 等更具体分类。开启安全类后两扇同时受理；关闭时同时拒绝。
  - **配置面**：`6. 排除与开关` 新增 `Allow Safety Exclusions`（默认 `false`）与 `Allow High Risk Exclusions`（默认 `false`，描述含高危警示）。二者进入 `ModConfig` / `RuleConfig` 战局启动快照，战局中途改 F12 下一局生效。
  - **调试面板**：非受理门按 `safety: ...` / `high risk: ...` / `physical: ...` 显示分类原因；顶栏只统计“仍被排除”的分类数量。
  - **适配器**：破坏输入直接取 `entry.DoorClass`；锁路径优先读注册表分类，注册表未命中才回落到类型映射。
  - **@oracle 前台审查**：PASS（0 Must-fix / 4 Should-fix），4 项均已修：物理门类回写 `Excluded`、双门候选排除高危组、锁路径复用注册表门类、面板计数排除已放行项；另补 `MetalThick` 材质保持原版（`ProfileFor` 无档案）。
  - **验证**：`dotnet build -c Release` 0 warning / 0 error；S0 单测 **100/100**；标准检查 PASS=12 / FAIL=0 / WAIVED=2。
  - **产物**：`bin\Release\DoorBreachOverhaul.dll` = `172,032 B` / SHA256 `CE2D0115BC09F4D54A217C40EA4D1DE565BC702348E40AC939DA0DC86C5A611A`（`release\` 一致）。尚未部署（交接时游戏/Launcher/服务端/MO2 均在运行）。
  - **待部署**：Overseer 退出 `EscapeFromTarkov` / `SPT.Launcher` 后复制到覆盖层，旧 DLL 备份至 `.artifacts\doorbreach-ticket06-backup\`；新 DLL 哈希以部署后复验为准。
  - **实机清单**：① 默认全关回归（普通门照常；安全类/高危组只显示原因、无孔洞；任务脚本与撤离交互正常）；② 仅安全类开（钥匙卡 / 滑门 / 开关门 / 双门 / 非可操作门逐类打孔并观察接管/动画，双门两扇同时生效；高危组仍拒绝）；③ 仅高危组开（任务触发门 / 撤离门可破坏，核对任务脚本与撤离判定；安全类仍拒绝）；④ 全开（叠加，物理项仍拒绝）；⑤ `Show Door Info` 面板逐类原因可读。
  - **已知交互边界**：上锁钥匙卡门的菜单仍只注入 Plant Explosive（KeyCard 重载），解锁后尾调用 Door 重载已有 DO 三动作；若实机发现解锁态缺动作，再单开修补。BB 锁血池对可操作安全类门本就可破，本单开关主要门控 DO 破坏注册表；实机需确认“破门 + 破坏”同时可用。
  - **残余风险**：滑门 / 开关门的门扇几何与接管动画未验证；高危组开启后 `DoorTakeover` 置 `Operatable=false` / `DoorState=Open` 对触发器与撤离判定的副作用需实测。
- 2026-10-24（Level B 通过）：
  - 部署：覆盖层 DLL 已更新为 `172,032 B` / SHA256 `CE2D0115BC09F4D54A217C40EA4D1DE565BC702348E40AC939DA0DC86C5A611A`，与 `bin\Release` / `release` 一致；旧版（工单 05，`A33D6E21…`）备份到 `.artifacts\doorbreach-ticket06-backup\DoorBreachOverhaul.pre-ticket06.dll`。
  - 主菜单日志：17 个 DoorBreach 补丁全部 `Enabled patch`；`Failed to create patch` = 0；DoorBreach Error/Exception = 0。
  - 配置快照行：`安全类排除=False, 高危组排除=False`，其余与既有 cfg 一致（Pleb/SemiPleb=False，金属门破门弹=True，容器/后备箱=True，耐久 200-400，霰弹 12，榴弹 6，近战 5，弹药 4，黑名单 0）。
  - 结论：Level B 通过；待 Level C 逐开关实机验证。
- 2026-10-25/26（Level C 第 1 轮部分证据，Shoreline）：
  - 注册表汇总：`373 doors: 321 destructible, safety excluded 0, high risk excluded 0, physical 52`。**Shoreline 本图无 `KeycardDoor/SlidingDoor/DoorSwitch/Double` 类门**，安全类/高危组无法在本图验证；需换 Interchange/Labs/Reserve/Streets。
  - 普通木门 `door_Shoreline_West_00045 (Normal/None)`：`accepted=True (accepted)`，面板 `destructible: WoodThin`，13 次命中、827/840 细胞完整、可见弹孔 → 普通破坏路径 PASS。
  - 普通木门 `door_Shoreline_Sanatorium_indoor_00046`：`accepted=True (accepted (locked))`，29 次命中、628/741 细胞完整、`leaf Attached`、上铰链 17% / 下铰链 54% / 门闩 75%、`pieces fallen 0`。**未脱落属预期**：两个铰链均未归零，需先打左上铰链至 0%（门变 Hanging），再打左下铰链至 0%（整扇 Detach）；不是 06 排除开关或破坏受理的问题。
  - 本局无 DoorBreach Error/Exception；`Hanging And Falling Doors (Experimental)=true`、`Show Holes=true` 均生效。
  - 待办：换图验证安全类（钥匙卡/滑门/开关门）默认拒绝与仅安全类开受理；再验证高危组开关。
- 2026-10-26（Factory 边界记录，物理优先）：
  - `Factory_Rework_Basement_00031`：面板类型 `(KeycardDoor)`、状态 `Locked`，但原因行是 `physical: material MetalThick stays vanilla`。注册表 `77 doors / 47 destructible / safety excluded 1 / high risk 0 / physical 29`；该门计入 **physical**，不是 safety（safety 1 是另一扇门）。
  - 结论：门类识别正确（KeycardDoor），但物理不可行优先于门类；`Allow Safety Exclusions=true` 也不会使其可破坏。面板 `hits 0`（未打该门），故无 `[Policy]` 行；若射击应得到 `accepted=False (physical: ...)`。
  - 用户可见性改进（未实施）：可在 physical 原因后追加 `(class KeycardDoor)`，避免“看着像钥匙卡门却不是 safety 分类”的困惑；需下一次构建/部署。
  - README 已补“物理优先”说明（6. 排除与开关表下）。
- 2026-10-27（Interchange 安全类门定位）：
  - `Shopping, Mall_DesignStuff_00051`：面板 `(KeycardDoor) Locked`，原因 `safety: keycard door`，材质 `Metal 1`、leaf `0.85 x 1.59 x 0.078 m`；当前 `hits 0`。
  - 本图注册表：`242 doors / 83 destructible / safety excluded 1 / high risk 0 / physical 158`。该门即 `safety excluded 1`，且材质有 `MetalThin` 档案（与 Factory 的 `MetalThick` 物理项不同），是安全类开关的正确测试目标。
  - 待验：默认全关射击应 `accepted=False (safety: keycard door)`、无孔；`Allow Safety Exclusions=true` 后重进战局应 `accepted=True`、面板 `destructible: MetalThin`、可打孔。本图 high risk 0，无法测高危组。
  - **B 局（`Allow Safety Exclusions=true`）PASS（2026-10-27）**：
    - 面板：`destructible: MetalThin (punch-through at pen 40)`、`safety excluded 0`、`hits 26`、`cells 544/544 intact`、leaf Attached、上/下铰链 100%/99%、latch 96%。
    - 日志：`[Destruction] 242 doors: 84 destructible, safety excluded 0, high risk excluded 0, physical 158`；`[Policy] Shopping_Mall_DesignStuff_00051 (Keycard/Safety): accepted=True (accepted (locked))`。
    - A 局（默认全关）用户观察“不出孔”；B 局受理且面板转为 `destructible` → **安全类总开关对 KeycardDoor 生效**。
    - 注意：26 次命中集中在锁/门闩区域（latch 96%、铰链 99-100%），`cells 544/544 intact`，所以门板暂无孔洞；要看到孔需打门板中心。
    - 仍待测：滑门 / 开关门 / 成对双门 / 非可操作门的安全子类；高危组（Interchange high risk 0，需换图）。
    - **Interchange 不适合作滑门/开关门测试**：A/B 两局 `safety excluded` 均为 1（就是该 KeycardDoor）；若存在额外的 `SlidingDoor/DoorSwitch` 安全门，计数应 >1。主入口玻璃门在游戏里多为普通 `Door` 或已开启状态，不是 `SlidingDoor` 类。滑门建议去 Labs（实验室走廊滑门多）；开关门建议 Reserve / Streets / Labs，以面板 `(type)` 字段为准。
