---
version: [3.11]
domain: server
topic: config
source: curated
---
# 配置系统笔记 [3.11]

> 状态：**已核实（源码实读 2026-09-07）** | 版本：[3.11]
> 源码：`server/project/src/servers/ConfigServer.ts`、`server/project/src/models/enums/ConfigTypes.ts`、`assets/configs/`（27 个 json）

## 加载机制（`ConfigServer.ts`，58 行）

- `@injectable() class ConfigServer`，构造器 `initialize()`（:13-19）。
- **加载路径**（`initialize()` :37）：
  - 编译版：`SPT_Data/Server/configs/`（`ProgramStatics.COMPILED` 为 true）
  - 开发版：`./assets/configs/`
- `getFiles(filepath, true, ["json","jsonc"], true)` 递归取 json/jsonc（:37）。
- 对每个文件：去扩展名 → `jsonUtil.deserializeJsonC` → **键 = `"spt-" + 文件名`**（:41-53）。例：`core.json` → 键 `spt-core`；`http.json` → `spt-http`。
- 公开方法：
  - `getConfig<T>(configType: ConfigTypes): T`（:21，取已加载 config）
  - `getConfigByString<T>(configType: string): T`（:29）

## ConfigTypes 枚举（`models/enums/ConfigTypes.ts`）

`AIRDROP="spt-airdrop"`、`BACKUP="spt-backup"`、`BOT="spt-bot"`、`PMC="spt-pmc"`、`CORE="spt-core"`、`HEALTH="spt-health"`、`HIDEOUT="spt-hideout"`、`HTTP="spt-http"`、`IN_RAID="spt-inraid"`、`INSURANCE="spt-insurance"`、`INVENTORY="spt-inventory"`、`ITEM="spt-item"`、`LOCALE="spt-locale"`、`LOCATION="spt-location"`、`LOOT="spt-loot"`、`MATCH="spt-match"`、`PLAYERSCAV="spt-playerscav"`、`PMC_CHAT_RESPONSE="spt-pmcchatresponse"`、`QUEST="spt-quest"`、`RAGFAIR="spt-ragfair"`、`REPAIR="spt-repair"`、`SCAVCASE="spt-scavcase"`、`TRADER="spt-trader"`、`WEATHER="spt-weather"`、`SEASONAL_EVENT="spt-seasonalevents"`、`LOST_ON_DEATH="spt-lostondeath"`、`GIFTS="spt-gifts"`

## core.json 关键字段（本次仓库：3.11.4）

```json
{ "sptVersion": "3.11.4", "profileSaveIntervalSeconds": 15, "features": { "compressProfile": false, ... }, ... }
```
- `features.compressProfile` 控制存档压缩（false = 不压缩，SaveServer 用）。
- `profileSaveIntervalSeconds` 控制自动保存间隔（15 秒）。
- configs 共 27 个 json（`assets/configs/`），对应上面的 ConfigTypes。

## 重要事实

1. **ConfigServer 只加载内建 config 目录，不扫描 `user/mods/`**——不存在「默认 config → mod config 覆盖」的系统级层叠。
2. **Mod 不能通过 ConfigServer 覆盖内建 config**（无 fallback/叠加机制）。mod 若要改内建 config 行为，通常 `getConfig()` 拿到对象后**原地改字段**（对象是引用，直接改动即生效于运行时）。
3. **`user/mods/<name>/config/<x>.json` 仅为业内约定**，ConfigServer 不注册它；mod 需在 `postSptLoad` 里用自己的 `FileSystem`（`container.resolve("FileSystem")`）或 node `fs` 自行读取（`${preSptModLoader.getModPath(modName)}config/<x>.json`，getModPath 返回 `"user/mods/<mod>/"`，PreSptModLoader.ts:99）。
4. **无 `ModConfigService`**（grep 全仓库无此文件）。

## mod 应知

- 内建 config 读取：`this.configServer.getConfig<ICoreConfig>(ConfigTypes.CORE)`（注入 `ConfigServer`）。
- 改内建值：直接改返回对象字段（引用共享）。
- 自定义 config：自管 JSON 文件（写 `user/mods/<name>/config/`），自读自写；可用 `OnUpdateModService.registerOnUpdate` 周期落盘。
