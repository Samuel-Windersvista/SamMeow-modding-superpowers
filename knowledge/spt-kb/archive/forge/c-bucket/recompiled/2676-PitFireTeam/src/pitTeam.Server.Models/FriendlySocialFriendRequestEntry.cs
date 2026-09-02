using System.Text.Json.Serialization;

namespace pitTeam.Server.Models;

public record FriendlySocialFriendRequestEntry
{
	[JsonPropertyName("_id")]
	public string Id { get; set; } = string.Empty;

	[JsonPropertyName("from")]
	public string From { get; set; } = string.Empty;

	[JsonPropertyName("to")]
	public string To { get; set; } = string.Empty;

	[JsonPropertyName("date")]
	public double Date { get; set; }

	[JsonPropertyName("profile")]
	public FriendlySocialFriendProfile Profile { get; set; } = new FriendlySocialFriendProfile();
}
