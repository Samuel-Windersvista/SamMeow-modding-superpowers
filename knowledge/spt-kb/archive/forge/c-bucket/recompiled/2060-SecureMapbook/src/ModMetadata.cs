// ModMetadata（反编译补全）：MrVibesRSA-SecureMapbook 4.0 → 4.1.2
// 4.0 record 继承 AbstractModMetadata → 4.1.2 实现 IModMetadata 接口（init-only + SemanticVersioning）
// 属性结构与默认值来自原 DLL 的 ModMetadata::.ctor IL（ilspycmd -il 反编译）：
//   Name="SecureMapbook", Author="MrVibesRSA", Version=1.5.5, SptVersion="~4.1.0"（已补丁）, IsBundleMod=true,
//   License="MIT", ModGuid="com.mrvibesrsa.securemapbook"；Contributors/Incompatibilities/ModDependencies/Url 未初始化（null）
// 4.1.2 接口新增 HasPrepatcher（原基类默认 false）。
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace _securemapbook;

public record ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.mrvibesrsa.securemapbook";
    public string Name { get; init; } = "SecureMapbook";
    public string Author { get; init; } = "MrVibesRSA";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.5.5", false);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0", false);
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";

    // 4.0 AbstractModMetadata 遗留属性：原 mod 为 bundle mod（bundles.json + bundles/）
    public bool? IsBundleMod { get; init; } = true;
}
