# Finding: blender-mcp 集成可行性分析 (SPT 3D 资产生成)

> Label: `wayfinder:research`
> Ticket: [#005 Define mod dev workflow](../tickets/005-define-mod-dev-workflow.md) (消费方;亦影响 #004 构建管线)
> Date: 2026-08-05
> Status: research complete — 外部调研;实现决策延后至 #005
> 研究方法: GitHub README 实读 + addon.py / server.py 源码审读 (DeepWiki 交叉核实) + SPT wiki / WTT / 社区工具链交叉核实

---

## 1. 结论速览 (TL;DR)

1. **blender-mcp (ahujasid/blender-mcp, 25.5k stars, MIT) 是"通过 MCP 控制 Blender"的桥**:Blender 内装 `addon.py`(TCP socket 服务器)+ 外部 FastMCP 服务器(`uvx blender-mcp`)。LLM 通过 21 个 MCP 工具驱动 Blender。
2. **原生工具面很窄**:场景检查 (3)、任意代码执行 (`execute_blender_code`)、外部资产生成 (PolyHaven / Sketchfab / Hyper3D / Hunyuan3D)。**没有原生文件导出工具、没有骨骼/动画/权重工具**。所有"正经活"都落在 `execute_blender_code`(任意 bpy 代码)上。
3. **SPT 静态物品 (loot/容器/钥匙) 建模:可行 (中-高可行性)**。blender-mcp 能产出 `.blend`/FBX;后续 Unity 管线 (EFT SDK + Bumped Specular SMap + AssetBundle) 社区有完整文档化流程。
4. **武器/配件级 rigging + 动画:低可行性**。无原生绑骨/动画工具,全靠 `execute_blender_code` 文本驱动 bpy,权重绘制/骨骼对齐/动画重定向在纯文本通道下极其脆弱。社区已有 `EFTWeaponBuilder` 这样的**专门 Blender 插件**解决此问题,反证其复杂度。
5. **硬边界:AssetBundle 打包无法自动化在 blender-mcp 内**。`.bundle` 必须在装有 **EFT SDK 的 Unity 2022.3** 内构建;blender-mcp 只能做到"产出源模型",Unity 步骤是必经的人工/半自动 (Unity batch mode) 环节。
6. **集成方式:作为独立 MCP server 追加进 `.mcp.json`**。blender-mcp 官方 README 原生支持 OpenCode 配置;与本仓库现有 node-based MCP (xedit/bgs_kb/mo2/spt) 并行,不冲突。

---

## 2. blender-mcp 概述

### 2.1 基本事实

| 项 | 值 | 来源 |
|---|---|---|
| 仓库 | `ahujasid/blender-mcp` | GitHub |
| 协议 | MIT, 169 commits, 25.5k stars / 2.4k forks | GitHub repo page |
| 定位 | "Open-source MCP to use Blender with any LLM" | README:5 |
| 官方站 | https://blendermcp.org/ | README |
| 客户端支持 | Claude Desktop / Claude Code / Cursor / VS Code / **OpenCode**(README 有原生 opencode 配置段) | README |
| 环境要求 | Blender **3.0+**、Python 3.10+、uv 包管理器;`uvx blender-mcp` 启动 | README Prerequisites |

### 2.2 架构与通信

```
LLM (OpenCode) ── MCP(stdio) ──> blender-mcp server (FastMCP, src/blender_mcp/server.py)
                                      │ JSON over TCP socket
                                      ▼
                              Blender addon (addon.py)  ── bpy.app.timers ──> Blender 主线程执行
```

- addon 在 Blender 内起 TCP socket 服务器,默认 `localhost:9876`(`BLENDER_HOST` / `BLENDER_PORT` 可改,支持远程主机)。
- 命令格式:`{"type": "<command>", "params": {...}}`;命令经 `bpy.app.timers.register` 在 Blender 主线程安全执行。
- **Blender GUI 必须打开且已点 "Connect to Claude"** —— 不是 headless 方案。
- addon 共注册 22 个 TCP 命令 handler(见 2.3)。

### 2.3 工具清单 (21 个 MCP 工具 / 22 个 TCP 命令)

| 类别 | 工具 | 对 SPT 的意义 |
|---|---|---|
| 场景检查 | `get_scene_info` / `get_object_info` / `get_viewport_screenshot` | 视觉反馈闭环;截图校验模型姿态/尺寸 |
| **任意代码** | `execute_blender_code` | **核心通道**:一切建模/导入/导出/绑骨/动画都靠它 |
| PolyHaven | `get_polyhaven_status` / `get_polyhaven_categories` / `search_polyhaven_assets` / `download_polyhaven_asset` / `set_texture` | 参考贴图/HDRI;`set_texture` 建 PBR 节点树 (Principled BSDF) |
| Sketchfab | `get_sketchfab_status` / `search_sketchfab_models` / `get_sketchfab_model_preview` / `download_sketchfab_model` | 现成低模素材源 (WTT 教程也推荐 Sketchfab low-poly) |
| Hyper3D Rodin | `get_hyper3d_status` / `generate_hyper3d_model_via_text` / `generate_hyper3d_model_via_images` / `poll_rodin_job_status` / `import_generated_asset` | 概念模型 AI 生成 (需 API key) |
| Hunyuan3D | `get_hunyuan3d_status` / `generate_hunyuan3d_model` / `poll_hunyuan_job_status` / `import_generated_asset_hunyuan` | 同上,腾讯系 |

**关键源码事实 (addon.py 审读):**

- **无原生导出 handler**:22 个命令里没有任何 `export_scene.fbx/obj/gltf`。文件导出只能走 `execute_code` 调 `bpy.ops.export_scene.fbx` 等。
- **导入仅存在于资产下载链路**:`download_polyhaven_asset` (models 分支) 调 `import_scene.gltf/fbx/obj`,Sketchfab 下载后 `import_scene.gltf`,`import_generated_asset` 导入 GLB。**无通用文件导入工具**(不能直接让 LLM 说"导入 C:\x.fbx")——同样要 `execute_code`。
- **纹理/材质**:`set_texture` 建 **Principled BSDF** 节点树 (Base Color/Roughness/Metallic/Normal/Displacement,含 ARM 贴图混合)。**注意:这是 PBR 工作流,与 Tarkov 的 specular 工作流 (Bumped Specular SMap) 不兼容**,需 `execute_code` 自建着色器。
- **无任何 rigging / armature / bones / animation handler**(截断部分也未见,DeepWiki 工具清单同样缺席)。
- 遥测:匿名遥测默认开,`DISABLE_TELEMETRY=true` 环境变量可完全关闭(生产环境必须关)。

---

## 3. SPT 用例能力评估

### 3.1 能力矩阵

| SPT 用例 | blender-mcp 能力 | 通道 | 评级 |
|---|---|---|---|
| 程序化创建几何体 (盒子/柱体/低模物品) | 有 | `execute_code` (bpy primitives) | 高 |
| AI 生成概念模型 (武器概念/奇物) | 有 | Hyper3D / Hunyuan3D | 中 (产物需大量清理,非 game-ready) |
| 导入现有模型 (FBX/OBJ/GLB/glTF) | 有 (非原生) | `execute_code` `bpy.ops.import_scene.*` | 中-高 |
| 编辑/修改模型 (拓扑/尺寸/变换) | 有 | `execute_code` + `get_viewport_screenshot` 反馈 | 中-高 |
| 贴图/材质 (Tarkov specular 工作流) | **部分** | `set_texture` 仅 PBR;specular 需 `execute_code` 自建 | 中 |
| 导出 Unity 兼容格式 (FBX/OBJ/.blend) | 有 (非原生) | `execute_code` `bpy.ops.export_scene.fbx` | 中 (需验证稳定性) |
| 绑骨 (armature/bones) | **无原生** | 全靠 `execute_code` | 低 |
| 权重绘制 | 无原生 | `execute_code` (数值驱动,文本通道极脆弱) | 低 |
| 动画 (pose/bake/重定向) | 无原生 | `execute_code` | 低 |
| AssetBundle 打包 | **无** | 必须 Unity + EFT SDK | 不可行 (硬边界) |

### 3.2 核心结论

- **blender-mcp 的定位是"场景搭建/资产组织助手",不是"游戏资产生产流水线"。** 它的原生工具面向:检查场景、搭低模场景、调材质、下载现成资产、AI 生成概念。
- 对 SPT 而言,**唯一能复用其价值的场景 = 静态物品的快速建模/改模 + 参考素材获取**。凡是武器级骨骼绑定、动画,它的工具面直接缺位,退化为"用 `execute_code` 写 bpy 脚本",而这恰恰是社区认为最难的部分(见 `EFTWeaponBuilder` 的存在)。
- `execute_blender_code` 是任意代码执行,与 xedit MCP 的治理规则同理:**必须只读默认 + 变更需显式同意**。

---

## 4. Unity/EFT 管线集成分析

### 4.1 社区标准管线 (WTT Vol.1 静态物品,已文档化)

```
Blender 建模 ──> 拖 .blend 进 Unity (EFT SDK 项目) ──> Extract Embedded Materials
  ──> 赋 Bumped Specular SMap 着色器 ──> GameObject 层级 (PreviewPivot + Convex Collider)
  ──> 建 prefab + Asset Label ──> Asset Bundle Browser 构建 .bundle ──> SPT 加载
```

具体步骤要点 (来源 `wiki.sp-tarkov.com/modding/tutorials/WTT_Vol1`):

1. **建模**:低模 (<10k 三角形),导入 EFT SDK 自带 `ExampleCharacter.fbx` (PMC 人模) 作为**比例参照**,对象对齐到真实世界米制;`Set Origin → Origin to Geometry`、`Ctrl+A` Apply Rotation & Scale、`Alt+G` 清变换、回世界原点 (0,0,0)。
2. **Unity 导入**:`.blend` 直接拖进 Unity Assets 文件夹 —— **Unity 用本机 Blender 转档,机器上必须装 Blender**。导出 FBX 并非必需,`.blend` 直拖是社区主流。
3. **材质**:`Inspector > Materials > Extract Embedded Materials`;赋 **`Bumped Specular SMap`**(Tarkov 最常用着色器)——**specular 烘焙进 diffuse 贴图**;无贴图基线值:Main Color 白 / Specularness 0.35 / Glossness 1 / Reflection Color 黑 / Specular Vals `1 1 0 0` / Defuse Vals `1 1 0 0`。
4. **GameObject 层级**:空根节点 @ (0,0,0) + 挂 SDK 的 `PreviewPivot` 脚本;网格子物体挂 `Mesh Collider` (勾 Convex)。
5. **打包**:prefab → Asset Label → `Window > Asset Bundle Browser > Build`。构建后必须等 Unity 完全响应,否则着色器丢失。
6. **SPT 侧注册** (WTT-CommonLib):`bundles.json` 声明 `key`(bundle 相对路径)+ dependencies (`physicsmaterials.bundle` / `shaders` / `cubemaps`);bundle 文件放 mod 的 `bundles/` 目录。

### 4.2 blender-mcp 能接管哪一段

| 管线段 | blender-mcp 可接管? | 说明 |
|---|---|---|
| 建模/改模/比例参照 | **是** | `execute_code` + 截图反馈;比例参照 PMC 也可经 `execute_code` 导入对照 |
| 导出 `.blend`/FBX | **是** (非原生) | `execute_code` 存 `.blend` 或 `export_scene.fbx`;社区惯例是直接产出 `.blend` 给 Unity |
| 材质赋 specular 参数 | **部分** | 可在 Blender 侧设好 PBR 或自建节点;Unity 侧 Extract + 赋 shader 仍是人工步 |
| Unity 导入/Extract/shader 赋值 | **否** | 无 Unity MCP;需人工或 Unity batch mode 脚本 |
| PreviewPivot / Collider / prefab | **否** | Unity 编辑器操作 |
| AssetBundle 构建 | **否** | 硬边界:必须在带 EFT SDK 的 Unity 2022.3 构建 |
| bundles.json 注册 | 否 (归 spt MCP/WTT-CommonLib) | 已有 spt MCP 消费面覆盖 |

### 4.3 Unity/EFT 关键约束

- **EFT 使用 Unity 2022.3.43f1**(WeaponAIOTool 环境清单);SPT 模组工具链均围绕该版本 + EFT SDK。
- **坐标系**:Blender 右手系 Z-up ↔ Unity 左手系 Y-up;FBX 导出/Unity 导入自动换算,但**比例必须真实米制**,否则在游戏内尺寸错乱。
- **着色器约定**:Tarkov 是 **specular 工作流 (Bumped Specular SMap)** —— diffuse 里带 specular 信息,不是 PBR metallic-roughness。blender-mcp 的 `set_texture` 建的是 Principled BSDF (PBR),**直接输出给 Tarkov 会材质不符**,需在 Blender 侧按 specular 约定贴图或接受 Unity 侧重调。
- **bundle 依赖**:bundle 必须声明 `shaders` / `cubemaps` / `physicsmaterials.bundle` 依赖 (WTT-CommonLib bundles.json 示例),否则加载缺失。
- **低模纪律**:官方教程明确 <10k 三角形,blender-mcp 的 Sketchfab/Hyper3D 产物需检查面数。

---

## 5. EFT/Tarkov 特有考量 (社区知识)

- **自定义武器管线**:`WeaponAIOTool`(WTT 团队) 覆盖 Asset Ripper → AssetStudioGui → Unity → Tarkov 全流程;环境 = Unity 2022.3.43f1 + EFT SDK + Asset Ripper + Blender + AssetStudio。这证明**武器级管线是重型多工具链路**,blender-mcp 只占其中"Blender 建模"一小格。
- **EFTWeaponBuilder**(社区 Blender 插件):自动导入 EFT 武器/mod、按 `weapon_compatibility.json` 挂骨、EFT Shader 材质、roughness 烘焙。**它是 GUI 插件而非 MCP**——说明"武器挂骨 + 材质"这类操作社区都认为需要专门工具,纯文本驱动不可靠。
- **服务端框架替代路径**:`ItemGen`(SPT 4.0.13) 提供**免 Unity 的 bundle 注入**——客户端 `BundleInjector` (BepInEx 插件) 按文件名匹配加载 `BepInEx/plugins/Serenity-ItemGen/bundles/*.bundle`。但 `.bundle` 仍须由 Unity 构建,只是绕过了 WTT-CommonLib 的 bundles.json 注册。
- **SPT 侧物品定义** (WTT-CommonLib `db/CustomItems/`):`itemTplToClone` + `overrideProperties.Prefab.path` 指向 bundle 内 prefab 路径 —— 3D 模型路径在服务端 JSON 里引用,blender-mcp 不涉及。
- **动画**:武器动画 (use/inspect/检查动画) 通常从其他游戏重定向 (WeaponAIOTool 教程含 Animation Retargeting),是独立于建模的高难环节。

---

## 6. 集成架构提案

### 6.1 定位:第五个 MCP server,与现有四个并行

```
OpenCode
 ├─ spt MCP      (mod 分析:模板/物品定义/bundles.json 注册)  [已有]
 ├─ xedit MCP    (BGS 插件,与 SPT 管线无关)                  [已有]
 ├─ bgs_kb MCP   (知识库)                                    [已有]
 ├─ mo2 MCP      (MO2 控制平面)                              [已有]
 └─ blender MCP  (3D 建模:工具面= execute_code 为主)          [新增,本提案]
```

- **注册方式**:blender-mcp 是 **Python/uvx 分发**,与本仓库 node-based MCP 不同源,但 OpenCode 的 MCP 注册是通用的。README 有现成 opencode 配置:

```jsonc
// opencode.json (新增条目,照 README OpenCode integration 段)
{
  "mcp": {
    "blender-mcp": {
      "type": "local",
      "command": ["uvx", "blender-mcp"],
      "enabled": true,
      "environment": { "BLENDER_HOST": "localhost", "BLENDER_PORT": "9876", "DISABLE_TELEMETRY": "true" }
    }
  }
}
```

- **Windows 注意** (README 明确):GUI 启动的客户端不继承终端 PATH,`spawn uvx ENOENT` 常见 → 用 `where uvx` 拿全路径,或 `"command": "cmd", "args": ["/c", "uvx", "blender-mcp"]`(README Windows 官方写法);建议 pin Python 3.11 (`uvx --python 3.11 blender-mcp`) 避开 conda/pyenv 冲突。
- **Blender 侧**:手动装 `addon.py` + 点 Connect。这是唯一不可脚本化的安装步(GUI 操作)。
- 备选:**不注册为 MCP**,而是把 blender-mcp 的 TCP 协议当"控制平面"复用 —— 不推荐,失去 MCP 工具面治理。

### 6.2 治理:镜像 xedit MCP 的约束模型

`execute_blender_code` = 任意代码执行,与 xedit 的 `-IKnowWhatImDoing` 同级的风险面:

1. **只读默认**:场景检查类 (`get_scene_info` / `get_object_info` / `get_viewport_screenshot`) 免审;任何 `execute_blender_code` 变更操作需显式用户同意。
2. **产物输出到工作区 mod 目录,不碰游戏 Data**(对齐仓库硬规则 1 的 MO2 overlay 语义):建模产物落 `<workspace>/mods/<mod-name>/` 或 staging 目录。
3. **验证闭环**:每步变更后 `get_viewport_screenshot` 截图回传;导出后校验文件存在 + 面数。
4. **遥测关闭**:`DISABLE_TELEMETRY=true`。

### 6.3 建议工作流 (静态物品 MVP)

```
用户描述物品 (如"军用弹药箱,低模,军事绿") 
  → spt MCP: 取克隆模板 (itemTplToClone) + Prefab 约定 + bundles.json 需求
  → blender MCP: get_scene_info 清场 → execute_code 建低模 (或 Sketchfab 搜低模) 
      → 导入 PMC 参照 → 缩放到米制 → 截图校验 → 导出 .blend 到 mod staging
  → [人工/半自动] Unity + EFT SDK: 拖 .blend → Extract → Bumped Specular SMap → PreviewPivot → 构建 .bundle
  → spt MCP / WTT-CommonLib: bundles.json + db/CustomItems JSON 注册
  → mo2 MCP: 打包进 MO2 mod 目录 / 测试
```

**阶段化建议**(供 #005 决策,非定案):

- **阶段 1 (MVP)**:静态物品 (loot/容器/钥匙) 建模 + `.blend` 导出 + 人工 Unity 步骤。验证 blender-mcp 文本建模质量、FBX/.blend 导出稳定性、截图反馈闭环。
- **阶段 2**:Unity batch mode 脚本化 (`unity -batchmode -executeMethod BuildAssetBundles`) 半自动化 bundle 构建,消除唯一人工大步骤。
- **阶段 3 (探索性,不建议优先)**:武器/动画。需自研 bpy 脚本库封装绑骨/权重/动画,或接受 `EFTWeaponBuilder` 类 GUI 插件人工介入 —— 此阶段 blender-mcp 收益边际递减。

---

## 7. 可行性评级与限制

### 7.1 评级 (S.P.E.C.I.A.L. 风格)

| 维度 | 评级 | 说明 |
|---|---|---|
| 静态物品建模自动化 | **B+** | 可行;质量取决于 LLM 的 bpy 代码能力,截图反馈可闭环 |
| 改模/重缩放/比例参照 | **A-** | `execute_code` + PMC 参照导入,社区流程完全可程序化 |
| 材质 (Tarkov specular) | **C+** | `set_texture` 是 PBR,specular 需自建节点;材质质量是最大扣分项 |
| 武器绑骨/动画 | **D** | 无原生工具;社区用专门插件解决,纯 MCP 通道不现实 |
| 管线端到端自动化 | **C** | 硬边界在 Unity/AssetBundle;blender-mcp 最多覆盖 30-40% 步骤 |

### 7.2 限制清单

1. **无原生导出/导入**:全部走 `execute_code`;`bpy.ops.export_scene.fbx` 在 GUI Blender 上下文可用性需实测(低风险,但必须验证)。
2. **非 headless**:Blender GUI 必须打开并保持连接,无法进 CI 无头流程;远程主机支持 (BLENDER_HOST) 可部分缓解。
3. **文本驱动建模的上下文消耗**:复杂模型 = 长 bpy 代码 + 多次迭代,LLM 上下文烧得快;对复杂武器不经济。
4. **Tarkov specular 材质 ≠ PBR**:`set_texture` 输出直接给 Tarkov 会材质不符;需自建 specular 节点或 Unity 侧重调。
5. **AI 生成模型 (Hyper3D/Hunyuan) 非 game-ready**:拓扑差、面数高、需手动清理,只适合概念参考。
6. **AssetBundle 硬边界**:`.bundle` 必须 Unity 2022.3 + EFT SDK 构建;blender-mcp 无法触及;若无 Unity 环境,管线断在建模产物。
7. **遥测与任意代码风险**:生产必须 `DISABLE_TELEMETRY=true`;`execute_blender_code` 需治理(见 6.2)。
8. **版本耦合**:Blender 3.0+ 即可,与 WTT 教程 (Blender 3.0+ 推荐) 兼容;但 bpy API 随版本漂移,脚本需固定 Blender 版本。

### 7.3 一句话裁决

> blender-mcp **值得作为 SPT 管线的"3D 资产作者助手"接入**(静态物品 MVP 可行),但它**不是**自动化建模/武器管线——真正的产品化路径是"blender-mcp 产源模型 + 人工/Unity batch mode 做 AssetBundle + spt MCP 注册",其中 Unity 步骤不可消除。

---

## 8. 证据坐标索引

| 事实 | 位置 |
|---|---|
| blender-mcp 仓库/README/工具清单/安装 | https://github.com/ahujasid/blender-mcp (README.md) |
| 21 工具分类与签名 | https://deepwiki.com/ahujasid/blender-mcp/4.3-tool-definitions |
| 架构 (FastMCP + TCP socket + addon 线程模型) | https://deepwiki.com/ahujasid/blender-mcp (System Architecture) |
| addon 22 命令 handler 映射 / 无导出 / 无骨骼动画 / set_texture PBR | `addon.py`(raw.githubusercontent.com/ahujasid/blender-mcp/main/addon.py) |
| OpenCode 原生配置段 / Windows uvx PATH 坑 / DISABLE_TELEMETRY | README (OpenCode integration / Troubleshooting / Telemetry) |
| EFT = Unity 2022.3.43f1;武器全流程工具链 | https://github.com/GrooveypenguinX/WeaponAIOTool |
| 静态物品标准管线 (比例/着色器/AssetBundle) | https://wiki.sp-tarkov.com/modding/tutorials/WTT_Vol1 |
| bundles.json 注册 + Prefab.path 物品定义 | https://github.com/WelcomeToTarkov/WTT-CommonLib (README) |
| 免 Unity 注册的 bundle 注入替代路径 (ItemGen BundleInjector) | https://github.com/ShaneeexD/ItemGen |
| 武器挂骨/材质专门插件 (反证复杂度) | https://github.com/papesgit/EFTWeaponBuilder |
| 本仓库 MCP 注册模式 (4 server, ${CLAUDE_PLUGIN_ROOT} 路径) | `.mcp.json`(仓库根) |
