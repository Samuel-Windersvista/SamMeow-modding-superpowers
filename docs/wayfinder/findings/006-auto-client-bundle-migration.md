# 3.11 客户端 Mod + Bundle 自动化迁移可行性

> 状态:侦察完成(2026-08-05)| 适用版本:[3.11.4 -> 4.1]| 目的:评估"客户端 DLL 自动反混淆改名 + bundle 自动验证/重建"的自动化边界
> 方法:实机读 3.11/4.1 游戏 Assembly-CSharp.dll、扫描 Life_in_Norvinsk_v0.3.2 全部 132 个 mod DLL 与 2622 个 bundle、核对 Class_Name_Mappings 覆盖度
> 素材:wiki/SPT_41/(Client_40_to_41、modding/client/Class_Name_Mappings)、curated/migration/(client-mod-311-to-41、bundle-311-to-41)、docs/(bundle-difference-audit、exploration-bundle-and-3d-pipeline、findings/003)、archive/forge/mods/(812/1342/2667 等 client mod 源码)、本地两个游戏实机

---

## 0. 结论先行(Executive Summary)

**客户端 DLL 自动反混淆改名:技术上部分可行,但"仅靠 9240 条映射表做 IL 级改名"这条捷径走不通。** 实机证据:

1. **映射表本身不完整**:3.11 游戏程序集里存在的 3920 个 GClass 名字,映射表只覆盖 3310 个(84.4%);本整合包 132 个 mod DLL 实际引用的 330 个 GClass 名中 **69 个(21%)不在映射表里**,而这些名字确实存在于 3.11 游戏程序集(GClass1040、GClass1341、GClass2202、GClass685...)。映射表不是"完整对照",照表机械替换必然漏。
2. **映射表只覆盖"类名",不覆盖方法名/字段名**:3.11 程序集还有大量混淆的 `method_NNN`(216 处)、`smethod_NNN`(135 处)、`func_NNN`(28 处)、`float_N/int_N/bool_N` 等字段名(数百处),4.1 全部归零。33/132 个 mod DLL 引用了混淆方法名,16/132 引用了混淆字段名——**这些没有映射表,是改名自动化的硬缺口**。
3. **社区官方口径 = 重新编译,不是改名**:wiki 明确"Every 4.0 client mod needs rebuilding against 4.1"(4.0/4.1 官方迁移文档),"The build errors will point you at each one"——即靠编译器报错逐个修,不存在公开的改名工具链。本仓库 curated/migration 文档也判定"无源码 DLL 无法可靠自动化"。
4. **映射表是 4.0->4.1 的,不是 3.11->4.1 的**:3.11 与 4.0 同属混淆时代(0.16.1 vs 0.16.9),GClass 编号在两版间是否逐一对齐**未经验证**——若编号漂移,映射表连"照抄"的前提都不成立,需要先做 3.11->4.0 对齐。

**Bundle 侧结论(沿用 003 号 findings + 本次补充)**:

- **2622 个 bundle 的脚本绑定对本包 0 风险**(0 个引用混淆类名,PreviewPivot/LoddedSkin/HotObject 两版同名)——bundles.json + bundles/ 可批量复制。
- **shader 是真风险但可自动预检**:3.11 与 4.1 的 shader bundle 中 `Bumped Specular SMap`/`Specular SMap` 等 EFT 自定义 shader 名字两边都存在(14/17 处相同),但 4.1 的 shaders bundle 是 3.11 的 **10 倍大**(36MB vs 378MB),内容有实质差异;shader 依赖可用 UnityPy 从 bundle 提取并与游戏 shader bundle 的名字表比对,这是可脚本化的。
- **自动化率预估**:bundle 迁移 80-90% 自动/半自动;客户端 DLL **全自动改名率约 30-40%**,剩余需要源码重编译或人工(映射缺口 21% + 方法/字段名缺口 + Harmony 反射目标)。

---

## 1. 客户端 DLL 自动迁移(研究问题 1)

### 1.1 地面事实(实机二进制验证)

| 检查项 | 3.11 | 4.1 |
|---|---|---|
| Assembly-CSharp.dll 大小 | 15,243,264 B(混淆) | 15,994,432 B(反混淆) |
| `GClass680` 出现 | 1 次 | 0 次 |
| `ABotProfileCreator` 出现 | 2 次 | 2 次 |
| 命名空间(Eft.UI/EFT.Player/EFT.InventoryLogic) | 有(部分稳定类) | 有(全部真实) |
| `method_NNN` 混淆方法名 | 216 处 | 0 处 |
| `smethod_NNN` | 135 处 | 0 处 |
| `func_NNN` | 28 处 | 0 处 |
| `float_N/int_N/bool_N` 混淆字段名 | 数百处 | 基本归零 |

结论:4.1 是**全量反混淆**(类名+方法名+字段名),不只是类名改名。映射表只覆盖了类名层。

### 1.2 映射表覆盖度实测(核心发现)

**映射表规模**:`Class_Name_Mappings.md` 实际 5961 行数据(GClass 3339 / GStruct 307 / GInterface 429 / Class 263 / 其余扁平别名 1623)。任务描述里的"~9240"与文件实际不符——**实际只有 ~5960 条**(且 4.0 部分改名别名也算进去了)。

**对 3.11 游戏程序集的覆盖**:

```
3.11 Assembly-CSharp 中的不同 GClass 名: 3920 个
映射表覆盖:                                 3310 个 (84.4%)
映射表缺失:                                  610 个 (15.6%)
缺失样例: GClass1008 GClass1020 GClass1032 GClass1040 GClass1050 ...
```

**对本整合包 132 个 mod DLL 实际引用的覆盖**:

```
132 个 DLL 引用的不同 GClass 名: 330 个
映射表覆盖:                       261 个 (79.1%)
映射表缺失:                        69 个 (20.9%)
缺失且已证实存在于 3.11 游戏程序集: 69/69 个 (100%)
缺失样例: GClass1040 GClass1050 GClass1202 GClass1341 GClass1806 GClass2202 GClass3168...GClass685 GClass825
```

**方法/字段名缺口**:

```
132 个 DLL 中引用混淆方法名(method_NNN/smethod_NNN): 33 个 DLL (25%)
132 个 DLL 中引用混淆字段名(float_N/int_N/gparam_N):  16 个 DLL (12%)
涉及大户: friendlyPMC(24)、Tyfon.UIFixes(21)、RealismMod(17)、SAIN(9)
```

### 1.3 "找 GClass680 换成 ABotProfileCreator"为什么不够

即使有完整类名映射,IL 级改名仍有四层障碍:

| 障碍 | 说明 | 实机证据 |
|---|---|---|
| **A. 映射表不全** | 21% 的实际引用没有映射条目 | §1.2 |
| **B. 方法/字段名无映射** | Harmony 目标 `AccessTools.Method(typeof(GClass3687), "method_2")` 里方法名也是混淆的;4.1 里 `method_2` 不存在 | 3.11 有 216 处 method_NNN,4.1 为 0;Picture-in-Picture-Disabler_2667_source/PiPDisabler.cs 等真实代码 `AccessTools.Method(typeof(GClass3687), "method_2")` |
| **C. 反射/Harmony 目标解析** | `PatchConstants.EftTypes.Single(t => t.GetMethod("GetMoneySums") != null)` 这类**按签名找类型**的写法反而不怕改名(UI-Fixes_1342_source/R.cs 大量使用);但 `Type.GetType("GClassXXX, Assembly-CSharp")` 字符串引用(Keep-Starting-Gear_2470_source)需要改字符串字面量 | 81/132 DLL 用 AccessTools,19/132 用 [HarmonyPatch] 特性;二者目标解析机制不同 |
| **D. 3.11->4.0 编号对齐未验证** | 映射表左列是"4.0 name";3.11 与 4.0 的 GClass 编号是否一致,无文档、无对照 | 任务上下文假设"3.11 的 GClass680 == 4.0 的 GClass680",但 SPT 3.11=EFT 0.16.1、4.0=EFT 0.16.9,中间 patch 升级可能重排混淆编号 |

### 1.4 工具可行性评估

| 工具 | 能做什么 | 不能做什么 | 结论 |
|---|---|---|---|
| **Mono.Cecil**(NuGet 有缓存,dotnet SDK 10 可用) | 枚举 TypeReference/TypeDefinition、改类名、改字符串字面量、重写 IL | 不知道 GClass685 该映射成什么(表里没有);不知道 `method_2` 该映射成什么(无方法映射) | **能做骨架,做不了决定** |
| **dnSpy / ILSpy**(机器未安装 dnSpy.exe) | 反编译为源码级 C#,半自动修正后重新编译 | 反编译产物仍带混淆名,替换逻辑与 Mono.Cecil 相同 | 半自动路径,可用于单 mod 处理 |
| **ILRepack / Assembly.Load 注入** | 与改名无关 | — | 不适用 |
| **dotnet SDK 10 + Roslyn 重编译** | 对**有源码**的 mod:换 4.1 引用 -> 按映射表 AST 替换 -> 编译错误驱动修正 | 需要源码(Forge 归档只有 18 个 source clone,大部分 mod 只有 release DLL) | **有源码 mod 的可行路径** |

### 1.5 社区怎么做(4.0 -> 4.1 client mod 迁移)

- **官方口径**(wiki.sp-tarkov.com Client_40_to_41):"Every 4.0 client mod needs rebuilding against 4.1... The build errors will point you at each one."——即**下载 4.1 程序集、在源码工程里换引用、重新编译**,编译器报错驱动修名字。不存在"改 DLL"的官方路径。
- 社区主力 mod(SAIN、friendlyPMC、WTT 系列)都是**作者自己发新版重编译**,不是用户侧改名。
- Forge 归档内 18 个 source clone 中,客户端侧(812 LootingBots、1342、2667 PiPDisabler、2470 KeepStartingGear、1454 CWX)展示了三种引用风格:直接 typeof(GClass)(需改名)、反射按成员签名找类型(天然免疫改名)、Type.GetType 字符串(需改字符串)。**第二种风格的 mod 理论上改名需求很低**。
- **结论:没有社区工具做自动改名;事实标准是"作者重编译 / 有源码者自行重编译"。**

### 1.6 客户端 DLL 迁移判定

| 场景 | 自动化率 | 路径 |
|---|---|---|
| 有源码 + 简单类引用(<=5 个 GClass) | ~85-95% | 映射表 AST 替换 + dotnet build 编译错误驱动 |
| 有源码 + 反射/Harmony 复杂目标 | ~50-70% | 同上,但方法名/字段名缺口需人工或 LLM 判定 |
| 无源码(只有 DLL)+ 类引用简单 | ~30% | Mono.Cecil 机械改名,但 21% 映射缺口 = 必然部分失败,失败点无编译错误兜底(运行时报错) |
| 无源码 + 方法名混淆 | ~5-15% | method_NNN 无映射,基本不可自动 |
| **本包实际** | **全自动约 30-40%** | 89/132 DLL 含 GClass 引用,其中 79% 类名有映射、方法名缺口 25% |

---

## 2. Bundle 自动验证(研究问题 2)

### 2.1 UnityPy 可行性

| 检查项 | 可脚本化? | 说明 |
|---|---|---|
| 头部 Unity 版本 + bundle format | 是 | 魔数 UnityFS + 版本串 + format(实测 3.11/4.1 游戏 bundle 均 format 8;mod bundle 2019.4.39f1 = format 7) |
| 资产 ClassID 清单(Texture2D=28/Mesh=43/Shader=48/MonoBehaviour=114) | 是 | 风险分级 CSV 的基础(003 号 findings §4.3 已有方案) |
| **脚本类引用名**(MonoBehaviour 的 m_Script 指向的类名) | 是 | **本次实测 0/2622 引用混淆类名**,此检查可自动化兜底其他整合包 |
| **shader 依赖提取** | 部分 | bundle 内 shader 资产名字可从 Shader 资产或依赖表读出;与游戏 shader bundle 名字表比对可行 |
| 实机加载是否成功 | 否 | 只能靠启动游戏 + 服务端日志(CRC32/bundleHashCache.json) |

**注意**:UnityPy 当前机器未安装(`pip install unitypy` 可用)。2239/2622 个 bundle 是 2019.4.39f1(format 7)构建,UnityPy 对 2019.4 的支持成熟。

### 2.2 shader 依赖比对(新证据)

| 项 | 3.11 | 4.1 |
|---|---|---|
| `StreamingAssets/Windows/shaders` 大小 | 36,311,840 B | 378,084,672 B(**10.2x**) |
| bundle format | 8 | 8 |
| Unity 版本串 | 2022.3.43f1 | 2022.3.43f1 |
| `Bumped Specular SMap` 出现次数 | 14 | 14 |
| `Specular SMap` 出现次数 | 17 | 17 |
| `cubemaps` 大小 | 15,745,712 B | 15,745,712 B(相同) |
| `physicsmaterials.bundle` | 4,128 B | 4,128 B(相同) |

- 关键 EFT 自定义 shader 名字**两边一致** -> 大部分 shader 依赖的**名字解析**不会断。
- 但 4.1 shaders bundle 是 3.11 的 **10 倍大**,说明 shader 集合/变体有实质扩充,依赖同一 shader 名但**语义可能变化** -> 低概率但必须实机验紫色模型。
- **自动化路径成立**:UnityPy 读每个 mod bundle 的 shader 依赖名集合 -> 与 4.1 游戏 `shaders` bundle 的 shader 名集合比对 -> 名字缺失/漂移的标红。
- 本包 2533 个 manifest key 中 **2421 个(95.5%)依赖 `shaders` + `cubemaps` + `physicsmaterials.bundle`** 这三个游戏原生 bundle -> 比对覆盖面极广。

### 2.3 bundle 验证结论

- **复制层自动**:bundles.json(manifest 字段 key+dependencyKeys 与 4.1 BundleManifestEntry 一致,未知字段被忽略)+ bundles/ 文件 -> 批量复制,IsBundleMod 字段删除不构成障碍。
- **验证层自动**:UnityPy 批量审计(版本头 + ClassID + 脚本类名 + shader 依赖名)可做,输出红黄绿 CSV,把人工范围压缩到黄+红。
- **最终判定人工**:模型显示/紫色模型/材质参数必须进游戏看。

---

## 3. Shader 修复自动化(研究问题 3)

| 环节 | 可脚本化? | 障碍 |
|---|---|---|
| AssetRipper 提取 bundle -> Unity 工程 | 半自动 | AssetRipper CLI 可跑,但 shader 反编译 + 材质重建有损耗 |
| Unity 2022.3.43f1 + EFT SDK 打开工程 | 需要 GUI/许可 | 2022.3 免费版可用;EFT SDK(预制体/材质/PreviewPivot 脚本)需要从作者工程或 4.1 游戏还原 |
| **Unity batch mode `-executeMethod BuildAssetBundles`** | **是** | 官方支持无头构建;前提是工程内容完整(资源导入 + 标签 + Asset Bundle Browser 配置都已就位)——**这一步本身可脚本化,但它前面的资产准备不可** |
| 重建后更新 bundles.json | 是 | key/dependencyKeys 通常不变 |

**判定**:
- "重建 bundle"的最后一步(BuildAssetBundles)可无头自动化,但 **80% 的工作量在它之前**(AssetRipper 还原质量、EFT SDK 脚本恢复、材质引用修复),这些需要 Unity 编辑器 + 人工/半自动。
- 本包 **85.4% 的 bundle 是 2019.4 构建**,在 4.1(2022.3)里靠 TypeTree 向前兼容;**除非实机出现紫色模型,不建议主动重建**。重建只针对失败清单(预期 <5%)。
- WTT 文档排障口径一致:"Purple Model? Reassign textures shaders in Unity"——即人工在编辑器里重指派,不是纯脚本。

---

## 4. 流水线设计(研究问题 4)

```
Stage 1  识别 mod 类型
         输入: MO2 mods/ 目录(194 个 overlay,177 真实 mod)
         规则: 有 BepInEx/plugins/*.dll -> client;有 user/mods/*/package.json -> server;
              有 bundles.json/bundles/ -> bundle mod;同时存在 -> 配对 mod
         实测分布: client-only 72 / server-only 76 / 配对 27 / 无代码 19
         自动化: 100% (文件系统规则)

Stage 2  客户端 DLL 处理
         有源码(Forge 18 个 source clone 中的 client 部分): 映射表 AST 替换 + dotnet build
         无源码: Mono.Cecil 机械改名 -> 映射缺口标记 -> 失败清单交人工
         实测: 89/132 DLL 含 GClass;映射覆盖 79%;方法名缺口 25%
         自动化: 30-40% 全自动,50% 半自动(需编译/人工复核),10-20% 需作者更新

Stage 3  bundle 复制
         bundles.json 原样 + bundles/ 原样复制到 4.1 mod 目录
         自动化: 100%

Stage 4  bundle 验证(UnityPy)
         版本头 / ClassID 集合 / MonoBehaviour 脚本类名 / shader 依赖名 vs 4.1 游戏 shader bundle
         输出 红黄绿 CSV(本包实测 0 红脚本绑定;shader 需看比对结果)
         自动化: 100% 出报告,判定人工

Stage 5  shader 修复(仅失败 bundle)
         AssetRipper 提取 -> Unity 2022.3 + EFT SDK -> 人工/半自动修复材质 -> batch mode 重建
         预期 <5% 的 bundle 走到这
         自动化: 20% (只有最后 Build 一步可脚本)

Stage 6  运行时验证
         启动 4.1 服务端: 日志 bundle 加载 + bundleHashCache.json CRC32
         进游戏: 模型显示/紫色/材质/附件挂点/PreviewPivot
         自动化: 服务端日志可自动,游戏内目视人工
```

---

## 5. 现实自动化率(研究问题 5,Life_in_Norvinsk_v0.3.2 实测)

### 5.1 包结构实测(本次扫描)

| 指标 | 数值 |
|---|---|
| MO2 overlay 目录 | 194(含 separator) |
| 含 BepInEx(客户端) | 99 |
| 含 user/ mods(服务端) | 103(76 server-only + 27 配对) |
| client-only / server-only / 配对 / 无代码 | 72 / 76 / 27 / 19 |
| 非框架 mod DLL(BepInEx 下) | **132**(分布在 92 个 mod) |
| 含 GClass 字符串的 DLL | 89 / 132 (67%) |
| 引用 HarmonyLib 的 DLL | 93 / 132 (70%) |
| 引用混淆方法名的 DLL | 33 / 132 (25%) |
| 引用混淆字段名的 DLL | 16 / 132 (12%) |
| 含 .bundle 文件的 mod | 47 |
| 含 bundles.json 的 mod | 38 |
| manifest 声明的 bundle key | 2533(文件 2622) |
| 依赖 shaders/cubemaps/physicsmaterials 的 key | 2421 / 2533 (95.5%) |

### 5.2 迁移分类与自动化率

| 类别 | 数量 | 全自动? | 说明 |
|---|---|---|---|
| 纯 bundle 资产(无 DLL) | ~30 mod | **~95%** | 复制 + UnityPy 验证 |
| 客户端 DLL(89 个含 GClass) | ~60-70 mod | **30-40%** | 映射表缺口 21% + 方法名缺口 25% |
| 其中无源码 DLL | 大部分 | ~20-30% | Mono.Cecil 机械改名 + 失败清单人工 |
| 有源码 client mod | 少数 | 50-70% | AST 替换 + 编译驱动 |
| bundle + shader 重建 | <5% 的 bundle | 20% | 只有 Build 一步可脚本 |

**整体判定**:bundle 侧 ~80-90% 自动/半自动;客户端 DLL 全自动 ~30-40%;整个 pack 的"完全无人干预"迁移率 **~35-45%**,其余需要编译反馈循环(半自动)或人工/作者更新。

---

## 6. 自动化阻断项(Automation Blockers,按严重度)

1. **[硬阻断] 映射表不完整**:3920 个游戏 GClass 名只映射 3310 个;本包实际引用的 330 个中 69 个(21%)无映射。**任何"照表替换"的自动化在缺口处静默产出坏 DLL**(无编译错误兜底)。
2. **[硬阻断] 方法名/字段名混淆无映射**:3.11 的 method_NNN/smethod_NNN/func_NNN/float_N/int_N 等(数百处)在 4.1 全部改名,映射表完全没有方法/字段层条目。33/132 个 DLL 受影响。
3. **[中] 3.11 vs 4.0 编号对齐未验证**:映射表左列是 4.0 名,3.11(0.16.1)与 4.0(0.16.9)的混淆编号是否一致无证据;若漂移则映射表对 3.11 不适用,需先做 3.11->4.0 对齐(需要 4.0 游戏实机程序集做比对)。
4. **[中] 反射目标解析多样性**:直接 typeof(改名)、Type.GetType 字符串(改字符串)、按签名找类型(免疫改名)、AccessTools.TypeByName(改字符串)四种并存;单一改名策略无法覆盖,需要按引用模式分类处理。
5. **[中] 无源码占比**:Forge 归档 95 个 release 中仅 18 个 source clone;client 侧源码更少。无源码 = 无编译错误兜底 = 改名错误静默。
6. **[低-中] 方法级 API 变化**:即使类名对齐,0.16.1 -> 0.16.9 期间游戏内部方法签名/行为可能变化,Harmony patch 目标方法存在性需运行时验证。
7. **[低] UnityPy 未安装**:审计脚本前置依赖,一次性 pip install。
8. **[低] 4.1 shaders bundle 膨胀 10 倍**:名字一致但语义可能漂移,必须实机抽查武器包(WTT Armory 627 bundles / EpicRangeTime 476 优先)。

---

## 7. 建议下一步(按 ROI)

1. **验证 3.11->4.0 编号对齐**(阻断项 3):若可行,先解决"映射表对 3.11 是否适用"这一前提。需要 4.0 程序集或作者确认。
2. **补映射表缺口**:对 69 个缺失 GClass 名,用"成员签名匹配"法(4.1 程序集里找方法签名一致的类型)自动推断候选新名,人工确认后回填 KB——这同时服务所有未来 3.11 包迁移。
3. **写 UnityPy bundle-audit 脚本**(003 号 findings §4.3 已立项):版本头 + ClassID + 脚本类名 + shader 依赖名 -> 红黄绿 CSV。
4. **POC 选 3 个 client mod**(建议 Tyfon.UIFixes 21 处 GClass + 方法名复杂、一个纯类引用简单 mod、一个无源码 DLL)实测 Mono.Cecil 改名管线,量化真实失败率。
5. **对 4.1 实机启动做 bundle 加载验证**(CRC32 + bundleHashCache),优先 WTT Armory 抽查 shader。

---

## 附:本次实测数据来源

- 3.11 游戏:`E:\Game\EFT_Offline\SPT_3114\`(Assembly-CSharp.dll 15,243,264 B,混淆)
- 4.1 游戏:`E:\Game\EFT_Offline\SPT_410\`(Assembly-CSharp.dll 15,994,432 B,反混淆)
- 整合包:`E:\Game\EFT_Offline\Life_in_Norvinsk_v0.3.2\mods\`(194 overlay,132 个 mod DLL,2622 个 bundle)
- 映射表:`knowledge/spt-kb/wiki/SPT_41/modding/client/Class_Name_Mappings.md`(5961 行)
- 既有研究:findings/003-bundle-upgrade-analysis.md、docs/bundle-difference-audit.md、curated/migration/client-mod-311-to-41.md
