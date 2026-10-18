using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace SamMeow.DoorBreachOverhaulServer;

/// <summary>
/// SPT 4.1 mod 元数据：服务器据此识别 mod、校验 SPT 版本兼容性、检查依赖/冲突，并作为同优先级加载的 tiebreaker。
/// STD-META-001：必须实现 IModMetadata，覆盖全部属性（可选属性赋 null），一个 mod 目录内恰好一个实现。
/// STD-META-002：元数据实现独立放在 ModMetadata.cs。
/// 4.1 的 IModMetadata 是接口（不是 4.0 的抽象 record），不要写 override；IsBundleMod 已移除。
/// </summary>
// STD-META-001 / STD-META-002
public record DoorBreachOverhaulServerMetadata : IModMetadata
{
    /// <summary>STD-META-003：全局唯一 ID，反向域名记法</summary>
    public string ModGuid { get; init; } = "com.sammeow.doorbreach.server";

    /// <summary>人类可读的 mod 名，展示给玩家</summary>
    public string Name { get; init; } = "Door Breach Overhaul Server（破门大修服务端）";

    public string Author { get; init; } = "SamMeow";

    public List<string>? Contributors { get; init; }

    /// <summary>STD-META-005：mod 自身版本，semver 三段式，与 csproj &lt;Version&gt; 同源</summary>
    public Version Version { get; init; } = new("0.1.0");

    /// <summary>STD-META-004：兼容的 SPT 版本范围；~4.1.0 = &gt;=4.1.0 且 &lt;4.2.0（tilde 区间）</summary>
    public Range SptVersion { get; init; } = new("~4.1.0");

    /// <summary>仅当含枚举 prepatch 定义（user/patchers/&lt;ModGuid&gt;/）时置 true，否则保持 false</summary>
    public bool HasPrepatcher { get; init; } = false;

    /// <summary>不兼容的 mod GUID 列表，存在任一则服务器拒绝加载</summary>
    public List<string>? Incompatibilities { get; init; }

    /// <summary>依赖的 mod：key 为 ModGuid，value 为要求的版本范围（本项目无依赖）</summary>
    public Dictionary<string, Range>? ModDependencies { get; init; }

    public string? Url { get; init; }

    public string License { get; init; } = "MIT";
}
