using System.Text.Json.Serialization;

namespace pitTeam.Server.Models;

public record FriendlySocialFriendProfile
{
	[JsonPropertyName("_id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("aid")]
	public string Aid { get; set; } = string.Empty;

	[JsonPropertyName("Info")]
	public FriendlySocialFriendInfo Info { get; set; } = new FriendlySocialFriendInfo();
}
