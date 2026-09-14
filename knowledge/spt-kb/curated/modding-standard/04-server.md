---
version: [4.1, 5.0]
domain: server
topic: modding-standard
source: curated
---

# 04 服务端机制（SRV）

> **Domain slug:** `SRV` · **规则 ID 前缀:** `STD-SRV-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：骨架。规则由 ticket 03（`.scratch/modding-standard/issues/03-rules-srv-cli.md`）填充。

## 维度范围

- DI 注册：`[Injectable(TypePriority = OnLoadOrder.X + n)]`，禁止裸数字
- 生命周期：`IOnLoad` / `IOnUpdate`
- 路由注册：`StaticRouter` / `DynamicRouter`
- Callbacks 与 `ISptLogger<T>` 注入

## 规则

<!-- 规则条目在此填写。五要素：ID 标题（`### STD-SRV-nnn — 标题`）/ Level（MUST|SHOULD|MAY）/ Applies（4.1.5|5.0|both）/ Evidence（引用 [evidence-index.md](evidence-index.md) 的 EV-* 锚点）/ 内联样例与深入链接。格式见 README.md「规则条目格式」。 -->
