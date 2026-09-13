# 02: 快照组装�?+ profile section

**What to build:** 第一个真实状态读取能力。实�?`tarkov_snapshot` 工具与确定性快�?schema（字段排序稳定、计数型摘要而非全量枚举、每�?section 标注数据来源与新鲜度）；实现 `profile` section：经 `/client/*` 路由取得 profile 数据并归一为摘要（等级/技�?任务进度计数）。sections 可选参数骨架就位（本期只有 `profile` 一个合法值，未知 section �?`UNSUPPORTED_SECTION`）�?

**Blocked by:** 01（穿甲弹：握手与传输层）

**Status:** ready-for-human

- [ ] `tarkov_snapshot` �?`sections: ["profile"]` 返回等级/技�?任务进度计数摘要
- [ ] 快照输出确定性：相同 fixture 输入两次调用产出逐字节一�?
- [ ] schema 标注数据来源（路由名）与数据新鲜�?
- [ ] 请求未知 section 返回结构�?`UNSUPPORTED_SECTION`
- [ ] 测试�?fake connection + fixture 路由响应驱动，断言归一结果而非 HTTP 细节
