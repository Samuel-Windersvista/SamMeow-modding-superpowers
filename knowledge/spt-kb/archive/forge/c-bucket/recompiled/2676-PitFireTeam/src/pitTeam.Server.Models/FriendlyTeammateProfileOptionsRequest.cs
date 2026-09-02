using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateProfileOptionsRequest : IRequestData
{
	[JsonPropertyName("aid")]
	public string? Aid { get; set; }
}
