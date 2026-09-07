---
version: [3.11]
domain: server
topic: database
source: curated
---
# 数据库结构笔记 [3.11]

> 状态：**已核实（源码实读 2026-09-07）** | 版本：[3.11]
> 源码：`server/project/src/servers/DatabaseServer.ts`、`utils/DatabaseImporter.ts`、`utils/ImporterUtil.ts`、`models/spt/server/IDatabaseTables.ts`

## 内存数据库（`servers/DatabaseServer.ts`，26 行）

```ts
@injectable()
export class DatabaseServer {
  protected tableData: IDatabaseTables = { bots, hideout, locales, locations, match,
                                           templates, traders, globals, server, settings };
  getTables(): IDatabaseTables   // :19  —— mod 直接改此返回对象
  setTables(tableData): void     // :23
}
```
`tableData` 是可变对象引用；`getTables()` 返回的就是服务器正在使用的数据库，mod 拿到后**直接改字段即生效**。

## 顶级表结构（`models/spt/server/IDatabaseTables.ts`，24 行）

```
bots?, hideout?, locales?, locations?, match?, templates?, traders?,
globals?, server?, settings?
```

## 数据库加载流程（JSON → Database 对象）

加载者：`utils/DatabaseImporter.ts`（196 行，实现 `OnLoad`，route `"spt-database"`）。

`onLoad()`（:50）：
1. `getSptDataPath()`（:51）：编译版 `SPT_Data/Server/`、开发版 `assets/`
2. 编译版校验 `checks.dat`（SHA1 批量校验，:53-71）
3. `await this.hydrateDatabase(this.filepath)`（:73）
4. 映射 `assets/images/` 到 ImageRouter（:75-77）

`hydrateDatabase(filepath)`（:83-100）：
```ts
const dataToImport = await this.importerUtil.loadAsync<IDatabaseTables>(
    `${filepath}database/`,        // 目录
    this.filepath,                 // strippablePath
    async (file, data) => await this.onReadValidate(file, data),
);
this.databaseServer.setTables(dataToImport);   // :99 写回 DatabaseServer
```

## ImporterUtil：JSON 文件路径 == 对象树路径（`utils/ImporterUtil.ts`，60 行）

- `loadAsync<T>(filepath, strippablePath, onReadCallback, onObjectDeserialized)`（:21）：
  1. 递归取 `[.json]` 文件（`getFiles(filepath, true, ["json"], true)`，:21）
  2. 并行读每个文件 → `deserializeWithCacheCheck` → `placeObject(...)`（:24-35）
  3. **`placeObject`**（:42-59）：把文件路径切分成对象树节点，按路径逐层挂载（idempotent 原地导入，不复制）
- **这就是「JSON 文件路径 → 对象树路径」的映射机制**。

实际目录布局（`assets/database/`）：子目录 `bots/ hideout/ locales/ locations/ match/ templates/ traders/`，根级 JSON `globals.json` `server.json` `settings.json`。
例：`database/bots/xxx.json` → `dataToImport.bots.xxx`；`database/globals.json` → `dataToImport.globals`。

## mod 应知

- 改数据库的官方途径：在 **postDBLoad** 钩子里向容器取 `DatabaseServer`（`@inject("DatabaseServer")`），调 `getTables()` 直接改对象（或经 `DatabaseService` 的 typed getter：`getProfiles()/getTraders()/getTemplates()/getGlobals()/getCustomization()/getItems()`）。
- 时机：`DatabaseImporter` 是 OnLoad[0]（数据库导入在 postDBLoad 之前完成，见 di-container.md / mod-loading.md）。
- 无独立 URL import 机制；数据库为内存对象，改后立即生效（不需要重载）。
