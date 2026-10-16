# 16: 版本、文档与发行（0.2.0）

**What to build:** 将产品版本 0.1.0 → **0.2.0** 并完成配套文档与发行产物：README 配置章节重写（外置文件集合 + 加载语义 + 与 `config.json` 的关系）；移植记录（port-notes）增补 v0.2.0 节（对齐结果 / 阶段实测 / 待验收项）；规格快照镜像至项目 `docs/specs/spec-v0.2.0.md`；`release/` 新版 zip（含 `config/` 子树）。

**Blocked by:** 08–15（全部数据与代码变更完成后才有可发行状态）

**Status:** ready-for-agent

- [x] ModMetadata 版本号 0.2.0（GUID 不变）
- [x] README / port-notes / spec 镜像更新
- [x] `build.ps1` 产物核验：DLL + `config.json` + `config/` 全树（12 文件）；`release/[5]软核大修-InescapableTarkovsSoftcore-0.2.0.zip` 归档
- [x] 变更清单随 Comments 登记（供 17 与用户核对）

## Comments

### 实施登记（2026-10-17，v0.2.0 收口）

**版本**
- `src/InescapableTarkovsSoftcore/ModIdentity.cs`：`Version` `0.1.0` → **`0.2.0`**（GUID / Name 不变）。
- 版本断言同步：`ModLoadHookTests.cs`（LoadLogLine 期望 `v0.2.0`）、`ModMetadataTests.cs`（`Version == "0.2.0"`）。
- grep `0.1.0` 全仓复核：剩余命中全部为历史快照 / 历史记录（`docs/specs/spec-v0.1.0.md`、port-notes §1–6、release 0.1.0 旧 zip、`customBackground` 移除项注释、README「版本自 0.1.0 起步」），无漏改现版本引用。

**文档**
- `README.md`：配置章新增「外置数据表（`config/` 子树）」节（12 表清单 + 目录树 + 加载语义 + 与 `config.json` 关系）；修正旧「内嵌、不投影 overlay」表述（原 65-70 / 100-112 / 346 行，全文再核）；产物布局树加入 `config/` 子树；数据表计数同步（barter 219 / clothing 33 / keycards ParentList 1 / medicals 21 / partsnmods 104+10 / provisions 23 / armbands 37 / backpacks 56）；文档索引加入 `docs/specs/spec-v0.2.0.md`；测试计数 215 → 235；状态节按 v0.2.0 更新。grep `不投影`：README 零残留。
- `docs/port-notes.md`：末尾增补 **§7 v0.2.0**（外置化 / A/B/C 对齐结果 / softcore 31 条保留结论 / barter·clothing 55 采纳 + 86 暂缓 / 阶段后移 / 父类修复 / 测试构建 / §7.6 待 17 占位表）；§1–6 历史只读未动。登记规格笔误：spec Testing Decisions「barter 175 不变」与 D3-C 冲突，实况按 D3-C（219）。
- `src/InescapableTarkovsSoftcore/InescapableTarkovsSoftcore.csproj`（第 22 行 S1 失步点注释）：更新为 v0.2.0 语义（内嵌 + build.ps1 分发 `config/` 外置模板；外部优先、内嵌兜底）。
- `docs/specs/spec-v0.2.0.md`：`.scratch/inescapable-softcore-416/spec-v0.2.0.md` 逐字节镜像（SHA256 `691B1606…`，15149 bytes，仅复制无改注）。

**GLOSSARY / ADR（按需）**：**不新增**。理由：v0.2.0 的两项决策（外置文件单一共享缝、处理阶段后移）已完整记录于 `spec-v0.2.0.md` D2/D4/D8 与本 port-notes §7，且均为既有架构的局部演进（未改变模块边界 / 新增系统级接缝）；无尚未记录的长期决策，按「按需」原则从简。

**构建产物**
- `scripts/build.ps1`（Release）跑通：0 警告 0 错误；overlay 产物 **14 项** = DLL + `config.json` + `config/` 12 外置表（`config/trueitems/*` ×6、`config/armbands.json`、`config/backpacks.json`、`config/softcore/*` ×4），内置存在性断言通过。
- 发行归档 `release/[5]软核大修-InescapableTarkovsSoftcore-0.2.0.zip`（`Compress-Archive -Path SPT_Runtime`，结构与 0.1.0 一致：2 目录项 + 14 文件项，共 16 条目）。
  - zip SHA256：`B724F8FA416A49AF02A82510A420BBBF49AFD0999D3EEADD0B0857197CE13C74`（137868 bytes）
  - DLL SHA256：`C2E0DE2927F714F5449623EE4051BF7A4930841A016B3FC0900463D095846255`（334336 bytes）
- `release/README.md`：结构图补 `config/` 子树；归档清单新增 0.2.0 行。

**测试证据**
- `dotnet test "…\InescapableTarkovsSoftcore.sln" --artifacts-path .artifacts/its-16 -c Debug --nologo -v q` → **通过 235 / 失败 0 / 跳过 0**（基线 235；版本断言同步后数字不变）。

**登记偏差**
- 本工单正文「`config/` 全树（11 文件）」为计数笔误；实际外置文件 **12 个**（工单 08 已登记同位笔误，规格 D1 清单亦为 12）。实况按 12 执行。

**遗留（工单 17）**
- 重部署 MO2 overlay + profile 备份 + 服务器日志零告警核验 + 用户进游戏抽查（见 port-notes §7.6 占位表）。

