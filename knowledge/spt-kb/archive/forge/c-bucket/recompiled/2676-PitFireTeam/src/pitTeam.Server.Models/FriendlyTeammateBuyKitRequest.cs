using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateBuyKitRequest : IRequestData
{
	[JsonPropertyName("aid")]
	public string? Aid { get; set; }

	[JsonPropertyName("items")]
	public List<Item>? Items { get; set; }

	[JsonPropertyName("price")]
	public int Price { get; set; }

	[JsonPropertyName("useItemsInStash")]
	public bool UseItemsInStash { get; set; }

	[JsonPropertyName("usedItems")]
	public List<FriendlyTeammateBuyKitUsedItem>? UsedItems { get; set; }
}
