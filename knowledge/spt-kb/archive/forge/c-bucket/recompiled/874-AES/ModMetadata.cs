using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SemanticVersioning;

namespace AES_Trader;

public record ModMetadata : IModMetadata
{
	public string ModGuid { get; init; } = "com.dono.aes";

	public string Name { get; init; } = "AES";

	public string Author { get; init; } = "Flowless";

	public List<string>? Contributors { get; init; } = new List<string> { "Colobos9mm" };

	public Version Version { get; init; } = new Version("0.7.9", false);

	public Range SptVersion { get; init; } = new Range("~4.1.0", false);

	public bool HasPrepatcher { get; init; }

	public List<string>? Incompatibilities { get; init; } = new List<string> { "ReadJsonConfigExample" };

	public Dictionary<string, Range>? ModDependencies { get; init; }

	public string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";

	public string License { get; init; } = "MIT";
}
