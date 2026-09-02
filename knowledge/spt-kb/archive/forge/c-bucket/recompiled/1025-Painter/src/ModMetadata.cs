// ModMetadata（反编译补全）：Painter 油漆匠商人
// SptVersion 从 ~4.0.0 改为 ~4.1.0
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace Painter;

public class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.painter.trader";
    public string Name { get; init; } = "Painter Trader";
    public string Author { get; init; } = "EpicRangeTime";
    public List<string> Contributors { get; init; } = [];
    public SemanticVersioning.Version Version { get; init; } = new(2, 0, 0);
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
