// ModMetadata：SPT 4.0 -> 4.1.2 迁移
// 4.0 record ModMetadata : AbstractModMetadata -> 4.1 record ModMetadata : IModMetadata
// 关键修改：set -> init；SemanticVersioning 类型；SptVersion 补丁为 ~4.1.0；新增 HasPrepatcher
// 原 4.0 DLL 构造函数仅初始化 ModGuid/Name/Author/Version/SptVersion/License（IL 确认无 Url/依赖）
using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SemanticVersioning;

namespace IncreaseClimbHeight;

public record ModMetadata : IModMetadata
{
	public string ModGuid { get; init; } = "redlaser42.IncreaseClimbHeight";

	public string Name { get; init; } = "Increase Climb Height";

	public string Author { get; init; } = "redlaser42";

	public SemanticVersioning.Version Version { get; init; } = new SemanticVersioning.Version("2.0.1", false);

	public SemanticVersioning.Range SptVersion { get; init; } = new SemanticVersioning.Range("~4.1.0", false);

	public bool HasPrepatcher { get; init; } = false;

	public List<string>? Contributors { get; init; }

	public List<string>? Incompatibilities { get; init; }

	public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }

	public string? Url { get; init; }

	public string License { get; init; } = "MIT";
}
