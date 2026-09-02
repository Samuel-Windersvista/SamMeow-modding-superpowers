# 可行性计划：tarkov.dev 数据源纳入 SPT 知识库

> 日期：2026-09-02 | 状态：计划待审 | 前置调研：tarkov.dev API 调研报告（@librarian，2026-09-02）

## 1. 目标

为 `knowledge/spt-kb/` 引入 tarkov.dev 作为新的结构化游戏数据源，用于支持：
- mod 开发时的弹药/护甲/任务/地图点位等数值查询
- 与 SPT 4.1 本地数据库的差异对照（live 演进方向参考）
- 中文译名参考（`_zh` 翻译字典）

原则：**原始快照入 archive，提炼结论入 curated，出处登记入 sources**；数值权威性始终以 SPT 本地数据库为准。

## 2. 数据源画像（调研结论摘要）

| 项 | 结论 |
|---|---|
| GraphQL | `https://api.tarkov.dev/graphql`，无鉴权、无速率限制；**当前故障**（Issue #474，2026-07-21 起，超 6 周未修复） |
| 可用通道 | **`https://json.tarkov.dev` REST dump**（实测可用，数据更新至 2026-09-01） |
| 数据量级 | 5312 物品（26 种属性联合体）/ 212 弹药 / 70 护甲 + 40 头盔 / 517 任务（20 种目标类型）/ 789 易物 / 17 地图全点位（loot/spawn/extract）/ 9 商人 / crafts / hideout / 17 语言本地化（含 `_zh`） |
| 数据来源 | tarkov-changes.com 数据挖掘（live 客户端）+ fandom wiki + 社区仓库 + 自营跳蚤扫描器 |
| 当前对应 live 版本 | **EFT 1.1.0.1.46911**（tarkov-changes 2026-08-24 记录） |
| 许可 | 仓库代码 GPLv3（package.json 却写 ISC，不一致）；仅引用数据需保留来源署名 |
| 已知数据缺口 | #481：商人枪械报价仅 18/171；dump 的 `name` 是 `<itemId> Name` 占位 key，需 `_zh`/`_en` 字典映射 |

## 3. 与 SPT 4.1 的版本对齐（核心风险）

- SPT 4.1.2 锁定 EFT **0.16.9.5.40743**；tarkov.dev 数据跟随 live **1.1.0.1.46911**。
- 中间跨越 1.0 与多个补丁：**live 新增的物品/任务/地图在 SPT 4.1 中不存在；同名物品的数值可能被 BSG 调整过**。
- 对策：
  1. 快照锚定：抓取时记录 dump 对应 live 版本，curated 文档标注快照日期；
  2. 对照校验：每批入库后抽样对比 `E:\Game\EFT_Offline\SPT_410\SPT_Data\Server\database\`（items.json 等），统计差异并记录；
  3. 标签隔离：新增版本标签 `[live-ref]`（live 参考数据，非 SPT 事实），在 `VERSIONS.md` 标签约定中补充定义——这类文档是**对照/概念参考**，写 mod 取数仍以 SPT 数据库为准。

## 4. 数据域筛选裁决表

| 域 | 量级 | 收录决策 | 理由 |
|---|---|---|---|
| ammo 弹药属性 | 212 | **[批次1] 提炼 + 快照** | 写 mod 最高频数值（伤害/穿透/初速/甲伤） |
| armor/helmet/插板 | 110+ | **[批次1] 提炼 + 快照** | class/耐久/zones/armorSlots，护甲 mod 核心 |
| tasks 任务全结构 | 517 | **[批次2] 提炼 + 快照** | 20 种目标类型对 quest mod 开发价值高 |
| maps 点位（loot/spawn/extract/boss） | 17 图 | **[批次3] 快照 + 对照笔记** | SPT loot 配置独立，作参考对照 |
| barters/crafts/hideout | 789+ | **[批次4] 提炼 + 快照** | 藏身处生产链在 SPT 中实现，结构同构 |
| traders 库存结构 | 9 商人 | **[批次5] 低优先** | 已知 #481 数据缺口，价值打折 |
| items 全量清单 | 5312 | **[批次0] 快照入库** | 供 ID 反查与未来扩展，不逐条提炼 |
| 中文译名 `_zh` | — | **[批次0] 随快照** | 汉化参考，与快照同捆 |
| 跳蚤价格/历史价格 | — | **排除** | live 动态经济，SPT 无对应 |
| status / goonReports / achievements 玩家统计 | — | **排除** | live 特有 |

## 5. 落点设计

```
knowledge/spt-kb/
├── archive/tarkov-dev/               # 新目录：dump 快照（含抓取脚本，可复现）
│   ├── README.md                     # provenance：抓取时间、live 版本、源 URL
│   ├── fetch-dump.ps1                # 抓取脚本（json.tarkov.dev 端点清单）
│   └── snapshot-<date>/              # items/tasks/barters/crafts/maps/traders + _zh 字典
├── curated/game-data-ref/            # 新目录：提炼文档，frontmatter 标 [live-ref]
│   ├── ammo.md                       # 弹药数值表（含与 SPT 差异对照）
│   ├── armor.md
│   ├── tasks.md
│   └── ...
├── sources/third-party.md            # 追加 tarkov.dev 条目
└── index.json                        # 重建登记新条目（schema_version 1）
```

**仓库体积权衡**：5312 物品全量 dump 约数十 MB。推荐方案：抓取脚本入库 + 筛选后快照入库；全量原始 dump 放 `D:\Temp\opencode` 或本地归档，不入 git。若 Overseer 希望全量入库 git，亦可（需确认）。

## 6. 分批实施计划

| 批次 | 内容 | 产出物 | 验证方式 |
|---|---|---|---|
| 0 | 通道打通：抓 dump、跑 `_zh` 字典映射、数据画像统计 | fetch 脚本 + 快照 + 画像报告（与 SPT 数据库抽样差异统计） | 抽样 20 弹药对比 SPT items.json 数值一致率 |
| 1 | 弹药 + 护甲提炼 | `curated/game-data-ref/ammo.md`、`armor.md` | 每条数值标注来源（tarkov.dev / SPT 数据库），差异样本记录 |
| 2 | 任务体系提炼 | `curated/game-data-ref/tasks.md`（20 种目标类型结构） | 与 SPT quests 模板结构对照 |
| 3 | 地图点位快照 + 对照笔记 | 17 图 loot/spawn/extract 数据 + SPT 对照说明 | 点位数据结构可读、坐标系说明 |
| 4 | 易物/制作/藏身处提炼 | `curated/game-data-ref/barters-crafts-hideout.md` | 与 SPT hideout 表对照 |
| 5 | （可选）商人库存 | `curated/game-data-ref/traders.md` | 注明 #481 缺口 |

每批结束：重建 `index.json` + smoke check（条目可打开、frontmatter 合规）+ 更新 `sources/third-party.md`。

## 7. 风险与对策

| 风险 | 对策 |
|---|---|
| GraphQL 持续故障 | 走 json.tarkov.dev；快照后本地可用，不依赖线上 |
| live 与 SPT 数值漂移 | `[live-ref]` 标签隔离 + 抽样对照表 + 快照锚定版本 |
| dump 名称占位 key | 抓取时即做 `_zh`/`_en` 字典 join，快照内直接存可读名称 |
| 数据缺口（#481 等） | 提炼文档中显式标注已知缺口，不当作全量事实 |
| 仓库体积膨胀 | 抓取脚本可复现，筛选子集入 git，全量留本地 |

## 8. 开放决策点（待 Overseer 裁决）

1. **收录深度**：推荐「脚本 + 筛选快照入库，全量留本地」（§5 权衡）；备选「全量 dump 直接入 git」。
2. **标签约定**：是否同意在 `VERSIONS.md` 新增 `[live-ref]` 标签定义（§3 对策 3）。
3. **批次范围**：是否按 §6 顺序执行批次 0（先抓数据画像再决定后续），还是直接推进到批次 1。
4. **排除项确认**：跳蚤价格/status/玩家统计等 live 特有数据是否确认排除（§4）。

---

*附：tarkov.dev 调研报告原文见会话历史（lib-1 任务产物，2026-09-02）。*
