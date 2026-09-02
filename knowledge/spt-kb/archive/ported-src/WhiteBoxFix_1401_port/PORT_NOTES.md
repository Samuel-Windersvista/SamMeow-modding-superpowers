# WhiteBoxFix 1401 移植笔记

> 移植日期：2026-08-10 | forgeId: 1401 | 目标：SPT 4.1.2（EFT 0.16.9.40743）

## 来源

- 3.11 编译产物：`D:\Temp\opencode\third-client\MarsyApp-WhiteBoxFix.dll`（9728 字节，spt-reflection 3.11.3）
- 反编译：`ilspycmd -p`（全程序集反编译，嵌套类型 `<>c` / ConfigurationManagerAttributes 单独 -t 反编译失败，改用 -p）

## 功能

Harmony prefix patch `ItemFilter.CheckItem`：清理拖拽物品过滤数组中损坏的节点（null / 无效），修复物品视图白色方块 bug。无渲染/材质代码——根因是数组数据而非 Shader。

## 改动清单

| 文件 | 改动 |
|------|------|
| `WhiteBoxFix.csproj` | SDK 风格 → 经典格式（ToolsVersion 15.0 / v4.7.2）；引用 `..\..\m2-port\Refs-410\`；HintPath 注意 csproj 在 `srv-port\1401\`，到 `m2-port` 需两级 `..` |
| `ItemViewPatches.cs` | `CheckItem` 参数 `ref string[]` → `ref MongoID[]`（4.1 签名）；MongoID 为 struct 不能 `== null`，改 `string.IsNullOrEmpty(s.ToString())`（MongoID.ToString() 对无效实例返回 Empty，经 Cecil 验证 IL）；补 `using EFT;` |
| `TarkovVersion.cs` | `Paths.ExecutablePath` → `BepInEx.Paths.ExecutablePath`——4.1 Assembly-CSharp 全局命名空间顶层有 public `Paths` 类（含 Start/ctor，无 ExecutablePath），遮蔽 BepInEx.Paths（KB 5.1 全局命名空间陷阱，与 Utils 类同类） |
| `AssemblyInfo.cs` | `TarkovVersion(35392)` → `TarkovVersion(40743)`（SPT 4.1.2 = EFT 0.16.9.40743，KB 5.1）；AssemblyVersion → 4.1.2.0 |
| `WhiteBoxFix.cs` | BepInPlugin 版本 3.11.0 → 4.1.2 |

## 未改动

- `Patcher.cs` / `PatchManager.cs`：ModulePatch API 4.1 完全一致（Enable/Disable/Logger/TargetMethod，Cecil 验证）
- Harmony 目标定位：`AccessTools.Method(typeof(ItemFilter), "CheckItem")` 按名匹配，4.1 中 `EFT.InventoryLogic.ItemFilter.CheckItem(Item, MongoID[])` 存在

## 验证

- 编译：MSBuild Release 0 错误（仅 CS0649 反编译残留警告，ConfigurationManagerAttributes 元数据字段，无害）
- 产物：`bin\Release\MarsyApp-WhiteBoxFix.dll` 10240 字节
- 部署：`E:\Game\EFT_Offline\Inescapable Tarkov\mods\[12]白色方块bug修复-WhiteBoxFix\BepInEx\plugins\MarsyApp-WhiteBoxFix.dll`
- 运行时验证：待进游戏确认（patch 绑定目标方法无报错预期，签名已按 4.1 核对）
