# waiver-mod fixture

C8 检查器回归夹具：豁免路径（FAIL -> WAIVED）。

- 形态：服务端 mod，故意不提供 `LICENSE`（`STD-STRUCT-006` FAIL），由 `MODDING-STD-WAIVER.md` 豁免
- 预期：exit 0；`STD-STRUCT-006` 报 WAIVED，FAIL 集合为空
- 用途：锁定「未豁免 FAIL 才 exit 1」的语义
