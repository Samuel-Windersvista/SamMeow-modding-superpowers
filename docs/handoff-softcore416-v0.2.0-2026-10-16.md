# Handoff: InescapableTarkovsSoftcore416 v0.2.0（配置外置化与数据全量对齐）

> 交接日期：2026-10-16 ｜ 交接方：上一会话（研究 / 设计 / 工单化） ｜ 接收方：新会话 agent
> 本文件按项目规则置于仓库 `docs/`（禁止临时目录）。**研究证据与本轮全部决策均已落档，新会话不需要重跑研究。**

## 一、任务一句话

对 `mods/InescapableTarkovsSoftcore416`（SPT 4.1.6 服务端 mod，部署于 MO2 实例 `Norvinsk_Forever`）实施 **v0.2.0：配置外置化 + 数据全量对齐（零缺失）+ 处理阶段修正 + 父类互换修复**。

## 二、当前状态：研究 → grill → spec → 工单 全部完成

- **规格书**（用户已确认，无异议）：`.scratch/inescapable-softcore-416/spec-v0.2.0.md`
  （含 D1–D8 实现决策、A/B/C 对齐清单、测试缝、验收标准）
- **全量对齐枚举**：`.scratch/inescapable-softcore-416/align-report-2026-10-16.txt`（633 行）+ 复跑脚本 `align_enum.py`
- **工单 08–17**（ready-for-agent）：`.scratch/inescapable-softcore-416/issues/`
- **v0.1.0 遗产（实现时必读）**：`mods/InescapableTarkovsSoftcore416/` 的 `README.md`、`docs/port-notes.md`、`docs/specs/spec-v0.1.0.md`、`scripts/build.ps1`、`config/default-config.json`；旧工单 `issues/01–07`

## 三、工单总览（依赖边）

| # | 工单 | Blocked by | 交付（一句话） |
|---|---|---|---|
| 08 | 外置文件加载与分发（基础设施） | — | 11 张数据表随包外置 + 统一加载语义（外部优先/缺失重建/损坏回落）+ build.ps1 |
| 09 | 臂章表对齐 | — | `armbands.json` 22→37（补 15 款，按既有档位） |
| 10 | 背包表对齐 | — | `backpacks.json` 43→56（删 1 / 补 14） |
| 11 | trueItems 删除无效 | — | `medicals.json` 删除 22 条 stim 占位（43→21） |
| 12 | trueItems 同类新增（补档） | 11 | 按品类纳入口径补整合包新增物品 |
| 13 | Softcore 四表引用清理 | — | 31 条 neither 逐条分类处置（recipes 12 条保留） |
| 14 | 处理阶段后移 | — | `Preload+1` → `PostLoad+n`，B 类物品可见生效 |
| 15 | 臂章/近战父类互换修复 | — | 纠正常量 + 注释 + 测试标签 |
| 16 | 版本、文档与发行 | 08–15 | 0.2.0 + README/port-notes/spec 镜像 + release zip |
| 17 | 重部署与日志核验 | 16 | overlay 更新（profile 先备份）+ 告警归零 + 验收登记 |

> 08–15 可并行（仅 12 依赖 11）；建议先 08；16 收口；17 需用户配合操作。

## 四、新会话进法（建议）

1. 读 `spec-v0.2.0.md` 与工单；从 frontier 开工（08–15，建议 08 先行）。
2. 每张工单按 `implement` 流程（内驱 `tdd` → 自验证），结果登记到工单 `## Comments`。
3. 全部完成后 `code-review` 双轴审查 → 16 → 17。
4. 部署/验收需用户人工操作（agent 不代启动 MO2/游戏）；验收标准 = 服务器日志中本 mod「未找到/未命中/跳过」类告警 **= 0**。

## Suggested skills（新会话 Skill 工具）

- `implement` / `implement-spec` —— 按工单实现（主流程）
- `tdd` —— 代码类工单（08 / 14 / 15）测试先行
- `code-review` —— 实现后双轴审查（Spec / Standards）
- `spt-mcp-automation`、`diagnosing-spt-problems` —— 部署期需要时
- `writing-spt-modpack-devlog` —— 按需登记

## 五、实现期要点（速记；细则以 spec 为准）

- **无上游 4.x 版本**（双重证否）：数据真相 = 当前整合包实际内容 + 既有风格，不引入上游 mod 数据。
- **A/B/C 边界**（用户已确认）：A 删（包中不存在）；B 后移生效（不删）；C 补档（按既有风格）。
- **阶段常量（4.1）**：Watermark 0 / Preload 100000 / GameCallbacks 200000 / TraderRegistration 300000 / Routers 400000 / HandbookCallbacks 500000 / … / PostLoad；DB 编辑常规窗口 = `PostLoad + n`；加载排序 = TypePriority 升序 + ModGuid 字母序。
- **父类真值**：`5b3f15d486f77432d0509248` = ArmBand 类；`5447e1d04bdc2dff2f8b4567` = Melee 类（来源 mod 与本项目均标反）。
- **环境**：MO2 实例 `E:\Game\EFT_Offline\Norvinsk_Forever`（overlay `[5]软核大修-InescapableTarkovsSoftcore`）；游戏根 `E:\Game\EFT_Offline\SPT_41x`；旧包对照 `E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2`。
- **历史教训**：overlay 目录全树物理路径 ≤260（F2）；含中文 `.ps1` 用 UTF-8 BOM；部署前 profile 备份。
- **边界**：5.0 线（`SamMeow-Inescapable-Tarkovs-Softcore`）不在本版范围；禁止在临时目录写文档。

## 六、用户协作

- 用户负责：启动 MO2/服务器、进游戏抽查（17 提供清单）、对拆分/结果把关。
- 工单拆分如需调整：直接改 `issues/` 文件（低代价），然后照常开工。
