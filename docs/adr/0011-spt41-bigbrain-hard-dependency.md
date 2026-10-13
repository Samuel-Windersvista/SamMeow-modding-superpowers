# 4.1.6 线编入 BigBrain 两模式并硬依赖 BigBrain 1.5.0

5.0 线不编译 BotDebug 的 BigBrainLayer / BigBrainLogic 模式（BigBrain 无 5.0 分支）。4.1.6 线上游 BigBrain 1.5.0 可用，且 Norvinsk_Forever 整合包已内置（AI 模组前置依赖），故本线裁决：两模式编入并声明硬依赖 `xyz.drakia.bigbrain 1.5.0`（镜像上游 BotDebug 默认构建形态）；`STANDALONE` 条件编译符号保留为逃生门（无 BigBrain 场景可另行构建，本任务不产出该变体）。

## Considered Options

- 软依赖（运行时探测 BigBrain，缺失时隐藏两模式）：插件无 BigBrain 也可加载，但非上游形态，实现与验证复杂度更高。
- 不编入（同 5.0 线处理）：功能面缩水，与"完整替代 4.1.6 包内 BotDebug"的目标矛盾。

## Consequences

- 未安装 BigBrain 时本插件整体不加载（BepInEx 依赖硬门）——与上游 BotDebug 行为一致；目标整合包不受影响。
- BigBrain 版本升级时需复核其 `DrakiaXYZ.BigBrain.Brains` 程序集 API（BrainManager / CustomLayer / CustomLogic 等）。
