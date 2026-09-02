# spt-metadata-reader — SPT 4.1 server mod DLL 元数据读取 helper

读 `user/mods/<mod>/` 下顶层 DLL 的 IModMetadata（ModGuid/Name/Author/Version/SptVersion），
用 AsmResolver 静态读取实现类型的**无参构造器 IL**（record 初始化器编译成的 ldstr 常量）。
供 spt-mcp 子进程同步调用。

## 构建

```powershell
dotnet build SptMetadataReader.csproj -c Release
# 产物: bin/Release/spt-metadata-reader.exe（含 AsmResolver DLL）
```

构建依赖：`lib/` 下的 AsmResolver DLL（从 SPT_Runtime 拷贝，勿用 NuGet 版本——
SPT 自带的是旧 API：`AsmResolver.PE.DotNet.Cil.CilOpCode`，无 `CilOpCodes` 常量类）。

## 用法

```
spt-metadata-reader.exe <dllPath> [<dllPath>...]
```

stdout 输出 JSON 数组，每元素一个 DLL：
```json
[{"path":"...","ok":true,"name":"Warsaw Pact Trader","guid":"com.sammeow.warsawtrader","version":"1.0.0","sptVersion":"~4.1.0","author":"SamMeow","license":"MIT","modDependencies":[]}]
```

失败元素：`{"path":"...","ok":false,"error":"..."}`（无 IModMetadata 实现 / 无参构造器缺失 / 文件损坏）。

## 与 spt-mcp 集成

- spt-mcp 通过 `SPT_MCP_HELPER` 环境变量定位 helper（OpenCode 插件已设置）
- 找不到时 `spt_list_mods` 对 DLL mod 回退目录名兜底，`readDllMetadata` 返回 error 标记（不抛异常）
- 知识背景：`knowledge/spt-kb/curated/api-notes-4.1/server-mod-metadata-dll.md`
