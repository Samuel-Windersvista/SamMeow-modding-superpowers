# 01: 项目脚手架

**What to build:** 建立 `mods/SamMeow-LLMTraderChat/` 项目骨架：三工程（Client＝BepInEx 5 插件空壳、Server＝SPT server mod 空壳含 `IModMetadata`/`OnLoadAsync`、Shared＝常量与协议）、tests 工程（xunit）、README、GLOSSARY、LICENSE（MIT）、.gitignore、目录约定（src/、release/、scripts/）与构建/打包脚本。遵循仓库 `templates/paired-mod` 模板与 `mods/InescapableTarkovsSoftcore416` 的既有惯例（含 `SptRoot` 派生引用）。交付：一条命令构建全绿、测试跑通、打包脚本能产出可部署的归档结构（空壳即可）。

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] 全解决方案构建 0 错误、0 新增警告
- [x] `dotnet test` 跑通（至少 1 个冒烟测试）
- [x] 打包脚本产出可部署归档（Client → BepInEx/plugins、Server → user/mods 的目标结构）
- [x] README 说明项目定位、构建与部署三步；GLOSSARY 收录核心术语（商人对话、检索后端适配口、开场消息）
- [x] 项目为 hub 仓库内普通目录（无嵌套 .git），默认忽略 bin/obj/release 产物

## Comments

- 2026-10-14：工单收口（全部验收项通过）。交付：四工程骨架（Client netstandard2.1 / Server net10.0 / Shared netstandard2.0 / Tests xunit net10.0）+ ADR×3（0001 载体选择 / 0002 LLM 接入 / 0003 检索策略）+ `pack.ps1`（含显式 `-Version` 透传与产物预检）+ README/GLOSSARY/LICENSE/.gitignore（23 个新文件，未提交）。主控台复跑验证：`dotnet test` 5/5（exit 0）；`pack.ps1` 产出 `release/SamMeow.LLMTraderChat-0.1.0.zip`（12 条目基线）；无嵌套 .git。oracle 双轴评审结论：修订后收口（无 BLOCKER）；修订轮已全部完成（ADR×3 + STD-2/3/4/5），复验通过。
- check-mod-standard（4.1.6）FAIL 归属登记（8 项，均不阻塞本工单；发布门禁前处理）：
  - 5 项 → 后续工单自然覆盖：STD-CFG-003（IOnDIConstruct 注册）、STD-CFG-002/005（config.jsonc 等）→ 03/07；STD-CFG-006（客户端 Config.Bind）→ 客户端侧后续或 waiver；STD-CLI-003（HarmonyPatch 缺失）→ 02。
  - 3 项 → 发布门禁前决策（仓库检查器/规格侧既有问题；Inescapable 同样 FAIL）：STD-BUILD-006（检查器只认 `SPTInstallPath`，仓库惯例为 `SptRoot`）、STD-META-003（检查器只认字面量 ModGuid，本交付用 SharedConstants 单源间接引用）、STD-BUILD-001（paired 形态下检查器把多 csproj 拼文取首个 TFM 的伪影，交付侧不可修）。
