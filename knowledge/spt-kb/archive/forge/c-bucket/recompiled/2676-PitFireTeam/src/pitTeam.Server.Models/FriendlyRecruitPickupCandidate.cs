using System.Text.Json.Serialization;

namespace pitTeam.Server.Models;

public record FriendlyRecruitPickupCandidate
{
	[JsonPropertyName("ProfileId")]
	public string ProfileId { get; set; } = string.Empty;

	[JsonPropertyName("AccountId")]
	public string AccountId { get; set; } = string.Empty;

	[JsonPropertyName("Nickname")]
	public string Nickname { get; set; } = string.Empty;

	[JsonPropertyName("Level")]
	public int Level { get; set; }

	[JsonPropertyName("Side")]
	public string Side { get; set; } = string.Empty;

	[JsonPropertyName("Voice")]
	public string Voice { get; set; } = string.Empty;

	[JsonPropertyName("Head")]
	public string Head { get; set; } = string.Empty;

	[JsonPropertyName("ProfileJson")]
	public string ProfileJson { get; set; } = string.Empty;
}
