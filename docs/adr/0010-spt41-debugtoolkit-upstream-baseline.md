# SPT41-DebugToolkit：4.1.6 版本线以 4.x 上游源件为基准、与 5.0 线同身份独立演进

SPT 4.1.6（Mono / BepInEx 5 / netstandard2.1）需要一套 DebugToolkit。既有 5.0 线（IL2CPP / BepInEx 6 / net6.0）代码无法在 4.1.6 加载，且回迁需双重改写（拆除 IL2CPP 适配 + 把 1.1.5 特化改动重映射回 0.16.9），否决。裁决：以三个 4.x 上游源件为行为基准（BotDebug 1.8.0、DebugTooltip 5.0.0、DadGamerMode 1.9.3），套用 5.0 线的整合设计（配置面 / 生命周期 / 模块划分 / README），工程落 `mods/SPT41-DebugToolkit`，保留 5.0 线的 AssemblyName `SamMeow.DebugToolkit` 与 BepInPlugin GUID `com.sammeow.debugtoolkit`（同身份、独立源码、独立演进）。

## Considered Options

- 从 5.0 代码回迁 Mono：单一代码谱系，但需同时反向拆除 IL2CPP 适配并重映射 1.1.5 特化改动（双重改写，风险与工作量最高）。
- 独立新身份（独立 GUID / 程序集名）：隔离彻底，但同一产品跨线身份断裂（cfg 键面 / 日志前缀 / README 不再互认）。

## Consequences

- 两条版本线的功能面允许存在记录在案的差异（4.1.6 线含 BigBrain 两模式与 DadGamer QOL 全组；5.0 线无），差异须在各线 README 偏差表登记。
- 上游源件升级时需人工对账，无跨线代码共享可自动同步。
