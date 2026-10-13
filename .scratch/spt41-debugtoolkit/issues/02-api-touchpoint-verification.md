# 02: 触点核实（三源补丁点 → 0.16.9 反混淆面）

**Spec:** `.scratch/spt41-debugtoolkit/spec.md`

**What to build:** 产出三份 API 触点核实表，把三个上游的全部补丁目标与反射触点逐一对照 EFT 0.16.9.40743 反混淆 Managed 程序集（及 BigBrain 1.5.0 DLL），每点给出：目标签名（或「缺失」）、可达性结论、适配方式或降级预案。覆盖范围：

- **Tooltip**（`docs/api-touchpoints/tooltip.md`）：`SimpleTooltip.Show` / `PriceTooltip.Show` / `ItemUiContext.Tooltip`·`.PriceTooltip` / `GridItemView.ShowTooltip` / `TradingItemView.ShowTooltip` / `SlotView.Show` / `ModSlotView.OnPointerEnter`·`TryGetArmorSlot` / `ModdingScreenSlotView.Start` / `QuestListItem.Init` / `NotesTask.Show` / `QuestObjectiveView.Show` / `TraderCard` 指针事件 / 快捷键入口；反射触点 `UIElement."UI"`、`UIInputNode."UI"`、`UIContext."AddDisposable"`、`GUIUtility."systemCopyBuffer"`；私有注入字段 `____slot`、`____trader`。
- **BotOverlay**（`docs/api-touchpoints/bot-overlay.md`）：`BotSpawner.GetBotDebugData`、`DebugBotStruct` 全部消费字段、`CameraManager.SSAA` 宽度取法、`GameWorld.OnGameStarted`；BigBrain 1.5.0 的 `BrainManager` / `CustomLayer` / `BaseLogicLayerSimple` / `CustomLogic` 成员面。
- **DadGamer**（`docs/api-touchpoints/dadgamer.md`）：`ActiveHealthController.ApplyDamage`·`DestroyBodyPart` 签名与语义、`FallSafeHeight`（后备字段）、效果清理接口（`EffectAddedEvent` / `RemoveEffectFromList` / `FindActiveEffect`）、`Player.BeingHitAction` 等价物、`Physical.Stamina`·`HandsStamina`·`Oxygen`、`BackendConfigSettingsClass.BaseLoadTime`·`BaseUnloadTime`，以及**瞬产 / 瞬建 / 负重的 3.8 混淆名 → 0.16.9 真名映射**。

方法：ilspycmd 对 `SPT_41x\EscapeFromTarkov_Data\Managed` 程序集与 BigBrain DLL 反编译核实。产出为文档、不含实现代码；不可达触点必须给出明确降级预案（与 spec「逐项降级」策略一致）。

**Blocked by:** 01

**Status:** ready-for-agent

- [x] 三份核实表覆盖上述全部触点，每点含：目标签名、可达性结论、适配/降级方案（tooltip.md 62 条 / bot-overlay.md 50 条 / dadgamer.md 37 条）
- [x] 混淆名映射（瞬产/瞬建/负重）给出 0.16.9 真名（`ItemsProducerBase.Update` / `AreaData.InitConstructing` / `InventoryEquipment.GetTotalWeight`），未触发降级
- [x] 每触点附一行可直接指导实现的适配指引（供 03/04/05/06 引用）
- [x] 抽查不少于 5 个触点用 ilspycmd 二次验证（各通道 7/6/12 条 + 主控台独立复跑命中）

**上游源码（本仓归档）:** `DrakiaXYZ-BotDebug_918_source`（1.8.0）、`DebugTooltip_1542_source`（5.0.0）、`DadGamerMode_686_source`（1.9.3）；BigBrain DLL 位于 Norvinsk_Forever 覆盖层 `[6]AI拟人MOD的前置框架-BigBrain`。

## Comments

- 2026-10-11（编排者）: **完成**（三通道产出 + 主控台独立抽验）。
  - 交付物：`docs/api-touchpoints/tooltip.md`（62 触点：[OK]56/[CHANGED]5/[UNCERTAIN]1/[MISSING]0）、`bot-overlay.md`（50 触点全 [OK]）、`dadgamer.md`（37 触点：[OK]17/[CHANGED]20/[MISSING]0/[UNCERTAIN]0）。
  - 三条旧混淆名映射全部定位成功：瞬产 → `EFT.Hideout.ItemsProducerBase.Update(float)`；瞬建 → `EFT.Hideout.AreaData.InitConstructing(int,bool)`；负重 → `EFT.InventoryLogic.InventoryEquipment.GetTotalWeight(IEnumerable<Slot>)`（＋`Item.TotalWeight`＋`Slot.ContainedItem`）。
  - 关键纠偏（4.1.6 ≠ 5.0 形态）：`QuestListItem` 2 参未改名（5.0 的 `QuestListItemView` 不适用）；`UIContext` 不存在（`UI` 字段类型为 `UIParent`）；`BackendConfigSettingsClass` 整体移除（→ `GlobalConfiguration`）；`DamageInfo` 为 struct；多类型位于全局命名空间（`DebugBotStruct`/`SSAA`/`BaseLogicLayerSimple` 等）。
  - 主控台独立复跑（ilspycmd，与子通道产出交叉）：QuestListItem 2 参、UIContext 缺失、SimpleTooltip.Show 4 参、BaseLogicLayerSimple 归 Assembly-CSharp、CameraManager 命名空间、ItemsProducerBase.Update、InitConstructing、GetTotalWeight、GlobalConfiguration 0.85/0.3、BackendConfigSettingsClass 缺失——全部命中。
  - 二次核实记录：tooltip V1–V7、bot-overlay V1–V6、dadgamer §4（12 条 ILSpy+反射双方法）。
  - 下游：03/04 已并行施工；05 以 `dadgamer.md` 为核心输入（瞬建行为待 S3 游戏内确认；降级预案见其 §3）。
