# Finding: MO2 VFS (USVFS) x SPT 集成分析

> Label: `wayfinder:research`
> Ticket: [#002 Survey MO2 VFS integration approach for SPT](../tickets/002-survey-mo2-vfs-integration.md)
> Date: 2026-08-02
> Status: research complete — implementation decisions deferred to #4
> 研究方法: 源码实读 + 本仓库控制平面代码 + KB 记录 + SPT wiki 交叉核实

---

## 1. 结论速览 (TL;DR)

1. **MO2 VFS 可以覆盖 SPT 的 mod 目录**,但需要**一个自定义 game plugin**(`mobase.IPluginGame`),把 MO2 的"数据目录"从 `<game>/Data/` 指向 **SPT 安装根**。社区对非 BGS Unity 游戏 (Boneworks / Hard Bullet 等) 已有同类先例。
2. **MO2 优先级只解决"文件覆盖"问题,不解决"加载顺序"问题。** SPT 服务端加载顺序由 `TypePriority` + `ModGuid` 决定,与目录顺序无关;BepInEx 插件顺序由依赖声明决定。这两层排序 MO2 都无法直接控制,只能通过"文件是否出现"间接影响。
3. **当前控制平面 (mo2_agent_control.py + mo2-mcp) 的 BGS 特异性集中在 `plugins.*`(esp/esm 插件表)与 `profile.initialize`(BGS INI 模板)**,其余方法 (`mods.*`、`organizer.*`、`launch.*`、`installation.*`) 通用,可直接复用。
4. **最大技术风险是进程传播**:SPT 有 3 个进程 (SPT.Server → SPT.Launcher → 游戏 exe),usvfs 对子进程的钩子传播需要实测验证。若传播失败,游戏进程看不到虚拟文件,方案 A 失效。
5. **SPT 的 `user/profiles/`(玩家存档)是真实写目录**,usvfs 只重定向"虚拟文件路径"的写入到 Overwrite,真实存在的目录写入直通——存档不会漂移到 Overwrite,这是好消息,但 `user/mods/<mod>/config.json` 类"mod 自己回写配置"的行为会落入 Overwrite,需要设计处理。

---

## 2. SPT 目录结构与 mod 加载模型

### 2.1 SPT 安装根 (游戏目录)

SPT installer 会把 EFT 文件复制并降级到独立目录(见 `knowledge/spt-kb/wiki/How_SPT_Works.md:19-26`),推荐位置如 `C:\Games\SPT`(见 `wiki/Installation_Guide.md:30`)。**SPT.Server 与 SPT.Launcher 必须留在该目录内**(`wiki/Installation_Guide.md:71-72` —— 移出会触发 "Watermark" 错误)。因此该目录整体 = MO2 语境下的"游戏根"。

### 2.2 与 mod 加载相关的目录

| 路径 (相对 SPT 根) | 用途 | 来源 |
|---|---|---|
| `user/mods/<mod-dir>/` | **服务端 mod**。每个子目录 = 一个 mod,顶层直接放 DLL(不递归),DLL 内必须恰有一个 `IModMetadata` 实现 | `SPTarkov.Server/Modding/ModLoader.cs:20,122,255-261`;`wiki/Mod_Types.md:15,20,26` |
| `user/patchers/<ModGuid>/` | 服务端 enum prepatch 定义(一个 JSON 数组文件) | `ModLoader.cs:21,340-388`;`IModMetadata.cs:75-79` |
| `user/profiles/` `user/logs/` `user/certs/` `user/credentials/` | 玩家存档 / 日志 / 证书 / 面板凭据 —— **运行时写数据** | `SPT-archive/.../docker/README.md:38-46` |
| `BepInEx/plugins/` | **客户端 mod**。BepInEx 插件 DLL,允许子目录(官方模块放 `plugins/spt/`) | `wiki/Mod_Types.md:32`;`build-4.0-assets/BepInEx/plugins/spt/ConfigurationManager` |
| `BepInEx/patchers/` | 客户端 prepatcher DLL(官方 `spt-prepatch.dll` 必留) | `wiki/Uninstalling_Mods.md:18,20` |
| `BepInEx/config/` `BepInEx/core/` | BepInEx 全局配置与核心 DLL(游戏进程启动时加载) | `build-4.0-assets/BepInEx/{config,core}` |

### 2.3 SPT mod 归档布局 (安装约定)

SPT 4.0+ 强制 mod 归档按 `SPT/`、`BepInEx/` 或两者同时存在的方式打包,解压后**拖入游戏根即完成安装**(`wiki/Installing_Mods.md:36-37`)。这意味着:
- 归档内的 `BepInEx/plugins/...` 与 `user/mods/...` 路径直接相对于 **SPT 根**,而不是任何 `Data/` 前缀。
- 组合 mod 同时含服务端与客户端两部分(`wiki/Mod_Types.md:40-41`)。

### 2.4 命名混淆提醒

`A-核心服务端/modules/project/SPT.Common/Utils/VFS.cs` 是纯文件 IO 工具类(读/写/移动),与 MO2 的 USVFS **毫无关系**。写代码/文档时避免把两者混为一谈。

---

## 3. MO2 控制平面现状 (本仓库)

### 3.1 部署面

- `scripts/install-mo2-control-plane.ps1`:`v0.1 无 C++ DLL`,部署 Python 插件 `mo2_agent_control.py` 到 `<MO2Root>/plugins/` + 支持目录 `Mo2AgentControl/`,并归一化 `ModOrganizer.ini lock_gui=false`(见脚本 :6-19)。
- 运行时文件:`<MO2Root>/plugins/Mo2AgentControl/bootstrap/runtime/{status.json, capabilities.json, endpoint.json, blocker-events.jsonl}`。
- 传输:Windows 命名管道 `\\.\pipe\mo2-control-plane-<pid>`(`mo2_agent_control.py:451-458,2865-2893`)。

### 3.2 已暴露的方法面 (mo2_agent_control.py:315-342, 2616-2794)

| 组 | 方法 | SPT 可复用性 |
|---|---|---|
| `system.*` | ping / capabilities / shutdown | 通用 |
| `mods.*` | list / set_active / set_priority / rename / remove / create / meta_read / meta_write | **通用,SPT 直接可用**(列表 + 启用/禁用 + 优先级 + meta.ini) |
| `plugins.*` | list / set_state / set_priority / set_load_order | **BGS 特有**(esp/esm 插件表、master/light 标志、loadOrder)。SPT 无 plugins.txt,此组应改为 no-op 或重定义 |
| `profile.*` | list / active / initialize | initialize 写 BGS 风格 INI 模板(`ProfileSetting`),SPT 需要变体或置空 |
| `executables.list` | 列出 ModOrganizer.ini 自定义可执行项 | 通用,需注册 SPT.Server / SPT.Launcher |
| `installation.*` | install_local_archive / create_mod_from_directory | 通用骨架,但 MO2 内建安装器 (FOMOD/BAIN) 不识别 SPT 归档布局,需自定义 `IModInstaller` |
| `organizer.*` | refresh / resolve_path / get_file_origins / find_files / virtual_file_tree / start_application / wait_for_application | **通用,对 SPT 冲突分析最有价值**(VFS 查询 + 进程启动) |
| `launch.*` | start / status / wait / stop | 通用;启动走 `organizer.startApplication`(经 usvfs)或 subprocess 回退(`:2505-2531`) |

消费面 `tools/mo2-mcp/`(TS MCP)约 34 个工具(`mo2-modlist`、`mo2-pluginlist`、`mo2-toggle-mod`、`mo2-switch-profile`、`mo2-assets-*`、`mo2-install` 等,见 `src/tools/`),其中 `mo2-pluginlist` / `mo2-toggle-plugin` / `mo2-profile-ini-*` 是 BGS 向,其余通用。

### 3.3 VFS 启动入口

`tools/mo2-vfs-launcher/` 提供 `ModOrganizer.exe run -e OpenCodeVfsLauncher` 模式,把任意目标放进已建立的 usvfs 上下文内执行(README:41-49)。这是 SPT 服务端/启动器通过 VFS 启动的现成管道。

### 3.4 本仓库 KB 已固化的 MO2 约束

- **Stock Game Data 只读**:游戏本体的改动一律以 mod 覆盖层表达(`knowledge/bgs-kb/packs/core/records/tooling-mo2/stock-game-data-read-only.v1.md`)。对 SPT:基础 SPT 安装视为 stock,所有 mod 内容进 `mods/<name>/`。
- **MO2 VFS 投影 `Data/`,不替换游戏根可执行文件邻域**(`records/engine/xse-update-workflow.v1.md:30,125`):script extender 类根级工具不被 VFS 覆盖。SPT 的 BepInEx doorstop(winhttp.dll 注入)类似此问题,需要验证或作为 stock 文件保留。
- **MO2 必须可见启动**(`records/tooling-mo2/mo2-visible-start-required.v1.md`):控制平面插件依赖真实可见的 MO2 进程,SPT 集成同样适用。

---

## 4. SPT 服务端加载顺序 (源码实读)

来源:`SamMeow_SPT410_source_code/SPTarkov.Server/Modding/ModLoader.cs` + `Libraries/SPTarkov.Server.Core/Models/Spt/Mod/IModMetadata.cs` + `DI/OnLoadOrder.cs`(与 `knowledge/spt-kb/curated/api-notes-4.1/mod-loading.md` 一致)。

1. **发现**:`Directory.GetDirectories("./user/mods/")`,每个目录 = 一个 mod;只读**顶层** `.dll`(`ModLoader.cs:122-137,255-261`);DLL 内找恰一个 `IModMetadata` 实现(`:303-338`);`ModGuid/Name/Author/License` 缺一即拒载(`:275-285`)。
2. **排序**:加载完成后按 `ModGuid` 不区分大小写排序(`ModLoader.cs:139`);DI 层进一步按 `Injectable.TypePriority` 排序、同优先级按 ModGuid 字母序 tiebreaker(`IModMetadata.cs:23-25`)。阶段常量:Watermark=0 → Preload=100000 → … → PostLoad=1000000(`OnLoadOrder.cs:5-15`)。
3. **校验**:`ModValidator` 检查 SPT 版本兼容、`ModDependencies`、`Incompatibilities`(`ModLoader.cs:56`)。
4. **Prepatch 流程**:`user/patchers/*` 的 JSON 定义在内存中补丁 `SPTarkov.Server.Core.dll`,写入 `./SPTarkov.Server.Core.Patched.dll` 后**以新进程引导 patched 服务端**(`ModLoader.cs:38-53,146-178,231-242`)——服务端根目录每启动都会重建 patched 程序集。
5. **客户端 enum 扩展 (4.1)**:不再自写客户端 prepatcher,由服务端 mod 通过 `ClientEnumDefinitions.Add(...)` 注册,游戏内建 prepatcher 从服务器拉取(`modding-guide/03-client-mod-anatomy.md:33-51`)。

**结论**:服务端加载顺序是 mod 元数据内在属性,MO2 无法通过面板排序控制;但文件覆盖(同一路径的 DLL/config/资源)由 MO2 优先级决定。

---

## 5. BepInEx 插件加载模型

- **结构**:插件 DLL 放 `BepInEx/plugins/`(允许子目录),声明 `[BepInPlugin("GUID", "Name", "Version")]` + 继承 `BaseUnityPlugin`(官方示例 `SPTCorePlugin.cs:8-9`)。BepInEx 核心在 `BepInEx/core/`,配置在 `BepInEx/config/BepInEx.cfg`(启用了 AssemblyCache、HideManagerGameObject,见 cfg:7,17)。
- **加载顺序**:BepInEx Chainloader 按 **`BepInDependency` 依赖声明做拓扑排序**——有依赖的插件在依赖之后加载;无依赖插件的先后无强保证(目录枚举顺序,NTFS 上实际近似字母序)。**没有用户可编辑的插件顺序文件**(区别于 BGS 的 plugins.txt)。
- **对 MO2 的意义**:插件加载顺序不可被 MO2 面板直接控制。MO2 能做的是:控制插件 DLL 是否出现(启用/禁用 mod)、哪个版本的 DLL 胜出(文件覆盖)。

---

## 6. MO2 VFS (USVFS) 适用性分析

### 6.1 机制要点

- MO2 的 usvfs 通过钩住被启动进程的文件系统调用,把 `mods/<name>/` 内容按左面板优先级(上=高)叠加到 game plugin 声明的 `dataDirectory` 上。
- BGS 游戏:`dataDirectory = <game>/Data/`,mod 根映射到 Data/。**SPT 没有 Data/ 层**——mod 内容直接相对于 SPT 根 (`BepInEx/plugins/...`、`user/mods/...`)。
- **关键结论**:需要自定义 game plugin 使 `dataDirectory = SPT 根`,这样 `<modRoot>/BepInEx/plugins/x.dll` 自然映射到 `<sptRoot>/BepInEx/plugins/x.dll`,`<modRoot>/user/mods/X/` 映射到 `<sptRoot>/user/mods/X/`。MO2 的 Python 插件系统支持 `mobase.IPluginGame`,可与现有 `mo2_agent_control.py` 并存部署。
- 写入重定向:usvfs 只把"虚拟文件路径"(即由 mod 提供、磁盘上不存在真实文件的路径)的写入重定向到 `<MO2Root>/overwrite/`。真实目录(如 `user/profiles/`)写入直通。

### 6.2 优势

| 优势 | 说明 |
|---|---|
| Mod 隔离 | 基础 SPT 安装保持只读 stock,mod 全部落在 `mods/<name>/`,卸载 = 禁用一个 mod,无残留文件 |
| 启用/禁用 | `mods.set_active` 立即改变 BepInEx/plugins 与 user/mods 的可见性 |
| 配置切换 | MO2 profile = 一套 modlist(顺序+启用集);SPT 组合包可做多 profile(如 原版 / 硬核 / 大修) |
| 文件冲突可视 | `organizer.get_file_origins` / `find_files` / 冲突图标给出"哪个 mod 提供了这个文件"的确定性答案——这正是 SPT 冲突分析 (ticket #1) 需要的运行时证据 |
| 玩家存档不受干扰 | `user/profiles/` 是真实目录,存档直通,不落入 Overwrite |
| 复用控制平面 | `mods.*`、`organizer.*`、`launch.*`、`installation.*` 无需改动 |

### 6.3 挑战与风险

| 挑战 | 说明 | 缓解方向 |
|---|---|---|
| **game plugin 缺失** | 现仓库无 SPT game plugin;MO2 不认识 SPT 根 | 新增 Python `IPluginGame`(与 mo2_agent_control.py 同机制部署) |
| **usvfs 子进程传播** | SPT.Server / SPT.Launcher / 游戏 exe 是 3 个进程,游戏由 Launcher 拉起;usvfs 对后代进程的钩子传播需实测 | 先做最小验证:MO2 内启动 SPT.Server,确认其能看到虚拟 `user/mods/`;再验证 Launcher→游戏链 |
| **根级原生文件** | BepInEx doorstop (winhttp.dll) 与 `BepInEx/core/` 在游戏根,若 MO2 VFS 不覆盖游戏根可执行邻域(KB xse 记录),则 BepInEx 核心必须作为 stock 文件保留在基础安装,只有 `plugins/`、`patchers/` 被虚拟化 | 按 KB 模式:根级工具不虚拟化,只虚拟 mod 内容目录 |
| **归档安装器** | SPT 归档无 FOMOD,MO2 内建安装器不识别 `SPT/` `BepInEx/` 布局 | 自定义 `mobase.IModInstaller`:识别归档布局 → 按相对路径解包进 mod 目录 |
| **mod 自回写配置** | 服务端 mod 常把运行时配置写回 `user/mods/<mod>/config.json`(虚拟路径) → 落 Overwrite | 文档化"Overwrite 是配置输出区";或把 config 层作为独立 mod 管理 |
| **prepatch 重建** | patched 服务端程序集每启动重建且依赖 `user/patchers/` 可见性 | 服务端必须经 VFS 启动;prepatch 目录纳入虚拟化 |
| **启动顺序** | SPT.Server 必须先于 Launcher;服务端 mod 需在服务端进程可见 | 控制平面编排:`launch.start(server)` → wait → `launch.start(launcher)` |
| **profile.initialize** | BGS INI 模板对 SPT 无意义 | 置空或提供 SPT 变体(如预置 BepInEx.cfg 片段) |

---

## 7. Mod 优先级 / 排序映射

| 层 | 排序机制 | MO2 能否控制 |
|---|---|---|
| MO2 左面板优先级 | 上 = 高优先级,决定**文件覆盖胜者** | 直接控制 (mods.set_priority) |
| SPT 服务端加载顺序 | `Injectable.TypePriority` → `ModGuid` 字母序 | **不能直接控制**;间接影响= 文件是否出现、哪份 config/DLL 胜出 |
| BepInEx 插件顺序 | `BepInDependency` 拓扑排序;无依赖时目录枚举序 | **不能直接控制**;MO2 决定 DLL 可见性与版本胜出 |

映射规则一句话:**MO2 优先级 = 文件级覆盖仲裁;加载顺序 = mod 内在属性。** 启用/禁用 mod (mods.set_active) = 从 BepInEx/plugins 与 user/mods 中摘除/挂载该 mod 的全部文件。组合 mod 的"服务端+客户端"两部分天然落在同一个 MO2 mod 目录下(归档内 `user/mods/...` 与 `BepInEx/plugins/...` 相对路径并存),一个开关同时控制两层。

---

## 8. 控制平面需求 (对 BGS 实现的增量)

### 8.1 必须新增

1. **SPT game plugin** (Python `IPluginGame`):`dataDirectory = SPT 根`;声明可执行项 `SPT.Server.exe` / `SPT.Launcher.exe`;游戏标识覆盖 `appliesTo`(KB 记录目前 games 列表只列 BGS 游戏)。
2. **SPT 归档安装器** (`IModInstaller`):解析 `SPT/`、`BepInEx/`、`user/` 相对路径布局;meta.ini 记录 `ModGuid`(供未来校验/冲突分析)。
3. **启动编排**:SPT.Server → SPT.Launcher 的时序启动与等待(复用 `launch.*` + `organizer.start_application`)。

### 8.2 需要改造 (BGS 特异性)

| 现方法 | BGS 假设 | SPT 处理 |
|---|---|---|
| `plugins.*` | esp/esm 插件表、master/light、loadOrder | 语义不适用:SPT 无 plugins.txt。保留实现(不破坏 BGS)但 SPT profile 下返回空/禁用,或增加 `plugins.provider = "none"` 声明 |
| `profile.initialize` | BGS INI 模板 (`ProfileSetting.MODS/CONFIGURATION`) | SPT 下跳过或写入 SPT 需要的占位(如 `user/modlist` 标记) |
| `installation.install_local_archive` | MO2 内建安装器链 | 前插自定义 SPT installer;无法识别时回退通用解包 |

### 8.3 无需改动 (直接复用)

`mods.*`(列表/启用/优先级/meta)、`organizer.*`(VFS 查询与冲突证据)、`launch.*`、`system.*`、`profile.list/active`、`executables.list`。SPT 的 `spt` MCP(见 MAP.md:14)应消费这些方法 + 新增的 game-plugin/installer 能力,而非复制一套。

---

## 9. 集成方案 (2-3 选一,含取舍)

### 方案 A:全 VFS 覆盖 (MO2 即运行时)

自定义 Python game plugin,`dataDirectory = SPT 根`;SPT.Server 与 SPT.Launcher 全部经 MO2 启动(usvfs 覆盖 `BepInEx/plugins|patchers`、`user/mods|patchers`);BepInEx 核心/doorstop 作为 stock 保留。

- **优点**:运行时 profile 秒切、冲突证据实时可查、卸载零残留、复用整个控制平面。
- **缺点**:依赖 usvfs 子进程传播(必须实测);MO2 必须开着;根级 BepInEx 行为需验证;复杂度最高。

### 方案 B:无 VFS 构建管线 (MO2 即打包机,产物为自包含 SPT 目录)

控制平面只用于:mod 清单/优先级/冲突仲裁(基于 mo2-assets-engine + 文件哈希 + meta.ini),按 profile 合并出**一个实体 SPT 目录**作为构建产物(对齐 MAP.md "自包含 SPT 目录" 的发行格式),SPT 进程原生运行,无 usvfs。

- **优点**:零运行时风险;发行格式就是产物本身;不要求 MO2 常驻。
- **缺点**:开关一个 mod 需要重新合并;多 profile 需多份目录或复刻;失去运行时冲突证据。

### 方案 C:混合 (开发用 A,发行用 B)

日常迭代走方案 A(快反馈、可诊断),发布时走方案 B 导出自包含目录(对齐 #4 的发行物定义)。

- **优点**:两全;先以 A 验证排序/冲突假设,再以 B 固化。
- **缺点**:两套路径都要实现;需在 #4 定义何时从 A 切 B。

> 推荐方向(供 #4 决策,非本票定案):**C 混合**,阶段一先做 A 的最小验证(游戏 plugin + SPT.Server 经 MO2 启动看到虚拟 user/mods + Launcher→游戏传播),验证失败再降级为 B。

---

## 10. 证据坐标索引

| 事实 | 位置 |
|---|---|
| 控制平面部署方式(无 C++ DLL) | `scripts/install-mo2-control-plane.ps1:6-19` |
| 控制平面方法注册表 | `tools/mo2-control-plane/live-bridge/mo2_agent_control.py:315-342,2616-2794` |
| VFS 启动入口 (OpenCodeVfsLauncher) | `tools/mo2-vfs-launcher/README.md:41-49` |
| MCP 消费面工具列表 | `tools/mo2-mcp/src/tools/` |
| KB: Stock Game 只读 / 可见启动 / 根级工具不虚拟化 | `knowledge/bgs-kb/packs/core/records/tooling-mo2/{stock-game-data-read-only,mo2-visible-start-required}.v1.md`, `records/engine/xse-update-workflow.v1.md` |
| SPT mod 安装布局约定 | `knowledge/spt-kb/wiki/Installing_Mods.md:36-37` |
| 服务端/客户端 mod 分类与目录 | `knowledge/spt-kb/wiki/Mod_Types.md:15,20,26,32` |
| 官方必留文件 | `knowledge/spt-kb/wiki/Uninstalling_Mods.md:18,20` |
| SPT 服务端加载路径/排序/校验 | `SamMeow_SPT410_source_code/SPTarkov.Server/Modding/ModLoader.cs:20-22,122-139,249-293` |
| TypePriority+ModGuid 排序语义 | `.../Libraries/SPTarkov.Server.Core/Models/Spt/Mod/IModMetadata.cs:23-25` |
| 加载阶段常量 | `.../DI/OnLoadOrder.cs:5-15` |
| 服务端部署目录 (user/ 内容) | `SamMeow_SPT410_source_code/docker/README.md:38-46` |
| BepInEx 结构/配置 | `C-构建与发布/build/build-4.0-assets/BepInEx/{config,core,plugins}` |
| BepInEx 插件声明模式 | `A-核心服务端/modules/project/SPT.Core/SPTCorePlugin.cs:8-9` |
| 4.1 客户端 enum 扩展流程 | `knowledge/spt-kb/curated/modding-guide/03-client-mod-anatomy.md:33-51` |
| SPT.Common/VFS.cs 命名混淆(纯 IO) | `A-核心服务端/modules/project/SPT.Common/Utils/VFS.cs` |
