using System.Text.Json.Serialization;

namespace pitTeam.Server.Models;

public record FriendlyTeammateDeleteResponse
{
	[JsonPropertyName("deleted")]
	public bool Deleted { get; set; }
}
