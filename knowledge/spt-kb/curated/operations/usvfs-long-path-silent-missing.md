---
version: [4.1]
domain: both
topic: troubleshooting
source: curated
---
# USVFS 物理路径 >260 字符导致 overlay 文件静默缺失（2026-10-13 实证）

> 状态：已验证（SPT 4.1.6 / MO2 便携实例 Norvinsk_Forever；WTT Content Backport 3 个 bundle 修复全程实测）

## 症状

- 服务器日志出现 `[Error][SPTarkov.Server.Core.Loaders.BundleLoader] Could not find bundle <虚拟路径> for mod <ModID>`；
- 但对应文件**实际存在**于 overlay 目录（虚拟路径与磁盘布局逐项核对一致）；
- 无其他报错，服务器正常启动、游戏可运行——仅个别资源静默缺失，极易漏诊。

## 根因

MO2/USVFS 不把**物理全路径 >260 字符**的 overlay 文件投映给经 MO2 启动的进程（本例为 SPT.Server）。
路径长度从 overlay 根（`<MO2实例>\mods\<mod名>\…`）起算，按完整物理路径计。

## 实证

- 3 个 bundle 物理路径 263 / 268 / 273 字符 → 每次启动均缺失；
- overlay 目录由 `[2]新内容-WTT-正式版内容-ContentBackport` 缩短为 `[2]WTT正式版内容`
  （全树最长 ≤252 字符）后重启，`Could not find bundle` 归零（复验：0 条）。

## 排查与操作约定

- 盘点手法：对实例目录做全量物理路径长度扫描（`robocopy /L` 枚举 + 过滤 >259）。
  ⚠ PowerShell 5.1 `Get-ChildItem -Recurse` 遇超长路径可能**静默跳过**目标，长路径盘点应改用 robocopy。
- overlay 目录命名与内部层级切分须保证**全树物理路径 ≤260**；
- 离线改名后同步修正 profile `modlist.txt` 的目录名（`+<新目录名>`）；改名前确保 MO2 已关闭
  （MO2 运行中会重写 modlist.txt）。
