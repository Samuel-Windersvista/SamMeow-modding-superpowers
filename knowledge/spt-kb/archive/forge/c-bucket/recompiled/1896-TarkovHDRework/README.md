# 1896 - Tarkov HD Item Rework（重编译 4.1.2）

SPT 4.0 服务端 mod 重编译为 SPT 4.1.2 兼容。高清材质 mod（bundle 替换，47 个 bundle 资源 + bundles.json manifest）。

## 来源

- 原始 DLL: `c-bucket-extracted\1896\SPT\user\mods\TarkovHDRework\TarkovHDRework.dll`（13,312 字节，SPT 4.0.1 编译）
- 类型: `tarkovhdrework`（ModMetadata / TarkovHDRework / NewLocales）

## 编译环境

- TargetFramework: net10.0（SPTarkov.Server.Core 4.1.2 为 net10.0）
- 引用: SPTarkov.Server.Core 4.1.2 / SPTarkov.DI 4.1.2 / SemanticVersioning 3.0.0
- 产物: TarkovHDRework.dll 13,312 字节，0 警告 0 错误

## 改动清单（4.0 -> 4.1.2）

| 文件 | 改动 |
|---|---|
| ModMetadata.cs | `: AbstractModMetadata` -> `: IModMetadata`；去掉 `override`；新增必填 `HasPrepatcher`；删除 4.1 接口中不存在的 `IsBundleMod`；`SptVersion` `~4.0` -> `~4.1.0`；4 必填非空（ModGuid/Name/Author/License）全部赋值 |
| TarkovHDRework.cs | `OnLoad()` -> `OnLoadAsync(CancellationToken)`（4.1.2 `IOnLoad` 新签名，命名空间 `SPTarkov.Server.Core.DI`）；`DatabaseService.GetTables()/GetItems()` -> 表模型 `TemplateTable`（`templateTable.Items`）；`ModHelper` using -> `SPTarkov.Server.Core.Helpers.Server`；`ISptLogger` using -> `SPTarkov.Common.Models.Logging`；`Injectable` 去掉 4.0 的 `null, int.MaxValue` 中间参数（4.1.2 只接受 `(InjectionType, TypePriority)`）；`Path` 全限定 `System.IO.Path`（4.1.2 新增 `Tables.Path` 类型造成二义性）；移除从未使用的 `LocaleService`/`ServerLocalisationService` 注入 |
| NewLocales.cs | 死代码类（无 Injectable，未被加载），仅为编译通过适配：`DatabaseService.GetLocales()` -> `LocaleTable`；`LazyLoad<Dictionary<string,string>>` -> `LazyLoad<GlobalLocaleDictionary>`；`GetText("x")` -> `GetText("x", (object?)null)`（4.1.2 无单参重载） |
| AssemblyInfo.cs | 未改动（保留原反编译产物） |

## 部署

- 目标: `E:\Game\EFT_Offline\Inescapable Tarkov\mods\[2]高清材质-TarkovHDRework\SPT_Runtime\user\mods\TarkovHDRework\`
- 已替换: TarkovHDRework.dll / .deps.json（net9.0+4.0.1 -> net10.0+4.1.2）/ .pdb
- 未动: bundles/、db/、bundles.json、staticwebassets.endpoints.json

## 验证

- 编译: 0 错误 0 警告
- 部署 DLL 反编译回读确认: `IModMetadata`、`OnLoadAsync(CancellationToken)`、`HasPrepatcher`、`SptVersion ~4.1.0`
- 运行时验证: 启动 SPT 4.1.2 服务端后应出现 "Mod loaded after database!" + "Asset replacement complete!" 日志
