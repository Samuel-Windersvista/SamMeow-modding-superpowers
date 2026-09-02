using System.Collections.Generic;
using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace pitTeam.Server.Models;

public record FriendlyTeammateBuyKitResponse
{
	[JsonPropertyName("playerStashItems")]
	public List<Item>? PlayerStashItems { get; set; }
}
