using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace SecureMapbookMod;

/// <summary>
/// SecureMapbookMod 迁移版元数据。
/// 3.11 package.json 的 isBundleMod: true 已移除（4.1 检测 bundles.json 存在性）。
/// </summary>
public record SecureMapbookModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.mrvibesrsa.securemapbookmod";
    public string Name { get; init; } = "Secure Mapbook Mod";
    public string Author { get; init; } = "MrVibesRSA";
    public List<string>? Contributors { get; init; }
    public Version Version { get; init; } = new("1.0.0");
    public Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}
