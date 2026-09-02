// ModMetadata（反编译补全）：Eukyre's Consortium of Things（新配件）
// 4.0 record : AbstractModMetadata -> 4.1.2 class : IModMetadata（set->init，补 HasPrepatcher）
// SptVersion 保持原 DLL 已补丁值 ~4.1.0
// 依赖修正：包内 WTT-ServerCommonLib 为 com.wtt.commonlib 3.0.3（原 ~2.0.20 范围不匹配）；
//           com.epicrangetime.aio 不在本整合包中，移除该依赖。
using System.Reflection;
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace EukyreECOT;

public class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.eukyre.ecot";
    public string Name { get; init; } = "Eukyre-Consortium";
    public string Author { get; init; } = "GrooveypenguinX, ProbablyEukyre";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new(
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.3.5", false);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new SemanticVersioning.Range(">=3.0.0 <4.0.0") }
    };
    public string? Url { get; init; }
    public string License { get; init; } = "CC-BY-NC-ND 4.0";

    // 4.0 AbstractModMetadata 遗留字段（4.1.2 IModMetadata 无此项，保留供参考）
    public bool? IsBundleMod { get; init; } = true;
}
