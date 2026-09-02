# 可行性分析：SPT 3.11.4 mod 转写为 4.1.1 mod

> 日期：2026-08-05
> 分析基于：真实 mod 源码（Forge 归档 18 个 source clone）、迁移文档、API 笔记、模板验证
> 方法：对比 3.11 和 4.1 的 API 表面，评估工具链的自动化支持能力

---

## 结论速览

| 迁移路径 | 性质 | 成功概率 | 依据 |
|----------|------|----------|------|
| **4.0 C# -> 4.1 C#** | 中等迁移（机械修改为主） | **85-95%** | 有官方迁移文档，编译器能抓剩余问题 |
| **3.11 TypeScript -> 4.1 C#（简单 mod）** | 完全重写（语言+框架双换） | **70-85%** | 核心逻辑简单，4.1 文档充分，模板可用 |
| **3.11 TypeScript -> 4.1 C#（复杂 mod）** | 完全重写 | **20-40%** | 代码量大，边缘情况多，需要深度理解原始逻辑 |

---

## 一、3.11 和 4.1 之间的真实差距

### 1.1 服务端：不是迁移，是重写

| 维度 | 3.11 (TypeScript) | 4.1 (C#) | 差距 |
|------|-------------------|----------|------|
| 语言 | TypeScript (Node.js) | C# (.NET 10) | **完全不同** |
| DI 框架 | tsyringe `DependencyContainer` | SPT 4.1 `[Injectable]` 属性 | **完全不同** |
| 入口 | `module.exports = { mod: new X() }` | `IModMetadata` record + `[Injectable]` class | **完全不同** |
| 生命周期 | `IPreSptLoadMod` / `IPostDBLoadMod` | `IOnLoad.OnLoadAsync(CancellationToken)` | **完全不同** |
| 表访问 | `DatabaseServer` / `DatabaseService` (tsyringe 解析) | 构造函数直接注入（`TemplateTable`, `GlobalTable` 等） | **完全不同** |
| 日志 | winston / `InstanceManager.logger` | `ISptLogger<T>` | **完全不同** |
| 文件 I/O | `vfs.read()` | `System.IO` 或 SPT 服务 | **完全不同** |
| 元数据 | 无（package.json 承载） | `IModMetadata` C# record | **新增** |

**实证（ETT mod，Expanded-Task-Text_2153_source）：**

3.11 写法：
```typescript
class DExpandedTaskText implements IPostDBLoadMod {
    postDBLoad(container: DependencyContainer): void {
        this.Instance.postDBLoad(container);
        this.tasks = this.Instance.database.templates.quests;
        this.locale = this.Instance.database.locales.global;
    }
}
module.exports = { mod: new DExpandedTaskText() }
```

4.1 等价写法（需要用工具链生成）：
```csharp
[Injectable(TypePriority = OnLoadOrder.PostLoad + 1)]
public class ExpandedTaskTextEntry(
    TemplateTable templateTable,
    LocaleTable localeTable,
    ISptLogger<ExpandedTaskTextEntry> logger) : IOnLoad
{
    public Task OnLoadAsync(CancellationToken ct) { ... }
}
```

**关键判断：** 这不是"改几行代码"的迁移，而是"拿 3.11 代码当规格说明书，用 4.1 框架重写"。3.11 的 TypeScript 源码的价值在于**业务逻辑**（改什么数据、怎么改），而不是**代码本身**。

### 1.2 客户端：4.0 -> 4.1 主要是改名

| 维度 | 4.0 | 4.1 | 差距 |
|------|-----|-----|------|
| 类名 | 混淆名（`GClass680`） | 真名（`ABotProfileCreator`） | 机械替换 |
| 命名空间 | 无 / 扁平别名 | 真实命名空间 | 需加 using |
| 引用程序集 | 4.0 Assembly-CSharp | 4.1 Assembly-CSharp | 换引用重编译 |
| BepInEx | 5.x | 5.x（不变） | 无 |
| Harmony | 2.x | 2.x（不变） | 无 |

客户端差距远小于服务端。有 `Class_Name_Mappings.md` 对照表，改完类名重新编译即可。

### 1.3 4.0 -> 4.1 服务端：中等迁移

| 改动 | 类型 |
|------|------|
| `AbstractModMetadata` -> `IModMetadata` | 机械（去 override） |
| 删 `IsBundleMod`，加 `HasPrepatcher` | 机械 |
| `OnLoad()` -> `OnLoadAsync(CancellationToken)` | 机械 |
| `DatabaseService` -> 直接表注入 | **需要理解新 DI 模型** |
| `~4.0.0` -> `~4.1.0` |  trivial |
| 重新编译 |  trivial |

有官方迁移文档覆盖全部改动点，编译器能抓漏。

---

## 二、工具链能做什么、不能做什么

### 2.1 工具链的自动化支持

| 能力 | 覆盖 | 具体 |
|------|------|------|
| 4.1 目标 API 文档 | **充分** | 7 篇 API 笔记 + 4 章 modding guide + 11 份配方 |
| 4.1 项目模板 | **充分** | server-mod/client-mod 模板，编译验证通过 |
| 4.1 源码参考 | **充分** | `SamMeow_SPT410_source_code` 完整源码 |
| 3.11 源码理解 | **有限** | 归档有 18 个 source clone，但仅 1 个是 3.11 TS mod |
| 3.11->4.1 迁移指南 | **缺失** | 只有 4.0->4.1 迁移文档，没有 3.11->4.1 |
| 编译验证 | **充分** | `dotnet build` 验证 API 正确性 |
| 加载验证 | **充分** | Level B 标准（启动+日志确认） |
| 冲突分析 | **部分** | MCP 可分析产出 mod 与其他 mod 的冲突 |

### 2.2 工具链不能做的

| 缺口 | 影响 |
|------|------|
| TypeScript -> C# 自动翻译 | 需要 LLM 逐文件重写，无法机械化 |
| 3.11 API -> 4.1 API 映射表 | 不存在，agent 需要同时理解两套 API 自行映射 |
| 业务逻辑等价性验证 | 编译通过 != 行为等价，需要人工功能测试 |
| 复杂 mod 的边缘情况处理 | 代码量大的 mod 有太多隐含假设，LLM 可能遗漏 |

---

## 三、按 mod 复杂度分级评估

### 3.1 简单 mod（1-5 源文件，单一功能）

**特征：** 改几个数据库值、加几个物品、调几个参数。
**例子：** ETT（3 个 TS 文件，核心逻辑 = 改 locale 表的任务描述文本）。

**迁移过程：**
1. Agent 读 3.11 源码，理解业务逻辑（改什么表、改什么 key、怎么改）
2. Agent 查知识库找到 4.1 对应的表和 API
3. Agent 从模板生成 4.1 项目
4. Agent 用 C# 重写业务逻辑
5. 编译验证 + 加载验证

**成功概率：70-85%**

**风险点：**
- 3.11 的某些 API 在 4.1 中没有直接对应（如 `vfs`），需要找替代方案
- 配置文件格式可能不同（3.11 用 JSON config，4.1 也支持但方式不同）
- LLM 可能在边界条件上犯错（空值处理、异常情况）

### 3.2 中等 mod（5-20 源文件，多功能）

**特征：** 自定义商人 + 物品 + 任务，或者有多条路由。
**例子：** 自定义 trader mod（需要操作 TradersTable + TemplateTable + LocaleTable + 可能的路由）。

**成功概率：50-70%**

**风险点：**
- 涉及多张表的交互，映射关系复杂
- 需要理解 3.11 mod 的架构设计（服务拆分、数据流），在 4.1 DI 模型下重新设计
- 可能有 3.11 特有的 workaround 在 4.1 不需要或不可行

### 3.3 复杂 mod（20+ 源文件，深度集成）

**特征：** 全面替换游戏系统（AI、经济、战斗）。
**例子：** SAIN（745 TS 文件，AI 系统完全替换）。

**成功概率：20-40%**

**风险点：**
- 代码量太大，LLM context window 放不下
- 隐含假设和边缘情况太多
- 3.11 和 4.1 的架构差异可能导致某些功能根本无法在 4.1 复现
- 实质上等于"参考 3.11 的设计文档，从零写一个新的 4.1 mod"

---

## 四、提升成功概率的建议

### 4.1 立即可做（工具链内）

1. **写一份 3.11 -> 4.1 API 映射表** -- 把 3.11 常用 API 模式（DatabaseServer、InstanceManager、tsyringe、CommonJS export）映射到 4.1 等价物。这目前缺失，是最大瓶颈。
2. **把 3.11 的 mod 示例注释化** -- 在归档的 3.11 mod 源码旁边生成"4.1 等价写法"注释，作为 agent 的参考。
3. **把 ETT 作为第一个实验对象** -- 只有 3 个源文件，业务逻辑简单，适合验证整个迁移工作流。

### 4.2 中期可做（工作流优化）

4. **建立迁移 checklist** -- 每个 3.11 mod 迁移前，先跑一个固定的检查清单（用了哪些 API、涉及哪些表、有没有路由、有没有 config），生成迁移工作量评估。
5. **分批迁移策略** -- 先迁简单的（改数值的），再迁中等的（加内容的），最后碰复杂的（改系统的）。
6. **人工验证标准** -- 定义"迁移成功"的最低验证标准（编译通过 + 加载无报错 + 核心功能可观察）。

### 4.3 长期可做（工具链扩展）

7. **IL 反编译管线** -- 对无源码的 3.11 mod（只有 DLL），IL 分析可以提取业务逻辑。
8. **3.11 API stub 生成** -- 自动生成 3.11 API 的 C# stub，让 LLM 在翻译时有类型参考。

---

## 五、最终判断

**当前工具链对 3.11 -> 4.1 mod 迁移的可行性：**

- **4.0 -> 4.1：** 可行，高成功率。有文档、有模板、有编译验证。这是"迁移"。
- **3.11 -> 4.1（简单 mod）：** 可行，中高成功率。需要 LLM 做语言翻译，但目标文档充分。这是"参考重写"。
- **3.11 -> 4.1（复杂 mod）：** 不建议在当前阶段尝试。这本质上是"参考旧设计从零写新 mod"，成功率低，不如直接从 4.1 配方出发重新设计。

**最大的缺口是 3.11 -> 4.1 的 API 映射文档。** 补上这个，简单 mod 的成功率可以推到 85%+。
