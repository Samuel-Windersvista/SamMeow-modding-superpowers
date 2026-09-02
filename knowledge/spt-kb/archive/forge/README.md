# SPT Forge 归档（archive/forge）

> 抓取日期：2026-08-10 全量刷新（hot-mods 详情 + 版本历史 + 成品 zip 重抓；源码 git clone 全量 pull） | 数据源：forge.sp-tarkov.com 公开只读 API（v0，无认证，限流）
> 目的：模组站若随 SPT 停止运作，本站数据仍有完整副本。

## 目录结构

```
archive/forge/
├── README.md               本文件
├── hot-index.json          热门 mod 索引（94 条：元数据 + 源码链接 + 最佳版本下载）
├── api/                    原始 API 快照（JSON）
│   ├── mods-catalog.json   全站目录 1830 条（id/name/slug/下载量等）
│   ├── spt-versions.json   SPT 版本参考数据
│   ├── mod-categories.json 分类参考数据
│   └── hot-mods/           每个热门 mod：<id>.json（详情）+ <id>.versions.json（全版本历史）
├── mods/                   提取层
│   └── <id>_source/        mod 源码（4.x 兼容 mod 全覆盖，共 124 个；64 个 git clone + 60 个早期 zip 解压）
└── tools/                  抓取脚本（可重跑）
    ├── snapshot.ps1        全站目录 + 热门详情（幂等，断点续跑）
    ├── fetch-versions.ps1  版本历史（3s 间隔防限流，分批）
    ├── fetch-releases.ps1  成品 zip 下载（断点续跑）
    └── build-index.ps1     本地重建 hot-index.json（离线）
```

## 热门清单来源

wiki 官方推荐页的点名并集（`../wiki/Recommended_Mods_40.md` + `../wiki/SPT_311/Recommended_Mods_311.md`），共 95 个在站 mod。

## 数据规模

| 项 | 数量 |
|----|------|
| 全站目录 | 1830 mod |
| 热门 mod 详情 | 94 |
| 源码（4.x 全覆盖） | 124（64 git clone + 60 zip 解压） |

> 2026-09-02：成品 zip（原 94 个 release 目录约 397.8MB）已清理——release 版对知识参照价值低且占空间，源码 clone 足够。`hot-index.json` 的 `best_link` 为 Forge 站外 URL 记录，不受影响。

## 无法获取（404，站内已删除或下架）

551（Custom Raid Times）、562（Valen's Progression）、1159（BDSM）、1311（RAM Cleaner Fix）、1698（QuickSell 3.11）、2250（Keep Starting Gear 3.11）、2389（Expanded Task Text 4.0）

## 4.1 现状速览（2026-08-10 刷新）

热门 mod 最佳版本中已标注 `~4.1` 兼容的共 32 个（4.0 系 35 个、3.11 系 12 个、其余兼容区间重叠）。4.1 生态较 8 月初（仅 4 个）大幅迁移，与 wiki SPT_41 迁移文档吻合。

## 源码备份（2026-08-07 扩至 4.x 全覆盖）

95 个热门 mod 中 84 个标注 4.x（4.0/4.1）兼容，其源码已全部备份至 `mods/<id>_source/`。源码链接来源：
1. Forge mod 详情页 HTML 的 **Source Code** 区块（`https://forge.sp-tarkov.com/mod/<id>/<slug>`，正则提取 href）——大部分 mod 在此
2. GitHub 搜索兜底（`q=<mod名> user:<owner> in:name`，gh CLI token 认证）
3. 个别在 GitLab（flir 的 betterkeys-ng / enemymarkers）

已知源码链接已回写 `hot-index.json` 的 `github` 字段（64 条，2026-08-10 刷新）。注意：
- 910 + 940 共用 `IgorEisberg/SPT-ClientMods` 合集仓库
- 2200 的 Forge 链接指向 releases/tag，clone 用仓库根 `Shibatsui/SPT-Dynamic-External-Resolution`
- 闭源 mod（GitHub/GitLab 均无公开仓库）不在此列（release zip 已清理，闭源 mod 如需反编译可重跑 `fetch-releases.ps1` 临时下载）

## 重跑方法

```powershell
# 全部重抓（幂等）
powershell -ExecutionPolicy Bypass -File tools\snapshot.ps1
# 补版本历史（3s 间隔，每次最多 25 个，重复执行直至全完成）
powershell -ExecutionPolicy Bypass -File tools\fetch-versions.ps1 -MaxIds 25
# 补成品下载（注意：release zip 已于 2026-09-02 清理，仅临时需要时使用）
powershell -ExecutionPolicy Bypass -File tools\fetch-releases.ps1
# 重建索引
powershell -ExecutionPolicy Bypass -File tools\build-index.ps1
# 补源码：Forge 详情页 HTML 提取 Source Code 链接（方法见上「源码备份」节）
# 批量 clone：git clone --depth 1 <url> mods/<id>_source/（浅克隆够用；如需全历史去掉 --depth 1）
```

## 源码链接提取方法（2026-08-07 验证）

```powershell
# 从 Forge mod 详情页提取 Source Code 链接
$r = Invoke-WebRequest -Uri "https://forge.sp-tarkov.com/mod/<id>/<slug>" -UseBasicParsing -Headers @{'User-Agent'='Mozilla/5.0'}
$c = $r.Content
$idx = $c.IndexOf('Source Code')   # 页面有 "Source Code" 区块
$seg = $c.Substring($idx, [Math]::Min(500, $c.Length - $idx))
[regex]::Match($seg, 'href="(https?://[^"]+)"').Groups[1].Value
```
页面 157KB+，Source Code 区块在 HTML 内（非 JS 渲染），正则可直接提取。优于 snapshot.ps1 的 description 解析（description 常不含链接）。

## 注意

- API 限流严格：连发约 250 请求后触发 403（约 5-30s 恢复）。脚本已内置退避重试与断点续跑
- `hot-index.json` 的 `github` 字段含少量误报（如 user-attachments 链接），克隆前人工确认
- release 成品 zip 已清理（2026-09-02）；如需解压产物请用重跑脚本下载到整合包工作区，不在此归档
