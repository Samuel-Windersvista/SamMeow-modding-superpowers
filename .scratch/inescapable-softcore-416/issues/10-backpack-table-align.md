# 10: 背包表对齐（A 删 1 + C 补 14）

**What to build:** `backpacks.json`：删除全整合包不存在的 1 条（`6621b28d9411498998d408c3`，A 类）；追加 14 条（C 类：基础库 11 + WTT-ContentBackport 3），按同类尺寸风格定值（清空过滤策略与既有条目一致）；对旧包沉浸感层曾移除的 9 条先做体积/网格核查再定值。测试钉值 43→**56**。id 清单见 `spec-v0.2.0.md` Further Notes / `align-report-2026-10-16.txt` §B。

**Blocked by:** None（可立即开始）

**Status:** ready-for-agent

- [x] 删除 A 类 1 条
- [x] 追加 C 类 14 条（含 WTT 3 条：Mystery Ranch 2 Day / NICE Frame Sling / TT Modular 45P）
- [x] 尺寸/网格核查记录（曾被移除 9 条）；不在风格外扩大网格
- [x] `BackpacksModuleTests` 钉值更新；全部测试绿

## Comments

实现完成（2026-10-16）。`data/backpacks/backpacks.json` 43→**56** 条（删 1 + 追加 14）；`BackpacksModuleTests` 钉值同步；`BackpacksModule.cs` XML 注释改为计数无关表述；全量 228 测试绿。

### 旧包两处源码定性（核查前置）

| 文件 | 定性 | 条目数 | 说明 |
|---|---|---|---|
| `《生活在诺文斯克》背包容量扩展-BetterBackpacks沉浸感增强设置\...\mod.ts` | **IMM 终态** | 43 | 与 v0.1.0 现表逐条一致（8×9 / 7×7 / 5×5 等扩容值）；**移除了 9 条** |
| `[7]更大的背包-BetterBackpacks\...\mod.ts` | **原版全表** | 44 | 含被移除的 9 条（值=4.1.6 实物网格，因 `WARNING ... GRID OVERLAPS` 不可外扩）；多数条目值为扩容前基准 |

被 IMM 移除的 9 条即 C 类基础库主体；原版全表给定了它们的原始值（见下表）。

### per-entry 核查表（id | 名称 | 4.1.6 原始网格 | 旧包源值 | 定值 H×V | 理由）

| id | 名称 | 4.1.6 原始网格 | 旧包源值 | 定值 | 理由 |
|---|---|---|---|---|---|
| 656f198fb27298d6fd005466 | Dragon Egg Mk II | 2×2 | 原版 2×2 | **2×2** | 旧包原版直系值，采纳 |
| 674da9cf0cb4bcde7103c07b | MR Terraframe (Xmas) | 2×5 | 原版 2×5 | **2×5** | 同上 |
| 674da107c512807d1a0e7436 | MR Terraframe | 2×5 | 原版 2×5 | **2×5** | 同上 |
| 656e0436d44a1bb4220303a0 | MR SATL Bridger | 6×2 | 原版 6×2 | **6×2** | 同上 |
| 67458730df3c1da90b0b052b | 5.11 Tactical RUSH 100 | 1×2 | 原版 2×2 | **2×2** | 采纳旧包原版（较实物 +1×+1，未越风格上沿） |
| 5f5e46b96bdad616ad46d613 | Eberlestock F4 Terminator | 5×4 | 原版 5×4 | **5×4** | 旧包原版直系值 |
| 5c0e805e86f774683f3dd637 | 3V Gear Paratus | 5×5 | 原版 5×5 | **5×5** | 同上 |
| 6034d2d697633951dc245ea6 | Eberlestock G2 Gunslinger II | 3×5 | 原版 3×5 | **3×5** | 同上 |
| 5d5d940f86f7742797262046 | Oakley Mechanism | 4×4 | 原版 4×4 | **4×4** | 同上 |
| 56e294cdd2720b603a8b4575 | Developer Pack (endlessBackpack) | 6×50 | 无 | **8×9** | 无旧包来源；6×50 远超风格上沿（V≤9），按最大包体类（6Sh118 8×9）归一。**见遗留问题** |
| 656ddcf0f02d7bcea90bf395 | Tehinkom Patrol | 1×2 | 无 | **6×7** | 无旧包来源；多网格小包，按 patrol/daypack 同族（LBT-8005A Day=6×7）定值 |
| 68947ab5a733b1602007e2fe | MR 2 Day Assault Pack (Black) | WTT 5×6 | 无 | **5×6** | WTT 作者原值（WTT-ContentBackport `Backpack_config.json`），在风格内（见注） |
| 68947ad3e4bf255d1b0ca75c | MR NICE Frame Load Sling | WTT 5×7 | 无 | **5×7** | WTT 作者原值，在风格内（见注） |
| 68947a8ce4bf255d1b0ca759 | TT Modular Pack 45 Plus (MCB) | WTT 5×8 | 无 | **5×8** | WTT 作者原值，在风格内（见注） |

**注（WTT 3 定值取向）**：WTT 3 条在注入 mod 内有作者给定的网格（5×6 / 5×7 / 5×8，均在既有风格域 H5–8/V5–9 内）。按「有真实来源优先于自造」原则采纳作者原值；若按工单示例「large frame pack 7×9 / 与 TT 35 对齐」取向，可改为 TT 45P=7×9、2 Day=6×7、Load Sling=5×6（备选，未采用）。

**注（9 条旧包来源值低于风格域）**：9 条采纳的旧包原版值（2×2…6×2）低于既有表风格域下沿，但未越上沿，符合「避免风格外扩格」；此为旧包 IMM 移除的既有安全值，属直系来源。

### 测试钉值

- `Table_HasFortyThreeBackpacks_WithPinned6Sh118` → `Table_HasFiftySixBackpacks_WithPinnedAnchors`：总数/distinct 56；6Sh118=8×9 保留；新增锚点 terraframe(674da107…)=2×5、paratus(5c0e805e…)=5×5、TT Modular 45P(68947a8c…)=5×8。
- `Apply_BaseOnlyBackpacks_AreNotTouched` → `Apply_FormerlyBaseOnlyBackpacks_AreNowAligned`：9 条现已在表内，断言 changed=9 且各条网格=表值、filters 清空（原「不在表内故不动」语义已失效）。
- `Apply_MissingBackpack_WarnsAndSkips`：warning 计数 43→56（report + logger 两处）。
- `Apply_AllFortyThree_AreChanged` → `Apply_AllFiftySix_AreChanged`：changed 43→56。
- `BackpacksModule` XML 注释「按内嵌查找表把 43 条背包的」→「按查找表把各背包的」（计数无关）。

### 验证

`dotnet test mods\InescapableTarkovsSoftcore416\InescapableTarkovsSoftcore.sln --artifacts-path .artifacts/its-10 -c Debug` → 通过 228 / 失败 0 / 跳过 0（未提交）。

### 偏差与边界

- `data/backpacks/backpacks.json` 文件头（第 1–6 行）**未动**（含第 3 行「43 条」计数，按「文件头勿动」保留原样）。
- `FeatureTables.cs` 第 22 行 `<summary>G5：43 条…</summary>` **未改**（按边界，留待编排者收口）。
- `build/overlay/.../config/backpacks.json`（构建产物）未手工改动。

### 编排者裁定（2026-10-16，三项定值取向）

1. **endless Developer Pack → 维持 8×9**。依据：spec D3「背包按同类尺寸风格定值」且 C 类清单显式列入该 id；现表已有收缩先例（本次核查新证：LBT-2670 Med 实物 6×8 → 表值 6×7）；该物品为开发/测试物品，统一风格优先于保留 6×50。已列入工单 17 用户验收清单（游玩时顺带确认；如需回退仅改此一行）。
2. **9 条低于风格域 → 维持采纳旧包原版值**。新证（编排者复核原版 mod.ts）：这些多网格包条目附近标注 `// WARNING MAKING THIS ANY BIGGER WILL CAUSE GRID OVERLAPS`——小值为界面网格重叠的物理上限，上浮会破坏多网格布局；其中 8 条与 4.1.6 实物一致（显式钉定 + 清过滤），RUSH 100 为原版扩张（1×2→2×2）。「风格域下沿」规则不适用于多网格包的 Grids[0]（非主仓）。
3. **WTT 3 → 维持作者原值（5×6 / 5×7 / 5×8）**。依据：内容 mod 作者给定值为最高优先风格来源（有真实来源优先于自造）；备选值不采用。

收口补充：`backpacks.json` 第 3 行计数（43→56）、`FeatureTables.cs` G4/G5 计数注释（改计数无关表述）、README 三处臂章计数由编排者同步。

