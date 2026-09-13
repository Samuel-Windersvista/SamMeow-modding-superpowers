---
version: [通用]
domain: both
topic: meta
source: wiki
---
# Tushonka Wiki Vendor 副本 — 上游坐标

本目录是 `SP-Tushonka/wiki` 仓库的 vendor 副本（SP-Tushonka fork 的 wiki Markdown 源），线上渲染于 https://wiki.sp-tushonka.com/。

- 上游仓库：https://github.com/SP-Tushonka/wiki
- 线上渲染版：https://wiki.sp-tushonka.com/（Wiki.js，内容即本仓库）
- 锁定 commit：`392e500531749644c86a580d426e89b04b469386`（2026-09-11）
- 锁定日期：2026-09-14
- 抓取方式：`git clone --depth 50`（经代理 `http://127.0.0.1:7890`）+ robocopy（剔除 `.git`）
- 原始归档位置：`E:\云文件\GitHub\SP-Tushonka-wiki`

## 与 `../wiki/` 的关系

- `../wiki/`：`sp-tarkov/wiki` 官方快照（2026-08-02 锁定，官方仓库已归档）——历史对照用，**保留旧路径引用不动**。
- 本目录：fork 持续维护的现行 wiki，页面结构已重组（4.x 页面归入 `SPT_4x/`，新增 `SPT_50/`、`Archived_Pending_Deletion/`）。
- 新知识优先查本目录；涉及旧路径的既有引用（curated/、docs/）仍指向 `../wiki/`，两棵树并存。

## 更新方法

```powershell
git -C "E:\云文件\GitHub\SP-Tushonka-wiki" pull
robocopy "E:\云文件\GitHub\SP-Tushonka-wiki" "本目录" /E /XD .git
# 然后更新本文件的 commit hash 与日期，并同步 index.json（新增/变更文件）
```

## 注意

- 本目录内容以 CC BY-NC-ND 许可（见 LICENSE 文件），仅作存档与学习用途。
- 修改请改 `curated/` 下的提炼文档，不要直接改本目录，保持与上游可对照。
