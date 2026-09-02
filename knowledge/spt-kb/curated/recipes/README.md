---
version: [4.1]
domain: server
topic: recipe
recipe_task: index
source: curated
---
# SPT 4.1 Mod 任务配方集

> 状态：**11/11 全部完成**（2026-08-02） | 每条配方 = 一个可独立完成的具体任务
> 版本目标：[4.1] | 配方中的源码坐标基于本地 fork `SamMeow_SPT410_source_code` 实读验证

## 配方模板

```markdown
# <任务名> [版本标签]
目标：一句话说清做完得到什么
前置：需要的环境/知识
步骤：1. 2. 3.（引用真实文件路径）
验证：怎么确认成功
坑：已知陷阱
来源：wiki 页面 / 示例仓库文件 / 源码坐标
```

## 配方清单

### 数据修改类
- [x] 添加自定义商人（trader）→ `01-add-custom-trader.md`
- [x] 修改数据库数值（globals/bot/hideout/地图）→ `02-edit-database-values.md`
- [x] 添加自定义物品 → `03-add-custom-item.md`
- [x] 修改现有物品属性 → `04-edit-item-properties.md`
- [x] 修改战利品刷新（loot）→ `05-loot-tweaks.md`

### 游戏机制类
- [x] 添加自定义任务（quest）→ `06-add-custom-quest.md`
- [x] 修改 bot 难度/行为参数 → `07-bot-difficulty-tweaks.md`
- [x] 自定义商人补货逻辑 → `08-trader-restock.md`

### 系统类
- [x] 挂自定义 HTTP 路由 → `09-custom-http-routes.md`
- [x] mod 间通信 → `10-mod-communication.md`
- [x] mod 配置界面（4.1 Mod Web Pages）→ `11-mod-web-config-ui.md`

## 素材来源

- `E-Mod开发示例/server-mod-examples/` — 多数配方有现成示例可提炼
- 4.1 源码 `Libraries/SPTarkov.Server.Core/` — 配方 05-08/10 的字段与 API 已实读验证
- `wiki/modding/tutorials/WTT_Vol1.md` — 社区教程
- `../api-notes-4.1/` — 源码级细节

## 建议的读法

1. 先读 `../modding-guide/02-server-mod-anatomy.md`（4.1 机制总览）
2. 按任务查配方；写码前用配方里的「来源」对照原文与源码
3. 配方 10 的「待核实」项在下次读源码时销账
