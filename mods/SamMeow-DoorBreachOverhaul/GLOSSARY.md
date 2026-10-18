# GLOSSARY — Door Breach Overhaul（SPT 4.1.6 线）

> 术语表（glossary）。只记录项目语言，不记录实现细节与规格。

## 攻击与终端语义

- **破门（Breach）**：通过攻击（枪械/近战/爆炸）使门从关闭或上锁状态变为可通过的行为——玩家视角的"进门"手段。
  _Avoid_：开锁（指用钥匙的正常开启）
- **破坏（Destruction）**：对门体本身的物理毁伤——弹孔、碎片、铰链与门闩损伤、门板脱落；源自 Door Overhaul 的能力域。
  _Avoid_：损坏（过泛）、破门（那是开门语义）
- **锁血池（Lock HP Pool）**：对门锁的有效命中（门把手附近区域）累积的耐久值；归零触发开门（原生踹门动画）；源自 Backdoor Bandit 的能力域。
  _Avoid_：门血量（会与破坏模拟的细胞健康度混淆）
- **双轨语义（Double-track Semantics）**：同一发命中同时被两套系统消费——锁血池决定"何时能开门"，破坏模拟决定"门被打成什么样"；两者互不阻塞。
  _Avoid_：双重伤害
- **上锁门不豁免（No Locked Exemption）**：项目准则—— `EDoorState.Locked` 不再是攻击的豁免理由；原 Door Overhaul 的 `locked: untouchable` 规则在本项目移除。
- **C4**：放置式爆炸破门物品（Backdoor Bandit 体系）。引爆映射为最强的破坏输入：走完整破坏管线；目标门不可破坏时回退为强制开门。

## 排除与开关

- **工程性排除（Engineering Exclusion）**：对特定门类的破坏豁免（钥匙卡门、滑门、开关门、成对双门、触发任务门、撤离门；另有无弹道碰撞体/无几何/异常尺寸等物理不可行项）。除物理不可行项外，均可由配置解除。
  _Avoid_：限制清单
- **高危组（High-risk Set）**：工程性排除中破坏后可能损伤任务脚本或撤离交互的门类（触发任务门、撤离门）；由独立子开关控制、默认保持排除。
- **KeyId 黑名单（BlacklistedRooms）**：按门 KeyId 排除破门的房间列表（Backdoor Bandit 机制）；本项目默认清空、机制保留。
- **总开关（Master Switch）**：一键解除全部"安全类"工程性排除的配置项。

## 目标与范围

- **容器与后备箱（Containers & Trunks）**：LootableContainer 与车辆后备箱；与门共用同一命中链与破门语义（可配置）。
- **服务端组件（Server Component）**：为 C4 等自定义物品提供注册、配方与资源的 SPT 服务端 mod；与客户端主线解耦，独立工单推进。
