# 04: server 日志解析——mod 加载清单

**What to build:** Level B 验证的核心断言来源。实现 S2 日志解析器，从 SPT server 日志提取已加载 server mod 清单；`tarkov_server_status` 新增 `mods` 字段输出该清单，并在 schema 标注 `source: "server-log"`；能力自报（`server_status` 的 MCP 能力清单）随当前工具面更新对齐。

**Blocked by:** 01（穿甲弹：server_status 骨架）

**Status:** claimed

- [ ] 日志解析器从 fixture 日志正确提取 mod 清单（含版本号，若日志提供）
- [ ] `server_status.mods` 返回清单且标注 `source: "server-log"`
- [ ] 日志缺失/不可读时 `mods` 返回结构化降级（明确标注不可用原因），不使整个 `server_status` 失败
- [ ] 能力自报反映当前已实现的工具与 sections
- [ ] fixture 覆盖正常日志、空日志、畸形行三种情形
