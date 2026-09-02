// ModMetadata（反编译补全）：Scorpion 毒蝎商人
// 4.0 record 继承 AbstractModMetadata → 4.1.2 实现 IModMetadata 接口（init-only + SemanticVersioning）
// 属性结构与默认值来自原 DLL 的 ModMetadata::.ctor IL（ilspycmd -il 反编译）
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace _scorpion;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.acidphantasm.scorpion";
    public string Name { get; init; } = "Scorpion";
    public string Author { get; init; } = "acidphantasm";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0", false);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0", false);
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new SemanticVersioning.Range("~2.0.15", false) }
    };
    public string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";
    public string License { get; init; } = "MIT";
}
