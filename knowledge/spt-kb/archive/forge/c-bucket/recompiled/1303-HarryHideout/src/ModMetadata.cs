// ModMetadata（反编译补全）：Harry's Hideout 藏身处物资商人
// 4.1.2 IModMetadata 接口：init-only 属性 + SemanticVersioning 类型
// 4.0 原实现继承 AbstractModMetadata（record），4.1.2 改为实现 IModMetadata 接口
using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SemanticVersioning;

namespace _harryHideout;

public class ModMetadata : IModMetadata
{
	public string ModGuid { get; init; } = "com.acidphantasm.harryhideout";

	public string Name { get; init; } = "Harry Hideout";

	public string Author { get; init; } = "acidphantasm";

	public List<string>? Contributors { get; init; }

	public SemanticVersioning.Version Version { get; init; } = new("3.0.0");

	public SemanticVersioning.Range SptVersion { get; init; } = new("~4.1.0");

	public bool HasPrepatcher { get; init; }

	public List<string>? Incompatibilities { get; init; }

	public Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }

	public string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";

	public string License { get; init; } = "MIT";
}
