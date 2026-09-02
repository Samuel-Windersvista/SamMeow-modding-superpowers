using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateTacticRequest : IRequestData
{
	[JsonPropertyName("aid")]
	public string? Aid { get; set; }

	[JsonPropertyName("tactic")]
	public string? Tactic { get; set; }
}
