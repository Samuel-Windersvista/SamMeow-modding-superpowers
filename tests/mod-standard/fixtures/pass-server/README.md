# pass-server fixture

C8 检查器回归夹具：服务端 mod 的「全 PASS」基线。

- 形态：SPT 4.1.5 服务端（`IModMetadata` + DI + `ISptLogger<T>`）
- 预期：exit 0 / FAIL 0 / SKIP 0
- 用途：`tests/mod-standard/run-fixtures.ps1` 断言检查器输出未漂移
