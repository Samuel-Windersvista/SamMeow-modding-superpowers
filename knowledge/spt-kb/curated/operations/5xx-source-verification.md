---
version: [5.0]
domain: server
topic: operations
source: curated
---
# SPT 5.x 源码核实与进展（5.0x-dev）

> 状态：**已核实（远程 API + 本地克隆取证 2026-09-13）** | 版本：[5.0]
> 源码位置：`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`
> 远程：`https://github.com/SP-Tushonka/server-csharp` 分支 `5.0x-dev`，克隆时 HEAD `ff0bf3281`（2026-09-12，"Add BP exchanging"）

## 一、真伪判定：确认是真正的 SPT 5.x 源码

| 证据 | 4.1.x（`main` / `4.1x-dev`） | `5.0x-dev` |
|------|------|------|
| `Build.props` → `SptVersion` | `4.1.5` | **`5.0.0`** |
| `core.json` → `compatibleTarkovVersion` | `0.16.9.40743`（EFT 0.16.9） | **`1.1.5.0.47242`**（EFT 1.1.5.0 build 47242） |
| 预发布标签 | `4.1.5`（+ 4.1.0~4.1.5 全系） | **`5.0.0-BEM-20260909` / `5.0.0-BEM-20260910`**（BEM = BLEEDINGEDGEMODS） |
| `TargetFramework` | `net10.0` | `net10.0`（相同，未升框架） |
| 仓库描述 | "A C# rewrite of the SPT server project" | 同 |

结论：**确认是真 SPT 5.0.0 开发线**，不是 4.1.x 的马甲分支。分支活跃（最后提交 2026-09-12），且已有 5.0.0 的 bleeding-edge 构建标签。

## 二、与 SPT 4.1.x 的差异

**分支关系**（克隆时）：
- `main` = `SptVersion 4.1.5`（发布线，最后提交 2026-09-11）
- `4.1x-dev` = `4.1.5` 开发线
- `5.0x-dev` = `5.0.0`，**领先 `4.1x-dev` 53 个 commit**（最早 2026-06-13 "update build.props" → 2026-09-12）

**代码规模**：`Libraries/` 内 239 个文件变更（+10792 / −3351 行）；当前 994 个 `.cs`（32 控制器 / 60 服务 / 53 路由 / 36 回调）。

**新增核心系统（5.x 独有，`.cs` 新增文件）**：

| 系统 | 代表文件 | 说明 |
|------|---------|------|
| 战斗通行证 BattlePass | `Controllers/BattlePassController`、`Callbacks/BattlePassCallbacks`、`Routers/ItemEvents/BattlePassItemEventRouter`、`Services/Commerce/BattlePassDocumentLimitService`、`Helpers/Traders/BattlePassAssortHelper` | 通行证解锁/兑换（BP exchanging） |
| 赛季与专精 Seasons/Perks | `Models/Spt/Tables/SeasonTable`、`Models/Eft/Seasons/*`、`ProfileSeasonalProgress`、`SeasonalPerkEffectParameters` | 赛季进度与 perk 效果 |
| 商店 Tarcoin | `Services/Commerce/TarcoinStoreService`（349 行）、`Routers/Dynamic/ShopDynamicRouter`、`Models/Spt/Tables/ShopTable`、`Models/Eft/Game/Shop*` | 氪金/代币商店（tarcoin） |
| 结局系统 Endings | `Controllers/EndingController`、`Callbacks/EndingCallbacks`、`Routers/Static/EndingStaticRouter`、`EndingsResponse`/`EndingRequest` | 对应 EFT 1.0「门票/The Ticket」多结局 |
| 剧情任务 Story Quests | `Helpers/Quest/QuestVariableHelper`、`Models/Eft/Quests/MainQuestsList`、`QuestChainsResponse`、`SubtitleGroupData`、`VariableGroupData` | 主线章节、任务链、任务变量 |
| 扩展包 Expansions | `Models/Eft/Game/ExpansionsAccessData`、`Servers/Ws/SessionRequestWebSocketHandler`、`Models/Eft/Ws/WsExpansions*` | 版本升级/扩展包余额（WebSocket 新通道） |
| 教程 Tutorial | `Models/Eft/Profile/TutorGame*`、`Models/Spt/Tables/Globals/TutorialGlobals` | 新手教程系统 |
| 其它 | `RagfairLevelService`、`SessionTokenService`、`Utils/RequestEncryptionUtil`（188 行）、`Utils/PooledBufferStream`、新枚举 `ComponentType`/`FaceCoverMask`/`WeaponTarget` | 跳蚤等级、会话令牌、请求加密、缓冲池 |

**新增游戏数据（`SPT_Data/database/`）**：
- 新地图：`icebreaker`（破冰船）、`terminal` / `terminal_ui`、`sandbox_start`、`laboratory_dark`、`lighthouse2`
- 新 bot 类型：`blackdivision` / `bossbullyblackdiv` / `pmcbotblackdiv` / `followerbullyblackdiv`（Black Division 系）、`bosswedge` / `bosswedgelab` / `followerwedgelab`（Wedge 系）、`followertagilla`、`vsrf` / `vsrfsniper`、`sentry`、`civilian`、`exusecfree`、`assaulttutorial`
- 新模板：`endings.json`、`mainQuestNotes.json`、`questChains.json`、`questVariables.json`、`subtitleTracks.json`、`tapes.json`、`tutorialLoadout.json`、`variableGroups.json`
- 新赛季数据：`season/active.json`、`battlePass.json`、`battlePassAssort.json`、`perks.json`
- 新商店：`shop/content.json`
- 新语言：`in`（印尼）、`th`（泰）、`vi`（越）、`ro`（罗马尼亚）
- 新商人：4 个新 trader id（`67f7af56…`、`688246518…`、`688246958…`、`68fe1591…`、`68fe1599…`、`69e0d6cc…`）
- `battle-pass` 图标资源一批（`SPT_Data/files/battle-pass/*.png`）

**工程结构变化**：
- `Tools/` 工具链并入 server 仓库（`LootDumpProcessor`、`QuestGenerator`、`QuestVariableGenerator`、`MongoIdTplGenerator`）
- 新解决方案格式 `server-csharp.slnx`（替代 `.sln`）
- 大数据用 Git LFS（18 文件 / 约 257 MB：各图 `looseLoot.json`、`templates/items.json`、`background.mp4`）

**未变**：目录结构 `Libraries/` 六件套（`SPTushonka.Common` / `.DI` / `.Reflection` / `.Server.Assets` / `.Server.Core` / `.Server.Web`）与 4.1 一致；`net10.0` 框架未变。

## 三、克隆记录

- 目标：`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`
- 分支：`5.0x-dev`（本地已创建并跟踪 `origin/5.0x-dev`），工作树干净
- LFS：已 `git lfs pull` 实体化，`git lfs fsck` OK
- 总占用：约 1.5 GB（含 `.git` 全部分支 + LFS 对象）

**踩坑（复现要点）**：
1. 直连 GitHub git 传输被重置（`Recv failure: Connection was reset`），需走本地代理 `http://127.0.0.1:7890`：
   `git -c http.proxy=http://127.0.0.1:7890 clone https://github.com/SP-Tushonka/server-csharp.git <target>`
2. 克隆时设 `GIT_LFS_SKIP_SMUDGE=1` 避免 checkout 阶段卡在 LFS；数据随后用
   `$env:HTTPS_PROXY=http://127.0.0.1:7890; git lfs pull` 拉取。
3. 中断的 checkout 会残留 `.git/index.lock`，需手动删除后再继续。

## 四、SPT 5 目前进展（截至 2026-09-13）

- **状态（2026-09-13 核实）**：当时为活跃开发中的预发布线，尚无 `5.0.0` 正式 tag；已有 `5.0.0-BEM-20260909/0910` 两个 bleeding-edge 构建标签。**2026-09-14 更新：SPT 5.0 已正式发布（ADR-0006），本文结论需按正式 release tag 复核。**
- **目标客户端：EFT `1.1.5.0.47242`**（4.1.x 为 `0.16.9.40743`）——跨了一个大版本线，对齐 EFT 1.0+ 的新内容。
- **开发节奏**：`5.0x-dev` 由 2026-06-13 起 53 commits（vs 4.1x-dev），2026-09 进入高频提交（近一周几乎每日多次，含多次从 `4.1x-dev` 合并的维护修复）。
- **内容主题**：围绕 EFT 1.0 的新系统做服务端本地化——战斗通行证/赛季、tarcoin 商店、多结局（门票）、主线剧情任务链与任务变量、扩展包/版本升级、教程、新地图（破冰船/终端等）与新派系 bot（Black Division/Wedge）。
- **兼容性影响（待专项评估）**：`compatibleTarkovVersion` 跳变意味着客户端 mod（Assembly-CSharp 类名）与 server mod（EFT 模型表结构）都可能需要新一轮迁移；4.1 时代的 mod API 是否沿用尚未核实。

## 五、注意事项

- **目标版本策略需复核**：`VERSIONS.md` 原定「最终目标 SPT 4.1，永久停更」。SPT 5.x 出现后该策略待 Overseer 决策（是否迁移/观望），本记录不擅自改策略。（2026-09-14 已复核：见 ADR-0006——4.1.5 稳定开发基线 + 5.0 已发布新主线双轨，「永久停更」表述作废。）
- `main` 分支虽为 4.1.5，但近期 `template-update` PR 已加入 `2-bug-report-5-0.yml` 模板，说明上游在为 5.0 做配套准备。
- 本记录为远程/源码取证，未在本机构建运行 5.0 服务端；运行时验证另行进行。

## 已核实位置

- 远程：`SP-Tushonka/server-csharp` `5.0x-dev`（`Build.props`、`Libraries/SPTushonka.Server.Assets/SPT_Data/configs/core.json`）
- 本地：`E:\云文件\GitHub\SamMeow_SP-Tushonka_5xx_source_code`（branch `5.0x-dev`）
