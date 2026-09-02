# Unity Asset Bundle 兼容性与 SPT 3.11 → 4.1 升级路径分析

> 状态:研究完成(2026-08-05)| 适用版本:[3.11 → 4.1]| 目的:2622 个 .bundle 文件(17.9 GB / 177 mod)从 3.11.4 迁移到 4.1 的可行性判定
> 素材:spt-kb/wiki/(FAQs_311、FAQs_40、Updating_SPT、debug_dnSpy、WTT_Vol1、Mod_Types、SPT_41/Server_40_to_41)、spt-kb/curated/(modding-guide、api-notes-4.1)、SamMeow_SPT410_source_code(BundleLoader/BundleCallbacks/BundleSerializer/BundleHashCacheService/BundleManifestEntry)、archive/forge/mods/(Picture-in-Picture-Disabler_2667_source、CWX-MegaMod_1454_source、All-Quests-Checkmarks_2025_source)、sp-tarkov/modules(SPT.Custom/Utils/BundleManager.cs)、Unity 官方文档(AssetBundlesIntro)、社区(OpenCritic、Forbes、whenwipe、ModernWeaponMods、WTT-CommonLib、dvize/SPT-TypeScript)

---

## 0. 结论先行(Executive Summary)

**任务前提需要修正**:SPT 3.11 并非「EFT 0.14/0.15 时代」。官方 FAQ 明确:

| SPT 版本 | EFT 版本 | 发布时间 | Unity 版本 |
|---|---|---|---|
| SPT 3.9.x | EFT 0.14.x | 2023-12 ~ 2024-08 | **Unity 2019.4.x** |
| SPT 3.10.x | EFT 0.15.5 | 2024-08 ~ 2024-12 | **Unity 2019.4.x** |
| **SPT 3.11.4 (LTS)** | **EFT 0.16.1.3.35392** | 2025-03-05 | **Unity 2022.3.43f1** |
| **SPT 4.0 / 4.1** | **EFT 0.16.9.x (40087+)** | 2025-09/10 | **Unity 2022.3.43f1**(同 LTS 线) |

**3.11 与 4.1 处于同一条 Unity 2022.3 LTS 线,序列化文件格式版本一致(bundle format 22)。** 因此 bundle 二进制迁移不是「2019 → 2022 大版本跨越」,而是「2022.3 patch 级」差异——绝大多数 bundle 可以原样加载。

- 3.11.4 整合包**当前能跑**,说明 2622 个 bundle 已经通过了 2019→2022 的兼容性门槛(历史上老 mod 的 2019.4 时代 bundle 也被 2022.3.43f1 引擎成功加载)。
- 4.1 引擎仍是 2022.3.x,主要风险从「引擎版本」转移到三个次级因素:**mod DLL 必须重编译**(bundle 不会被加载的前提)、**0.16.1 → 0.16.9 游戏侧资产漂移**(shader 依赖、物品模板 Prefab 路径)、**MonoBehaviour 类名绑定**(4.1 反混淆改名)。
- 预估:**80~90% 的 bundle 可零改动直接迁移;10~20% 需要验证或重建**。真正的工作量在 177 个 mod 的 DLL 与 bundles.json 迁移,不在 bundle 二进制本身。

---

## 1. Unity 版本映射(3.11 vs 4.1 vs 历史)

### 1.1 证据链

| 证据 | 内容 | 来源 |
|---|---|---|
| SPT 3.11 对应 EFT | `0.16.1.3.35392`,2025-03-05 发布 | `wiki/SPT_311/FAQs_311.md`(官方) |
| SPT 4.0 对应 EFT | `0.16.9.0.40087`,2025-10-02 发布;官方同时托管 3.11.4(LTS,2025-09-01) | `wiki/FAQs_40.md`(官方) |
| 0.16.0.0 引擎升级 | 2024-12-26 升级到 **Unity 2022.3.43f1** | OpenCritic 补丁说明、Forbes |
| 0.14 时代引擎 | Unity **2019.4.x**(0.14.1 为 2019.4.39) | SlejmUr/TarkovServer(LTS 列表) |
| 0.15 时代引擎 | 仍为 2019.4.x(2022 升级原计划 0.15.4,被推迟到 0.16) | OpenCritic、Forbes |
| 4.0 调试环境 | dnSpy 调试包基于 **Unity 2022.3.43f1** 生成 | `wiki/modding/tutorials/debug_dnSpy.md` |
| 4.x 模组开发 SDK | EFT SDK 项目使用 **Unity 2022.3.43** | S3RAPH-1M/EscapeFromTarkov-SDK |

### 1.2 精确判定方法(实机验证)

官方调试指南给出的方法:`EscapeFromTarkov_Data/../UnityPlayer.dll` 的文件版本号(Details 选项卡)即引擎精确版本。bundle 文件头(`UnityFS` 魔数之后)也内嵌构建它的 Unity 版本字符串(除非构建时用了 `AssetBundleStripUnityVersion`)。**4.1 实机安装后应读一次 UnityPlayer.dll 版本确认**;若仍是 2022.3.43f1 则 3.11→4.1 的引擎差异为 0。

---

## 2. Unity Asset Bundle 兼容性规则(官方)

来源:Unity 官方文档 `AssetBundlesIntro`(2023.1 + 6000.x 版本文档口径一致)+ Unity 官方博客《Unity Asset Bundles tips and pitfalls》(2024-04)。

1. **向后兼容读取(官方语义:旧版构建 → 新版引擎)**:旧版 Unity 构建的 bundle「通常」能在新版引擎加载。版本跨度越大,兼容性越差;若对象序列化格式有重大变更,必须用新版 Unity 重建。
2. **无向前兼容**:新版构建的 bundle **不能**被旧版引擎加载。所以「把 4.1 的 bundle 拿回 3.11」不行,反过来可以。
3. **TypeTree 是跨版本加载的机制**:bundle 内嵌对象类型的「类型树」,引擎加载时用它做 safe binary read,把旧序列化字段映射到新布局。桌面平台默认写入 TypeTree。
   - 若用 `BuildAssetBundleOptions.DisableWriteTypeTree` 构建,任何跨版本序列化变更都会导致加载失败甚至崩溃。
   - 跨版本加载有性能开销(safe read 慢于原生读)。
4. **类型不稳定时需重建**:「Many Unity features 的类型相当稳定,minor 变更通常没问题;重大变更时旧数据无法在新版本产生预期结果,必须重建」。
5. **`AssetBundleStripUnityVersion`**:bundle 头默认内嵌编辑器版本号,会造成「编辑器 patch 升级 → 客户端全量重下」;可选项,普通 mod 通常不启用。

### 对 Tarkov 场景的推导

- 3.11.4 的 mod bundle 由模组作者用 **Unity 2022.3.43f1 + EFT SDK**(预制体 + Asset Bundle Browser)构建,含 TypeTree(默认)。4.1 引擎同为 2022.3.x → **格式版本一致,TypeTree 匹配,可直接加载**。
- 唯一需要警惕的引擎级例外是 **Shader**:shader 序列化在 Unity 版本间最不稳定(2019→2022 变化巨大;2022.3.x 内部稳定)。3.11 时代能跑的 shader bundle,4.1 大概率继续能跑(同一引擎线);但 0.16.1→0.16.9 期间 BSG 若改动过内置 shader 依赖(见 §5 风险),相关 bundle 会出问题。

---

## 3. SPT bundle 加载架构(3.11 与 4.1 对比)

### 3.1 两条加载路径

| 路径 | 机制 | 涉及资产类型 | 3.11 / 4.1 差异 |
|---|---|---|---|
| **A. 服务端分发(标准路径)** | mod 文件夹含 `bundles.json` + `bundles/` 目录;服务端启动时解析清单、计算 CRC32;客户端通过 `SPT.Custom/Utils/BundleManager` 拉清单 + 下载,游戏内 `AssetBundleManager` 按 item 模板的 `Prefab.path` 请求 | 自定义武器/物品模型、动画、音频、UI | **协议完全一致**;仅 `IsBundleMod` 元数据字段移除 |
| **B. 客户端本地加载** | BepInEx 插件自己 `AssetBundle.LoadFromFile()` 读自己插件目录下的 .bundle | shader、后处理、私有资产 | **与 SPT 版本无关**,纯 Unity 引擎兼容性 |

归档实证(archive/forge/mods):
- 路径 B 实例:`Picture-in-Picture-Disabler_2667_source/Patches/ReticleRenderer.cs` 与 `ScopeEffectsRenderer.cs` 用 `AssetBundle.LoadFromFile(pluginDir + "pipdisabler_*_shaders.bundle")` 加载 shader;`CWX-MegaMod_1454_source` 的 FSR 注释同样展示本地加载。归档里仅有的 2 个 .bundle 文件都是这条路径的 shader bundle。
- 路径 A 是自定义武器/物品的主流方式(WTT-CommonLib、ItemGen、CustomItemService 配方 12 的 Bundle 示例)。

### 3.2 4.1 服务端实现(源码实证,`SamMeow_SPT410_source_code`)

- `SPTStartupHostedService.cs`:`File.Exists(modPath + "bundles.json")` → `bundleLoader.LoadBundlesAsync(mod)`,**无 IsBundleMod 判断**(4.0→4.1 迁移文档「Bundles」节:该字段已删除)。
- `BundleLoader.cs`:解析 `bundles.json` 的 `manifest`(字段:`key` + `dependencyKeys`),文件路径 = `<mod>/bundles/{key}`;对每个 bundle 用 CRC32 计算并写入 `user/cache/bundleHashCache.json`。
- `BundleStaticRouter.cs`:`/singleplayer/bundles`(清单);`BundleDynamicRouter.cs`:`/files/bundle`;`BundleSerializer.cs`:按 `/bundle/{key}` 拆 key → `SendFileAsync`。
- `BundleManifestEntry.cs`:仅 `key`(必填)+ `dependencyKeys`(可空)。System.Text.Json 反序列化默认忽略未知字段 → **3.11 的 bundles.json 若含额外字段(`path`/`rc`/`crc` 等)也能被 4.1 读取**,只要 `key`/`dependencyKeys` 结构一致。

### 3.3 客户端实现(官方 modules 仓库,4.x 与 3.11 同协议)

`project/SPT.Custom/Utils/BundleManager.cs`(4.x 模块名;3.11 时代为 Aki.Custom,协议相同):

- `DownloadManifest()` → `GET /singleplayer/bundles`
- `DownloadBundle()` → `GET /files/bundle/{FileName}`,本地模式直接读 `SPT/{ModPath}/bundles/{FileName}`
- `ShouldAcquire()` → 本地模式直接返回;缓存模式 CRC32 比对(字段 `Crc`)
- 缓存目录 `SPT/user/cache/bundles/`

### 3.4 bundles.json 格式(3.x 实样)

dvize/SPT-TypeScript(3.x 时代)ModularNVG 的 `bundles.json` 实样:

```json
{
  "manifest": [
    {
      "key": "assets/content/items/mods/scopes/scope_all_flir_rs32_225_9x_35_60hz.bundle",
      "dependencyKeys": ["shaders", "cubemap", "assets/systems/effects/nightvision.bundle", "..."]
    }
  ]
}
```

- `key` 可含子路径(如 `staticspawns/my_objects.bundle`),映射到 `<mod>/bundles/<key>`;dependencyKeys 引用**游戏原生 bundle**(如 `shaders`、`cubemap`、`physicsmaterials.bundle`)——这正是 0.16.9 资产漂移的敏感点。
- **该格式与 4.1 的 `BundleManifestEntry` 逐字段一致,JSON 层面无需迁移。**

### 3.5 bundle 构建管线(作者侧,`wiki/modding/tutorials/WTT_Vol1.md`)

Blender(建模)→ Unity Hub + **EscapeFromTarkov-SDK(Unity 2022.3.43)** → 预制体 + `PreviewPivot` SDK 脚本 + Tarkov 材质(shader: Bumped Specular Specmap)→ Asset Label → Asset Bundle Browser Build。产物即 `bundles/` 下的 .bundle 文件。3.11 与 4.1 的构建管线**相同**(同一 SDK、同一引擎),作者为 4.1 更新的 mod 多数会直接复用旧 bundle,只重编译 DLL(ModernWeaponMods 仓库实证:源码仓库只含 DLL,「does not contain the mod's item data, 3D models, or textures」——资产随发布包原样走)。

---

## 4. 可用工具链(bundle 读取 / 提取 / 重建)

### 4.1 读取与提取(只读,批量友好)

| 工具 | 语言/形态 | Unity 2022.3 支持 | 用途 |
|---|---|---|---|
| **AssetStudio**(Perfare) | C# GUI | 支持 2022.x(含 2022.3) | 单 bundle 浏览/导出:mesh、texture、audio、animation、shader(部分 fork) |
| **AssetRipper** | C# GUI/CLI | 完整支持 2022.x | **批量**把 bundle 还原为 Unity 工程(预制体/材质/场景);反编译 shader;适合「提取 → 重建」流水线 |
| **UnityPy**(CabbageHard) | Python 库 | 支持 2022.x | **脚本化批量**:遍历 2622 个 bundle,读头部 Unity 版本 + 资产 ClassID 清单 → 生成风险分级 CSV(推荐的第一步自动化) |
| **UABE**(Unity Asset Bundle Extractor 3.0) | C# GUI | 支持 2022.x(3.0 beta) | 手术式编辑:替换资源、改字段,不重建整个 bundle |
| **uTinyRipper** | C# GUI | 老,被 AssetRipper 取代 | 不推荐 |

### 4.2 重建(写操作,必须用 Unity 编辑器)

**不存在「把 bundle 重新编译到另一 Unity 版本」的魔法工具。** 官方兼容机制是 TypeTree safe read;要真正重建,唯一正路是:

1. **AssetRipper** 提取为 Unity 工程(或直接用作者 SDK 工程);
2. 用 **Unity 2022.3.43f1** 编辑器(与 EFT 引擎一致;Unity Hub 免费下载)打开;
3. 按 WTT_Vol1 流程(预制体 → Asset Label → **Asset Bundle Browser → Build**)重新导出;
4. 更新 `bundles.json`(`key`/`dependencyKeys` 通常不变)。

### 4.3 自动化建议(本仓库可落地)

用 UnityPy 写一个 `bundle-audit.py`:
- 输入:177 个 mod 的 `bundles/` 目录(2622 个文件);
- 对每个 bundle 解析头部(魔数 `UnityFS`、Unity 版本串、bundle 格式版本)与资产 ClassID 集合(Texture2D=28、Mesh=43、AudioClip=83、AnimationClip=74、Shader=48、MonoBehaviour=114 等);
- 输出 CSV:`[mod, bundle, unityVersion, classIdSet, riskTier]`,riskTier 按「含 Shader → 高;含 MonoBehaviour → 中;纯 Mesh/Texture/Audio → 低」分级;
- 高/中风险名单进人工复核,低风险直接批量搬运。

---

## 5. 推荐迁移策略(三个阶段)

### 阶段 1:盘点与分级(纯只读)

1. 用 §4.3 的 UnityPy 脚本对 2622 个 bundle 批量审计,产出风险 CSV。
2. 对 177 个 mod 建立映射:哪些 bundle 走路径 A(bundles.json)哪些走路径 B(插件目录 LoadFromFile)——路径 B 的 bundle 随 DLL 走,迁移唯一前提是 DLL 重编译。
3. 确认 4.1 实机 `UnityPlayer.dll` 版本(§1.2)。

### 阶段 2:Drop-in 直测(主路线,零重建)

1. 先完成 177 个 mod 的 **DLL 迁移**(4.1 客户端反混淆 + 服务端 DI/API 变化,见 findings 001 与 `SPT_41/Server_40_to_41.md` 迁移文档——这是真正的阻塞项);
2. bundles.json 原样保留,`bundles/` 原样复制到 4.1 的 mod 目录;
3. 启动 4.1 服务器,检查日志:
   - `BundleLoader` 的 `Could not load bundle manifest` / `Could not find bundle` 警告(缺文件/清单不匹配);
   - `user/cache/bundleHashCache.json` 生成情况(CRC32 校验是否全绿);
   - 4.0.5 曾修 `duplicate bundle hashes`、4.0.11 修过 `bundle loading relating to modded headless clients`——服务端 bundle 处理在 4.0 早期有 bug,建议用 **4.0.5+ / 4.1** 基线。
4. 进游戏逐 mod 验证:自定义武器贴图/模型/音效/UI 是否出现;紫皮(紫色模型)= shader 失效信号。

### 阶段 3:定向重建(仅对失败 bundle)

1. 失败清单 → AssetRipper 提取 → 2022.3.43f1 + EFT SDK 重建;
2. shader bundle 优先排查(最不稳定类型);
3. 检查 dependencyKeys 指向的游戏原生 bundle 在 0.16.9 是否仍存在(0.16.1→0.16.9 资产改名/移除会造成「依赖缺失」类失败,此时**无需重建 bundle,只需改 bundles.json 的 key**);
4. 涉及 MonoBehaviour 的 bundle 检查类名绑定(4.1 反混淆改了游戏类名;SDK 脚本如 PreviewPivot 本身不参与运行时绑定,通常无害)。

---

## 6. 风险评估

### 6.1 结论数字

| 风险层 | 占比(估算) | 说明 |
|---|---|---|
| **零改动直接迁移** | **~75-85%** | 纯 Mesh/Texture/Audio/UI 预制体 bundle;已通过 3.11 的 2022.3 兼容门槛,4.1 引擎同线 |
| 需验证、大概率可过 | ~10-15% | 依赖游戏原生 bundle(shaders/cubemap/physicsmaterials)的武器/物品 bundle;0.16.1→0.16.9 资产漂移的敏感带;通常只需改 bundles.json 而非重建 |
| 需人工重建/作者更新 | ~3-8% | shader bundle、含 MonoBehaviour 的脚本化预制体、走路径 B 且 DLL 无 4.1 版的 mod |

**汇总:约 80-90% 的 bundle 可自动/半自动迁移(复制即用),10-20% 需验证或重建。** 迁移成败不取决于 bundle 格式,而取决于 177 个 mod 的 DLL 兼容性与作者更新情况。

### 6.2 关键风险点(按优先级)

1. **[高] mod DLL 是前置条件**:bundle 由 mod 加载,mod 在 4.1 无法运行则 bundle 无从加载。4.1 客户端反混淆 + 服务端重写导致所有 3.11 mod 必须更新——这是迁移的硬门槛,与 bundle 无关。
2. **[中] 0.16.1 → 0.16.9 游戏资产漂移**:dependencyKeys 引用的原生 bundle 改名/移除;物品模板 `Prefab.path` 变更;内置 shader 属性改名。症状:依赖缺失报错、紫皮、黑模。处置:改清单/重建 shader。
3. **[低] 引擎 patch 差异**:4.1 若 UnityPlayer.dll 仍为 2022.3.43f1,此项为零;若 BSG 后续升级 2022.3.x patch,TypeTree safe read 兜底,代价是加载性能略降。
4. **[低] 4.0 早期服务端 bundle bug**:duplicate bundle hashes(4.0.5 修)、headless client bundle loading(4.0.11 修)。用 4.1 基线无此问题。

### 6.3 与常见误区对照

| 误区 | 事实 |
|---|---|
| 「3.11 是 0.14/0.15 时代,升级 = 2019→2022 大迁移」 | 3.11.4 = EFT 0.16.1.3 = **Unity 2022.3.43f1**,与 4.1 同线;2019→2022 的迁移发生在 **3.10 → 3.11**(SPT 侧) |
| 「bundle 必须重建才能换 Unity 版本」 | 官方规则:旧版 bundle 在新版引擎**向后兼容**(TypeTree 机制);只有 shader 等不稳定类型或跨大版本才需重建 |
| 「bundles.json 是 4.1 新格式,3.11 的不能用」 | 3.x 与 4.1 的 manifest 字段一致(`key`+`dependencyKeys`),4.1 反序列化忽略未知字段,直接可用;唯一变化是删了 `IsBundleMod` 元数据字段 |

---

## 7. 遗留问题 / 后续动作

1. [ ] 用 UnityPy 对 2622 个 bundle 跑一次批量审计,产出风险 CSV(§4.3)——建议作为独立 wayfinder 任务。
2. [ ] 4.1 实机装好后读 `UnityPlayer.dll` 版本号,锁定精确引擎版本(§1.2)。
3. [ ] 收集 177 个 mod 在 Forge 归档中的 4.1 更新状态(哪些作者已发布 4.x 版,可复用其新 bundle 或旧 bundle)。
4. [ ] 抽查 3-5 个路径 A 的自定义武器 mod,对比其 3.11 bundles.json 与 4.x 发布版 bundles.json 的 diff,量化清单层面的实际漂移。
5. [ ] 排查归档中 shader bundle 数量(现仅确认 2 个,但用户整合包 2622 个文件中占比未知),决定 shader 专项处置投入。

---

## 附:主要来源

- 官方文档:wiki.sp-tarkov.com(FAQs_311 / FAQs_40 / Updating_SPT / debug_dnSpy / WTT_Vol1 / Server_40_to_41)
- 源码:SamMeow_SPT410_source_code(`Libraries/SPTarkov.Server.Core/{Loaders,Callbacks,Routers,Services,Models.Spt.Bundles}`)
- 客户端:github.com/sp-tarkov/modules(`project/SPT.Custom/Utils/BundleManager.cs`)
- Unity 官方:docs.unity3d.com Manual/AssetBundlesIntro(2023.1/6000.x);unity.com/blog《Unity Asset Bundles tips and pitfalls》
- 社区:OpenCritic/Forbes(0.16.0.0 = Unity 2022.3.43f1)、whenwipe.com(版本史)、SlejmUr/TarkovServer(0.14 = 2019.4)、S3RAPH-1M/EscapeFromTarkov-SDK、cwstp/ModernWeaponMods、WTT-CommonLib(market.dev)、dvize/SPT-TypeScript(3.x bundles.json 实样)
