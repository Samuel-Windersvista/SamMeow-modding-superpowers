namespace {{ROOT_NAMESPACE}}.Shared;

/// <summary>
/// 两端共享的纯数据 / 常量。
/// STD-STRUCT-004：Client 与 Server 共享的纯数据、常量放 Shared/，两端共同引用，避免各写一份导致漂移。
/// 本工程不得引用 SPT 或 BepInEx 类型，只放能被 netstandard2.1（客户端）与 net10.0（服务端）同时消费的内容。
/// </summary>
public static class SharedConstants
{
    /// <summary>示例：客户端调用、服务端注册的 SPT 路由路径；两端必须一致。</summary>
    public const string PingRoute = "/spt/{{MOD_CLASS_NAME}}/ping";

    /// <summary>示例：客户端与服务端握手用的协议版本。</summary>
    public const int ProtocolVersion = 1;
}
