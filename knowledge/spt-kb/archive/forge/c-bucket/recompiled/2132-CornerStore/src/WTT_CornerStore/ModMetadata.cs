// ModMetadata（4.0 record 反编译补全 → 4.1.2 IModMetadata 接口）
// 4.1.2 变更：AbstractModMetadata 移除，改实现 IModMetadata（全部 init-only 属性）
// 新增 HasPrepatcher 必填成员；SptVersion ~4.0.2 → ~4.1.0
// ModDependencies com.wtt.commonlib：~2.0.0 → >=3.0.0 <4.0.0（4.1 CommonLib 为 3.0.3）
// 4.0 的 IsBundleMod=true 在 IModMetadata 中不存在，已丢弃（HasPrepatcher 对应物）
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace WTT_CornerStore;

public class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = "com.libertypiratewtt.cornerstore";
    public string Name { get; init; } = "WTT - Corner Store";
    public string Author { get; init; } = "RockaHorse";
    public List<string>? Contributors { get; init; }
    public SemanticVersioning.Version Version { get; init; } = new("1.0.0");
    public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");
    public bool HasPrepatcher { get; init; } = false;
    public List<string>? Incompatibilities { get; init; }
    public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; } = new()
    {
        { "com.wtt.commonlib", new SemanticVersioning.Range(">=3.0.0 <4.0.0") }
    };
    public string? Url { get; init; }
    public string License { get; init; } = "MIT";
}
