# 01: 工程脚手架与构建接缝（曳光弹）

**Spec:** `.scratch/spt41-debugtoolkit/spec.md`

**What to build:** 在 `mods/SPT41-DebugToolkit` 建立最小可构建、可通过标准检查的 SPT 4.1.6 客户端插件工程骨架：netstandard2.1、BepInEx 5 / Managed / spt plugins 引用、`SptRoot` / `SPTInstallPath` 参数化（默认 `$(SptRoot)\SPT_41x`）、扁平输出、release 复制（`-p:SkipCopyToRelease=true` 可关）、`scripts/package.ps1` 雏形；插件入口（BepInPlugin 同身份 `com.sammeow.debugtoolkit` / `SamMeow.DebugToolkit` / 0.1.0、`BaseUnityPlugin` 生命周期、FilePrivatePart 40743 版本门禁仅警告 fail-open、单一日志源、6 节配置骨架（1. 通用 / 2. 提示信息 / 3. Bot 覆盖层 / 4. 无敌 / 5. 恢复 / 6. 便利）、4 个模块注册点约定——各模块独立入口类、空实现，主插件按序调用）；README + LICENSE + THIRD-PARTY-NOTICES 骨架。本单**不含部署**（用户裁决：构建先行，部署留到 07）。

**Blocked by:** None（可立即开始）

**Status:** ready-for-agent

- [x] `dotnet build -c Release` 0 error；`-p:SptRoot="<其他路径>"` 覆盖可用（主控台复跑：0 warning / 0 error；默认 `E:\Game\EFT_Offline\SPT_41x`，`-p:SptRoot=D:\Other\SPT` → `D:\Other\SPT\SPT_41x`）
- [x] `scripts/check-mod-standard.ps1 -ModPath mods/SPT41-DebugToolkit -TargetSptVersion 4.1.6 -Kind client` 通过（PASS=13 FAIL=0 SKIP=0 WAIVED=1，exit 0；STD-CLI-003 豁免理由见 `MODDING-STD-WAIVER.md`）
- [x] 工程布局：`src/`、`README.md`、`LICENSE`、`THIRD-PARTY-NOTICES.md`；版本号三处一致（csproj / `Plugin.cs` / README 均 `0.1.0`）
- [x] 插件骨架：版本门禁读 `EscapeFromTarkov.exe` FilePrivatePart=40743，不匹配仅 LogWarning、绝不 throw；6 节配置骨架与 4 模块注册点已定义
- [x] 无 MO2 写入、无游戏目录写入（本单零部署）

**上游参考（本仓归档）:** `knowledge/spt-kb/archive/forge/mods/` 三源源码；工程样板 `mods/PerformanceTweaks413`（4.1 线构建接缝）、`mods/SPT5-*`（组织惯例）；模板 `templates/client-mod`。

## Comments

- 2026-10-11（编排者）: **完成**（fix-1 施工 + 主控台新鲜复核全绿）。
  - 构建：`dotnet build -c Release` → 0 warning / 0 error；扁平输出 `bin\Release\SamMeow.DebugToolkit.dll`；`release\` 复制就位。
  - 标准检查：PASS=13 / FAIL=0 / SKIP=0 / WAIVED=1（STD-CLI-003：本工程统一 `SPT.Reflection.Patching.ModulePatch`、零 `[HarmonyPatch]` 特性，属有意偏离；待补丁工单落地时复核）。
  - 参数覆盖三态：默认 / `-p:SptRoot` / `-p:SPTInstallPath` 均正确。
  - 实现决策：共享日志源 `internal static new ManualLogSource Logger`（显式隐藏 `BaseUnityPlugin.Logger` 实例属性，避免遮蔽）；`1. 通用` 只读项在 Settings 内联构造（暂不抽 ConfigUi，留待需要显示名工厂时）；`package.ps1` 带 UTF-8 BOM（PS 5.1 中文注释解析）。
  - 旁证：源码零 IL2CPP 形态（TryCast/ClassInjector/IntPtr/Il2Cpp/SPTushonka）；未做 git 写操作；未触碰游戏目录 / MO2。
  - 产出：15 个新文件于 `mods/SPT41-DebugToolkit/`；下一个前沿 = 工单 02（触点核实，三通道并行）。
