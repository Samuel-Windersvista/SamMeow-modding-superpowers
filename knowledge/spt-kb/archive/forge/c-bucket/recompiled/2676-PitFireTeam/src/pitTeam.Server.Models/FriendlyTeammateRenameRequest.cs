using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateRenameRequest : IRequestData
{
	[JsonPropertyName("aid")]
	public string? Aid { get; set; }

	[JsonPropertyName("nickname")]
	public string? Nickname { get; set; }
}
