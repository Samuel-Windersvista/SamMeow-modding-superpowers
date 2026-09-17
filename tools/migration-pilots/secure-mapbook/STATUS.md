# SecureMapbookMod 迁移试点 — STATUS

> 本文件是 C9「pilots 退出机制」（决策 D4）的状态标记，随试点保留原位；
> 归档动作待实机验证完成后执行（见下「归档去向」）。

## 状态（完成度）

**端到端迁移试点已完成（含 bundle 资产链路）。**

| 项 | 事实 |
|---|---|
| 类型 | 服务端 mod（net10.0）+ 物品 bundle，3.11 TS -> 4.1 C# 迁移试点 |
| 版本 | v1.0.0（`SecureMapbookMod.csproj`） |
| 源码 | `src/ModEntry.cs`（304 行）+ `src/ModMetadata.cs` |
| 资产 | `config/config.json`、`bundles.json`、`bundles/assets/content/items/barter/item_mapbook/mapbook.bundle` |
| 试点结论 | `knowledge/spt-kb/curated/migration/pilot-experience-secure-mapbook.md`（状态：已验证，2026-08-05） |

## Owner

Samuel-Windersvista（Overseer）

## 验证状态

**无实机验证记录。**

已有验证止于服务端侧（`pilot-experience-secure-mapbook.md`）：
编译 0 错误 0 警告、模组注册成功、bundle 注册 0 错误、dbdump 确认 mapbook 物品
（`621a2e3a8d8b1a9f0e3b4c5`）创建成功并含槽位 filters、bundle（4.94 MB / Unity 2022.3.43f1）零改动加载。

**未做**：进 raid 实测 mapbook 的实际使用行为（商人购买 / 保险 / 安全箱与特殊槽位规则）。

## 关闭条件

1. 实机进 raid 验证：mapbook 物品可按 config 规则购买、可放入 secure containers / 特殊槽位、
   保险规则与 `allowInsurance` 配置一致。
2. 行为与 3.11 原版一致（无回归）。

## 归档去向

`examples/`（待上述验证完成后执行；本单不移动文件）。

---

> 相关：`knowledge/spt-kb/curated/migration/pilot-experience-secure-mapbook.md`、
> `knowledge/spt-kb/curated/migration/bundle-compat-311-to-41.md`。
