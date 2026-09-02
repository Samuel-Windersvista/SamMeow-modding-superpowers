---
version: [3.11, 4.1]
domain: both
topic: conflict-analysis
source: curated
---
# Mod 冲突分析的正确输入模型（2026-08-05 修正）

> 状态：已验证（诺文斯克 0.3.2 整合包，165 启用 mod，0 真冲突）
> 用途：spt-conflict-audit / building-spt-modpack 的冲突分析输入规则

## 三条铁律（从误报事故沉淀）

### 1. 正确输入 = modlist.txt 的启用 mod

冲突分析的对象必须是 **MO2 里实际启用的 mod**，不是 `mods/` 目录的全部。

MO2 的启用状态由 `profiles/<profilename>/modlist.txt` 决定：
- `+modname` — 启用（参与游戏运行，纳入冲突分析）
- `-modname` — 禁用（不参与游戏运行，**必须排除**）
- `_backup` 后缀 — 备份版本（通常禁用或需人工确认）

**错误案例**：把 `friendlyPMC - 已AI优化`（禁用）和 `friendlyPMC 与 PITFireTeam 合并版本`（启用）
同时纳入分析，误报"同一 mod 双版本共存冲突"。实际只有启用的那个参与运行。

### 2. 嵌套路径必须完整映射（不能截断前缀）

3.11 混合 mod 的结构：`mods/<ModName>/user/mods/<actual-name>/...`
- 客户端部分：`mods/<ModName>/BepInEx/plugins/...`
- 服务端部分：`mods/<ModName>/user/mods/<actual-name>/package.json`

**文件扫描必须用 mod 根目录**（`mods/<ModName>/`），不能用嵌套子目录。
若扫描 `user/mods/<name>/`，`relative()` 会丢失 `user/mods/<name>/` 前缀：

```
错误（截断前缀）:
  Hephaestus:    db/base.json        <- 看起来"同路径"
  Scorpion:      db/base.json        <- 错误判为冲突

正确（完整前缀）:
  Hephaestus:    user/mods/Hephaestus/db/base.json
  Scorpion:      user/mods/acidphantasm-scorpion/db/base.json
  -> 路径不同，不冲突（两个 mod 各自的服务端文件）
```

### 3. 同路径文件必须比内容 hash（SHA-1）

即使路径真的相同，也要看内容是否一致：
- **同路径 + hash 全相同** → `duplicate_file`（无害重复：内容一致，谁覆盖谁都一样）
- **同路径 + hash 不同** → `file_overwrite`（真冲突：未胜出内容被静默遮蔽）

忽略结构性噪音：`package.json`/`meta.ini`/`src/mod.ts`/开发脚手架
（`build.mjs`/`tsconfig.json`/`package-lock.json`/`readme.md`）——各 mod 自带项目文件，内容不同正常。

## 验证过程（三层错误如何被纠正）

| 阶段 | 误报数 | 原因 |
|------|--------|------|
| 原始分析 | 34 真冲突 | 嵌套路径截断 + 未过滤 modlist + 同内容误报 |
| 加 hash 过滤后 | 11 真冲突 | 只剩双版本共存（但 friendlyPMC/RaidOverhaul 其中一个是禁用） |
| 过滤 modlist.txt 后 | **0 真冲突** | 禁用的 mod 全部排除 |

## 应用位置

- `spt-mcp` 的 `spt_analyze_conflicts` 工具（输入 modPaths 前应先按 modlist.txt 过滤）
- `spt-conflict-audit` skill（Step 1: Collect mod inventory）
- `building-spt-modpack` skill（构建前冲突验证）

## 关联

- `knowledge/spt-kb/curated/operations/destructive-operation-guardrails.md` — 破坏性操作护栏
- `docs/wayfinder/findings/001-spt-conflict-taxonomy.md` — 20 型冲突分类学
- MO2 modlist.txt 格式：`+` 启用 / `-` 禁用 / `+name_separator` 分隔符
