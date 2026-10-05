# Coverage — SPT 5.x 任务文本提取（第一阶段）

> 由 `tools/extract_quests.py` 生成。本阶段只做数据提取与索引，不做剧情概括/翻译/创作。

## 1. 数据源与口径

- quests.json：`E:\Game\EFT_Offline\SPT_5xx\SPT_Runtime\SPT_Data\database\templates\quests.json`
- ch.json：`E:\Game\EFT_Offline\SPT_5xx\SPT_Runtime\SPT_Data\database\locales\global\ch.json`
- en.json：`E:\Game\EFT_Offline\SPT_5xx\SPT_Runtime\SPT_Data\database\locales\global\en.json`

- `chCharCount` = 任务 `localization['ch']` **全部键**字符数（任务级 + 条件级 + 残留键），与侦察基准同口径。
- `taskChCharCount` = 仅 8 个任务级后缀的字符数（见下表）。
- 全库 ch 字符总数：**232525**（基准 232525）；其中任务级 154020。
- 全库 en 字符总数：**579118**（基准 579118）。

## 2. 分类依据

- **主线/支线**：严格按 quest 对象的 `isStoryQuest` 布尔字段。`side` 字段全库恒为 `Pmc`，不用于分类。
- 主线 192 / 支线 594，合计 786。
- `notDisplayedQuest=True`：87；`secretQuest=True`：0。
- `side` 取值分布：{'Pmc': 786}。
- `type` 取值分布：{'Discover': 49, 'Completion': 259, 'PickUp': 169, 'Elimination': 112, 'Exploration': 76, 'Merchant': 22, 'Skill': 38, 'Standing': 1, 'Multi': 25, 'WeaponAssembly': 27, 'Loyalty': 7, 'Experience': 1}。

## 3. 任务级后缀非空计数（ch）

| 后缀 | ch 非空数 | en 非空数 |
|---|---:|---:|
| name | 519 | 519 |
| description | 507 | 507 |
| successMessageText | 513 | 513 |
| failMessageText | 44 | 44 |
| whileAvailableMessageText | 63 | 19 |
| acceptPlayerMessage | 14 | 14 |
| declinePlayerMessage | 10 | 10 |
| completePlayerMessage | 23 | 23 |

- 侦察基准声称 questNoteText 376。经核实：本版数据中 **questNoteText 不是任务级后缀**（`<qid> questNoteText` 键 0 个），376 实际是形如 `<前缀> questNoteText` 的键；且这些前缀在任何 quest 的条件中都不存在（全部为孤立键）。它们从内容看是叙事日志条目，已按任务保留在 `extras.orphanQuestNotes`。
- 其余后缀非空数与侦察基准完全一致：name 519 / successMessageText 513 / description 507 / whileAvailableMessageText 63 / failMessageText 44 / completePlayerMessage 23 / acceptPlayerMessage 14 / declinePlayerMessage 10。

## 4. 字符分布对照（ch）

### 4.1 主/支线

| 分类 | 任务数 | ch 字符 | 基准 ch 字符 | 一致 |
|---|---:|---:|---:|:--:|
| 主线 | 192 | 50022 | 50022 | 是 |
| 支线 | 594 | 182503 | 182503 | 是 |

### 4.2 按商人（ch 字符降序）

| traderId | 名称 | 任务数 | ch 字符 | 基准 ch 字符 | 一致 |
|---|---|---:|---:|---:|:--:|
| `5a7c2eca46aef81a7ca2145d` | Mechanic | 128 | 41533 | 41533 | 是 |
| `58330581ace78e27b8b10cee` | Skier | 71 | 27981 | 27981 | 是 |
| `54cb50c76803fa8b248b4571` | Prapor | 89 | 27901 | 27901 | 是 |
| `67f7af56c117b6140af2a607` | Player Trader | 140 | 22954 | 22954 | 是 |
| `5c0647fdd443bc2504c2d371` | Jaeger | 65 | 21168 | 21168 | 是 |
| `54cb57776803fa99248b456e` | Therapist | 57 | 20369 | 20369 | 是 |
| `5ac3b934156ae10c4430e83c` | Ragman | 65 | 19638 | 19638 | 是 |
| `5935c25fb3acc3127c3d8cd9` | Peacekeeper | 51 | 17797 | 17797 | 是 |
| `638f541a29ffd1183d187f57` | Lightkeeper | 40 | 8961 | 8961 | 是 |
| `656f0f98d80a697f855d34b1` | BTR司机 | 22 | 8527 | 8527 | 是 |
| `6617beeaa9cfa777ca915b7c` | 竞技场裁判 | 20 | 6738 | 6738 | 是 |
| `579dc571d53a0658a154fbec` | Fence | 19 | 6307 | 6307 | 是 |
| `688246518448b05efd61d461` | Kerman | 11 | 1966 | 1966 | 是 |
| `688246958448b05efd61d462` | Voevoda | 5 | 346 | 346 | 是 |
| `69e0d6cc77b63940375b9173` | 幸存者 | 2 | 282 | 282 | 是 |
| `68fe15990f29ba3fdbba9d55` | 无线电台 | 1 | 57 | 57 | 是 |

商人 ch 字符合计：232525（基准 232525）。

## 5. trader id -> 名称映射

| traderId | 中文名 (Nickname) | 英文名 | 来源 |
|---|---|---|---|
| `5a7c2eca46aef81a7ca2145d` | Mechanic | Mechanic | ch.json/en.json |
| `58330581ace78e27b8b10cee` | Skier | Skier | ch.json/en.json |
| `54cb50c76803fa8b248b4571` | Prapor | Prapor | ch.json/en.json |
| `67f7af56c117b6140af2a607` | - | Player Trader | database/traders/67f7af56c117b6140af2a607/base.json nickname（无 zh 本地化） |
| `5c0647fdd443bc2504c2d371` | Jaeger | Jaeger | ch.json/en.json |
| `54cb57776803fa99248b456e` | Therapist | Therapist | ch.json/en.json |
| `5ac3b934156ae10c4430e83c` | Ragman | Ragman | ch.json/en.json |
| `5935c25fb3acc3127c3d8cd9` | Peacekeeper | Peacekeeper | ch.json/en.json |
| `638f541a29ffd1183d187f57` | Lightkeeper | Lightkeeper | ch.json/en.json |
| `656f0f98d80a697f855d34b1` | BTR司机 | BTR Driver | ch.json/en.json |
| `6617beeaa9cfa777ca915b7c` | 竞技场裁判 | Ref | ch.json/en.json |
| `579dc571d53a0658a154fbec` | Fence | Fence | ch.json/en.json |
| `688246518448b05efd61d461` | Kerman | Mr. Kerman | ch.json/en.json |
| `688246958448b05efd61d462` | Voevoda | Voevoda | ch.json/en.json |
| `69e0d6cc77b63940375b9173` | 幸存者 | Survivor | ch.json/en.json |
| `68fe15990f29ba3fdbba9d55` | 无线电台 | Radio station | ch.json/en.json |

- 名称键格式：`<traderId> Nickname`（空则退 FullName/FirstName），中文取 ch.json，缺失退 en.json。
- `67f7af56c117b6140af2a607` 在 ch.json/en.json 中无任何名称键，名称取自 `database/traders/<id>/base.json` 的 nickname（Player Trader，无中文）。

## 6. 条件文本挂接

localization 中条件级键的三种形式：

- `<condId>`：条件短文本（目标描述），挂到条件对象 `text`。
- `<condId>_hint`：条件提示，挂到 `hint`。
- `<condId> questNoteText`：条件备注，挂到 `note`。

- 条件条目总数（含嵌套）：3322；其中挂接 ch 文本 1947，仅 en 文本 0。
- 残留键（进入 `extras`）共 731 个：
  - `orphanCondTexts`：355 个短文本/提示残留键（前缀不在本 quest 条件中）。
  - `orphanQuestNotes`：376 个 questNoteText。**经全库核对，全部 376 个 questNoteText 的前缀在任何 quest 的条件中都不存在**，无法自动挂接；从内容看它们是主线叙事日志条目，价值高，已按 quest 保留原文。
- 1 个裸键前缀在别的 quest 的条件中存在（跨 quest 打包遗留），其余不在任何条件中。

## 7. 数据质量

- ch 名称为空的任务：267 / 786；其中中英文名称都为空：267。
- 至少缺一项任务级中文的任务：783 / 786。
- en 兜底：`textsEn` 共 519 条（全部 name + 缺失中文的任务级后缀）；条件级仅在无 ch 时附 en（标记 `textSource='en'`，实际仅 0 条）。
- `missingCh` 逐任务列出缺失 ch 的任务级后缀。
- questNoteText 任务级缺失属结构正常（本版为条件级），不计入异常。

- 前置依赖边：454 条；反向后继一致性：全部闭合；指向不存在任务的前置：3 个（`6863e09a5f4d17fd3e01feee`, `686403eeb4aaef121c0f0f06`, `6a88255e8636c03a2d09ba37`，属已归档/移除任务）。

### 7.1 主/支线文本完整度（关键异常，下一阶段须知）

| 分类 | 任务数 | name 非空 | description 非空 | 缺 name | 缺 description |
|---|---:|---:|---:|---:|---:|
| 主线 | 192 | 12 | 0 | 180 | 192 |
| 支线 | 594 | 507 | 507 | 87 | 87 |

**结论（重要）**：本数据集中 **192 个主线任务全部没有 ch description**，其中 180 个连 name 也没有（中英文皆空）。主线任务的叙事文本不在 quests.json 的 localization 里，很可能位于 `dialogue.json` / `mainQuestNotes.json` / `tapes.json` 等文件；本阶段仅覆盖 quests.json，故主线叙事缺失属**源数据范围问题**，非提取错误。主线任务仍保留条件级目标文本（`conditions[*].text`）。
- 支线中缺 name/description 的 87 个任务与 `notDisplayedQuest=True` 的 87 个——即隐藏任务。
- 因此：后续“594 支线”可正常从本数据集蒸馏；“192 主线”的完整叙事需另行提取对话/日志文件。

## 8. 分片建议（下一阶段输入）

约束：商人不可切分；按 ch 字符 LPT 装箱，目标每片 40–60K。建议 5 片。

### 片 1：ch 48271 字符 | 任务 148（主线 32/支线 116）

- 商人：Mechanic、竞技场裁判
  - `5a7c2eca46aef81a7ca2145d` Mechanic：128 任务 / 41533 字符
  - `6617beeaa9cfa777ca915b7c` 竞技场裁判：20 任务 / 6738 字符

### 片 2：ch 45469 字符 | 任务 133（主线 20/支线 113）

- 商人：Skier、Lightkeeper、BTR司机
  - `58330581ace78e27b8b10cee` Skier：71 任务 / 27981 字符
  - `638f541a29ffd1183d187f57` Lightkeeper：40 任务 / 8961 字符
  - `656f0f98d80a697f855d34b1` BTR司机：22 任务 / 8527 字符

### 片 3：ch 45698 字符 | 任务 140（主线 23/支线 117）

- 商人：Prapor、Peacekeeper
  - `54cb50c76803fa8b248b4571` Prapor：89 任务 / 27901 字符
  - `5935c25fb3acc3127c3d8cd9` Peacekeeper：51 任务 / 17797 字符

### 片 4：ch 45243 字符 | 任务 224（主线 106/支线 118）

- 商人：Player Trader、Ragman、Kerman、Voevoda、幸存者、无线电台
  - `67f7af56c117b6140af2a607` Player Trader：140 任务 / 22954 字符
  - `5ac3b934156ae10c4430e83c` Ragman：65 任务 / 19638 字符
  - `688246518448b05efd61d461` Kerman：11 任务 / 1966 字符
  - `688246958448b05efd61d462` Voevoda：5 任务 / 346 字符
  - `69e0d6cc77b63940375b9173` 幸存者：2 任务 / 282 字符
  - `68fe15990f29ba3fdbba9d55` 无线电台：1 任务 / 57 字符

### 片 5：ch 47844 字符 | 任务 141（主线 11/支线 130）

- 商人：Jaeger、Therapist、Fence
  - `5c0647fdd443bc2504c2d371` Jaeger：65 任务 / 21168 字符
  - `54cb57776803fa99248b456e` Therapist：57 任务 / 20369 字符
  - `579dc571d53a0658a154fbec` Fence：19 任务 / 6307 字符

> 分片仅按字符量均衡，未做主题聚类；后续可按需在片内再按商人/区块细分。

## 9. 自检记录

脚本内置自检：固定随机种子 2077，从 786 个任务中抽 3 个，逐字段对照 quests.json 源文件（traderId/location/type/side/isStoryQuest/notDisplayedQuest/textsCh/chCharCount/prereqQuestIds）。

### 样例 `666314a50aa5c7436c00908a` — PASS

- 名称：旧情难却｜trader：5ac3b934156ae10c4430e83c｜主线：False｜ch 字符：200｜前置：['666314a31cd52e3d040a2e76']
- 检查项：traderId=OK、location=OK、type=OK、side=OK、isStoryQuest=OK、notDisplayedQuest=OK、textsCh=OK、chCharCount=OK、prereqQuestIds=OK
- description 摘要：之前和你一起开创全新装备产品线的合作真是愉快！仿佛回到了之前生意一天比一天火热、利润滚滚而来的时候，那会算得上是塔科夫的黄金时代。顺便一提，我突然想到，如果能改进一下剪裁方式，应该能让口袋装下更多东西。想让我先用你的裤子试试吗？那你要给钱的…

### 样例 `63987301e11ec11ff5504036` — PASS

- 名称：枪匠大师 - 8｜trader：5a7c2eca46aef81a7ca2145d｜主线：False｜ch 字符：296｜前置：['5b477f7686f7744d1b23c4d2']
- 检查项：traderId=OK、location=OK、type=OK、side=OK、isStoryQuest=OK、notDisplayedQuest=OK、textsCh=OK、chCharCount=OK、prereqQuestIds=OK
- description 摘要：来了个有意思的单子。这一回不是平常的单件，而是要了两把枪。不过以你的水平肯定能轻松拿捏：第一把是雷明顿 M700 狙击步枪，要用全套 AB Arms 套件改装枪身，配上高倍瞄准镜和一个消音器。人机功效不低于 35，至于总和后坐力，别高于 5…

### 样例 `5a68663e86f774501078f78a` — PASS

- 名称：医疗隐私 - 2｜trader：54cb57776803fa99248b456e｜主线：False｜ch 字符：522｜前置：['5a68661a86f774500f48afb0']
- 检查项：traderId=OK、location=OK、type=OK、side=OK、isStoryQuest=OK、notDisplayedQuest=OK、textsCh=OK、chCharCount=OK、prereqQuestIds=OK
- description 摘要：救护车的事情多亏了有你帮忙。我们的人刚好赶在 Scav 们之前到达了现场，至少他们还没有把所有东西都洗劫一空。在调查隧道倒塌后果的同时，我们找到了一辆 G 型越野车，以前 TerraGroup 的高管们经常坐着这些大 G 招摇过市。看样子除…

自检结果：**全部通过**。

幂等性：脚本输出使用确定性排序与固定缩进；对同一源数据连续运行两次，产物字节一致（SHA-256 前 16 位：quests_joined.json=27925849559d9958、quests_by_trader.json=72978fa28c841fb0、quests_index.md=ec8598601048ec96）。

（哈希在每次运行时重新计算并写入，故本行仅代表本次运行的产物指纹。）

## 10. 第二阶段：主线叙事提取

> 由 `tools/extract_main_story.py` 追加。第一阶段内容保持不变。

### 10.1 来源与方法

| 文件 | 文本存放方式 | 提取方法 |
|---|---|---|
| quests.json | `<noteId> questNoteText` 内嵌 localization | 直接读取 note 文本，主/英双取 |
| mainQuestNotes.json | 仅结构（id/chapterId/conditionIds/links） | 提供 note->章节/任务映射 |
| dialogue.json | `element.localization[lang][subtitleId]` | 按行展开字幕文本；`dialogueId` 关联任务 |
| tapes.json | 全局 locale 以 subtitleId 为键 | 拼接字幕；tape id 在 quests.json 中反查任务 |
| subtitleTracks.json | 全局 locale 以 subtitleId 为键 | 独立字幕轨，无任务引用 |
| endings.json | 全局 locale `<systemName>_name/_description/_caption/.consequence` | 结局文本 + 条件目标 |
| questChains.json | `elements{chainId:[questId]}` | 链结构；经核实为支线链 |
| archivedQuests.json | dict questId->quest | 确认悬空前置 |

### 10.2 覆盖统计（对比 192 主线）

- 主线任务总数：192
- 获得叙事文本（并集）：**138 / 192**；未覆盖 54
  - notes：83
  - dialogue：86
  - tapes：10
  - endings：2
- 未覆盖的 54 个主线任务中 52 个无名称，多为章节内的分步目标任务；其叙事可能由所属章节 notes 概括，但无直接文本引用。

### 10.3 关键发现

- mainQuestNotes 451 条，其中 364 条有文本；文本全部来自 quests.json 的 `<noteId> questNoteText` 键。第一阶段的 376 个孤儿 note 键对应唯一 id 364 个（部分 note 在多个 quest 中重复）。
- note 与任务的映射优先用 `conditionIds` 解析到具体任务，否则退回 `chapterId`；185 条含 conditionIds 的 note 中 169 条可解析到任务，其余因 condition id 不在 quests.json 条件集合内而退回章节。
- dialogue 文本不在全局 locale，而在 `element.localization`；quests.json 有 116 个 quest 带 dialogueId，全部命中 dialogue 元素（其中 86 个为主线）。
- questChains 共 26 链，含 226 个任务，但**主线任务数为 0**——是支线链，不能作为主线分组依据。
- endings 文本键为 `<systemName>_name/_description/_caption/.consequence`，共 4 个结局。

### 10.4 异常与未解决

- 未获得叙事文本的主线任务 54 个（见 `data/main_story_index.md` 清单）。
- 无文本 note 87 条（mainQuestNotes 中无对应 questNoteText）。
- 未被任何 quest 直接引用的对话元素 84 个；其中从 quest-linked 元素经 SwitchDialog 可达 32 个。
- 悬空前置 3 个：
  - `6863e09a5f4d17fd3e01feee`：在 quests/archived/chains 中均不存在，无对应任务。
  - `686403eeb4aaef121c0f0f06`：在 quests/archived/chains 中均不存在，无对应任务。
  - `6a88255e8636c03a2d09ba37`：作为 quest 成员出现在 questChains 中，但 quests.json 无该任务对象（已归档/移除的链成员）。
- subtitleTracks 共 42 条，均无任务引用。

### 10.5 自检

抽查引用与源文件一致性：

- note：PASS — quests.json#68cc0f9ddabf984a73078e49（owner 6895bf0872e2151e9e0a59fa）文本一致=True
- dialogue：PASS — dialogue.json#67d1897a20a895fa2a19b3d3 抽查 3 行，文本不一致 0
- tape：PASS — tapes.json#68889451ad1e91bfa40db8fd 首字幕对照 ch.json 一致=True
- subtitleTrack：PASS — subtitleTracks.json#68ab1fe9c37233de4e4d5157 首字幕对照 ch.json 一致=True
- ending：PASS — endings.json#YouDidntEscapeFromYourself name 对照 ch.json 一致=True
- note结构：PASS — mainQuestNotes#68cc0f9ddabf984a73078e49 chapterId/conditionIds 一致=True
- 自检结论：全部通过。

产物：`data/main_story_extract.json`、`work/main-story-text.md`、`data/main_story_index.md`。

## 11. 联网回补与并入 lore（2026-10-05）

- 第二轮官方来源核验（`research/verification-round2.md`，通道：Fandom MediaWiki API + 官方论坛 patch notes）：4 处 `[未验证]` 全部替换为可溯源表述——SEZ 法律地位与封锁（官方认可 wiki 引游戏内描述）；Scav 帮派命名与 Cultists 设定（Witnesses of the Arrival、祭司 Zhrets、22:00-7:00、850 HP）；时间线仅保留可证实锚点（1999 USEC 成立、2004 与 TerraGroup 建立联系），其余年份标注「社区整理推测，未见官方年份」；章节顺序修正为「10 章、非严格线性」；四结局官方名 Savior / Debtor / Survivor / Fallen；Ground Zero 称谓双源并存。
- 终稿（162,021 字符）并入 KB：`knowledge/spt-kb/curated/lore/13-quest-text-distillation.md`（含 frontmatter 共 162,237 字符）。
- KB 维护动作：lore README 阅读顺序与来源表更新；`sources/third-party.md` 增补 Battlestate 官方论坛通道；`sync-index.mjs --write` 注册条目（total 224→225，generated 2026-09-28→2026-10-05），`validate-index.mjs` 契约校验通过（exit 0）。
- 引用终验：709 处游戏数据引用 0 缺失；`[未验证]` 残留仅 1 处（导言中的格式定义句）。
