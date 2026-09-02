---
version: [3.11, 4.1]
domain: both
topic: migration
source: curated
---

# Bundle 兼容性知识：SPT 3.11 -> 4.1.2（实测验证 2026-08-08）

> 状态：已提炼（2026-08-08）| 来源：3114 整合包迁移实战（M2 阶段 bundle 全量检查）
> 方法：UnityPy 深读 19 个已部署 bundle + BetterRearSights 38 个 bundle 双版本 MD5 对比
> 结论先行：**从 3114 原包同步的 bundle 无需升级适配**——原包本身就跑在 Unity 2022.3.43f1
> （与 4.1.2 相同），且其 bundle 大部分是 2019.4.x 编译，Unity 向后兼容加载已由原包验证。

---

## 1. 环境事实（实测）

| 项 | 值 | 证据 |
|----|-----|------|
| SPT_410（4.1.2）Unity 版本 | **2022.3.43f1** | `UnityPlayer.dll` FileVersion 2022.3.43.8735101 |
| 3114 原包 Unity 版本 | **2022.3.43f1**（完全相同） | 原包 `UnityPlayer.dll` 同版本 |
| 3114 原包 bundle 编译版本 | 混合：**2019.4.39f1 / 2019.4.34f1 / 2022.3.43f1** | bundle 头 `UnityFS` 后字符串 |
| Forge 源码 bundle 编译版本 | 2019.4.39f1（与原包相同） | BetterRearSights 38 个全同 |

**核心推论**：Unity 5.0 之后 AssetBundle **向后兼容**（新版本 Unity 可加载旧版本 bundle）。
原包（2022.3.43f1）里跑着大量 2019.4.x 编译的 bundle 且是验证过的可玩整合包——
**2019.4 bundle 在 2022.3 下可用是既成事实**，不需要"升级重打包"。

## 2. bundle 内部结构（UnityPy 深读实测）

### 2.1 UnityFS 头
```
"UnityFS\0" + 格式版本(uint32) + flags(byte: bit0-5 = 压缩类型: 0=none 1=lzma 2=lz4 3=lz4hc)
+ unity_version 字符串 + 其他元数据
```
实测：EFT mod bundle 多为 `UnityFS fs=0 flags=7`（flags=7 表示 LZ4HC 压缩 + 其他标志）。

### 2.2 对象类型分布（两类典型）

**3D 模型 bundle（HeliCrash sikorsky_uh60，2019.4.34f1）**：
```
GameObject:76 Transform:76 Mesh:39 MeshCollider:26 MonoBehaviour:25 MeshRenderer:24
MeshFilter:24 Texture2D:13 ParticleSystem:8 Material:8 AudioClip:8 ...
MonoScript:7
```
**武器配件 bundle（BetterRearSights barrel，2019.4.39f1）**：
```
GameObject:9 Transform:9 MonoBehaviour:8 MonoScript:6 Texture2D:4 MeshFilter:2 Mesh:2
MeshRenderer:2 Material:2 LODGroup:1 BoxCollider:1
```

### 2.3 关键观察：EFT mod bundle 不内嵌 shader
两个深读 bundle 的 Material **都引用游戏内置 shader**（bundle 内无 Shader 对象）。
→ Material 的 shader 通过**名称绑定**到游戏资源（如 EFT/Standard 系列）。
→ 跨 Unity 版本时若游戏 shader 名变化，材质可能粉红（风险低——EFT 0.14→0.16 shader 名稳定）。

### 2.4 脚本引用（MonoScript / MonoBehaviour.m_Script）——真正的风险点
bundle 内 MonoBehaviour 的 `m_Script` 引用两种形式：
- **fileID=0 + pathID** → 引用 bundle **内嵌 MonoScript**（安全，随 bundle 走）
- **fileID!=0** → 引用**外部程序集**（Assembly-CSharp.dll / 或 mod 自己的 DLL）

**外部引用是唯一需要核对的地方**：绑定键 = 程序集名 + 命名空间 + 类名（m_ClassName/m_AssemblyName/m_Namespace）。
重编译 mod DLL 后必须确认这三者不变，否则 prefab 加载报 "Script missing"。

## 3. 已部署 bundle 检查结论（2026-08-08，26 个全过）

| bundle | Unity 版本 | 脚本引用 | 结论 |
|--------|-----------|---------|------|
| StashSearch stashsearch.bundle | 2019.4.39f1 | 全 UnityEngine.UI/TMP（系统 UI） | 安全 |
| accessibilityindicators.bundle | 2022.3.43f1 | 全系统 UI | 安全 |
| GamePanelHUD ×6 | 2022.3.43f1 | `GamePanelHUDHealth.dll` 3 类（HealthHUDController/HealthHUDView/HealthUIView） | **核对通过**：4.1 重编译 DLL 类名/命名空间完全一致 |
| Waypoints navmesh ×10 | 2019.4.39f1（bigmap 2022.3.43f1） | 纯 NavMeshData 数据（无脚本） | 安全 |
| HeliCrash sikorsky | 2019.4.34f1 | Assembly-CSharp 内置组件（BallisticCollider/Door/LootableContainer/VolumetricLight） | 安全（游戏内置类型 4.1 仍存在） |
| KmyTarkovApi config | 2022.3.43f1 | `KmyTarkovConfiguration.dll` 9 类（Config/ConfigColor/ConfigFloat/ConfigHeader 等） | **核对通过**：4.1 DLL（v1.5.0.0）类名/命名空间全匹配 |
| BRNVG ×6（夜视模型） | 2019.4.x | Assembly-CSharp 内置（Dress/NightVisionDevice/PreviewPivot）+ 纯 Texture2D | 安全 |

**双版本对比**（Forge 源码 vs 3114 原包）：BetterRearSights 38 个 bundle **MD5 完全相同**——
作者只更新代码不更新 bundle 是常态。**源码仓库里的 bundle 与原包一致时，直接用原包的即可**。

## 3.5 作者差异特征（精炼观察，2026-08-08 实测）

不同作者的 bundle 打包习惯差异明显，可作为快速判断依据：

| 作者系 | 特征 | 典型 |
|--------|------|------|
| DrakiaXYZ | 2019.4.39f1 + flags=7（LZ4HC）；navmesh 纯数据 / UI 纯系统组件；**bundle 不引用自身 DLL**（逻辑运行时挂载） | Waypoints、StashSearch、TaskListFixes 系 |
| kmyuhkyuk（Kmy 系） | 2022.3.43f1 + flags=8；**bundle 引用自身 DLL 的 UI 类**（程序集名匹配即安全，需核对类名）；配置 UI bundle 巨大（458 MonoBehaviour） | KmyTarkovApi、GamePanelHUD |
| 模型系（HeliCrash/BRNVG/武器包） | 2019.4.x + 大量 GameObject/Mesh/MeshRenderer；**引用游戏内置组件**（Assembly-CSharp）；**无内嵌 shader**（材质靠名称绑定游戏 shader）；LODGroup/BoxCollider 常见 | HeliCrash 直升机、BRNVG 夜视仪、BetterRearSights 配件 |
| 服务端 mod 资源 | bundle 放 `user/mods/<ModName>/bundles/`（与代码分离）；多为纯模型/贴图资源 | BRNVG、SPT Battlepass、SecureMapbook |

**共性规律（跨作者）**：
1. **EFT mod bundle 几乎不内嵌 shader**（实测 4 个深读 bundle 全无）——材质统一绑定游戏内置 shader 名
2. **客户端 UI bundle 引用自身 DLL 的类**（只有 kmy 系这么干）——核对程序集名+命名空间+类名
3. **模型/资源 bundle 引用游戏内置组件或纯数据**——无需核对，跨版本安全
4. **Unity 版本头只反映作者打包时的 Unity 版本，不决定兼容性**（2022.3 向后兼容加载 2019.4）

## 4. 检查方法（工具脚本，已归档 `archive/forge/tools/`）

| 脚本 | 用途 |
|------|------|
| `check-bundle-version.py` | 扫 overlay 全部 bundle 的 Unity 版本头（快筛） |
| `inspect-bundle-scripts.py` | UnityPy 深读：对象类型分布 + MonoScript 引用清单 |
| `sync-bundles-from-3114.py` | 从 3114 原包同步 bundle 到 overlay（按 overlay 名映射） |
| `bundle-inventory.py` | 全量清单：逐 mod 列出所有 bundle + Unity 版本 + 压缩（含 SPT_Runtime 路径） |
| `check-bundle-gaps.py` | **遗漏检查**：原包同名目录有 bundle 但 overlay 没有 → 报遗漏 |
| `inspect-bundle-deep.py` | 深读指定 bundle：类型分布 + MonoScript 引用 + shader 内嵌检查 |

### 标准检查流程（部署带 bundle 的 mod 必做）
0. **遗漏检查**（`check-bundle-gaps.py`）：部署后先确认原包同名目录的所有 bundle 都已同步——实测抓到 2 处遗漏（KmyTarkovApi config bundle、BRNVG 6 个夜视 bundle）
1. `check-bundle-version.py`：确认 bundle 是 UnityFS 格式 + 记录 Unity 版本
2. `inspect-bundle-scripts.py`：列出全部 MonoScript 引用
3. **若引用外部 mod DLL**：核对 4.1 重编译 DLL 的 程序集名+命名空间+类名（Mono.Cecil）
4. 若引用游戏内置 Assembly-CSharp：确认类型在 4.1 存在（多数内置类型稳定）
5. 若只有系统 UI / 内嵌 script：安全，跳过

## 5. 决策树（bundle 来源选择）

```
bundle 来源候选：
├─ 3114 原包（推荐首选）——原包是 2022.3.43f1 验证过的可玩包，天然适配
│   └─ 前提：mod 是 B 桶已移植的（bundle 不含对重编译 DLL 的破坏性引用）
├─ Forge 源码仓库——与 3114 原包通常 MD5 一致（作者不更新 bundle）；有差异时以 Forge 为准
└─ 作者发布包（Nexus/Forge zip）——仅在 原包/Forge 都没有时获取

何时需要真正"升级"bundle（重打包）：
├─ bundle 引用的 mod DLL 类被重编译改名（必须改 bundle 或改 DLL 保持类名）——首选保持 DLL 类名
├─ bundle 内嵌 shader 且 shader 在 2022.3 失效（EFT mod 罕见，实测无内嵌 shader）
└─ bundle 用了 2022.3 移除的 Unity 内置组件（需 UnityPy 检查 + 游戏内验证）
```

## 6. 已知未决（后续学习点）

- 服务端 mod 的 bundle（`user/mods/.../bundles/`，占原包 2622 个中的绝大多数）走
  SPT 4.1 服务端 bundle 加载系统——**4.1 服务端 C# 化后 bundle 分发机制是否变化**待验证
  （部分 3.x 服务端 mod 用 `database` 直接指向 bundle 路径，4.x 需确认 `bundle` 配置格式）
- shader 名称在 0.14→0.16 间的漂移未逐一核对（风险低，游戏内可见粉红材质时回查）
