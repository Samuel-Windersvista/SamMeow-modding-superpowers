# 1688 HoodsEnergyDrinks — 4.0 → 4.1.2 重编译留档

> 2026-08-09 | 服务端 C# mod（更多能量饮料商人 + 战利品）| forge 1688 / slug more-energy-drinks / v1.2.0

## 产物

| 项 | 值 |
|---|---|
| 源码 | `src/`（本目录）|
| 编译 | `dotnet build -c Release` → 0 错误 0 警告 |
| DLL | `HoodsEnergyDrinks-CSharp.dll`（28,672 字节，net10.0）|
| 部署 | `E:\Game\EFT_Offline\Inescapable Tarkov\mods\[2]新酒水-HoodsEnergyDrinks\SPT_Runtime\user\mods\HoodsEnergyDrinks\`（含完整 mod 内容：bundles/ 76 个 + config/ + 3 个 json + 新 DLL）|

## csproj

- net10.0 + SPTarkov.Server.Core 4.1.2 + SPTarkov.DI 4.1.2 + SemanticVersioning 3.0.0
- AssemblyName=HoodsEnergyDrinks-CSharp

## 改动清单（4.0 → 4.1.2）

| 文件 | 变更 |
|---|---|
| ModMetadata.cs | record : AbstractModMetadata → class : IModMetadata；属性全部 init；新增 HasPrepatcher=false；SptVersion `~4.0.0` → `~4.1.0`；保留 Name/Author/Version/Url/License/ModGuid 原值 |
| HoodsEnergyDrinks.cs | `OnLoad()` → `OnLoadAsync(CancellationToken)`；DatabaseService/DatabaseServer 移除 → 注入 TemplateTable/LocationTable/GlobalTable/TradersTable；ConfigServer.GetConfig<RagfairConfig>() → 直接注入 RagfairConfig；MongoId.op_Implicit() 噪声清理为隐式转换；CollectionsMarshal/ref 结构噪声清理 |
| ItemCreator.cs | DatabaseServer.GetTables().Globals → GlobalTable.Configuration；NewItemFromCloneDetails 新增必填 NewItemName；Buff 命名空间 Eft.Common → Spt.Tables；(HealthFactor)3/2 → HealthFactor.Energy/Hydration |
| FluentTraderAssortCreator.cs | DatabaseService.GetTables().Traders → 注入 TradersTable；内部字典键 string → MongoId（对齐 TraderAssort.BarterScheme/LoyalLevelItems）；GetTrader() null 防御；Money 已为 MongoId 直接传 |
| TraderHelper.cs | MongoId.op_Implicit 清理；Money.ROUBLES 直接传（MongoId）|
| EnergyDrinkBuffs.cs | Buff 命名空间 Eft.Common → Spt.Tables |
| Drink/DrinkConfig/DrinkProps/Loot/StaticLoot/ModConfig | 无 API 变更，保留反编译原样 |

## 验证

- 编译 0 错误 0 警告（clean rebuild 确认）
- 新 DLL ModMetadata 反编译确认：IModMetadata + SptVersion ~4.1.0
- 部署目录：bundles.json / EnergyDrinkBuffs.json / EnergyDrinkInfo.json / config/ / bundles/ / HoodsEnergyDrinks-CSharp.dll 全部就位
