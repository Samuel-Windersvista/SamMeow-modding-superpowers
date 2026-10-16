# 13: Softcore 四表引用分类与清理

**What to build:** 对 softcore 四表中「基础库与内容 mod 均无」的引用逐条分类并处置：scavcase **5 条**（`62710974e71632321e5afd5f / 62710a8c403346379e3de9be / 62710a69adfbd4354d79c58e / 6271093e621b0a76055cd61e / 62710a0e436dcc0b9c55f4ec`）、fleamarket **20 条**（`5b47574386f77428ca22b2xx` 系列）、crafting-rebalance **5 条**（`61c77cc6fcc1673f08540e9b / 5dc1f4d9e078d303d91b44c7 / 63966fccac6f8f3c677b9d89 / 5dd3c5a67da3785e63275437 / 6179ad0a6e9dd54ac275e3f2`）。分类规则：品类/基准类引用 → 核实 4.1.6 跳蚤分类表后保留（注明）；物品引用 → 删除或修正；内部 id（配方自身等）→ 保留（注明）。crafting-recipes 的 12 条 `63da4dbe…` 系附加配方自身 `_id`，**保留不动**。

**Blocked by:** None（可立即开始）

**Status:** ready-for-agent

- [ ] 31 条 neither 逐条分类结论成表（删除 / 保留并注明），随 Comments 登记
- [ ] 按结论清理/保留；`Softcore*ResourceTests` 计数与抽查同步（若有变化）；全部测试绿
- [ ] 参考：`spec-v0.2.0.md` D3-A、`align-report-2026-10-16.txt` §E

## Comments

### 31 条 neither 引用分类结论（2026-10-16 实现会话）

核实基准（4.1.6 真实库）：`E:\Game\EFT_Offline\SPT_41x\SPT_Runtime\SPT_Data\database\`。全部处置后数据主体不变，仅新增 JSONC 注释。

**A. scavcase 5 条 — 内部 id（配方自身标识）→ 保留 + 注释**

| id | 所属表/字段 | 分类 | 核实依据 | 处置 |
|---|---|---|---|---|
| 62710974e71632321e5afd5f | scavcase.json `recipes[].id` | 内部 id | `ScavCaseChanger.ToRecipes` 以 `Id = entry.Id` 新建 ScavRecipe（ScavCaseChanger.cs:240-263） | 保留 |
| 62710a8c403346379e3de9be | 同上 | 内部 id | 同上 | 保留 |
| 62710a69adfbd4354d79c58e | 同上 | 内部 id | 同上 | 保留 |
| 6271093e621b0a76055cd61e | 同上 | 内部 id | 同上 | 保留 |
| 62710a0e436dcc0b9c55f4ec | 同上 | 内部 id | 同上 | 保留 |

**B. fleamarket 20 条 — 4.1.6 handbook 类目节点 Id → 保留 + 注释**

| id | handbook.json Categories[].Id 行 | 分类 | 处置 |
|---|---|---|---|
| 5b47574386f77428ca22b2ed | 4 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b2ee | 102 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b2ef | 375 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b2f0 | 200 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b2f1 | 88 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b2f2 | 151 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b2f3 | 347 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b2f4 | 445 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b2f6 | 522 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b335 | 81 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b336 | 18 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b337 | 67 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b338 | 46 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b339 | 95 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b33a | 487 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b33e | 515 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b340 | 361 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b341 | 305 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b343 | 403 | handbook 节点 | 保留 |
| 5b47574386f77428ca22b344 | 592 | handbook 节点 | 保留 |

说明：20 条全部命中 `templates\handbook.json` 的 `Categories[].Id`（4.1.6）。消费方 `EconomyOptionsChanger.ApplyPacifist` 第 85 行以 `handbookItem.ParentId` 匹配该白名单，语义一致，无需删改。

**C. crafting-rebalance 5 条 → 保留 + 注释**

| id | 所属表/字段 | 分类 | 核实依据（文件:行） | 处置 |
|---|---|---|---|---|
| 61c77cc6fcc1673f08540e9b | `recipeAdjustments[].recipeId` | 配方 _id 引用 | hideout\production.json:7653 | 保留 |
| 5dc1f4d9e078d303d91b44c7 | `recipeAdjustments[].recipeId` | 配方 _id 引用 | hideout\production.json:6214 | 保留 |
| 5dd3c5a67da3785e63275437 | `recipeAdjustments[].recipeId` | 配方 _id 引用 | hideout\production.json:7809 | 保留 |
| 6179ad0a6e9dd54ac275e3f2 | `requirements[].questId` | 任务 _id 引用 | templates\quests.json:76968（QuestName "The Huntsman Path - Outcasts"） | 保留 |
| 63966fccac6f8f3c677b9d89 | `requirements[].questId` | 任务 _id 引用 | templates\quests.json:90087（QuestName "Snatch"） | 保留 |

**63966fcc 深核结论（更正预研）**：预研称其「不在 quests.json」，实测**存在**——`templates\quests.json:90087` 为任务定义键，`:90089` 为其 `_id`，QuestName = "Snatch"。该 id 亦出现于 `hideout\production.json:6343`（对应配方 endProduct `5c052fb986f7746b2101e909` 的 QuestComplete 前置，与本表 `replaceRequirements` 内容一致）。故非死引用，**保留**；配方不会 softlock。

**D. crafting-recipes 12 条（`63da4dbee8fa73e22500000{1-9,4}` / `…0014` / `…0011` / `…0012`）→ 保留不动**
`additionalRecipes[]._id` 系附加配方自身 `_id`，运行时直接注册（`CraftingChangesChanger.ApplyAdditionalRecipes`），spec D3-A 已定案。文件内加注释注明，不改数据。

### 变更文件

- `data/softcore/scavcase.json`：recipes 数组上方加注释（id 为配方自身标识）。
- `data/softcore/fleamarket.json`：`fleaListingsWhitelistHandBook` 上方加注释（20 条已核实为 handbook 节点 Id）。
- `data/softcore/crafting-rebalance.json`：3 处 `recipeId` + 2 处 `questId` 加注释（核实依据含文件:行）。
- `data/softcore/crafting-recipes.json`：`additionalRecipes` 上方加注释（_id 为配方自身标识）。

### 测试

`dotnet test "mods\InescapableTarkovsSoftcore416\InescapableTarkovsSoftcore.sln" --artifacts-path .artifacts/its-13 -c Debug --nologo -v q` → 通过 228 / 失败 0 / 跳过 0。数据主体未变，`Softcore*ResourceTests` 计数无需同步。
