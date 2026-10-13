# 01: 编译基线：全量移植 + 4.1.6 适配

**What to build:** 在 toolkit 仓库 `mods/InescapableTarkovsSoftcore416/` 建立本项目，把移植源（5.0 实现）的程序集、数据表与配置模板全量移植，并完成 SPT 4.1.6 适配（可空性修正 + 工程/元数据改写），使 `dotnet build -c Release` 通过并产出 `InescapableTarkovsSoftcore.dll`。config 模板与配置模型移除 `customBackground` 键（对应启动器背景移除项）。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] 项目结构镜像移植源（src / config / data），解决方案暂仅含主工程（测试工程属 02）
- [ ] `SptVersion` = `~4.1.6`；运行时引用指向 `SPT_41x\SPT_Runtime`（经 `SptRoot` 约定，默认 `E:\Game\EFT_Offline`）
- [ ] 全部适配点逐项落实（规格书 D7 表 1–10）：弹药堆叠 StackMaxSize 空值守卫 / 可掠夺 Unlootable 判真 / 配方时间 ProductionTime 空值守卫（4 处）/ 建设时间 ConstructionTime 空值守卫 / Skier MinSalesSum 兜底 / 跳蚤等级 MinUserLevel 取整（4.1.6 为 int）/ 藏身处 IsSpawnedInSession 判真
- [ ] `config/default-config.json` 去除 `customBackground` 键；配置模型同步删除该属性；src 范围无启动器背景残留逻辑
- [ ] 数据资源（trueitems 6 表 / armbands / backpacks / softcore 4 表 / 默认配置模板）随程序集内嵌
- [ ] `dotnet build -c Release` 通过（0 错误）；可空警告清零或显式抑制并记录
- [ ] 构建产物 `InescapableTarkovsSoftcore.dll` 生成
