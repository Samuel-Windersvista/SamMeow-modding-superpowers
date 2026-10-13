# 03: 构建脚本与 overlay 组装

**What to build:** `scripts/build.ps1` 幂等产出 `build/overlay/`：`SPT_Runtime\user\mods\com.sammeow.inescapable-softcore\{DLL, config.json}`。config.json 为 `config/default-config.json` 的逐字节拷贝；本期无任何静态件（启动器背景已移除，脚本不得含相关逻辑）。

**Blocked by:** 01（编译基线）

**Status:** ready-for-agent

- [ ] 每次运行重建 `build/overlay/`；DLL 为 Release 构建产物
- [ ] `config.json` 与模板逐字节一致
- [ ] 产物相对路径与部署映射一致（可由 MO2 overlay 直接投影）
- [ ] 脚本中文注释、UTF-8 with BOM；重复运行幂等
- [ ] 脚本在干净工作树（无 bin/obj）下可一键成功
