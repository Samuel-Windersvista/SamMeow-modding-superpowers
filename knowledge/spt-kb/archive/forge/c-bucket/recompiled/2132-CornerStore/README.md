# 2132 WTT - Corner Store — 4.0 → 4.1.2 重编译留档

> 2026-08-10 | 服务端 C# mod（更多酒水/零食商人，依赖 WTT CommonLib）| forge 2132 / WTT - Corner Store / v1.0.0

## 产物

| 项 | 值 |
|---|---|
| 源码 | `src/`（本目录）|
| 编译 | `dotnet build -c Release` → 0 错误 0 警告 |
| DLL | `WTT-CornerStore.dll`（9,216 字节，net10.0）|
| 部署 | `E:\Game\EFT_Offline\Inescapable Tarkov\mods\[2]新酒水-WTT - CornerStore\SPT_Runtime\user\mods\WTT-CornerStore\`（含完整 mod 内容：bundles/ 113 个 + db/ 5 个 + bundles.json + 新 DLL）|

## csproj

- net10.0 + SPTarkov.Server.Core 4.1.2 + SPTarkov.DI 4.1.2 + SemanticVersioning 3.0.0
- AssemblyName=WTT-CornerStore，RootNamespace=WTT_CornerStore
- 程序集引用：`ref\WTT-ServerCommonLib.dll`（部署环境 3.0.3，`<Private>false`，运行时由 WTT-CommonLib mod 提供）

## 改动清单（4.0 → 4.1.2）

| 文件 | 变更 |
|---|---|
| ModMetadata.cs | record : AbstractModMetadata → class : IModMetadata；属性全部 init；新增 HasPrepatcher=false；SptVersion `~4.0.2` → `~4.1.0`；ModDependencies `com.wtt.commonlib: ~2.0.0` → `>=3.0.0 <4.0.0`（4.1 CommonLib 为 3.0.3）；4.0 的 IsBundleMod=true 在 IModMetadata 中不存在，已丢弃 |
| WttItemCreator.cs | `OnLoad()` → `OnLoadAsync(CancellationToken)`；Injectable 特性 4.0 三参 `(InjectionType, Type, int32)` → 4.1.2 两参（原 blob 解码：InjectionType=Transient、typeOverride=null、TypePriority=400002 → `[Injectable(InjectionType.Transient, 400002)]`）；`(Exception)null` 反编译噪声清理；命名空间与类同名冲突 → 完全限定 `WTTServerCommonLib.WTTServerCommonLib`；其余经 CommonLib 3.0.3 API（CustomItemServiceExtended.CreateCustomItems / CustomBuffService.CreateCustomBuffs）原样保留，无 DatabaseService 直连 |

## 验证

- 编译 0 错误 0 警告（clean rebuild 确认）
- 新 DLL ilspycmd 反编译确认：`ModMetadata : IModMetadata`（SptVersion `~4.1.0`、ModDependencies `com.wtt.commonlib: >=3.0.0 <4.0.0`）+ `WttItemCreator : IOnLoad`（Injectable Transient/400002、OnLoadAsync）
- 部署目录：bundles.json / bundles/ 113 / db/ 5 / WTT-CornerStore.dll 全部就位，文件数与原提取包一致（120）
