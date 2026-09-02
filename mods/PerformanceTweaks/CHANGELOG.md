# PerformanceTweaks 更新日志

## v0.3.0（2026-08-19）

**主题**：4.1.2 审计成果反查 3114——祖传病灶进补 + 议会复核排雷。

### 背景

对 SPT 4.1.2 做扩大审计时发现一批"新病灶"。为确认它们是不是 3.11 就有的祖传病，
对 3114 反编译树做了两路反查（AI/动画/导航 + 渲染/散点），结论交议会复核后形成本版本。

### 新增补丁（2 个）

#### P13 本地AI的IK隔帧（`LocalBotFbbikFrameSkipPatch`）

- **病灶（复查确认）**：单机模式下每个 bot 的全身 IK（端枪姿势、手脚贴合地面）每帧完整解算
  `Player.FBBIKUpdate`（EFT/Player.cs:26050-26065），无任何隔帧。而 BSG 在联机观察玩家路径
  自己做了隔 3 帧（ObservedPlayerView.cs:847 `frameCount % 3 == 0`）——官方认可这种降级。
- **补丁**：Prefix 按距离分档——40m 内每帧（手感区，主玩家恒在此档不受影响）；
  40-80m 隔 2 帧；80m 外隔 3 帧（对齐官方观察玩家语义）。
- **游戏里**：可见 bot 的 IK 开销省 1/2 ~ 2/3，bot 密集近战时帧数更稳。
  副作用：远处 bot 在坡地可能轻微滑步，肉眼难察。
- **议会裁决**：进版（唯一高置信收益项）。与 P7 正交——P7 管 AI 逻辑帧，P13 管动画/IK 帧。

#### P14 AI扎堆斥力分帧（`LocalAvoidanceFrameSkipPatch`）

- **病灶（复查确认）**：每个 bot 每帧遍历同体素所有 bot 做互斥计算
  `GClass476.ManualUpdate`（GClass476.cs:47-87，BotMover.cs:273 每帧调用），
  扎堆场景（撤离点/门口/僵尸群）O(k²)。3.11 甚至比 4.1.2 还少一个 NearDoor 短路。
- **补丁**：体素内 bot ≥ 拥挤阈值（默认 4，可配 3-20）时斥力循环隔帧；
  跳过的帧手动调用 `method_0()`（public，GClass476.cs:89）保持偏移衰减连续；
  稀疏场景原样执行，不降频到 N>2。
- **游戏里**：撤离点/Boss 小队/僵尸潮等扎堆场景的帧尖峰减轻。
  副作用：推挤响应慢一帧（1/60 秒），站位精度无感。
- **议会裁决**：以保守形态进版。明确**禁止**直接分帧降频（斥力是防 bot 站位重叠的唯一机制）。

### 议会复核的排除项（没加，附理由）

| 项 | 排除理由 |
|---|---|
| 观察玩家 255 槽管线 | SPT 强制 LocalGame（ForceRaidModeToLocalPatch），NetworkGame 的 255 槽调度在离线局是死代码。侦察员的"SPT 每局活跃"被 7 条证据链推翻 |
| CullingManager 主线程循环 | "上限 10000"被误读为"常态 10000"——离线局实际注册对象只有几十到几百个动态光源，成本微秒级 |
| HairRenderer 每帧 GetComponent | 死代码：OnValidate 强制模式回落 StaticHeightBased，运行时分支不触发 |
| BTR 同步 Resources.Load | 每局一次（非每次生成），入局一次性成本，不划算 |
| Impostors 缓冲区重建 | 有脏标记门控，非每帧（4.1.2 报告的"每帧无脏标记"描述被两版代码共同推翻） |
| 不可见 bot 动画状态机降频 | 实测驱动待定：硬跳过会破坏动画事件时序（脚步/换弹回调），需 dt 补偿分帧 + 交错，先 profile 再定 |
| NavMesh carving 合批 | 禁止全局合批（门开但寻路堵死 = bot 卡门）；触发源级节流留作 backlog |
| 烟雾射线 / 100f 重连采样 / 火箭后喷 | 4.1 独有内容，3114 不存在 |

### 工程变更

- `src/Patches/LocalBotFbbikFrameSkipPatch.cs`（新增）
- `src/Patches/LocalAvoidanceFrameSkipPatch.cs`（新增）
- `src/PerfTweaksConfig.cs`：P13/P14 两组中文配置（P14 带"拥挤判定阈值"数值项）
- `src/Plugin.cs`：注册 P13/P14，版本 0.3.0，日志 "X/14 个补丁生效"
- 版本号 0.3.0（csproj + Plugin.cs）
- 编译 0 错误 0 警告，已部署至 `SPT_3114/BepInEx/plugins/`

### 事故记录

更新过程中用 PowerShell 直接改写 Plugin.cs/csproj 导致 GBK 编码灾难（PS 5.1 对无 BOM UTF-8
按 GBK 读取），文件中文全毁。已完整重建两文件并编译验证。教训已记入 KB
`3114-client-mod-build-gotchas.md` 第 5 条。

### 验证状态

- [x] 编译 0 错误
- [ ] BepInEx 日志确认 14/14（下次启动游戏时自查 LogOutput.log）
- [ ] 战局实测（建议重点观察：近战多 bot 时帧稳定性（P13）、撤离点/扎堆时尖峰（P14）、远处 bot 滑步是否可察（P13 副作用））

---

## v0.2.0（2026-08-17）

T1+T2 全量：P1-P12（AI 感知/寻路减负五件 + 引擎/杂项七件）。配置全中文化（人话精简版）。
9 仓源码兼容性审计零真冲突；QB 顺序敏感修复（P1 prefix Priority.Low）。
实测：灯塔平均 FPS 75.2→90.4（+20.2%），帧时间 -15.4%，1% low 持平。
详见 `docs/eft-0.16-性能分析与优化mod可行性报告.md` 与 `mods/PerformanceTweaks/STATUS.md`。
