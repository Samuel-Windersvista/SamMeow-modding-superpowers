---
version: [4.1]
domain: server
topic: recipe
recipe_task: weapon-preset-assort
source: curated
---
# 配方：商人上货武器 preset [4.1]

> 状态：已提炼 + 实战验证（2026-08-05，WarsawTrader 试点：13 把华约武器全部用 preset 上货）
> 适用：给自定义商人卖**整把可用武器**（裸枪含配件）

## 为什么必须用 preset

EFT 的武器模板 ID（如 `_tpl: "59d6088586f774275f37482f"` = AKM）**只是机匣/框架/接收器**，单独卖给玩家是**不可用的残件**（只有核心部件，无枪管/握把/枪托等）。

**整把裸枪 = preset**（globals.json `ItemPresets`），每个 preset 是一个**物品树**：
- `_items[0]` = 武器本体（`_tpl` 指向武器模板 ID，无 parentId）
- `_items[1..n]` = 全部配件（`_tpl` + `parentId` 指向父物品 + `slotId` 指向槽位，如 `mod_gas_block`/`mod_muzzle`/`mod_pistol_grip`）

## 数据来源

- Preset 库：本地 SPT `SPT_Data/Server/database/globals.json` 的 `ItemPresets` 对象（399 个 preset）
- 每个 preset 字段：`_id`（preset id）、`_encyclopedia`（对应武器模板 ID）、`_items`（物品树）、`_changeWeaponName`、`_name`、`_parent`、`_type`
- **`_encyclopedia` = 武器模板 ID**，用它做"武器模板 -> preset"映射

## 步骤

### 1. 找武器的 preset

```javascript
// Node.js
const globals = JSON.parse(fs.readFileSync('.../database/globals.json', 'utf8'));
const presets = globals.ItemPresets;
// 武器模板 -> preset：
for (const [pid, p] of Object.entries(presets)) {
  if (p._encyclopedia === '59d6088586f774275f37482f') { /* AKM 的 preset */ }
}
```

### 2. 重组物品树写入 assort

```javascript
const crypto = require('crypto');
const newId = () => crypto.randomBytes(12).toString('hex');
const preset = presets[akmPresetId];

// 重映射整棵树的所有 _id（避免与其他 mod 冲突）
const idMap = {};
for (const it of preset._items) idMap[it._id] = newId();
const rootId = idMap[preset._items[0]._id];

preset._items.forEach((it, idx) => {
  const entry = { _id: idMap[it._id], _tpl: it._tpl };
  if (idx === 0) {
    entry.parentId = 'hideout';      // 根物品固定
    entry.slotId = 'hideout';
    entry.upd = { UnlimitedCount: false, StackObjectsCount: 100, BuyRestrictionMax: 3, BuyRestrictionCurrent: 0 };
  } else {
    entry.parentId = idMap[it.parentId];  // 配件指向重映射后的父
    entry.slotId = it.slotId;
    if (it.upd) entry.upd = it.upd;
  }
  assort.items.push(entry);
});
// barter_scheme / loyal_level_items 用 rootId 作键
```

### 3. 验证结构

- `assort.items` 里根物品（`parentId == "hideout"`）数量 = 武器数
- 每个 barter_scheme 键都能在根物品 `_id` 中找到（无孤儿键）
- 每个子物品的 `parentId` 都能在 items 中找到（树完整）

## 对照真实数据（Prapor assort）

Prapor（华约武器商人）的 assort.json 里 AKM 就是根物品 + 7 个直接子配件（gas_block/muzzle/pistol_grip 等）的树结构，与 preset 的 `_items` 完全一致。

## 坑

1. **必须重映射 `_id`**：直接复用 preset 的 `_id` 可以工作，但多个 mod 可能撞 ID；安全做法是整棵树重新生成
2. **子物品的 `parentId` 也要重映射**（指向新的根 ID），否则树断裂、武器不可用
3. preset 里 `_items[0]` 可能有 `upd.FireMode` 等字段——保留即可
4. 弹匣/弹药是单物品，不需要 preset，直接 `_tpl` 上货

## 来源

- `globals.json` 的 `ItemPresets`（本地 SPT 安装）
- 实战：`tools/warsaw-trader-mod/data/assort.json`（本仓库，13 把武器 100 条物品树）
