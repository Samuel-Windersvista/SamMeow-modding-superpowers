# Door Breach Overhaul Server（破门大修服务端）

`DoorBreachOverhaul（破门大修）`的 **C4 服务端组件**，用于 SPT 4.1.6。以 4.1 原生 API
（`CustomItemService` + 表注入）实现，不使用任何第三方服务端库（不依赖 WTT Server Common Lib）。

目标是让 C4 炸药在游戏内可获取：服务端注册 C4 物品模板（含手册/跳蚤价与 locale），并把它的
**工作台配方**追加到 `hideout.production.recipes`，玩家在工作台 2 级即可合成。

> 配置系统不在本组件范围内：配置已本地化到客户端 BepInEx（见客户端仓库
> `docs/adr/0002-local-config-carrier.md`），本服务端不做 HTTP 配置端点。

## 身份

| 字段 | 值 |
|---|---|
| ModGuid | `com.sammeow.doorbreach.server` |
| Name | `Door Breach Overhaul Server（破门大修服务端）` |
| Version | `0.1.0` |
| SptVersion | `~4.1.0` |
| License | MIT |
| 依赖 | 无（ModDependencies = null） |

## 目录结构

```
SamMeow-DoorBreachOverhaulServer/
├── SamMeow.DoorBreachOverhaulServer.csproj
├── bundles.json                     # 4.1 bundle 清单（BundleLoader 自动扫描）
├── bundles/
│   └── C4Explosive.bundle           # C4 模型/图标资源（来自上游发行物，SHA256 见 THIRD-PARTY-NOTICES）
├── db/
│   ├── customitems/c4explosive.jsonc # C4 的 TemplateItem 数据（ID = 6636606320e842b50084e51a）
│   └── hideout/c4explosive_recipe.jsonc # 工作台配方（endProduct = C4 ID）
├── src/
│   ├── ModMetadata.cs               # IModMetadata（mod 身份与版本）
│   ├── ModEntry.cs                  # 入口：[Injectable(TypePriority = OnLoadOrder.Preload + 1)] + IOnLoad
│   └── Services/
│       └── C4RegistrationService.cs # 物品 + 配方注册（fail-fast 契约校验）
├── README.md
├── LICENSE
└── THIRD-PARTY-NOTICES.md
```

## 4.1 数据契约（反编译 `SPTarkov.Server.Core` 4.1.6 实测）

- `CustomItemService.CreateItem(NewItemDetails newItemDetails, Assembly? callingAssembly = null)`：
  直接提交一个完整的 `TemplateItem`（`NewItemDetails.NewItem`），服务负责写入物品表、locale、
  手册价（`AddToHandbook` + `HandbookParentId` + `HandbookPriceRoubles`）与跳蚤价
  （`AddToFleaPriceDb` + `FleaPriceRoubles`，两者为 true 时价格必须 > 0，否则抛异常）。
- **4.1 无 `db/CustomItems/*.json` 自动加载器**：物品必须由 mod 代码显式提交，故本工程把数据放
  在 `db/` 下、由 `C4RegistrationService` 读取后调用 `CreateItem`。
- **bundle 由 `BundleLoader` 独立处理**：服务器启动时扫描每个 mod 根目录的 `bundles.json`，
  按 `key` 到 `bundles/<key>` 找文件、算 CRC32，经 `/singleplayer/bundles` 下发客户端。
  物品模板里的 `_props.Prefab.path` 必须与 `bundles.json` 的 `key` 一致（此处均为 `C4Explosive.bundle`）。
- **配方不走 JSON 表覆盖**：`HideoutTable.Production.Recipes`（`List<HideoutProduction>`）是 DI 单例，
  代码直接追加即可。
- **物品注册阶段必须是 `Preload`**：`SaveCallbacks`（`TypePriority = 600000`）会把
  `CustomItemService.ProfilesLoaded` 置 true，之后再调 `CreateItem` 会抛
  `InvalidOperationException`；`PostLoad`（1000000）晚于它。`Preload`（100000）由
  `RunPreSptLoadCallbacks` 在数据库加载完成后、Web 服务启动前执行，此时表已就绪且可安全写入。

## 构建

```powershell
# 工作目录：mods/SamMeow-DoorBreachOverhaulServer
dotnet build -c Release
```

默认 `SPTInstallPath` 由 `mods/Directory.Build.props` 的 `SptRoot` 派生为
`$(SptRoot)\SPT_41x\SPT_Runtime`；可命令行覆盖：

```powershell
dotnet build -c Release -p:SPTInstallPath="D:\SPT\Server"
```

构建产物在 `bin/Release/`：`SamMeow.DoorBreachOverhaulServer.dll` + `bundles.json` +
`bundles/` + `db/`（SPT 运行时 DLL 因 `<Private>false</Private>` 不会一并输出）。

## 部署

把 `bin/Release/` 下的全部内容（DLL + `bundles.json` + `bundles/` + `db/`）放到：

```
SPT_Runtime/user/mods/SamMeow.DoorBreachOverhaulServer/
```

> 部署属整合包流程（MO2 overlay），本工程不直接写入游戏安装目录。

## 运行时验证（待启动服务器后执行）

1. 服务器启动日志出现单条成功行：
   `[SUCCESS] [DoorBreachOverhaulServer] 已加载：C4 物品 tpl=6636606320e842b50084e51a，工作台配方 id=665d4ce7e381d16c8676292b`
2. 日志出现 bundle 加载（`BundleLoader ... ok: >=1`），无 `Could not find bundle C4Explosive.bundle`。
3. 客户端请求 `/singleplayer/bundles` 返回 `C4Explosive.bundle` 且 CRC 正常。
4. 图鉴中出现 C4（炸药分类）；工作台 2 级可见该配方，产出 C4。
5. 进游戏实测：拆装 C4（模型/图标来自 bundle）与破门交互。

## 标准检查

```powershell
# 仓库根
powershell -ExecutionPolicy Bypass -File scripts/check-mod-standard.ps1 `
  -ModPath mods/SamMeow-DoorBreachOverhaulServer -TargetSptVersion 4.1.6 -Kind server
```

已知：本组件按设计**不含配置系统**，因此配置类规则（`STD-CFG-002/003/005`）会 FAIL —— 这是如实结果，
不伪造豁免（见工单 08 硬约束）。其余可检规则应 PASS。

## 未决问题

- 客户端 `DamageUtility` 仍引用 `660249a0712c1005a4a3ab41`（12/70 破门弹，上游自定义物品 ID）。
  本单只实现 C4 服务端组件，未注册该弹种 / 弹药盒；若客户端依赖它出现在物品表，需要单独工单补齐
  （对应上游 `DoorBreacher.bundle` / `DoorBreacherBox.bundle`）。
