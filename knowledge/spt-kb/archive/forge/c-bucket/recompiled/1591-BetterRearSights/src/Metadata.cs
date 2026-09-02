// Metadata（反编译重写）：Better Rear Sights by pein
// 4.0 record Metadata : AbstractModMetadata -> 4.1 class Metadata : IModMetadata
// 关键修改：set -> init；SemanticVersioning 类型；SptVersion 补丁为 ~4.1.0；新增 HasPrepatcher
// 原 4.0 DLL 构造函数未初始化 Contributors/Incompatibilities/ModDependencies（无 com.wtt.commonlib 依赖）
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace SPTBetterRearSights;

public class Metadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.pein.betterrearsights";
    public string Name { get; init; } = "Better Rear Sights";
    public string Author { get; init; } = "pein";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.7.0", false);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0", false);
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/peinwastaken";
    public string License { get; init; } = "MIT";
}
