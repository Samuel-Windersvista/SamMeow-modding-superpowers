# C9 · 游戏侧复制分叉与硬编码路径 — 实施规格（安全子集）

> 来源：架构审查候选 C9 + grilling 确认（2026-09-17）。报告 §2 C9 卡（组 D · 资产与发布；P3 观望 → 安全子集解冻）。
> 侦察：`exp-4` 游戏侧触点清点（2026-09-17）。
> **进展（Work Status）**: CLOSED — 2026-09-17（全流程完成：车道 A 交付 + 双轴评审无 BLOCKER + 修复轮 F1-F6/R1-R6 全落地 + 编排者终验 bootstrap 12/12；未提交）

## 决策记录（grilling 确认）

| # | 决策 | 结论 |
|---|------|------|
| D1 | 范围 | **安全子集**：①路径可移植化 ②pilots 退出机制 ③release DLL 可复现验证；**共享核心重构延后**（与实测期不冲突） |
| D2 | 路径方案 | `Directory.Build.props` 集中 `SptRoot`（env / `-p:` 可覆盖 + 缺失响亮失败）；**放置于 `mods/` 与 `tools/migration-pilots/` 两处**（避免根级 props 干扰 `tools/` 其他工程）；默认值按现实校正 |
| D3 | release DLL | 保持入仓 + 构建再生 + hash 漂移验证；**验证不得污染已入仓 DLL**；若发现漂移 → 报告后单独裁决是否替换 |
| D4 | pilots 退出 | 加 `STATUS.md`（状态 / owner / 关闭条件 / 归档去向），保留原位；归档动作待实机验证完成后 |
| D5 | 共享核心 | 延后；目标形态记录：纯逻辑骨架（DetectSain / TryPatch / 配置模式 / 补丁骨架）→ shared core；API 绑定（目标类型/方法/字段/配置键/补丁表）→ 版本薄壳。触发条件 = 实测期结束 + 需求出现 |

## 事实基线（exp-4 侦察）

- **双树分叉**：`mods/PerformanceTweaks`（3.11.x / net471 / 混淆名）vs `mods/PerformanceTweaks413`（4.1.x / netstandard2.1 / 真名）；18 个同名文件（416+/204− diff）；差异 = API 差异（必须分叉：命名空间/GUID/补丁 14→16/目标类型方法名/Unity 模块引用）+ 复制漂移（注释/标点，无实质逻辑差异）。
- **硬编码路径**：`mods/PerformanceTweaks/PerformanceTweaks.csproj:19` = `E:\Game\EFT_Offline\SPT_3114`；`mods/PerformanceTweaks413/PerformanceTweaks413.csproj:19` = `SPT_41x`；`mods/SPT5-*/**.csproj` = `SPT_5xx`；`tools/migration-pilots/{ett,secure-mapbook,skills-extended}/*.csproj` = `SPT_410\SPT_Runtime`（**SPT_410 不存在**）/ `SPT_410\`（客户端 TarkovDir）。无 mods 级 props。
- **release DLL**：两 DLL 均入仓（`SamMeow.PerformanceTweaks.dll` / `SamMeow.PerformanceTweaks413.dll`）；311 ≈ 同步；**413 比 src 晚 15 天**；`CopyToRelease` target 于 `dotnet build -c Release` 后覆盖 release/。
- **pilots**：ett / secure-mapbook 无任何状态标记、无实机验证记录；skills-extended 服务端完成、客户端编译 0 错但**实机未验证**（`PROGRESS.md:43` 未勾选）；三者均无退出机制。
- **部署现状**：413 DLL **未部署**到 `SPT_41x\BepInEx\plugins`（与文档"已部署"矛盾——属游戏侧动作，不在本单执行）。
- **实测状态**：3.11 线 = 用户实机测试期（`STATUS.md:3` / handoff-20260819 / 路线图一致）；CHANGELOG 验证项未勾选。

## 目标设计

### 1) 路径可移植化（D2）

- 新建 `mods/Directory.Build.props` 与 `tools/migration-pilots/Directory.Build.props`：

```xml
<Project>
  <PropertyGroup>
    <SptRoot Condition="'$(SptRoot)' == ''">E:\Game\EFT_Offline</SptRoot>
  </PropertyGroup>
</Project>
```

- 各 csproj 的硬编码默认值改为 `$(SptRoot)\<sub>` 形态（`SPT_3114` / `SPT_41x` / `SPT_5xx` / `SPT_41x\SPT_Runtime`——**先核实 `SPT_41x\SPT_Runtime` 实际存在性**，不存在则定位真实运行时路径）；`-p:SPTInstallPath=...` / `-p:SptRoot=...` / 环境变量仍可覆盖。
- 缺失响亮失败：引用缺失时构建自然失败（如需显式校验由实现决定）。
- **文档纠偏**：mods 自身文档（`STATUS.md` / `CHANGELOG.md`）与 pilots 文档中的 `SPT_410` 引用与"已部署"陈述按实际修正（413 标注"待部署验证"）。

### 2) pilots 退出机制（D4）

- 新建 `tools/migration-pilots/{ett,secure-mapbook,skills-extended}/STATUS.md`：状态（完成度）/ owner / 验证状态（ett·secure-mapbook：无实机验证记录；skills-extended：客户端实机未验证）/ 关闭条件 / 归档去向（`examples/`，待验证完成后执行）。

### 3) release DLL 可复现验证（D3）

- 新脚本 `scripts/verify-mod-release-dlls.ps1`：对两 mod 重建 → SHA256 对照已入仓 DLL → 报告（**不得覆盖已入仓 DLL**：临时输出 / 条件化 CopyToRelease / 构建后 `git checkout -- release/` 还原，三选一）。
- 已知线索：413 DLL 晚于 src 15 天（预期可能漂移或为重复构建产物，报告为准）。

### 4) 共享核心（延后，仅记录形态）

- 见 D5；本单不改 `src/**`。

## 已声明行为 delta

1. 构建配置变化（路径解析集中化 + 可覆盖）；**mod 运行时行为零变更**（路径配置不影响编译产物内容）。
2. 文档纠偏（SPT_410 → 实际路径；部署陈述按实际状态）。
3. pilots 新增 STATUS 标记（纯增量）。
4. 新脚本（验证用，不影响构建/运行）。
5. release DLL：验证后若无漂移则不变；有漂移 → 报告后裁决（不在本单自动替换）。
6. 修复轮延伸（评审后）：3 个漏网 tools csproj（dbdump-mod / locale-test-mod / warsaw-trader-mod）+ `tools/tarkov-active-probe/TarkovActiveProbe.csproj` + `tools/tarkov-runtime-bridge/Directory.Build.props` 同类路径可移植化；skills-extended 服务端 csproj 加 client-src glob 排除（534 错 → 0 错）。
7. 修复轮记录项：secure-mapbook STATUS 体积转录纠正（1.94→4.94 MB）；验证脚本头部措辞（exit-1 语义）；CRLF 事故与 release 还原痕迹记录。

## 车道任务

### 车道 A（fixer）：路径 + pilots 标记 + DLL 验证脚本 + 文档纠偏
1. 核实 `SPT_41x\SPT_Runtime` 与各默认目录存在性。
2. 两处 `Directory.Build.props` + 全部 csproj 改造（2 mods + 2 SPT5 + 4 pilots csproj）。
3. 构建验证：各 mod 以默认路径与 `-p:SptRoot=` 覆盖各构建一次；核对 release DLL 未被意外覆盖。
4. pilots `STATUS.md` ×3。
5. `scripts/verify-mod-release-dlls.ps1` + 运行 → hash 报告。
6. 文档纠偏（mods/pilots 自身文档）。
7. 验证矩阵全跑。

### 车道 B（orchestrator）：终验 + review + 记录
- 全量矩阵 → oracle 双轴 code-review → 修复轮 → dev-log + spec CLOSED。

## 验证矩阵

| # | 命令/动作 | 通过标准 | 车道 |
|---|-----------|----------|------|
| A1 | `dotnet build mods/PerformanceTweaks -c Release` 与 413 | exit 0；`git status` release/*.dll 无未声明变化 | A |
| A2 | `-p:SptRoot=<临时根>` 覆盖构建 | exit 0（可覆盖性成立） | A |
| A3 | `powershell -File scripts/verify-mod-release-dlls.ps1` | 输出 311/413 hash 对照报告 | A |
| A4 | pilots `STATUS.md` ×3 | 含状态/owner/关闭条件 | A |
| A5 | `git grep "SPT_410"`（全仓） | 剩余命中逐条分类（历史文档保留 / 其余清零） | A |
| B1 | `verify-all.ps1` | 12/12（回归） | B |
| B2 | oracle 双轴评审 | 无 BLOCKER（或修复轮闭环） | B |

## 修复轮补记（2026-09-17，oracle 双轴评审后；全部落地，终验通过）

| # | 项 | 处置 |
|---|----|------|
| F1 | `secure-mapbook/STATUS.md` 体积转录 1.94→**4.94 MB** | 已改 |
| F2 | skills-extended 服务端 glob 缺陷（client-src 混入 net10.0 编译，534 错） | `DefaultItemExcludes` 排除 client-src/bin/obj → **0 错**（1 既有 CS9113 警告；STATUS 措辞校正） |
| F3/R3 | 漏网硬编码同类修复（3 tools csproj + active-probe csproj + bridge props） | 内联/局部 `SptRoot`；构建 exit 0；覆盖链实测（`-p:SptRoot` 正确派生） |
| F4 | 验证脚本头部 exit-1 措辞过强 | 已改（"需人工裁决；本机预期为 PE 溯源元数据差异"） |
| F5/R4 | CRLF 事故残留（4 文件 LF）+ release 还原痕迹 | 记录（纯告警）；事故经双轴复核闭环（XML 15/15、首字符完好） |
| F6 | 默认根定义点 6 处（2 props + 4 内联） | 记录（漂移风险；后续可评估合并） |
| R1 | D3 报告 + "不替换"裁决 | dev-log 落盘（311 `47E50BE3…` / 413 `F02BB894…`；签名集 diff=0；确定性重建） |
| R2 | A5 分类依据（KB curated 8 命中） | dev-log 落盘（运行参考类；`.scratch/c6-state-authority` 已有路径更正工单） |
| R5 | 服务端构建证据 | 已复跑（exit 0）+ STATUS 注记 |
| R6 | dev-log 预存改动（WPS 条目）与 C9 条目分列 | 提交口径注记 |

验证（终验 fresh）：bootstrap **12/12**；全部受影响工程构建 exit 0；`SPT_410` 29 命中全历史/KB；XML 解析 15/15；release DLL 与 HEAD 逐字节一致（防污染成立）。

## 风险与逃生方案

- R1 `SPT_41x\SPT_Runtime` 实际不存在 → 定位真实运行时路径后再改（报告为准）。
- R2 构建覆盖已入仓 DLL → 验证流程显式防污染（矩阵 A1 核对 git status）。
- R3 413 DLL 与源码重建不一致 → 报告 + 单独裁决（不自动替换）。
- R4 props 意外影响其他工程 → 仅放 `mods/` 与 `tools/migration-pilots/`（不放根级）；构建验证覆盖。

## 红线

- 不 commit；**不改 `mods/**/src/**` 与 pilots 源码**（仅构建配置 / 文档 / 标记）；**不重建并替换已入仓 release DLL**（除裁决后）；**不写入游戏安装目录 `E:\Game\**`**（只读引用）；不碰 `templates/**`、`knowledge/spt-kb/archive/**`、`external/**`；中文注释与回复；无 emoji。
