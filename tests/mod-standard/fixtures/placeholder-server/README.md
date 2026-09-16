# placeholder-server fixture

C8 检查器回归夹具：未实例化的服务端模板（脚手架占位符仍在）。

- 形态：`{{MOD_VERSION}}` / `{{MOD_GUID}}` 未替换
- 预期：exit 0；`STD-META-005` / `STD-META-003` / `STD-VER-002` 报 SKIP（取值形状类检查降级），其余 PASS
- 用途：锁定「占位符 -> SKIP」的降级语义
