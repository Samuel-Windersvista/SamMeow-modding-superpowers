namespace PassServer.Config;

// STD-CFG-004：配置类不得标注 Injectable 特性，由 IOnDIConstruct 注册后构造注入消费
public class PassServerConfig
{
    public bool Enabled { get; set; } = true;
}
