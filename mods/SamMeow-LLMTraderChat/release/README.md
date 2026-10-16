# release —— 发行归档

> 本目录仅保留 `.gitkeep` 与 `README.md`；归档 zip 被 `.gitignore` 忽略（`release/*`）。

- 命名：`SamMeow.LLMTraderChat-<version>.zip`
- 结构（顶层按游戏根相对路径）：
  ```
  BepInEx/plugins/SamMeow.LLMTraderChat/
    ├─ SamMeow.LLMTraderChat.Client.dll
    └─ SamMeow.LLMTraderChat.Shared.dll
  SPT_Runtime/user/mods/SamMeow.LLMTraderChat/
    ├─ SamMeow.LLMTraderChat.Server.dll
    ├─ SamMeow.LLMTraderChat.Shared.dll
    ├─ config/
    └─ knowledge/
  README.md / LICENSE / GLOSSARY.md
  ```
- 生成：`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/pack.ps1`
- 解压即得可拖入 SPT 根目录（或作为 MO2 overlay 部署）的目录树。

## 归档清单

| 文件 | 版本 | 备注 |
|---|---|---|
| `SamMeow.LLMTraderChat-0.1.0.zip` | 0.1.0（候选） | 双端同包：客户端插件 + 服务端 mod + 示例配置 + 知识文档 + 中文 prompt 模板 |

### 0.1.0 构建说明

- 生成：`powershell -NoProfile -ExecutionPolicy Bypass -File scripts/pack.ps1`（默认输出到本目录）。
- 内容：`BepInEx/plugins/SamMeow.LLMTraderChat/`（Client.dll + Shared.dll）、
  `SPT_Runtime/user/mods/SamMeow.LLMTraderChat/`（Server.dll + Shared.dll + `config/`（含 `prompts/system-zh.txt`）+ `knowledge/traders/<traderId>/`）、
  以及顶层 `README.md` / `LICENSE` / `GLOSSARY.md`。
- 部署：解压到 SPT 游戏根（合并 `BepInEx/` 与 `SPT_Runtime/`），或作为 MO2 overlay 部署。
- 前置：服务端 `config.jsonc` 需配置 LLM 端点/密钥/模型（默认示例为云端 DeepSeek，可换本地 Ollama）；详见顶层 `README.md`。
- 状态：**候选**（M2/M3 实机验收已通过；剩余 = 正式发布动作；尚未做公开发布）。
