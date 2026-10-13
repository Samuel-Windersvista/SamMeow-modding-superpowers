# 05: 无敌/部分无敌组移植（DadGamer 健康组 + CODMode）

**Spec:** `.scratch/spt41-debugtoolkit/spec.md`

**What to build:** 端到端编译级可用的健康/无敌模块（游戏内验证随 08）：GodMode（除坠落外全伤害清零）、Keep1Health（`All` / `Head And Thorax` 下拉）、Ignore Headshot Damage、Percentage Headshot Damage + 头部/全身百分比值、No Falling Damage（FallSafeHeight）、CODMode 延迟自动治疗（含流血/骨折/疼痛清理，速率/等待/流血保留可配）；伤害与部位摧毁补丁（`ApplyDamage` / `DestroyBodyPart` 等价物）；`4. 无敌`（8 键）与 `5. 恢复`（4 键）配置节，默认全关；效果清理接口按 02 结论或降级；全部补丁体 fail-open。基线：DadGamerMode 1.9.3 源码，触点与混淆名映射照 02 核实表执行。

**Blocked by:** 02

**Status:** ready-for-agent

**Execution note:** 可与 03 / 04 并行（模块文件域隔离）；`ApplyDamage` 补丁文件为 06 的串行前置。

- [x] GodMode / Keep1Health / IgnoreHeadshot / 百分比 / NoFallingDamage 按上游语义实现（GodMode 坠落语义按 spec 裁定：除外）
- [x] CODMode 行为与配置（速率默认 10 / 等待默认 10 / 流血保留默认 false）
- [x] `4. 无敌` + `5. 恢复` 配置节；列表用 AcceptableValueList 下拉；默认全关
- [x] 补丁体 fail-open；构建 0 error；标准检查通过
- [x] 偏差条目随单记录（07 汇总）

**上游源码（本仓归档）:** `knowledge/spt-kb/archive/forge/mods/DadGamerMode_686_source/`（v1.9.3；上游 LICENSE 占位，按 Props/dvize 署名）。

## Comments

- 2026-10-11（编排者）: 前置就绪——工单 02 子通道 C 触点表已产出并经主控台抽验（`docs/api-touchpoints/dadgamer.md`：37 触点；三条旧混淆名全定位；`ApplyDamage`/`DestroyBodyPart` 非 virtual、`DamageInfo` 为 struct、`BodyState`/`HealthValue` 映射已核）。csproj 引用面已预置；本单文件域 `src/Modules/Invincibility/**`；`ApplyDamage` 补丁文件为 06 的串行前置（敌伤倍率留给 06）。施工（fix-4 续用）。
- 2026-10-11（编排者）: **完成**（fix-4 施工 + 主控台复核）。
  - 交付：`src/Modules/Invincibility/**` 6 文件（Config 12 键、战局组件、3 支补丁：`ApplyDamage` / `DestroyBodyPart` / `GameStartPatch`）；桩 → 真实启停。
  - 复核（编排者复跑）：`dotnet build -c Release` 0 warning / 0 error；标准检查 PASS=13 / FAIL=0 / WAIVED=1。
  - 偏差/决策 7 条随单记录（GodMode 坠落语义＝spec 裁定；CODMode 走原生事件（较 5.0 的降级路径更贴近上游）；无坠落走 `FallSafeHeight` 属性（`_fallSafeHeight` 在 0.16.9 为 private）；`currentHealth.Current = 3f` 原样保留（值拷贝无效写、行为等价；5.0 线省略该行——代码形态差异）；对敌倍率留插入点给 06；挂载 `GetComponent ?? AddComponent`；供 07 汇总）。
  - **AtMinimum 段裁定（编排者）**：保留「常驻」语义、不改码——上游 + 5.0 线两处一致（主控台逐行核实 5.0 代码：其形式 `Current <= Minimum`，与本线 `AtMinimum` 等价、同样独立于 Keep 1 Health 开关）。该行为（已黑部位受击在非 CODMode 下被短路）入 Level C 清单与 README 行为注记（07）。
  - 行为级（`EffectAddedEvent → RemoveEffectFromList` 清理、CODMode 自愈）随 08 / S3。
  - 双轴代码审查在模块工单全数完成后统一进行（修订回填本单）。
- 2026-10-11（编排者）: **双轴审查闭环（Round 1 → 修订轮 → 复验）**。本单相关：`InvincibilityModule` 迁移共享 `PatchSet`（逐项装配、Disable 无条件清理，`_enabled` 仅防重复 Enable）；`InvincibilityConfig` 迁移 `ConfigDescriptions` 工厂。复验：构建 0 warning/0 error、标准检查 PASS=13/FAIL=0/WAIVED=1。记录：`reviews/round1-*.md` 三件。
