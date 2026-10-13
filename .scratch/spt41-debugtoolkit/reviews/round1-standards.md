# 双轴代码审查 · Round 1 — Standards 轴

> 审查员：独立 oracle 会话（ora-1，与实现会话隔离）｜2026-10-11
> 固定点适配：无 git 提交（用户裁决）；变更集 = `mods/SPT41-DebugToolkit/**` 全树（greenfield）。
> 工具已强制项（不复报）：`check-mod-standard` PASS=13/FAIL=0/WAIVED=1；构建 0 warning/0 error。

## (a) 硬违规（文档化标准）

1. 「补丁全撤销 + 逐项降级」未达标 —— spec「补丁与健壮性」「配置与健壮性」（US42/43）：
   - `InvincibilityModule.cs:36-43`：三个补丁顺序装配于单一 try；`:56-59` Disable 以 `if (!_enabled) return` 早退。中途补丁 Enable 抛异常（版本漂移下补丁点缺失的典型场景）→ 已装配补丁永不撤销、加载/卸载不可逆。
   - `QoLModule.cs:39-52 / 65-68` 同形；`BotOverlayModule.cs:48-57 / 62-65` 同形（另有配置订阅残留）。
   - 正确形态见 `TooltipModule.cs:128-139`（逐项 try/catch + 列表撤销），但未横向复用。
2. 偏差记录制度未落地 —— spec「偏差记录制度」、US42/44：
   - `README.md:91-95` 偏差表仍为「（待补）」；必录项：`BotInfo.cs:329`（Chest Broken 字段修正）、`TooltipCopier.cs:16-17`（ctrl 键笔误修正）、GodMode 坠落语义——零登记。
   - `MODDING-STD-WAIVER.md:7` 理由仍称「工单 01 纯脚手架、尚无补丁类」，与现状不符，应按 spec 约定重写理由。
3. 低危：`scripts/package.ps1:60` 默认输出 mod 内 `dist/`；根 `.gitignore` 锚定 `/dist/` 不覆盖子目录 → 打包产物可能被误提交（bin/obj 已被 `**/` 覆盖，无问题）。

## (b) 判断项（Fowler 坏味）

1. 重复代码：`ConfigDescription` 构造同形五处 —— `Settings.cs:41-53`、`TooltipSettings.cs:68-78`、`BotOverlaySettings.cs:143-156`、`InvincibilityConfig.cs:151-164`、`QoLSettings.cs:112-125`；应抽共享工厂。
2. 重复 + 发散式变化：四模块补丁管理模板重复且已分叉（列表式 vs 字段式）——即 (a)1 的根因；统一到共享注册器可一并消除。
3. 神秘命名：`BotDebugComponent.cs:399` 包装类 `BotData` 与 EFT `DebugBotStruct.BotData`（`:221-227` 同页并用）同名异义，建议改 `BotOverlayEntry`。
4. 夸夸其谈通用性（死代码）：`DebugTooltipState.cs:17-20` 的 `SetDebugInfo(string)` 与 `DebugInfo.cs:33-45` 的 `BasicDebugInfo` 全工程零调用者——删。
5. 微：`EnumExtension.cs:11-33` Next/Previous 同形两份。（接受：不动作）

## 合规面抽检（通过）

零 `[HarmonyPatch]`（IL2CPP 字样仅现于注释）、无空 catch / `Console.Write`、键名英文 + 显示名中文、列表下拉 + 数值滑块、单一日志源、中文注释、版本门禁仅警告。

## 编排者处置（2026-10-11）

- (a)1 + (b)1,2 → 修订轮：共享补丁集注册器 + 四模块生命周期改造（Disable 无条件清理、不得早退）。
- (b)1 → 共享 `ConfigDescription` 工厂（5 处迁移）。
- (b)3 → 改名 `BotOverlayEntry`；(b)4 → 删死代码；(a)3 → 根 `.gitignore` 增 `**/dist/`。
- (a)2 → 移交工单 07（README 偏差表 + waiver 措辞 + NOTICES 全文）。
