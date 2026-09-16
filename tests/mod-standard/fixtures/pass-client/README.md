# pass-client fixture

C8 检查器回归夹具：客户端 mod 的「全 PASS」基线。

- 形态：SPT 4.1.5 客户端（BepInEx 5 / `BaseUnityPlugin` / Harmony）
- 预期：exit 0 / FAIL 0 / SKIP 0
- 用途：`tests/mod-standard/run-fixtures.ps1` 断言客户端检查分支未漂移
