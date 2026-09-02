---
version: [3.11]
domain: client
topic: operations
source: curated
---
# PerformanceTweaks 制作经验（客户端性能 mod 全流程试点，2026-08-16 ~ 08-19）

> 状态：已验证（mod v0.2.0 部署，灯塔实测平均 FPS +20.2%）
> 产物：`mods/PerformanceTweaks/`；分析：`docs/eft-0.16-性能分析与优化mod可行性报告.md`

## 全流程回顾（可复用的管线）

```
反编译(ILSpy) → 4路并行侦察 → 证据表(文件:行号) → 逐目标读码核实 → 补丁设计(Tier分级)
→ 实现 → 兼容性源码审计 → 编译 → 免战局加载验证 → 实测(AMD Adrenalin CSV 对比)
```

**这条管线被证明是通的**，每一环都有可复用产出（见关联记录）。

## 关键经验（按价值排序）

### 1. 侦察假设必须读码核实，三次误判都是这么抓出来的
- **"SAIN 接管整个感知链"** → 源码审计证伪：SAIN 只 patch 参数级方法，CheckLookEnemy/UpdateLook/CalcPath 本体不碰。若按假设做"检测到 SAIN 就禁用 AI 补丁"，会白白丢掉共存收益
- **"GClass1828 装备观察器是每帧热点"** → 读码发现是事件驱动（OnItemAddedOrRemoved），从补丁清单剔除，省一个无效补丁
- **"docs/internal/superpowers 可以直接归档"** → git grep 发现 20+ 处引用（含 sqlite 二进制），立即回滚移动。目录移动前先查引用

### 2. 补丁工程四原则（验证有效）
1. 全 Prefix/Postfix、禁 Transpiler（spt-singleplayer 在 BotOwner.UpdateManual 上有固定 IL 索引 Transpiler，Transpiler 叠加即死）
2. 每补丁独立开关（F12 热调）——实测出问题时用户可单点关闭
3. fail-open：补丁自身任何异常都放行原逻辑，性能补丁绝不能成为崩游戏的理由
4. 同方法多 mod prefix 的顺序敏感用 `HarmonyMethod(method, Priority.Low)` 让己方后执行解决（QuestingBots 案例）

### 3. 反编译树的正确读法
- 全局命名空间 vs EFT 命名空间分布要早摸清（AI 类全局/BotOwner·Player·GameWorld 在 EFT）
- `[SPTRenamedClass("GClassxxxx")]` 特性是混淆名与真名的桥梁
- 反编译看不到 IL 层事实（如编译器是否缓存委托），标注"需 IL 确认"而不是猜
- 派发链要追到底：Flicker 不是 ComponentSystem 直连，中间隔了 GClass841→Singleton→FlickerSystem 三层

### 4. 实测与估算的关系
- 事先估算：中量战局 +5~12%、重量 +10~20%、1% low 改善为主 → **实测：平均 +20.2%、1% low 持平**
- 教训 1：收益形态判断错了——平均帧涨得比预期多，1% low 没动（极端顿卡源于资源加载/GC，不在补丁覆盖面）。**性能 mod 的承诺应对准"平均帧/帧时间"，不要承诺消灭顿卡**
- 教训 2：AMD Adrenalin 的 CSV 记录（FPS.Latency.*.CSV + Hardware.*.CSV，%LOCALAPPDATA%\AMD\CN）是零成本的 A/B 数据源；Hardware CSV 里 GPU 占用随 FPS 上升是"CPU 瓶颈被松开"的旁证
- 教训 3：单局对比只能算强烈指示；严格 A/B 靠 mod 自带的独立开关（这个设计在测试期回报了价值）

### 5. 配置文案是产品面
- 初版说明是工程师视角（"做什么/默认值由来/调大调小"），被用户要求改成"游戏里发生什么→你会感到什么→副作用"的人话版再精简
- 结论：**写给用户看的说明，先讲感受再讲机制**；数值项必须给"建议上限"（如"N 不超过 4，否则远处 AI 一卡一卡"）

### 6. 产出组织约定（本次确立）
- `mods/<ModName>/` = 源码 + release/ 可部署 DLL（csproj 里 CopyToRelease target 自动复制）
- 反编译缓存入 `external/decompile-cache/` + gitignore + 报告附再生成命令
- 每个 mod 配 STATUS.md 作为跨会话续作入口

## 关联

- 热点地图：`3114-eft016-perf-hotspots.md`
- 审计方法学：`client-mod-compat-audit-playbook.md`
- 构建/反编译坑：`3114-client-mod-build-gotchas.md`
- 续作入口：`mods/PerformanceTweaks/STATUS.md`
