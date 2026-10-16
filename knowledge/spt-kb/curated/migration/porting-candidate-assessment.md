---
version: [4.1]
domain: both
topic: migration
title: "移植候选批量评估方法论（4.1.6 未就绪清单实战）：源码真实性核验 / 上游就绪复核 / 迁移量口径 / 难度分级"
keywords: [移植评估, 源码快照, Forge, 上游就绪, LOC, 难度分级, 重写型, 依赖阻断, Softcore-NG, 静态评估, 性价比顺序, 数据可信度, 未就绪清单, net10.0, BepInEx, 客户端引用集, 目标线, 归档快照]
summary: "《永远的诺文斯克》4.1.6 未就绪清单静态评估沉淀出的批量移植候选筛选方法：四项判据——源码真实性核验（逐目录清点 .cs/.ts/.csproj/.sln/.dll，剔除仅剩 bin/obj 的快照）、上游就绪复核（源码证据显示疑似已就绪者先与 Forge/Hub 实况核对再决定自建）、实际迁移量口径（以有效源码 LOC 计而非文件数，剔除 types/*.d.ts 与 node_modules）、难度分级与结论分类（低/中/高 + 适合移植/有条件/重写型/不建议）；三项附加维度——依赖阻断、冲突矩阵、可裁剪性；直接结论：B 类 22 快照仅 5 个含真实源码，IOF/BRNVG 疑似已就绪应先复核，Softcore-NG 重定向一次覆盖 5 个未就绪条目为最高性价比，Realism/PTT/RaidOverhaul/Softcore 直译四项为高工作量重写型；输出物为分档详述 + 性价比顺序表 + 待复核事项"
---

# 移植候选批量评估方法论（4.1.6 未就绪清单实战）：源码真实性核验 / 上游就绪复核 / 迁移量口径 / 难度分级

> 来源：2026-10-06《永远的诺文斯克》4.1.6 未就绪清单静态评估（未编译、未实机、只读）；评估文件 `.scratch/norvinsk-forever-4.1.6/porting-assessment.md`（含 A 类自有仓库 16 库 / B 类 Forge 归档快照 22 个）。

## 一、适用场景与输入

当整合包的未就绪清单里有一批「本地已有源码」的候选、需要快速判断哪些值得排期移植、哪些只是重复劳动或伪源码时，使用本方法。

- 评估对象：未就绪清单中「本地已有源码」的交集——A 类自有 GitHub 仓库 13 组 / 16 库；B 类 Forge 归档快照 20 组 / 22 个。
- 评估目标：SPT 4.1.6（服务端 net10.0；客户端 BepInEx 5 / net472 / netstandard2.1）。
- 方法：静态源码评估（未编译、未实机、只读），逐候选对照 `curated/migration/api-mapping-311-to-41.md`（4 大阻断器 + 难度占比低约 50% / 中约 35% / 高约 15%）；以 `Moew-SAIN-For-4013`（3.x → 4.0.13 客户端+服务端移植先例）为参照。
- 输入前提：清单已按 pack 定位编号；候选清点报告已生成但含自动匹配误报，须人工过滤后再进入评估。
- 评估性质：只读，未改动任何被评估仓库；结论仅针对「本地已有源码」，无源码快照不纳入移植基础。

## 二、四项判据

### 判据 1：源码真实性核验

目录名含 `_source` 不等于有源码。做法是逐目录清点 `.cs / .ts / .csproj / .sln / .dll`，凡只剩 `bin/obj` 构建残件（`AssemblyInfo` 等生成代码与缓存，无源无 DLL）者一律剔除，不可作为移植基础。

- 本次 22 个快照中仅 5 个含真实源码：DadGamerMode_686、IOF-2960、Headshot-Damage-Redirection_2953、Item-Context-Menu-Extended_940、BRNVG_954；其余 17 个不可移植。
- 构件的版本/年代证据可从 `obj\**\AssemblyInfo.cs` 与 `project.assets.json` 反查（如 ProfileEditor 4.1.0 / net10.0-windows）。
- A 类自有仓库则核对 csproj 的目标框架 TF、硬编码引用路径、源码 LOC、入口文件与补丁/服务面计数。

### 判据 2：上游就绪复核

源码证据（README / 包引用 / 元数据）显示「疑似已就绪」者，先与 Forge/Hub 实况复核，再决定是否自建，避免重复劳动。本次两例：

- **IOF-2960**：README 明示 "Ported to SPT 4.1.x by flir"，作者已产出 4.1.x 移植源码。
- **BRNVG-954**：服务端 `PackageReference` 直接引用 `SPTarkov.Server.Core 4.1.0`，`Metadata.SptVersion = Range("~4.1.0")`。

### 判据 3：实际迁移量口径

清点脚本报出的文件数（如 221/219/218 个 TS 文件）包含 `types/*.d.ts` 与 node_modules，会显著高估。口径统一为「去掉类型声明与依赖目录后的有效源码 LOC」。

- 以有效 LOC 计，实际迁移量常小一个数量级（本次 IDC 服务端仅 555 LOC、HomeComforts 465、LeaveItThere 496）。
- 仓库内 `-副本` 重复文件须剔除，实际源文件数量以有效文件为准。

### 判据 4：难度分级与结论分类

两轴并行，不得只给难度不给去向：

- 难度分级：低 / 中 / 高，附人天区间。
- 结论分类：适合移植 / 适合移植（有条件 / 被依赖阻断）/ 重写型 / 不建议。
- 前置排查：先查「是否已由 SPT/BSG 修复」或「上游是否已有 4.x 版本」，避免重复修复与重复移植（本次 WhiteBoxFix、ThatsLit 均属此列）。

## 三、附加维度

- 依赖阻断：跨仓库硬依赖会改变结论。本次 WHM（A9）硬依赖 `RealismMod.dll` 公开 API → 若 Realism 不上 4.1.6，其结论由「适合移植」升级为「不可移植」（或需改造去除依赖，工作量升为高）。
- 冲突矩阵：与 pack 已装 mod 的交互面须逐项核对，尤其 bot 生成、AI 视线、经济数值、物品交互区（Realism 与已装 69 项中的 SAIN/APBS/ABPS 等深度耦合）。
- 可裁剪性：单机整合包可直接删除 Fika / DissonanceVoip / FikaModule 等多人组件以降低工作量（本次 ImmersiveDaylightCycle、HomeComforts、LeaveItThere 均适用）。

## 四、本次评估的直接结论（仍有效）

- **IOF（2960）**：源码 README 明示 "Ported to SPT 4.1.x by flir"，已有 4.1.x 移植源码，重编译即用——先复核 pack 的 pending 判定口径（filter spt_version=4.1.6）。
- **BRNVG（954）**：服务端已是 4.1 世代（net10.0 + `SPTarkov.Server.Core 4.1.0` 包引用，Metadata SptVersion ~4.1.0），近乎重编译；对照表归其为「非 Forge（Hub 独占）」，需核对 Hub 版本与授权，可能零开发。
- **Softcore-NG（`SamMeow-Inescapable-Tarkovs-Softcore`）**：5.0 线 C# 服务端整合 mod（52 文件 / 5260 LOC），一次覆盖 Softcore / True Items / noFIR / 反重力臂章 / 更大背包 5 个未就绪条目，最高性价比；重定向 4.1.6 需实测 API 漂移（半天级），可行即清 5 项，失败则归 5.0 线。该方向后续已实际执行并发布（反向移植实战：`curated/migration/pilot-experience-softcore-416-backport.md`；正向线开发经验：`pilot-experience-inescapable-softcore.md`）。
- **重写型（高工作量）四项**：Realism（客户端 28.5K + 服务端 33K LOC）、PTT+IEAPI（5.1K + 7.8K）、RaidOverhaul（5.2K + 10.8K）、Softcore 直译（5.3K）；不建议以「顺手移植」排期，须专项先决策 4.1.6 / 5.0 目标线。
- **战略前提**：SPT 5.0 已发布为前进线，且用户已在 5.0 线重写（Inescapable 系列）。数千行级重写型移植前应先定目标线，避免 4.1.6 资产短期废弃；低成本客户端/数据类可先行。

## 五、输出物形态（分档详述 + 性价比顺序表 + 待复核事项）

评估报告应包含三部分：

1. **分档详述**：适合移植 / 重写型 / 不建议各档逐条列「事实（体量 + 核心文件 + 引用形态）→ 工作内容 → 风险 → 结论（含人天）」。
2. **性价比移植顺序表**：按收益 / 成本 / 风险排序。本次低成本可先行类——BRNVG、IOF、Headshot-Damage-Redirection（单补丁 3 文件）、ImmersiveDaylightCycle（服务端仅 5 路由）、BetterBackpacks（数据型）、HollywoodCam、DadGamerMode、ItemContextMenuExt+CustomInteractions（一次清两条目）。
3. **数据可信度与待复核事项**：标注「经深入核对」与「快速判断（未编译）」的分界，列明影响结论的待复核项——IOF pending 口径、BRNVG Hub 版本与授权、Softcore-NG 重定向可行性、B 类无源码快照是否取上游、4.1.6 客户端引用集（Managed + `BepInEx\plugins\spt`）是否就绪。

## 相关

- 上游映射与 4 大阻断器：`curated/migration/api-mapping-311-to-41.md`
- Softcore-NG 实战经验（正向 5.0 开发）：`curated/migration/pilot-experience-inescapable-softcore.md`
- Softcore-NG 反向移植 4.1.6 实战（后续执行）：`curated/migration/pilot-experience-softcore-416-backport.md`
- 服务端 3.11→4.1 重写模板：`curated/migration/server-mod-311-to-41.md`
