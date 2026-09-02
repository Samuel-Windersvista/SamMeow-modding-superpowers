# SPT 4.1 客户端 Mod 模板

最小可用的 SPT 4.1 客户端 mod 模板（BepInEx 5 插件 + Harmony 补丁），部署到游戏目录的 `BepInEx/plugins/`。

## 目录结构

```
client-mod/
├── ClientModTemplate.csproj     # 项目文件（HintPath 引用 BepInEx / 0Harmony / Assembly-CSharp）
├── src/
│   ├── Plugin.cs                # BepInEx 插件入口（BaseUnityPlugin + BepInPlugin 特性）
│   ├── Configuration.cs         # BepInEx 配置封装（ConfigEntry 模式）
│   └── Patches/
│       └── ExamplePatch.cs      # Harmony 补丁示例（Prefix / Postfix）
├── README.md
└── .gitignore
```

## 使用步骤

### 1. 替换占位符

| 占位符 | 说明 | 示例值 |
|---|---|---|
| `{{ROOT_NAMESPACE}}` | 根命名空间 | `SamMeow.MyClientMod` |
| `{{MOD_CLASS_NAME}}` | 类名前缀（PascalCase，同时是程序集名） | `MyClientMod` |
| `{{MOD_NAME}}` | 显示名 | `My Client Mod` |
| `{{MOD_GUID}}` | 全局唯一 ID（BepInPlugin GUID） | `com.sammeow.myclientmod` |
| `{{MOD_VERSION}}` | 版本（semver 三段式） | `1.0.0` |
| `{{SPT_INSTALL_PATH}}` | SPT 客户端根目录（含 `EscapeFromTarkov.exe`） | `D:\Games\SPT` |
| `{{TARGET_CLASS_NAME}}` | 补丁目标游戏类型（4.1 反混淆真名） | `EFT.Player` |
| `{{TARGET_METHOD_NAME}}` | 补丁目标方法名 | `Something` |

### 2. 重命名

- 项目文件夹：`client-mod` → `MyClientMod`
- `ClientModTemplate.csproj` → `MyClientMod.csproj`

### 3. 确定补丁目标

4.1 客户端已反混淆（类型有真名真命名空间），但必须先确认目标方法与签名：

1. 用 dnSpy / ILSpy 打开 `EscapeFromTarkov_Data\Managed\Assembly-CSharp.dll`
2. 找到目标类与方法，记录完整命名空间
3. 把 `{{TARGET_CLASS_NAME}}` 换成完整类型名（含命名空间），如 `EFT.Player`
4. 方法名用字符串写法（`"{{TARGET_METHOD_NAME}}"`），运行时解析，签名不匹配会在启动时抛异常

### 4. 构建

```powershell
dotnet build -c Release -p:SPTInstallPath="D:\Games\SPT"
```

### 5. 部署

把构建产物复制到：

```
D:\Games\SPT\BepInEx\plugins\MyClientMod.dll
```

### 6. 验证

启动游戏，BepInEx 控制台/日志（`BepInEx/LogOutput.log`）应出现：

```
[Info   : My Client Mod] My Client Mod v1.0.0 已加载
```

配置文件生成在 `BepInEx/config/com.sammeow.myclientmod.cfg`。

## 关键点（4.1）

- **目标框架是 `netstandard2.1`，不是 net10.0**：客户端在 Unity 的 Mono 运行时下加载，net10.0 程序集无法被 Mono 加载。SPT 官方客户端模块（`external/spt-archive/modules/`）同样以 netstandard2.1 为目标
- 客户端改「表现」（UI/输入/渲染/本地计算），服务端改「规则与数据」——跨端同步的枚举数值必须一致
- 不要为 enum 扩展写自研 prepatcher DLL：4.1 由服务端 mod 经 `ClientEnumDefinitions` 注册，客户端内建 prepatcher 拉取
- `BepInEx/plugins/spt/` 与 `BepInEx/patchers/spt-prepatch.dll` 是 SPT 官方文件，卸载 mod 时不要删

完整参考：`knowledge/spt-kb/curated/modding-guide/03-client-mod-anatomy.md`。

## 坑

- 补丁目标方法签名与 `{{TARGET_METHOD_NAME}}` 不匹配 → 启动即炸（先 dnSpy 确认）
- 直接引用游戏类型时记得补 `using`（类型进了命名空间）
- 4.0 编译的客户端 mod 在 4.1 上无法加载，必须用 4.1 程序集重新编译
