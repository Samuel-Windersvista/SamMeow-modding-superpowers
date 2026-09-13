# scripts/spt-kb —— sp-mod.com 源码归档工具

把 [sp-mod.com](https://sp-mod.com) 上按 SPT 版本 + 更新时间窗口筛选出的 mod 源码，
批量浅克隆进知识库归档目录 `knowledge/spt-kb/archive/forge/mods/`，并生成 provenance MANIFEST。

本目录脚本源自 2026-09 一次手工采集（271 mod / 297 条源码链接）跑通的临时脚本，
现固化为可重复使用的仓库工具。算法照搬，仅做参数化与合并。

## 三步工作流

```
fetch  →  clone  →  finalize
抓列表    浅克隆     重写 MANIFEST
```

### 1. fetch —— 抓取 mod 列表

```powershell
node scripts/spt-kb/fetch-mods.mjs `
  --spt-version 4.1.5 `
  --since 2026-08-14 --until 2026-09-14 `
  --out-dir D:\Temp\opencode\fetch-test
```

- 调用 `https://sp-mod.com/api/v0/mods`，参数：
  `per_page=50&page=<N>&filter[spt_version]=<v>&filter[updated_between]=<since>,<until>&include=source_code_links,versions&sort=-updated_at`
- 按响应 `meta.last_page` 翻页，每页写一个 `mods-p<N>.json`（compact JSON，形状与 API 响应一致）。
- 结束时打印 `DONE total=<API total> count=<实际条数> pages=<页数>`。

参数（均可选）：

| 参数 | 说明 | 默认 |
|------|------|------|
| `--spt-version` | SPT 版本过滤，如 `4.1.5` | 不限 |
| `--since` / `--until` | 更新时间窗口 `YYYY-MM-DD` | 不限 |
| `--out-dir` | 输出目录 | `../../knowledge/spt-kb/archive/forge/.incoming/`（相对本脚本） |

### 2. clone —— 批量浅克隆源码

```powershell
# 先看计划（不落盘）
node scripts/spt-kb/clone-sources.mjs --dry-run --input-dir D:\Temp\opencode\fetch-test

# 确认后执行
node scripts/spt-kb/clone-sources.mjs --input-dir D:\Temp\opencode\fetch-test
```

- 从 `--input-dir` 读取全部 `mods-p*.json`（按页码顺序）。
- 对每条 `source_code_links[].url` 规范化后 `git clone --depth 1`。
- 目标目录：`knowledge/spt-kb/archive/forge/mods/<sanitize(name)>_<id>[_<n>]_source`
  - 目录名 sanitize：`<>:"/\|?*` → `-`，空白 → `-`，连续 `-` 合并，首尾 `-` 去除，截断 80 字符。
  - 同一 mod 有多条链接时，第 2、3… 条分别加 `-2`、`-3` 后缀。
  - 按 URL 去重（忽略大小写与结尾 `.git`）：跨 mod 复用同一仓库只克隆一次。
- 目录已存在则跳过（可重复运行、断点续跑）。
- 失败重试 1 次（共 2 次尝试）。
- 结束打印 `DONE total=.. ok=.. skip=.. fail=..`。
- `--dry-run`：只打印每条计划（含规范化后的 clone 命令），不执行、不落盘。

参数：

| 参数 | 说明 | 默认 |
|------|------|------|
| `--input-dir` | `mods-p*.json` 所在目录 | `../../knowledge/spt-kb/archive/forge/.incoming/` |
| `--dry-run` | 只打印计划 | 关闭 |

### 3. finalize —— 以磁盘实际状态重写 MANIFEST

```powershell
# 只读统计
node scripts/spt-kb/finalize-manifest.mjs --dry-run `
  --input-dir D:\Temp\opencode\fetch-test `
  --spt-version 4.1.5 --since 2026-08-14 --until 2026-09-14

# 正式写出
node scripts/spt-kb/finalize-manifest.mjs `
  --input-dir D:\Temp\opencode\fetch-test `
  --spt-version 4.1.5 --since 2026-08-14 --until 2026-09-14
```

- 不信任克隆时的内存结果：重新扫描目标目录，逐条 `git rev-parse HEAD` 取真实 commit。
- 写出 `knowledge/spt-kb/archive/forge/mods/MANIFEST-sp-mod-<YYYY-MM>.md`。
- 缺失目录计为 `missing` 并在末尾列出；`--dry-run` 时不写文件（有 missing 时退出码 2）。

参数：

| 参数 | 说明 | 默认 |
|------|------|------|
| `--input-dir` | `mods-p*.json` 所在目录 | `../../knowledge/spt-kb/archive/forge/.incoming/` |
| `--month` | MANIFEST 文件名里的 `YYYY-MM` | 由 `--date` 推导 |
| `--date` | 抓取日期（写入表头） | 今天 |
| `--spt-version` / `--since` / `--until` | 表头 provenance | 省略则写通用来源行 |
| `--dry-run` | 只打印统计，不写文件 | 关闭 |

## 代理

所有网络操作都走 HTTP 代理，因为 Node 原生 `fetch` 不原生支持 HTTP 代理：

- `fetch-mods.mjs` 通过 `curl.exe -g -sS -m 120 -x <proxy>` 发请求。
- `clone-sources.mjs` 通过 `git -c http.proxy=<proxy> clone ...` 克隆。

代理地址由环境变量 `SP_MOD_PROXY` 提供，默认 `http://127.0.0.1:7890`。

```powershell
$env:SP_MOD_PROXY = "http://127.0.0.1:1080"
```

## [重要] URL 规范化教训

API 的 `source_code_links[].url` 并不总是仓库根 URL，而是**混有 GitHub 网页 URL**：

```
https://github.com/LycorisOni/SiccCaseFix/tree/4.1
https://github.com/<o>/<r>/releases/tag/<tag>
```

直接 `git clone` 这类 URL 会失败。必须先规范化：

```
https://github.com/<o>/<r>/(tree|<releases/tag>)/<ref>
   ↓
git clone --depth 1 --branch <ref> https://github.com/<o>/<r>
```

`clone-sources.mjs` 已内建该规范化（合并自原 `fixup-tree-urls.mjs`）。
去重与目录命名仍以**原始 URL** 为准，因此同仓库的 `/tree/<ref>` 与根 URL 会各占一个目录
（分别带不同 ref / commit），MANIFEST 中保留原始 URL 以便追溯。

## MANIFEST 格式

`MANIFEST-sp-mod-<YYYY-MM>.md`，Markdown：

```
# sp-mod.com 源码归档 MANIFEST（SPT <version>）

- 抓取日期：<YYYY-MM-DD>
- 来源：https://sp-mod.com API v0（filter[spt_version]=... & filter[updated_between]=...,...）
- 克隆方式：git clone --depth 1（经代理 <proxy>；/tree/<branch> 链接以 --branch 补克隆）；目录 = mods/<Name>_<id>_source/
- 统计：total=.. ok=.. missing=..

| 目录 | Mod | id | 仓库 | commit | 状态 |
|------|-----|----|------|--------|------|
| <dir> | <name> | <id> | <原始 URL> | <sha 或 -> | ok / missing |
```

- `total` = 去重后的源码条目数（不是 mod 数：一个 mod 可有多条链接）。
- `ok` = 目录存在；`missing` = 目录不存在。
- `commit` = 该目录 `HEAD` 的完整 SHA。

## 依赖

- **Windows**（脚本在 win32 上验证；`curl.exe` 为 Windows 10+ 自带）。
- **Node.js**（ESM，`.mjs`；开发用 v24）。
- **git**（`--depth 1` 浅克隆、`--branch`、`rev-parse`）。
- **curl.exe**（仅 `fetch-mods.mjs` 需要，用于代理请求）。

## 硬规则

- 不要手改 `knowledge/spt-kb/archive/` 下的归档内容与已生成的 MANIFEST；
  变更一律通过重跑 `fetch → clone → finalize` 产生。
- 目录名与去重规则是归档稳定性的契约，改动会导致与既有归档错位。
