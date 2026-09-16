# meta005-fourpart-client fixture

C8 检查器回归夹具：锁定 D5 修复的反向半边。

- 形态：客户端 mod，`<Version>1.3.4.5</Version>`（四段式，非 semver）
- 预期：exit 1；`STD-META-005` FAIL（放宽只覆盖 semver 后缀，四段式仍拒绝）
- 用途：与 `meta005-prerelease-client` 成对，锁死 META-005 的上下界
