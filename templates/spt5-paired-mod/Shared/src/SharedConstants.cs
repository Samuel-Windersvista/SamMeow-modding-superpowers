namespace {{ROOT_NAMESPACE}}.Shared;

/// <summary>
/// 两端共享的纯数据 / 常量。
/// STD-STRUCT-004：Client 与 Server 共享的纯数据、常量放 Shared/，两端共同引用，避免各写一份导致漂移。
/// 本工程不得引用 SPT 或 BepInEx 类型，只放能被 net6.0（客户端）与 net10.0（服务端）同时消费的内容。
/// </summary>
public static class SharedConstants
{
    /// <summary>
    /// 示例：服务端注册的 SPT 路由路径（供客户端/工具调用）；模板样例中仅服务端消费本常量，
    /// 客户端仅消费 ProtocolVersion。
    /// 服务端示例路由 {{MOD_CLASS_NAME}}Router 以此常量作为注册路径
    /// （KB 约定：mod 自定义路由使用独立前缀 /spt/…，见 api-notes-5.0/http-routing.md；
    /// 占位段正式命名建议替换为小写 kebab，如 /spt/my-mod/ping）。
    /// </summary>
    public const string ExampleRoute = "/spt/{{MOD_CLASS_NAME}}/example";

    /// <summary>示例：客户端与服务端握手用的协议版本。</summary>
    public const int ProtocolVersion = 1;
}
