# 04: 维度 ⑥-⑧ 规则：CFG / LOG / DEP

**What to build:** 撰写 `06-config.md`（服务端标准路径 `config/config.jsonc` + `defaultConfig.jsonc` 首启复制模式、`IOnDIConstruct` + `AddSingleton` 加载、config 类禁止 `[Injectable]`；客户端 BepInEx `Config.Bind`）、`07-logging.md`（`ISptLogger` 使用与日志级别、错误处理与降级、不吞异常）、`08-dependencies.md`（`ModDependencies` 硬语义：key=ModGuid、SemVer Range、失败即整批拒载——**标注"机制推断，无语料先例"**；服务端无软依赖机制→条件逻辑在 `IOnLoad` 自判；客户端 `[BepInDependency]` soft/hard 使用策略）。格式要求同 ticket 02。

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] 三个维度文件完成，规则五要素齐全
- [ ] CFG 含完整可复制的加载样例（IOnDIConstruct 注册代码）
- [ ] DEP 的 `ModDependencies` 声明样例明确标注证据等级（机制推断）
- [ ] index.json 登记本票文件
