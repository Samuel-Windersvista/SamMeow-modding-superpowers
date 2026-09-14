---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 11 版本差异（VER）

> **Domain slug:** `VER` · **规则 ID 前缀:** `STD-VER-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 06，2026-09-14）。
> 注：`SptVersion` 的 tilde 区间声明规则见 `02-metadata.md` 的 `STD-META-004`（本维度不重复）。

## 维度范围

- 4.1.5 与 5.0 机制差异要点
- 引用 migration 文档与 api-notes-5.0

## 规则

### STD-VER-002 — 复用 4.1 的 mod 骨架，而非重写未被破坏的契约

- **Level:** SHOULD
- **Applies:** 5.0
- **Evidence:** 机制：`curated/api-notes-5.0/architecture-map.md`（`Models/Spt/Mod`、`Modding`、`DI` 目录在 `4.1x-dev...5.0x-dev` 之间 `git diff` 无输出）；`curated/api-notes-5.0/mod-loading.md`（`IModMetadata` 11 个属性在 4.1 与 5.0 完全相同）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** 面向 5.0 时沿用 4.1 的 `IModMetadata` / `[Injectable]` / `IOnLoad` / 路由 / 配置注入骨架，不做无依据的重写；版本差异只体现在 `SptVersion` 区间与所引用的内容数据上。

```
4.1x-dev → 5.0x-dev 无 diff：
  SPTushonka.Server/Modding
  Libraries/SPTushonka.Server.Core/Models/Spt/Mod
  Libraries/SPTushonka.DI
```

> 深入：[api-notes-5.0/architecture-map.md](../api-notes-5.0/architecture-map.md)、[api-notes-5.0/mod-loading.md](../api-notes-5.0/mod-loading.md)

### STD-VER-003 — 仅在需要 5.0 新系统时引用新表与新扩展点

- **Level:** MAY
- **Applies:** 5.0
- **Evidence:** 机制：`curated/api-notes-5.0/modding-api.md`（`SeasonTable`、`ShopTable` 为 5.0 新扩展点，「旧 mod 不强制使用」）；`curated/operations/5xx-source-verification.md`（新增战斗通行证、赛季、tarcoin 商店、结局、剧情任务链、教程系统）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** 5.0 的赛季、通行证、tarcoin 商店、结局、剧情任务链、教程等为可选扩展点；不依赖这些内容的 mod 无需改动即可在 5.0 运行。

> 深入：[api-notes-5.0/modding-api.md](../api-notes-5.0/modding-api.md)、[operations/5xx-source-verification.md](../operations/5xx-source-verification.md)

### STD-VER-004 — 面向 5.0 的客户端与服务端按新 EFT 版本重新迁移绑定

- **Level:** SHOULD
- **Applies:** 5.0
- **Evidence:** 机制：`curated/operations/5xx-source-verification.md`（`compatibleTarkovVersion` 由 `0.16.9.40743` 跳至 `1.1.5.0.47242`，客户端 `Assembly-CSharp` 类名与服务端 EFT 模型表结构都可能需新一轮迁移，原文标注「待专项评估」）；`curated/migration/client-mod-311-to-41.md`（类名映射是客户端迁移核心）；语料：无（**机制推断，无语料先例**，5.0 暂无 mod 语料）（EV-NOCORPUS）
- **Rule:** 面向 5.0 的 mod 不能假定 4.1 的客户端类名绑定与服务端 EFT 模型表沿用；按 `Class_Name_Mappings` 与 5.0 源码重新核对后再发布。

> 深入：[operations/5xx-source-verification.md](../operations/5xx-source-verification.md)、[migration/client-mod-311-to-41.md](../migration/client-mod-311-to-41.md)
