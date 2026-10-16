using SamMeow.LLMTraderChat.Shared;
using SPTarkov.Server.Core.Models.Spt.Mod;
using Range = SemanticVersioning.Range;
using Version = SemanticVersioning.Version;

namespace SamMeow.LLMTraderChat.Server;

/// <summary>
/// SPT 服务端 mod 元数据：本 mod 目录内唯一的 <see cref="IModMetadata"/> 实现
/// （同目录的 Shared.dll 不得再实现该接口）。
/// 版本取自集中生成的 ModVersion.Value（与客户端半共用同一版本）。
/// </summary>
public sealed class ModMetadata : IModMetadata
{
    public string ModGuid { get; init; } = SharedConstants.ModGuid;

    public string Name { get; init; } = SharedConstants.ModName;

    public string Author { get; init; } = "SamMeow";

    public List<string>? Contributors { get; init; }

    public Version Version { get; init; } = new(ModVersion.Value);

    /// <summary>~4.1.0 = &gt;=4.1.0 且 &lt;4.2.0。</summary>
    public Range SptVersion { get; init; } = new("~4.1.0");

    public bool HasPrepatcher { get; init; } = false;

    public List<string>? Incompatibilities { get; init; }

    public Dictionary<string, Range>? ModDependencies { get; init; }

    public string? Url { get; init; }

    public string License { get; init; } = "MIT";
}
