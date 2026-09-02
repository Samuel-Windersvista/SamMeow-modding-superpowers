using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateDeathEscapeRequest : IRequestData
{
	public bool Notify { get; set; } = true;

	public bool ResolveOnly { get; set; }

	[JsonPropertyName("Entries")]
	public List<FriendlyTeammateDeathEscapeEntry> Entries { get; set; } = new List<FriendlyTeammateDeathEscapeEntry>();
}
