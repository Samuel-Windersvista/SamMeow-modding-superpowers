# release —— 发行归档

> 本目录仅保留 `.gitkeep` 与 `README.md`；归档 zip 被 `.gitignore` 忽略（`release/*`）。

- 命名：`[5]软核大修-InescapableTarkovsSoftcore-<version>.zip`
- 结构（= `build/overlay/` 内容的逐字节拷贝）：
  ```
  SPT_Runtime/user/mods/com.sammeow.inescapable-softcore/
    ├─ InescapableTarkovsSoftcore.dll
    ├─ config.json
    └─ config/               # 外置数据表（12 个；可编辑，外部优先）
       ├─ trueitems/*.json   # 6 表
       ├─ armbands.json
       ├─ backpacks.json
       └─ softcore/*.json    # 4 表
  ```
  解压即得可放入 MO2 `mods\` 的 overlay 树（overlay 目录名自定；注意全树物理路径 ≤260，见 KB「USVFS 长路径」条目）。

## 归档清单

| 文件 | 日期 | 备注 |
|---|---|---|
| [5]软核大修-InescapableTarkovsSoftcore-0.1.0.zip | 2026-10-13 | DLL SHA256 头 `91B9C9E9…`；215/215 单测、服务器核验 10609/32/0、F2 复验 PASS |
| [5]软核大修-InescapableTarkovsSoftcore-0.2.0.zip | 2026-10-17 | 含 `config/` 12 外置表；235/235 单测；zip SHA256 `B724F8FA…`、DLL SHA256 头 `C2E0DE29…`；部署验收待工单 17 |
