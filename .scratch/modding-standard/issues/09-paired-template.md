# 09: 新增 paired 模板

**What to build:** 新建 `templates/paired-mod`：`Client/` + `Server/` + `Shared/`（+ 可选 `Fika/`）布局、单 `.sln` 统管、版本号联动（集中属性定义）、打包脚本约定（单 zip 同时含 `BepInEx/plugins/<Mod>/` 与 `SPT_Runtime/user/mods/<Mod>/`；服务端目录唯一 `IModMetadata`）。对齐 ticket 08 的模板惯例与规则引用注释。S1 验证：`dotnet build` 通过。

**Blocked by:** 08

**Status:** ready-for-agent

- [ ] paired 模板结构完整（Client/Server/Shared + sln + 打包脚本）
- [ ] 版本号联动机制就位（两端同版本）
- [ ] 构建通过（S1 验证，记录在票内 Comments）
- [ ] 规则引用注释与既有模板一致
