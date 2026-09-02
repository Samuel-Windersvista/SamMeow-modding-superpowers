---
version: [通用]
domain: both
topic: lore
source: curated
---

# 塔科夫术语表

> 状态：2026-08-16 提炼 | 版本标签：[通用]
> 来源：Fandom Wiki（Scavs 等）、eftarkov.com、Wikipedia

## 游戏机制术语

| 术语 | 说明 |
|------|------|
| Raid（战局） | 一局游戏：进入地图搜刮/任务/交战，20-50 分钟，需在时限内撤离，否则算 MIA/死亡 |
| PMC | 玩家扮演的雇佣兵（BEAR 或 USEC），本命角色，死亡损失装备 |
| Scav（拾荒者） | 可扮演的 AI 阵营角色；冷却计时后可免费进场，死亡不掉本体物资，战利品可带回仓库 |
| Scav karma / Fence 声望 | Scav 行为的善恶值体系：杀 PMC 加分、杀 Scav 扣分；影响车辆撤离费、BTR 服务、Fence 售价、AI 态度与任务解锁 |
| Boss | 地图头目（Reshala/Shturman/Killa/Kaban/Kollontay/Tagilla 等），高血量高装备，带护卫 |
| 三狗（Goons） | Knight/Big Pipe/Birdeye 三人组 Boss 的社区俗称，刷新于森林西北天线区、海关 Scav 基地等 |
| Cultists（邪教徒） | 夜间活动的神秘教团，毒匕首偷袭；"降临的见证者" |
| Rogues（游荡者） | 前 USEC 盘踞灯塔水处理厂，重火力据守 |
| Scav Raiders | 军事化 Scav，活动于实验室/储备站 |
| Tagged and Cursed | 系统标记：出生即被 AI 优先追猎（常见于资源受限/裸装局） |
| Kappa 容器 | 最大安全箱（3×4），收藏家任务线奖励；"收藏家 3*4 安全箱" |

## 经济与成长术语

| 术语 | 说明 |
|------|------|
| 删档（Wipe） | PVP 赛季制清空所有进度（等级/仓库/任务），每 4-8 个月一次；PVE 永不删档 |
| 赛季服 / BattlePass | 1.0 时代运营模式：赛季专属任务（17 个）与战斗通行证（8 类文档：蓝图/财务/医疗/PMC 档案/项目/技术/测试/员工） |
| 转生（Prestige） | 满级后重置进度换取特殊奖励的机制（1.0 加入） |
| 藏身处（Hideout） | 玩家基地：升级模块（情报中心、太阳能、工作台、医疗站等）解锁功能与制作 |
| 情报中心 | 藏身处模块：Scav 任务、Kerman 线剧情、CD 缩短 |
| 跳蚤市场（Flea Market） | 玩家间交易市场（SPT 中可配置） |
| 保险（Insurance） | 死亡后由商人送回未丢失装备的机制（实验室/迷宫保险失效） |
| BTR | 付费载具运输（司机撤离/跨图移动） |
| V-Ex | 付费车辆撤离点（每人 5000 卢布，最多 4 人） |

## 世界观术语

| 术语 | 说明 |
|------|------|
| Norvinsk 特别经济区 | 俄罗斯西北部虚拟经济特区，冲突发生地 |
| TerraGroup | 跨国控股集团（反派），非法研究与黑料核心 |
| TerraGroup Labs PLC | 其英国子公司，官方农业生物技术、实际非法研究 |
| 黑师（Black Division） | TerraGroup 内部精锐特种部队 |
| UNTAR | 联合国驻塔科夫维和部队（蓝盔） |
| RUAF | 俄罗斯武装部队（封锁执行方） |
| 无名者（The Nameless Ones） | 在 TerraGroup 文件上留言的神秘组织，"净化"计划相关 |
| Kerman | 门票线的神秘委托人，驻扎灯塔 |
| 守望者（Warden） | 无名者线提到的组织 |
| 净化（Purification） | TerraGroup 相关秘密计划，涉及蓝冰燃料催化剂 |
| 蓝冰 | "净化"用的燃料催化剂（神秘蓝焰章） |
| Zryachiy | 守卫灯塔桥的 Lightkeeper 亲信（狙击手） |
| Firefly 行动 | Lightkeeper 参与的 USEC 行动（Icebreaker 档案） |
| Knossos LLC | 建造迷宫"主题公园"的 TerraGroup 承包商 |
| Paradigm 航运 | 破冰船 Boreas 相关的航运公司（海报开启 Boreas 线） |
| Russia-2028 宇宙 | 塔科夫与 Contract Wars/Hired Ops 共享的架空世界观 |
| 转场（Transit） | 1.0 起的跨图机制（不经撤离进入相邻地图） |

## 常用物品/机制简称

| 简称 | 全称/说明 |
|------|----------|
| DSP 发射器 | 编码版 Digital secure DSP radio transmitter（Lightkeeper 通行证） |
| 门禁卡/钥匙卡 | 实验室黑/蓝/绿/红/紫/黄卡、Labrys 卡、克鲁格洛夫钥匙卡等 |
| RSP-30 | 反应式信号弹（黄/绿），撤离与剧情信号 |
| Kappa | 见上（最大安全箱） |
| xTG-12 / Perfotoran | 毒素解毒剂（对 Cultists 毒刃） |
| 绿信号棒 | 信号弹撤离标记（Boreas 线撤离条件） |

## 对 SPT 环境的意义

- 上述术语大多有对应数据库对象（`database/templates`、`database/locations`、`database/quests`），写 mod 时可用本表做术语对照。
- 社区 mod 命名常用这些术语（如 LootingBots、SAIN 的 bot 类型），理解本表可避免误配置。

## 来源与追溯

| 内容 | 来源 |
|------|------|
| Scav/karma/Boss 体系 | Fandom: Scavs / Customs / Woods |
| 赛季/BattlePass/转生 | eftarkov.com（首页导航 + 删档历史） |
| 世界观术语 | Fandom + eftarkov.com 剧情页 |
