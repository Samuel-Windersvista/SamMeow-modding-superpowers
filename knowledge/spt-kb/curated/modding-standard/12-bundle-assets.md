---
version: [4.1, 5.0]
domain: both
topic: modding-standard
source: curated
---

# 12 bundle/资产（BND）

> **Domain slug:** `BND` · **规则 ID 前缀:** `STD-BND-`
> 分级标准、规则条目格式与豁免流程见 [README.md](README.md)。
> 状态：规则已填充（ticket 06，2026-09-14）。

## 维度范围

- 资源替换与数据库覆盖包的目录/打包约定
- Unity bundle 升级兼容陷阱（约定+陷阱，不做从零教程）

## 规则

### STD-BND-001 — 把服务端 bundle 放在 `bundles/` 下并用 `bundles.json` 注册

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`curated/migration/bundle-compat-311-to-41.md` §3.5（服务端 mod 资源放 `user/mods/<ModName>/bundles/`）、`curated/migration/bundle-311-to-41.md` §3.3（4.1 删除 `IsBundleMod`，改为检测 `bundles.json` 存在性）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** 服务端分发的资源 bundle 放 `<mod>/bundles/`，并在 `<mod>/bundles.json` 的 `manifest` 中用相对 `bundles/` 的 `key` 注册；不要依赖已删除的 `IsBundleMod` 元数据字段。

```json
{
  "manifest": [
    {
      "key": "assets/content/items/mods/scopes/x.bundle",
      "dependencyKeys": ["shaders", "cubemap", "physicsmaterials.bundle"]
    }
  ]
}
```

> 深入：[migration/bundle-compat-311-to-41.md](../migration/bundle-compat-311-to-41.md)、[migration/bundle-311-to-41.md](../migration/bundle-311-to-41.md)、[bundle-difference-audit.md](../../../../docs/bundle-difference-audit.md)

### STD-BND-002 — 保持 `bundles.json` 的 `key` 与文件路径一一对应

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`docs/wayfinder/findings/003-bundle-upgrade-analysis.md` §3.2（`BundleLoader` 文件路径 = `<mod>/bundles/{key}`，CRC32 写入 `user/cache/bundleHashCache.json`）、`curated/migration/server-mod-311-to-41.md` §8.5 检查清单第 4 项（manifest 条目全对应 `bundles/` 下文件）；语料：资源/数据库覆盖包 37 例（EV-CORPUS-TYPE）
- **Rule:** `manifest` 每个 `key` 必须在 `bundles/` 下有对应文件；`dependencyKeys` 只列目标 EFT 版本中真实存在的游戏原生 bundle（如 `shaders`、`cubemap`、`physicsmaterials.bundle`），缺依赖会加载失败。

```jsonc
// bundles.json：每个 key 必须在 bundles/{key} 有对应文件
{
  "manifest": [
    { "key": "assets/content/items/mods/scopes/x.bundle" }
  ]
}
```

> 深入：[migration/bundle-compat-311-to-41.md](../migration/bundle-compat-311-to-41.md) §4、[migration/server-mod-311-to-41.md](../migration/server-mod-311-to-41.md)

### STD-BND-003 — 客户端私有 bundle 用 `AssetBundle.LoadFromFile` 随插件分发

- **Level:** MAY
- **Applies:** both
- **Evidence:** 机制：`docs/wayfinder/findings/003-bundle-upgrade-analysis.md` §3.1 路径 B（BepInEx 插件自行 `AssetBundle.LoadFromFile()` 读插件目录下的 `.bundle`，与 SPT 版本无关）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** 仅客户端使用的 shader、后处理与私有资产走本地加载，不注册进服务端 `bundles.json`；其兼容性只取决于 Unity 引擎，与 SPT 版本无关。

```csharp
// 插件目录下的私有 bundle：仅本地加载，不注册进服务端 bundles.json
string path = Path.Combine(pluginDir, "my_shaders.bundle");
AssetBundle bundle = AssetBundle.LoadFromFile(path);
```

> 深入：[wayfinder/findings/003-bundle-upgrade-analysis.md](../../../../docs/wayfinder/findings/003-bundle-upgrade-analysis.md)、[migration/bundle-311-to-41.md](../migration/bundle-311-to-41.md)

### STD-BND-004 — 升级 bundle 时先核对脚本绑定键，再实机验证 shader/材质

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`curated/migration/bundle-compat-311-to-41.md` §2.4（外部程序集引用的绑定键 = 程序集名 + 命名空间 + 类名，重编译 DLL 后必须三者不变，否则 prefab 报 "Script missing"）、§4 标准检查流程；`curated/migration/bundle-311-to-41.md` §3.1（shader 是最常见破坏源，症状为紫色模型）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** 重编译 mod DLL 后，必须确认 bundle 引用的程序集名/命名空间/类名未变，并进游戏验证 shader、材质与挂点。

```
外部脚本引用绑定键：m_AssemblyName + m_Namespace + m_ClassName
紫色模型 → shader 不兼容；Script missing → 脚本绑定断裂；纯白/黑 → 材质参数错误
```

> 深入：[migration/bundle-compat-311-to-41.md](../migration/bundle-compat-311-to-41.md)、[migration/bundle-311-to-41.md](../migration/bundle-311-to-41.md)

### STD-BND-005 — 不以 Unity 版本头判定兼容性

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`curated/migration/bundle-311-to-41.md` §1–§2（引擎同为 Unity 2022.3.43f1；实测 85.4% 的 bundle 为 2019.4 构建，靠 TypeTree 向后兼容加载）、`docs/wayfinder/findings/003-bundle-upgrade-analysis.md` §2（旧版 bundle 可被新版引擎加载，无向前兼容）；语料：既有规范素材（EV-CORPUS-MATERIALS）
- **Rule:** bundle 头的 Unity 版本只反映作者打包环境，不决定兼容性；旧版（2019.4）bundle 在新版引擎向后兼容加载，只有 shader 等不稳定类型或跨大版本才需重建。

> 深入：[migration/bundle-311-to-41.md](../migration/bundle-311-to-41.md)、[migration/bundle-compat-311-to-41.md](../migration/bundle-compat-311-to-41.md)

### STD-BND-006 — 由 mod 代码显式加载数据库覆盖，不依赖服务器自动合并

- **Level:** SHOULD
- **Applies:** both
- **Evidence:** 机制：`curated/api-notes-5.0/config-system.md`（`ConfigLoader` 只扫 `SPT_Data/configs`，不扫 `user/mods/`，无「默认 → mod」覆盖层叠机制）、`curated/api-notes-5.0/mod-loading.md`（`ModLoader` 只载目录顶层 `.dll`，无 DLL 抛 `ModLoaderException`）、`curated/migration/server-mod-311-to-41.md` §8.2–8.3（WTT 服务从 `db/CustomItems/*.json` 等独立目录读取）；语料：`db/` 子目录在近期目录出现 14 次（EV-CORPUS-STRUCT）
- **Rule:** 数据库覆盖数据放 mod 目录内的独立子目录（如 `db/`），由 DLL 在 `IOnLoad` 中读取并注入内存表；不要假设把 JSON 放进 `user/mods/` 就会被服务器自动合并。

```csharp
// IOnLoad：从 mod 目录内的 db/ 读取覆盖 JSON 并注入内存表
// （具体注入 API 随 mod 而异，此处为示意）
string modDir = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
var items = modHelper.GetJsonDataFromModFile<List<MyItem>>("db", "customItems.json");
// 再调用数据库服务对应方法把 items 注入内存表
```

> 深入：[api-notes-5.0/config-system.md](../api-notes-5.0/config-system.md)、[migration/server-mod-311-to-41.md](../migration/server-mod-311-to-41.md)
