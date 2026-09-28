# 部署卫生与文档修订建议（收尾清单）

- 日期：2026-09-20
- 范围：本次修复涉及的部署事实记录 + 源码/文档不一致清单（按用户选择"部署卫生收尾"整理）
- 性质：记录与建议；未改动任何游戏/仓库文件

---

## 1. 客户端 Assembly-CSharp.dll 改写（事实记录）

- 部署版：`E:\Game\EFT_Offline\SPT_3114_Novinsk_Edition\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`
  - 大小 15,243,264 B；mtime 2026-09-02 22:23:52；SHA256 前缀 `12D5FD17…E10F47D5`
- 原版备份：同目录 `Assembly-CSharp.dll.spt-bak` 与 `Assembly-CSharp备份\EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`
  - 大小 15,406,648 B；mtime 2025-03-10 10:15:04；SHA256 前缀 `904F4158…56043FE0`（两者一致）
- 改写特征：净 +1 类型；匿名类命名从控制符名改为 `Class0/Class1/…`（Mono.Cecil 重写特征）；改写工具与目的未知。
- 结论：NRE 路径相关类型（BotBoss / GClass545 / PatrolDataFollower / GClass241 等）与原版**逐行一致** → 改写与本次症状无因果。
- 建议：保留 `.spt-bak` 与备份目录；后续升级/重装客户端时注意该改写会丢失（需重做或记录来源）。

---

## 2. MO2 部署事实（现状）

- 实例：`C:\Users\Winde\AppData\Local\ModOrganizer\诺瓦克优化包`
  - gamePath = `E:\Game\EFT_Offline\SPT_3114_Novinsk_Edition`；base_directory = `E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2`；profile = 单机运行整合包
- 关键 mod 与版本：SAIN 4.4.0（Moew fork）、LootingBots 1.6.1（Moew fork）、BigBrain 1.3.2、friendlyPMC 4.4.12（"合并版本"启用；"已AI优化"版在 profile 中禁用——冗余，可删）、FFT 1.3.0 + SAINAddon 1.4.0、BossNotifier 1.6.0、friendlyPMC-Optimizer 1.0.0（本地特制）。
- 本次新增/更新的覆盖层：
  - `[6]AI修复-护卫NRE护栏-FollowerGuard`（新增；0.1.0 → 0.1.1）
  - `[6]AI修复-friendlyPMC护栏-FriendlyPmcGuard`（新增）
  - LootingBots / SAIN DLL 更新（明细见 `fix-plan.md` 执行日志）

---

## 3. SPT 源码仓库文档/代码不一致（建议修订）

- 仓库：`E:\云文件\GitHub\SamMeow_SPT3114_source_code`（README 自称 3.11.5-Live-In-Norvinsk-Edition）
- 不一致点（exp-3 取证）：
  - CHANGELOG/README 宣称的 server 修复（ApplicationContext 会话化、unhandledRejection、RandomUtil ECO-5、BotLootGenerator HIGH-5 等）在实际源码树中**不存在**（已回滚/未实施；`.superpowers/sdd/optimization-plan-staged/progress.md` 记录 PLAN TERMINATED）。
  - 建议二选一：a) 按文档补齐代码（需重新评估风险）；b) 修订 README/CHANGELOG 标注"已回滚/未实施"。
- modules 特制化（保留现状即可）：2 个 Stopwatch transpiler 改写（静态等价）+ `CustomAiPatch` 角色随机化 + 若干错误处理修复。
- 另：`release/3.11.5` 打包产物与源码树的一致性无法核对（pkg 打包）。

---

## 4. 其它观察（低优先级）

- LB cfg 距离键调整历史：900 → 150（M1）→ 250（用户在 F12 调整）；A4 后阈值见 P2-1 批次。
- SAIN 行为配置真实入口是 SAIN Editor（F6），F12 cfg 仅含 Editor 绑定（属正常现象）。
- 本会话创建的文档与产物：
  - `.scratch\lootingbots-idle-diagnosis\diagnosis-report.md`（含 R3 修订记录）
  - `.scratch\lootingbots-idle-diagnosis\fix-plan.md`（含执行日志）
  - `.scratch\lootingbots-idle-diagnosis\loot-price-chain-analysis.md`
  - `.scratch\lootingbots-idle-diagnosis\deployment-hygiene-notes.md`（本文件）
