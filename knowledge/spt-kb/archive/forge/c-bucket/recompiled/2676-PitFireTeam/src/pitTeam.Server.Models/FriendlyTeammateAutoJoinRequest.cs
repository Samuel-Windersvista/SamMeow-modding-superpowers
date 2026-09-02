using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateAutoJoinRequest : IRequestData
{
	[JsonPropertyName("aid")]
	public string Aid { get; set; } = string.Empty;

	[JsonPropertyName("enabled")]
	public bool Enabled { get; set; }
}
