---
version: [4.1]
domain: both
topic: troubleshooting
title: "MO2 fork 的 SPT 版本误判（4.1.x → 4.0.x）：looksValid 谓词等价根因与修复（2026-10-12 实证）"
keywords: [MO2, SPT4.1, looksValid, gameName, basic_games, sptvfsbridge.bat, SPT_Runtime, 版本误判, fail-closed, 互斥谓词, core.json, compatibleTarkovVersion, game_spt4, game_spt41, game_spt5, spt_diagnose, KEY_LAYOUT, Browse, 实例重建]
summary: "MO2 fork 把 SPT 4.1.x 实例识别为 SPT 4.0.x，启动服务器报「Windows 找不到文件」；机制链为版本识别不发生在插件内（MO2 选插件三路径：ini gameName 全等匹配 / Browse 与缺 gameName 兜底均取枚举序首个 looksValid 命中），而 SPT4/SPT4.1/SPT5 三个 def 的 looksValid 默认谓词完全等价（都只查 GameBinary EscapeFromTarkov.exe 是否存在），叠加枚举文件名序与自然序双偏置均指向 SPT4，非当前的 managed game executables() 每次重写 sptvfsbridge.bat 为 4.0 布局（spt_dir=SPT），4.1 安装只有 SPT_Runtime 故服务器起不来；历史根因是 2026-08-05 961c0ab 的正解版本探测 looksValid 在同日 24b5618 拆分为逐版本插件时被整体删除；修复方案 A 为三个 def 各覆写互斥版本感知 looksValid（4.0 要求 SPT\SPT.Server.exe；4.1 要求 SPT_Runtime\SPT.Server.exe 且 core.json 主版本==0；5.x 同布局且主版本>=1，缺 JSON/解析失败 fail-fast 返回 False），配套 spt_diagnose.py 一致性校验与 CHANGELOG 回归警示；独立复验四项全绿（部署 SHA256 4/4、AST 12/12、离线谓词矩阵三树各 8/8、diagnose 6/6，且同 harness 对修复前源码 8/8 全红）；预防为恢复互斥判别 + 操作规约「创建 SPT 实例手选版本勿 Browse 盲选」+ 发布说明标注 strict 谓词让历史错配显式暴露。"
---
# MO2 fork 的 SPT 版本误判（4.1.x → 4.0.x）：looksValid 谓词等价根因与修复（2026-10-12 实证）

> 状态：已验证（方案 A 于 2026-10-12 实施并通过独立复验；Overseer 同日实测各 SPT 版本识别正确，验收通过）
> 涉及 fork 仓库：`E:\云文件\GitHub\SamMeow-Tarkov-specific-Mod-Organizer`；现场实例：`E:\Game\EFT_Offline\SPT-Organizer-v2.5.2-Novinsk`（base `Norvinsk_Forever`）。

## 症状

- MO2 实例把 SPT 4.1.x 安装识别为 SPT 4.0.x（插件选中 `SPT4` 而非 `SPT4.1`）。
- 以该实例启动服务器报「Windows 找不到文件」——`sptvfsbridge.bat` 指向不存在的 `SPT\`。
- live 实例状态**有时正确**（走 ini `gameName` 精确匹配路径，日志 `using game plugin 'SPT4.1'`），
  所以表现为「看起来时好时坏」，极易漏诊；实例重建 / "Browse..." / 缺 `gameName` 兜底任一发生即复发。
- 严重度：**结构性缺陷**，非偶发。

## 根因机制链（三路径选插件 / 谓词等价 / 双排序偏置 / bat 重写 / 历史回退）

### 1. 版本识别不发生在插件里：MO2 选插件三条路径

| 路径 | 代码 | 行为 |
|---|---|---|
| 正常启动（ini 有 `gameName`） | `instancemanager.cpp:250-288` | `gameName` 全等匹配（CaseInsensitive，无前缀/模糊）；`SPT4.1` → `game_spt41`，正确 |
| 实例创建对话框 "Browse..." | `createinstancedialogpages.cpp:364-379` | 按列表序取**首个 `looksValid` 命中** |
| ini 缺 `gameName` 兜底 | `instancemanager.cpp:289-306` | 同取**首个 `looksValid` 命中**并写回 `gameName` |

### 2. 缺陷核心：`looksValid` 谓词等价
- 默认实现只查 `GameBinary` 是否存在（`basic_game.py:624-625`）：`return directory.exists(self.binaryName())`。
- 五个 SPT 插件**均未覆写** `looksValid`，而三者的 `GameBinary` 同为 `EscapeFromTarkov.exe`：

| 插件 | GameName | GameBinary | 对 `SPT_41x` 目录判定 |
|---|---|---|---|
| `game_spt3` | `SPT3` | `SPT.Launcher.exe`（根） | False（4.1 根无此文件） |
| `game_spt4` | `SPT4` | `EscapeFromTarkov.exe` | **True（等价，先命中）** |
| `game_spt41` | `SPT4.1` | `EscapeFromTarkov.exe` | True（等价） |
| `game_spt5` | `SPT5` | `EscapeFromTarkov.exe` | True（等价） |
| `game_sptaki` | `SPT AKI` | `Aki.Launcher.exe`（根） | False |

- → `SPT4` / `SPT4.1` / `SPT5` 三者谓词**完全等价**；`SPT3` 与 `SPT AKI` 的 GameBinary 与 4.x 布局**天然互斥**，不参与本缺陷。

### 3. 顺序双偏置：为什么固定是 SPT4
- 注册/枚举：`basic_games/__init__.py` 用 `glob.glob(games/*.py)` 逐文件 import（无排序）→ 文件名序 `spt3 < spt4 < spt41 < spt5` → 首个命中 `game_spt4`。
- Browse 显示：`sortedGamePlugins()` 按 `naturalCompare(displayGameName())` 排序 → `"SPT4" < "SPT4.1"` → 首个命中同为 `SPT4`。
- 两条排序互相独立却都指向 SPT4，故「总是识别成 4.0」是**确定性**的。

### 4. 载体与 bat 重写

| 项 | SPT 4.0.x | SPT 4.1.x | SPT 5.x |
|---|---|---|---|
| 服务器布局 | 根下 `SPT\` | `SPT_Runtime\` | `SPT_Runtime\` |
| server / launcher | `SPT\SPT.Server.exe` / `SPT.Launcher.exe` | `SPT_Runtime\SPT.Server.exe` / 同 | 同 4.1 |

- 每个 def 的 `executables()` 把 `<gamePath>\sptvfsbridge.bat` **每次枚举都重写**（注释 "always rewrite so a fixed version never lingers"）：
  `game_spt4.py` → `set "spt_dir=%game_root%SPT"`（4.0）；`game_spt41.py` → `%game_root%SPT_Runtime`（4.1）。
- 写出者是**当前 managed game** 的 `executables()`（`executableslist.cpp:136-149`）→ 选错 def 就等于写错 bat。

### 5. 因果链一句话

> 猜插件（Browse / 无 `gameName` 兜底）→ 首个 `looksValid` 命中 = `SPT4`（谓词等价 + 排序在前）→ `game_spt4.executables()` 重写 bat 为 `spt_dir=SPT` → 4.1 安装无 `SPT\` → 服务器「找不到文件」启动失败。

### 6. 历史回退是反复的根因（fork `src/plugins` git 史，全部 2026-08-05）

- `961c0ab`：**正解**——`looksValid` 按 MO2 传入目录探测版本（`SPT_Runtime/SPT.Server.exe`→4.1；`SPT/`→4.0；根 `SPT.Launcher.exe`→3.x；`Aki.Launcher.exe`→3.7）。
- `24b5618`：**回退**——拆分为逐版本插件时**整体删除**版本探测 `looksValid`，此后无任何版本判别；
  此后修复只做过两类（删代码 / 改实例 ini 数据），故「修过好几次仍复发」。
- 活体物证仍在盘上：`SPT_5xx\sptvfsbridge.bat` 至今是 4.0 模板（该目录无 `SPT\`）。

## 修复（方案 A：互斥版本感知谓词 + diagnose 一致性校验 + CHANGELOG 回归警示）

- `game_spt4.py` / `game_spt41.py` / `game_spt5.py` 各覆写互斥 `looksValid(directory)`：
  - `SPT4`：要求 `<root>\SPT\SPT.Server.exe`；
  - `SPT4.1`：要求 `SPT_Runtime\SPT.Server.exe` 且 `core.json` 主版本 == 0；
  - `SPT5`：同 `SPT_Runtime` 布局且主版本 >= 1；
  - `core.json` 缺失 / 解析失败 → **fail-fast 返回 False**（宁「选不中」不「选错」）。
  - 版本号 `1.2.1 → 1.2.2`；41/5 新增模块级 `_eft_major_version`。
- 配套 `spt_diagnose.py`：`_check_layout` 由「四种布局任一存在即正常」改为**「所选插件版本 ↔ 目录实际布局」一致性校验**，不一致即报 `KEY_LAYOUT`（版本 `1.0.0 → 1.1.0`）。
- `CHANGELOG.md` 增「勿再删版本探测」回归警示。
- 方案 B（合并回单插件 + 运行时双态）不推荐首选；方案 C（仅诊断增强 + 操作规约）仅治标。

## 复验证据（独立复验四项）

1. 部署一致性：repo ↔ install ↔ build 三处 SHA256 全等 **4/4**。
2. AST 语法门：**12/12**。
3. 离线谓词矩阵 `check.py` 三棵树各 **8/8** 全绿：
   `SPT_41x`→仅 4.1；`SPT_40x`→仅 4；`SPT_5xx`→仅 5；`3114`/`376`→全否；
   合成边界：仅 `SPT\`、缺 `core.json` fail-closed、未来 2.x 版本线；diagnose 一致性用例 **6/6** 全绿。
4. 判别力证据：同一 harness 对**修复前源码** **8/8** 全红（三谓词对任意 EFT 目录恒 True）→ 证明测试真在测谓词，而非空转。
- 现场物证：`SPT_41x\sptvfsbridge.bat` 为 `SPT_Runtime`（正确）；`SPT_5xx\sptvfsbridge.bat` 残留 4.0 模板（活体残留样本）。
- Overseer 2026-10-12 实测各版本识别正确，验收通过。

## 复发条件与预防

- 复发条件（任一）：Browse 盲选目录；ini 缺 `gameName`；实例导入 / 兜底解析；
  使用旧发行包（无 `game_spt41.py`，如 `v2.5.3Beta2` 名不副实）；错配后未重新触发 bat 重写 → 错误 bat 滞留。
- 预防：
  ① 恢复互斥版本判别（已做）；
  ② `spt_diagnose` 一致性校验把未来错配组合直接报出；
  ③ 操作规约「**创建 SPT 实例手选版本，勿 Browse 盲选**」；
  ④ 发布说明标注 strict 谓词会让历史错配**显式暴露**（fail-fast 取代静默写错 bat）。

## 适用性边界

- `SPT3` / `SPT AKI` 与 4.x 布局天然互斥，不需改。
- 5.x 判别基于 `core.json` 的 `compatibleTarkovVersion` 主版本（`0.` vs `1.`）；
  复验含未来 2.x 版本线边界，但跨未来 SPT 版本的稳定性待抽查。
- 工具链侧 `mo2-mcp` 的 `GAME_NAME_TO_KEY` 无 SPT 条目（`SPT4.1` fallback 为 `"spt4.1"`），
  与本次缺陷无关，但版本语义不统一，记录待后续。
- 本仓与 fork 仓库**跨仓**：修复落在 fork，追踪/归档在本仓——勿让「修复上下文」只留在对话里。

## 相关

- `curated/operations/usvfs-long-path-silent-missing.md`（同实例环境：SPT 4.1.6 / MO2 便携实例 Norvinsk 家族）
- `curated/operations/destructive-operation-guardrails.md`
- fork 仓库：`E:\云文件\GitHub\SamMeow-Tarkov-specific-Mod-Organizer`（改动落 `src/plugins/`）
- 本仓研究报告：`docs/research/mo2-spt-version-misdetection.md`（机制、时间线、现场证据、实施记录全文）
