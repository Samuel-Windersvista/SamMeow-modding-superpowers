# 14: 处理阶段后移（Preload+1 → PostLoad+n）

**What to build:** 入口 `TypePriority` 后移至 `OnLoadOrder.PostLoad + n`（候选；以实现期运行时枚举与日志实测为准），使包内内容 mod（WTT-ContentBackport/Armory/Artem、Epic AIO、TGC-NG、Russian-SF-Pack 等）注入的物品对模块可见——B 类 8 条背包 + Vinyl 1 条由「告警跳过」转为**真实生效**。内部模块固定顺序（G6→G1→G4→G5→G2→G3→G7）不变。

**Blocked by:** None（可立即开始）

**Status:** ready-for-agent

- [ ] 阶段后移（含风险核对：确认无组依赖早于内容注入的窗口；如个别组需保留早相位，逐组论证并记录）
- [ ] 现有 215 项测试保持绿
- [ ] 备注：端到端验收（日志同类告警=0、变更计数覆盖 B 类物品）在 17 执行；本工单只落实现与静态验证

## Comments

- 证据与候选：`spec-v0.2.0.md` D4（时序实证 14:39:02 vs 14:39:05/08；4.1 阶段常量表）。
- 2026-10-16 实施完成：
  - 改动点：`src/InescapableTarkovsSoftcore/ModLoadHook.cs` 的 `[Injectable(TypePriority = ...)]` 由 `OnLoadOrder.Preload + 1` 改为 `OnLoadOrder.PostLoad + 1`，并把 XML 文档注释重写为 PostLoad 窗口语义（运行于全部 mod 装载回调之后、包内内容 mod 物品注入之后）；无其他逻辑改动。
  - 测试：`tests/.../ModLoadHookTests.cs` 用例改名 `Loader_IsInjectable_AtPreloadPlusOne` → `Loader_IsInjectable_AtPostLoadPlusOne`，断言改为 `OnLoadOrder.PostLoad + 1`。
  - n=1 的理由：PostLoad 是 4.1 阶段链的最末窗口（Watermark 0 / Preload 100000 / GameCallbacks 200000 / TraderRegistration 300000 / Routers 400000 / HandbookCallbacks 500000 / … / PostLoad）；内容注入 mod（Croupier 14:39:05、TGC 14:39:08）晚于原入口 14:39:02，故需后移至内容注入完成之后。n=1 保证排在全部内容注入之后（同相位按 ModGuid 字母序，+1 无冲突）。
  - grep `Preload`（src/ + tests/）：仅上述两处，已全部改毕；README、config 未动。
  - ModuleOrchestrator.cs 未动（归并行 lane 08）。
  - 测试：`dotnet test "mods\InescapableTarkovsSoftcore416\InescapableTarkovsSoftcore.sln" --artifacts-path .artifacts/its-14 -c Debug` → 通过 216 / 失败 0 / 跳过 0（相对工单基线 215 的 +1 来自并行 lane 15 对 SamuelTweaksModuleTests 的追加，非本工单引入）。
  - 遗留：端到端验收（日志同类告警=0、变更计数覆盖 B 类 8 条背包 + Vinyl 1 条）待工单 17 实测。
