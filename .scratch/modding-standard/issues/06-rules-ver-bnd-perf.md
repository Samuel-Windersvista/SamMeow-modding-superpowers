# 06: 维度 ⑪-⑬ 规则：VER / BND / PERF

**What to build:** 撰写 `11-version-differences.md`（版本差异规则：4.1.5 与 5.0 的机制差异要点，引用 migration 文档与 api-notes-5.0）、`12-bundle-assets.md`（资源替换与数据库覆盖包的目录/打包约定 + Unity bundle 升级兼容陷阱——引用既有 bundle 报告，不做从零教程）、`13-perf-security.md`（性能：避免已知热点模式，引用 perf 热点报告；安全：路径遍历/输入校验，引用 4.1.5 源码审查报告）。格式要求同 ticket 02。

**Blocked by:** 01

**Status:** done

- [x] 三个维度文件完成，规则五要素齐全
- [x] BND 明确"约定+陷阱"边界，链接 `curated/migration/bundle-*.md` 与 operations 报告
- [x] PERF 引用 `3114-eft016-perf-hotspots.md` 与 `415-source-review-report.md` 的具体发现
- [x] index.json 登记本票文件

## Comments

**2026-09-14 完成（agent）**

- 交付：`11-version-differences.md`（STD-VER-002..004，3 条）、`12-bundle-assets.md`（STD-BND-001..006）、`13-perf-security.md`（STD-PERF-001..008），共 17 条规则。
- 双轴评审后修复：删除与 STD-META-004 重复的 STD-VER-001（编号空洞保留，附注指向）；补 9 个样例（BND-002/003/006、PERF-001/002/003/005/006/008）；VER-004 / PERF-007/008 补 `EV-NOCORPUS` 锚点。
- BND 链接 `curated/migration/bundle-*.md`；PERF 引用 `3114-eft016-perf-hotspots.md` 与 `415-source-review-report.md` 的具体发现。
- 机械核验：五要素齐全、ID 唯一、链接与锚点全部可解析。
