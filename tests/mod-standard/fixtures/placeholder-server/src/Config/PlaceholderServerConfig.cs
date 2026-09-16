namespace PlaceholderServer.Config;

// STD-CFG-004：配置类不得标注 Injectable 特性
public class PlaceholderServerConfig
{
    public bool Enabled { get; set; } = true;
}
