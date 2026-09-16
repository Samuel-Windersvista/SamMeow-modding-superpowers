# meta005-prerelease-client fixture

C8 检查器回归夹具：锁定 D5 修复（META-005 放宽）。

- 形态：客户端 mod，`<Version>1.3.4-spt5.1</Version>`（三段核心 + semver 预发布后缀）
- 预期：exit 0；`STD-META-005` PASS（改造前为 FAIL 的假阳性）
- 用途：防止 semver 后缀支持被回退；四段式 `1.3.4.5` 仍必须 FAIL
