# Door Breach Overhaul（破门大修）（SPT 4.1.6）

> 目标线：**SPT 4.1.6** / EFT 0.16.9.40743 / Mono / BepInEx 5 / netstandard2.1
> 身份：GUID `com.sammeow.doorbreach` · 程序集 `DoorBreachOverhaul` · 版本 `0.1.0`
> 部署目标：MO2 实例 `Norvinsk_Forever`（SPT 4.1.6 / profile Default）
> 上游关系：以 **Backdoor Bandit NG（BB-NG）** master `451c6cb`（MIT，dvize / v-engelowski / kobethuy）
> 为合并基座；**Sage's Door Overhaul v1.0.0**（BSD-3，Sage）的破坏与交互能力域**待并入**（后续工单）。

---

## 项目定位

把 BB-NG 的**破门**能力与 Sage's Door Overhaul 的**破坏 / 交互**能力合并为一个自持 SPT 4.1.6 客户端 mod。
本工程不以依赖方式引用上游，而是复制上游源码为本地工程后**独立演进**（便于对账、便于按整合包需要改造）。

**当前状态（工单 03）**：BB 破门 + DO **破坏系统**已并入（**上锁门不豁免**，双轨并行）；DO 交互能力（Peek / Open Further / Open Quietly / 推门 / 踢门持枪）待工单 04。

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

### C4 菜单项（部分可用）

- 门 / 钥匙卡门交互菜单中出现 "Plant Explosive" 动作；放置 C4 → 倒计时（蜂鸣音效）→ 引爆 → 开门。
- **物品与配方依赖服务端组件**（独立工单，当前未随包分发）：玩家无 C4 物品时，该菜单项自然禁用。
- 音频资源随插件目录解析（`audio\Beep.mp3` / `audio\FinalBeepTone.mp3`）；缺失则 C4 静音运行，不影响其它逻辑。

---

## F12 配置面（当前 6 节：1 / 2 / 3 / 5 / 6 / 7；4. 交互 预留至工单 04/05）

键名英文（cfg 兼容），显示名中文；列表型为逗号分隔字符串。

> 生效时机：**1/2/5/6 节**为战局启动快照（战局中途修改在下一个战局生效，无需重启游戏）；**3. 破坏 / 7. 调试**由破坏系统实时读值（战局中途修改立即影响新命中 / 调试面板）。
> 破门语义提醒：默认（Pleb/SemiPleb 关闭）时，**霰弹枪必须使用弹药白名单内的弹药**（普通鹿弹/独头弹不在白名单 → 不开门）；近战武器受材质限制（金属门除外）；Pleb/SemiPleb 放行武器/弹药/材质/黑名单，但**锁血池仍只吃门把手锁区命中**（打门板/铰链只会造成破坏，不会提前破门）。

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
| `Blacklisted Rooms` | string | 空 | KeyId 黑名单（逗号分隔；默认空，本 fork 偏离上游默认） |

### 7. 调试

| 键名 | 类型 | 默认 | 说明 |
|------|------|------|------|
| `Show Impacts` | bool | `false` | 显示命中标记（穿透 / 伤害 / 是否击穿） |
| `Show Door Info` | bool | `false` | 显示准星所指门的 ID / 类型 / 材质 / 是否可破坏（及原因） |
| `Show Damage Map` | bool | `false` | 显示准星所指门的破坏图谱（细胞健康 / 孔洞 / 将落件） |

> 节 4「交互」为工单 04/05 预留。

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
| 12 | 破坏 / 调试配置 | DO 独立 5 节（英文） | 并入本工程 6 节配置面（键名沿用、显示名中文）；破坏/调试项实时读值 | 工单 03 |
| 13 | Pleb/SemiPleb 命中锚定 | 上游两种模式完全跳过锁区判定（任意命中积攒锁血池） | 放行武器/弹药/材质/黑名单，但**锁血池只吃锁区（门把手附近）命中**——避免与破坏/铰链实验互斥（打铰链侧不再提前开门） | 实测反馈 + 用户故事 4（锚定语义保留）；规则层在工单 05 正式化 |

---

## 版本号一致性（维护者）

三处必须同步：`DoorBreachOverhaul.csproj` 的 `<Version>` / `Plugin.cs` 的 `PluginVersion` / 本 README 头部。

---

## 许可与署名

- 本工程：MIT（`Copyright (c) 2026 SamMeow`，全文见 [LICENSE](LICENSE)）。
- BB-NG：MIT（dvize / v-engelowski / kobethuy 署名，上游 `LICENSE.txt` 为未填占位模板）。
- Sage's Door Overhaul：BSD-3-Clause（Sage）——破坏与交互并入（工单 03）时补全署名。
- 第三方署名全文见 [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md)。
