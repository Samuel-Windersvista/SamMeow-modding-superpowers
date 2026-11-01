# Door Breach Overhaul（破门大修）（SPT 4.1.6）

> 目标线：**SPT 4.1.6** / EFT 0.16.9.40743 / Mono / BepInEx 5 / netstandard2.1
> 身份：GUID `com.sammeow.doorbreach` · 程序集 `DoorBreachOverhaul` · 版本 `0.1.0`
> 部署目标：MO2 实例 `Norvinsk_Forever`（SPT 4.1.6 / profile Default）
> 上游关系：以 **Backdoor Bandit NG（BB-NG）** master `451c6cb`（MIT，dvize / v-engelowski / kobethuy）
> 为合并基座；**Sage's Door Overhaul v1.0.0**（BSD-3，Sage）的破坏与交互能力域已并入（工单 03 / 04）。

---

## 项目定位

把 BB-NG 的**破门**能力与 Sage's Door Overhaul 的**破坏 / 交互**能力合并为一个自持 SPT 4.1.6 客户端 mod。
本工程不以依赖方式引用上游，而是复制上游源码为本地工程后**独立演进**（便于对账、便于按整合包需要改造）。

**当前状态（工单 06 / 07）**：BB 破门 + DO 破坏 + DO 交互已并入（**上锁门不豁免**，双轨并行）；判定层已纯函数化为 `AttackPolicy`，由 `ApplyHit` 单一分派同时驱动锁血池与破坏；排除体系已重构为配置化 `ExclusionPolicy`（安全类总开关 + 高危组子开关 + 物理不可行硬排除）；C4 引爆已接入 DO 完整破坏管线（门扇脱离/炸飞，门对象保留），不可破坏目标回退强制开门。S0 表驱动单测 100/100。**06 逐开关实测与 07 C4 实机验证待 Overseer**。

---

## 功能总览（当前真实能力）

### 破门（Backdoor Bandit，已可用）

- **锁血池**：铺场时为门 / 容器 / 车辆后备箱分配随机耐久（`Min Hit Points` ~ `Max Hit Points`）。
- **武器白名单**：霰弹枪 / 榴弹发射器 / 近战 / 其他武器 / 弹药分别配置模板 ID 白名单。
- **命中锚定**：门按门把手区域判定有效命中；后备箱 / 容器按锁区域判定。
- **容器与后备箱**：可被射开（本 fork 默认开，见「上游偏差表」）。
- **KeyId 黑名单**：按门 KeyId 排除破门（本 fork 默认清空）。
- **模式**：全放行模式（PlebMode）/ 半放行模式（SemiPleb）/ 破门弹可开金属门。
- **PerfectCulling 空引用防护**：前缀接管 `Toggle`，规避原方法空引用。

### 破坏（Door Overhaul，已并入）

- 弹孔 / 断片 / 碎片迸溅 / 玻璃细节；门板细胞化破坏模拟（`Fracture Cell Size`）。
- 穿透判定：薄木 / 厚木 / 薄金属阈值可配（`Punch-Through Penetration: *`）。
- 铰链可击毁（实验开关）：单铰链悬挂、全毁门板脱落（可配是否阻挡玩家）。
- 门闩损伤与开门配合；门板接管（DoorTakeover）后继续受力。
- 调试面板：命中标记 / 门信息 / 破坏图谱（F12 `7. 调试`）。
- **上锁门不豁免**：上锁门同样承受破坏模拟（与本 fork 的破门锁血池双轨并行，同一发命中同时喂两条线）。

### 交互（Door Overhaul，已并入）

- **Peek**：点按快速开缝 / 按住逐步开到全开（`Seconds To Fully Open`、`Minimum Peek Percent`、`Quick Peek Percent`、`Silent Peek`）。
- **Open Further**：门已部分打开时继续推开至全开。
- **Open Quietly**：按配置幅度缓速、低音量开门（`Quiet Open Percent`、`Quiet Swing Slowdown`、`Quiet Volume`）。
- **体推门**：走向已打开/已开缝的门会把它推得更开（`Body Push`、`Body Push Strength`）。
- **踢门持枪**：踢门时武器保持就绪并可腰射（`Kick Weapon Ready`），武器保持指向视线方向（`Kick Weapon Forward`）。
- **菜单单点注入**：DO 三动作与 Plant Explosive 一次注入、按键名/种类去重；KeyCard 重载复用同入口。

### C4（工单 07，客户端已完成）

- 门 / 钥匙卡门交互菜单中出现 "Plant Explosive" 动作；门控与上游一致：**持有 C4 + 门状态（Shut/Locked/Breaching）+ 破门角度**，任一不满足则菜单项禁用。
- 放置锚定回退链：**锁具 → 门把手 → 门中心**；放置失败（物品模板缺失 / ItemFactory 未就绪 / SetupItem 失败）返回 false，**不扣物品、不发 Fika 包、不启动倒计时**。
- 倒计时与蜂鸣音效（`Explosive Timer Seconds` 可配）；音频缺失时静音运行。
- **引爆接入 DO 破坏管线**：整扇门板一次性脱离（`FractureState.Detonate` → 分组/视觉 → `DoorTakeover` 接管并施加爆炸冲量），门对象保留；爆炸特效与可选范围伤害由 `5. 爆炸` 节控制。
- **不可破坏目标回退强制开门**：安全类/高危组开关关闭、物理不可行、Fika、`Destructible Doors` 关闭或 `Show Holes` 关闭时，C4 仍强制开门（上游“C4 永远有效”语义）。
- **降级路径**：服务端组件缺失时，玩家不会持有 C4，菜单项自然禁用；创建/放置失败只记一次警告，不崩溃、不刷屏。

---

## F12 配置面（当前 7 节：1 / 2 / 3 / 4 / 5 / 6 / 7）

键名英文（cfg 兼容），显示名中文；列表型为逗号分隔字符串。

> 生效时机：**1/2/5/6 节**为战局启动快照（战局中途修改在下一个战局生效，无需重启游戏）；**3. 破坏 / 4. 交互 / 7. 调试**由对应系统实时读值（战局中途修改立即影响新命中 / 交互 / 调试面板）。
> 破门语义提醒：默认（Pleb/SemiPleb 关闭）时，**霰弹枪必须使用弹药白名单内的弹药**（普通鹿弹/独头弹不在白名单 → 不开门）；近战武器受材质限制（金属门除外）；Pleb 放行武器/弹药/材质/黑名单；SemiPleb 放行非金属门的武器/弹药/材质，但仍受 KeyId 黑名单限制。两种模式都**保留锁血池的门把手锁区锚定**（打门板/铰链只会造成破坏，不会提前破门）。

### 1. 通用

| 键名 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `Plugin Version` | string | `0.1.0` | 插件版本（只读） |

### 2. 破门

| 键名 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `Pleb Mode` | bool | `false` | 全放行模式（任何武器均可破门） |
| `Semi Pleb Mode` | bool | `false` | 半放行模式（非金属门任意武器可破） |
| `Breaching Rounds Open Metal Doors` | bool | `false` | 破门弹可开金属门 |
| `Open Lootable Containers` | bool | `true` | 容器可被射开（本 fork 偏离上游默认） |
| `Open Car Doors` | bool | `true` | 后备箱可被射开（本 fork 偏离上游默认） |
| `Min Hit Points` | int | `100` | 门锁耐久下限（范围 1–100000） |
| `Max Hit Points` | int | `200` | 门锁耐久上限（范围 1–100000） |
| `Shotgun Weapons` | string | 11 项 | 霰弹枪白名单（模板 ID，逗号分隔） |
| `Grenade Launcher Weapons` | string | 6 项 | 榴弹发射器白名单（模板 ID，逗号分隔） |
| `Melee Weapons` | string | 5 项 | 近战武器白名单（模板 ID，逗号分隔） |
| `Other Weapons` | string | 空 | 其他武器白名单（模板 ID，逗号分隔） |
| `Whitelisted Rounds` | string | 3 项 | 弹药白名单（模板 ID，逗号分隔） |

### 3. 破坏

| 键名 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `Destructible Doors` | bool | `true` | 可破坏的门（总开关） |
| `Show Holes` | bool | `true` | 显示孔洞（关闭 = 仅模拟，门体不切洞） |
| `Hanging And Falling Doors (Experimental)` | bool | `false` | 铰链脱落：悬挂 / 门板脱落（实验） |
| `Hanging And Fallen Doors Block Players` | bool | `true` | 悬挂 / 脱落门板阻挡玩家与 bot |
| `Hole Size` | float | `2` | 孔洞大小（0.5–3） |
| `Punch-Through Penetration: Thin Wood` | float | `25` | 击穿穿透阈值：薄木（1–100） |
| `Punch-Through Penetration: Thick Wood` | float | `30` | 击穿穿透阈值：厚木（1–100） |
| `Punch-Through Penetration: Thin Metal` | float | `40` | 击穿穿透阈值：薄金属（1–100） |
| `Fracture Cell Size (cm)` | float | `5` | 破坏单元尺寸（3–10；越小越细） |

### 4. 交互

| 键名 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `Kick Weapon Ready` | bool | `true` | 踢门时武器保持就绪并可腰射（阻止开镜） |
| `Kick Weapon Forward` | bool | `true` | 踢门时武器保持指向视线方向（需开启上一项） |
| `Body Push` | bool | `true` | 用身体推开已打开/已开缝的门 |
| `Body Push Strength` | float | `1` | 推门力度（0.3–3） |
| `Seconds To Fully Open` | float | `2.5` | Peek / Open Further 按住到全开所需秒数（0.5–10） |
| `Minimum Peek Percent` | int | `8` | 关闭门开始 Peek 时的初始开缝（1–50） |
| `Quick Peek Percent` | int | `33` | 轻点 Peek 的开度（5–100） |
| `Silent Peek` | bool | `false` | Peek 时压低开门音效 |
| `Quiet Open Percent` | int | `60` | Open Quietly 的开度（10–100） |
| `Quiet Swing Slowdown` | float | `2` | 静开摆动减速倍数（1–5） |
| `Quiet Volume` | float | `0` | 静开音量（0 静音 – 1 正常） |

### 5. 爆炸

| 键名 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `Explosive Timer Seconds` | int | `10` | C4 倒计时（秒）（范围 1–120） |
| `Explosion Does Damage` | bool | `false` | 爆炸造成伤害 |
| `Explosion Radius` | int | `5` | 爆炸半径（范围 0–50） |
| `Explosion Damage` | int | `80` | 爆炸伤害（范围 0–1000） |

### 6. 排除与开关

| 键名 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `Allow Safety Exclusions` | bool | `false` | 解除安全类排除（总开关）：钥匙卡门 / 滑门 / 开关门 / 成对双门 / 非可操作门参与破坏 |
| `Allow High Risk Exclusions` | bool | `false` | 解除高危组排除（子开关）：触发任务门 / 撤离门可被破坏；高危，可能打断任务脚本与撤离交互 |
| `Blacklisted Rooms` | string | 空 | KeyId 黑名单（逗号分隔；默认空，本 fork 偏离上游默认） |

> **物理优先**：即使门类是 `KeycardDoor`，若材质为 `MetalThick`（DO 无破坏档案）、无弹道碰撞体、无几何或异常尺寸，仍归 `physical` 硬排除；安全类总开关不会使其可破坏。调试面板显示 `physical: ...` 原因（门类信息保留在注册表 `DoorClass` 中）。

### 7. 调试

| 键名 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `Show Impacts` | bool | `false` | 显示命中标记（穿透 / 伤害 / 是否击穿） |
| `Show Door Info` | bool | `false` | 显示准星所指门的 ID / 类型 / 材质 / 是否可破坏（及原因） |
| `Show Damage Map` | bool | `false` | 显示准星所指门的破坏图谱（细胞健康 / 孔洞 / 将落件） |

---

## 规则层（Attack Policy）与 S0 单测（工单 05）

- 位置：`src/Rules/`（纯 C#，无 Unity/EFT 依赖）。
- 输入 `AttackInput`：武器/弹药事实、材质、锁锚命中、门类/门状态、配置快照；输出 `AttackDecision`：锁损伤量、破坏是否受理、拒绝原因。
- 单一命中分派：`BallisticCollider.ApplyHit` postfix → `AttackPolicyAdapter` → `AttackPolicy.Evaluate` → 同时喂锁血池与破坏模拟；两轨互不阻塞。
- 排除策略：`ExclusionPolicy`（纯函数）统一判定门类 → 受理/分类/原因；注册表 `DoorDestructionRegistry` 与 `AttackPolicy.EvaluateDestruction` 共用同一函数（不各写一套）。物理不可行项硬排除、永不可开关化；Fika 仍在注册表环境层硬排除。
- 配置快照：`DoorBreachPlugin.RuleConfig`，由 `Settings.BuildRuleConfig(ModConfig)` 在插件启动 / 每个战局启动重建；列表转为 `HashSet` 供 O(1) 判定。
- 扩展位：`RuleConfig` 预留按材质/门类/命中区追加规则组；`AttackInput` / `AttackDecision` 结构保持稳定。
- S0 单测：`tests/SamMeow.DoorBreachOverhaul.Tests`（xUnit / net10.0），只编译 `src/Rules/`，主工程用 `<Compile Remove="tests\**\*.cs" />` 排除测试源码。命令：
  ```
  dotnet test mods/SamMeow-DoorBreachOverhaul/tests/SamMeow.DoorBreachOverhaul.Tests -c Release
  ```
  当前 100 个用例：46 个既有攻击判定用例（武器类别 / 弹药白名单 / 材质 / 锚定 / 门类 / 模式开关 / 血量区间 / 破坏受理 / 双轨独立性）+ 工单 06 新增排除体系用例（默认关闭态回归、仅安全类开、仅高危组开、两者都开、物理不可行恒拒绝、原因按 safety / high risk / physical 可读）。

### 列表型配置载体

- 载体：F12 `2. 破门` 的 `Shotgun Weapons` / `Grenade Launcher Weapons` / `Melee Weapons` / `Other Weapons` / `Whitelisted Rounds`，以及 `6. 排除与开关` 的 `Blacklisted Rooms`。
- 格式：逗号分隔的模板 ID；解析时 Trim 并丢弃空项，可换行/加空格；空字符串 = 空列表。
- 增删条目：在 F12 中追加/删除对应模板 ID（武器/弹药 ID 来自游戏模板，KeyId 来自门对象）；修改在下一个战局启动时进入快照（`1/2/5/6` 节）。
- 默认值：武器/弹药白名单沿用上游；`Blacklisted Rooms` 默认空。

---

## 构建

```
dotnet build -c Release
```

- 覆盖 SPT 根：`dotnet build -c Release -p:SptRoot="D:\SPT"`；
- 覆盖安装路径：`dotnet build -c Release -p:SPTInstallPath="D:\SPT\SPT_41x"`；
- 关闭 release 复制：`dotnet build -c Release -p:SkipCopyToRelease=true`。
- 产物：`bin\Release\DoorBreachOverhaul.dll`（+ `bin\Release\audio\*.mp3`）；构建后复制到 `release\`
  （`release\DoorBreachOverhaul.dll` 与 `release\audio\Beep.mp3`、`release\audio\FinalBeepTone.mp3`）。

---

## 安装

占位：部署与 MO2 覆盖层在**工单 09** 收口；本工单不部署。

---

## API 触点核验（工单 02）

4.1.6 实装（EFT 0.16.9.40743）的触点核实表，按模块分文件、逐项带反编译证据：

- [bb-breach.md](docs/api-touchpoints/bb-breach.md) —— BB 侧 5 补丁目标与门族/容器依赖成员
- [do-destruction.md](docs/api-touchpoints/do-destruction.md) —— Door Overhaul 破坏侧补丁与高风险私有成员
- [do-interactions.md](docs/api-touchpoints/do-interactions.md) —— Door Overhaul 交互侧补丁与输入/踹门成员

结论：全部 `[OK]`，无 `[CHANGED]/[MISSING]`；WTT-ClientCommonLib 经源码扫描确认**非编译必需依赖**。

---

## 上游偏差表（雏形）

| # | 项目 | 上游行为 | 本 fork 行为 | 依据 |
|---|------|----------|--------------|------|
| 1 | 配置载体 | 服务端下发 `/backdoorbandit/load`（JSON） | 本地 BepInEx 配置（F12） | ADR-0002 |
| 2 | 版本门禁 | 无 | 重写为**仅警告**（fail-open，绝不阻止加载） | DebugToolkit 惯例 |
| 3 | 默认值宽松化 | 容器/后备箱默认关、KeyId 黑名单默认 12 项 | 容器/后备箱默认**开**、黑名单默认**空** | 决策 D7 |
| 4 | 补丁绑定 | `GetMethod(name)` 触发 `AmbiguousMatchException` | `AccessTools` 显式选择重载 | 4.1.6 实证 |
| 5 | PerfectCulling 字段 | 4.0 混淆名 `Int_0` / `RuntimeGroupContent_0` | 4.1.6 真名 `runtimeGroupDataSize` / `_runtimeGroupData` | 4.1.6 实证 |
| 6 | 音频路径 | 硬编码 `BepInEx\Plugins\BackdoorBanditClient\audio\...` | 以插件自身目录解析 `audio\...`，缺失静音运行 | 布局一致性 |
| 7 | Fika 子工程与旧服务端载荷 | 含 `BackdoorBanditFika` / `server files` | 未并入（范围）；`FikaBridge` 保留为接入位 | 本工单范围 |
| 8 | 日志 | 3 个独立日志源、英文 "BackdoorBandit:" 前缀 | 单一日志源 `DoorBreachPlugin.Logger`、中文消息、统一前缀 | 本工单约定 |
| 9 | 配置生效时机 | 仅在插件启动时加载一次（服务端下发） | 插件启动 + **每个战局启动**重读 F12 快照（战局中途修改下一局生效） | 单机快照语义（ADR-0002） |
| 10 | 上锁门豁免 | `locked: untouchable`（上锁门不承受破坏） | **移除**：上锁门完整承受破坏（项目准则） | 决策 D6 / R2-Q4 |
| 11 | 命中分派 | DO 用 `collider.OnHitAction` 订阅、BB 用 `ApplyHit` postfix（两套观察者） | 统一 `ApplyHit` postfix 单点：先派发破坏（任意射手），后走 BB 门禁（互不阻塞） | 工单 03（spec 实现决策） |
| 12 | 破坏 / 交互 / 调试配置 | DO 独立 5 节（英文） | 并入本工程 7 节配置面（交互键名按上游语义规范化为 Title Case、显示名中文）；破坏/交互/调试项实时读值 | 工单 03 / 04 |
| 13 | Pleb/SemiPleb 命中锚定 | 上游两种模式完全跳过锁区判定（任意命中积攒锁血池） | 放行武器/弹药/材质/黑名单（**Pleb 绕过黑名单；SemiPleb 仍受限制**），但**锁血池只吃锁区（门把手附近）命中**——避免与破坏/铰链实验互斥（打铰链侧不再提前开门） | 实测反馈 + 用户故事 4（锚定语义保留）；规则层在工单 05 正式化 |
| 14 | 铰链实验实测边界 | 上游只注明“可能卡住 / 穿模 / 表现异常”，无具体数值 | 沿用上游数值与 0.1m 锚点半径。2026-10-19 实测（WoodThin 门，AK-105 PS）：36 发后下铰 44%、上铰 89%；继续射击 → 单铰 FAILED / `leaf Hanging` → 全断后整扇脱框。面板曾在视觉已脱框时仍显示 `Hanging`（上游实验粗糙度）。工单 05 可增加“铰链伤害倍数”旋钮 | 2026-10-19 实测；工单 03 |
| 15 | 判定层 | BB 判定分散在 `DamageUtility` / 补丁分支；DO 破坏独立 | 纯函数 `AttackPolicy` + `AttackPolicyAdapter`；`ApplyHit` 单一分派同时喂两轨；S0 表驱动用例（工单 06 后 100 个） | 工单 05 |
| 16 | 排除体系 | DO 硬编码排除（钥匙卡 / 滑门 / 开关 / 双门 / 任务触发门 / 撤离门 / 非可操作），玩家不可解除 | 重构为 `ExclusionPolicy`（纯函数，注册表与规则层共用）：安全类总开关 + 高危组子开关（默认均关，带警示）+ 物理不可行硬排除（无弹道碰撞体 / 无几何 / 异常尺寸 / 材质 Other）；Fika 仍在注册表环境层硬排除；双门在含安全类的候选集上成对标记并重跑判定，避免半扇可破坏 | 工单 06 / spec 111 / decisions R2-Q4 |
| 17 | C4 引爆语义 | `OpenDoorIfNotAlreadyOpen` 后 `Destroy(door.gameObject)`；锚定 Lock → Handle → Lock 组件 → 门中心 | **不销毁门对象**：`FractureState.Detonate` 整扇门板脱离 → DO 视觉/接管管线（`DoorTakeover` + 爆炸冲量），门对象保留供任务/撤离判定；不可破坏 / 安全类或高危组开关关闭 / 物理不可行 / Fika / 破坏关闭 → 回退强制开门；锚定回退链精简为 Lock → Handle → 门中心；放置失败不扣物品、不发包、不启动倒计时；爆炸伤害按玩家去重并处理半径 0 | 工单 07 / spec 62-68 / 110 |

---

## 版本号一致性（维护者）

三处必须同步：`DoorBreachOverhaul.csproj` 的 `<Version>` / `Plugin.cs` 的 `PluginVersion` / 本 README 头部。

---

## 许可与署名

- 本工程：MIT（`Copyright (c) 2026 SamMeow`，全文见 [LICENSE](LICENSE)）。
- BB-NG：MIT（dvize / v-engelowski / kobethuy 署名，上游 `LICENSE.txt` 为未填占位模板）。
- Sage's Door Overhaul：BSD-3-Clause（Sage）——破坏与交互并入（工单 03 / 04）时补全署名。
- 第三方署名全文见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。
