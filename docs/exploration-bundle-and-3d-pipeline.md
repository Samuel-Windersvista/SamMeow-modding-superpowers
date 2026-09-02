# 探索报告：Bundle 升级路径与 3D 资产自动化管线

> 日期：2026-08-05
> 触发：Overseer 要求探索 mod 中 bundle 的 4.x 升级方案 + Blender MCP 集成可行性
> 数据基础：Life_in_Norvinsk_v0.3.2 整合包（177 mods, 2622 bundles, 17.9 GB）
> 研究报告：[`findings/003-bundle-upgrade-analysis.md`](wayfinder/findings/003-bundle-upgrade-analysis.md) + [`findings/004-blender-mcp-analysis.md`](wayfinder/findings/004-blender-mcp-analysis.md)

---

## 一、核心结论

### 1.1 Bundle 升级：比预期乐观得多

**SPT 3.11 和 SPT 4.1 使用同一个 Unity 版本（2022.3.43f1）。** 2019 -> 2022 的大迁移发生在 SPT 3.10 -> 3.11 之间，而 3.11 -> 4.1 只是同引擎线的 patch 级差异。

| 结论 | 数据 |
|------|------|
| **75-85% bundle 零改动直接迁移** | 模型/贴图/音频/UI 类，二进制兼容 |
| **10-15% 需验证但大概率只改 bundles.json** | 依赖游戏原生 shader bundle 的武器包 |
| **3-8% 需人工重建** | shader bundle、脚本化预制体 |
| **总自动/半自动迁移率：80-90%** | |

**bundle 不是迁移的瓶颈。** 真正的瓶颈是 177 个 mod 的 DLL 重编译（4.1 客户端反混淆 + 服务端 TypeScript->C# 重写）。bundle 只是随 DLL 走的资产。

### 1.2 服务端 bundles.json 零迁移

SPT 的 bundle 分发协议（bundles.json + `/singleplayer/bundles` + CRC32 校验）在 3.11 和 4.1 之间**完全一致**。JSON 层不需要任何改动。

唯一的元数据变化：`IsBundleMod` 字段从 IModMetadata 中删除，4.1 改为检测 `bundles.json` 是否存在。

### 1.3 Blender MCP：有限但有用

| 能力 | 可行性 | 说明 |
|------|--------|------|
| 静态物品建模（loot/容器/钥匙） | **B+ 可行** | 值得做 MVP |
| 武器绑骨/动画 | **D 不可行** | 无原生工具，社区有专门插件解决 |
| AssetBundle 打包 | **不可能** | 必须在带 EFT SDK 的 Unity 内进行 |
| 端到端自动化 | **C** | blender-mcp 只覆盖 30-40% 步骤 |

**blender-mcp 适合作为"3D 资产作者助手"接入，但不是自动化建模管线。**

---

## 二、整合包资产全景（Life_in_Norvinsk_v0.3.2）

### 2.1 规模

| 指标 | 数值 |
|------|------|
| MO2 overlays | 194（含 17 个 separator） |
| 真实 mod | **177** |
| .bundle 文件 | **2622** |
| .bundle 总大小 | **17.9 GB** |
| .ts 源文件 | **1299**（3.11 TypeScript 服务端 mod） |
| .dll 文件 | **137**（BepInEx 客户端 + 服务端 mod） |
| .json 配置 | **1953** |

### 2.2 Bundle 大户

| Mod | Bundle 数 | 大小 | 类型 |
|-----|-----------|------|------|
| WTT Armory | 627 | 8.3 GB | 武器包 |
| Artem | 262 | 1.6 GB | 武器/物品 |
| Echoes of Tarkov | 271 | 1.5 GB | 物品/装备 |
| EpicRangeTime-Weapons | 476 | 1.3 GB | 武器包 |
| WTT-PackNStrap | 42 | 777 MB | 装备 |
| TacticalGearComponent | 129 | 658 MB | 装备 |
| WTT CornerStore | 112 | 559 MB | 物品 |

前 7 个 mod 占了 bundle 总量的 ~80%。

### 2.3 迁移工作量估算

| 类别 | Mod 数 | 迁移路径 | 工作量 |
|------|--------|----------|--------|
| 纯 bundle 资产（无 DLL） | ~30 | bundle 直接复制 | 极低 |
| 简单服务端 mod（1-5 TS 文件） | ~40 | TypeScript -> C# 重写 + bundle 复制 | 低-中 |
| 中等服务端 mod（5-20 TS 文件） | ~50 | TypeScript -> C# 重写 + bundle 复制 | 中 |
| 复杂服务端 mod（20+ TS 文件） | ~10 | 参考重写或重新设计 | 高 |
| 客户端 mod（BepInEx DLL） | ~40 | 反混淆改名 + 重编译 | 低-中 |
| 配对 mod（服务端+客户端） | ~7 | 双线迁移 | 中-高 |

---

## 三、Bundle 迁移策略

### 3.1 主路线：复制即用

```
3.11 mod 目录
  ├── user/mods/<ModName>/
  │   ├── bundles.json        <-- 直接复制（协议一致）
  │   └── bundles/            <-- 直接复制（Unity 版本一致）
  └── BepInEx/plugins/        <-- DLL 需重编译（反混淆）
```

**步骤：**
1. 复制 bundle 文件和 bundles.json 到新 mod 目录
2. 重编译/重写 DLL（这是主要工作量）
3. 启动 SPT 4.1，检查日志中 bundle 加载是否成功
4. 对加载失败的 bundle 做定向排查

### 3.2 定向重建路线（少量）

对加载失败的 bundle：
1. 用 AssetRipper 或 AssetStudio 提取 bundle 内容
2. 在 Unity 2022.3.43f1 + EFT SDK 中重新打包
3. 更新 bundles.json

### 3.3 批量审计（可选优化）

用 UnityPy 写一个批量审计脚本：
1. 读取每个 .bundle 文件的头部信息
2. 识别 Unity 版本、资产类型、shader 依赖
3. 输出风险分级 CSV（绿=直接迁移、黄=需验证、红=需重建）

这可以把 2622 个 bundle 的迁移风险评估从"全部人工检查"压缩到"只看黄和红"。

---

## 四、Blender MCP 集成方案

### 4.1 定位

blender-mcp 作为**第五个独立 MCP server**追加到 OpenCode 配置，与现有 spt/xedit/bgs_kb/mo2 并行。定位是"3D 资产作者助手"，不是自动化建模管线。

### 4.2 适用场景

| 场景 | 可行 | 说明 |
|------|------|------|
| 新建简单物品（罐头、钥匙、容器） | Yes | blender-mcp 建模 -> .blend -> Unity 打包 |
| 修改现有模型（调尺寸、换贴图） | Partial | 需要 execute_blender_code，可行但脆弱 |
| 新建武器模型 | No | 绑骨/动画超出 MCP 能力 |
| 批量 bundle 处理 | No | 用 UnityPy/AssetRipper 更合适 |

### 4.3 集成架构

```
用户: "我要一个新的罐头物品"
  |
  v
spt MCP: 查知识库 -> 获取物品模板和约定
  |
  v
blender-mcp: 建模 -> 截图校验 -> 导出 .blend
  |
  v
[人工/Unity batch mode]: Unity + EFT SDK 打包 bundle
  |
  v
spt MCP: 注册 bundles.json -> 写入 mod 目录
```

### 4.4 环境要求

- Blender 3.0+（GUI 必须在线，非 headless）
- Python 3.10+
- uv 包管理器（`uvx blender-mcp` 启动）
- Windows 需处理 uvx PATH 问题

---

## 五、迁移优先级建议

按 ROI 排序（投入少、见效快优先）：

| 优先级 | 动作 | 影响 |
|--------|------|------|
| **P0** | 写 3.11 -> 4.1 API 映射表 | 解锁所有服务端 mod 迁移 |
| **P0** | 迁移纯 bundle mod（~30 个） | 零代码改动，直接复制 |
| **P1** | 迁移简单服务端 mod（~40 个） | 业务逻辑简单，模板可用 |
| **P1** | 迁移客户端 mod（~40 个） | 反混淆改名 + 重编译 |
| **P2** | UnityPy 批量 bundle 审计 | 2622 个 bundle 的风险分级 |
| **P2** | 迁移中等服务端 mod（~50 个） | 需要更多 LLM 判断 |
| **P3** | blender-mcp 集成 MVP | 静态物品建模能力 |
| **P3** | Unity batch mode bundle 打包脚本 | 消除打包人工步骤 |
| **P4** | 迁移复杂 mod（~10 个） | 可能需要重新设计 |
| **P4** | blender-mcp 武器建模 | 不可行，等社区工具 |

---

## 六、风险与不确定性

| 风险 | 概率 | 缓解 |
|------|------|------|
| bundle 加载失败率高于预估 | 中 | 先做 3-5 个武器 mod 的实机验证 |
| 4.1 反混淆导致 MonoBehaviour 脚本丢失 | 低-中 | 审计脚本标记含脚本预制体的 bundle |
| 3.11 mod 的隐含 API 依赖在 4.1 中不存在 | 中 | API 映射表 + 编译验证 |
| Unity batch mode 无法完全自动化 bundle 打包 | 中 | 保留人工打包作为 fallback |
| blender-mcp execute_blender_code 的安全风险 | 低 | 只读默认 + 变更需显式同意 |

---

## 七、下一步行动

1. **写 3.11 -> 4.1 API 映射表** -- 这是解锁所有迁移工作的关键前置
2. **拿 ETT（Expanded Task Text）做第一个迁移实验** -- 3 个 TS 文件，简单可控
3. **复制纯 bundle mod 验证 bundle 兼容性** -- 选 2-3 个零 DLL 的 mod 直接拷贝验证
4. **UnityPy 批量审计脚本** -- 2622 个 bundle 的风险分级

---

> 详细技术数据见：
> - `docs/wayfinder/findings/003-bundle-upgrade-analysis.md` -- Unity 版本映射、兼容性规则、工具链
> - `docs/wayfinder/findings/004-blender-mcp-analysis.md` -- blender-mcp 能力、集成架构、可行性评级
