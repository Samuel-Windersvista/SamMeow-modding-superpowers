---
version: [3.11]
domain: server
topic: mod-loading
source: curated
---
# Mod 加载流程笔记 [3.11]

> 状态：**已核实（源码实读 2026-09-07）** | 版本：[3.11]
> 源码：`server/project/src/loaders/`（PreSptModLoader、PostSptModLoader、PostDBModLoader、ModLoadOrder、ModTypeCheck、BundleLoader）、`utils/App.ts`、`callbacks/ModCallbacks.ts`

## 概览：三大阶段钩子

| 钩子 | 触发者 | 时机 | 使用时机 |
|---|---|---|---|
| `preSptLoad` / `preSptLoadAsync` | `PreSptModLoader`（`Program.ts:29`，直接调用） | 服务器初始化早期，数据库载入前 | 注册 OnLoad/OnUpdate、拿 Container |
| `postDBLoad` / `postDBLoadAsync` | `PostDBModLoader`（OnLoad[2]，`App.ts:63-65` 依次触发） | **数据库载入后** | 改数据库对象（DatabaseServer.getTables()） |
| `postSptLoad` / `postSptLoadAsync` | `ModCallbacks`（OnLoad[7] → `PostSptModLoader`） | SPT 核心加载后 | 最终初始化/自建数据持久化 |

> **重要（与常见认知相反）**：postSptLoad 在 postDBLoad **之后**执行。顺序由 `Container.ts:338-348` 的 OnLoad 注册次序决定：DatabaseImporter(0) → GameCallbacks(1) → **PostDBModLoader(2)** → HandbookCallbacks(3) → HttpCallbacks(4) → SaveCallbacks(5) → TraderCallbacks(6) → **ModCallbacks(7)（postSptLoad）** → PresetCallbacks(8) → RagfairPriceService(9) → RagfairCallbacks(10)。

## 完整加载时序

```
Program.start()  (Program.ts:20-32)
 ├─ Container.registerTypes             (Program.ts:22)
 ├─ createChildContainer + Watermark    (Program.ts:23-25)
 ├─ Container.registerListTypes         (Program.ts:28)  ← OnLoad/OnUpdate tag 集合
 ├─ PreSptModLoader.load()              (Program.ts:29)
 │   ├─ importModsAsync()  (PreSptModLoader.ts:103)
 │   │   ├─ 扫描 user/mods/ 目录       (:114)
 │   │   ├─ 读 order.json               (:119, 损坏时 try-catch 只报错)
 │   │   ├─ validMod() 逐项校验         (:488)
 │   │   ├─ 重复检测 (author-name)      (:230)
 │   │   ├─ 依赖/不兼容/sptVersion 校验  (:145-165)
 │   │   ├─ 整体校验失败 → 全部不加载    (:167-170, errorsFound return)
 │   │   ├─ sortMods() 按 order.json    (:206)
 │   │   ├─ ModCompilerService.compileMod() (有 src/*.ts 且 COMPILED, :393-406)
 │   │   └─ ModLoadOrder.setModList() 拓扑排序 (:193)
 │   └─ executeModsAsync()  (PreSptModLoader.ts:325)
 │       ├─ sortModsLoadOrder() 读 loadorder.json 优先 (:379-387)
 │       └─ require(main) → preSptLoadAsync (try-catch) / preSptLoad (无 try-catch)  (:341-371)
 ├─ Container.registerPostLoadTypes     (Program.ts:31)
 └─ App.load()  (All OnLoad 顺序 onLoad)  (Program.ts:32)
     ├─ OnLoad[0] DatabaseImporter.onLoad()   ← 数据库导入 (setTables)
     ├─ OnLoad[1] GameCallbacks
     ├─ OnLoad[2] PostDBModLoader.onLoad()    ← postDBLoad + addBundles()
     ├─ OnLoad[3-6] Handbook/Http/Save/Trader Callbacks
     ├─ OnLoad[7] ModCallbacks.onLoad()       ← postSptLoad (PostSptModLoader)
     ├─ OnLoad[8-10] Preset/RagfairPriceService/Ragfair
     └─ setInterval(5000) → onUpdate 每 5s tick  (App.ts:67-69)
```

## package.json 支持字段（`models/spt/mod/IPackageJsonData.ts`）

| 字段 | 类型 | 说明 |
|---|---|---|
| `name` | string | mod 名（必填，校验 :517） |
| `author` | string | 作者（必填；`author-name` 是依赖/重复判定的 key） |
| `version` | string | 版本（必填且须合法 semver，:532） |
| `license` | string | **必填**（校验用美式拼写 `license`，:517；接口里 `licence`/英式拼写是易混点，加载不读） |
| `sptVersion` | string | 兼容的 SPT 版本（semver/range；缺失或不合规则**拒绝加载**，:287-319） |
| `main` | string | 入口文件（必须 `.js`；可在 `src/` 存 `.ts` 由编译器生成，:537-556） |
| `isBundleMod` | boolean | 是否含 client bundle（触发 `addBundles`，PostDBModLoader.ts:74） |
| `incompatibilities?` | string[] | 不兼容的 `author-name` 列表（:460） |
| `loadBefore?` | string[] | 需排在哪些 mod 之前（ModLoadOrder.ts:26） |
| `loadAfter?` | string[] | 需排在哪些 mod 之后（ModLoadOrder.ts:115） |
| `modDependencies?` | Record<string,string> | 依赖 key=`author-name`，value=满足的版本（:425） |
| `dependencies?` | Record<string,string> | npm 级依赖（addModAsync 复制为 pkg.dependencies，:412） |
| `scripts` | Record<string,string> | **故意清空**（`pkg.scripts={}`，:409） |
| `devDependencies` / `url` / `contributors` | - | 声明但不作为加载约束 |

**不存在的字段**（grep 确认）：`isMain`、`postLoad`、`tro`、`loadOrder`（package.json 里）。加载顺序由 `user/mods/order.json`（自动记录，PreSptModLoader.ts:26）与 `user/mods/loadorder.json`（mod 可手写，存在则直接覆盖计算顺序，:379-387）控制。

## 排序语义（`loaders/ModLoadOrder.ts`）

- `setModList(mods)` :17：克隆副本；先将每个 mod 的 `loadBefore` **反转**为被指定 mod 的 `loadAfter`（`invertLoadBefore`，:78）。
- `getLoadOrderRecursive()` :91：DFS 拓扑排序；`visited` 检测到环 → 抛 `modloader-cyclic_dependency`（:105）；`loadAfter` 相互冲突 → 抛 `modloader-load_order_conflict`（:123）；`modDependencies` 的 key 加入依赖集（:118）；先递归依赖，再写入 `loadOrder`（:143）。
- 一句话：`loadAfter:[X]`= 排在 X 之后；`loadBefore:[X]` = 排在 X 之前；`modDependencies` = 依赖（并保证先加载）。

## 隔离 / 容错（try-catch 位置汇总）

| 场景 | 位置 | 是否隔离 |
|---|---|---|
| `preSptLoadAsync` 抛错 | PreSptModLoader.ts:352-365 | 是（记 error，continue） |
| `preSptLoad`（同步）抛错 | PreSptModLoader.ts:368-371 | **否**（向上抛） |
| `require(mod 入口)` | PreSptModLoader.ts:341 / PostSptModLoader.ts:41 / PostDBModLoader.ts:48 | **否**（入口异常终止服务） |
| `postSptLoadAsync` 抛错 | PostSptModLoader.ts:44-53 | 是 |
| `postSptLoad`（同步） | PostSptModLoader.ts:56-58 | 否 |
| `postDBLoadAsync` 抛错 | PostDBModLoader.ts:51-61 | 是 |
| `postDBLoad`（同步） | PostDBModLoader.ts:63-65 | 否 |
| 整体校验 errorsFound | PreSptModLoader.ts:167-170 | 任一校验失败 → 全部不加载（return） |
| mod 非 3.x+ 兼容 | PreSptModLoader.ts:343-348 | 记 error、delete imported[mod]、**return 终止整个循环**（上游 bug：应为 continue） |
| order.json 损坏 | PreSptModLoader.ts:126-133 | 是（try-catch 兜底） |
| OnUpdate 抛错 | App.ts:83-87 | 是（记 error 后继续其它 OnUpdate） |
| OnLoad 队列 | App.ts:63-65 | **否**（某个 OnLoad 抛错中断后续队列） |
| 全局兜底 | Program.ts:33-35 | `errorHandler.handleCriticalError` |

**要点**：异步钩子基本都有各自 try-catch；**同步钩子与 `require()` 没有**。这是 3.11 最明显的隔离缺口——mod 入口写不好会拖垮整个服务器。

## Bundle 加载（`loaders/BundleLoader.ts`）

- `addBundles(modpath)` :51：读 `mods/*/bundles.json`（`IBundleManifest` :74），遍历 `manifest`，对每个 `key` 计算/匹配 hash（`BundleHashCacheService`），按 `mods/<mod>/bundles/<key>` 路径注册，得到 `BundleInfo(modpath, filename=key, crc, dependencies)`。
- `getBundles()` :37 返回克隆数组（供 Serializer 下发到客户端 `singleplayer/bundles`）。
- 触发者：`PostDBModLoader.addBundles()` :69，只对 `pkg.isBundleMod` 为真的 mod。

## Mod 类型检测（`loaders/ModTypeCheck.ts`）

纯 Duck-Typing 守卫：`isPreSptLoad`/`isPostSptLoad`/`isPostDBLoad` + 异步变体 (`*Async`)，`isPostV3Compatible()` :63 任一为真即视为 3.x+ 兼容。即「两种类型」= 同步钩子与异步钩子。

## validMod() 拒绝条件（`PreSptModLoader.ts:488`）

- 文件名为 `bepinex`/`user`/`src`/`db`（:491-501，拒绝）
- 含 `plugins/` 目录或 `.dll`（:503-506，判断为 client mod）
- 缺 `package.json`（:509-513）
- 缺必需字段 / 版本非 semver / `main` 非 `.js` / `incompatibilities` 非数组（:516-564）

## 已核实位置

- `loaders/PreSptModLoader.ts:25-26` basepath=user/mods/、modOrderPath=user/mods/order.json
- `loaders/PreSptModLoader.ts:103` importModsAsync / `:325` executeModsAsync / `:488` validMod / `:425` areModDependenciesFulfilled / `:460` isModCompatible / `:287` isModCombatibleWithSpt
- `utils/App.ts:72-102` update() 每 5s tick
- `callbacks/ModCallbacks.ts:28-32` postSptLoad 入口
