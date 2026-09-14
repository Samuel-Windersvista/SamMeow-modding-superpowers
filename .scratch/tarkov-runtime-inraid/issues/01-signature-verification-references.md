# 01: 侦查核验——EFT 1.1.5 interop 成员签名 + 参考源登记

**What to build:** 产出后续桥实现的事实底座（Phase 2 局内状态）。用本地 SPT 5.0 客户端的 BepInEx interop 程序集核验并记录 bridge 所需的 EFT 1.1.5 成员签名：玩家（位置/朝向/姿态/血量总+肢体/存活）、raid 元数据（地图/raid 状态/剩余时间/raidId）、bot 枚举（数量/位置/类型/阵营/存活）。逐条给出证据与「可直接读 / 需转换 / 读不到」结论；不确定项显式列出供 02/04 票实测。同时登记参考源：本地已 fetch 的 `SP-Tushonka/modules@5.0x-dev`（tip `b5513e6`）中与状态读取/patch 相关的代码模式，以及生态先例（Web Minimap 的导出与 bot 枚举、bepinex-mcp 的 HttpListener 模式）的关键文件定位。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] 成员签名清单落盘，覆盖：GameWorld/MainPlayer 访问路径、位置与朝向、姿态状态、ActiveHealthController（总+肢体）、存活判定、raid 计时/元数据、bot 枚举与类型/阵营判定
- [x] 每条标注核验方式（ilspycmd 或运行时反射）与证据（文件/成员名）
- [x] 不确定/需运行时验证的项显式列出（供 02/04 票首刀实测）
- [x] 参考源登记：modules@5.0x-dev 本地路径 + 上游 commit + 相关文件清单；生态先例关键文件清单
- [x] 结论表可直接指导 04/05 票字段实现（无「待研究」残留）

## Comments

### 2026-09-14 orchestrator 执行（ticket 01 完成，评审抽查中）

- 产物：`../01-signature-verification-report.md`（成员签名清单 + 字段映射表 + 参考源登记 + 不确定项 7 条）
- 方法：ilspycmd 11 直读本地 SPT 5.0 客户端 interop（`Assembly-CSharp.dll` 54.1MB、`PlayerEnums.dll` 等）+ `tools/eft-classmap` 成员清单；与 SPT 官方 `modules@5.0x-dev` 既有用法交叉验证（双源证据）
- 关键结论：四域（访问链 / 玩家 / raid 元数据 / bot）**全部可读**；两处迁移注意——① `GameTimer` 无 4.x 的 `EscapeTimeSeconds()`（剩余时间改用 `SessionTime − PastTime`）；② `EBodyPart` 声明在 `PlayerEnums.dll`（跨程序集引用）
- 最高优先不确定项：`GetBodyPartHealth` 返回 interop 包装 `ValueStruct` → T02/T03 运行时实测取值
- 参考源：modules@5.0x-dev（tip `b5513e6`）+ 两个生态先例 clone 至 `external/references/`（已加入 `.gitignore`）
- 独立抽查（@oracle，已完成并核销）：**1 处实质错误**——`EscapeTimeSeconds` 实为 `EFT.GameTimerExtension` 扩展方法（原判「不存在」有误，已修正为主路径）；**4 处瑕疵**——`StopDateTime` 非 Nullable、总血量路径补 `EBodyPart.Common`、`ValueStruct` 取值明确为 `.Current`、BotMonitor 行号校正。均已修正（报告 v2 勘误记录）；验收标准 5 条覆盖确认通过。
