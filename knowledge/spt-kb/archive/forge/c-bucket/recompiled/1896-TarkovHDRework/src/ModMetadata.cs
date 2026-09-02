using System.Collections.Generic;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SemanticVersioning;

namespace tarkovhdrework;

public record ModMetadata : IModMetadata
{
	public string ModGuid { get; init; } = "tarkov.hd.rework";

	public string Name { get; init; } = "TarkovHDRework";

	public string Author { get; init; } = "PulledP0rk";

	public List<string>? Contributors { get; init; }

	public Version Version { get; init; } = new Version("0.4.4");

	public Range SptVersion { get; init; } = new Range("~4.1.0");

	public bool HasPrepatcher { get; init; }

	public List<string>? Incompatibilities { get; init; }

	public Dictionary<string, Range>? ModDependencies { get; init; }

	public string? Url { get; init; }

	public string License { get; init; } = "CC BY-NC-ND 4.0";
}
