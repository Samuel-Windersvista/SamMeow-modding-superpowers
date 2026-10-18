# pass-client-modulepatch fixture

C8 检查器回归夹具：客户端 mod 以 `ModulePatch` / `.Enable()` 主导形态应用补丁。

- 形态：SPT 4.1.5 客户端（BepInEx 5），补丁经 `ModulePatch.Enable()` 显式应用、`OnDestroy` 撤销。
- 预期：exit 0 / FAIL 0
- 用途：证明 `.Enable()` 被识别为补丁应用（`PatchAll()` 之外的受支持形态），且模块整体 PASS。
