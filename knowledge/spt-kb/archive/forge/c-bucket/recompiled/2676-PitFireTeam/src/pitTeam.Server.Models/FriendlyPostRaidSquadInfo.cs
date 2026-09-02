using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace pitTeam.Server.Models;

public record FriendlyPostRaidSquadInfo
{
	[JsonPropertyName("Mate")]
	public bool Mate { get; set; }

	[JsonPropertyName("AllyBoss")]
	public string? AllyBoss { get; set; }

	[JsonPropertyName("Partial")]
	public bool Partial { get; set; }

	[JsonPropertyName("Lost")]
	public List<string> Lost { get; set; } = new List<string>();
}
