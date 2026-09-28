using SPTarkov.Server.Core.Models.Spt.Mod;
using Version = SemanticVersioning.Version;
using Range = SemanticVersioning.Range;

namespace {{ROOT_NAMESPACE}}.Server;

/// <summary>
/// SPT 5.0 mod 元数据：服务器据此识别 mod、校验 SPT 版本兼容性、检查依赖/冲突，并作为同优先级加载的 tiebreaker。
/// STD-META-001：必须实现 IModMetadata，覆盖全部属性（可选属性赋 null），一个 mod 目录内恰好一个实现。
/// STD-META-002：元数据实现独立放在 ModMetadata.cs。
/// STD-PKG-004：一个 SPT_Runtime/user/mods/&lt;Name&gt;/ 目录内只允许这一个 IModMetadata 实现；
///              同目录内的 {{MOD_CLASS_NAME}}.Shared.dll 等其它 assembly 不得再实现该接口。
/// STD-META-007 / STD-PKG-005：Version 取自集中定义的 ModVersion.Value（Directory.Build.props 的 $(Version)），
///                              与客户端半共用同一版本，不在此另写版本字符串。
/// 5.0 说明：IModMetadata 是接口，11 个属性与 4.1 完全相同；服务器加载器只扫 mod 目录顶层 DLL，
///          不再使用 package.json / mod.json——mod 身份完全由此文件承担。
/// </summary>
// STD-META-001 / STD-META-002 / STD-PKG-004
public record {{MOD_CLASS_NAME}}Metadata : IModMetadata
{
    /// <summary>STD-META-003：全局唯一 ID，反向域名记法（如 com.sammeow.mymod），禁止 "mymod"、"mod1" 这类易撞名</summary>
    public string ModGuid { get; init; } = "{{MOD_GUID}}";

    /// <summary>人类可读的 mod 名，展示给玩家</summary>
    public string Name { get; init; } = "{{MOD_NAME}}";

    public string Author { get; init; } = "{{MOD_AUTHOR}}";

    public List<string>? Contributors { get; init; }

    /// <summary>STD-META-005：mod 自身版本，semver 三段式；来源为集中定义的 $(Version)（见 Directory.Build.props）</summary>
    public Version Version { get; init; } = new(ModVersion.Value);

    /// <summary>
    /// STD-META-004 / STD-VER-002：兼容的 SPT 版本范围。
    /// 5.0 必填含 5.0.0 的区间（~5.0.0 = >=5.0.0 且 &lt;5.1.0），否则服务器 Satisfies 校验失败、mod 被拒载。
    /// </summary>
    public Range SptVersion { get; init; } = new("~5.0.0");

    /// <summary>仅当含枚举 prepatch 定义（user/patchers/&lt;ModGuid&gt;/）时置 true，否则保持 false</summary>
    public bool HasPrepatcher { get; init; } = false;

    /// <summary>不兼容的 mod GUID 列表，存在任一则服务器拒绝加载</summary>
    public List<string>? Incompatibilities { get; init; }

    /// <summary>STD-DEP-002：依赖的 mod（key 为 ModGuid，value 为要求的版本范围）；无硬依赖时保持 null</summary>
    public Dictionary<string, Range>? ModDependencies { get; init; }

    public string? Url { get; init; }

    public string License { get; init; } = "{{MOD_LICENSE}}";
}
