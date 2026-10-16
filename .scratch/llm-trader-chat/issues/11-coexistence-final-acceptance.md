# 11: 共存 + 终验 + 发布准备

**What to build:** 收口与发布准备：与同类聊天 mod（Trash Talk / Miyako，若安装）共存实测；spec 的六条验收清单全部收口；`check-mod-standard -TargetSptVersion 4.1.6` 通过（或豁免有记录）；发布资料齐备（README 含配置指南/隐私说明/LLM 前置条件/AI 生成标注；LICENSE；release 归档结构可部署）；全量构建与测试复跑。

**Blocked by:** 08: mod 商人接入验证；09: M2 验收；10: 检索接线 + 文档内容

**Status:** ready-for-agent

- [x] 共存实测：同类 mod 同装时各自功能正常，或冲突被明确记录与规避
- [x] spec 六条验收清单全部勾选（两商人对对话 / 文档答案 / 重启保留 / 日志无错 / 共存 / 未启用商人无变化）
- [x] `check-mod-standard -TargetSptVersion 4.1.6` 通过或豁免有记录
- [x] 发布资料齐备：README、LICENSE、release 归档可部署
- [x] 全量构建与测试复跑通过（0 错误）

## Comments

- 2026-10-14/15（封卷）：终审「修订后收口（仅文档）」→ 修订轮落地：`docs/acceptance.md` / `README.md` / `release/README.md` 验收状态同步（M2/M3 实测事实 + 证据指针：工单 07/09/10 评论 + `spt20261014.log`）、§三 NIT-2 因果措辞修正、`docs/standards-waiver.md` severity 标注 + 门禁注记。复验 108/108（主控台）。
- **最终部署完成**：NF overlay 刷新至最终构建（Server.dll 78,848 bytes @ 00:32:52；保 key 核验 `sk-75***c1e1`；3 商人；downloads 副本同步）。`[kg] 来源` 日志自本构建起生效（`[LLMTC][kg] 命中数=N 来源=<文件名>`，随下次游玩查看即可见）。
- 共存：按「未同装」结案（Overseer 决策）+ 代码侧分析入 `docs/acceptance.md` §二（经终审对照归档源码抽验属实）；将来同装按其中 3 条观察点核对。
- 豁免：`check-mod-standard` 27 PASS / 3 FAIL（3 条为检查器缺陷误报，已入 `docs/standards-waiver.md`；检查器修复去向已列，门禁注记已写）。
- 遗留（流程外，未执行）：`git commit`、Forge 上传、release notes。
