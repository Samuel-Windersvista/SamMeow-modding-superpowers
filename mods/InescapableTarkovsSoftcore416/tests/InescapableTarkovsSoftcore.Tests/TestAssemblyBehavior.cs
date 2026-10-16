using Xunit;

// 程序集级序列化：ExternalJsonFile 的 pending 告警队列是进程级共享状态，
// xUnit v2 默认跨 collection 并行会使「清空→入队→排空」序列交错（冷启动重建告警并发入队）。
// 测试耗时百毫秒级，串行成本可忽略；此举为后续同类全局态用例根除竞态。
[assembly: CollectionBehavior(DisableTestParallelization = true)]
