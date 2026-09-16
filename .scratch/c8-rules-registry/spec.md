# C8 · Modding Standard 规则机读化 — 实施规格

> 来源：架构审查候选 C8 + grilling 确认（2026-09-16）。报告：`D:\Temp\architecture-review-20260916-1337.html` §2 C8 卡。
> **进展（Work Status）**: CLOSED — 2026-09-16（全流程完成：车道 A 收尾 + 终验 + oracle 双轴 review + 修复轮 10 项；未提交）

## 决策记录（grilling 确认）

| # | 决策 | 结论 | 依据 |
|---|------|------|------|
| D1 | 注册表范围 | **全量 84 条元数据 + 可检子集（~30 条）检查规格** | S3 自检常驻；prose↔registry 双向校验；单源化可检子集 |
| D2 | 形态与位置 | **JSON** @ `knowledge/spt-kb/curated/modding-standard/rules.json` | PS 5.1 `ConvertFrom-Json` 与 Node 双端原生、零新依赖；随 curated 进便携树 |
| D3 | 检查器架构 | **PS 保留 + 读 rules.json**（声明式规格 + 命名 handler） | 零迁移风险；CLI 参数/输出格式/退出码/waiver 语义不变 |
| D4 | 一致性方向 | **双向校验**（registry ↔ prose 全量一致）+ 检查器读 registry | S3 不变量全部机检；README 措辞改指单一源 |
| D5 | META-005 修复 | **检查器放宽 + prose 微调**：三段核心 + 可选 prerelease/build 后缀 | radar 活体 FAIL 清零；与散文「不得非 semver」语义对齐 |

## 事实基线（侦察实测）

- 规则集：84 条 / 13 章（`knowledge/spt-kb/curated/modding-standard/01-13*.md`），条目格式 `### STD-<DOMAIN>-<nnn> — <标题>` + `- **Level:**` + `- **Applies:**` + `- **Evidence:**` + `- **Rule:**`；README 含「二期检查器接口约定」「机械自检（S3）」章节；README 索引 16 文件。
- 检查器：`scripts/check-mod-standard.ps1`（300 行，~30 条硬编码检查；ID 锚点；占位符 `{{...}}` → SKIP；waiver 文件 `Waiver: STD-XXX-NNN: reason`；exit 1 若有未豁免 FAIL）。
- S3 机械自检：曾以**一次性脚本**完成（2026-09-14：84/0 重复、28 MUST 全双源、22 条 NOCORPUS 全登记、README/index 一致）——脚本未入库、不常驻。
- 维护税先例：CLI-007 校准 = 散文（05-client.md）+ 检查器 + version-matrix + 豁免四处同改（wave2 ticket 04 实证）。
- **META-005 假阳性（活体机检实锤）**：`mods/SPT5-AccurateCircularRadar` 的 `<Version>1.3.4-spt5.1</Version>` → `[FAIL] STD-META-005`（exit 1）；散文 02-metadata.md:104 说「不得使用四段式或非 semver 字符串」，而 `1.3.4-spt5.1` 是合法 semver（prerelease）——检查器正则 `^\d+\.\d+\.\d+$` 比散文严格。
- 消费者：`tests/bootstrap/verify-standard-compliance.ps1`（4 模板目标）、`skills/porting-spt-mod-to-spt5/SKILL.md:70`、`tools/tarkov-runtime-bridge/MODDING-STD-WAIVER.md`（WAIVED 3）、radar waiver（CLI-003/006）、根 README:80 与 RELEASE-NOTES。

## 目标设计

### rules.json（注册表）

```jsonc
{
  "schema_version": 1,
  "generated": "2026-09-16",
  "source": "knowledge/spt-kb/curated/modding-standard/*.md",
  "domains": { "STRUCT": "01-structure.md", "META": "02-metadata.md", "...": "…（13 项）" },
  "rules": [
    {
      "id": "STD-STRUCT-002",
      "domain": "STRUCT",
      "level": "MUST",              // MUST | SHOULD | MAY（与 prose 一致）
      "applies": "both",            // "4.1.5" | "5.0" | "both"（与 prose 一致）
      "title": "不提交 TypeScript/JavaScript 源码",   // 与 prose 标题逐字一致
      "checkable": true,
      "check": {                    // checkable=false 时为 null
        "kind": "both",             // both | server | client（检查适用面，与 Applies 不同轴）
        "handler": "no-files-by-extension",
        "params": { "extensions": [".ts", ".js", ".tsx", ".jsx"] }
      }
    }
    // … 84 条；**顺序 = 现有检查器输出顺序**（保持 golden 除 META-005 外零 diff）
  ]
}
```

- 可检子集 = 现检查器全部 ~30 条检查，1:1 平移（语义不变；唯一例外 META-005，见下）。
- handler 原则：简单检查用数据化 handler（`no-files-by-extension` / `repo-file-exists` / `proj-value-regex` / `proj-value-equals` / `proj-regex-present` / `src-regex-present` / `src-regex-absent` / `src-regex-count` / `src-regex-any-of` / `mod-file-exists` …）；复杂逻辑保留为**命名 handler**（VER-001 范围覆盖、VER-002 版本等值、CFG-004 Injectable-config 扫描、CLI-007 条件生命周期、META-005 等）。具体 handler 划分由实现定，但每条规则的**参数（正则/取值/消息）必须落在 rules.json**，handler 内不得再硬编码规则常量。
- 占位符/waiver/monorepo/kind 检测逻辑保持共享（既有 helper 不变）。

### 检查器改造（scripts/check-mod-standard.ps1）

- 读 rules.json（脚本相对路径 → 仓库根 `knowledge/spt-kb/curated/modding-standard/rules.json`；便携树同布局）。
- 按 registry 顺序执行检查；输出格式逐字保持：`[PASS] STD-XXX-NNN  --  <detail>`、汇总行、`FAILED rules:`、exit 码。
- CLI 参数不变：`-ModPath -TargetSptVersion -Kind -WaiverFile`。
- META-005：pattern 更新为 `^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?(\+[0-9A-Za-z.-]+)?$`（三段核心 + 可选 semver 后缀；四段式仍 FAIL）。
- 头注释更新（registry 驱动说明）。

### validator（scripts/validate-mod-standard.ps1，新）

- 读 rules.json + 13 章 prose，双向校验：
  - ID 集合一致（prose `### STD-...` ↔ registry；无孤儿、无缺失）；
  - 每条的 title / Level / Applies 与 prose 逐字一致；
  - registry 内部：ID 格式与唯一性、domain slug 合法、checkable 规则必有 check 规格、非 checkable 的 check 为 null；
  - 输出统计（84 条 / 28 MUST / 可检 N 条）+ 错误清单；exit 0/1。
- S3 不变量（README「机械自检」节）改由本脚本常驻执行。

### 夹具回归（tests/mod-standard/，新）

- 合成 mod 夹具（最小树，覆盖各态）：`pass-server`、`fail-server`（多个 FAIL）、`pass-client`、`placeholder-server`（SKIP 降级）、`waiver-mod`（WAIVED）、`meta005-prerelease-client`（锁定 D5 修复：`1.3.4-spt5.1` PASS）。
- `tests/mod-standard/run-fixtures.ps1`：逐夹具跑检查器，断言「预期 exit 码 + 预期 FAIL ID 集合 + 关键规则状态」；输出 PASS/FAIL 汇总；exit 0/1。
- 目的：检查器行为可回归（「可单测」的 PS 形态）。

### 接线与文档

- `tests/bootstrap/verify-standard-compliance.ps1` 扩展为三段：① `validate-mod-standard.ps1`；② 4 模板机检（既有）；③ `run-fixtures.ps1`。失败文案具体化。（bootstrap 保持 10 项。）
- `knowledge/spt-kb/curated/modding-standard/README.md`：「二期检查器接口约定」改为 registry 单一源说明；「机械自检（S3）」改为常驻脚本；目录树补 `rules.json`。
- `knowledge/spt-kb/curated/modding-standard/02-metadata.md`：META-005 措辞微调（允许 semver 预发布/构建后缀；禁四段式）。
- 根 `README.md:80`：检查器描述更新（registry 驱动；条数口径）。
- `scripts/check-mod-standard.ps1` 头注释同步。

## 已声明行为 delta（全部需 golden/审查确认）

1. **META-005 放宽**：`1.3.4-spt5.1` 类 semver 后缀从 FAIL → PASS（radar 活体 FAIL 清零）；四段式仍 FAIL。
2. 检查器内部重构：**除 META-005 外所有目标输出逐字不变**（golden 对比：4 模板 + bridge + radar；预期唯一 delta = radar META-005 行 + 汇总数字）。
3. bootstrap 组成变化：verify-standard-compliance 内含 validator + 夹具（总数仍 10 项；对模板的机检结果不变）。
4. 新增文件：rules.json / validator / 夹具套件（纯增量）。

## 车道任务

### 车道 A（fixer）：golden 捕获 + 注册表 + validator + 检查器改造 + 夹具 + 接线 + 文档
0. **改前 golden**：对 6 目标跑检查器（4 模板 + `tools/tarkov-runtime-bridge -TargetSptVersion 5.0.0` + `mods/SPT5-AccurateCircularRadar -Kind client`），输出存 `.scratch/c8-rules-registry/goldens/pre/<target>.txt`。
1. `rules.json`：从 13 章 prose 机械提取 84 条（ID/domain/level/applies/title）+ 可检子集检查规格（1:1 平移现检查器；顺序=现输出顺序）。
2. `scripts/validate-mod-standard.ps1`（双向校验 + 统计）。
3. `scripts/check-mod-standard.ps1` 改造（读 registry；输出/退出码/waiver 语义不变；META-005 pattern 更新）。
4. `tests/mod-standard/` 夹具 + `run-fixtures.ps1`。
5. META-005 措辞（02-metadata.md）+ README/根 README 文档更新 + verify-standard-compliance 接线。
6. 验证（下节矩阵）+ golden 对比 + 报告。

### 车道 B（orchestrator）：终验 + review + 记录
- 全量验证矩阵 → oracle 双轴 code-review → 修复轮 → dev-log + spec CLOSED。

## 验证矩阵

| # | 命令/动作 | 通过标准 |
|---|-----------|----------|
| 1 | `powershell -File scripts/validate-mod-standard.ps1` | exit 0；84 条 / 双向一致 |
| 2 | 检查器 ×4 模板（server/client/paired×2） | 与 pre golden 逐字一致（FAIL=0） |
| 3 | 检查器 ×bridge（5.0.0）与 ×radar（client） | bridge 与 pre 逐字一致；radar 仅 META-005 FAIL→PASS |
| 4 | `powershell -File tests/mod-standard/run-fixtures.ps1` | 全夹具断言通过 |
| 5 | `powershell -NoProfile -ExecutionPolicy Bypass -File tests/bootstrap/verify-all.ps1` | 10/10 |
| 6 | 便携重建 + 便携树跑 validate + 单夹具机检 | exit 0（registry 相对路径在便携树成立） |
| 7 | registry 覆盖断言：可检规则在对应 kind 目标输出中全部出现 | 无遗漏 |

## 风险与逃生方案

- R1 提取期发现 prose↔检查器其他不一致 → 逐项报告；默认 registry 镜像 prose，除 META-005 外不改语义（若发现新不一致，登记为待决而非静默对齐）。
- R2 检查器重构回归 → 6 目标 golden + 夹具套件兜底。
- R3 输出顺序/格式漂移 → registry 顺序=现输出顺序；格式逐字保持。
- R4 便携树 registry 路径断裂 → 矩阵第 6 项显式覆盖。

## 红线

- 不 commit；不改 4 模板与 radar/bridge 的源文件（radar 的 META-005 修复体现在**检查器侧**，radar 源不动）。
- 除 META-005 外零语义变化；输出格式逐字保持。
- prose 仅动 META-005 措辞 + README 接口/自检两节；其余 83 条正文不动。
- 中文注释；无 emoji。
