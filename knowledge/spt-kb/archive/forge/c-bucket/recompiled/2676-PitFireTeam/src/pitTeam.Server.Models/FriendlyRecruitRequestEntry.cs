using System.Text.Json.Serialization;

namespace pitTeam.Server.Models;

public record FriendlyRecruitRequestEntry : FriendlyRecruitPickupCandidate
{
	[JsonPropertyName("createdAt")]
	public long CreatedAt { get; set; }
}
