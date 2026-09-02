using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace pitTeam.Server.Models;

public record FriendlyTeammateRepairEquipmentResponse
{
	[JsonPropertyName("itemId")]
	public string? ItemId { get; set; }

	[JsonPropertyName("durability")]
	public double? Durability { get; set; }

	[JsonPropertyName("maxDurability")]
	public double? MaxDurability { get; set; }

	[JsonPropertyName("playerStashItems")]
	public List<Item>? PlayerStashItems { get; set; }

	[JsonPropertyName("playerNewStashItems")]
	public List<Item>? PlayerNewStashItems { get; set; }

	[JsonPropertyName("playerChangedStashItems")]
	public List<Item>? PlayerChangedStashItems { get; set; }

	[JsonPropertyName("playerDeletedStashItemIds")]
	public List<string>? PlayerDeletedStashItemIds { get; set; }
}
