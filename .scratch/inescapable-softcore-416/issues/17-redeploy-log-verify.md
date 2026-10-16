# 17: 重部署与服务器日志核验（0.2.0）

**What to build:** 更新 MO2 overlay `[5]软核大修-InescapableTarkovsSoftcore`（**先做 profile 备份**，流程同 v0.1.0）为 0.2.0 并重启服务器核验：**同类告警（未找到/未命中/跳过）= 0**；B 类物品（8 背包 + Vinyl）变更计数覆盖；各模块 summary 登记；抽查项（新增臂章重量、WTT 背包尺寸、Vinyl 堆叠）交付用户进游戏渐进验收。

**Blocked by:** 16（版本与发行就绪）

**Status:** ready-for-agent

- [x] profile 备份（`.mo2-mcp/profile-backups/`）+ overlay 更新（DLL + config.json + config/ 全树）
- [x] 服务器日志核验：同类告警归零；模块变更计数与预期一致（登记实况）
- [x] 验收登记：port-notes §v0.2.0；用户抽查清单交付（agent 不代启动 MO2/游戏，需用户操作）
- [x] 参考：spec `D4/D6`、v0.1.0 部署流程（port-notes §5）

## Comments

### 部署与日志核验登记（2026-10-17）

**部署（编排者执行，用户配合关闭/启动进程）**
| 步骤 | 证据 |
|---|---|
| profile 备份 | `Default_pre-softcore416-v0.2.0`（5 文件）——`.mo2-mcp\profile-backups\` |
| overlay 更新 | 14/14 文件 SHA256 核对一致（mismatch=0）；DLL `C2E0DE2927F7…4625 55`（334336 B）；config/ 12 表全就位；meta.ini 未动 |
| overlay 启用 | modlist priority 37，`enabled=true` |
| 部署前快照 | 旧 DLL `91B9C9E9…`（v0.1.0）+ config.json；更新后叠加 config/ 子树 |

**服务器日志核验**（`E:\Game\EFT_Offline\Norvinsk_Forever\overwrite\SPT_Runtime\user\logs\spt\spt20261016.log`，v0.2.0 块 L4783–L4794）
- `[ITS] Inescapable Tarkov's Softcore v0.2.0 loaded`（16:12:10）✓
- `[ITS] 合计：执行 7 个模块，变更 17850，警告 0，错误 0，跳过 0` ✓
- 各模块：softcore 16905/0；samuelTweaks 247/0（臂章可掠夺 37 / 近战可掠夺 27 — 父类修复后标签与域真值精确对应）；antigravArmbands 37/0；backpacks 56/0；trueItems 328/0；noFirHideout 277/0；raidDuration 0/0
- 同类告警（未找到 / 未命中 / 跳过）= **0**；外置表 0 重建 / 0 回落消息；全文件 ITS Error / Critical = 0
- B 类覆盖：8 背包全部纳入（56/56）+ Vinyl 随 trueItems 328 项应用（0 告警）

**对比 v0.1.0 基线（同日志历史块）**：backpacks 34+9 告警 → 56+0；trueItems 250+23 告警 → 328+0；antigravArmbands 22 → 37；samuelTweaks 126 → 247（覆盖面扩大 = 阶段后移后内容 mod 物品可见）。

**用户抽查清单（已交付，渐进确认中，非阻塞）**：新增臂章重量 / WTT 背包尺寸 / Vinyl 堆叠 / Developer Pack 8×9 / 新增 barter·clothing·provisions 堆叠抽验。

**回退预案**：禁用或移除 overlay + 恢复 `Default_pre-softcore416-v0.2.0` 备份。
