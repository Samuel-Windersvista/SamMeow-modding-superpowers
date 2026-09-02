---
version: [通用]
domain: both
topic: meta
source: wiki
---
# Wiki Vendor 副本 — 上游坐标

本目录是 `sp-tarkov/wiki` 仓库的 vendor 副本（SPT 官方 wiki 的 Markdown 源）。

- 上游仓库：https://github.com/sp-tarkov/wiki
- 线上渲染版：https://wiki.sp-tarkov.com/（Wiki.js，内容即本仓库）
- 锁定 commit：`ba82cdff8b2690885a4f240fc0c479e4997b506c`
- 锁定日期：2026-08-02
- 抓取方式：`git clone` 后 robocopy（剔除 .git）
- 原始归档位置：`E:\云文件\GitHub\G-网站与维基/wiki\`（含完整 git 历史）

## 更新方法

```powershell
git -C "E:\云文件\GitHub\G-网站与维基/wiki" pull
robocopy "E:\云文件\GitHub\G-网站与维基/wiki" "本目录" /E /XD .git
# 然后更新本文件的 commit hash 与日期
```

## 注意

- 本目录内容以 CC BY-NC-ND 许可（见 LICENSE 文件），仅作存档与学习用途
- 修改请改 curated/ 下的提炼文档，不要直接改本目录，保持与上游可对照
