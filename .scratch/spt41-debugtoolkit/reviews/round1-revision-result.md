# 双轴代码审查 · Round 1 — 修订轮结果

> 执行：fix-5（独立 fixer 会话）｜复验：编排者（2026-10-11，新鲜证据）。

## 落地内容（对照 round1-standards / round1-spec 处置）

- **A（硬性）**：新增 `src/PatchSet.cs`——共享补丁集注册器：`Enable()` 逐项 try/catch + 成功记账（含去重）；`DisableAll()` 逐项 try/catch 撤销 + 清空；幂等、全 fail-open。四模块（Tooltip / BotOverlay / Invincibility / QoL）全部迁移；`Disable` 无条件清理（无 `_enabled` 早退），配置订阅逐项退订。
- **A4**：`BotDebugComponent` Awake-only 解析改 `TryResolveReferences()` 惰性重试（对齐 `InvincibilityRaidComponent`/`QoLRaidComponent` 基线）。
- **B**：新增 `src/ConfigDescriptions.cs`——工厂 `Describe(displayName, description, acceptableValues, readOnly)`；5 处同形私有实现迁移；逐键显示名/描述/默认/范围不变。注：`readOnly` 为向后兼容超集扩展（用于 Settings 版本只读项，复刻原 `"ReadOnly"` 标签行为）——编排者接受。
- **C**：`BotData`→`BotOverlayEntry` 改名（引用点同步）；删除死代码 `SetDebugInfo(string)` + `BasicDebugInfo`（零调用已确认）。
- **D**：`TooltipModule.Disable` 新增 `TooltipCopier.RemoveAll()`（组件实例登记 + 临时文本清理 + 仅销毁组件本体）。
- **E**：根 `.gitignore` 追加 `**/dist/`（L36）。
- **F**：两条不改码注记复述保留——① 瞬建边界语义（供 07 行为注记 + S3）；② 换弹还原范围差异（已裁定）。

## 复验（编排者，主控台新鲜证据）

- `dotnet build -c Release` → **0 warning / 0 error**；
- `check-mod-standard.ps1`（4.1.6 / client）→ **PASS=13 / FAIL=0 / SKIP=0 / WAIVED=1**（exit 0）；
- 抽读：`PatchSet.cs`（逐项记账 + 幂等）✓；`ConfigDescriptions.cs`（工厂 + readOnly 分支）✓；`InvincibilityModule.cs`（PatchSet 迁移 + 无条件清理）✓；`.gitignore` L36 `**/dist/` ✓。

## 关闭状态

Round 1 双轴 findings 全部关闭：硬性 2 项（补丁生命周期、死代码/命名等级联）+ 低危 1 项（忽略规则）已修；判断项按处置表执行或明示接受（EnumExtension 微重复：接受不动作）。遗留（非本阶段范围）：README/NOTICES/waiver → 工单 07；瞬建边界 → S3（工单 08）。
