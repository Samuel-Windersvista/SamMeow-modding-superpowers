using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;

namespace RaidOverhaulMain.Models;

public record TransferRequestData : IRequestData
{
	[JsonPropertyName("items")]
	public List<Item>? Items { get; set; }

	[JsonPropertyName("traderId")]
	public string? TraderId { get; set; }

	[JsonPropertyName("message")]
	public string? Message { get; set; }
}
