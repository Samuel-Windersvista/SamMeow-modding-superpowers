# SamMeow.DebugToolkit 4.1.6 — Level C 验证清单（Overseer 执行）

> 前置：覆盖层 `[13]MOD工具-综合调试工具包-SamMeow.DebugToolkit` 已部署至 MO2 实例 `Norvinsk_Forever`（Default）：
> `+[13]MOD工具-综合调试工具包-SamMeow.DebugToolkit`、`-[13]BotDebug`、`-[13]DebugTooltip`、`+[6]BigBrain`。
> 执行方式：经 MO2 启动游戏，按项操作；每项记录 `OK` 或 `FAIL + 现象`。任一 FAIL 请附 `BepInEx\LogOutput.log` 相关片段与 F12 配置要点。
> 热键：`F9` 切 tooltip 调试信息；`F10`/`F11` 循环 bot 监视模式（均可配置）。默认全关：tooltip 开、覆盖层关、无敌组关、CODMode 关、QOL 关。

## A. 加载与配置面（Level B 由 agent 读日志断言；此处仅复核现象）
| # | 操作 | 预期 |
|---|------|------|
| A1 | 启动至主菜单 | 日志出现 `[SamMeow.DebugToolkit] … 已加载`；版本门禁匹配 40743；无 Harmony 补丁错误 |
| A2 | 打开 F12 | 六节齐备（`1. 通用` … `6. 便利`）；中文显示名；下拉/滑块正常；`Plugin Version` 只读灰显 |
| A3 | 检查互斥 | 无 BotDebug 覆盖层残留显示；无 DebugTooltip 重复提示（两源已禁用） |

## B. Tooltip 调试信息（局外界面）
| # | 操作 | 预期 |
|---|------|------|
| B1 | 悬停库存物品 | tooltip 追加调试块：Item ID / Tpl ID / Owner / Address |
| B2 | 悬停交易（商人）物品 | 同上 |
| B3 | 悬停空装备槽 | Slot ID / 父物品 ID |
| B4 | 武器改装界面悬停改装槽 / 护甲槽 | Slot ID / 父物品 ID；护甲挂载信息 |
| B5 | 任务列表项 + 任务详情 | Quest ID + 状态 |
| B6 | 任务目标行 | Condition ID |
| B7 | 商人卡片 | Trader ID |
| B8 | 悬停调试块时 `ctrl-c` / `ctrl-alt-c` / `ctrl-shift-c` | 分别复制短 ID / 备用 ID（Tpl/父 ID）/ 完整块（粘贴验证） |
| B9 | 按 `F9` | tooltip 调试信息即时开/关（免开 F12） |
| B10 | F12 关 `Show Debug Tooltip` | 调试块消失；开 `Show Copy Instructions` 开关行为正确 |

## C. Bot 调试覆盖层（战局内）
| # | 操作 | 预期 |
|---|------|------|
| C1 | F12 开 `Enable`（默认 `Behaviour`） | 各 bot 头顶出现调试框：Brain/策略、Layer、Node、距离等 |
| C2 | `F10`/`F11` 循环 | 依次经过 `Behaviour`→`BattleState`→`Health`→`Specials`→`Custom`→`BigBrainLayer`→`BigBrainLogic`→`Minimized`（隐藏）→回到 `Behaviour` |
| C3 | 观察颜色编码 | 白=普通；绿=玩家所属；红=把我当敌人 |
| C4 | 调整 `MaxDrawDistance` / `FontSize` | 生效（距离筛掉远处、字号变化） |
| C5 | 撤离后重新进图 | 覆盖层自动挂载；死亡 bot 条目 3 秒内消失；退出后无残留 |

## D. 无敌 / 部分无敌组（战局内）
| # | 操作 | 预期 |
|---|------|------|
| D1 | 开 `God Mode`，受枪/爆炸伤 | 不掉血；**坠落伤害仍生效**（该语义为「除坠落外」） |
| D2 | 开 `Keep 1 Health`（`All`） | 部位不再低于 1；致死伤害被拦截 |
| D3 | `Keep 1 Health Selection = Head And Thorax` | 仅头/胸受保护，四肢不保护 |
| D4 | 开 `Ignore Headshot Damage` | 爆头零伤 |
| D5 | 百分比组合（如全身 50、仅头 0） | 全身减半；头部独立计算 |
| D6 | 开 `No Falling Damage` | 高处跳下不受伤 |
| D7 | 开 `CODMode` | 受击后按 `Heal Wait` 延迟开始治疗（分部位回血）；`Bleeding Damage` 开关语义正确 |
| D8 | 全关状态：已黑部位再被击中 | 该次伤害被短路（AtMinimum 常驻保护——上游既有语义，**属预期，非故障**） |

## E. 便利组 QOL
| # | 操作 | 预期 |
|---|------|------|
| E1 | 开 `Infinite Stamina` / `Infinite Energy` / `Infinite Hydration` | 体力（含手部/氧气）/能量/水分不下降或即时回满 |
| E2 | 开 `Instant Production`，打开藏身处制造队列 | 制造即刻完成产出（比特币/燃料配方除外） |
| E3 | 开 `Instant Construction`，点藏身处升级 | 立即到「可安装」；重进藏身处时在建区域表现符合预期（边界：零时长/自动升级区域可能停在「可安装」需手动点一下——**属待确认边界，非必然故障**） |
| E4 | 开 `Enable Reload/Unload Mag Speed` 调数值；随后关闭 | 换弹/卸弹变快；关闭后恢复默认（0.85/0.3） |
| E5 | `Enemy Damage Multiplier = 5` | 对敌伤害提高（一枪对比） |
| E6 | `Item Total Weight % = 50`（**战局前设置**） | 物品总重减半（负重表验证） |
| E7 | 全关复核 | 上述行为回到常态（D8 例外为已知语义） |

## F. 回滚演练（可选）
| # | 操作 | 预期 |
|---|------|------|
| F1 | 禁用本覆盖层、重新启用 `[13]BotDebug` + `[13]DebugTooltip` | 源工具恢复工作（验证回滚路径） |

## 记录格式（回报示例）
```
A1: OK
B8: FAIL — ctrl-alt-c 复制内容为短 ID，期望 Tpl ID
E3: OK（普通区域） / 边界未触发
…
```
