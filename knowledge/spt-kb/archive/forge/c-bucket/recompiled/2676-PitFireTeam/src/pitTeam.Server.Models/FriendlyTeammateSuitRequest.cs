using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateSuitRequest : IRequestData
{
	[JsonPropertyName("aid")]
	public string? Aid { get; set; }

	[JsonPropertyName("suit")]
	public string[]? Suit { get; set; }
}
