# 01: 穿甲弹——脚手架 + 传输层 + 握手门禁 + 首个工具

**What to build:** 从零到第一个可演示行为的完整窄切片。建立 `tarkov-runtime-mcp` TypeScript 工程（复用 spt-mcp/mo2-mcp 的工具注册、类型与 vitest 测试惯例）；实现 S1 传输层抽象（`SptConnection` 形态，zlib 压缩、`PHPSESSID` cookie、5.0 shuffle 加解密全部封装其后，实现依据 `knowledge/spt-kb` 的 5.0 api-notes 与源码 `RequestEncryptionUtil`）；实现连接握手（候选端口探测，默认 6969 可配置覆盖；`/singleplayer/settings/version` 解析 SPT 版本；锚定 BEM tag 的版本门禁，不匹配以结构化 `VERSION_MISMATCH` 拒绝并给出期望/实际版本）；`tarkov_server_status` 返回版本与门禁结果；`raid.*` 命名空间占位，全部返回 `CLIENT_BRIDGE_NOT_INSTALLED`。BEM tag 锚定点开工时复核（暂定 `5.0.0-BEM-20260910`，若有更新 tag 在 PR 中说明取舍）。

**Blocked by:** None (can start immediately)

**Status:** ready-for-human

- [ ] MCP 工程可按仓库惯例构建，vitest 通过
- [ ] fake `SptConnection` 驱动下，握手成功路径返回解析出的 SPT 版本
- [ ] 版本不匹配时握手拒绝，错误为结构化 `VERSION_MISMATCH`（含期望/实际版本）
- [ ] 传输层单测覆盖 shuffle 加解密往返、zlib 处理、cookie 会话
- [ ] `tarkov_server_status` 经 fake connection 返回版本/门禁结果
- [ ] 任意 `raid.*` 工具调用返回 `CLIENT_BRIDGE_NOT_INSTALLED`
- [ ] 测试只经 S1/S3 接缝断言外部行为，不触及内部实现

## Comments

### 2026-09-13 fixer ���� + orchestrator ǩ��

- ������ `tools/tarkov-runtime-mcp/`������㣨shuffle/zlib/cookie����������汾�Ž���`tarkov_server_status` / `tarkov_instances` / `raid.*` ռλ��
- orchestrator �������ˣ�`npx vitest run` 9 �ļ� 43 ����ȫ�̣�build/typecheck �ɾ���
- **ƫ���¼���Ž����ȣ�**��`/singleplayer/settings/version` ֻ���� `SPT 5.0.0 (BEM) <commit7>`��BEM tag �����ڶβ��ɹ۲⡣���ã��Ž��Ƚϡ����İ汾 + ����ͨ����BEM���������ڶν���ê���ʶ�����պ��辫ȷ�����ڣ�����Ѱ��¶����ʱ��Ķ˵��� Phase 2 �Ų��䡣
- �������ˣ����� BEM tag ȷ��Ϊ `5.0.0-BEM-20260910`�������ݶ�ê�㡣
- ��֪ follow-up��connect() �԰汾�˵����� GET�����Ż�������ʵ server �߼����������� ticket 06 ð�̡�
