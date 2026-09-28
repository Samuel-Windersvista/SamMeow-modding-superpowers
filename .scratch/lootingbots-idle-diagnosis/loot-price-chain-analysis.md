# LootingBots 价格链路分析（价格获取 → 阈值 → 拾取）与优化空间

- 日期：2026-09-20
- 对象：Moew-LootingBots fork（1.6.1，= 当前部署源码）+ 部署配置实测 + 上游 1.7.0 对照
- 来源：exp-10 只读分析（链路 5 个核心文件 + 配置 + 上游 1.7.0 逐行核对）
- 配置快照（2026-09-20 实测，用户已在 F12 调整）：检测距离 250m；PMC Min=2000；Scav Min=3000；Max=0；UseMarketPrices=true；ValueFromMods=true；Maximum looting bots=15
  - 我方 23:55 备份 `.bak` 为旧值（900 / PMC 8000 / Scav 2000 / max 22）——如需还原可用

---

## 1. 链路总览（文字 + file:line）

```
[价格数据源初始化]
  LootingBots.cs:502-518 Update() 门控（HandbookData==null || !MarketInitialized）
    -> ItemAppraiser.cs:20 Init()
         market=true : :24-34 RagfairGetPrices（异步回调，仅一次）；MarketInitialized=true
         market=false: :39   HandbookClass.Instance.Items（静态）
       :42 _priceCache.Clear()

[扫描 + 价值门控]（仅 FindLootLogic 触发）
  FindLootLogic.cs:35-44 BeginSearch()
    -> LootFinder.cs:93 FindLootCoroutine()
         :106 OverlapSphereNonAlloc（半径 = 三距离最大值）；:127 按距离排序
         逐候选：
           :164 IsLootIgnored? -> skip
           :176-189 canLootItem：SearchableItem | 更好护甲 | (IsValuableEnough(rootItem) && 空格>尺寸)
                   ^^^^^^^^^^^^^^^^^^ 唯一"扫描期"价值门控 -> LootingBrain.cs:496 -> LootingInventoryController.cs:848
           :191 canLootCorpse / :170 canLootContainer：不做任何价值判断
           :212 IsLootInRange（路径长度）/ IsLootInSight（射线）
           :225 CacheActiveLootId；:229-243 设 ActiveXxx + Destination
           break（首个通过者即目标）

[移动]
  LootingLogic.cs:59 TryLoot()
    :64-88 IsCloseEnough? -> StartLooting；否则 :148 TryMoveToLoot() GoToPoint
    （未到目标前不再做价值判断；卡住>2 或导航>30 -> HandleNonNavigableLoot）

[交互 + 入包]
  LootingBrain.cs:289 StartLooting()
    :315 LootCorpse / :370 LootContainer / :422 LootItem
      -> LootingInventoryController.cs:265 TryAddItemsToBot(items)
           :271 UseExamineTime 延时
           :281 CurrentItemPrice = ItemAppraiser.GetItemPrice(item)   <- 拾取期计价
           :296 GetEquipAction -> 换装/丢弃
           :317 AllowedToEquip -> 装备
           :325 LootNestedItems（容器/背心递归）
           :336 AllowedToPickup -> :888 IsValuableEnough(CurrentItemPrice)   <- 拾取期阈值门控
           :342 武器拆配件
           :311/319/338 Stats.AddNetValue(CurrentItemPrice)
```

**核心结构结论**：价值过滤两处、时机不同 —— 松散物品在**扫描期**过滤（不会为低价值物品走路）；容器/尸体**到达后逐件**过滤（扫描期零预判 → 250m 内走遍所有容器/尸体）。

---

## 2. 关键实现细节

### 2.1 价格获取（ItemAppraiser.cs）

- 数据源：
  - `UseMarketPrices=true` → `RagfairGetPrices`（:29），回调写 `MarketData`；**仅客户端启动/首次 Init 拉取一次**（1.6.1 无 30 分钟刷新，上游 1.7 有）。
  - `false` → `HandbookData`（启动时构建一次）。
- `GetItemPrice(Item) :46` 算法：
  1. `:49` 缓存命中（键 = `TemplateId.ToString()`）直接返回；
  2. `:57-61` market 且 `MarketData!=null`：武器且 `ValueFromMods` → `GetWeaponMarketPrice`（:119，**仅累加 Mods 配件，不含底座**）；否则 `GetItemMarketPrice`（:141，未命中返回 0，**无 handbook 兜底**）；
  3. `:63-67` 否则 handbook：武器 → `GetWeaponHandbookPrice`（:84，同样仅配件）；否则 `GetItemHandbookPrice`（:105，**不乘 StackObjectsCount**）；
  4. `:69-74` 皆空 → 0；
  5. `:77` 写缓存。
- 缓存：键 = TemplateId（忽略配件组合与堆叠数）；生命周期 = 每次 raid（Init 清 + raid 结束清）。
- 调用频次：扫描期（松散物品每模板 1 次）；拾取期（每件）；装备估值（每 bot 3 次）；换装/丢弃。

### 2.2 阈值（LootingInventoryController.cs:848-859）

```
isPMC = BotTypeUtils.IsPMC(role)
min = isPMC ? PMCMinLootThreshold : ScavMinLootThreshold
max = isPMC ? PMCMaxLootThreshold : ScavMaxLootThreshold
return itemPrice >= min && (max==0 || itemPrice <= max)
```

- 比较对象 = **单件整价**（未除以格数、未聚合容器）。
- 分支：仅 PMC vs 非 PMC（Scav/Boss/Raider/Cultist 全走 Scav 阈值）。
- 先后：松散物品"先判价后走路"；容器/尸体"先走路后判价"。

### 2.3 拾取（TryAddItemsToBot，LootingInventoryController.cs:265-378）

ExamineTime 延时 → 计价 → 弹匣可用性 → 换装 → 直接装备 → 嵌套容器递归 → AllowedToPickup 阈值门控 → 入包 → 武器拆配件。
豁免：货币、狗牌、可用弹匣；**弹药无豁免**。

---

## 3. 优化清单（按 收益/风险 排序）

### A. 正确性（高收益 / 中低风险）

| # | 位置 | 问题 | 建议 | 风险 |
|---|------|------|------|------|
| A1 | ItemAppraiser.cs:49,77 | 缓存键=TemplateId，武器配件组合被同模板复用 → 错价 | 武器/可堆叠不入 TemplateId 缓存；或直接移除缓存（上游做法） | 低 |
| A2 | ItemAppraiser.cs:105-114,141-149 | **未乘 StackObjectsCount**：60 发子弹按 1 发计价 | `price *= lootItem.StackObjectsCount`（上游 :149/:213） | 低 |
| A3 | ItemAppraiser.cs:57-68 | market 未就绪时返回 0 **并缓存 0** | 不缓存 0；market 未命中回退 handbook（上游 :231） | 低 |
| A4 | LootingInventoryController.cs:888 | 阈值按整件而非每格 → 大件低密度物品挤占背包 | `IsValuableEnough(CurrentItemPrice / GetItemSize())`（上游） | 中（需重调 cfg） |
| A5 | LootingInventoryController.cs:731（疑似） | 换 secondary 分支写 `new ValuePair(secondary.Id, lootValue)` —— 旧枪 ID 配新枪价值 | 应为 `lootWeapon.Id`（需确认） | 低 |
| A6 | ItemAppraiser.cs:84-102,119-138 | 武器价值不含底座模板，仅配件和 | 文档化或补底座价（会抬高全部武器估值） | 中 |
| A7 | LootingInventoryController.cs:885-889 | 无 `IsUsableAmmo` 豁免 + A2 低估 → 基本不拾取松散弹药 | 引入 `IsUsableAmmo`（上游 :1279）+ A2 | 低 |
| A8 | LootingInventoryController.cs:920,311,338 | NetLootValue 受缓存错价污染 | 随 A1/A2 自动修正 | 低 |

### B. 性能（中收益 / 中风险）

| # | 位置 | 问题 | 建议 | 风险 |
|---|------|------|------|------|
| B1 | LootFinder.cs:93-253 | 协程逐候选逐帧 `yield`；无全局并发限制（fork 删了上游 ScanScheduler） | 回填 ScanScheduler + CancellationToken；或降半径 | 中 |
| B2 | LootFinder（无空扫描冷却） | 反复扫描空区域 | 回填上游 MaxEmptyAttempts=3 / 180s 冷却 | 低 |
| B3 | ItemAppraiser.cs:46-79 | 武器首次 O(mods)，每次 pickup 仍计算 | 模板基价缓存与武器动态价分离 | 低 |
| B4 | LootFinder.cs:106 | 3000 buffer + 250m | 降半径/分层查询 | 中 |

### C. 行为（中高收益 / 中风险）

| # | 位置 | 问题 | 建议 | 风险 |
|---|------|------|------|------|
| C1 | LootFinder.cs:170-194 | 容器/尸体扫描期零价值预判 → 250m 内走遍所有容器/尸体 | 扫描期做容器聚合/最高价值粗筛，或上游优先队列 | 中 |
| C2 | LootFinder.cs:188 | 松散物品已提前过滤（好），但整件阈值（见 A4） | 随 A4 | 低 |
| C3 | LootingBrain.cs:315-365 | 尸体仅装备槽起手 | 上游 `GetPriorityItems` + async | 中 |
| C4 | 无 | 无击杀/空投优先 | 上游 OnKilledEnemyPlayer / OnAirdropLanded | 中 |

### D. 配置建议

- `market=true` 但 1.6.1 只启动拉一次价（长会话失真）→ 若不回填刷新逻辑，考虑 `market=false`（静态但稳定）或接受陈旧。
- 整件阈值（A4 未修前）要逼近"每格 N₽"效果需把阈值放大到 `N × 平均格数`（这解释了旧配置 PMC 8000 的含义）。
- 距离 250m（默认 80m）是 B1/B4 帧率风险主因；建议 100-150m + B2 冷却。
- `ValueFromMods=true` 合理，但 A1 未修前会放大武器错价（配件越多污染越重）。

---

## 4. 上游 1.7.0 可回填项

| 环节 | 1.7.0 做法 |
|------|-----------|
| 价格缓存 | 彻底移除 `_priceCache`；`Dictionary<MongoID,float>` 键化 |
| 堆叠计价 | `price *= lootItem.StackObjectsCount` |
| 弹药盒 | AmmoBox 按其弹药计价 |
| 市场价兜底 | 未命中回退 handbook |
| 价格刷新 | UpdatePricesAsync + 30min 刷新 + Stopwatch + 异常兜底 |
| 阈值语义 | 每格价格 `itemValue / GetItemSize()` |
| 弹药豁免 | `IsUsableAmmo` |
| 净值聚合 | 嵌套/容器物品计入 NetWorth |
| 扫描调度 | ScanScheduler 全局并发 + CancellationToken + async |
| 空扫描冷却 | MaxEmptyAttempts=3 / 180s |
| 优先队列 | 击杀尸体 / 空投容器 |
| 尸体重构 | ActiveLoot 统一 + GetPriorityItems |
| 异步化 | 全链路 async Task + LootTimeout 取消 |
| 池化 | ListPool/DictionaryPool |

**回填优先级**：A1/A2 → A3 → A4（需重调 cfg）→ B2/B1 → C3/C4。

---

## 5. 建议实施顺序

1. **低风险正确性批次（推荐先做）**：A2（堆叠计价）+ A1（武器缓存修复）+ A3（0 价兜底）+ A7（弹药豁免）。
2. **次批**：A4（每格阈值，同步重调 cfg 阈值）+ B2（空扫描冷却）。
3. **后续**：B1（ScanScheduler）/ C1（容器预筛）/ C3 / C4。

---

## 6. 不确定项

- [不确定] 武器 `Mods` 是否已含默认部件（影响 A6 严重度）；cfg 已开 `Item Appraiser Log Levels = Debug`，可运行时取证。
- [不确定] 地面松散狗牌是否因 handbook 价 0 而永不拾取。
- [不确定] `LootingInventoryController.cs:731` 赋值疑点需结合换装回调确认。
- [不确定] RagfairGetPrices 回调实际到达时间（决定 A3 竞态是否真实发生）。
