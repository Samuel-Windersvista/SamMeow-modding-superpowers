# 09: 部署与验证收口

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 部署与收口：打包 MO2 覆盖层（DLL + README + LICENSE + THIRD-PARTY-NOTICES + 偏差表；命名与 meta.ini 符合包惯例）；替换包内 `[8]门的大修-DoorOverhaul`（旧目录保留、一键回滚）；收口 BB 未就绪清单两项；`[15]` 配置叠层做本 mod 配置与既有 DO 配置的对照迁移记录；执行 Level C 全清单复验（破门/破坏/C4/交互/开关/共存）；spec 风险面章节闭环。

**Blocked by:** 06（排除开关体系）、07（C4 客户端行为）、08（C4 服务端组件）

**Status:** ready-for-human

- [x] 覆盖层打包完成（命名/meta.ini comments+notes 符合包惯例；版本号与工程一致）
- [ ] 替换部署完成且可一键回滚（旧覆盖层目录保留）；BB 未就绪两项收口；对照表/dev-log 更新（提交由 Overseer 执行）
- [x] `[15]` 配置对照迁移记录（本 mod 配置项 ↔ 既有 DO 配置；既有调参不丢失）
- [ ] Level C 全清单通过：破门（上锁/近战/容器）、破坏（弹孔/穿透/铰链/门板）、C4 全流程、交互全套、总开关与高危组行为、调试面板、门状态随机化共存
- [x] README / 许可（MIT × BSD-3）/ THIRD-PARTY-NOTICES / 偏差表齐备；spec 风险面章节闭环
- [x] 回滚点核对（MO2 profile 备份现状记录）

## Comments

- 2026-10-25（09 非部署部分完成，部署与 Level C 待 Overseer）：
  - **覆盖层 staging**：`.artifacts\doorbreach-ticket09-staging\`，内容 = 客户端 07 DLL（`4FF4DB6F…` / 175,616 B）+ audio + 服务端 08 树（DLL `E74F7D98…` / bundle `471BFB5A…`）+ 最新项目 README（19,060 B）/ LICENSE / THIRD-PARTY-NOTICES + `meta.ini` + `DEPLOY.md`。
  - **staging meta.ini**：`version=0.1.0`（原覆盖层为 0.1.0-test）、`installationFile=DoorBreachOverhaul-0.1.0.zip`；comments/notes 已更新为 06/07 收口后的描述，并注明旧 `[8]门的大修-DoorOverhaul` 目录保留作回滚、配置迁移文档位置。
  - **分发包**：`.artifacts\DoorBreachOverhaul-0.1.0.zip`，2,279,701 B / SHA256 `047EDC3EE6491F8601534711ECDB9F1C9EF60692F7BB8AE9DFD6D8638220BE70`。
  - **配置对照迁移**：`docs/doorbreach-config-migration-2026-10-25.md`。旧 DO cfg（`[15]\...\com.sage.dooroverhaul.cfg`）→ 新 cfg（`overwrite\BepInEx\config\com.sammeow.doorbreach.cfg`）全键映射；**5 项旧调参需迁移**：`Silent Peek=true`、`Quiet Volume=0.04977376`、`Punch-Through Penetration: Thin Wood=7.977375`、`Thick Wood=16.56108`、`Thin Metal=22.52941`；BB/C4/排除开关为新 mod 独有项，保留当前用户值，不覆盖。
  - **BB 未就绪清单**：`E:\Game\EFT_Offline\Norvinsk_Forever\docs\pending-mods-v0.1.0.md` 第 27/28 项（`[8]武器破门-BackdoorBandit客户端文件` / `服务器文件`）。**待 Level C 通过后**再把这两项标记为已由 DoorBreachOverhaul 覆盖；本轮不改该文件（未验证不先收口）。
  - **回滚点**：已用 MO2 工具新建 profile 备份 `E:\Game\EFT_Offline\Norvinsk_Forever\.mo2-mcp\profile-backups\Default_pre-doorbreach-0.1.0-20261025`（5 文件：archives/initweaks/lockedorder/modlist/settings）。当前 `profiles\Default\modlist.txt` SHA256 `91D4FB42C9E8CB6362ED3FF8156FD2DAEA93C30FF045740DF6D229C060C05850`；旧 `[8]门的大修-DoorOverhaul` 为禁用（`-`），新覆盖层为启用（`+`），旧目录保留。
  - **部署前置**：Overseer 完全退出 `EscapeFromTarkov` / `SPT.Launcher` / `SPT.Server`；建议关闭 MO2。部署步骤/hashes/回滚见 staging `DEPLOY.md`；当前覆盖层仍是 06 诊断构建 `1EF07AB9…`，部署前先备份该 DLL 到 `.artifacts\doorbreach-ticket06-backup\DoorBreachOverhaul.ticket06-diagnostic.dll`。
  - **Level C**：06 三轮（默认全关 / 仅安全类开 / 仅高危组开）+ 07/08 十步（放置→倒计时→引爆→destruction/fallback-open→范围伤害→降级→反复进出）尚未执行；执行后回填本工单与 spec 风险面。
  - **残余**：`@oracle` 因 matt-bridge 预设模型 `opencode-go/gemini-4-argon-contributor` 弃用不可用；06/07 审查以主会话双轴自审记录（见 06/07 Comments）。若要在 09 收口前做独立审查，需先修 oracle 模型配置并重启 OpenCode。
- 2026-10-25（覆盖层已部署，游戏/服务端/Launcher 已退出，仅 MO2 运行）：
  - 目标覆盖层已更新为 07 构建：`DoorBreachOverhaul.dll` = 175,616 B / SHA256 `4FF4DB6FCB5FF923E624F35DA9F1B7A1DCA59C77739CBAB333A1DA85AAADE61C`；README = 19,060 B；`meta.ini version=0.1.0`；服务端 DLL `E74F7D98…` / bundle `471BFB5A…` / db 未变。旧 `[8]门的大修-DoorOverhaul` 目录保留且仍为禁用。
  - 部署后整目录备份：`.artifacts\doorbreach-ticket09-backup\2026-10-25-07-deployed\`（16 文件 / 2,407,674 B；DLL hash 一致）。
  - **[偏差]** 覆盖前整目录备份命令失败（`New-Item`/`Copy-Item` 路径处理），06 诊断 DLL（`1EF07AB9…`）未单独留存；但 07 构建已包含 06 诊断日志（`[Destruction]` / `[Policy]`），06 Level C 仍可用 07 构建执行。05 与 06 初始备份仍在 `.artifacts\doorbreach-ticket06-backup\`。
  - MO2 运行中完成复制；`meta.ini` 可能被 MO2 缓存覆盖，重启 MO2 / 启动游戏后需复核 `version=0.1.0` 与 comments/notes。
  - 下一步：Overseer 经 MO2 启动 → agent 读日志断言（17 补丁 / 0 failed / 快照两开关 / 无 DoorBreach 异常）→ 06 三轮 Level C + 07/08 十步 E2E → 通过后收口 BB 第 27/28 项与 dev-log。
- 2026-10-25（07 部署后 Level B 通过，Level C 部分）：
  - 启动日志：17 个 DoorBreach 补丁全部 `Enabled patch`；`Failed to create patch=0`；DoorBreach Error/Exception=0；快照 `安全类排除=False / 高危组排除=False`。
  - Level C 第 1 轮部分（Shoreline）：注册表 `373 doors / 321 destructible / safety 0 / high risk 0 / physical 52`；普通门 `door_Shoreline_West_00045` accepted=True，面板 `destructible: WoodThin`、13 次命中、可见弹孔；`door_Shoreline_Sanatorium_indoor_00046` 29 次命中、上铰链 17% / 下铰链 54%、`leaf Attached`（未脱落属预期，铰链未归零）。Shoreline 无安全类/高危类门，需换 Interchange/Labs/Reserve/Streets 继续。
  - 部署后覆盖层保持 `4FF4DB6F…` / 175,616 B；游戏退出后未回滚。
  - 待办：换图完成 06 安全类/高危组开关；07/08 C4 十步 E2E；BB 第 27/28 项与 dev-log 收口。
