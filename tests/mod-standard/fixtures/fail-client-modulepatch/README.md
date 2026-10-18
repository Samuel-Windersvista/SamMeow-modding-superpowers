# fail-client-modulepatch fixture

C8 检查器回归夹具：证明 `ModulePatch` / `.Enable()` 被识别为 Harmony 使用。

- 形态：SPT 4.1.5 客户端（BepInEx 5），补丁经 `ModulePatch` 显式 `Enable()`，**有意省略撤销**。
- 预期：exit 1 / FAIL = {`STD-CLI-007`}
- 用途：旧检查器把该形态判为 N/A（PASS）；修复后须落到实际判定并因缺撤销而 FAIL。
