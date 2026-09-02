using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateFollowerGenerateRequest : IRequestData
{
	[JsonPropertyName("MemberId")]
	public string? MemberId { get; set; }

	[JsonPropertyName("ScavId")]
	public string? ScavId { get; set; }

	[JsonPropertyName("Custom")]
	public FriendlyTeammateFollowerGenerateCustomization? Custom { get; set; }
}
