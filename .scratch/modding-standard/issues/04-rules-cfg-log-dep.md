# 04: 维度 ⑥-⑧ 规则：CFG / LOG / DEP

**What to build:** 撰写 `06-config.md`（服务端标准路径 `config/config.jsonc` + `defaultConfig.jsonc` 首启复制模式、`IOnDIConstruct` + `AddSingleton` 加载、config 类禁止 `[Injectable]`；客户端 BepInEx `Config.Bind`）、`07-logging.md`（`ISptLogger` 使用与日志级别、错误处理与降级、不吞异常）、`08-dependencies.md`（`ModDependencies` 硬语义：key=ModGuid、SemVer Range、失败即整批拒载——**标注"机制推断，无语料先例"**；服务端无软依赖机制→条件逻辑在 `IOnLoad` 自判；客户端 `[BepInDependency]` soft/hard 使用策略）。格式要求同 ticket 02。

**Blocked by:** 01

**Status:** done

- [x] 三个维度文件完成，规则五要素齐全
- [x] CFG 含完整可复制的加载样例（IOnDIConstruct 注册代码）
- [x] DEP 的 `ModDependencies` 声明样例明确标注证据等级（机制推断）
- [x] index.json 登记本票文件

## Comments

**2026-09-14 完成（agent）**

- 交付：`06-config.md`（STD-CFG-001..006）、`07-logging.md`（STD-LOG-001..005）、`08-dependencies.md`（STD-DEP-001..005），共 16 条规则。
- CFG-003 含完整可复制 `IOnDIConstruct` 加载样例；DEP-001 声明样例标注「机制推断，无语料先例」。
- 双轴评审后修复：CFG-004 / LOG-004 / LOG-005 补 `EV-NOCORPUS` 锚点；交叉引用补全。
- 机械核验：五要素齐全、ID 唯一、链接与锚点全部可解析。
