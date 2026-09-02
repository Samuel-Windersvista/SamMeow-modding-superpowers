# tarkov.dev REST dump 快照（批次 0）

## 抓取信息

- **抓取时间**：2026-09-02
- **快照目录**：`knowledge/spt-kb/archive/tarkov-dev/snapshot-2026-09-02/`
- **抓取脚本**：`knowledge/spt-kb/archive/tarkov-dev/fetch-dump.ps1`（参数 `-OutputDir` 指定落盘目录，`-Force` 覆盖已存在文件；18 个端点全部抓取成功，共 32.7 MB）
- **画像报告**：`snapshot-2026-09-02/IMAGE-REPORT.md`（数据画像 + SPT 本地数据库抽样对照）

## 数据源

- **通道**：https://json.tarkov.dev —— tarkov.dev 的 REST dump 通道（`regular/*` 端点），实测可用。
- **背景**：tarkov.dev 的 GraphQL 端点（api.tarkov.dev/graphql）因 [Issue #474](https://github.com/the-hideout/tarkov-dev/issues/474) 故障已不可用约 6 周，故改用 REST dump 通道抓取全量快照。
- **存档**：`endpoints.json` 为 tarkov.dev 端点清单（抓取时状态存档）。

## 版本对应关系（重要）

- **EFT live**：1.1.0.1.46911（tarkov-changes.com 2026-08-24 记录）
- **SPT**：4.1.2，锁定客户端版本 0.16.9.5.40743

> 本快照为 **live 参考数据**，并非 SPT 事实。live 与 SPT 锁定版本之间存在内容差异（如 live 1.1.0 新增弹药/地图 SPT 0.16.9.5 未收录），使用时务必以 SPT 本地数据库为准，tarkov.dev 数据仅用于对照、补全与趋势分析。

## 数据来源链

tarkov-changes.com 数据挖掘 + Fandom wiki + 社区仓库 + 跳蚤市场扫描器（tarkov.dev 官方说明）。

## 许可

- 仓库代码：GPLv3（注：tarkov.dev 的 package.json 中许可证字段写的是 ISC，与仓库声明不一致，使用时以仓库 LICENSE 为准）。
- 数据引用：需保留来源署名（tarkov.dev / json.tarkov.dev）。

## 已知缺口

- [Issue #481](https://github.com/the-hideout/tarkov-dev/issues/481)：商人枪械报价仅 18/171（live dump 自身数据缺口，非抓取问题）。
- dump 中物品的 `name` / `shortName` / `description` 为占位 key（形如 `<itemId> Name`），**必须**用 `items_zh.json` / `items_en.json` 等翻译字典 join 才能得到可读文本。
- barters / crafts 无翻译字典（`translations: false`）。

## 排除项说明

以下端点未抓取：

| 端点 | 原因 |
|---|---|
| `prices` | live 跳蚤价格历史，波动数据，不适合作为快照参考 |
| `status` | 服务状态，非内容数据 |
| `pve` / `pvp-season` | 游戏模式数据，与 SPT 单机场景无关 |

## 快照文件清单

| 文件 | 说明 |
|---|---|
| `items.json` / `items_zh.json` / `items_en.json` | 全物品 + 中英翻译字典 |
| `tasks.json` / `tasks_zh.json` / `tasks_en.json` | tasks / questItems / achievements / prestige + 翻译 |
| `barters.json` | 兑换交易（无翻译） |
| `crafts.json` | 藏身处制作（无翻译） |
| `hideout.json` / `hideout_zh.json` / `hideout_en.json` | 藏身处设施 + 翻译 |
| `maps.json` / `maps_zh.json` / `maps_en.json` | 地图域（maps/goonReports/mobs/lootContainers/stationaryWeapons）+ 翻译 |
| `traders.json` / `traders_zh.json` / `traders_en.json` | 商人 + 翻译 |
| `endpoints.json` | 端点清单存档 |

## 复现

```powershell
powershell -ExecutionPolicy Bypass -File knowledge/spt-kb/archive/tarkov-dev/fetch-dump.ps1 -OutputDir knowledge/spt-kb/archive/tarkov-dev/snapshot-YYYY-MM-DD
```
