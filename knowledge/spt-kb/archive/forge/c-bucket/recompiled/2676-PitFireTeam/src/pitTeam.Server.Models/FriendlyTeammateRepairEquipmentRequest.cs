using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Repair;
using SPTarkov.Server.Core.Models.Utils;

namespace pitTeam.Server.Models;

public record FriendlyTeammateRepairEquipmentRequest : IRequestData
{
	[JsonPropertyName("aid")]
	public string? Aid { get; set; }

	[JsonPropertyName("target")]
	public string? Target { get; set; }

	[JsonPropertyName("repairKitsInfo")]
	public List<RepairKitsInfo>? RepairKitsInfo { get; set; }

	[JsonPropertyName("traderId")]
	public string? TraderId { get; set; }

	[JsonPropertyName("repairCount")]
	public double? RepairCount { get; set; }
}
