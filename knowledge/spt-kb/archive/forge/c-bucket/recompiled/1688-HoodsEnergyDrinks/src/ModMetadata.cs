// ModMetadata（4.0 record 反编译补全 → 4.1.2 IModMetadata 接口）
// 4.1.2 变更：AbstractModMetadata 移除，改实现 IModMetadata（全部 init-only 属性）
// 新增 HasPrepatcher 必填成员；SptVersion 补丁 ~4.0.0 → ~4.1.0
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace HoodsEnergyDrinks_CSharp;

public class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.hood.moreenergydrinks";
    public string Name { get; init; } = "Hoods Energy Drinks";
    public string Author { get; init; } = "Hood";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.2.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; } = "https://github.com/Hood26/HoodsEnergyDrinks-CSharp/tree/master";
    public string License { get; init; } = "MIT";
}
