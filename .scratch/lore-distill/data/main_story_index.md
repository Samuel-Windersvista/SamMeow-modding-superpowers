# 主线叙事提取 — 索引与覆盖统计（第二阶段）

> 由 `tools/extract_main_story.py` 生成。

## 覆盖总览

- 主线任务总数：**192**
- 获得叙事文本（并集）：**138 / 192**
- 仍未覆盖：**54**

| 来源 | 覆盖主线任务数 |
|---|---:|
| mainQuestNotes（笔记） | 83 |
| dialogue（对话） | 86 |
| tapes（音频日志） | 10 |
| endings（结局） | 2 |

## 各文件贡献

| 文件 | 条目/计数 | 说明 |
|---|---|---|
| archivedQuests.json | archived=6 | dict questId->quest；含 QuestName 与旧条件结构 |
| dialogue.json | elements=195, withChLoc=193, questLinked=111, subtitleIds=4856 | elements[]；文本在 element.localization[lang][subtitleId] |
| endings.json | endings=4 | elements[]；文本在全局 locale，键 <systemName>_name/_description/_caption 与 <systemName>.consequence |
| mainQuestNotes.json | notes=451, distinctChapterId=10, withConditionIds=185, withText=364 | list[{id,links,chapterId,conditionIds}]；文本不在此文件 |
| questChains.json | chains=26, questsInChains=226, storyInChains=0 | elements{chainId:[questId]}（经核实为支线链，0 主线） |
| quests.json | quests=786, storyQuests=192 | note 文本以 <noteId> questNoteText 内嵌于 localization |
| subtitleTracks.json | tracks=42 | list[{id,subtitles[]}]；文本在全局 locale |
| tapes.json | tapes=18, withText=18 | list[{id,subtitles[]}]；文本在全局 locale 以 subtitleId 为键 |

## 主线章节（10 章）与笔记数

| chapterId | 章节名 | 笔记数（有文本） |
|---|---|---:|
| `68da33fe00868edcb6025ac4` | 门票 | 81/140 |
| `69d38381cea4b428690ea1d9` | Boreas | 56/75 |
| `68da36cf7cff54fc6109874a` | Batya | 48/49 |
| `6903d779fdfc4078740a4bd0` | 他们已经来了 | 41/43 |
| `6900927ab7d28358f80b9421` | 无名者 | 35/37 |
| `69052e18e680c2d3e3034d3a` | 意外证人 | 28/30 |
| `68cbd33676fe74b1e80bfd91` | 塔科夫之旅 | 25/26 |
| `68cbcdc4c964ab83cc0c928e` | 陨落星辰 | 25/26 |
| `68e3a35002661eb2d30ce387` | 探秘"迷宫" | 14/14 |
| `68e784b7fa3f1fa3770094ba` | 神秘蓝焰 | 11/11 |

## 未覆盖主线任务清单（54）

- `67b892b6b4c09aae5309c275` (无名称)
- `67b892ba0e9aad30720bfc56` (无名称)
- `67b892bb1c013f0fcb058246` (无名称)
- `67bc7abc7801bf5c41017b68` (无名称)
- `67bc9d70adb794ecb40f5755` (无名称)
- `67bdafa17c1ef7a64804319c` (无名称)
- `67ced00fd47d32692302aca6` (无名称)
- `67ced012152372f12708b7b6` (无名称)
- `67ced01bc23108bd9e031725` (无名称)
- `68588ed156fb54c7e00e4318` (无名称)
- `68588ed2b2a45312020df028` (无名称)
- `68588ee109509e9fa1045b38` (无名称)
- `6895bbb0e7dac53c7c08797b` (无名称)
- `68bdb25ad9e4342bb10c3449` (无名称)
- `68bdb3b00deb8afba70216bd` (无名称)
- `68c02056b4000f84a4026e06` (无名称)
- `68c6a4ece3e3d7f69d0a5bd9` (无名称)
- `68c6a8551dfbf3fb78018562` (无名称)
- `68caba3606c31ae60602980f` (无名称)
- `68caba3985c14c12720e9287` (无名称)
- `68caba4119f13a8d1b0ad3ac` (无名称)
- `68cc07e96a6359b02109da18` (无名称)
- `68cd7d6d9510d63fdb05a76a` (无名称)
- `68d5be85d12284307b08175e` (无名称)
- `68d6894a05ab6c954c01f65e` (无名称)
- `68d9b97c42007d9a0a0f901c` (无名称)
- `68d9b97ef19ccd1e51002c80` (无名称)
- `68dbf867ff23e2cbe2014c02` (无名称)
- `68dfa85efdedf14d640a6ee0` (无名称)
- `68e19fee596a40531902e73c` (无名称)
- `68e2d8d52a2449612b06ac9b` “门票” - 目标：打开加固手提箱
- `68e2ecfeb88d405a420774f5` 获得逃离塔科夫的门票
- `68e4cf5136a9240a3704e777` (无名称)
- `68e6a5b42a3fa47e9100c196` (无名称)
- `68e79a92aded99fd1a06023c` (无名称)
- `68e90a73a3d110355b03e3a2` (无名称)
- `68e9156369a053d132045c56` (无名称)
- `68ec5135459f4ca1d60c601d` (无名称)
- `68ec5137062b3169d009eaf9` (无名称)
- `68ec64b49012da9779025825` (无名称)
- `68ee523e54da3fa9aa01af75` (无名称)
- `68f16d504a7305f74f0c746b` (无名称)
- `68f4d98bd2016ca1a60461a4` (无名称)
- `68fbdbae729bd58bc80974f1` (无名称)
- `68ffdaf2853a782081039a16` (无名称)
- `69009349b7d28358f80b942c` (无名称)
- `69125a2fec72e1b67a0ace56` (无名称)
- `6914d59b1bd36f988e0d95bc` (无名称)
- `6917c6efa59476a52c02e149` (无名称)
- `69247ccc803723e83c0439fd` (无名称)
- `6924820dbf8cf5498b05dac0` (无名称)
- `69c13a003ee6585df2057a39` (无名称)
- `6a05dfd31681410e9e0b9afc` (无名称)
- `6a05e47ec8c57a62940cd74a` (无名称)
