# MODDING-STD-WAIVER — SamMeow.DoorBreachOverhaulServer（SPT 4.1.6 服务端）

本文件登记对本工程检查器 FAIL 的有意豁免。格式：`Waiver: STD-XXX-NNN: <理由>`。

## 已登记豁免

Waiver: STD-CFG-003: 本组件按设计不含配置系统——C4 的物品与配方数据是固定契约（模板 ID `6636606320e842b50084e51a` 与客户端硬编码一致），无用户可调项；配置已整体本地化到客户端 BepInEx（ADR-0002），服务端不再承载任何配置，故无 `IOnDIConstruct` 配置注册。

Waiver: STD-CFG-002: 同上——本组件无用户可调配置，不存在 `config/config.jsonc`（有意不引入空配置以通过检查）。

Waiver: STD-CFG-005: 同上——无 `config/defaultConfig.jsonc`。
