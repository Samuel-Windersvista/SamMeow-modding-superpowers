using System.Text.Json.Serialization;

namespace pitTeam.Server.Models;

public record FriendlyTeammateBuyKitUsedItem
{
	[JsonPropertyName("itemId")]
	public string? ItemId { get; set; }

	[JsonPropertyName("templateId")]
	public string? TemplateId { get; set; }

	[JsonPropertyName("count")]
	public int Count { get; set; }
}
