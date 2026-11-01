# DoorBreachOverhaul × 旧 Door Overhaul 配置对照迁移记录

> 日期：2026-10-25 ｜ 对应工单：09（部署与验证收口）｜ 目的：把旧 DO 覆盖层的既有调参映射到新 mod 的 7 节 F12 配置，确保既有调参不丢失。
> 旧载体：`E:\Game\EFT_Offline\Norvinsk_Forever\mods\[15]《永远的诺文斯克》-客户端MOD-F12设置cfg所在\BepInEx\config\com.sage.dooroverhaul.cfg`（GUID `com.sage.dooroverhaul`）。
> 新载体：`E:\Game\EFT_Offline\Norvinsk_Forever\overwrite\BepInEx\config\com.sammeow.doorbreach.cfg`（GUID `com.sammeow.doorbreach`；BepInEx 运行时写入 `overwrite`，不要改 `[15]` 里的种子文件）。
> 原则：**不重置用户配置**；只补齐旧 DO 调参，不覆盖新 mod 的 BB/C4/白名单/耐久等用户已调值。新 mod **无配置迁移器**（spec 决策），迁移为一次性人工/脚本操作。

## 一、映射总表（旧 DO → 新 DoorBreach）

| 旧节 / 键 | 新节 / 键 | 旧值 | 当前新值 | 处理 |
|---|---|---|---|---|
| 1. General / Kick with weapon ready | 4. 交互 / `Kick Weapon Ready` | true | true | 已一致 |
| 1. General / Kick: keep weapon pointed forward | 4. 交互 / `Kick Weapon Forward` | true | true | 已一致 |
| 1. General / Push doors with your body | 4. 交互 / `Body Push` | true | true | 已一致 |
| 1. General / Body push strength | 4. 交互 / `Body Push Strength` | 1 | 1 | 已一致 |
| 2. Peek / Seconds to fully open | 4. 交互 / `Seconds To Fully Open` | 2.5 | 2.5 | 已一致 |
| 2. Peek / Initial crack (%) | 4. 交互 / `Minimum Peek Percent` | 8 | 8 | 已一致 |
| 2. Peek / Tap peek (%) | 4. 交互 / `Quick Peek Percent` | 33 | 33 | 已一致 |
| 2. Peek / Silent peek | 4. 交互 / `Silent Peek` | **true** | false | **需迁移 → true** |
| 3. Open Quietly / Open amount (%) | 4. 交互 / `Quiet Open Percent` | 60 | 60 | 已一致 |
| 3. Open Quietly / Swing slowdown | 4. 交互 / `Quiet Swing Slowdown` | 2 | 2 | 已一致 |
| 3. Open Quietly / Sound volume | 4. 交互 / `Quiet Volume` | **0.04977376** | 0 | **需迁移 → 0.04977376** |
| 4. Destruction / Destructible doors | 3. 破坏 / `Destructible Doors` | true | true | 已一致 |
| 4. Destruction / Show holes | 3. 破坏 / `Show Holes` | true | true | 已一致 |
| 4. Destruction / Hanging and falling doors (experimental) | 3. 破坏 / `Hanging And Falling Doors (Experimental)` | true | true | 已一致 |
| 4. Destruction / Hanging and fallen doors block players | 3. 破坏 / `Hanging And Fallen Doors Block Players` | true | true | 已一致 |
| 4. Destruction / Hole size | 3. 破坏 / `Hole Size` | 2 | 2.158371 | 保留新值（用户已调） |
| 4. Destruction / Punch-through penetration: thin wood | 3. 破坏 / `Punch-Through Penetration: Thin Wood` | **7.977375** | 25 | **需迁移 → 7.977375** |
| 4. Destruction / Punch-through penetration: thick wood | 3. 破坏 / `Punch-Through Penetration: Thick Wood` | **16.56108** | 30 | **需迁移 → 16.56108** |
| 4. Destruction / Punch-through penetration: thin metal | 3. 破坏 / `Punch-Through Penetration: Thin Metal` | **22.52941** | 40 | **需迁移 → 22.52941** |
| 4. Destruction / Fracture cell size (cm) | 3. 破坏 / `Fracture Cell Size (cm)` | 5 | 5 | 已一致 |
| 5. Destruction Debug / Show impacts | 7. 调试 / `Show Impacts` | false | false | 已一致 |
| 5. Destruction Debug / Show door info | 7. 调试 / `Show Door Info` | false | false | 已一致 |
| 5. Destruction Debug / Show damage map | 7. 调试 / `Show Damage Map` | false | false | 已一致 |

## 二、必须迁移的 5 项（旧 cfg 有调参、新 cfg 尚未继承）

1. `Silent Peek`：`false` → `true`
2. `Quiet Volume`：`0` → `0.04977376`
3. `Punch-Through Penetration: Thin Wood`：`25` → `7.977375`
4. `Punch-Through Penetration: Thick Wood`：`30` → `16.56108`
5. `Punch-Through Penetration: Thin Metal`：`40` → `22.52941`

> `Hole Size`（2 → 2.158371）与 `Hanging And Falling Doors (Experimental)=true` 已是新 cfg 当前值，无需改动。

## 三、新 mod 独有、旧 DO 无对应项（不要从旧 cfg 推断）

- **2. 破门（BB）**：`Pleb Mode` / `Semi Pleb Mode` / `Breaching Rounds Open Metal Doors` / `Open Lootable Containers` / `Open Car Doors` / `Min Hit Points` / `Max Hit Points` / 武器与弹药白名单 / `Blacklisted Rooms`。
  - 当前用户值（2026-10-25）：Pleb=False / SemiPleb=False / 金属门破门弹=True / 容器=True / 后备箱=True / 耐久 200–400 / 霰弹 12（含 `6761b213607f9a6f79017d23` Serbu Super Shorty）/ 弹药 4（含 `5d6e6806a4b936088465b17e` 12/70 8.5mm 鹿弹）/ 黑名单空。**保留，不要用旧 DO 默认值覆盖。**
- **5. 爆炸（C4）**：`Explosive Timer Seconds=15` / `Explosion Does Damage=true` / `Explosion Radius=6` / `Explosion Damage=80`。旧 DO 无对应项；保留当前值。
- **6. 排除与开关**：`Allow Safety Exclusions=False` / `Allow High Risk Exclusions=False`（06 新增）；`Blacklisted Rooms` 空。旧 DO 无对应项。
- **1. 通用**：`Plugin Version=0.1.0`（只读）。
- **7. 调试**：三项与旧 DO 同名同值，已映射。

## 四、应用步骤（建议由 Overseer 执行，agent 不代改运行中 cfg）

1. 完全退出游戏（`EscapeFromTarkov` / `SPT.Launcher` / `SPT.Server`）。
2. 备份当前 cfg：
   `copy overwrite\BepInEx\config\com.sammeow.doorbreach.cfg .artifacts\doorbreach-config-backup-2026-10-25.cfg`
3. 仅修改第二节的 5 个键值（F12 或文本编辑均可；键名英文、节名中文）。
4. 启动游戏进一次战局，确认 F12 中 5 项值生效；`[Settings] 配置快照` 日志会打印快照（不含破坏/交互 5 项，属正常）。
5. 旧 `com.sage.dooroverhaul.cfg` 保留在 `[15]` 作为对照，不删除；旧 DO 覆盖层目录 `[8]门的大修-DoorOverhaul` 保留作回滚。

## 五、验证清单

- [ ] `Silent Peek=true`、`Quiet Volume≈0.0498`
- [ ] 击穿穿透：薄木≈7.98 / 厚木≈16.56 / 薄金属≈22.53
- [ ] `Hole Size=2.158371`、`Hanging And Falling Doors (Experimental)=true` 未被覆盖
- [ ] BB 白名单（Serbu + 12/70 8.5mm）、耐久 200–400、两开关 False 未被覆盖
- [ ] 进战局后 `[Settings] 配置快照` 与 F12 值一致；无配置解析异常
