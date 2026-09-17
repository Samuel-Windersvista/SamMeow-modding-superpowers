# ExpandedTaskText（ett）迁移试点 — STATUS

> 本文件是 C9「pilots 退出机制」（决策 D4）的状态标记，随试点保留原位；
> 归档动作待实机验证完成后执行（见下「归档去向」）。

## 状态（完成度）

**迁移实现已落地，编译通过；属可行性验证试点，非交付 mod。**

| 项 | 事实 |
|---|---|
| 类型 | 服务端 mod（net10.0），3.11 TS -> 4.1 C# 迁移试点 |
| 版本 | v1.6.4（`ExpandedTaskText.csproj`） |
| 源码 | `src/ModEntry.cs`（316 行）+ `src/ModMetadata.cs` |
| 资产 | `config/config.json`、`db/GunsmithLocaleEN.json`、`db/QuestInfo.json`（3.11 原样搬运） |
| 上游来源 | `knowledge/spt-kb/archive/forge/mods/Expanded-Task-Text_2153_source/src/`（mod.ts 331 行） |
| 试点定位 | `docs/wayfinder/findings/005-auto-ts-to-csharp-conversion.md` §1.1 的实测样本（暴露 3.11 全部常见模式） |

## Owner

Samuel-Windersvista（Overseer）

## 验证状态

**无实机验证记录。** 未发现 dbdump、服务端启动加载或进 raid 的任何验证记录。

已知核心 blocker（记录于 `docs/wayfinder/findings/005-auto-ts-to-csharp-conversion.md` §3 阻断器 1）：
4.1 `LocaleService` 只有 getter（GetLocaleDb / TryGetLocaleDb / GetDesiredGameLocale），
**没有 setter / 没有保存本地化的入口**——而本 mod 的核心正是改写任务文本本地化。
该 blocker 属平台能力问题，不是迁移工作量问题。

## 关闭条件

1. 4.1 locale 写入方案定论（LocaleService setter 缺失 blocker 解除，或确认不可行并据此关闭试点）。
2. 服务端加载验证：启动 SPT server，日志确认 mod 注册成功。
3. 实机进 raid 确认任务文本按配置项（Collector / LightKeeper / Gunsmith 等）正确显示。

## 归档去向

`examples/`（待上述验证完成后执行；本单不移动文件）。

---

> 相关：`docs/wayfinder/findings/005-auto-ts-to-csharp-conversion.md`、`knowledge/spt-kb/curated/migration/`。
