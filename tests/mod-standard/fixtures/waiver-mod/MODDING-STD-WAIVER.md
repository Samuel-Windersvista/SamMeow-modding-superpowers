# Modding Standard 豁免记录 — waiver-mod fixture

> 机检：`scripts/check-mod-standard.ps1 -ModPath tests/mod-standard/fixtures/waiver-mod -Kind server`
> 豁免格式：`Waiver: STD-XXX-NNN: <reason>`（检查器解析）。

Waiver: STD-STRUCT-006: fixture 故意不提供 LICENSE，用于回归「FAIL -> WAIVED」路径；替代方案：真实 mod 应随附授权文件。
