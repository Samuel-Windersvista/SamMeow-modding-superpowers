# 02: 桥侧装备/武器字段 + boss 判定表扩展

**What to build:** `/raid/player` 扩展 `weapon`（`{tpl,name,ammoInMag,ammoInChamber}|null`）与 `equipment`（`[{slot,tpl,name}]`）——成员路径 spike 核验（`Player.HandsController` → `IFirearmHandsController.Item` + 弹药计数；`Profile.Inventory.Equipment` 槽枚举）。boss 判定表扩展：role 含 `boss` 或命中显式表（`sectantpriest`）→ boss；表为单点常量 + 单测。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] `weapon`/`equipment` 契约实现（字段序稳定；非持枪/空槽语义明确）
- [x] 成员路径 spike 结论记录（HandsController/FirearmController/Inventory 链）——见 Comments live 实证
- [x] boss 判定表：`sectantpriest` 等归位；既有 `pmc`/`scav`/`other` 行为不回归
- [x] 纯逻辑单测覆盖（武器字段映射、装备槽映射、分类表）
- [x] 构建 0 error；`dotnet test` 全绿

## Comments

### 2026-09-15 实施 + live 核验

**落地**：`/raid/player` 扩展 `weapon`（`{tpl,name,ammoInMag,ammoInChamber}|null`）+ `equipment`（`[{slot,tpl,name}]`，仅占用槽）；`BotClassifier` boss 表扩展（显式表 + `boss` 子串）+ 单测。

**成员路径（live 实证）**：
- `weapon` ← `Player.HandsController` → `IFirearmHandsController.Item`（`StringTemplateId`/`Name`）+ `GetCurrentMagazineCount()` / `ChamberAmmoCount`：live 读到 AK-105（`5ac66d9b5acfc4001633997a`，弹匣 25 + 膛内 1）。
- `equipment` ← `Player.Profile.Inventory.Equipment.Slots`：live 读到 12 个占用槽（FirstPrimaryWeapon / SecondPrimaryWeapon / Scabbard / FaceCover / Headwear / TacticalVest / SecuredContainer / Backpack / Pockets / Earpiece / Dogtag / ArmBand）。
- `tpl` 与 SPT 本地化数据交叉一致（`SPT_Data/database/locales/global/en.json`）。

**偏差**：`name` 在客户端 raid 上下文返回本地化键形态（`<tpl> Name`）而非本地化值——`tpl` 为权威标识；本地化名解析列 backlog（README 已注明）。

**分类**：live 未观察到分类回归（Interchange：pmc 6 / scav 16 / boss 0；Sandbox 局无异常）。
