# 02: 测试移植与全绿

**What to build:** 把移植源的 xUnit 测试套件移植到目标运行时引用，并新增「可空守卫专项断言」（D7 涉及的可空字段为 null 时对应变更零发生、无异常）；`dotnet test` 全绿，差异表锚点断言全部通过。

**Blocked by:** 01（编译基线）

**Status:** ready-for-agent

- [ ] 测试工程加入解决方案；引用目标运行时 DLL（`Private=true` 以支持测试宿主反射加载）+ `FrameworkReference Microsoft.AspNetCore.App` + `Spectre.Console.Ansi` 直引
- [ ] 全部继承测试通过：配置层（默认/未知键/缺键）与 G1–G7 样例（锚点值照旧）
- [ ] 新增断言：`customBackground` 键不存在于默认模板
- [ ] 新增适配专项断言：可空字段为 null 时零变更不崩溃（覆盖 D7 各相关变更）
- [ ] `dotnet test` 全绿（0 失败）
