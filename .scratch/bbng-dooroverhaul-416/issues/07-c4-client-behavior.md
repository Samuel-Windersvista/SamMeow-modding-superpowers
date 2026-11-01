# 07: C4 客户端行为

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** C4 客户端全流程落地：菜单门控（持有 C4、门状态/角度合规）→ 放置（锚定回退链：锁具 → 把手 → 门中心）→ 倒计时与蜂鸣音效 → 引爆**映射为完整破坏管线**（门扇脱落/炸飞 + 爆炸特效 + 可选范围伤害）→ 不可破坏目标回退强制开门（对齐 06 的排除策略）；C4 物品缺席时优雅降级（菜单项不可用、不崩溃、无日志刷屏）。

**Blocked by:** 06（排除开关体系）

**Status:** ready-for-human

- [x] 菜单门控语义与上游一致（物品/状态/角度）；物品缺席时不崩溃、菜单项不可用（降级路径）
- [x] 放置锚定（回退链）与倒计时/音效正常；计时可配
- [x] 引爆走完整破坏管线（不再直接销毁门对象）；范围伤害/特效可配
- [x] 不可破坏目标回退强制开门（上游 C4 永远有效语义）
- [ ] 游戏内验证：无服务端时降级路径通过；（与 08 联合）真实物品端到端
- [x] 与 08 的联合端到端清单就绪（放置→倒计时→引爆→破坏管线）

## Comments

- 2026-10-25（工单 07 代码侧完成，待部署与实机验证）：
  - **菜单门控集中化**：新增 `ExplosiveBreachComponent.IsPlantExplosiveDisabled(door, player)`，统一判定「持有 C4 + 门状态（Shut/Locked/Breaching）+ 破门角度」；`IsBreachAngle` / 装备栏读取异常均按禁用处理，并用 `LogOnce` 单次告警，避免每次打开菜单刷屏。`DoorMenuMerge` 改为调用该入口。
  - **放置失败不消耗物品**：`StartExplosiveBreach` 改为返回 `bool`；`TryPlaceC4OnDoor` 在 ItemFactory 未就绪 / `CreateItem` 返回 null 或抛异常 / GameWorld 不可用 / `SetupItem` 返回 null 时返回 false。`DoorMenuMerge` 只有放置成功后才扣物品、发 Fika 包、启动倒计时。
  - **锚定回退链**：精简为 锁具（`transform.Find("Lock")`）→ 门把手（`door.Handle`）→ 门中心（`door.transform`）；移除上游多余的 `GetComponent("Lock")` 分支。放置时日志记录 `anchor=Lock|Handle|Center`。
  - **引爆接入 DO 破坏管线**：`FractureState.Detonate()` 跳过铰链实验开关，调用既有 `DetachEverything` 生成整扇门板脱离结果（`DestructionResult.Detonated=true`）；`DoorDamageIntake.ApplyDetonation` 走既有分组/视觉/玻璃管线；`LeafVisuals` 将 `Detonated` 传给 `DoorTakeover`，后者对爆炸使用更大冲量上限（`mass * 8`，子弹仍为 `mass * 0.5`），门扇被炸飞/脱落。**门对象不再 `Destroy`**，保留给任务触发/撤离判定。
  - **回退强制开门**：`TryDetonateDoor` 仅在 `DestructionEnabled && ShowHoles && entry.Eligible && entry.Frame != null` 时走破坏；否则 `ApplyHit.OpenDoorIfNotAlreadyOpen`。因此 06 的安全类/高危组开关关闭、物理不可行、Fika、破坏总开关关闭、Show Holes 关闭都会回退，符合“C4 永远有效”。日志 `[C4] 引爆：<doorId> -> destruction|fallback-open` 可断言。
  - **爆炸伤害/特效**：`Effects` 单例缺失时单次告警并跳过特效；半径 <= 0 或伤害 <= 0 直接跳过；`OverlapSphere` 命中按 `Player` 去重；LOS 用 `GetComponentInParent<Player>`；C4 本体先销毁再处理门。
  - **验证**：`dotnet build -c Release` 0 warning / 0 error；S0 单测 **100/100**；标准检查 PASS=12 / FAIL=0 / WAIVED=2。产物 `bin\Release\DoorBreachOverhaul.dll` = `175,616 B` / SHA256 `4FF4DB6FCB5FF923E624F35DA9F1B7A1DCA59C77739CBAB333A1DA85AAADE61C`（`release\` 一致）。**尚未部署**（当前覆盖层为 06 诊断构建 `1EF07AB9…`）。
  - **审查说明**：按流程原应 @oracle 双轴复审；matt-bridge 预设的 oracle 模型 `opencode-go/gemini-4-argon-contributor` 已被弃用，复用 ora-1 与全新会话两次均失败（`ModelDeprecated`）。未擅自改用户配置，改由主会话完成 Spec/Standards 自审，重点核对：破坏管线时序（Detonate → 下一帧 LateUpdate 创建 DoorTakeover）、回退分支覆盖、放置失败不扣物品、爆炸伤害去重/半径 0、06 排除策略联动、子弹脱落冲量未回归。自审未发现 Must-fix；残余风险见下。
  - **残余风险**：① `DoorTakeover` 在引爆后的下一个 `LateUpdate` 才创建；若网格/材质准备失败，门可能“状态已脱离但视觉未接管”，需实机确认；② C4 在门上的模型位置/朝向（锁具/把手/中心）未实机确认；③ 爆炸 LOS 可能被 C4 自身碰撞体/门框遮挡（上游同源风险）；④ 滑门/开关门的 `DoorTakeover` 动画未验证。
  - **部署待办**：Overseer 退出游戏后，将 `bin\Release\DoorBreachOverhaul.dll` 复制到覆盖层；部署前把当前 06 诊断 DLL（`1EF07AB9…`）备份到 `.artifacts\doorbreach-ticket06-backup\DoorBreachOverhaul.ticket06-diagnostic.dll`。
  - **与 08 的联合端到端清单**：
    1. 服务端启动日志：`[DoorBreachOverhaulServer] 已加载：C4 物品 tpl=6636606320e842b50084e51a，工作台配方 id=665d4ce7e381d16c8676292b`；BundleLoader 无 `Could not find bundle` / `not a valid Unity asset bundle`；`/singleplayer/bundles` 含 `C4Explosive.bundle`。
    2. 工作台（≥2 级）配方可见并可产出 C4；图鉴炸药类出现 C4；图标/模型正常。
    3. 带 C4 进战局：对 Shut/Locked 门、破门角度内，菜单 `Plant Explosive` 可用；无 C4 / 门已开 / 角度不符时禁用。
    4. 放置：C4 模型出现在锁具（回退把手/门中心）；日志 `[C4] 放置：<doorId> anchor=...`；倒计时与蜂鸣音效按 `Explosive Timer Seconds`。
    5. 引爆普通可破坏门：门扇脱离/炸飞、碎片/特效；门对象保留；玩家可通行；日志 `[C4] 引爆：<doorId> -> destruction`。
    6. 引爆安全类/高危组排除门（对应开关关闭）：强制开门、无门板脱离；日志 `-> fallback-open`；任务脚本/撤离交互不被误伤。
    7. 引爆物理不可行/Fika/`Destructible Doors` 关闭/`Show Holes` 关闭的门：回退强制开门。
    8. `Explosion Does Damage` 开/关、半径/伤害：范围伤害按距离衰减；半径 0 不崩溃。
    9. 降级：移除服务端组件（或 C4 模板）→ 玩家无 C4、菜单禁用、无异常/日志刷屏。
    10. 反复进出战局：C4 协程/物品/接管无残留。
