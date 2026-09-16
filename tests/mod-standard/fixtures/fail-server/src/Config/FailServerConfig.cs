namespace FailServer.Config;

// 配置类本身合规（不标注 Injectable 特性）：CFG-004 仍 PASS
public class FailServerConfig
{
    public bool Enabled { get; set; } = true;
}
