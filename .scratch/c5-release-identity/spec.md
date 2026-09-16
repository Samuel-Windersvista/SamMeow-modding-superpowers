# C5 · 发版身份契约归一 — 实施规格

> 来源：架构审查候选 C5 + grilling 确认（2026-09-17）。报告：`D:\Temp\architecture-review-20260916-1337.html` §2 C5 卡（组 D · P1 杠杆）。
> 侦察：`exp-1` 全仓身份/版本触点清点（2026-09-16）。
> **进展（Work Status）**: CLOSED — 2026-09-17（全流程完成：车道 A/B 交付 + 双轴评审无 BLOCKER + 修复轮 6 项落地 + 编排者终验 bootstrap 11/11；未提交）

## 决策记录（grilling 确认）

| # | 决策 | 结论 |
|---|------|------|
| D1 | 版本模型 | **统一插件组件版本**：单一源 = 根 `package.json` 的 `version`；Node 工具 `scripts/version/sync-version.mjs`（默认 check / `--write` 传播 / `--set <v>` 可选）；bootstrap 第 11 项常驻断言；退役 `scripts/bump-version.sh` 与 jq 依赖 |
| D2 | 标签与 pin | 发布 tag 约定 `v0.2.0`（`-spt` 后缀退役）；`RELEASE-NOTES.md` 当前段标题同步 `v0.2.0`；`.opencode/INSTALL.md` pin = 真实 URL + `#v0.2.0`；**打标属发版动作（不在本单）** |
| D3 | 身份禁入检查 | 操作性表面定向：`scripts/**`、`package.json`、`.opencode/**`、`tools/mo2-control-plane/**`、`tools/*/package.json`；模式 = `bgs-modding-superpowers`、`awesome-bgs-mod-master`、`BGS_MODDING_SUPERPOWERS`（大小写不敏感）；`BGS` 裸词不禁（Bethesda 格式术语与谱系叙述合法；`BGS_` 环境前缀已被既有 verify-bootstrap-injection 覆盖） |
| D4 | package.json 元数据 | `repository`/`homepage`/`bugs` → origin（`https://github.com/Samuel-Windersvista/SamMeow-modding-superpowers`）；上游署名保留于 README/skills，不重复 |
| D5 | 文档清扫口径 | **连历史档案一并规范化**（Q5-B）：指针/操作性引用（路径、树名、安装命令、现状陈述）→ 归一为现名；事件记录（旧名是事件主体，如"移除 bgs 树""幽灵物化"）与署名/谱系（Forked from / BGS lineage）→ **保留原文**；版本串：现役文档归一，历史条目保留 |
| D6 | MO2 资源审计器 | 归一 Mo2 前缀：类 `Mo2AssetsInspectorPlugin`、`NAME = "Mo2AssetsInspector"`（与支持树/模块名一致）、标题 "MO2 资源审计器" / "MO2 Assets Inspector" |
| D7 | LICENSE | 保留上游版权行 + 追加 fork 行 `Copyright (c) 2026 SamMeow-modding-superpowers contributors`（根 + `tools/tarkov-runtime-bridge/` 两处；`tests/mod-standard/fixtures/**` 的 LICENSE 是夹具，不动） |
| D8 | 版本目标边界 | 纳入 = 插件组件（mcp-kit / spt-mcp / mo2-mcp / tarkov-runtime-mcp / mo2-mcp-sidecar / mo2-assets-engine / tarkov-runtime-bridge / tarkov-active-probe）及其常量与 lockfile；**排除** = `tools/dbdump-mod-311`、`tools/dbdump-mod`、`tools/locale-test-mod`、`mods/**`、`templates/**`（独立生命周期 / 占位符驱动） |

## 事实基线（exp-1 侦察实测）

**版本散落面**：根 `package.json:3` = 0.2.0（repository/homepage 指向 BGS 上游，无 author/bugs）；`tools/{mcp-kit,spt-mcp,mo2-mcp,tarkov-runtime-mcp,dbdump-mod-311}/package.json` = 0.1.0 + 各 lockfile 顶层 0.1.0；`tools/mo2-mcp-sidecar/pyproject.toml:7` = 0.1.0；`tools/mo2-assets-engine/pyproject.toml:7` = 0.1.0-dev0 + `src/mo2_assets_engine/__init__.py:3` `__version__` = 0.1.0-dev0；`tools/tarkov-runtime-bridge/TarkovRuntimeBridge.csproj:8`、`tools/tarkov-active-probe/TarkovActiveProbe.csproj:11`、`tools/dbdump-mod/SptDbDump.csproj:11`、`tools/locale-test-mod/LocaleTest.csproj:11` `<Version>` = 0.1.0；`tools/{spt,mo2,tarkov-runtime}-mcp/src/index.ts` `SERVER_VERSION` = "0.1.0"；`tools/tarkov-runtime-bridge/src/Plugin.cs:36` `PluginVersion` = "0.1.0"。

**工具链**：`scripts/bump-version.sh`（bash+jq，只读 `.version-bump.json` 的 `files[]`；当前仅根 package.json；audit 注明"人工负责"）；`CONTRIBUTING.md:62-68` 引用之；`.version-bump.json` 含 `audit.exclude`（RELEASE-NOTES / CHANGELOG / docs/internal / .artifacts / .opencode / .external-resource / node_modules）。

**身份命中（bgs 模式，操作性）**：`scripts/start-mo2.ps1`（:6 注释 "The bgs-modding-superpowers agent harness…"；:32 示例路径 `D:\awesome-bgs-mod-master\.artifacts\mo2`；:64 `Write-Host "bgs-modding-superpowers: start MO2"`）；`scripts/install-mo2-control-plane.ps1`（:3 synopsis；:58 `Write-Host "bgs-modding-superpowers MO2 control plane install"`）；`tools/mo2-control-plane/live-bridge/mo2_assets_inspector/plugin.py`（:1 注释、:22 class、:23 NAME、:61 注释、:134-135）；`localization.py:41,63` 窗口标题。

**文档陈旧面**：`docs/全局路线与进度报告.md:13`（"当前版本 v0.2.0-spt"）、:52（"plugins/ 仅有 bgs-modding-superpowers 一棵打包树"）、:105（"根 .opencode/plugins/bgs-modding-superpowers.js 121 行旧版"）；`docs/internal/superpowers/specs/2026-05-13-xedit-native-adoption-design.md:113`、`docs/internal/superpowers/plans/2026-04-23-mo2-gui-blocker-auto-handling.md:605,614,761`（`D:\awesome-bgs-mod-master` 路径）；`docs/internal/specs/session-wiring-contract.md:17,37,38`（待分类）；`RELEASE-NOTES.md:3`（`v0.2.0-spt（未发布）`）、:23（"移除物化的 plugins/bgs-modding-superpowers 树" = 事件记录，保留）；`.opencode/INSTALL.md:77,86`（`<owner>` 占位）；`skills/using-spt-modding-superpowers/SKILL.md:155`（Forked from = 署名，保留）。

**杂项**：`tools/mo2-vfs-launcher/lib/xedit-client.launch.ps1:20` 注释引用已不存在的 `setting-up-bgs-modding-environment`（指针类，顺手归一）；`tools/mcp-kit/src/schema.ts:76` 注释旧路径、各 tools 测试内 `D:/awesome-bgs-mod-master/...` 夹具路径（**超范围，只报告不改**）；仓库无 `v*` tag、无 `.github/`、无发布脚本；无 `bgs-*` 目录/文件（陈旧树已清，`verify-layout` 已断言缺失）。

## 目标设计

### 1) 版本工具（新）`scripts/version/sync-version.mjs`

- Node ESM（沿用 `scripts/spt-kb/*.mjs` 惯例）；读 `.version-bump.json`（扩展 schema）+ 根 `package.json` version（单一源）。
- 模式：默认 = check（逐目标比对源；有 drift → 打印清单 + exit 1；干净 → exit 0）；`--write` = 将源版本写入全部目标（幂等）；`--set <v>`（配合 `--write`）= 先更新源再传播（可选便利）。
- 目标 kind 与写回方式：
  - `json`：点路径 set（数字段 → 数组下标；`field` 语义与旧工具一致）。
  - `npm-lock`：lockfile 顶层 `version` + `packages[""].version` 两处同写。
  - `toml`：`[project]` 区 `version = "..."` 行替换（正则，保留其余字节）。
  - `xml`：首个 `<Version>...</Version>` 替换（csproj；保留缩进/其余字节）。
  - `code`：按注册的 `pattern`（带捕获组）替换该行值。
- 退出码：0 = 一致；1 = drift（check）；2 = 配置/IO 错误。输出逐目标 `[ok]` / `[drift]` / `[write]` 行 + 汇总。
- 源版本非 semver（`^\d+\.\d+\.\d+` 不匹配）→ 响亮报错。
- 编码：UTF-8；不改动目标文件除替换区外的字节（含行尾风格）。

### 2) `.version-bump.json`（扩展为版本注册表）

```jsonc
{
  "source": { "path": "package.json", "field": "version" },
  "files": [
    { "path": "tools/mcp-kit/package.json",            "kind": "json",     "field": "version" },
    { "path": "tools/mcp-kit/package-lock.json",       "kind": "npm-lock" },
    { "path": "tools/spt-mcp/package.json",            "kind": "json",     "field": "version" },
    { "path": "tools/spt-mcp/package-lock.json",       "kind": "npm-lock" },
    { "path": "tools/mo2-mcp/package.json",            "kind": "json",     "field": "version" },
    { "path": "tools/mo2-mcp/package-lock.json",       "kind": "npm-lock" },
    { "path": "tools/tarkov-runtime-mcp/package.json", "kind": "json",     "field": "version" },
    { "path": "tools/tarkov-runtime-mcp/package-lock.json", "kind": "npm-lock" },
    { "path": "tools/mo2-mcp-sidecar/pyproject.toml",  "kind": "toml" },
    { "path": "tools/mo2-assets-engine/pyproject.toml", "kind": "toml" },
    { "path": "tools/mo2-assets-engine/src/mo2_assets_engine/__init__.py",
      "kind": "code", "pattern": "__version__\\s*=\\s*\"([^\"]+)\"" },
    { "path": "tools/tarkov-runtime-bridge/TarkovRuntimeBridge.csproj", "kind": "xml" },
    { "path": "tools/tarkov-runtime-bridge/src/Plugin.cs",
      "kind": "code", "pattern": "PluginVersion\\s*=\\s*\"([^\"]+)\"" },
    { "path": "tools/tarkov-active-probe/TarkovActiveProbe.csproj", "kind": "xml" },
    { "path": "tools/spt-mcp/src/index.ts",
      "kind": "code", "pattern": "SERVER_VERSION\\s*=\\s*\"([^\"]+)\"" },
    { "path": "tools/mo2-mcp/src/index.ts",
      "kind": "code", "pattern": "SERVER_VERSION\\s*=\\s*\"([^\"]+)\"" },
    { "path": "tools/tarkov-runtime-mcp/src/index.ts",
      "kind": "code", "pattern": "SERVER_VERSION\\s*=\\s*\"([^\"]+)\"" }
  ],
  "audit": { "exclude": [ "…保留现有清单…" ] }
}
```

（lockfile 实际存在性以仓库为准；若某 lockfile 不存在则不加该条目并报告。）

### 3) 根 `package.json` 元数据

```jsonc
"repository": "https://github.com/Samuel-Windersvista/SamMeow-modding-superpowers",
"homepage":   "https://github.com/Samuel-Windersvista/SamMeow-modding-superpowers",
"bugs":       "https://github.com/Samuel-Windersvista/SamMeow-modding-superpowers/issues"
```

其余字段不动；`version` 仍为 0.2.0（单一源）。

### 4) bootstrap 第 11 项（新）`tests/bootstrap/verify-version-identity.ps1`

- 第 1 部分：跑 `node scripts/version/sync-version.mjs`（check 模式）→ exit 0 即通过；drift 时打印其输出并 FAIL。
- 第 2 部分：身份禁入扫描（D3 表面 + 模式，大小写不敏感；`git grep -in -E` 实现；命中 → 逐条 `path:line` 打印 + FAIL）。
- 输出风格与既有 bootstrap 脚本一致（PASS/FAIL 行 + 汇总）；exit 0/1。
- 注册进 `tests/bootstrap/verify-all.ps1` 的 `$checks`（插入在 `verify-standard-compliance.ps1` 之后、`scripts/verify-doc-stats.ps1` 之前）。

### 5) 身份归一修复（车道 B）

- `scripts/start-mo2.ps1` :6/:32/:64、`scripts/install-mo2-control-plane.ps1` :3/:58 → 归一为 `spt-modding-superpowers` / 当前仓库路径。
- `tools/mo2-control-plane/live-bridge/mo2_assets_inspector/plugin.py` + `localization.py` → D6 改名（含 :1/:61 注释）。
- `tools/mo2-vfs-launcher/lib/xedit-client.launch.ps1:20` → `setting-up-spt-modding-environment`（指针类顺手归一）。
- 完成后 `git grep -in -E "bgs-modding-superpowers|awesome-bgs-mod-master|BGS_MODDING_SUPERPOWERS" -- scripts/ tools/mo2-control-plane/ package.json .opencode/` 必须为空。

### 6) 文档规范化（车道 B，D5 口径）

- 现役/现状类（归一）：`.opencode/INSTALL.md` :77/:86（`<owner>` → `Samuel-Windersvista`）；`RELEASE-NOTES.md:3`（标题 `v0.2.0`）；`docs/全局路线与进度报告.md` :13/:52/:105（现态陈述 → 现态；历史进度行保留）；`docs/internal/superpowers/**` 旧路径 → 当前仓库路径（`E:\云文件\GitHub\SamMeow-modding-superpowers`）。
- 分类处理（grep 全仓后逐条）：`docs/internal/specs/session-wiring-contract.md`、`docs/wayfinder/**`、`docs/dev-log.md`、`docs/README.md`、`skills/**`、`knowledge/spt-kb/**`、`README.md`——指针 → 归一；事件/署名 → 保留。
- 事件记录保留示例：`RELEASE-NOTES.md:23`（"移除物化的 plugins/bgs-modding-superpowers 树"——那棵树当年确实叫此名）、dev-log 幽灵物化条目、`skills/using-spt-modding-superpowers/SKILL.md:155`（Forked from 署名）。
- 交付报告必须包含：最终全仓 grep 结果逐条分类（已归一 / 保留+理由 / 超范围）。

### 7) 接线与文档

- `CONTRIBUTING.md` 版本节：改为单一源说明 + 新命令（`node scripts/version/sync-version.mjs [--write]`）；删除 jq 说明；删除对 `bump-version.sh` 的引用。
- 删除 `scripts/bump-version.sh`（全仓 grep 引用清零）。
- `RELEASE-NOTES.md` 只改标题行；不为 C5 追加内容（发版时处理）。

## 已声明行为 delta

1. 插件组件版本 0.1.0（含 `-dev0`）→ 0.2.0（约 17 个目标；工具/源码/清单/lockfile）。
2. `bump-version.sh` 删除（被 `scripts/version/sync-version.mjs` 替代）；jq 依赖消失。
3. bootstrap 10 → 11 项（新增 verify-version-identity）。
4. MO2 资源审计器显示名/类名变更（MO2 侧旧名配置条目将孤儿化，无害）。
5. 文档层：陈旧指针归一 + 标题 `v0.2.0`；事件记录与署名保留（逐条报告）。
6. LICENSE 追加 fork 版权行（原行保留）。
7. 缺目标 fail-loud：目标缺失 / pattern 未命中 → 响亮失败（exit 2）；旧 `bump-version.sh` 对缺失目标为 warn+skip（修复轮 F3 补记）。

## 车道任务

### 车道 A（fixer）：版本工具链 + 元数据 + bootstrap #11 + CONTRIBUTING
1. 实现 `scripts/version/sync-version.mjs`（check / `--write` / `--set`）。
2. 扩展 `.version-bump.json`（schema 见上；lockfile 存在性核实）。
3. `node scripts/version/sync-version.mjs --write` 一次性传播 → 全部目标 = 0.2.0。
4. 根 `package.json` 元数据（D4）。
5. `tests/bootstrap/verify-version-identity.ps1` + `verify-all.ps1` 接线（第 11 项）。
6. `CONTRIBUTING.md` 版本节改写；删除 `scripts/bump-version.sh`（引用清零）。
7. 验证矩阵 A 组全跑；报告（含"第 2 部分在车道 B 落地前可能红"的说明）。

### 车道 B（fixer）：身份归一 + LICENSE + 文档规范化
1. scripts 命中修复（5 处）+ xedit 注释指针归一。
2. MO2 inspector 改名（D6）。
3. LICENSE ×2 追加 fork 行（D7）。
4. 文档规范化（D5 口径，含全仓 grep 分类报告）。
5. `RELEASE-NOTES.md` 标题 + `INSTALL.md` owner 修正。
6. 验证矩阵 B 组全跑；报告（逐条分类清单）。

### 车道 C（orchestrator）：终验 + review + 记录
- 全量验证矩阵 → oracle 双轴 code-review（Standards/Spec）→ 修复轮 → dev-log + spec CLOSED。

## 写作用域（并行互斥）

- **车道 A**：`.version-bump.json`、`package.json`、`scripts/bump-version.sh`（删）、`scripts/version/**`（新）、`CONTRIBUTING.md`、`tests/bootstrap/verify-version-identity.ps1`（新）、`tests/bootstrap/verify-all.ps1`、`tools/{mcp-kit,spt-mcp,mo2-mcp,tarkov-runtime-mcp}/package.json`+`package-lock.json`、`tools/mo2-mcp-sidecar/pyproject.toml`、`tools/mo2-assets-engine/pyproject.toml`+`src/mo2_assets_engine/__init__.py`、`tools/tarkov-runtime-bridge/TarkovRuntimeBridge.csproj`+`src/Plugin.cs`、`tools/tarkov-active-probe/TarkovActiveProbe.csproj`、`tools/{spt-mcp,mo2-mcp,tarkov-runtime-mcp}/src/index.ts`。
- **车道 B**：`scripts/start-mo2.ps1`、`scripts/install-mo2-control-plane.ps1`、`tools/mo2-vfs-launcher/lib/xedit-client.launch.ps1`、`tools/mo2-control-plane/**`、`LICENSE`、`tools/tarkov-runtime-bridge/LICENSE`、`.opencode/INSTALL.md`、`RELEASE-NOTES.md`、`docs/**`、`README.md`（仅当模式命中）、`skills/**`（仅当模式命中且属指针类）、`knowledge/spt-kb/**`（仅当模式命中）。
- 交集为空；共享文件（如 `tests/bootstrap/verify-all.ps1`）仅车道 A 写。

## 验证矩阵

| # | 命令/动作 | 通过标准 | 车道 |
|---|-----------|----------|------|
| A1 | `node scripts/version/sync-version.mjs` | exit 0（无 drift） | A |
| A2 | `node scripts/version/sync-version.mjs --write` 二次运行 | 幂等（git diff 不变） | A |
| A3 | `powershell -NoProfile -ExecutionPolicy Bypass -File tests/bootstrap/verify-version-identity.ps1` | 第 1 部分 PASS；第 2 部分（依赖车道 B）最终 PASS | A→C |
| A4 | 四个工具套件：`npm --prefix tools/{mcp-kit,spt-mcp,mo2-mcp,tarkov-runtime-mcp} test`（先 build 如套件需要） | 全绿（基线：kit 25 / spt 98 / tarkov 395 / mo2 499） | A |
| A5 | `python -m py_compile tools/mo2-assets-engine/src/mo2_assets_engine/__init__.py` | exit 0 | A |
| B1 | `git grep -in -E "bgs-modding-superpowers\|awesome-bgs-mod-master\|BGS_MODDING_SUPERPOWERS"` | 剩余命中全部属保留类（事件/署名），逐条分类报告 | B |
| B2 | `git grep -in "BgsAssetsInspector"` | 无命中 | B |
| B3 | `python -m py_compile` 改动 py 文件；pytest inspector 测试（若可用） | exit 0 | B |
| C1 | `powershell -NoProfile -ExecutionPolicy Bypass -File tests/bootstrap/verify-all.ps1` | **11/11** | C |
| C2 | `node scripts/spt-kb/validate-index.mjs` + `powershell -File scripts/validate-mod-standard.ps1` | 双双 exit 0（回归） | C |
| C3 | `powershell -File scripts/build-portable-plugin.ps1 -Force` + 便携树抽查工具版本 | 便携树内工具版本 = 0.2.0；validate 仍绿 | C |

## 风险与逃生方案

- R1 lockfile 手工同步与 npm 行为差异 → A2 幂等 + `git diff` 审查；若 npm 后续重写，值一致即无 delta。
- R2 身份扫描假阳性（表面内合法历史提及）→ 表面已限操作性文件；若命中合法内容，精化扫描规则而非删历史。
- R3 文档规范化误伤历史叙述 → D5 分类 + 逐条报告 + 双轴 review 复核。
- R4 工具套件断言旧版本串 → 属版本归一的一部分，同步更新断言；涉及外部依赖版本则不动（报告）。
- R5 双车道并行 → 写作用域互斥表已声明；终验由车道 C 统一执行。

## 红线

- 不 commit；不 push。
- 不动：`mods/**`、`templates/**`、`knowledge/spt-kb/archive/**`、`external/**`、`tests/mod-standard/fixtures/**`（夹具冻结）、`tools/dbdump-*/**`、`tools/locale-test-mod/**`（独立版本边界）。
- 事件记录不伪造：旧名作为事件主体的叙述保留原文。
- 中文注释与回复；无 emoji。

## 修复轮补记（2026-09-17，oracle 双轴评审后；全部落地，终验通过）

| # | 项 | 处置 |
|---|----|------|
| S1a | 05-31 历史计划命令示例指针（:249/:250/:290/:321） | 归一为当前仓库路径；主体类 4 行（:260/:264/:268/:271）保留 |
| S1b | 身份命中逐条分类表（规格 §6 要求） | 落盘 `.scratch/c5-release-identity/identity-classification.md` |
| S2 | MO2 控制面版本常量（`mo2_agent_control.py` / `mo2_assets_inspector/plugin.py` 的 `VersionInfo(0,1,0,…)`） | **豁免**：MO2 插件版本体系独立，冻结 0.1.0；纳入机制（version-tuple kind）留作后续观察项 |
| S3 | `verify-version-identity.ps1` EAP=Stop 下 node stderr 中止 | 局部 `$ErrorActionPreference = "Continue"` 守卫；`verify-kb-index.ps1:29` 同款一并修。**根因补充**：该文件原为 UTF-8 无 BOM，PS 5.1 按 ANSI(936) 解码吞掉 9 个换行致守卫失效——已加 UTF-8 BOM（与既有惯例一致）；`verify-kb-index.ps1` 保持纯 ASCII |
| S4 | `CONTRIBUTING.md` `<owner>` 占位 | 归一为 `Samuel-Windersvista` |
| S5 | 便携树缺 `.version-bump.json`（sync 工具在树内不可运行） | 构建时**按树内实际存在的目标裁剪**后写入树根（18→10；树不含 lockfile 与两个 .NET 组件源码，原样复制会因 fail-loud 恒 exit 2）；`$requiredPortablePaths` 加入注册表与 sync 脚本；树内 sync check exit 0 |
| N4p | `docs/全局路线与进度报告.md:52` 日期口径 | "2026-09-14 首清 / 2026-09-16 复清" |
| 编码卫生 | 全仓"中文无 BOM" PS1 普查（5 处） | C5 触及的 3 处对齐 BOM：`build-portable-plugin.ps1` / `start-mo2.ps1` / `xedit-client.launch.ps1`（字节级前置，解析零错误，便携重建复验）；未触及的 `templates/paired-mod/scripts/pack.ps1`（红线禁区）与 `verify-mcp-entrypoints.ps1` 留观察 |
| 记录项（不修） | N1 死 catch / N2 `audit.exclude` 消费者 / N3 semver 前缀匹配 / N5 扫描盲区（设计内）/ N6 `mo2-install.ts` 示例串 / F4 计数硬化 / F5 MO2 版本口径 | 记入 dev-log |
