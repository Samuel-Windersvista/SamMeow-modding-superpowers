# 01: Validator 维度数数据驱动（预重构）

**What to build:** 让规则集校验器从注册表的 domain/章节数据推导期望维度数，而不是把 13 硬编码在校验逻辑里。完成后，新增 `MP` / `LOC` 维度只需改数据，不需要再改校验器。现有 13 个维度必须保持全绿。

**Blocked by:** None (can start immediately)

**Status:** done

- [x] 校验器不再依赖硬编码的 13；期望维度数来自注册表/章节数据的单一来源。
- [x] 现有 13 个维度的 `validate-mod-standard.ps1` 仍 exit 0。
- [x] 用一个临时夹具证明：增加一个 domain/章节后，校验器无需改代码即可识别新维度数。
- [x] `run-fixtures.ps1` 与 bootstrap 验证仍全绿。
- [x] 不修改任何规则 ID、规则 prose、Level 或 Applies。

**执行说明：** 这是预重构票，先于 MP/LOC 维度票；不与其他票共享写文件，可独立完成。依据 spec 的 D11。

## Comments

- 2026-10-18：已实现并验收。validator / 维度夹具 / 7 项 fixture / bootstrap 4 段全部 exit 0；临时目录清理成功。Standards review 的 3 项 judgement call 已修（对象序列化替代字符串拼接、try/finally 清理、PathType + null domains 守卫、回归测试接入 bootstrap）。未 git commit。

