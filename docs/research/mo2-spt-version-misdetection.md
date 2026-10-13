# MO2 SPT 版本误判研究报告 —— SPT 4.1.x 被识别为 SPT 4.0.x

- 日期：2026-10-12
- 性质：研究报告 + 实施记录（方案 A 已于 2026-10-12 实施并通过独立复验，见 §9）
- 方法：三路并行研究单元（fork 源码识别链路 / 实体证据与日志 / 跨仓历史考古）+ 主控台代码直验
- 涉及对象：
  - fork 源码仓库：`E:\云文件\GitHub\SamMeow-Tarkov-specific-Mod-Organizer`
  - MO2 现场：`E:\Game\EFT_Offline\SPT-Organizer-v2.5.2-Novinsk`
  - SPT 安装：`SPT_41x`（4.1.6）、`SPT_40x`（4.0.13）、`SPT_5xx`、`SPT_3114`、`SPT_376`
  - 证据标签：`[直验]` = 主控台本次直接读取代码/文件核实；`[lane-A/B/C]` = 对应研究单元报告
- 结论严重度：**结构性缺陷**。当前 live 实例状态正确（这解释了"看起来时好时坏"），但实例重建 / "Browse..." / 无 gameName 兜底解析任一发生即复发。

---

## 0. 结论摘要

1. **版本识别根本不发生在插件里**。MO2 选插件只有两类路径：按 `gameName` 精确匹配（全等比较），或按「首个 `looksValid` 命中」猜。五个 SPT def 中 `SPT4 / SPT4.1 / SPT5` 的 `looksValid` 谓词**完全等价**（都只查根目录 `EscapeFromTarkov.exe`），猜路径必然收敛到枚举/排序在前的 **SPT4**。
2. `game_spt4.py` 的 `executables()` 会把 `<gamePath>\sptvfsbridge.bat` 重写为 4.0 布局（`set "spt_dir=%game_root%SPT"`）；而 4.1 安装只有 `SPT_Runtime\`、没有 `SPT\` → 启动时报「Windows 找不到文件」，服务器起不来。
3. **历史回退是反复的根因**：2026-08-05 `961c0ab` 曾实现「MO2 传入目录 + 版本探测」的 `looksValid`（正解），同日 `24b5618` 拆分为逐版本插件时**整体删除**；此后只能靠人工改实例 ini 止血 → "修过好几次仍复发"。
4. **活体物证仍在盘上**：`SPT_5xx\sptvfsbridge.bat` 至今是 4.0 模板（该目录无 `SPT\`）；`.scratch\norvinsk-forever-4.1.6\sptvfsbridge_41x_original.bat` 是"4.1 被写成 4.0"的留档原件。

---

## 1. 机制根因链（逐环证据）

### 1.1 载体：布局差异与 bat 重写机制

| 项 | SPT 4.0.x | SPT 4.1.x | SPT 5.x |
|---|---|---|---|
| 服务器布局 | `SPT\`（根下） | `SPT_Runtime\` | `SPT_Runtime\` |
| server / launcher | `SPT\SPT.Server.exe` / `SPT.Launcher.exe` | `SPT_Runtime\SPT.Server.exe` / `SPT.Launcher.exe` | 同左（`SPT_Runtime\`） |
| 目录实测 | `SPT_40x` 有 `SPT\` `[lane-B]` | `SPT_41x` 有 `SPT_Runtime\`、无 `SPT\` `[直验]` | `SPT_5xx` 有 `SPT_Runtime\`、无 `SPT\` `[lane-B]` |

- 每个 SPT game def 在 `executables()` 里把 `<gamePath>\sptvfsbridge.bat` **重写**（每次枚举执行项都会重写，见文件内注释 "always rewrite so a fixed version never lingers"）：
  - `game_spt4.py:62-80,93` → `set "spt_dir=%game_root%SPT"`（4.0）`[直验]`
  - `game_spt41.py:62-80,93` → `set "spt_dir=%game_root%SPT_Runtime"`（4.1）`[直验]`
- 写出者是**当前 managed game**的 `executables()`：`upstream/modorganizer/src/executableslist.cpp:136-149 getPluginExecutables(game)` `[直验]`。→ 选错 def = bat 被写错。

### 1.2 MO2 如何选插件：三条路径

| 路径 | 代码 | 行为 |
|---|---|---|
| 正常启动（ini 有 gameName） | `instancemanager.cpp:250-288` `[直验]` | `m_gameName.compare(game->gameName(), CaseInsensitive) == 0` 全等匹配；无前缀/模糊。`gameName=SPT4.1` → 正确命中 `game_spt41` |
| 实例创建对话框 "Browse..." | `createinstancedialogpages.cpp:364-379` `[直验]` | 对目录**按列表顺序取首个 `looksValid` 命中**：`for (auto& g : m_games) { if (g->game->looksValid(path)) { select(g->game); return; } }` |
| ini 无 gameName 兜底 | `instancemanager.cpp:289-306` `[直验]`；实例管理另见 `instancemanager.cpp:665-721` `[lane-A]` | 同样按插件枚举顺序取**首个 `looksValid` 命中**并写回 `m_gameName` |

### 1.3 `looksValid` 谓词等价性（缺陷核心）

- 默认实现（upstream basic_games）：`basic_game.py:624-625` `[直验]`
  ```python
  def looksValid(self, directory: QDir):
      return directory.exists(self.binaryName())   # 只查 GameBinary 是否存在
  ```
- 五个插件 `GameBinary`（均**未覆写** `looksValid`，`src/plugins/*.py` 全文无 `looksValid`）`[lane-A]`：

| 插件 | GameName | GameBinary | 对 SPT_41x 目录的判定 |
|---|---|---|---|
| game_spt3 | `SPT3` | `SPT.Launcher.exe`（根） | False（4.1 根无此文件） |
| **game_spt4** | `SPT4` | `EscapeFromTarkov.exe` | **True（等价，先命中）** |
| **game_spt41** | `SPT4.1` | `EscapeFromTarkov.exe` | True（等价） |
| **game_spt5** | `SPT5` | `EscapeFromTarkov.exe` | True（等价） |
| game_sptaki | `SPT AKI` | `Aki.Launcher.exe`（根） | False |

→ **SPT4 / SPT4.1 / SPT5 三者谓词完全等价**；SPT3 与 SPT AKI 的 GameBinary 与 4.x 布局天然互斥（不参与本缺陷）。

### 1.4 顺序偏置（为什么固定是 SPT4 而不是 SPT5）

- **注册/枚举顺序**：`plugins/basic_games/__init__.py:24-65` 用 `glob.glob(games/*.py)`（无排序）逐文件 import 并实例化所有 `BasicGame` 子类 `[lane-A/B]`。文件名序：`game_spt3.py < game_spt4.py < game_spt41.py < game_spt5.py` → 插件枚举首个命中 = **game_spt4**。
- **Browse 显示序**：`createinstancedialogpages.cpp:411-426 sortedGamePlugins()` 按 `naturalCompare(displayGameName())` 排序 `[直验]` → `"SPT4" < "SPT4.1"` → 首个命中同为 **SPT4**。
- 两条排序互相独立，却都指向 SPT4（枚举按文件名、UI 按自然序），因此表现是**确定性**的"总是识别成 4.0"。

### 1.5 因果链一句话

> 猜插件（Browse / 无 gameName 兜底）→ 首个 `looksValid` 命中 = SPT4（谓词等价 + 排序在前）→ `game_spt4.executables()` 重写 bat 为 `spt_dir=SPT` → 4.1 安装无 `SPT\` → 服务器启动失败。

### 1.6 补充判别数据（供修复用）

`SPT_Runtime` 本身形态在 4.1.6 与 5.x 高度相似（两边都有 `SPT.Server.exe` / `SPTarkov.*.dll` / `SPT_Data/` / `user/`）`[直验]`。可用于区分的元数据：

| 安装 | `SPT_Runtime\SPT_Data\configs\core.json` 的 `compatibleTarkovVersion` |
|---|---|
| SPT_41x（4.1.6） | `"0.16.9.40743"`（EFT 0.16.x 线） `[直验]` |
| SPT_5xx（5.x） | `"1.2.0.0.47965"`（EFT 1.x 线） `[直验]` |

→ 4.1 与 5.x 的互斥判定可基于该字段的主版本前缀（`0.` vs `1.`）；辅证：4.1 侧 `sptLogger.json` vs 5.x 侧 `sptLogger.Development.json`（弱标记，不建议单用）。

---

## 2. 触发场景（复现条件）

以下任一即触发错配（写错 bat）：

1. 实例创建对话框用 **"Browse..."** 选择 SPT 安装目录（`createinstancedialogpages.cpp:364-379`）。
2. 实例 ini **缺 `gameName`**（或旧格式只有 `game=` 不匹配）→ 兜底解析首个 `looksValid`（`instancemanager.cpp:289-306`）。
3. 实例管理/导入路径经 `gamePluginForDirectory` 兜底（`instancemanager.cpp:665-721`）。
4. 使用**旧包**（不含 `game_spt41.py` 的发行版，如 `带SPT全版本识别插件的MO2_v2.5.3Beta2.zip`）→ 4.1 目录只能被 SPT4 接住（见 §4.1）。
5. 以上任一发生后再"改正"为 SPT4.1，若未重新触发 bat 重写，错误 bat 会**滞留**（下次以正确 def 启动 MO2 才会被覆盖）。

---

## 3. 历史时间线与「修了又回退」

fork 仓库 `src/plugins` git 历史（全部 2026-08-05）`[直验]`：

| commit | 标题 | 对本问题的作用 |
|---|---|---|
| `362615c` | game_tarkov plugin + Launch SPT orchestrator | 单插件起点 |
| `914b3fb` | executables() → bridge bat | bat 生成来源 |
| `f94ace0` | multi-version SPT support（布局检测 + 自适应） | 首次尝试自动版本识别 |
| **`961c0ab`** | **looksValid uses MO2-passed directory for version detection (3114/410 recognition)**（+15/-2，game_tarkov.py） | **正解**：`looksValid` 按目录探测版本（`SPT_Runtime/SPT.Server.exe`→4.1；`SPT/`→4.0；根 `SPT.Launcher.exe`→3.x；`Aki.Launcher.exe`→3.7） |
| `baeb718`/`8404c2c`/`84a2e46` | 健康轮询修正 | bat 内容 |
| **`24b5618`** | **split into per-version plugins (spt3/spt4/spt41/sptaki)**（-171 game_tarkov.py、-103 spt_launcher.py、+383 四个新插件）`[直验 git show --stat]` | **回退**：拆分时删除版本探测 `looksValid`；此后无任何版本判别 |
| `9236a82`/`37a3faf`/`bb56fff`/`c0ae0e2` | bat 路径转义 / WebView2 黑名单 / stderr / LocalSavegames | 其他加固 |
| `3d8d881` | `spt_diagnose.py`（含 KEY_LAYOUT 诊断） | 误选兜底提示（非阻断） |
| `95319d6` | mod 内容分类图标 | 无关 |

文档层修复痕迹：

- fork `CHANGELOG.md:84`：「**实例选错插件**：SPT411 实例曾指向 SPT4（4.0 布局）→ 改指 SPT4.1」`[lane-B]` —— 数据层人工修复。
- `E:\Game\EFT_Offline\Norvinsk_Forever\docs\dev-log.md:96`：`SPT_41x\sptvfsbridge.bat：spt_dir 指向旧布局 SPT\（不存在）导致「Windows 找不到文件」；改为 SPT_Runtime\`；原件备份见同文件 Refs `[lane-B]`。

> 结论：修复只做过两类——「代码正解被删除」与「改 ini 数据」。没有一次是在现状结构上恢复版本判别。

---

## 4. 现场证据（活体）

### 4.1 三方发行物对照 `[lane-B]`

- A = 已安装 `SPT-Organizer-v2.5.2-Novinsk\plugins\` ；B = `SPT特别优化版MO2-Organizer-v2.5.2-Novinsk.zip` 解包；C = `带SPT全版本识别插件的MO2_v2.5.3Beta2.zip` 解包。
- **A ≡ B**：五个 `game_spt*.py` 逐字节相同（SHA256 一致），均含 1.2.1 修复（SPT_Runtime 模板 + 健康轮询 + WebView2 黑名单）。
- **C 是旧基线**：只有 `game_spt4.py`（v1.2.0, Author=Samuel，只在首次写 bat 且写 `SPT` 模板）+ `game_spt_Test.py`（旧 SPT3，Archon）+ `game_sptaki_Test.py`（旧 AKI）——**没有 game_spt41 / game_spt5 / 全树无 `SPT_Runtime` 字样**。该包名中的"全版本识别"名不副实，实为旧三件套；不可作为修复底本。
- 注册机制：`games/__init__.py` 为空；注册全靠 `basic_games/__init__.py` 的 glob 自动枚举（放文件即生效）`[lane-A/B]`。

### 4.2 日志证据 `[lane-B]`

- `SPT-Organizer-v2.5.2-Novinsk\logs\mo_interface.log:5`：`using game plugin 'SPT4.1' ... at E:/Game/EFT_Offline/SPT_41x`（内容 2026-10-11 16:11 UTC）→ 当前实例走了**精确匹配路径**，正确。
- `usvfs-2026-10-11_16-11-55.log:30-31`：`blacklisting 'msedgewebview2.exe'` 连续两行 → 1.2.1 版 def 活跃。
- `%LOCALAPPDATA%\ModOrganizer\SPT4.1.x《永远的诺文斯克》\logs\`：实际注入进程为 `SPT_41x\SPT_Runtime\SPT.Server.exe` 与 `SPT.Launcher.exe` → 链路当前工作正常。
- 无 Traceback / failed 异常。

### 4.3 `sptvfsbridge.bat` 全盘考古 `[lane-B]`

| 目录 | 存在 | 修改时间（本地） | `spt_dir=` | 模板判定 |
|---|---|---|---|---|
| `SPT_41x` | 是 | 2026-10-12 00:34:51 | `%game_root%SPT_Runtime` | 4.1（正确） |
| `SPT_40x` | 无 | — | — | — |
| **`SPT_5xx`** | **是** | **2026-10-12 00:06:08** | **`%game_root%SPT`** | **4.0 模板（误判残留！该目录无 `SPT\`）** |
| `SPT_3114` | 是 | 2026-10-11 17:30:12 | `%game_root%` | 3.x 模板 |
| `SPT_376` | 是 | 2026-08-05 20:32:43 | `%game_root%` + Aki exe | AKI 模板 |
| `SPT_3114_Novinsk_Edition` | 是 | 2026-10-12 00:07:19 | `%game_root%` | 3.x 模板 |

另有历史原件留档：`.scratch\norvinsk-forever-4.1.6\sptvfsbridge_41x_original.bat`（名为 41x、内容 `spt_dir=SPT`、旧 `timeout /t 2` 模板）`[lane-B]`。

### 4.4 实例 ini 全表 `[lane-B]`

| 实例 | gameName | gamePath | 备注 |
|---|---|---|---|
| `Norvinsk_Forever\ModOrganizer.ini` | `SPT4.1` | `SPT_41x` | 10-11 18:41（被修过） |
| `SPT-Organizer-v2.5.2-Novinsk\ModOrganizer.ini` | `SPT4.1` | `SPT_41x`（base=Norvinsk_Forever） | 10-12 00:18 |
| `Inescapable Tarkov\ModOrganizer.ini` | `SPT5` | `SPT_5xx` | 但其 bat 是 4.0 模板（矛盾样本） |
| `%LOCALAPPDATA%\ModOrganizer\` 注册表 | — | — | 8 个实例：SPT AKI/SPT3×3/SPT4.1/SPT5/Fallout4，均声明正确 gameName |

### 4.5 mtime 审查 `[lane-B]`

- `game_spt3/41/5/aki.py` 四者同秒（2026-10-10 23:48:47）——即"4.1 修复批次"；`game_spt4.py` 单独在 10-11 17:51 被改。
- 所有 `.pyc` 均晚于对应 `.py`，无陈旧字节码风险。
- `spt_diagnose.py`（09-13）与插件的 KEY_LAYOUT 检测未覆盖本次缺陷（见 §5 配套）。

---

## 5. 修复建议

### 方案 A（推荐）：恢复「版本感知 + 互斥」的 `looksValid`

- 目标：让 `{SPT4, SPT4.1, SPT5}` 三个 def 的 `looksValid(directory)` **互相排斥**，任何目录只可能命中一个（SPT3/AKI 已天然互斥，不改）。
- 改动点（fork 仓库）：`src\plugins\game_spt4.py` / `game_spt41.py` / `game_spt5.py` 各覆写 `looksValid(directory)`；参考被删除的 `961c0ab` 实现（`git show 961c0ab`），但把 4.1 与 5.x 拆开：
  - `SPT4`：根有 `EscapeFromTarkov.exe` 且存在 `SPT\`（实施时确认 `SPT\SPT.Server.exe`）。
  - `SPT4.1`：存在 `SPT_Runtime\SPT.Server.exe` 且 **5.x 判别为假**。
  - `SPT5`：存在 `SPT_Runtime\SPT.Server.exe` 且 **5.x 判别为真**。
  - 5.x 判别（本报告实测）：读 `SPT_Runtime\SPT_Data\configs\core.json` 的 `compatibleTarkovVersion`，`0.` 前缀 → 4.x 线；`1.` 前缀 → 5.x 线。JSON 缺失/解析失败时按 fail-fast 语义处理（返回 False，宁可"选不中"也不"选错"）。
- 效果：Browse / 兜底 / 重建实例都会选对；旧错配（ini=SPT4 指 4.1 目录）在启动时 `instancemanager.cpp:258-278` 会显式报 game dir not valid / GameGone——从"静默写错 bat"升级为"显式失败"，符合 fail-fast。
- 风险：strict 谓词会让历史错配实例立刻暴露（需改选插件）——需写进发布说明；`looksValid` 是 MO2 也会用于"check installation"入口的公共函数，谓词要足够宽松地覆盖合法目录（如 `SPT_Runtime\SPT.Server.exe` 为主条件，避免过度约束）。

### 方案 B（不推荐首选）：合并回单插件 + 运行时版本双态

- 即 fork 设计文档 `docs/superpowers/specs/2026-08-04-spt-mo2-design.md` §6.1 原案（GameName 单入口 + 布局探测）。
- 优点：不存在多 def 互斥维护问题。缺点：删除/合并 5 个 def、联动存档目录映射与模板切换、`spt_diagnose` 引用调整；且 `24b5618` 刚从这条路反方向拆开（原因未记录）。适合后续大版本整理，不适合止血。

### 方案 C（仅治标）：现状 + 诊断增强 + 操作规约

- 强化 `src\plugins\spt_diagnose.py:193-207`：`_check_layout()` 目前"四种布局特征任一存在即视为正常"，应改为**比对 `managedGame().gameName()` 与目录实际布局**（4.1 期望 `SPT_Runtime`；SPT4 期望 `SPT`；SPT5 期望 5.x 判别），不一致即报 KEY_LAYOUT。
- 在 skill/文档固化操作规约：「创建 SPT 实例必须手选对应版本；不要用 Browse 盲选」。
- 清理 `SPT_5xx\sptvfsbridge.bat` 残留（以 SPT5 实例启动一次 MO2 让其重写，或手写正确模板）。

### 配套项（无论选 A/B/C）

1. **先 commit fork 仓库未提交改动**：当前 `src/plugins/*`（1.2.1 批次 + `game_spt5.py` 未跟踪 + diagnose 修改）尚在工作区，未成历史——修复前先落盘，避免与既有工作混淆。
2. `spt_diagnose.py` 增强（见方案 C 第一条）建议与 A 同批实施——它能把"未来的新错配组合"直接报出来。
3. 本仓文档层：在 SPT 实例操作相关 skill 中补一条规约（引用本报告）。

---

## 6. 修复后验证方案

1. 实例创建对话框 Browse 指向 `E:\Game\EFT_Offline\SPT_41x` → 应自动选中 **SPT4.1**（旧行为：SPT4）；检查 `SPT_41x\sptvfsbridge.bat` 的 `spt_dir=SPT_Runtime`。
2. Browse 指向 `SPT_40x` → SPT4；`SPT_5xx` → SPT5；`SPT_3114` → SPT3；`SPT_376` → SPT AKI。
3. 构造旧错配（ini `gameName=SPT4` 指向 4.1 目录）→ MO2 应显式报目录无效/GameGone（而非静默写错 bat）。
4. 以 `Norvinsk_Forever`（SPT4.1）实例实际启动一次：server → `/health` 200 → launcher 链路无回归。
5. 错配样本（如 `SPT_5xx\bat` 为 4.0 模板时）应触发增强版 KEY_LAYOUT 诊断。
6. 回归面：SPT3 / SPT AKI / SPT5 实例各 Browse 一次确认无新误配。

---

## 7. 未决问题

1. `SPT_5xx\sptvfsbridge.bat`（4.0 模板，写于 2026-10-12 00:06）的**写入者未闭合**：两个指向 `SPT_5xx` 的实例均声明 `gameName=SPT5`；疑为调试期临时实例/切换 def 所致。建议保留为修复后回归样本精确复现。
2. `24b5618` 拆分动机无提交记录（当时"为什么删掉刚修好的版本探测"没有写）——修复时勿再重蹈；建议新 commit 补写设计理由。
3. `compatibleTarkovVersion` 主版本判别（`0.` vs `1.`）跨未来 SPT 版本的稳定性未核——发布验证时抽查。
4. fork 仓库的 1.2.1 改动尚未 commit，引入时间点无法从 git 历史定位。
5. mo2-mcp 侧 `GAME_NAME_TO_KEY` 无 SPT 条目，`SPT4.1` 会 fallback 为 `"spt4.1"`（`tools\mo2-mcp\src\mo-ini.ts:117-131,147-153`）`[lane-C]`；本缺陷无关，但工具链对 SPT 版本语义不统一，记录待后续。
6. 本仓与 fork 仓库**跨仓**：修复落在 fork，追踪/归档在本仓——沿用本报告建立的双仓引用即可，勿再让"修复上下文"只留在对话里。

---

## 8. 来源索引

**关键代码（fork 仓库）**
- `upstream/modorganizer/src/instancemanager.cpp:250-306`（精确匹配 + 兜底）、`:665-721`（gamePluginForDirectory）
- `upstream/modorganizer/src/createinstancedialogpages.cpp:346-386`（Browse 首个命中）、`:411-426`（显示名排序）
- `upstream/modorganizer/src/executableslist.cpp:136-149`（managed game executables → bat）
- `upstream/basic_games/basic_game.py:624-625`（looksValid 默认实现）、`plugins/basic_games/__init__.py:24-65`（glob 枚举注册）
- `src/plugins/game_spt4.py` / `game_spt41.py`（v1.2.1）`executables()`、`:93` 模板行；`src/plugins/spt_diagnose.py:101-107,193-207`

**关键提交（fork 仓库）**：`362615c`、`914b3fb`、`f94ace0`、`961c0ab`、`24b5618`、`3d8d881`

**现场文件**：`SPT_41x\sptvfsbridge.bat`；`SPT_5xx\sptvfsbridge.bat`；`Norvinsk_Forever\ModOrganizer.ini`；`SPT-Organizer-v2.5.2-Novinsk\logs\mo_interface.log:5`；`.scratch\norvinsk-forever-4.1.6\sptvfsbridge_41x_original.bat`；`SPT_41x\SPT_Runtime\SPT_Data\configs\core.json` / `SPT_5xx` 同路径；`CHANGELOG.md:84`（fork）；`Norvinsk_Forever\docs\dev-log.md:96`

**本报告合成自**：三路只读研究单元（fork 识别链路考古 `ses_ed4192159ffeIJiBTkU14Dvga3`；实体证据 diff+日志 `ses_ed4190dcfffeuQTgL3XA8OocYW`；跨仓旧修考古 `ses_ed418fdc4ffefT0HEbnFtxDQoh`）+ 主控台直验（`[直验]` 标注处）。

---

## 9. 实施记录（2026-10-12；方案 A 已实施并通过独立复验）

- 实施方式：后台实施单元（fixer `ses_ed405d72dffeTJy4sn0co6A5gj`）应用精确补丁 → 主控台独立复验（fresh evidence，不采信实施方自证）。
- 改动（fork 仓库 `src/plugins/`）：
  - `game_spt4.py` / `game_spt41.py` / `game_spt5.py`：新增互斥 `looksValid`（4.0 要求 `<root>\SPT\SPT.Server.exe`；4.1 要求 `SPT_Runtime\SPT.Server.exe` + core.json 主版本 == 0；5.x 要求同布局 + 主版本 >= 1）；版本 `1.2.1 → 1.2.2`；41/5 新增模块级 `_eft_major_version`。
  - `spt_diagnose.py`：`_check_layout` 改为「所选插件版本 ↔ 目录实际布局」一致性校验（含 4.x/4.1/5.x 互斥判定 + `_eft_major_version`）；版本 `1.0.0 → 1.1.0`；KEY_LAYOUT 说明同步更新。
  - `CHANGELOG.md`：`[Unreleased] → ### 修复` 顶部三条 bullet（含"勿再删除版本探测"的回归警示）。
- 部署（repo ↔ install ↔ build SHA256 全等，4/4）：
  - 现场安装：`E:\Game\EFT_Offline\SPT-Organizer-v2.5.2-Novinsk\plugins\`（`basic_games\games\game_spt4/41/5.py` + `spt_diagnose.py`）
  - 构建树：`E:\build\spt-mo2\prefix\install\bin\plugins\`（同布局）
- 独立复验（主控台执行）：
  - 离线谓词矩阵 `D:\Temp\opencode\looksvalid-verify\check.py`：三棵树各 8/8 全绿（SPT_41x→仅 4.1；SPT_40x→仅 4；SPT_5xx→仅 5；3114/376→全否；合成边界：仅 `SPT\`、缺 core.json fail-closed、未来 2.x 版本线）；diagnose 一致性用例 6/6 全绿。
  - 判别力证据：同一 harness 对修复前源码 8/8 全红（三谓词对任意 EFT 目录全 True）。
  - AST 语法门 12/12；部署文件 mtime 2026-10-12 01:21；基线备份 12 文件存 `D:\Temp\opencode\fix-A-backup\`。
- 未提交：全部改动仍在工作区（含既有未提交批次）；未执行任何 git 写操作——是否提交由 Overseer 决定。
- 待人工验证（MO2 侧，agent 不代启动）：① 以 SPT4.1 实例启动一次 MO2 确认无回归（同时自动重写 `SPT_5xx\sptvfsbridge.bat` 残留）；② 新建测试实例用 "Browse..." 指向 `SPT_41x` 应自动选中 SPT4.1；③ 旧错配（ini=SPT4 + 4.1 目录）应显式失败并触发新 KEY_LAYOUT 诊断。
- **验收（2026-10-12）**：Overseer 实测确认——MO2 内各 SPT 版本均可正确识别，修复验收通过。
