# 05: MO2 部署

**What to build:** 部署至 `Norvinsk_Forever` 实例：先做 profile 备份，再创建 overlay `[5]软核大修-InescapableTarkovsSoftcore`、以 `build/overlay/` 内容填充、启用，并置于 [5] 组顶部（`基于原玩法的大修（开关此类需重开档）` 分隔线上方）。meta.ini 的 comments/notes 按本包惯例以中文填写（含来源、部署方式与回滚方法）。

**Blocked by:** 02（测试移植与全绿）、03（构建脚本与 overlay 组装）

**Status:** ready-for-agent

- [ ] profile 备份完成（可回滚，记录备份标识）
- [ ] overlay 文件布局 = `SPT_Runtime/user/mods/com.sammeow.inescapable-softcore/{DLL, config.json}`
- [ ] modlist 中 overlay 已启用，且位于 [5] 组顶部
- [ ] meta.ini 中文 comments/notes 完整（来源、部署方式、回滚步骤）
