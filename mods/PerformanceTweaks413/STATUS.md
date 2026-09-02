# PerformanceTweaks413 — 进度档案（下次会话入口）

> 创建：2026-08-19 | 当前版本：v0.3.0（P1-P16 全部落地，待部署验证） | 状态：**实施中**

> **2026-08-21 外部事件评估**：SPT 官方归档 + SP-Tushonka fork 发布 4.1.3。
> 对本项目**零影响**：4.1.3 客户端版本要求不变（EFT 0.16.9.40743），Assembly-CSharp 未动，
> 补丁目标全部有效。server 侧变化（命名空间/程序集名保留）也不影响本客户端 mod。
> 依据：`knowledge/spt-kb/curated/operations/413-fork-transition.md`。
> 后续若迁移到 4.1.3 安装目录，仅需改 csproj 的 SPTInstallPath。
>
> **2026-09-01 复查**：fork 活跃开发在 `4.1x-dev` 分支（4.1.3 后 +12 commits 至 08-27，
> 全是服务端修复/行为调整；IModMetadata/ModLoader/客户端版本要求零改动）。
> 对本客户端 mod **仍零影响**。两个源码 fork 均已同步：`SamMeow_SPT410_source_code`
> （tushonka remote 已 fetch 最新 4.1x-dev）、`SamMeow_SP-Tushonka_source_code`
> （upstream/4.1x-dev 已由本地对穿同步至 bb102040）。

## 这是什么

SPT 4.1.2（EFT 0.16.9.5）客户端性能优化 mod，3.11 版 PerformanceTweaks 的移植+扩展版。
与 3.11 版（`mods/PerformanceTweaks/`，net471）**独立演进，不共享源码**（框架、混淆名、病灶均有差异）。

## 筹备状态

- [x] 病灶复查：12/12 在 4.1.2 存活（`docs/eft-0.16.9.5-spt412-性能复查报告.md`，含改名映射表）
- [x] 扩大审计 + 议会复审（同文档 v2：255 槽转 Fika、P12 单机坐实等）
- [x] 五盲区清查（动画/NavMesh 收编，音频/任务证伪）
- [x] 实施计划定稿：`docs/PerformanceTweaks413-实施计划.md`（16 项四批 + 排除清单）
- [x] 4.1.2 反编译缓存：`external/decompile-cache/eft-0.16.9.5-spt412/`（8620 文件，ILSpy 11 零错误）
- [x] 工程骨架：csproj（netstandard2.1）+ Plugin.cs 骨架，**编译 0 错误已验证**
- [x] 4.x 生态兼容性审计（2026-08-21）：SAIN 4.x 零真冲突、QB 4.x 仍 patch CheckLookEnemy
      （P1 已用 Priority.Low）、P5 因 SAIN 登记链路默认关闭
- [x] 第一批实施（12 项 = 10 默认开 + P2/P5 默认关），v0.2.0 编译通过并已部署到 SPT_410
      （详见 CHANGELOG.md；映射表修正 1 处：P12 参数实为 IBallisticsCalculator）
- [ ] 加载验证（下次启动游戏查 LogOutput.log 的 12/12）

## 开工顺序（照计划批次）

1. **前置：兼容性审计**——对工作区 4.x 版 mod 源码重跑 playbook 审计
   （3.11 结论不能继承）。现成对象：`E:\云文件\GitHub\Moew-SAIN-For-4013`（SAIN 4.x），
   其余 4.x 版 QB/Realism 等按其落地情况补充。方法：`knowledge/spt-kb/curated/operations/client-mod-compat-audit-playbook.md`
2. **第一批 10 项改名移植**——按 `eft-0.16.9.5-spt412-性能复查报告.md` §2 改名映射表；
   源码可参考 `mods/PerformanceTweaks/src/Patches/`（3.11 版同名补丁），注意 P1 部位容器结构差异（§3.1）
3. **第二批 1 项重写**（P1+烟雾合并拦截，距离门限前置到 IsRayIntersectAnySmoke 之前）
4. **第三批 3 项新病灶**（IK 隔帧——3.11 版 P13 已有参考实现；carving 合批；扎堆斥力——3.11 版 P14 参考）
5. **第四批 2 项实测驱动**（Impostors/CullingManager，先 profile）

## 关键约定

- 纪律同 3.11：仅 Prefix/Postfix、独立开关、fail-open、配置全中文人话版
- 验证：每批编译 → 免战局加载验证（-token 直启 + LogOutput.log）→ Level B 战局冒烟
- 实测基线：AMD Adrenalin CSV 对比法（同图 A/B），验收看平均帧 + 1% low（carving 项）
- 版本号：v0.1.0-alpha 起步，第一批落地升 v0.2.0，全部四批完成升 v1.0.0
- 部署目标：`E:\Game\EFT_Offline\SPT_410\BepInEx\plugins\`（骨架 DLL 已可部署验证加载，但建议第一批落地后再装）

## 文件地图

| 内容 | 位置 |
|---|---|
| 实施计划（人话版作用+预期提升） | `docs/PerformanceTweaks413-实施计划.md` |
| 病灶复查 + 改名映射 + 议会裁决 | `docs/eft-0.16.9.5-spt412-性能复查报告.md` |
| 3.11 版参考实现 | `mods/PerformanceTweaks/`（v0.3.0，14 补丁） |
| 4.1.2 反编译缓存 | `external/decompile-cache/eft-0.16.9.5-spt412/` |
| 迁移主线背景 | `docs/wayfinder/handoff-20260819.md`（412 版可并入 ticket #9 客户端验证批次） |
