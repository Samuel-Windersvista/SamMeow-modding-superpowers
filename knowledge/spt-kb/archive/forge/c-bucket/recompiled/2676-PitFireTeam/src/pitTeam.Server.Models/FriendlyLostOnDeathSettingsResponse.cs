using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace pitTeam.Server.Models;

public record FriendlyLostOnDeathSettingsResponse
{
	[JsonPropertyName("equipment")]
	public Dictionary<string, bool> Equipment { get; set; } = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

	[JsonPropertyName("playerGearProtectedByRaidStatusOverride")]
	public bool PlayerGearProtectedByRaidStatusOverride { get; set; }
}
