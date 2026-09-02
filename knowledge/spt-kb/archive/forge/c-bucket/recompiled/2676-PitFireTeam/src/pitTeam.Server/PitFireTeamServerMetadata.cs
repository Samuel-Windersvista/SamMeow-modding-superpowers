using System.Collections.Generic;
using SemanticVersioning;
using SPTarkov.Server.Core.Models.Spt.Mod;

namespace pitTeam.Server;

public record PitFireTeamServerMetadata : IModMetadata
{
	public string ModGuid { get; init; } = "xyz.pit.fireteam";

	public string Name { get; init; } = "PitFireTeam";

	public string Author { get; init; } = "PitAlex";

	public List<string>? Contributors { get; init; }

	public Version Version { get; init; } = new("0.9.0");

	public Range SptVersion { get; init; } = new("~4.1.0");

	public bool HasPrepatcher { get; init; }

	public List<string>? Incompatibilities { get; init; }

	public Dictionary<string, Range>? ModDependencies { get; init; }

	public string? Url { get; init; } = "https://github.com/pitAlex/SPT-PitFireTeam";

	public string License { get; init; } = "MIT";
}
