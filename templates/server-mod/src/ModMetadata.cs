using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace {{ROOT_NAMESPACE}};

/// <summary>
/// SPT 4.1 mod 元数据：服务器据此识别 mod、校验 SPT 版本兼容性、检查依赖/冲突，并作为同优先级加载的 tiebreaker。
/// IModMetadata 是接口（4.0 的 AbstractModMetadata 已移除），所有属性必须实现，可选的赋 null。
/// </summary>
public record {{MOD_CLASS_NAME}}Metadata : IModMetadata
{
    /// <summary>全局唯一 ID，推荐反向域名记法（如 com.sammeow.mymod），禁止 "mymod"、"mod1" 这类易撞名</summary>
    public string ModGuid { get; init; } = "{{MOD_GUID}}";

    /// <summary>人类可读的 mod 名，展示给玩家</summary>
    public string Name { get; init; } = "{{MOD_NAME}}";

    public string Author { get; init; } = "{{MOD_AUTHOR}}";

    public List<string>? Contributors { get; init; }

    /// <summary>mod 自身版本，semver 三段式；"1.0.0.0" 四段式非法</summary>
    public Version Version { get; init; } = new("{{MOD_VERSION}}");

    /// <summary>兼容的 SPT 版本范围；~4.1.0 = >=4.1.0 且 &lt;4.2.0</summary>
    public Range SptVersion { get; init; } = new("~4.1.0");

    /// <summary>仅当含枚举 prepatch 定义（user/patchers/&lt;ModGuid&gt;/）时置 true，否则保持 false</summary>
    public bool HasPrepatcher { get; init; } = false;

    /// <summary>不兼容的 mod GUID 列表，存在任一则服务器拒绝加载</summary>
    public List<string>? Incompatibilities { get; init; }

    /// <summary>依赖的 mod：key 为 ModGuid，value 为要求的版本范围</summary>
    public Dictionary<string, Range>? ModDependencies { get; init; }

    public string? Url { get; init; }

    public string License { get; init; } = "{{MOD_LICENSE}}";
}
