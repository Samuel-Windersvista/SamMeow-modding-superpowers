# SPT 离线版塔科夫整合包自动化搭建 -- 可行性研究报告（终版）

> [HISTORICAL] 基线 SPT 3.11.4，部分结论已被 wayfinder 决议取代（见 `docs/wayfinder/MAP.md`）。保留作历史参考。
> 版本: v3.0 -- 整合第一性原理分析与实操验证
> 报告日期: 2026-05-28
> 研究对象: 《生活在诺文斯克》(Live in Norvinsk) 整合包搭建流程的 AI 驱动自动化
> 基线: SPT 3.11.4 / server-csharp / 150+ MOD

---

## 零、摘要

### 核心结论

**基于 AI 从 GitHub 拉取 MOD 源码、分析、集成、构建一套 SPT 整合包的设想，约 60% 的工作可以自动化。真正的瓶颈不是 AI 能力，而是架构层面的隐含假设。** 通过三个方向的架构突破（IL 反编译分析管线、功能域配置模型、绕过 MO2 自包含分发），自动化上限可推至 80-85%。

### 三个最被低估的突破点

1. **无源码客户端 DLL 并非"黑盒"** -- .NET IL 反编译工具链（Mono.Cecil / ICSharpCode.Decompiler）可从编译产物中静态提取 Harmony 补丁目标、优先级、程序集依赖。这是 10+ 年成熟技术，一周内可实现。

2. **MO2 不是必须的** -- 构建时完成文件系统分层合并（烘焙 USVFS 逻辑），输出自包含 SPT 目录。用户解压即玩，无需学习 MO2。

3. **整合包应是一等软件项目** -- 当前架构中整合包是 150 个独立 MOD 的"寄生者"。应倒转为：整合包是主人（自有源码仓库、构建系统、版本号），MOD 是供应商（通过 git subtree 引入）。

---

## 一、第一性原理: 挑战隐含假设

在深入分析之前，先列出当前流程中**被默认接受但未必成立的假设**:

| # | 隐含假设 | 现状 | 挑战 |
|---|---------|------|------|
| A | MO2 是整合包必需的"管理壳" | 已接受 | 构建时可烘焙其虚拟文件系统 |
| B | 客户端 C# DLL = 不可分析的黑盒 | 已接受 | IL 反编译可提取 Harmony 补丁、依赖图、配置项 |
| C | MOD 冲突本质上是运行时问题 | 已接受 | 可通过编译时能力声明+兼容性矩阵前置 |
| D | 整合包 = 150 个独立 MOD 的集合 | 已接受 | 应重新定义为"功能域配置"的一等软件项目 |
| E | SPT 服务器是不可修改的底座 | 已接受 | SPT 是开源 C# 项目，可以 fork 并内联 MOD 逻辑 |
| F | 整合包版本 = 各 MOD 版本的排列组合 | 已接受 | 整合包应有自己的版本号（如 LIN-2026.05.28） |

以下的所有分析都在挑战这些假设。

---

## 二、当前系统架构

### 2.1 游戏运行架构

```
+-------------------+     HTTP(6969)     +-------------------+
|   游戏客户端       | <---------------> |   SPT 服务器        |
| (EscapeFromTarkov |                    | (SPT.Server.exe)   |
|  + BepInEx 注入)   |                    | C# .NET 9.0       |
+-------------------+                    +-------------------+
         |                                        |
    客户端 MOD:                              服务端 MOD:
    BepInEx/plugins/*.dll                   user/mods/<name>/
    (C# 编译产物)                             (TypeScript + package.json)
```

关键事实:
- SPT 服务器已从 Node.js 迁移为 C# (.NET 9.0) 实现（仓库: sp-tarkov/server-csharp）
- 服务端通过 ModDllLoader + tsyringe DI 加载 TypeScript MOD
- 客户端通过 BepInEx + Harmony 补丁框架加载 C# DLL
- 目前 MOD 管理依赖 Mod Organizer 2 (USVFS 虚拟文件系统)

### 2.2 MOD 类型分类（基于 150+ MOD 抽样）

| 类型 | 结构 | 源码可得性 | 代表 MOD |
|------|------|-----------|----------|
| 纯客户端 | `BepInEx/plugins/*.dll` | ~35% 有 C# 源码 | AmandsGraphics, UIFixes |
| 纯服务端 | `user/mods/<name>/` | ~90% 有 TS 源码 | Softcore, APBS |
| 混合型 | 两者兼备 | 取决于子部分 | Realism, SAIN, RaidOverhaul |

### 2.3 当前搭建流程（人工）

1. 从 Forge 下载 SPT 安装器，安装客户端
2. 从 Forge 逐个下载 MOD 压缩包
3. 通过 MO2 逐个安装，设置加载顺序
4. 逐个 MOD 配置 config.json
5. 排查 MOD 间兼容性问题
6. 跟踪各 MOD 更新，对比差异，同步配置

**人工耗时: 4-8 小时一次初始搭建，每次 MOD 更新额外 1-3 小时。**

---

## 三、自动化流程架构

### 3.1 设想的 AI 驱动流程（修正版）

```
用户: "搭建基于 SPT 4.X 的整合包，包含 ModA, ModB, ModC..."
   ↓
[Phase 1 - 自动] 源码获取与结构化分析
  ├── git clone 所有 MOD 的 GitHub 源码仓库
  ├── IL 反编译客户端 DLL（提取 Harmony 补丁/依赖）
  ├── 解析 package.json (name/version/loadBefore/loadAfter/incompatibilities)
  ├── 提取 DI 注册 (container.register / afterResolution)
  ├── 识别 DB 修改路径 (db/ 目录下修改了哪些 JSON)
  └── 输出: 每个 MOD 的结构化分析摘要 (JSON)

   ↓
[Phase 2 - 自动] 冲突检测与功能域映射
  ├── 交叉对比所有 MOD 的结构化摘要
  ├── 检测: 同名 DI 注册、afterResolution 单例竞争、DB 路径重叠
  ├── 检测: Harmony 补丁目标方法冲突（基于 IL 分析）
  ├── 按功能域自动分类 (经济/AI/武器/商人/...)
  └── 输出: 冲突矩阵 + 功能域映射表

   ↓
[Phase 3 - 交互] 人工决策
  ├── 呈现同功能域重叠的设计理念冲突（"BotGenerator 控制权归谁？"）
  ├── 呈现 Harmony 补丁优先级建议
  ├── 呈现自动合并方案预览
  └── 用户确认 → 生成合并决策文件 (merge-manifest.json)

   ↓
[Phase 4 - 自动] 构建与打包
  ├── 按 merge-manifest.json 执行 TypeScript 源码合并
  ├── 按需执行 IL 级客户端 DLL 合并（非冲突补丁）
  ├── JSON 配置深度合并（按 namespace 展平）
  ├── 构建时烘焙 USVFS 文件系统分层
  └── 输出: 自包含 SPT 整合包目录（无需 MO2）
```

### 3.2 各步骤可行性逐项评估

**步骤 A: 源码获取（git clone）**
- 可行度: 90%
- 约 90% MOD 服务端有 TS 源码，约 35% 客户端有 C# 源码
- 需要手工维护 MOD 名→GitHub 仓库映射表（`mods.json`）

**步骤 B: IL/反编译分析管线（新增 - 关键突破）**
- 可行度: 90%
- 技术基础: Mono.Cecil / ICSharpCode.Decompiler（.NET 生态 10+ 年成熟工具链）
- 能从任意 BepInEx DLL 中提取:
  - `[HarmonyPatch]` 属性 → 补丁目标类/方法
  - `[HarmonyPriority]` / `[HarmonyBefore]` / `[HarmonyAfter]` → 优先级声明
  - `[BepInDependency]` → 软/硬依赖关系
  - 程序集引用 → MOD 间依赖图
- 实现量: 约 400-800 行 C#，一周内可完成
- **这将客户端分析覆盖率从 35% 提升至 100%**

**步骤 C: TypeScript 源码结构化分析**
- 可行度: 85%
- LLM 擅长分析 TS 代码中的 DI 注册、DB 修改、生命周期钩子
- 窗口限制可通过分批+摘要缓存解决（每个 MOD 只分析一次，输出 JSON 摘要）

**步骤 D: 冲突检测**
- 可行度: 80%（服务端）/ 70%（客户端 Harmony）
- 同名 DI 注册 → 直接可检测
- 同名 DB 路径写入 → 直接可检测
- Harmony 补丁冲突 → 基于 IL 提取的目标方法+优先级静态分析
- 不可检测: 补丁的运行时语义冲突（两个 Postfix 都修改同一个 float 返回值，但语义上互斥）

**步骤 E: 源码级集成（步骤5）**
- 基于 12 个真实 MOD 的交叉对比验证:
  - 完全正交的 MOD 对: **95%** 可自动集成（如 botplacementsystem + RaidOverhaul）
  - 轻量重叠: **70-85%** 可自动，剩余需参数化策略选择
  - 深度重叠（单例竞争）: **40-60%** 需人工裁决
  - 客户端硬依赖: **35%** 需接口抽象
- 加权平均: **62%** 可自动处理，38% 需人工决策

**步骤 F: 绕过 MO2 自包含打包**
- 可行度: 95%
- 核心逻辑: 按 MO2 优先级顺序将文件复制到输出目录（高优先级覆盖低优先级）
- 实现量: 约 500 行代码
- JSON 深度合并（namespace 展平）是额外复杂度但可解

### 3.3 核心不可解问题（坦诚）

| 问题 | 程度 | 为什么不可解 |
|------|------|-------------|
| Unity 二进制资产 | 不可解 | bundle/贴图/模型是二进制，LLM 无法操作。但这不是集成障碍 -- 它们可以原样拷贝分发 |
| 运行时行为验证 | 不可解 | 整合包的正确性只能通过实际游玩来验证。可以设计自动化冒烟测试（启动服务器→加载 MOD→检查日志无错误），但无法替代人工测试 |
| 设计理念冲突 | 不可自动 | BotGenerator 控制权归 Realism 还是 APBS？Fence 应该是和平主义（仅卖垃圾）还是削弱（限制高级装备）？这些是游戏设计决策，不是代码问题 |

---

## 四、IL/反编译分析管线（关键突破）

### 4.1 为什么这个方向被严重低估

当前报告将"65% 客户端 C# MOD 无源码"定性为"致命障碍"。这是错误的。

所有 BepInEx 插件（无论有无源码）归根结底是对 Unity 游戏的 **Harmony 补丁 + MonoBehaviour 注入**。这些信息**可以从编译后的 DLL 中静态提取**。

.NET 程序集包含完整的元数据（类型、方法、属性、IL 字节码）。反编译工具链存在超过 10 年且高度成熟:
- **Mono.Cecil**: 读取/修改 .NET 程序集（不需要源码）
- **ICSharpCode.Decompiler**: 将 IL 反编译为可读 C# 代码
- **ILRepack/ILMerge**: 合并多个程序集为一个

**SPT MOD 生态中，没有任何已知 MOD 使用商业级混淆器。** 这意味着反编译准确率 > 99%。

### 4.2 技术路径

```
编译 DLL (BepInEx 插件)
  ↓
Mono.Cecil 静态分析:
  ├── 枚举 [HarmonyPatch] 属性 → 提取目标类 + 目标方法 + Patch 类型 (Prefix/Postfix/Transpiler)
  ├── 枚举 [HarmonyPriority] / [HarmonyBefore] / [HarmonyAfter] → 提取补丁优先级
  ├── 枚举 [BepInDependency] → 提取 MOD 间依赖关系
  ├── 枚举 [BepInPlugin] → 提取 MOD 元数据
  ├── 枚举程序集引用 → 构建 MOD 依赖图
  └── 枚举 BepInEx 配置读取路径 → 提取可配置参数清单
  ↓
结构化输出 (JSON):
  {
    "modName": "TarkovIRL-WHM",
    "patches": [
      { "target": "Player.UpdateSwayFactors", "type": "Postfix", "priority": "Normal" },
      ...
    ],
    "dependencies": ["RealismMod"],
    "assemblyReferences": ["RealismMod.dll", "spt-common.dll"],
    "configKeys": ["EnableTacSprint", "SwayMultiplier"]
  }
```

### 4.3 冲突检测示例

```csharp
// MOD A 的 DLL（反编译后）
[HarmonyPatch(typeof(Player), nameof(Player.UpdateSwayFactors))]
class Patch_A { static void Postfix(ref float __result) { __result *= 1.5f; } }

// MOD B 的 DLL（反编译后）
[HarmonyPatch(typeof(Player), nameof(Player.UpdateSwayFactors))]
[HarmonyPriority(Priority.VeryHigh)]
class Patch_B { static void Postfix(ref float __result) { __result *= 2.0f; } }
```

分析管线输出:
```json
{
  "conflicts": [
    {
      "method": "Player.UpdateSwayFactors",
      "patches": [
        { "mod": "A", "type": "Postfix", "priority": "Normal" },
        { "mod": "B", "type": "Postfix", "priority": "VeryHigh" }
      ],
      "assessment": "两个 Postfix 都修改 __result。B 的优先级更高，将最后执行。无语法错误但语义可能冲突（两者都是乘法因子）。建议人工验证最终行为。"
    }
  ]
}
```

### 4.4 实现量估算

| 组件 | 技术 | 代码量 | 开发时间 |
|------|------|--------|---------|
| DLL 加载器 | Mono.Cecil | ~100 行 | 1 天 |
| Harmony 补丁提取器 | Mono.Cecil + 属性反射 | ~200 行 | 1-2 天 |
| 依赖图构建器 | Mono.Cecil AssemblyReference | ~100 行 | 1 天 |
| 反编译输出 | ICSharpCode.Decompiler | ~150 行 | 1 天 |
| 冲突检测引擎 | 对比算法 | ~200 行 | 1-2 天 |
| **总计** | | **~750 行 C#** | **1 周** |

---

## 五、功能域配置模型（架构突破）

### 5.1 问题

当前整合包定义为"150 个独立 MOD 的集合"。每个 MOD 有自己的版本号、配置格式、发行节奏。版本管理的复杂度是 150 维的。

### 5.2 方案: 双层抽象

```
上层（用户看到的）: 体验配置
  experience.json:
  {
    "version": "LIN-2026.05.28",
    "features": {
      "economy": { "type": "barter", "difficulty": "hard", "fleaMarket": "disabled" },
      "combat": { "ballistics": "realistic", "armor": "reworked" },
      "ai": { "behavior": "realistic", "loot": "enabled", "vision": "line-of-sight" },
      "progression": { "hideout": "enabled", "skills": "extended" }
    }
  }

下层（构建工具看到的）: MOD 实现映射
  feature-map.json:
  {
    "economy": {
      "barter": { "primary": "barter_economy", "secondary": "Softcore" },
      "hard": { "mods": ["barter_economy", "Softcore.economyOptions.hard"] }
    },
    "combat.ballistics.realistic": { "primary": "Realism" },
    "ai.behavior.realistic": { "primary": "SAIN", "requires": ["BigBrain", "Waypoints"] }
  }
```

### 5.3 收益

- 版本复杂度从 150 维降为 1 维（整合包版本 = LIN-2026.05.28）
- 用户按"特性"开关选配，而非按"MOD"开关（不认识 MOD 的新用户也能理解）
- 兼容性矩阵内置于功能域层，而非分散在 150 个 MOD 的 package.json 中
- MOD 版本锁定自动化（如果 Realism 2.4 破坏了 SAIN，构建工具自动锁定 Realism 到 2.3）

### 5.4 初始建模成本

将 150 个 MOD 映射到约 25 个功能域，并为每个功能域编写兼容性规则。估计 3-5 天手工工作。一次投入，持续复用。

---

## 六、绕过 MO2 自包含分发（分发突破）

### 6.1 问题

当前整合包分发形式: "游戏本体 + MO2 程序 + MOD 文件夹 + 配置"。用户需要学习 MO2 的复杂操作。

### 6.2 方案: 构建时烘焙 USVFS

MO2 的虚拟文件系统本质上是一个文件系统分层合并引擎。在运行时:

```
for each file in mod_list (ordered by priority):
    virtual_root[file] = highest_priority_mod_containing_file
```

这完全可以在构建时完成:

```
构建时（开发者侧）:
  输入: 150 个 MOD 目录 + modlist.txt (优先级排序)
  处理: 按优先级顺序将文件复制到输出目录（高优先级覆盖低优先级）
  输出: 单一 self-contained MOD 目录

最终分发物:
  SPT_4.x_LiN.zip
    ├── EscapeFromTarkov.exe
    ├── BepInEx/plugins/     ← 已合并、已分析冲突的客户端 DLL
    ├── user/mods/LiN/       ← 所有服务端 MOD 合并为一个
    └── SPT.Server.exe       ← 标准 SPT 服务端
```

用户安装流程: 解压 → 运行。不需要 MO2，不需要手动排序，不需要逐个配置 MOD。

### 6.3 实现量

| 组件 | 代码量 | 说明 |
|------|--------|------|
| 文件系统分层合并引擎 | ~300 行 | 按 modlist 优先级顺序复制文件 |
| JSON 深度合并 + namespace 展平 | ~200 行 | config.json 冲突字段自动加前缀 |
| 兼容性验证 | ~150 行 | 确保输出目录结构符合 SPT 约定 |
| **总计** | **~650 行** | **3-5 天** |

---

## 七、步骤5实操模拟（已验证）

基于 12 个真实 SPT MOD 源码仓库的交叉对比分析。详见 `步骤5实操模拟-集成可行性演示.md`。

### 核心发现

| 场景 | MOD 组合 | 挑战类型 | 自动合并 | 人工决策 | 集成策略 |
|------|---------|---------|---------|---------|---------|
| A | Softcore + Realism-Server | 经济系统重叠 | 65% | 4 个 | 参数化配置 + 冲突字段优先级策略 |
| B | APBS + Realism-Server | Bot 生成重叠 | 55% | 3 个 | 选主控制器,另一方配置注入 |
| C | botplacementsystem + RaidOverhaul | 正交协调 | 95% | 1 个 | 几乎无需改动 |
| D | TarkovIRL_WHM + Realism-Mod-Client | 客户端硬依赖 | 35% | 3 个 | IL 分析 + 接口抽象 |

**加权平均: AI 可自动处理约 62% 的集成工作。**

推广到全量 150 MOD 的估计: ~25% 完全正交 (90%+ 自动)、~35% 轻量重叠 (70-85% 自动)、~25% 深度重叠 (40-60% 自动)、~15% 硬依赖/核心冲突 (10-35% 自动)。综合自动化率约 **60%**。

结合 IL 反编译管线后（客户端覆盖率 35%→100%），客户端侧的自动化率可额外提升，最终综合自动化率约 **70-75%**。

---

## 八、分阶段实施路线图

### Phase 0（本周）: 基础数据建设

- 完成 `mods.json` 映射表（150 个 MOD 名→GitHub 仓库地址）
- 对 MOD 分类（group 字段）
- 标注已知冲突和设计决策（notes 字段）
- **耗时: 半天**

### Phase 1（1 周）: IL/反编译分析管线

- 开发 `SPT-IL-Toolkit`（基于 Mono.Cecil）
- 功能: 提取 Harmony 补丁目标 + 优先级 + 依赖
- **产出**: 客户端分析覆盖率 35% → 100%
- **为什么先做这个**: 技术风险最低，收益最高，是后续所有分析的基础

### Phase 2（1 周）: 绕过 MO2 自包含打包

- 实现构建时 USVFS 烘焙引擎
- JSON 配置 namespace 展平
- **产出**: 一键输出自包含整合包
- **为什么紧接着做**: 用户体验改善最大，技术实现最简单

### Phase 3（2-4 周）: 源码分析 + 冲突检测引擎

- TypeScript MOD 结构化分析（基于 LLM）
- 交叉对比冲突检测（DI 注册、DB 路径、Harmony 补丁）
- **产出**: 自动化冲突矩阵 + 合并建议

### Phase 4（1-2 月）: 功能域配置模型

- 定义 25 个功能域
- 编写功能域→MOD 实现映射
- 编写兼容性规则引擎
- **产出**: `experience.json` 驱动构建

### Phase 5（3-6 月）: 渐进式 Fork SPT Server（远期）

- 先只 fork DB 加载层 + DI 容器初始化层
- 逐步将高冲突 MOD 编译进 Server
- **前提条件**: SPT API 趋于稳定，整合包规模 > 200 MOD

---

## 九、风险矩阵

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| MOD 作者删库/改私有 | 中 | 高 | 定时 fork 所有源码仓库到自有 org |
| SPT API 破坏性变更（每大版本） | 确定 | 高 | 版本锁定 + 延迟升级 + AI 检测 API 变更 |
| IL 分析遇到混淆 DLL | 极低 | 中 | SPT 生态目前无混淆传统。如有，降级为签名级 API 调用分析 |
| LLM 幻觉导致错误分析 | 中 | 高 | 输出为"建议"而非自动执行。关键路径默认人工确认 |
| LLM API 成本 | 高（初始）/ 低（增量） | 中 | 仅用云模型做复杂分析，本地模型做 diff/检测。初始全量分析约 $5-25 |
| Fork SPT 的法律/许可风险 | 低 | 中 | SPT 本身开源，但需核实反灰条款。Phase 5 作为远期选项，当前不投入 |
| Unity/SPT 版本更新导致 IL 结构变化 | 低 | 低 | Harmony 属性 ABI 稳定。Unity 版本更新不改变 .NET 元数据格式 |

---

## 十、结论

### 直接回答

**"AI 驱动的 SPT 整合包自动化搭建在当前技术上可行吗？"**

可以用三个层次的回答:

**保守回答（仅用当前 AI 能力，不改架构）: ~60% 自动**

- MOD 源码分析、冲突检测、JSON 合并、构建脚本生成都可以自动化
- 但客户端 MOD 的分析覆盖率只有 35%，Harmony 补丁冲突无法静态检测
- 最终仍需 MO2 分发，用户学习成本仍存在

**进取回答（加入 IL 反编译管线）: ~75% 自动**

- 客户端分析覆盖率 35% → 100%，Harmony 冲突可静态检测
- 这是**一周内可实现的**关键突破，不应被无限期推迟

**终极回答（加入功能域配置 + 绕过 MO2）: ~85% 自动 + 用户体验质变**

- 版本复杂度从 150 维降为 1 维
- 用户解压即玩，不需要理解 SPT MOD 生态
- 整合包从不稳定的拼装升级为持续可复现的软件制品

### 核心洞察

> 整合包的质量天花板不由 AI 能力决定，而由"整合包作为 MOD 寄生者"这个架构决定。
> 只要还依赖于 MO2 的运行时分层、150 个独立 MOD 的加载顺序、tsyringe 的 afterResolution 猴子补丁竞争 -- 无论 AI 多强，它也只是在摇摇欲坠的地基上刷漆。
> 
> 把整合包定位为一等软件项目（自有源码仓库、自有构建系统、自有版本号），才是根本性的解决方案。
> AI 是加速这个转型的工具，不是替代这个转型的魔法。

### 立即可以做的事（按优先级）

1. 完成 `mods.json` 映射表（半天）
2. 开发 `SPT-IL-Toolkit` 反编译分析原型（一周）
3. 实现 USVFS 烘焙打包引擎（一周）
4. 基于以上基础设施，逐步实现完整自动化流水线

---

## 十一、附录

### A. 技术参考资料

| 资源 | 链接 |
|------|------|
| SPT 服务器源码 (C#) | https://github.com/sp-tarkov/server-csharp |
| SPT MOD 下载站 | https://forge.sp-tarkov.com/mods |
| MO2 源码 | https://github.com/ModOrganizer2/modorganizer |
| Mono.Cecil (.NET IL 操作库) | https://github.com/jbevain/cecil |
| ICSharpCode.Decompiler | https://github.com/icsharpcode/ILSpy |
| Harmony (运行时补丁框架) | https://github.com/pardeike/Harmony |

### B. 关联文档

- `Docs/mods-template.json` -- MOD 清单模板（150 个条目，待填写 repo/group/role/notes）
- `Docs/步骤5实操模拟-集成可行性演示.md` -- 12 个 MOD 的交叉对比集成分析

---

> **Vault-Tec 免责声明:**
> 本报告基于 2026-05-28 的技术水平。.NET IL 反编译技术已存在超过 10 年，其稳定性远超大多数 SPT MOD。
> AI 技术演进速度约为核战后地表辐射消退速度的 100 倍。半年后请重新评估。
> Vault-Tec 不对因过度自动化导致的 Overseer 职业倦怠承担责任。
> 毕竟 -- Preparing for the Future!

--- 报告结束 ---
