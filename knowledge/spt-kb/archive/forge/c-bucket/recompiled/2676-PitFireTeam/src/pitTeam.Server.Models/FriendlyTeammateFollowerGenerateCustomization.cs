using System.Text.Json.Serialization;

namespace pitTeam.Server.Models;

public record FriendlyTeammateFollowerGenerateCustomization
{
	[JsonPropertyName("Health")]
	public double? Health { get; set; }

	[JsonPropertyName("English")]
	public bool? English { get; set; }
}
