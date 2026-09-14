# 10: writing-spt-mod 技能改造

**What to build:** 更新 `skills/writing-spt-mod/SKILL.md`：server/client/paired 三条流程引用三套模板与规则 ID（如"按 `STD-STRUCT-001` 组织目录"）；补豁免记录流程（`Waiver: STD-XXX-nnn` 写入 mod README 或 dev-log）；保留既有反模式。验证：技能文本自审 + 按技能流程走一遍（引用检查）。

**Blocked by:** 01, 08, 09

**Status:** done

- [x] 三条流程均引用模板与规则 ID
- [x] 豁免流程写入技能
- [x] 走查一遍流程，引用无悬空（规则 ID 与模板路径均存在）
- [x] 既有反模式未被删除

## Comments

### 2026-09-14 — 技能改造完成

**改动文件：** `skills/writing-spt-mod/SKILL.md`（唯一源码改动）

**三条流程改动摘要：**

- **Server**（模板 `templates/server-mod/`）：Step 3 补仓库布局规则（`STD-STRUCT-001/003/005/006`、`STD-META-001/002`）；Step 4 补元数据（`STD-META-003/004/005`）、构建（`STD-BUILD-001/004/005/006`）、DI/生命周期（`STD-SRV-001/002/003/004`）、配置（`STD-CFG-001/002/003/004/005`）、路由（`STD-SRV-005/006/007`）、日志（`STD-SRV-008`/`STD-LOG-001/004/005`）、依赖（`STD-DEP-001/002`）、bundle（`STD-BND-001/002`）；Step 5 补 `STD-VERIFY-001/002/003/009`。
- **Client**（模板 `templates/client-mod/`）：Step 3 补 `STD-STRUCT-001/003/005/006`、`STD-BUILD-002/003/005/006`；Step 4 补入口/补丁/配置/日志/依赖（`STD-CLI-001..007`、`STD-META-005/006`、`STD-CFG-006`、`STD-LOG-003`、`STD-DEP-004/005`、`STD-PERF-004`）；Step 5 补 `STD-VERIFY-001/004`。
- **Paired**（模板 `templates/paired-mod/`）：补单仓库分层 `STD-STRUCT-004`、两端同版本 `STD-META-007`/`STD-PKG-005`、路由 `STD-SRV-005/006/007`、打包 `STD-PKG-001/003/004/006`。

**引用的 Rule ID 清单（60 个，全部经 `### STD-... — ` 标题检索命中）：**
STD-BND-001/002、STD-BUILD-001..006、STD-CFG-001..006、STD-CLI-001..007、STD-DEP-001/002/004/005、STD-LOG-001/003/004/005、STD-META-001..007、STD-PERF-004、STD-PKG-001/003/004/005/006、STD-SRV-001..008、STD-STRUCT-001/003/004/005/006、STD-VERIFY-001/002/003/004/009。

**豁免流程段落摘要：** 新增「豁免流程（Waiver）」章节，指向 `knowledge/spt-kb/curated/modding-standard/README.md` 的「豁免流程（Waiver）」；规定仅 MUST 可豁免、SHOULD 只需一句话理由、MAY 不适用；记录位置为 mod README 或项目 dev-log；记录内容为理由 + 替代方案；标记格式 `Waiver: STD-XXX-nnn`，附 markdown 示例。

**引用验证结果：**

- Rule ID 走查：60 个引用全部在 `knowledge/spt-kb/curated/modding-standard/*.md` 中以规则标题形式命中，**悬空 0 个**。
- 路径走查：23 条模板/文档路径 `Test-Path` 全部通过，**失败 0 个**（含三套模板目录与 README、标准 README/01/13/version-matrix、index.json、recipes、api-notes-4.1、modding-guide、wiki、external/spt-archive/modules、docs/wayfinder 发现文件）。
- 既有反模式：5 条原文保留未删改。
