---
version: [4.1]
domain: both
topic: client-anatomy
source: curated
---
# Client Mod 解剖 [4.1]

> 适用版本：[4.1] | 主要来源：`wiki/SPT_41/Client_40_to_41.md`、`wiki/SPT_41/modding/client/Class_Name_Mappings.md`、`wiki/SPT_41/modding/EnumExtensions.md`、`wiki/modding/tutorials/Client_Modding_Quick_Guide.md`

## 基本形态

客户端 mod = BepInEx 插件 DLL，装进游戏目录的 `BepInEx/plugins/`。极少数还带 `BepInEx/patchers/` 的 prepatcher DLL（4.1 起官方 enum 扩展不再需要自写 prepatcher，见下文）。

- 快速入门（Jehree 的经典教程）：`wiki/modding/tutorials/Client_Modding_Quick_Guide.md`
- 调试：`wiki/modding/tutorials/debug_dnSpy.md`
- 官方客户端模块源码（4.1）：`A-核心服务端/modules/`
- 4.0 时代示例：`E-Mod开发示例/mod-examples/`（注意版本，4.1 需重新编译）

## 4.1 最大变化：客户端反混淆

4.0 客户端是混淆的：`GClass680`、`GStruct80` 这类无命名空间的名字，4.0 还带一层部分改名（`LoggerClass` 之类的扁平别名）。

4.1 客户端反混淆了：类型有真名、有真命名空间。

| 4.0 | 4.1 |
|-----|-----|
| `GClass680` | `ABotProfileCreator` |
| `GStruct80` | `AbsolutDecals.DecalMeshVertexData` |
| `GClass1033` | `AbsolutDecals.DecalSystemUtils` |
| `LoggerClass` | （真实类名，查映射表） |

影响：**任何直接引用游戏类型的客户端 mod**（包括 Harmony patch 目标）都要改。全量对照表在 `wiki/SPT_41/modding/client/Class_Name_Mappings.md`（4.0 名 → 4.1 名，Ctrl+F 查旧名）。类型进了命名空间，还要补对应 `using`。

4.0 构建的客户端 mod 在 4.1 上直接无法加载——所有引用了游戏程序集的 mod 必须用 4.1 程序集重新编译。

## 客户端枚举扩展（4.1 新流程）

不需要自写 prepatcher DLL。由**服务端 mod** 注册：

```csharp
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class MyMod(ClientEnumDefinitions clientEnumDefinitions) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken cancellationToken)
    {
        clientEnumDefinitions.Add("com.example.my-mod", new EnumEntryDefinition
        {
            EnumType = "EFT.EBuffId",
            ConstantName = "MyBuff",
            ConstantValue = 10000,
            JsonEnumName = "my_buff",
        });
        return Task.CompletedTask;
    }
}
```

流程：游戏启动 → 客户端内建 prepatcher 从服务器拉 `/singleplayer/customEnumEntries` → 写入 `Assembly-CSharp` → 继续加载。服务器必须先启动并注册完毕。

普通插件（在 Assembly-CSharp 已打补丁后加载）可用 `Enum.TryParse<EFT.EBuffId>("MyBuff", ...)` 验证。

## 客户端与服务端的边界

- 客户端改「表现」：UI、输入、渲染、本地计算
- 服务端改「规则与数据」：商人、任务、战利品、存档
- 跨端同步的枚举数值必须一致（两端各自注册，名字可不同，值必须相同）

## 坑

- 不要为 enum 扩展在 BepInEx 里放自写 prepatcher DLL（官方流程已接管）
- `spt-prepatch.dll`（`BepInEx/patchers/`）是 SPT 官方文件，卸载 mod 时不要删
- `BepInEx/plugins/spt/` 是 SPT 官方客户端模块，不要删
