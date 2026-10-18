# Handoff: DoorBreachOverhaul（破门大修）—— 规划完成，从工单 01 起步实施

> 交接日期：2026-10-17 ｜ 交接方：上一会话（研究 / grill-with-docs / to-spec / to-tickets） ｜ 接收方：新会话 agent
> 本文件按项目规则置于仓库 `docs/`（禁止临时目录）。**研究证据与本轮全部决策均已落档，新会话不需要重跑研究。**

## 一、任务一句话

把 Backdoor Bandit（BB-NG master，MIT）作为合并基座移植到 SPT 4.1.6，并并入 Sage's Door Overhaul（BSD-3）的门破坏与门交互能力，产出**单 mod `DoorBreachOverhaul`**（自持维护、独立演进），部署到 MO2 实例 `Norvinsk_Forever`。

## 二、当前状态：研究 → grill → spec → 工单 全部完成（实现尚未开始）

- 研究（机制/迁移/生态 + 实包解包证据）：`.scratch/bbng-dooroverhaul-416/research.md`
- 决策记录（R1+R2 全量 + 三条准则）：`.scratch/bbng-dooroverhaul-416/decisions.md`
- Spec（用户已确认；47 条用户故事 / 实施决策 / 测试决策 / 风险面）：`.scratch/bbng-dooroverhaul-416/spec.md`
- 工单 01–09（均 ready-for-agent）：`.scratch/bbng-dooroverhaul-416/issues/`
- 项目术语表 + ADR-0001/0002：`mods/SamMeow-DoorBreachOverhaul/GLOSSARY.md`、`mods/SamMeow-DoorBreachOverhaul/docs/adr/`
- hub ADR-0012（立项基线与身份）：`docs/adr/0012-sammeow-doorbreachoverhaul-project-baseline.md`

## 三、工单总览（依赖边）

| # | 工单 | Blocked by | 交付（一句话） |
|---|---|---|---|
| 01 | 工程奠基：合并基座与 BB 独立基线（曳光弹） | — | 工程落位 + 身份定型 + 本地配置载体 + 构建/加载/破门基线 |
| 02 | 触点核验：4.1.6 实装审计（BB+DO）与修复 | 01 | ilspycmd 触点表 + 修复；DO 并入前技术闸门 |
| 03 | 破坏并入与上锁门豁免移除 | 02 | DO 破坏全套并入；`locked: untouchable` 移除；双轨生效 |
| 04 | 交互并入与菜单合并 | 03 | Peek/静开/推门/踢枪并入；菜单单点去重 |
| 05 | 规则层、统一命中分派与配置面 | 04 | 纯函数规则层 + S0 单测 + 7 节 F12 全旋钮 |
| 06 | 排除开关体系 | 05 | 总开关 + 高危组子开关 + 物理不可行原因 |
| 07 | C4 客户端行为 | 06 | 引爆→完整破坏管线 + 回退 + 优雅降级 |
| 08 | C4 服务端组件（独立工作流） | 01（可与 02–07 并行） | 4.1 服务端重建（C4 物品/配方/bundle） |
| 09 | 部署与验证收口 | 06 / 07 / 08 | MO2 覆盖层 + 包内替换 + Level C 全清单 |

## 四、新会话进法（建议）

1. 读 `spec.md` 与工单 01；按 `implement` 流程执行（内驱 `tdd` → 自验证），过程与结论登记到工单 `## Comments`。
2. 之后按 frontier 逐张推进（02 → 03 → … → 09）；08 在 01 完成后可与 02–07 并行。
3. 部署与游戏内验证需用户人工操作（**agent 不代启动 MO2/游戏**）；agent 负责日志断言、清单提供与偏差记录。
4. 全部完成后 `code-review` 双轴审查 → 09 收口；**git 提交由用户执行**（本规划会话已留下未提交文件：`.scratch/bbng-dooroverhaul-416/**`、`mods/SamMeow-DoorBreachOverhaul/**`、`docs/adr/0012-*.md`、本文件）。

## Suggested skills（新会话 Skill 工具）

- `implement` / `implement-spec` —— 按工单实现（主流程）
- `tdd` —— 规则层单测（工单 05 / 06）
- `code-review` —— 实现后双轴审查（Spec / Standards）
- `spt-mcp-automation`、`diagnosing-spt-problems` —— 加载/部署期排障
- `writing-spt-modpack-devlog` —— 按需登记

## 五、实现期要点（速记；细则以 spec 与工单为准）

- **身份**：AssemblyName `DoorBreachOverhaul` / GUID `com.sammeow.doorbreach` / 0.1.0（三处同步）。
- **三条准则**：锁门不豁免 / 排除可开关 / 默认宽松。
- **双轨语义（ADR-0001）**：锁血池（破门）与破坏模拟（毁伤）同时消费同一命中；不统一耐久。
- **配置本地化（ADR-0002）**：弃服务端 HTTP 下发；本地 BepInEx（F12，键名英文、显示名中文）。
- **基座物证**：BB-NG 快照 `external/references/backdoorbandit-ng`（master `451c6cb`）；DO `external/references/door-overhaul`（v1.0.0）。
- **环境**：游戏根 `E:\Game\EFT_Offline\SPT_41x`（4.1.6 / EFT 0.16.9.40743）；MO2 实例 `Norvinsk_Forever`；`WTT-ClientCommonLib 3.0.6` 已装于包内；.NET SDK / ilspycmd / 7-Zip 就绪。
- **上游发行包解包**位于 `D:\Temp\opencode\bbng-2.0.4`（临时区；若被清理，从 GitHub release `2.0.4` 重下，SHA256 记录在 `research.md`）。
- **验证注意**：BB-NG master 的 4.1 迁移**无运行时实测**——工单 01/02 即为其验证；WTT 是否编译必需依赖的定论在 02。
- **环境教训（沿用）**：含中文 `.ps1` 用 UTF-8 BOM；overlay 全树物理路径 ≤260；部署前 profile 备份。
- **仓库噪声**：`mods/InescapableTarkovsSoftcore416/tests` 的既有 LSP 报错与本项目无关，忽略。
- **边界**：不做 5.0 线 / Fika / Forge 发布动作；agent 不 git commit、不启动 MO2/游戏。

## 六、用户协作

- 用户负责：启动 MO2/游戏、进游戏场景抽查（09 提供 Level C 清单）、git 提交、对实现结果把关。
- 工单如需调整：直接改 `issues/` 文件（低代价），然后照常开工。
- 用户偏好（速记）：简体中文、无 emoji、无寒暄；代码快失败（不静默 fallback）；注释中文、标识符英文。
