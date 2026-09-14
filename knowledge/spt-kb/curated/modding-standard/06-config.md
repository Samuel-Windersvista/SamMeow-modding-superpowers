---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 06 配置系统（CFG）

> **Domain slug:** `CFG` · **规则 ID 前缀:** `STD-CFG-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：骨架。规则由 ticket 04（`.scratch/modding-standard/issues/04-rules-cfg-log-dep.md`）填充。

## 维度范围

- 服务端标准路径 `config/config.jsonc` + `defaultConfig.jsonc` 首启复制模式
- `IOnDIConstruct` + `AddSingleton` 加载
- 配置类禁止 `[Injectable]`
- 客户端 BepInEx `Config.Bind`

## 规则

<!-- 规则条目在此填写。五要素：ID 标题（`### STD-CFG-nnn — 标题`）/ Level（MUST|SHOULD|MAY）/ Applies（4.1.5|5.0|both）/ Evidence（引用 [evidence-index.md](evidence-index.md) 的 EV-* 锚点）/ 内联样例与深入链接。格式见 README.md「规则条目格式」。 -->
