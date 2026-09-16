# tests/mod-standard

Modding Standard 检查器（`scripts/check-mod-standard.ps1`）的夹具回归套件。

## 运行

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tests/mod-standard/run-fixtures.ps1
```

## 断言什么

每个夹具逐条断言五类事实（任一不满足即 exit 1）：

1. 检查器进程 exit 码；
2. 输出 ID 序列 == `rules.json` 中「该 kind 适用」的可检规则发射序列（顺序 + 集合）；
3. 汇总行 `checks: PASS=.. FAIL=.. SKIP=.. WAIVED=..` 存在、计数和 == 逐条输出条数、`FAIL` 计数 == 预期 FAIL 集合大小（防重复发射/漏报）；
4. `FAIL` ID 集合精确相等（不能多、不能少）；
5. 关键规则的逐条状态（`PASS` / `FAIL` / `SKIP` / `WAIVED`）。

## 夹具

| 夹具 | kind | 形态 | 预期 |
|------|------|------|------|
| `pass-server` | server | 4.1.5 服务端全合规 | exit 0 / FAIL 0 / SKIP 0 |
| `fail-server` | server | 故意缺 README、LICENSE、config 目录，`net9.0`、两段式版本、`Console.Write`、裸 `TypePriority` | exit 1 / FAIL 10 条 |
| `pass-client` | client | 4.1.5 客户端全合规（BepInEx 5 + Harmony） | exit 0 / FAIL 0 / SKIP 0 |
| `placeholder-server` | server | 脚手架占位符未替换 | exit 0 / `META-005`、`META-003`、`VER-002` 报 SKIP |
| `waiver-mod` | server | 缺 LICENSE，由 `MODDING-STD-WAIVER.md` 豁免 | exit 0 / `STRUCT-006` 报 WAIVED |
| `meta005-prerelease-client` | client | `<Version>1.3.4-spt5.1</Version>` | exit 0 / `META-005` PASS（D5 修复上界） |
| `meta005-fourpart-client` | client | `<Version>1.3.4.5</Version>`（四段式） | exit 1 / `META-005` FAIL（D5 修复下界） |

夹具是**纯文本**的最小树（不参与编译），只用于驱动检查器的正则/取值判定。
`fail-server` 没有 `README.md` 是有意为之（`STD-STRUCT-005` 需要 FAIL），其预期写在
`run-fixtures.ps1` 的夹具表与本表中。

## 与 bootstrap 的关系

`tests/bootstrap/verify-standard-compliance.ps1` 调用本套件，作为其「夹具」段；
bootstrap 总数不变（10 项）。
