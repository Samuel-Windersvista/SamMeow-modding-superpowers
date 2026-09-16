# 06: 文档收尾 + code-review

**What to build:** 桥 / MCP README 更新（新端点契约 + 配置表）、devlog 追加、spec 与全部工单状态收尾；两轴 code-review（Standards + Spec）执行并处置发现；按用户规则不提交，提交由用户决定。

**Blocked by:** 05

**Status:** ready-for-human

- [x] 桥 README：/logs/recent、/logs/summary 契约行 + LogWatch* 配置表 + 故障排查补充
- [x] MCP README：logs_recent / logs_summary 工具说明 + 轮询配置
- [x] dev-log 追加一条（本波交付）
- [x] spec 与 01–05 工单状态更新（01–05 全部核销为 ready-for-human；spec 状态按既有约定保留）
- [x] code-review 两轴执行，发现项处置或转记
- [x] 保持未提交状态（用户规则；closeout 复核：无新提交）

## Comments

### 2026-09-16 深夜 两轴评审记录 + 修复处置（orchestrator）

**范围**：logwatch 工作树（桥 + MCP，fixed point = `86af8535`，全部未提交）。

**Standards 轴（@oracle）**：无硬违规；机检 PASS=13 / FAIL=0 / WAIVED=1（STD-CLI-006 既有豁免）。
1. `LogRingBuffer`/`RaidEventBuffer` 照抄 → 记录：第三个消费者出现时提取泛型环（不改）。
2. 归一化两侧不同构 → **已修**（桥侧镜像 TS 放宽 24hex 右边界 + 回归测试）。
3. 死成员（`DefaultMinLevel` / store 级 `MaxGroups`、`OverflowDropped`）→ **已修**（删除；page 级保留）。
4. 移除监听器失败 `LogDebug` → **已修**（`Warning`，STD-LOG-002）。
5. `LogWatchListener`「STD-LOG-003 例外」措辞 → **已修**（改为防递归理由）。
6. `[LogWatch]` 键名冗余 → **已修**（`Enabled`/`MinLevel`/`RingSize`）。

**Spec 轴（@oracle）**：
1. 归一化两侧不同构 → **已修**（同 Standards-2）。
2. server/fatal 降级不可见 → **已修**（可用性元数据 + 4 reason）。
3. `/logs/recent` 级别过滤晚于 limit 截窗（starvation）→ **已修**（先过滤后截窗 + 回归测试）。
4. 记录不改：`since` 双形态（ISO/整数 Ticks，文档+测试齐）；`LogWatchSource` 注入接缝（非桥接缝，符合既有注入模式）。

**修复后验证**：桥 **295/295**；MCP **395/395** + typecheck + build（均为 orchestrator 独立复跑）。

### 2026-09-16 上午 收尾完成（orchestrator）

- **工单 05 live 验收全部通过**（logwatch）：端点上线 / 告警可见性实测 age=1–4s / 刷屏聚合 count=5610 与日志文件逐字吻合 / `since` 增量无缝 0 重复 / level 过滤 / 三通道合并 / 桥故障降级 / fatal 文件级（fixture；真实 `ErrorLog.log` 空）——证据见工单 05 Comments。
- **雷达复测全部通过**（同场 Sandbox 局）：tracked=69（修复前 0）、4719 条价格表、熔断隔离、F12 阈值即时（69→17）、无闪退——证据见工单 05 Comments 与 `docs/dev-log.md` 雷达复测段。
- **落档**：mod README 第 11 项 + §6 复测证据；PROVENANCE 复验行；归档镜像刷新（DLL `76D8C6E3…` 与 MO2 部署副本一致）；devlog 两段；MANIFEST 行更新（11 项）。
- **状态**：01–05 全部核销；全部改动**未提交**——**提交由 Overseer 决定**。
