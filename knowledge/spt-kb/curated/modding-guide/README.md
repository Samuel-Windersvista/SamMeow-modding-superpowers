---
version: [4.1]
domain: both
topic: index
source: curated
---
# SPT Mod 开发指南（重组版）

> 状态：骨架 | 目标版本：[4.1] | 本目录按「开发流程顺序」组织，而非 wiki 的页面结构

本指南把 wiki、官方示例、源码笔记串成一条「从零写出一个 SPT 4.1 mod」的路径。

## 章节规划

1. **环境与工具链** — .NET SDK、IDE、dnSpy、引用 SPT 程序集的方法
   - 素材：`wiki/modding/Modding_Resources.md`、`wiki/modding/tutorials/debug_dnSpy.md`
2. **Server Mod 解剖** — 目录结构、mod.json、DI 注册、生命周期钩子
   - 素材：`E-Mod开发示例/server-mod-examples/`、`../api-notes-4.1/`
3. **Client Mod 解剖** — BepInEx 插件结构、Patch 机制、与 server mod 的分工
   - 素材：`wiki/modding/tutorials/Client_Modding_Quick_Guide.md`、`A-核心服务端/modules/`
4. **数据驱动的 mod** — 改数据库（物品/商人/任务）而不写代码的边界在哪
5. **调试与排障** — 日志位置、常见崩溃模式、50/50 法在开发期的应用
6. **打包与发布** — 目录约定、版本兼容声明（SPT 约束格式）
   - 素材：`F-数据与工具/forge/` 的 mod 元数据结构

## 写作规范

- 每章开头标注版本标签与资料来源清单
- 代码示例优先引用 `server-mod-examples` 的真实文件路径，其次才写新示例
- 与 wiki 原文有出入时以 4.1 源码为准，并在文中注明差异
