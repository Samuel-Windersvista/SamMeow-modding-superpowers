# PerformanceTweaks — 进度档案（下次会话入口）

> 最后更新：2026-08-19 | 当前版本：**v0.3.0（14 补丁 P1-P14）** | 状态：**用户实机测试期**

## 这是什么

SPT 3.11.4（EFT 0.16）客户端性能优化 mod，BepInEx + Harmony，12 个补丁（P1-P12），
全部 Prefix/Postfix（禁 Transpiler）、独立开关、fail-open。

## 当前状态

- [x] EFT 0.16 全量反编译分析（4 路侦察，证据树在 `external/decompile-cache/eft-0.16-spt3114/`）
- [x] T1 五补丁（P1-P5：AI 感知/寻路减负）
- [x] T2 七补丁（P6-P12：引擎/杂项；原 P13 GClass1828 经核实为低频路径，砍）
- [x] 9 仓源码兼容性审计（SAIN/QB/LB/Realism/ThatsLit/Donuts/APBS/ABPS/spt 官方）——零真冲突；
      QB 的 CheckLookEnemy 顺序敏感已用 P1 prefix Priority.Low 消除
- [x] 配置菜单全中文（人话精简版说明）
- [x] 编译 0 错误；BepInEx 日志确认 12/12 补丁加载成功
- [x] NLog.config 调优（4 个 Trace logger → Off；备份 `NLog.config.bak-20260817`）
- [x] AMD Adrenalin 实测对比（灯塔，昨日基线 vs 今日全开）：**平均 FPS 75.2→90.4（+20%），
      帧时间 -15.4%，1% low 基本持平（36.5→37.6）**
- [x] 3114 扩大复查（4.1.2 病灶反查）+ 议会复核（2026-08-19）：**v0.3.0 新增 P13（本地AI的IK隔帧）、
      P14（AI扎堆斥力分帧）**。议会排除项：255 槽（离线不激活，7 条证据链）、CullingManager（规模误读）、
      HairRenderer（死代码）、BTR Load（每局一次）；Impostors 有脏标记非每帧；烟雾/100f 采样/火箭后喷为 4.1 独有
- [x] 实测驱动待议（需 profile 后定）：不可见 bot 动画状态机 dt 补偿分帧、carving 触发源节流、
      LocalAvoidance 已以保守形态进版（拥挤阈值≥4 才分帧）
- [ ] **进行中：用户实战测试（数天）** — 观察 AI 行为是否异常
- [ ] 待定：T3-1（远距子弹视觉降采样，唯一值得做的 T3；需与 Realism 联调）

## 测试期关注项（出问题先查这三个）

1. **P7 远距bot分帧降频** — 行为面最大。远处 AI"发愣"先关它（F12 → P7 → 启用=false）
2. **P5 初始感知距离门限** — 120m 外开局不登记。若感觉 AI"变瞎子"关它
3. **P2 感知调度放宽** — AI 反应变慢明显时调小"感知任务周期"

其他已知事项：
- 1% low 未改善是预期内——极端顿卡源于资源加载/GC 尖峰，不在 P1-P12 覆盖面
- 装 That's Lit 时把 P1 视距倍率调到 2.0+
- 配置文件：`BepInEx/config/com.sammeow.spt311.performancetweaks.cfg`（说明热更新，数值保留）

## 文件地图

| 内容 | 位置 |
|---|---|
| mod 源码 + release DLL | `mods/PerformanceTweaks/`（release/ 下为可部署 DLL） |
| **mod 更新日志** | `mods/PerformanceTweaks/CHANGELOG.md`（v0.3.0 详录：新增什么/排除什么/为什么） |
| 完整分析报告（含 9 仓审计 §7.3） | `docs/eft-0.16-性能分析与优化mod可行性报告.md` |
| 反编译缓存（本体 + spt 插件） | `external/decompile-cache/`（已 gitignore，可用 ilspycmd 重建） |
| KB：性能热点地图 | `knowledge/spt-kb/curated/operations/3114-eft016-perf-hotspots.md` |
| KB：冲突审计 playbook | `knowledge/spt-kb/curated/operations/client-mod-compat-audit-playbook.md` |
| KB：反编译/构建坑 | `knowledge/spt-kb/curated/operations/3114-client-mod-build-gotchas.md` |
| 游戏侧部署 | `E:\Game\EFT_Offline\SPT_3114\BepInEx\plugins\SamMeow.PerformanceTweaks.dll` |

## 常用命令

```powershell
# 重新编译（输出自动复制到 mods/PerformanceTweaks/release/）
dotnet build "mods\PerformanceTweaks\PerformanceTweaks.csproj" -c Release

# 部署到游戏
Copy-Item "mods\PerformanceTweaks\release\SamMeow.PerformanceTweaks.dll" "E:\Game\EFT_Offline\SPT_3114\BepInEx\plugins\" -Force

# 不进战局的加载验证：起 SPT.Server，等 6969 监听，然后：
# EscapeFromTarkov.exe -token=test -config={"BackendUrl":"http://127.0.0.1:6969","Version":"live","MatchingVersion":"live"}
# 查 BepInEx/LogOutput.log 里 "12/12 个补丁生效"
```

## 下次会话怎么接

直接说"继续 PerformanceTweaks"即可。可能的方向：
1. 测试反馈处理（哪个补丁行为异常 → 调参或默认关）
2. T3-1 远距子弹视觉降采样（先读报告 §5 Tier 3 与 KB 热点表 #5/#6，注意 Realism 已接管 CreateShot）
3. 严格 A/B 基准测试（同图同难度，F12 整体开关，2-3 局取平均）
4. 若升级 SPT 4.x：本 mod 全部目标需重新核实（反编译缓存和 KB 记录都要重建）
   → **已核实完毕（2026-08-19）**：12/12 病灶在 4.1.2 存活，改名映射表 + 议会终审移植清单见
   `docs/eft-0.16.9.5-spt412-性能复查报告.md`（v2 含扩大审计 + 议会裁决）。4.1.2 反编译缓存：
   `external/decompile-cache/eft-0.16.9.5-spt412/`。移植 = 10 改名 + 1 重写（P1+烟雾合并）+ 3 新纳入
   → **最终实施计划已定稿（2026-08-19）**：`docs/PerformanceTweaks412-实施计划.md`（16 项四批 + 排除清单 + 盲区清查结论）
   → **412 工程已备妥**：`mods/PerformanceTweaks412/`（骨架 v0.1.0-alpha 编译通过，含独立 STATUS.md 入口）
