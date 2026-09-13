# SPT Mod 编写规范（Modding Standard）— Spec

Status: ready-for-agent

## Problem Statement

SPT mod 生态缺乏统一编写规范。297 个近期 4.1.5 mod 的语料调查（`.scratch/modding-standard/corpus-survey.md`）显示：约 7% 提交构建产物（`bin/`/`obj/`）、27% 缺 README、24% 缺 LICENSE、`ModMetadata` 文件名与位置混乱（根目录/`src/`/`Server/`/`csharp/*/Mod/` 均有）、GUID 命名不统一、源码平铺根目录；配置文件散落 5 种以上位置。后果：代码质量参差、整合包维护与自动化成本高、每次写新 mod 都要重新踩同样的坑；且"社区惯例"与"官方机制"的系统性冲突无人记录（典型：`ModDependencies` 是硬依赖语义，但 392 个语料目录中零个真实使用）。

## Solution

从语料统计 + 官方机制 + 移植经验归纳一套**分级 mod 编写规范**（Modding Standard），以"工程化默认值"形态落地：

- 13 维度规则集（`knowledge/spt-kb/curated/modding-standard/`），规则带稳定 ID（`STD-<DOMAIN>-<nnn>`）、MUST/SHOULD/MAY 分级、逐条证据与版本标签（4.1.5 / 5.0 / both）。
- 模板升级：`templates/server-mod`、`templates/client-mod` 按规范修订；新增 `templates/paired-mod`。
- `writing-spt-mod` 技能引用规则 ID，写 mod 流程默认走规范。
- 试点验证：3 个自研 mod 的逐规则合规报告（同时反向校准规则）。
- 二期：检查器脚本（本 spec 仅留接口约定，不含实现）。

## User Stories

1. As a mod 编写 agent, I want 每条规则有稳定 ID, so that 我能精确引用（"按 STD-STRUCT-001"）而不是模糊要求"写得规范点"。
2. As a mod 编写 agent, I want MUST/SHOULD/MAY 分级, so that 我知道哪些不可妥协、哪些是偏好。
3. As a mod 编写 agent, I want 每条规则附证据（机制/语料/经验及计数）, so that 我能信任规则并在冲突时看到判断依据。
4. As a mod 编写 agent, I want 关键代码样例内联（csproj 片段、ModMetadata 骨架、config 注册、Harmony 样板）, so that 规则可直接执行而不必翻文档。
5. As a mod 编写 agent, I want 深入解释链接到既有 KB 文档, so that 我能按需深挖且不产生重复维护。
6. As a mod 编写 agent, I want 每条规则带版本标签（4.1.5/5.0/both）, so that 我知道它在目标版本是否适用。
7. As a 维护者, I want 一份 4.1.5↔5.0 版本差异对照表, so that 跨版本变化集中可见。
8. As a mod 编写 agent, I want server 模板按规范生成项目, so that 脚手架产物天然合规。
9. As a mod 编写 agent, I want client 模板同上, so that BepInEx 侧同样有默认值。
10. As a mod 编写 agent, I want 新增 paired 模板, so that hybrid mod（58 个语料实例的现实需求）从第一天就是对的布局。
11. As a mod 编写 agent, I want writing-spt-mod 技能引用规则 ID 与模板流程, so that 规范在执行路径上被强制而非可选。
12. As a mod 编写 agent, I want 服务端配置的标准路径与加载样例（`config/config.jsonc` + `defaultConfig.jsonc` + `IOnDIConstruct`）, so that 配置行为可预测、可被工具处理。
13. As a mod 编写 agent, I want 依赖声明规则（`ModDependencies` 硬语义、服务端无软依赖、`[BepInDependency]` soft/hard 策略）, so that 依赖失败不会拖垮整批加载。
14. As a 整合包维护者, I want 打包与发布规则（MO2 overlay 布局、paired 单 zip 双端、服务端目录唯一 `IModMetadata`、README/LICENSE 必备）, so that 安装与自动化处理可预测。
15. As a mod 编写 agent, I want 豁免政策（MUST 可豁免但留痕）, so that 合理例外不被教条扼杀且有记录。
16. As a 维护者, I want 3 个自研 mod 的试点合规报告, so that 规范有第一批"应用样例"，且不合理的规则被反向修正。
17. As a 维护者, I want 证据索引（语料统计与引用汇总）, so that 规则可审计、语料更新后知道哪些规则需要复核。
18. As a 维护者, I want 规则集机械自检（ID 唯一、MUST 双源、版本标签齐全）, so that 规范自身的质量可自动核验。
19. As a 检索 agent, I want 规范条目进入 index.json, so that KB 检索路径能找到它。
20. As a 维护者, I want 规范可扩展（新增维度/规则有固定流程）, so that 未来的新需求有位置安放。

## Implementation Decisions

- **13 维度与 domain slug**：①仓库与目录结构 `STRUCT` ②元数据与版本声明 `META` ③构建与目标框架 `BUILD` ④服务端机制 `SRV`（DI/生命周期/路由/回调）⑤客户端机制 `CLI`（BepInEx/Harmony/入口）⑥配置系统 `CFG` ⑦日志与错误处理 `LOG` ⑧依赖管理 `DEP` ⑨打包与发布 `PKG` ⑩验证流程 `VERIFY` ⑪版本差异 `VER` ⑫bundle/资产 `BND` ⑬性能与安全 `PERF`。
- **ID 方案**：`STD-<DOMAIN>-<nnn>`，三位序号，不复用已删除编号。
- **文件结构**：`curated/modding-standard/README.md`（使用说明 + 索引 + 豁免流程）+ `01-structure.md` … `13-perf-security.md` + `version-matrix.md` + `evidence-index.md`（固化 corpus-survey / gap-investigation 数据）。
- **分级与证据标准**：MUST = 机制 + 语料双源（样本 ≥10 或全量检查）；SHOULD = 单源；MAY = 经验即可。纯机制推断（无语料先例，如 `ModDependencies` 声明写法）须显式标注"机制推断，无语料先例"。
- **版本标签**：每条规则标注 `Applies: 4.1.5 / 5.0 / both`；证据形态不对称记录在案（4.1.5 = 语料 + 文档；5.0 = 源码 + 笔记，暂无 mod 语料）。
- **内容形态**：关键代码样例内联；原理与深入内容链接既有文档（modding-guide / api-notes-* / wiki-tushonka）。
- **豁免**：MUST 可豁免，须在 mod README 或项目 dev-log 记录理由与替代方案；豁免标记格式 `Waiver: STD-XXX-nnn`（供二期检查器识别）。
- **模板**：升级 `templates/server-mod`（补 config 加载示例、README/LICENSE 模板）+ `templates/client-mod`；新增 `templates/paired-mod`（`Client/`+`Server/`+`Shared/` 布局、单 sln、版本联动、打包脚本约定）。bundle 模板暂缓。
- **技能**：`writing-spt-mod` 改造为引用规则 ID 与三模板流程。
- **KB 集成**：规范文件登记进 index.json；版本标签沿用 KB 惯例（`[4.1]`/`[5.0]`/`[通用]`）。
- **试点**：WarsawTrader / tarkov-active-probe / NoStaminaDrain 各一份合规报告（PASS/FAIL/N-A + 修复建议）；报告可触发规则修订（校准闭环）。
- **二期接口预留**：检查器对规则 ID 与豁免标记的读取约定——规则 ID 以固定格式出现在规则标题中；豁免以 `Waiver: STD-XXX-nnn` 记录。

## Testing Decisions

（接缝已与 Overseer 确认）

- **S1（复用既有接缝）**：三个模板项目 `dotnet build` 通过——沿用 `writing-spt-mod` 既有模板验证方式，验证"规范在模板里可执行"。
- **S2（新，最高层）**：3 个自研 mod 的逐规则合规报告——规则集的端到端测试；同时反向校准（不合理的规则要改）。
- **S3（新，轻量机械检查）**：规则集自检——ID 唯一性、MUST 双源证据完整性（对照 `evidence-index.md`）、版本标签齐全（4.1.5/5.0/both）。
- 刻意不新开接缝：规则内容正确性靠 S2 校准；技能改造效果靠走一遍 S2 流程；文件结构靠 S3 检查。

## Out of Scope

- bundle 从零制作教程（只写约定 + 兼容陷阱，引用既有 bundle 报告）
- bundle 模板；二期检查器脚本实现
- SPT 3.11 / 4.0 的规则（3.11 材料仅作参考）
- 面向人类公开发布（agent-first；未来可选）
- 迁移指南重复（链接 `curated/migration/` 既有文档）

## Further Notes

- 证据底座：`.scratch/modding-standard/corpus-survey.md`（315 行语料调查）、`gap-investigation.md`（239 行缺口补查）。
- 决策记录：本 spec 综合 16 项 grilling 决策；标准形态见 `docs/adr/0005-modding-standard-shape.md`。
- 语料坐标：`archive/forge/mods/` 297 个近期仓库（`MANIFEST-sp-mod-2026-09.md`）。
