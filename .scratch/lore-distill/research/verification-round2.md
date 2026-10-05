# 塔科夫剧情统一蒸馏 — 第二轮定点核验报告

> 任务：对终稿中 5 处待核断言做定点核验 / 回补。  
> 核验时间：2026-10-05  
> 来源优先级：escapefromtarkov.com 官网 > Battlestate 官方论坛 patch notes > 官方认可 wiki（Fandom）。  
> 标注规则：每条结论后附可核对 URL 与引文片段；无法核验的明确说明。

---

## 网络可达性汇总（本轮）

| 来源 | 状态 | 说明 |
|------|------|------|
| `escapefromtarkov.com/?page=about` | 200 但无 SSR 文本 | Nuxt SPA，页面内容完全由 JS 渲染，webfetch 无法提取 lore 文本。 |
| `escapefromtarkov.com/?page=game` | 同上 | 同上。 |
| `escapefromtarkov.fandom.com` 普通页面 | 403 | 直接访问 HTML 被拦截。 |
| `escapefromtarkov.fandom.com/api.php` | 200 | 通过 MediaWiki API 可成功获取页面源码文本（本次主要突破口）。 |
| `forum.escapefromtarkov.com` | 200 | 可访问官方 patch notes 帖子。 |
| `forum.escapefromtarkov.com/search` | 403 | 无法使用论坛搜索。 |
| YouTube / Wayback / X / Wikipedia | 不可达 | 与第一轮相同。 |

结论：本轮主要通过 **Fandom MediaWiki API** 获取了官方认可 wiki 的完整文本，可对第一轮中无法核验的多处断言进行回补。

---

## 一、Norvinsk 特别经济区（SEZ）的命名与法律地位

### 原断言
> 「俄罗斯西北部、以税收优惠吸引跨国资本、因企业丑闻与武装冲突封锁」

### 核验结论
**部分证实，措辞需要调整。**

官网 about / game 页由于 SSR 关闭，无法提取原始文本。但官方认可 wiki（Fandom）的 `Norvinsk region` 页面直接引用了游戏内描述，并补充了背景正文，足以支撑以下结论：

1. **法律地位**：Norvinsk SEZ 是 "a limited territory within Norvinsk region ... possessing a special legal status in relation to the rest of the Russia and privileged economic conditions for national and foreign enterprises"。  
2. **门户地位**：位于 "a gateway between Russia and Europe"。  
3. **吸引资本**："Preferential conditions for large international companies ... attracted law-abiding businesses, but corporations of dubious intent as well"。  
4. **丑闻与冲突**：Tarkov 是 region 内最大城市之一，"a transatlantic corporation became the ground zero of a political scandal. Six months later, the political standoff escalated into an armed conflict involving UN peacekeepers, Internal Troops of Ministry of Internal Affairs, and two private military companies"。  
5. **封锁**："The region's borders were sealed off, and those trapped in the middle of flaring up local warfare were isolated from the outside world"。

### 替换措辞建议
> 「Norvinsk 特别经济区是俄罗斯与欧洲之间的门户地带，包含 Norvinsk 与 Tarkov 两城及其附属基础设施；该区域在俄罗斯境内享有特殊法律地位，并为国内外企业提供优惠经济条件。优惠条件既吸引了守法企业，也引来了动机可疑的跨国公司。Tarkov 的一家跨大西洋公司成为政治丑闻的 ground zero，六个月后政治对峙升级为涉及联合国维和部队、内务部内卫部队及两家 PMC 的武装冲突，区域边界随后被封锁。」

### 来源
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — Norvinsk region  
  URL: `https://escapefromtarkov.fandom.com/wiki/Norvinsk_region`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=Norvinsk_region&prop=text&format=json`

### 证明性引文
```
A limited territory within Norvinsk region, including the cities of Norvinsk and Tarkov 
with adherent objects of infrastructure, possessing a special legal status in relation 
to the rest of the Russia and privileged economic conditions for national and foreign 
enterprises.
—In game description
```

```
The events of Escape from Tarkov take place in the fictional Norvinsk region Special 
Economic Zone that became a gateway between Russia and Europe. Preferential conditions 
for large international companies, however, have not only attracted law-abiding 
businesses, but corporations of dubious intent as well. In Tarkov, one of the largest 
cities of the region, a transatlantic corporation became the ground zero of a political 
scandal. Six months later, the political standoff escalated into an armed conflict 
involving UN peacekeepers, Internal Troops of Ministry of Internal Affairs, and two 
private military companies. The region's borders were sealed off, and those trapped in 
the middle of flaring up local warfare were isolated from the outside world.
```

> [未验证] 官网 `?page=about` 仍无法静态抓取，因此未能与官网原文逐字核对；上述引文来自官方认可 wiki 引用的游戏内描述。

---

## 二、时间线具体年份

### 原断言
> 2004 USEC 与 TerraGroup 建立雇佣关系；2009 化工厂 16 号土地非法出售；2015-2018 SEZ 繁荣；2017-2018 BEAR 组建；2018-2019 丑闻与对峙；2019-2020 武装冲突；约 2021 封锁。

### 核验结论
**仅 1999、2004 可证实；其余年份无法核验，建议删除或明确标注为社区推测。**

- **USEC 成立 1999**：官方认可 wiki USEC 页面："established in 1999 after merger of two companies: KerniSEC and Safe Sea"。
- **USEC 与 TerraGroup 2004 建立雇佣关系**：官方认可 wiki USEC 页面："In 2004, an agent of Terra Group international holding made contact with USEC, which have consecutively become, essentially, a private army of the holding"。
- **BEAR 组建**：官方认可 wiki BEAR 页面："established by a secret decree of the Russian Federation Government as a countermeasure against illegal activities of the TerraGroup in the Russian territory"，但未给出具体年份。
- **其他年份（2009 / 2015-2018 / 2017-2018 / 2018-2019 / 2019-2020 / ~2021）**：在官方认可 wiki 的 Norvinsk region、USEC、BEAR、TerraGroup、Ground Zero、Factory 等页面中均未找到对应年份表述。
- **官方刻意模糊时间**：Fandom 页面没有提供精确日历，Norvinsk region 页面仅使用 "Six months later" 这种相对时间表述。

### 替换措辞建议
> 终稿中时间线应仅保留可证实节点：「USEC 于 1999 年由 KerniSEC 与 Safe Sea 合并成立；2004 年 TerraGroup 国际控股与 USEC 建立联系，后者实质成为该集团的私人军队」。其余年份若需保留，必须标注「社区整理，未见官方年份」或删除。

### 来源
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — USEC  
  URL: `https://escapefromtarkov.fandom.com/wiki/USEC`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=USEC&prop=text&format=json`
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — BEAR  
  URL: `https://escapefromtarkov.fandom.com/wiki/BEAR`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=BEAR&prop=text&format=json`

### 证明性引文
```
The USEC private military company was established in 1999 after merger of two companies: 
KerniSEC and Safe Sea. In 2004, an agent of Terra Group international holding made contact 
with USEC, which have consecutively become, essentially, a private army of the holding, 
with offices all around the world and over 7500 strong of staff.
```

```
The BEAR private military company was established by a secret decree of the Russian 
Federation Government as a countermeasure against illegal activities of the TerraGroup in 
the Russian territory. This Russian PMC comprises ex-special forces officers from all over 
former soviet countries.
```

> [无法核验] 2009 / 2015-2018 / 2017-2018 / 2018-2019 / 2019-2020 / ~2021 等年份在本次采集中未找到任何官方或官方认可 wiki 的逐句对应。建议终稿删除或降级为「社区推测时间线」。

---

## 三、Scav 帮派命名与 Cultists 设定

### 原断言
> 四大帮派 + 两独特帮派；Cultists 夜间活动等设定。

### 核验结论
**Scav 帮派命名与 Cultists 核心设定均可通过官方认可 wiki 证实。**

#### Scav 帮派
官方认可 wiki `Scavs` 页面列出四个主要武装组织：
- **Grizzle** armed group
- **Pashutin (Boris Semyonovich)** armed group，绰号 **Fiend**
- **Zhilnov (Yaroslav Grigoryevich)** group，绰号 **Yarik / Yaga**
- **Stoporenko** group，别名 **Lawyer**

以及两个独特帮派：
- **Svetloozersk Gang**，由 **Shturman** 领导
- **Zavodskoy Gang**，由 **Reshala** 领导

#### Cultists
官方认可 wiki `Cultists` 页面确认：
- 正式名称：**Witnesses of the Arrival**
- 外观：戴兜帽，夜间活动，"conducting rituals and gathering in marked rooms at night"
- 组成：一名祭司（priest，又称 **Zhrets**）与 2-4 名战士（warrior，又称 **Sektant**）
- 活动时间：**22:00 - 7:00**
- 武器：使用毒刀（"poisoned cultist knife"）
- 祭司总血量：**850 HP**

### 来源
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — Scavs  
  URL: `https://escapefromtarkov.fandom.com/wiki/Scavs`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=Scavs&prop=text&format=json`
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — Cultists  
  URL: `https://escapefromtarkov.fandom.com/wiki/Cultists`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=Cultists&prop=text&format=json`

### 证明性引文
```
Grizzle armed group
Pashutin (Boris Semyonovich) armed group (Fiend)
Zhilnov (Yaroslav Grigoryevich) group (Yarik, Yaga)
Stoporenko group (alias Lawyer)
Svetloozersk Gang, led by Shturman
Zavodskoy Gang, led by Reshala
```

```
Witnesses of the Arrival, the hooded figures conducting rituals and gathering in marked 
rooms at night. ... The priest and his 2-4 followers have different health values than PMCs 
and Scavs. ... Cultists will lay prone in groups of 3-5 ... with knives out waiting to ambush 
the player. ... The Cultists spawn between 22:00 and 7:00. ... Cultist Priest's Health ... Total: 850
```

---

## 四、主线章节顺序与「门票」四结局触发条件

### 原断言
> 9 章 + 终局顺序；门票四结局触发条件。

### 核验结论
**章节列表可证实，但「严格线性顺序」应修正为「多章节并行、部分章节有前置关系」；四结局名称与核心触发条件可证实。**

#### 主线章节（Story Chapters）
官方认可 wiki `Story chapters` 页面列出 10 个章节：
1. Accidental Witness（意外证人）
2. Batya
3. Blue Fire（神秘蓝焰）
4. Boreas
5. Falling Skies（陨落星辰）
6. The Labyrinth（探秘“迷宫”）
7. The Ticket（门票）
8. The Unheard（无名者）
9. They Are Already Here（他们已经来了）
10. Tour（塔科夫之旅）

注意：这是 wiki 表格列出的顺序，**并非官方给出的剧情推进顺序**。

#### 章节前置关系（官方认可 wiki 各页面 `Related quests` / `Requirements`）
- **Tour**："This chapter gets automatically added to the quest list upon starting the game" —— 开局自动获得。
- **Falling Skies**：前置为 Tour（"Ask Mechanic about the downed plane while progressing through the story chapter Tour"），后续通向 The Ticket（"Leads to: The Ticket"）。
- **The Ticket**：前置为 Falling Skies（"Previous: Falling Skies"），后续通向 Endings（"Leads to: - Endings flowchart"）。
- 其他章节（Accidental Witness / Batya / Blue Fire / Boreas / The Labyrinth / The Unheard / They Are Already Here）与 The Ticket 之间未见简单的线性前置标注，而是通过任务物品 / 商人对话 / 地图探索触发。

#### 四结局名称
官方认可 wiki `Endings` 页面确认：
- **Savior**（中文常译「为了全人类」）
- **Debtor**（中文常译「灯塔」—— 奖励含 Lighthouse poster / Armband (Lighthouse)）
- **Survivor**（幸存者）
- **Fallen**（中文常译「堕入黑暗」）

#### 四结局核心触发条件（基于官方认可 wiki `The Ticket` 页面）
关键分支点位于 **Falling Skies** 末尾的 "armored case"（加固手提箱）处置：

| 结局 | Falling Skies 选择 | The Ticket 关键选择 |
|------|-------------------|-------------------|
| **Savior** | 保留箱子（Keep the armored case） | 接受并帮助 Mr. Kerman 追查 TerraGroup 黑料 |
| **Debtor** | 把箱子交给 Prapor（Hand over the armored case） | 接受 Mr. Kerman 的帮助并走 Lightkeeper 路线 |
| **Survivor** | 保留箱子（Keep the armored case） | 拒绝 Mr. Kerman 的帮助，走 Prapor 路线（交 5 亿卢布；若 Falling Skies 已交箱子则为 3 亿） |
| **Fallen** | 把箱子交给 Prapor（Hand over the armored case） | 在 The Ticket 中拒绝帮助 Kerman / 走 Prapor 黑暗路线 |

Falling Skies 页面明确标注："This is an important point of no return choice!"

### 替换措辞建议
> 「1.0 时代主线由 10 个 Story Chapters 组成（Tour / Falling Skies / They Are Already Here / Blue Fire / Accidental Witness / Batya / The Labyrinth / The Unheard / Boreas / The Ticket）。其中 Falling Skies 前置 Tour，The Ticket 前置 Falling Skies 并通向四种结局；其余章节通过任务物品、商人对话与地图探索并行触发。四结局为 Savior、Debtor、Survivor、Fallen，核心分支取决于 Falling Skies 末尾是否将 armored case 交给 Prapor，以及 The Ticket 中是否接受 Mr. Kerman 的帮助。」

### 来源
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — Story chapters  
  URL: `https://escapefromtarkov.fandom.com/wiki/Story_chapters`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=Story_chapters&prop=text&format=json`
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — Falling Skies  
  URL: `https://escapefromtarkov.fandom.com/wiki/Falling_Skies`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=Falling_Skies&prop=text&format=json`
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — The Ticket  
  URL: `https://escapefromtarkov.fandom.com/wiki/The_Ticket`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=The_Ticket&prop=text&format=json`
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — Endings  
  URL: `https://escapefromtarkov.fandom.com/wiki/Endings`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=Endings&prop=text&format=json`

### 证明性引文
```
Story chapters in Escape from Tarkov are multistage quests that let players dive deep into 
the story and ultimately escape from Tarkov with one of four endings.
```

```
Keep the armored case for yourself or hand it over to Prapor
This is an important point of no return choice! The rewards differ depending on the decision.
Decision: Keep the armored case ... Unlocks achievement Just Business
Decision: Hand over the armored case directly to Prapor ... Unlocks achievement Man of His Word
Decision: Pretend you didn't find the armored case, but still hand it over to Prapor ... 
Unlocks achievement Man of His Word
```

```
Contact Mr. Kerman ... This is an important point of no return choice! ... Saying you will 
help Mr. Kerman will end this ending's route. [对应 Survivor/Fallen 分支]
```

```
The Endings system in Escape from Tarkov represents the final progression stage within the 
game's storyline, allowing players to reach one of four endings based on their decisions 
throughout the story chapters. ... Savior ... Debtor ... Survivor ... Fallen
```

> [未验证] 官方 patch notes 或官网公告中对 Story Chapters 系统的具体说明仍极有限；上述章节列表与结局触发细节来自官方认可 wiki 对游戏内任务文本的整理。

---

## 五、Ground Zero 内 TerraGroup 设施的官方称谓

### 原断言
> KB 记「总部」，另有资料作「俄罗斯分公司办公室」。

### 核验结论
**两种说法均有来源，但来源层级与措辞不同，建议终稿同时引用并说明差异。**

1. **游戏内 / 官方认可 wiki 用 "headquartered"**：  
   Fandom `Ground Zero` 页面直接引用游戏内描述："This is where TerraGroup was headquartered."。
2. **官方 patch notes 用 "main Russian branch office"**：  
   0.14.0.0 patch notes 称 Ground Zero 中心是 "the main Russian branch office of TerraGroup, where the original conflict began"。

差异解释："headquartered" 可能是游戏内对玩家呈现的简化说法（即 TerraGroup 在塔科夫地区的总部）；"main Russian branch office" 是官方 patch notes 对同一建筑的更精确企业法称谓。

### 替换措辞建议
> 「Ground Zero（中心区）位于塔科夫市中心，官方 patch notes 称其为 TerraGroup 的 'main Russian branch office'（俄罗斯主要分公司办公室），而游戏内描述与官方认可 wiki 则记为 'where TerraGroup was headquartered'（TerraGroup 总部所在地）。两种表述指向同一设施，可并存使用。」

### 来源
- 【官方】Escape from Tarkov 官方论坛 — Patch notes for 0.14.0.0  
  URL: `https://forum.escapefromtarkov.com/topic/176591-patch-notes-for-01400/`
- 【官方认可 wiki】Escape from Tarkov Wiki (Fandom) — Ground Zero  
  URL: `https://escapefromtarkov.fandom.com/wiki/Ground_Zero`  
  API: `https://escapefromtarkov.fandom.com/api.php?action=parse&page=Ground_Zero&prop=text&format=json`

### 证明性引文
```
The Ground Zero location, situated in the city center of Tarkov, has been added to the game. 
... In the very center of the location is the main Russian branch office of TerraGroup, 
where the original conflict began.
— Patch notes for 0.14.0.0
```

```
This is where TerraGroup was headquartered.
— Ground Zero in-game description / Fandom
```

---

## 六、综合建议（供终稿采纳）

| 原断言 | 建议处理 |
|--------|----------|
| Norvinsk SEZ 法律地位与封锁 | **保留**，改用 Fandom 引用的游戏内描述，标注来源为「官方认可 wiki 引游戏内文本」。 |
| 时间线具体年份 | **大幅删减**：仅保留 1999 / 2004；其余年份删除或标注「社区推测，未见官方年份」。 |
| Scav 帮派 + Cultists 设定 | **保留**，来源改为官方认可 wiki，补充正式名称 Witnesses of the Arrival 与血量 850 等细节。 |
| 主线章节顺序与四结局 | **修正**：10 章存在，但顺序非严格线性；四结局名称用 Savior / Debtor / Survivor / Fallen，触发条件以 Falling Skies 箱子选择 + The Ticket Kerman 选择为核心。 |
| Ground Zero TerraGroup 称谓 | **并存**：patch notes 用 "main Russian branch office"，游戏内用 "headquartered"，两者指向同一设施。 |

---

## 七、本轮新增来源索引

| 来源 | 类型 | URL |
|------|------|-----|
| Fandom — Norvinsk region | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/Norvinsk_region` |
| Fandom — USEC | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/USEC` |
| Fandom — BEAR | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/BEAR` |
| Fandom — Scavs | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/Scavs` |
| Fandom — Cultists | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/Cultists` |
| Fandom — Story chapters | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/Story_chapters` |
| Fandom — Falling Skies | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/Falling_Skies` |
| Fandom — The Ticket | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/The_Ticket` |
| Fandom — Endings | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/Endings` |
| Fandom — Ground Zero | 官方认可 wiki | `https://escapefromtarkov.fandom.com/wiki/Ground_Zero` |
| 官方论坛 — Patch 0.14.0.0 | 第一方 | `https://forum.escapefromtarkov.com/topic/176591-patch-notes-for-01400/` |
