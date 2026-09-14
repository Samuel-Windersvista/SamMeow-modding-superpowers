---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 10 验证流程（VERIFY）

> **Domain slug:** `VERIFY` · **规则 ID 前缀:** `STD-VERIFY-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 05，2026-09-14）。
> 范围：本维度面向 modpack 批次验证与单 mod 冒烟；纯源码仓库（不产出归档）场景按 N-A 处理。

## 维度范围

- build → 本地 server 冒烟 → 日志断言
- 既有工具链衔接（MO2 安装/备注工具、server 日志解析、tarkov-runtime 快照断言）

## 规则

### STD-VERIFY-001 — 以 Release 构建作为验证的第一步

- **Level:** MUST
- **Applies:** both
- **Evidence:** 机制：`templates/server-mod/ServerModTemplate.csproj`、`templates/client-mod/ClientModTemplate.csproj`（工程须构建产出可部署 DLL）；`skills/building-spt-modpack/SKILL.md`（`dotnet build -c Release`）；语料：近期 297 个目录中 278 个含 `.csproj`（EV-CORPUS-LANG）、35 个含构建脚本（EV-CORPUS-STRUCT）
- **Rule:** 任何验证批次前，先以 Release 配置构建服务端与客户端工程，确认零编译错误并产出待部署 DLL；构建失败即中止验证。

```powershell
dotnet build -c Release
```

> 深入：[modding-guide/01-environment-toolchain.md](../../curated/modding-guide/01-environment-toolchain.md)

### STD-VERIFY-002 — 经 MO2 走完 Level B 冒烟链

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`skills/testing-spt-modpack/SKILL.md`（Level B 验收链：MO2 启动 SPT → server 起来 → launcher 连接 → 到达主菜单）；`skills/building-spt-modpack/SKILL.md`（Level B 最低验证）；语料：机制推断，无语料先例（EV-NOCORPUS）
- **Rule:** 安装批次后，经 MO2 启动 SPT 并逐级确认：`SPT.Server.exe` 启动并保持运行、`SPT.Launcher.exe` 连接成功、游戏到达主菜单；链路在哪一级中断即记录为失败点。

```
MO2 启动 SPT  ->  SPT.Server.exe 起来  ->  SPT.Launcher.exe 连接  ->  主菜单
     |                   |                        |                    |
   经 VFS 启动       user/logs/ 有就绪信号      profile 列表加载    BepInEx 客户端无崩溃
```

> 深入：[skills/testing-spt-modpack/SKILL.md](../../../../skills/testing-spt-modpack/SKILL.md)

### STD-VERIFY-003 — 断言每个服务端 mod 的加载日志

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`skills/testing-spt-modpack/SKILL.md`（解析 `user/logs/` 中每个 `user/mods/<Mod>` 加载行，静默缺失即失败信号）；`skills/diagnosing-spt-problems/SKILL.md`（server 日志位于 `<SPT_Root>\user\logs\`）；语料：机制推断，无语料先例（EV-NOCORPUS）
- **Rule:** 解析 server 日志（`user/logs/`，取最新文件），为批次中每个服务端 mod 找到对应加载行；缺失加载行视为失败信号（可能未通过校验或放错目录树）。

```powershell
$log = Get-ChildItem "<SPT_Root>\user\logs" -File |
    Sort-Object LastWriteTime -Descending | Select-Object -First 1
Select-String -LiteralPath $log.FullName -Pattern '<ModName>'
```

> 深入：[skills/diagnosing-spt-problems/SKILL.md](../../../../skills/diagnosing-spt-problems/SKILL.md)、[api-notes-4.1/mod-loading.md](../../curated/api-notes-4.1/mod-loading.md)

### STD-VERIFY-004 — 断言每个客户端插件的加载日志

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`skills/testing-spt-modpack/SKILL.md`（BepInEx 控制台 / `BepInEx/LogOutput.log` 中每个插件加载行）；`knowledge/spt-kb/curated/migration/pilot-experience-lootingbots.md:126`（启动后查 `BepInEx/LogOutput.log` 完成加载级验证）；语料：机制推断，无语料先例（EV-NOCORPUS）
- **Rule:** 解析 `BepInEx/LogOutput.log` 与控制台输出，确认批次中每个客户端插件已加载且无异常；缺失加载行或出现 Harmony patch 异常即失败信号。

```powershell
Select-String -LiteralPath "<SPT_Root>\BepInEx\LogOutput.log" -Pattern '<PluginGuid>'
```

> 深入：[skills/diagnosing-spt-problems/SKILL.md](../../../../skills/diagnosing-spt-problems/SKILL.md)

### STD-VERIFY-005 — 用 tarkov-runtime MCP 做结构化状态断言

- **Level:** SHOULD
- **Applies:** 5.0
- **Evidence:** 机制：`tools/tarkov-runtime-mcp/src/index.ts`（`tarkov_server_status` 返回 server 版本、BEM tag 门禁结果与已加载 mod 清单；`tarkov_snapshot` 返回 profile/traders/quests/hideout/inventory 计数快照；`tarkov_wait_for` 谓词轮询、超时返回结构化 `WAIT_TIMEOUT`）；语料：机制推断，无语料先例（EV-NOCORPUS）
- **Rule:** 在 SPT 5.x 上，用 `tarkov_server_status` 断言已加载 mod 清单、用 `tarkov_wait_for` 等待 server 就绪或 mod 加载完成、用 `tarkov_snapshot` 对 profile/商人/任务/藏身处做确定性断言，替代 sleep 与人工看日志。

```
tarkov_server_status                              # 断言 mod 清单含批次每个 mod
tarkov_wait_for("server_status.mods contains <ModGuid>", timeout)   # 等待加载完成
tarkov_snapshot(sections: ["traders", "quests"])  # 断言商人类/任务类 mod 生效
```

> 深入：[tools/tarkov-runtime-mcp/src/index.ts](../../../../tools/tarkov-runtime-mcp/src/index.ts)、[.scratch/tarkov-runtime-mcp/spec.md](../../../../.scratch/tarkov-runtime-mcp/spec.md)

### STD-VERIFY-006 — 启动冒烟前用 spt-mcp 做离线预检

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`tools/spt-mcp/src/tools/list-mods.ts`（扫描 mod 清单）、`predict-load-order.ts`（预测服务端加载顺序）、`analyze-conflicts.ts`（重复 GUID / 文件覆盖 / JSON key 碰撞）；语料：机制推断，无语料先例（EV-NOCORPUS）
- **Rule:** 在启动冒烟前，用 `spt_list_mods` 核对安装目录中的 mod 清单、`spt_predict_load_order` 预测服务端加载顺序、`spt_analyze_conflicts` 检出冲突，作为日志断言前的静态基线。注意工具限制：`spt_list_mods(type: "server")` 仅扫描 `package.json`（JS/TS mod），C# DLL-only 服务端 mod 不会被列出，需结合其他手段（客户端插件扫描 / BepInEx 日志）核对。

```
spt_list_mods(path: "<SPT_Root>/SPT_Runtime/user/mods", type: "server")
spt_predict_load_order(modPaths: [...])
spt_analyze_conflicts(modPaths: [...], sptPath: "<SPT_Root>")
```

> 深入：[tools/spt-mcp/src/tools/analyze-conflicts.ts](../../../../tools/spt-mcp/src/tools/analyze-conflicts.ts)、[skills/spt-conflict-audit/SKILL.md](../../../../skills/spt-conflict-audit/SKILL.md)

### STD-VERIFY-007 — 验证失败即移交 diagnosing-spt-problems

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`skills/testing-spt-modpack/SKILL.md`（失败信号退出本技能、移交 `diagnosing-spt-problems`，不在验证流程内就地改修）；语料：机制推断，无语料先例（EV-NOCORPUS）
- **Rule:** 一旦出现失败信号（server 未起、mod 未加载、CTD、预期内容缺失、严重本地性能崩溃），停止"测试"并移交 `diagnosing-spt-problems`，不在验证流程内就地改修。

> 深入：[skills/testing-spt-modpack/SKILL.md](../../../../skills/testing-spt-modpack/SKILL.md)、[skills/diagnosing-spt-problems/SKILL.md](../../../../skills/diagnosing-spt-problems/SKILL.md)

### STD-VERIFY-008 — 在主进度之外的 profile/save 边界验证

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`skills/testing-spt-modpack/SKILL.md`（用一次性/批次前 profile 或存档边界；PASS 前不写入主进度）；语料：机制推断，无语料先例（EV-NOCORPUS）
- **Rule:** 在一次性或批次前的测试 profile 上验证，批次获得 PASS 前不将未验证状态写入主进度存档。

> 深入：[skills/testing-spt-modpack/SKILL.md](../../../../skills/testing-spt-modpack/SKILL.md)、[wiki-tushonka/SPT_4x/Profiles.md](../../wiki-tushonka/SPT_4x/Profiles.md)

### STD-VERIFY-009 — 以 raid 冒烟作为可选深验（Level C）

- **Level:** MAY
- **Applies:** both
- **Evidence:** 机制：`skills/testing-spt-modpack/SKILL.md`（Level C：进 raid 观察批次承诺效果，仅当影响只在局内可见时）；语料：机制推断，无语料先例（EV-NOCORPUS）
- **Rule:** 当批次的影响只在局内可见（商人 assort、loot、bot、任务、物品）时，可选执行一次 raid 冒烟，观察承诺效果且无即时 CTD；Level B 通过即为批次验收门槛。

> 深入：[skills/testing-spt-modpack/SKILL.md](../../../../skills/testing-spt-modpack/SKILL.md)
