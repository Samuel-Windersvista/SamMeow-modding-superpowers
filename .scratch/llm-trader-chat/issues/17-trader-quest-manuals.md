# 17: 商人任务手册（静态层，自动生成）

**What to build:** 为**有任务的商人**生成「任务手册」文档放入 `knowledge/traders/<id>/`，使关键词检索能命中**不依赖玩家进度**的委托常识（任务名、目标、地点、委托人视角摘要）——补足运行时注入（工单 16）只覆盖「进行中」任务之外的空白。

**Blocked by:** 无（可与工单 16 并行；两者只共享 `knowledge/` 目录，写入文件不同名）

**Status:** open

## 为什么需要静态层（实证）

实机中玩家问的「水瓶座行动」**并不在玩家 profile 的 25 条任务里**（exp-2 核对：两个 Aquarius id 均不在 profile 中）。因此**只注入「进行中」任务的运行时方案无法覆盖此场景**——玩家会问未接取、传闻中、或已完成的任务。静态任务手册用于兜住这一类。

## 交付形态（对本工单的一处方案修订，提请知悉）

原口头方案为「人工/agent 起草 FAQ」。**建议改为脚本生成**：从任务数据（原版 `quests.json` + `ch.json`，以及已定位的 mod 任务文件）生成每位商人的任务手册，**一段一任务**，便于现有段落分块检索。

- 优点：完整性由数据保证（Prapor 77 / Mechanic 105 条不可能手写且不遗漏）、可随版本重生成、避免主观改写引入错误。
- 代价：文本为游戏原生口吻（非人格化改写），但任务描述本就是**该商人第一人称台词**，还原度反而更高。

## 覆盖范围（**实测交付结果**，2026-10-16 生成器产出）

生成器实跑：`traders=15 quests=742 files=15 unresolvedName=0 unresolvedObjectives=10 droppedNoTrader=0`（幂等已验证：连续两次运行 SHA256 全同）。

| 商人 | 交付任务数 | 文件字节 |
|---|---|---|
| Mechanic | 108 | 73,690 |
| Scorpion | 122 | 58,392 |
| Prapor | 79 | 54,913 |
| Skier | 74 | 55,295 |
| Jaeger | 80 | 54,397 |
| Peacekeeper | 62 | 44,365 |
| Ragman | 59 | 42,464 |
| Therapist | 58 | 41,964 |
| Fence | 16 | 13,391 |
| Lightkeeper | 15 | 11,637 |
| BTR Driver | 15 | 12,156 |
| Ref | 15 | 8,468 |
| Artem | 23 | 11,004 |
| Painter | 12 | 6,141 |
| Badger | 4 | 1,853 |

**跳过 4 位零任务商人**（Storyteller / Croupier / Hideout Harry / Courier）：**不产出** `05-quests.md`（取舍已由工单 16 的 R6 写入 mod README）。

**与原侦察表的偏差（以实测为准）**
- **Ref**：`sptfriendlyquests` 的 6 条 id **已存在于原版任务表**，实为**覆盖（override）而非净增** → Ref = 15（原估算 21 有误）。
- **Skier = 74**（原估算 72）：Vagabond 的 `hideout_relocation` 亦指向 Skier（其 config 内 `newQuest.traderId` 直接可读，无需猜测）。
- **WTT-Armory 实际贡献 14 条**（原估算 12）。
- **文本降级（已知、不修）**：Artem / Vagabond / WTT-Armory 三源仅英文（脚本优雅降级并记录）；Mechanic 1 条 + Ref 9 条目标在 locale 中无文本被跳过（共 10 条）。

**未触发拆分**：最大文件 Mechanic 73,690 B，远低于 200 KB 阈值，故全部为单文件 `05-quests.md`。

## 生成器

`scripts/gen-quest-manuals.mjs`（Node ESM，UTF-8 无 BOM、LF、控制台 ASCII 安全、幂等）。
Flags：`--spt-data`、`--mods-root`、`--out`、`--no-mods`、`--split-bytes`（默认 204800）。
真值来源＝**任务表**（并合并 7 个 mod 源）；禁止反向从 locale 生成。

## 输出格式

mod 任务定义文件（exp-2 已定位）：
- Scorpion：`[4]新商人-毒蝎-acidphantasm-scorpion\...\data\quests\Scorpion_quests.json`（122；自带 `data\locales\zh-CN\ScorpionLocale.json`）
- Artem：`[4]新商人-Artem\...\WTT-Artem\db\CustomQuests\66bf757f27d0b097db0acea5\Quests\ArtemQuests.json`（23）
- Painter：`[4]新商人-油漆匠-Painter\...\Painter-4.0\db\CustomQuests\668aaff35fd574b6dcc4a686\Quests\painter.json`（12）
- Ref（sptFriendlyQuests）：`...\acidphantasm-reffriendlyquests\db\quests.json`（6；**文件内无 traderId**，由 DLL 绑定 Ref）
- WTT-Armory：`CustomQuests\{PRAPOR,MECHANIC,PEACEKEEPER,JAEGER}\Quests\*.json`（12）
- Epic's All in One：`CustomQuests\Badger\Quests\BadgerQuests.json`（5，含 Mechanic 1）
- Vagabond：`...\Vagabond\config\quests\*.json`（8，JSONC 带注释）

## 已知未知（实现前需处理或显式降级）

- [UNKNOWN] **Painter 任务文本载体**：mod 目录内未找到 locale 文件，文本疑似嵌入 `Painter-4.0.dll` / res。
- [UNKNOWN] **Vagabond 任务文本载体**未逐一定位。
- [UNKNOWN] **3 条当日生成的 locked 任务**（`6ad21662d9425e64404241a8` / `…b5` / `…c2`）来源不明（mods / SPT_Runtime / overwrite 全盘 grep 零命中）。
- **非任务孤儿键**：`664ca9f577af670dad0ad339`（水瓶座行动 - 2）**不是 mod 任务**，是 SPT 只同步了 locale 未同步模板的孤儿键，本包内不产生任务——生成脚本应**以任务表为准**，不反向从 locale 生成（否则会产出幽灵条目）。

## 输出格式

每位商人一个文件 `knowledge/traders/<traderId>/05-quests.md`，结构：

```markdown
## 水瓶座行动 - 1
- 类型：PMC 专属｜地点：海关
- 目标：
  - 在海关找到被藏在宿舍楼里的水
  - 以幸存状态撤离该区域
- 委托人原话（摘要）：<description 截断 ≤200 字>
```

一段一任务 → 现有段落/标题分块检索可按任务粒度命中。

## 验收清单

- [x] 生成脚本落库（`scripts/gen-quest-manuals.mjs`，与 `pack.ps1` 并列），**幂等已验证**（连续两次运行 15 文件 SHA256 全同，orchestrator 亲测）
- [x] 15 位商人各产出 `05-quests.md`；4 位零任务商人**不产出**（orchestrator 亲测确认无该文件）
- [x] 抽查：Therapist 手册含「水瓶座行动 - 1」（+「在海关找到被藏在宿舍楼里的水」）与「水瓶座行动 - 2」（+「在海关击杀Scav」），且与 `ch.json` 一致
- [x] 每文件体积可控：最大 Mechanic 73,690 B（< 200 KB 阈值），**未触发拆分**
- [x] 与 `pack.ps1` 协同：`knowledge/` 随包（stage 103 文件已含 15 份手册；zip 350,801 B）
- [x] 孤儿键防泄漏：`664ca9f577af670dad0ad339` 在 15 份文件中**零命中**（orchestrator 亲测）
- [ ] 部署后实机检索命中：问「水瓶座行动」→ `[kg] 来源=05-quests.md`
