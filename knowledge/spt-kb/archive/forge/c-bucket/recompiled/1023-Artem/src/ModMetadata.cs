// ModMetadata（反编译补全）：Artem 商人
// 4.1.2 IModMetadata 接口：init-only 属性 + SemanticVersioning 类型
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace Artem;

public class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.wtt.artem";
    public string Name { get; init; } = "Artem Trader";
    public string Author { get; init; } = "WTT";
    public List<string> Contributors { get; init; } = [];
    public SemanticVersioning.Version Version { get; init; } = new(3, 0, 1);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string> Incompatibilities { get; init; } = [];
    public Dictionary<string, SemanticVersioning.Range> ModDependencies { get; init; } = new()
    {
        { "com.wtt.servercommonlib", new SemanticVersioning.Range(">=3.0.0 <4.0.0") }
    };
    public string Url { get; init; } = "";
    public string License { get; init; } = "";
}
