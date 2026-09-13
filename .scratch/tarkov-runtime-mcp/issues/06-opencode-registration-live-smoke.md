# 06: 集成收尾——OpenCode 注册 + 真实 server 冒烟

**What to build:** 从"测试全绿"到"agent 真能用"。将 MCP 以 `tarkov` 名注册进 OpenCode 配置（与 spt/mo2 MCP 并列）；能力清单与最终实现对齐；对真实 SPT 5.0 server（锚定 BEM tag）执行人工/半自动冒烟清单：握手 + 版本门禁一次、全 sections 快照一次、`wait_for` 成功与超时各一次、`raid.*` 占位行为一次；冒烟结果记录到 dev-log（`writing-spt-modpack-devlog` 惯例）。冒烟发现的协议偏差（如 shuffle 实现与真实 server 不符）在本票内修复或降级为 follow-up。

**Blocked by:** 03（快照 sections 补全）, 04（mod 清单）, 05（wait_for）

**Status:** ready-for-agent

- [ ] OpenCode 配置中 `tarkov` MCP 可被 agent 会话发现与调用
- [ ] 真实 server 冒烟：握手成功且版本与锚定 tag 一致
- [ ] 真实 server 冒烟：全 sections 快照返回且数据与游戏内实际一致（人工核对）
- [ ] 真实 server 冒烟：`wait_for` 成功路径与超时路径各验证一次
- [ ] 冒烟结果（含发现的偏差与处置）记录进 dev-log
