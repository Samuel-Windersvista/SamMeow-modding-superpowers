---
version: [4.1]
domain: server
topic: save-profile
source: curated
---
# 存档（Profile）笔记 [4.1]

> 状态：**已核实（源码定位 2026-08-02，细节待读实现）** | 适用：[4.1]
> 源码：`Libraries/SPTarkov.Server.Core/Servers/SaveServer.cs`、`Routers/SaveLoad/ProfileSaveLoadRouter.cs`、`Callbacks/SaveCallbacks.cs`、`Services/Profile/ProfileMigrationService.cs`

## 关键事实（源码确认）

1. **存档读写**：`SaveServer`（`Servers/SaveServer.cs`）负责保存；网络层走 `Routers/SaveLoad/ProfileSaveLoadRouter.cs`
2. **保存回调阶段**：`Callbacks/SaveCallbacks.cs` 挂在 `OnLoadOrder.SaveCallbacks`（600000）——mod 持久化数据在此阶段读写
3. **迁移管线可扩展**：`ProfileMigrationService` 构造注入 `IEnumerable<IProfileMigration>` 并排序执行——**mod 可实现 `IProfileMigration` 接口注册自己的存档迁移逻辑**（`Core/Migration/` 命名空间）
4. 迁移失败（存档完全无法加载）→ 抛 `InvalidOperationException`
5. 4.1 Profile 服务族：`Services/Profile/`：`BackupService`、`CreateProfileService`、`ProfileActivityService`、`ProfileFixerService`、`ProfileMigrationService`（原 ProfileValidatorService 已更名）
6. 存档模型：`Models/Eft/Profile/SptProfile`（迁移服务的返回类型）

## mod 持久化数据放哪（推断，待源码确认）

- 首选：profile 模型内自定义字段（若存在扩展点）或独立文件（`user/profiles/` 旁或 mod 文件夹，用 ModHelper 路径）
- 写入时机：`SaveCallbacks` 阶段的钩子接口（接口名待读 SaveCallbacks.cs 确认）

## 待查问题（读 SaveServer/SaveCallbacks 实现时回答）

1. `SaveCallbacks.cs` 暴露的钩子接口签名
2. mod 数据写入 profile 的官方扩展点（是否有 `SptProfile` 的 mod 字段）
3. 存档保存触发时机（回合结束/定时/关闭）在 4.1 的确切实现

## 已核实坐标

- `Servers/SaveServer.cs`
- `Routers/SaveLoad/ProfileSaveLoadRouter.cs`
- `Callbacks/SaveCallbacks.cs`
- `Services/Profile/ProfileMigrationService.cs`（含 `IEnumerable<IProfileMigration>` 注入）
- `Migration/`（IProfileMigration 定义位置，待确认具体文件）
