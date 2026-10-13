# 双轴代码审查 · Round 1 — Spec 轴

> 审查员：独立 oracle 会话（ora-2，与实现会话隔离）｜2026-10-11
> 固定点适配：无 git 提交（用户裁决）；变更集 = `mods/SPT41-DebugToolkit/**` 全树（greenfield）。
> S1 接缝独立复跑：`dotnet build -c Release` 0 警告/0 错误；标准检查 PASS=13/FAIL=0/WAIVED=1（exit 0）。

## (a) 缺失 / 部分实现

- README 仍为占位（`README.md:13,18,27,83,91-95`）：偏差表未汇总——03/04/05/06 随单偏差（Chest 修正、ctrl 笔误修正、ActiveMode 字符串化、瞬建双路径、Task.CompletedTask）与「GodMode 坠落语义」「逐项降级清单」未落表；US42/US44 未收口。
- `THIRD-PARTY-NOTICES.md:8-27` 仅署名行，无三上游 MIT 全文；US47 部分达成。
- US45 部署及 07/08 全项未执行（`issues/07`、`08` 清单全未勾）——属已登记前沿，非 03-06 缺项。
- 03-06 范围内未发现缺项。

## (b) 额外行为（对照 Out of Scope）

- 未见越界（未触服务端/Fika/Forge/翻译/5.0 线；csproj 未定义 STANDALONE、未产变体）。
- 判定项：`InstantConstructionPatch.cs:90-118` 把 02 表 §3「降级备选」升为并行第二补丁点（已记 issue 06）；`QoLRaidComponent.cs:81-101` 停用时写全局默认（上游无，已裁定）。

## (c) 疑似实现有误

1. 瞬建双挂缺口：真实 `WaitUpgradeTime`（AreaData L510-534）的 `waitingTime < float.Epsilon` 分支会调 `CompleteUpgradeZone`；尾部 `DecideStatus`（L618-643）在 `ConstructionTime < epsilon || AutoUpgrade` 时还会 `Upgrade()/StartAutoUpgrade()`。前缀（`InstantConstructionPatch.cs:98-117`）跳过整链，零时长/自动升级区域将停在 ReadyToInstall 需手动安装；普通区域结果态等价（已逐行核对）。另 `InitConstructing` 前缀把 `alreadyUnderConstructing=true` 的「恢复构造中」分支也直接完成（`:60-78`；真实 L428-445 不完成）。建议 S3 定点验证。
2. `BotDebugComponent.cs:42-52` 只在 Awake 解析 botSpawner/gameWorld，失败即整局静默失效（`:161,:185` 永返）；同工程 Invincibility/QoL 均为惰性重试（`InvincibilityRaidComponent.cs:117`、`QoLRaidComponent.cs:123`），此点弱于工程基线。
3. 停用残留：`TooltipModule.cs:107-108` 只清 PriceTooltip 克隆；挂在 tooltip 上的 `TooltipCopier`（`TooltipCopier.cs:38` Update）未移除，停用后 ctrl-c 仍写旧文本——US43「组件无残留」字面缺口（停用仅发生于插件卸载，实际影响可忽略）。

## 核对通过项（独立反编译）

`EDamageType.Fall=2`、`WaitUpgradeTime` 存在、瞬产 `EndTime→Produce→Progress=1.0→ProcessDoneItems` 链成立；配置键名/默认/范围与 spec 表逐项一致（含与上游范围比对）；全部补丁体 fail-open 齐备。

## 编排者处置（2026-10-11）

- (a) 全部 → 工单 07 收口（README/NOTICES/部署），已在 07 派工前登记。
- (c)1 → 不改码；边界行为入 S3 清单 + 07 行为注记（普通区域逐行等价，边界待游戏内确认）。
- (c)2 → 修订轮修复（惰性重试，对齐工程基线）。
- (c)3 → 修订轮修复（Disable 清理 TooltipCopier）。
