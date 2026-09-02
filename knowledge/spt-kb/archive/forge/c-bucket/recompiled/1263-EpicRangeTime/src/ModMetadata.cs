// ModMetadata（反编译补全 + 4.1 适配）：Epic's All in One（forgeId 1263）
// 4.0.13 record 继承 AbstractModMetadata（4.1 移除）→ IModMetadata；属性 set→init
// SptVersion ~4.0.13 → ~4.1.0；ModDependencies com.wtt.commonlib ~2.0.20 → >=3.0.0 <4.0.0
// IsBundleMod 属性 4.1 IModMetadata 已删除，不实现
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace EpicsAIO;

public class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.epicrangetime.aio";
    public string Name { get; init; } = "Epics All in One";
    public string Author { get; init; } = "EpicRangeTime";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new(4, 0, 8);
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new SemanticVersioning.Range(">=3.0.0 <4.0.0") }
    };
    public string? Url { get; init; }
    public string License { get; init; } = "CC-BY-NC-ND 4.0";
}
