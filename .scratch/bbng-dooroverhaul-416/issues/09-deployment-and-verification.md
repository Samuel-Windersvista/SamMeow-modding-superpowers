# 09: 部署与验证收口

**Spec:** `.scratch/bbng-dooroverhaul-416/spec.md`

**What to build:** 部署与收口：打包 MO2 覆盖层（DLL + README + LICENSE + THIRD-PARTY-NOTICES + 偏差表；命名与 meta.ini 符合包惯例）；替换包内 `[8]门的大修-DoorOverhaul`（旧目录保留、一键回滚）；收口 BB 未就绪清单两项；`[15]` 配置叠层做本 mod 配置与既有 DO 配置的对照迁移记录；执行 Level C 全清单复验（破门/破坏/C4/交互/开关/共存）；spec 风险面章节闭环。

**Blocked by:** 06（排除开关体系）、07（C4 客户端行为）、08（C4 服务端组件）

**Status:** ready-for-agent

- [ ] 覆盖层打包完成（命名/meta.ini comments+notes 符合包惯例；版本号与工程一致）
- [ ] 替换部署完成且可一键回滚（旧覆盖层目录保留）；BB 未就绪两项收口；对照表/dev-log 更新（提交由 Overseer 执行）
- [ ] `[15]` 配置对照迁移记录（本 mod 配置项 ↔ 既有 DO 配置；既有调参不丢失）
- [ ] Level C 全清单通过：破门（上锁/近战/容器）、破坏（弹孔/穿透/铰链/门板）、C4 全流程、交互全套、总开关与高危组行为、调试面板、门状态随机化共存
- [ ] README / 许可（MIT × BSD-3）/ THIRD-PARTY-NOTICES / 偏差表齐备；spec 风险面章节闭环
- [ ] 回滚点核对（MO2 profile 备份现状记录）

## Comments
