---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# SPT Mod 编写规范（Modding Standard）

> 定位：**工程化默认值**。模板按规范生成、技能引用规则 ID；本文档是载体而非本体。
> 形态决策：`docs/adr/0005-modding-standard-shape.md` · Spec：`.scratch/modding-standard/spec.md`
> 证据来源：[evidence-index.md](evidence-index.md)（全部 `EV-*` 锚点）· 版本对照：`version-matrix.md`（ticket 07 交付后可用）

## 这是什么

一套**分级**的 SPT mod 编写规则集，覆盖 13 个维度。每条规则具备五要素：

| 要素 | 说明 |
|------|------|
| Rule ID | `STD-<DOMAIN>-<nnn>`，稳定、可精确引用 |
| Level | `MUST` / `SHOULD` / `MAY` |
| Applies | `4.1.5` / `5.0` / `both` |
| Evidence | 机制引用 + 语料计数（指向 [evidence-index.md](evidence-index.md)） |
| 样例与链接 | 关键代码样例内联 + 深入文档链接 |

规则分层承载"正确性"与"惯例现实"：机制与语料双源证据支撑的进 MUST；单源支撑的进 SHOULD；偏好进 MAY。规范不做"唯一正确写法"的教条。

## 目录结构

```
modding-standard/
├── README.md                    # 本文件：使用说明 + 索引 + 豁免流程
├── 01-structure.md              # 维度 ① 仓库与目录结构（STRUCT）
├── 02-metadata.md               # 维度 ② 元数据与版本声明（META）
├── 03-build.md                  # 维度 ③ 构建与目标框架（BUILD）
├── 04-server.md                 # 维度 ④ 服务端机制（SRV）
├── 05-client.md                 # 维度 ⑤ 客户端机制（CLI）
├── 06-config.md                 # 维度 ⑥ 配置系统（CFG）
├── 07-logging.md                # 维度 ⑦ 日志与错误处理（LOG）
├── 08-dependencies.md           # 维度 ⑧ 依赖管理（DEP）
├── 09-packaging.md              # 维度 ⑨ 打包与发布（PKG）
├── 10-verification.md           # 维度 ⑩ 验证流程（VERIFY）
├── 11-version-differences.md    # 维度 ⑪ 版本差异（VER）
├── 12-bundle-assets.md          # 维度 ⑫ bundle/资产（BND）
├── 13-perf-security.md          # 维度 ⑬ 性能与安全（PERF）
├── evidence-index.md            # 统一证据索引（EV-* 锚点）
└── version-matrix.md            # 4.1.5 ↔ 5.0 对照表（待交付：ticket 07）
```

## Rule ID 规则

格式：`STD-<DOMAIN>-<nnn>`。

- `<DOMAIN>`：13 个 domain slug 之一（见[维度索引](#13-维度索引)）。
- `<nnn>`：三位十进制序号，**域内**从 `001` 起、按规则首次写入顺序分配。
- **编号不复用**：规则删除后保留空洞，新规则取下一个未用编号；禁止重编号。
- 规则标题必须以 ID 开头，格式 `### STD-<DOMAIN>-<nnn> — <标题>`（ID 与标题之间为 em dash `—`）。

## 分级与证据标准

| 级别 | 含义 | 最低证据要求 |
|------|------|--------------|
| **MUST** | 不可妥协；偏离必须走[豁免流程](#豁免流程waiver)并留痕 | **双源**：机制证据 + 语料证据（样本 ≥10 或全量检查） |
| **SHOULD** | 强烈建议；偏离应说明理由（无需豁免标记） | 单源：机制或语料其一 |
| **MAY** | 可选偏好 | 经验即可 |

证据分级（强 → 弱）：**机制证据**（源码/接口/官方文档，注明文件路径）> **语料证据**（`archive/forge/mods/` 统计，注明计数与锚点）> **经验证据**（实战经验，注明出处）。

**无语料先例的规则**：必须显式标注「机制推断，无语料先例」，并登记到 [evidence-index.md](evidence-index.md) 的 `EV-NOCORPUS`；级别按单源判 SHOULD（MUST 仍需机制 + 语料双源）。

## 规则条目格式

每条规则一个 `###` 小节，五要素齐全（ticket 02–06 按此格式填写各维度文件）：

```markdown
### STD-<DOMAIN>-<nnn> — <祈使句标题>

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：<文件路径或 KB 文档>；语料：<计数>（EV-CORPUS-XXX）
- **Rule:** <一句话规范陈述>

<关键代码/配置样例（涉及代码或配置的规则必附）>

> 深入：<KB 文档链接>
```

- `Applies` 取值：`4.1.5` / `5.0` / `both`。
- 证据栏必须引用 `evidence-index.md` 的 `EV-*` 锚点；无法引用时说明原因。
- 深入链接优先指向既有 KB 文档（modding-guide / api-notes-* / wiki-tushonka / operations / migration），不重复维护内容。

## 豁免流程（Waiver）

MUST 规则可豁免，但必须留痕：

1. **适用范围**：仅 MUST。SHOULD 偏离建议一句话说明理由（无需豁免标记）；MAY 不适用。
2. **记录位置**：mod 仓库 README 或项目 dev-log（整合包项目记 dev-log）。
3. **记录内容**：① 理由——为什么无法遵守；② 替代方案——如何达成同等效果或安全。
4. **标记格式**：`Waiver: STD-XXX-nnn`（每行一条；`XXX` 为 domain slug、`nnn` 为规则编号）。

mod README / dev-log 中的记录示例：

```markdown
## Standard Waivers
- Waiver: STD-XXX-nnn — 理由：<...>；替代方案：<...>
```

## 13 维度索引

| # | 文件 | Domain | 维度 | 范围概述 |
|---|------|--------|------|----------|
| 01 | [01-structure.md](01-structure.md) | `STRUCT` | 仓库与目录结构 | 单仓库分层、根目录仓库文件、构建产物禁止提交 |
| 02 | [02-metadata.md](02-metadata.md) | `META` | 元数据与版本声明 | `IModMetadata`、元数据文件位置、GUID、`SptVersion`、版本联动 |
| 03 | [03-build.md](03-build.md) | `BUILD` | 构建与目标框架 | `net10.0` / `netstandard2.1`、程序集引用、csproj 属性 |
| 04 | [04-server.md](04-server.md) | `SRV` | 服务端机制 | DI 注册、生命周期、路由、Callbacks、日志注入 |
| 05 | [05-client.md](05-client.md) | `CLI` | 客户端机制 | BepInEx 入口、Harmony patch、客户端依赖与日志 |
| 06 | [06-config.md](06-config.md) | `CFG` | 配置系统 | 服务端 config 标准路径与加载、客户端 `Config.Bind` |
| 07 | [07-logging.md](07-logging.md) | `LOG` | 日志与错误处理 | `ISptLogger`、错误处理与降级、不吞异常 |
| 08 | [08-dependencies.md](08-dependencies.md) | `DEP` | 依赖管理 | `ModDependencies` 硬语义、无软依赖、`[BepInDependency]` 策略 |
| 09 | [09-packaging.md](09-packaging.md) | `PKG` | 打包与发布 | MO2 overlay、meta.ini、paired 单 zip、唯一 `IModMetadata` |
| 10 | [10-verification.md](10-verification.md) | `VERIFY` | 验证流程 | build → server 冒烟 → 日志断言 |
| 11 | [11-version-differences.md](11-version-differences.md) | `VER` | 版本差异 | 4.1.5 ↔ 5.0 机制差异要点 |
| 12 | [12-bundle-assets.md](12-bundle-assets.md) | `BND` | bundle/资产 | 资源替换与数据库覆盖约定、升级兼容陷阱 |
| 13 | [13-perf-security.md](13-perf-security.md) | `PERF` | 性能与安全 | 已知热点模式、路径遍历与输入校验 |

配套文件：[evidence-index.md](evidence-index.md)（证据索引）；`version-matrix.md`（4.1.5 ↔ 5.0 对照表，待交付：ticket 07）。

## 版本标签与证据形态

- 每条规则标注 `Applies: 4.1.5 / 5.0 / both`；跨版本差异对照见 `version-matrix.md`。
- 证据形态不对称（记录在案）：**4.1.5** = mod 语料 + 文档；**5.0** = 源码 + 笔记（暂无 mod 语料）。
- 单规则集 + 规则级版本标签；不维护两份并行规范（见 ADR-0005）。

## 使用方式

- **写新 mod（agent）**：走 `writing-spt-mod` 技能 → 按模板脚手架 → 实现时对照本规范；引用规则时说"按 `STD-<DOMAIN>-<nnn>`"，不要模糊说"写得规范点"。
- **审 mod / 写合规报告**：逐规则给出 `PASS / FAIL / N-A`，FAIL 附修复建议；豁免项按 `Waiver:` 标记核验。
- **检索（agent）**：`knowledge/spt-kb/index.json` 中 `topic: "modding-standard"` 过滤本系列。

## 扩展流程

**新增规则**：① 选定维度（domain）→ ② 定级（MUST/SHOULD/MAY）→ ③ 补齐证据（对照 evidence-index；如引入新数据，先更新 evidence-index 再加规则）→ ④ 分配域内下一个未用编号 → ⑤ 写入对应维度文件。

**新增维度**：需要一次形态决策（更新或新增 ADR，见 `docs/adr/0005-modding-standard-shape.md`）→ 定义新 domain slug → 维度文件编号顺延（`14-...`）→ 更新本 README 索引与 `index.json`。

## 二期检查器接口约定

本规范以固定文本约定支持后续机械检查（本期不实现）：

- 规则 ID：以 `### STD-<DOMAIN>-<nnn> —` 形式出现在规则标题行首。
- 豁免标记：mod 侧以 `Waiver: STD-XXX-nnn` 记录（`XXX` 为 domain slug、`nnn` 为规则编号）。
- 证据锚点：[evidence-index.md](evidence-index.md) 中的 `EV-*` 标题锚点（`##` / `###` 级）供规则引用。

## 机械自检（S3）

规则集自身的不变量，供自检（ticket 12）与人工核对：

- Rule ID 全局唯一，且符合 `STD-<DOMAIN>-<nnn>`。
- 每条规则 Level / Applies 齐全；MUST 具备机制+语料双源；无语料先例的规则已标注「机制推断，无语料先例」并登记 `EV-NOCORPUS`。
- 本 README 索引与目录实际文件一致；`index.json` 全量登记。

---

> 状态：骨架（ticket 01 建立）。规则由 ticket 02–06 填充；`version-matrix.md` 由 ticket 07 交付。
