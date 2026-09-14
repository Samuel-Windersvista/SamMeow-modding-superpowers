---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 08 依赖管理（DEP）

> **Domain slug:** `DEP` · **规则 ID 前缀:** `STD-DEP-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：骨架。规则由 ticket 04（`.scratch/modding-standard/issues/04-rules-cfg-log-dep.md`）填充。

## 维度范围

- `ModDependencies` 硬语义（key=ModGuid、SemVer Range、失败即整批拒载）
- 服务端无软依赖机制（条件逻辑在 `IOnLoad` 自判）
- 客户端 `[BepInDependency]` soft/hard 策略

## 规则

<!-- 规则条目在此填写。五要素：ID 标题（`### STD-DEP-nnn — 标题`）/ Level（MUST|SHOULD|MAY）/ Applies（4.1.5|5.0|both）/ Evidence（引用 [evidence-index.md](evidence-index.md) 的 EV-* 锚点）/ 内联样例与深入链接。格式见 README.md「规则条目格式」。 -->
