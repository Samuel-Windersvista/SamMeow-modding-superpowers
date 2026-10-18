# 08: C4 服务端组件（独立工作流）

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 为 4.1.6 重建 C4 服务端组件（以发行包内 `BackdoorBanditServer` 的职责为基准，按服务端 4.0→4.1 迁移指南重建；上游源码不在仓库）：注册 C4 物品（模板 + bundle 资源）与工作台配方，使游戏内可获得并带入战局；**不做 HTTP 配置端点**（配置已本地化，ADR-0002）。与客户端 07 联合完成端到端（放置→引爆→破坏管线）。本单在 01 完成后可与 02–07 并行推进。

**Blocked by:** 01（工程奠基：合并基座与 BB 独立基线；脚手架后即可并行）

**Status:** ready-for-agent

- [ ] 服务端 mod 在 4.1.6 加载成功（版本约束正确、日志无异常）（待 Overseer 启动服务器断言）
- [ ] C4 物品游戏内可获得（配方/商人途径）并可带入战局；bundle 资源渲染正常（图标/模型）（待 Overseer 实测）
- [ ] 与 07 联合端到端通过（放置→倒计时→引爆→完整破坏管线）（待工单 07 完成）
- [ ] bundle 兼容性验证记录（4.1.6 加载正常）（待服务器日志断言 BundleLoader）
- [x] 打包/命名符合包内服务端 mod 惯例；随主工程归档（同名 repo）——`mods/SamMeow-DoorBreachOverhaulServer`（ModGuid `com.sammeow.doorbreach.server`）；MO2 覆盖层测试部署完成

**上游参考（本仓归档）:** 发行包解包 `D:\Temp\opencode\bbng-2.0.4`（`SPT\user\mods\BackdoorBanditServer` 构成基准）；KB `wiki/SPT_41/Server_40_to_41.md`（服务端迁移全要点）。

## Comments

- 2026-10-18（实现完成，代码侧）：@fixer 后台作业（fix-3）在 `mods/SamMeow-DoorBreachOverhaulServer` 新建 4.1.6 服务端工程（net10.0，独立于客户端）。
  - **4.1 API 实证**（反编译 `SPTarkov.Server.Core` 4.1.6）：`CustomItemService.CreateItem(NewItemDetails, Assembly?)` 直接写 `TemplateTable.Items`（4.1 无 `db/CustomItems/*.json` 自动扫描）；bundle 由 `BundleLoader` 扫描 mod 根 `bundles.json`（`manifest: [{key, dependencyKeys}]` + `bundles/<key>`），CRC 经 `/singleplayer/bundles` 下发（`IsBundleMod` 已移除）；自定义配方 = 代码 append `HideoutTable.Production.Recipes`（`HideoutAreas.Workbench = 10`）。
  - 交付：`src/ModMetadata.cs`（IModMetadata，GUID `com.sammeow.doorbreach.server`、`~4.1.0`）、`src/ModEntry.cs`（IOnLoad）、`src/Services/C4RegistrationService.cs`（契约断言 + CreateItem + 配方 append，fail-fast）、`bundles.json` + `bundles/C4Explosive.bundle`（2,147,196 B，SHA256 `471BFB5A…F1F19415`，与上游 2.0.4 发行同源）、`db/customitems/c4explosive.jsonc`、`db/hideout/c4explosive_recipe.jsonc`（工作台 2 级；材料 TNT×2/电线束/引信/军用电路板/精英钳）、README/LICENSE/NOTICES/豁免文件。
  - **偏差裁决 ①（加载阶段）**：工单身份栏写 `PostLoad + 1`，实现改用 `Preload + 1`。编排者独立复核证据链成立：`OnLoadOrder.PostLoad = 1000000`；`SaveCallbacks`（`TypePriority = 600000`）执行 `customItemService.ProfilesLoaded = true;`；`CustomItemService` 在 `ProfilesLoaded` 后调用 `CreateItem` 抛 `InvalidOperationException`。→ **采纳 `Preload + 1`**（工单文字为笔误，证据为准）。
  - **偏差裁决 ②（配置系统）**：本组件无配置（ADR-0002）；标准检查 STD-CFG-002/003/005 FAIL 属如实结果。已登记 `MODDING-STD-WAIVER.md`（三条：固定契约数据、无用户可调项、不做空配置过检查）。复跑：PASS=20 / FAIL=0 / WAIVED=3。
  - **偏差裁决 ③（破门弹）**：客户端仍引用上游已移除的破门弹 `660249a0712c1005a4a3ab41`（`DamageUtility.IsBreachingSlug` 死分支，无物品也不崩溃）。本单范围仅 C4，不注册该弹种；如需复活另开工单（当前无影响）。
  - 编排者复验：`dotnet build -c Release -t:Rebuild` 0 warning / 0 error（DLL 17,408 B）；标准检查复跑 PASS=20 / FAIL=0 / WAIVED=3；bundle SHA256 与源一致；数据 JSONC 结构完整（SPT 真实 converter 解析已验证：item prefab/stackMax、recipe Workbench/6 requirements）。
  - 部署：MO2 覆盖层为**单一合并 mod** `[8]门的大修-破门大修-DoorBreachOverhaul`（Overseer 指令，2026-10-18）——同一覆盖层携带 `BepInEx\plugins\DoorBreachOverhaul\`（客户端）与 `SPT_Runtime\user\mods\DoorBreachOverhaulServer\`（本服务端组件）；原独立 test-build 覆盖层已删除。VFS 核验：两 DLL 均可见。
- 待 Overseer（运行时）：
  1. 启动服务器 → 断言 `[SUCCESS] [DoorBreachOverhaulServer] 已加载：C4 物品 tpl=6636606320e842b50084e51a，工作台配方 id=665d4ce7e381d16c8676292b`。
  2. BundleLoader 无 `Could not find bundle` / `not a valid Unity asset bundle`；`/singleplayer/bundles` 含 `C4Explosive.bundle`。
  3. 游戏内：图鉴炸药类出现 C4；工作台（≥2 级）出现配方并可产出；物品图标/模型正常（bundle 2019.4→2022.3 兼容性待实测）。
