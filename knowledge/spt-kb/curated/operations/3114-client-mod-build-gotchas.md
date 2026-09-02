---
version: [3.11]
domain: client
topic: tooling
source: curated
---
# SPT 3.11.4 反编译与客户端 mod 构建坑（2026-08-16 实证）

> 状态：已验证（EFT 0.16 Assembly-CSharp.dll 反编译 + net471 客户端 mod 构建全程踩坑记录）

## ILSpy 反编译 EFT 0.16

- **ilspycmd 10.1 会在 `BackendAbstractClass.RegenerateToken` 崩溃**（ICSharpCode bug，ArgumentNullException: annotation，HandleDelegateConstruction 路径），整个反编译中断。`dotnet tool update -g ilspycmd` 升到 **11.0** 后全量通过，仅 18 个方法失败（全部后端占位类/后端 dummy，不影响性能/AI 分析）
- 命令：`ilspycmd -p --nested-directories -o <输出目录> <dll路径>`（-p 工程模式按类型分文件，便于 grep）
- 14.5MB 的 Assembly-CSharp.dll 反编译产出约 8340 个 .cs / 33MB / 3500 万行
- 产物是派生缓存：放 `external/decompile-cache/`，gitignore，报告里附再生成命令
- spt 官方插件（spt-core/spt-singleplayer）也应反编译留档——官方 patch 目标清单是冲突分析基线

## EFT 0.16 代码结构速查（混淆版）

- AI 核心类在**全局命名空间**（EnemyInfo/LookSensor/BotMover/BotMemoryClass/AITaskManager/BotsClass/BotStandByType/EAITaskGroupType）；`BotOwner`/`Player`/`GameWorld`/`IPlayer` 在 **EFT 命名空间** → using EFT 不能省
- 反编译命名：`[SPTRenamedClass("GClass3402")]` 特性标记原名（如 CameraClass=GClass3402）
- 嵌套类定位：`AccessTools.Inner(typeof(GClass895), "Class558")`；Player 嵌套泛型类：`AccessTools.Inner(typeof(Player), "GClass1828`1")`

## net471 客户端 mod 构建坑

5. **PowerShell 5.1 改写 UTF-8 源码文件 = 编码灾难（2026-08-19 事故实证）**：`(Get-Content $f -Raw) | Set-Content $f` 处理无 BOM 的 UTF-8 文件时按 GBK 读、按 UTF-8 写回，中文注释/字符串全毁（string 字面量里的中文变 `?` 直接导致 CS1039 编译错误）。规则：① 修改代码/项目文件永远优先用 agent 的 read/edit/write 工具（自带正确编码）；② 必须用 shell 时，读写两侧都显式 `-Encoding UTF8`；③ 事故后恢复：本例靠完整重写文件（mods/ 未入 git，无版本可回退——另一个教训：产出 mod 目录应尽早 git add）

1. **目标框架 net471**：以 spt-core.csproj 为准（反编译官方插件可得），不是 4.1 模板的 netstandard2.1
2. **Comfort.dll 必须引用**：`Comfort.Common.Singleton<T>` 在 Comfort.dll（Managed 目录），用到 Singleton 时 csproj 加引用
3. **全局命名空间污染**：Assembly-CSharp 里有全局 `Paths` 类，会遮蔽 `BepInEx.Paths`（CS0117 而非 CS0433，具有迷惑性）→ 写全 `BepInEx.Paths.PluginPath`
4. **AccessTools.FieldRefAccess 返回类型**是 `AccessTools.FieldRef<T,TField>` 委托，直接存该类型，不要自定义 delegate（签名不匹配 CS0029）
5. 引用全部 `<Private>false</Private>`（运行时由游戏目录提供，不随 DLL 分发）

## 运行时验证捷径（不进战局确认补丁加载）

1. DLL 放 `BepInEx/plugins/`
2. 启动 SPT.Server.exe，等 6969 端口 LISTENING（约 20-40s）
3. 直接起游戏（绕过启动器点击）：
   `EscapeFromTarkov.exe -token=<任意串> -config={"BackendUrl":"http://127.0.0.1:6969","Version":"live","MatchingVersion":"live"}`
   token 无效最多卡在登录，但 **BepInEx 插件 Awake 照常执行**
4. 查 `BepInEx/LogOutput.log` 里插件的补丁应用日志即完成加载级验证
